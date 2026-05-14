# AEGIS — ANALISI STATO PIATTAFORMA BACKEND
## FA-003 — Report Tecnico Senior Developer Analyst
**Data**: 2026-05-14

---

## 1. STATO ATTUALE — COSA È IMPLEMENTATO

| Fase | Endpoint coperti | Stato |
|------|-----------------|-------|
| Auth | Register, Login, Refresh, Revoke | ✅ Completo |
| Profilo | CRUD, Skills, Enrichment (percentile, stagnation, quick wins) | ✅ Completo |
| Assessment | Start, Answer, Result, History, Abandon | ✅ Completo |
| Raccomandazioni | Generate, Latest, History | ✅ Completo |
| Market | KPI per skill/ruolo, trends | ✅ Struttura completa |
| Monitoring | Dashboard, Alerts, Mark read, History | ✅ Completo |
| Jobs | SkillSync (daily), Alerting (daily), Snapshot (weekly) | ✅ Schedulati |
| Security | JWT rotation, GlobalLimiter, auth rate limit | ✅ Implementato |

L'architettura segue Clean Architecture + CQRS in modo coerente. Il flow utente è percorribile end-to-end. I meccanismi di scoring (SkillROI, salary percentile, stagnation risk, composite recommendation score) sono deterministici e documentati. I Hangfire job sono schedulati e registrati correttamente.

---

## 2. COSA MANCA PER UNA PIATTAFORMA BACKEND COMPLETA

### 2.1 Pipeline Dati KPI — GAP CRITICO BLOCCANTE

`KpiComputationService` esiste e implementa tutte le formule (DemandScore, AIRiskScore, SalaryMedian), ma riceve `RawMarketData` come input — e nulla produce questo input. La tabella `market_kpis` è vuota in produzione.

**Impatto reale**: salary percentile = null, alert non scattano mai (condizioni su DemandScore/AIRiskScore sempre false), recommendation scoring cade su valori di default `5.0` per ogni ruolo.

Serve integrare almeno una fonte dati esterna:
- **Adzuna API** (piano gratuito: 250 req/giorno — sufficiente per POC): offre job count per ruolo/paese, salary ranges
- **Remotive.io / JobsPikr** per remote salary data
- Un nuovo `KpiIngestionJob` Hangfire (settimanale) che chiama le API esterne, costruisce `RawMarketData`, invoca `KpiComputationService.ComputeForRoleAsync()` per ciascuno dei 10 ruoli definiti in `SkillPrioritizationService.RoleRequiredSkills`

### 2.2 Gestione Account Completa

Mancano endpoint che ogni piattaforma B2C richiede prima del go-live:

| Endpoint | Motivo |
|----------|--------|
| `PUT /auth/change-password` | Cambio password autenticato |
| `POST /auth/forgot-password` | Richiesta reset (invia email con token firmato HMACSHA256) |
| `POST /auth/reset-password` | Reset con token temporaneo |
| `GET /profile/export` | GDPR Art. 20 — diritto alla portabilità dei dati |
| `DELETE /profile` | GDPR Art. 17 — diritto all'oblio con cascade su tutti i dati personali |

### 2.3 Verifica Email

La registrazione non verifica l'indirizzo email. In produzione questo consente registrazioni con email inesistenti, impedisce il recupero password, e viola le best practice di deliverability per le email transazionali.

Serve: token di verifica firmato (HMACSHA256 + expiry timestamp), `POST /auth/verify-email?token=...`, blocco delle funzionalità sensibili per utenti non verificati.

### 2.4 Enforcement Subscription Tier

`SubscriptionTier` esiste sull'entità `User` ma:
- Non c'è enforcement in `GenerateRecommendationCommand` (utenti Free generano raccomandazioni illimitate)
- Il check limite mensile assessment è hard-coded, non configurabile per tier
- Manca il flow di upgrade: `POST /subscriptions/upgrade`, webhook Stripe per conferma pagamento, downgrade automatico a scadenza

### 2.5 Notifiche Push/Email

Gli alert vengono scritti nel database ma l'utente deve fare polling attivo sul dashboard. In produzione questo è inutilizzabile: un utente non aprirà l'app ogni giorno per controllare.

Serve un sistema di delivery:
- Email digest settimanale con gli alert accumulati (non letti)
- Email immediata per alert di severità `High`
- (Fase 2 opzionale) Push notification via Firebase Cloud Messaging

### 2.6 EmbeddingVector Pipeline

`Skill.EmbeddingVector` (`float[1536]`, pgvector) è configurato ma mai popolato. Il campo è pensato per la ricerca semantica delle skill, ma attualmente la ricerca è solo `ILIKE` su `Name`/`CanonicalName`/`NameIt`.

Serve un Hangfire job che per ogni skill sincronizzata dall'ESCO chiami OpenAI `text-embedding-3-small`, ottenga il vettore 1536-dimensionale e lo persista. Costo indicativo: ~$0.0001 per skill.

### 2.7 Admin / Backoffice API

Non esistono endpoint protetti dal ruolo `Admin` per:
- Visualizzare e gestire tutti gli utenti
- Triggerare manualmente i job Hangfire (attualmente solo via dashboard UI con credenziali embed)
- Inserire manualmente dati KPI prima che la pipeline automatica sia operativa
- Vedere metriche aggregate (assessments/giorno, raccomandazioni generate, retention)

### 2.8 Audit Log Strutturato

Operazioni sensibili (login, cambio password, export dati, generazione raccomandazione) non producono un audit trail separato. I log Serilog catturano le richieste HTTP ma non le azioni di business. In produzione (specialmente con dati GDPR) è necessaria un'entità `AuditEvent` con: `UserId`, `Action`, `Timestamp`, `IpAddress`, `UserAgent`, `Result`.

### 2.9 Soft Delete Coerente

Non è verificabile dalla struttura effettiva di `Entity` base se `IsDeleted` sia ereditato coerentemente. Se assente, la cancellazione è hard delete — problematico per audit e GDPR (spesso serve anonimizzazione, non eliminazione fisica).

### 2.10 API Versioning Formale

Le route hanno `/api/v1/` come prefisso URL ma non c'è un meccanismo formale di versioning. Quando si rilascerà una breaking change in un DTO, non sarà possibile mantenere la compatibilità con i client v1 esistenti.

---

## 3. IMPLEMENTAZIONI TECNICHE PER UNA PIATTAFORMA COMPETITIVA

### 3.1 Redis — Caching Distribuito (già configurato, mai usato)

Redis è presente in `ConnectionStrings.Redis` e registrato con `AddStackExchangeRedisCache`, ma `IDistributedCache` non viene iniettato in nessun handler. Il sistema esegue N query PostgreSQL per ogni richiesta, incluse le KPI che cambiano al massimo una volta a settimana.

**Cosa cachare e con quale TTL**:

```
kpi:role:{role}:{geo}         → TTL 6h   (cambiano settimanalmente)
skills:search:{query}         → TTL 15m  (catalogo aggiornato giornalmente)
assessment:questions:{hash}   → TTL 24h  (riduce chiamate LLM costose)
user:dashboard:{userId}       → TTL 5m   (evita 3 query aggregate per ogni refresh)
```

**Redis come Rate Limiter distribuito**: l'attuale `FixedWindowLimiter` è in-memory. Con più istanze del servizio (load balancer), ogni istanza ha il suo contatore separato e un attacker può fare `10 × N` richieste. Soluzione: `RedisRateLimitingExtensions` (package `dotnet-stack/aspnetcore-rate-limiting`) o contatori atomici via Lua script.

### 3.2 Polly — Resilienza per Chiamate Esterne

`EscoApiClient`, `AnthropicOrchestrator`, e la futura pipeline KPI chiamano API esterne. Attualmente i fallback sono solo `try/catch` con log warning. In produzione, un timeout o un errore 429 dall'API ESCO interrompe l'intero job.

**Implementazione con `Microsoft.Extensions.Http.Resilience`**:

```csharp
builder.Services.AddHttpClient<EscoApiClient>(...)
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(2);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    });
```

Introduce: retry con backoff esponenziale, circuit breaker (smette di chiamare ESCO dopo N fallimenti consecutivi evitando cascade), timeout globale per request.

### 3.3 OpenTelemetry — Distributed Tracing

Prometheus misura metriche aggregate, Serilog logga eventi, ma non c'è tracciabilità end-to-end di una singola richiesta attraverso MediatR handlers, repository, e chiamate LLM. In produzione, diagnosticare "perché la raccomandazione per l'utente X ha impiegato 8 secondi" è impossibile senza trace.

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Aegis.Application")
        .AddOtlpExporter(o => o.Endpoint = new Uri(config["Otlp:Endpoint"])));
```

Backend consigliato: Jaeger (self-hosted) o Grafana Tempo. Ogni `GenerateRecommendationCommand` diventerebbe un trace con span separati per KPI fetch, LLM calls, DB writes.

### 3.4 FluentEmail + SendGrid — Email Transazionale

Alert, password reset, verifica email, digest settimanale — tutto richiede un sistema email affidabile con template HTML.

**Stack consigliato**:
- `FluentEmail.Core` + `FluentEmail.SendGrid` (o Mailgun per costi inferiori)
- Template Razor per HTML email renderizzati server-side
- `IEmailService` in Application layer (rispetta Clean Architecture)
- `EmailDigestJob` Hangfire domenicale per il digest settimanale degli alert non letti

```csharp
public interface IEmailService
{
    Task SendAlertDigestAsync(int userId, string email, List<UserAlert> alerts, CancellationToken ct);
    Task SendPasswordResetAsync(string email, string resetToken, CancellationToken ct);
    Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken ct);
}
```

### 3.5 SignalR — Notifiche Real-Time

Il polling via `GET /monitoring/alerts` richiede che il client faccia richieste periodiche. Per un'app web moderna, gli alert dovrebbero apparire in tempo reale senza polling.

```csharp
public class AlertHub : Hub
{
    public async Task JoinUserGroup(int userId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
}

// In AlertingService, dopo AddAsync:
await _hubContext.Clients.Group($"user-{alert.UserId}")
    .SendAsync("NewAlert", new { alert.Title, alert.Severity, alert.AlertType });
```

Richiede `builder.Services.AddSignalR()` e `MapHub<AlertHub>("/hubs/alerts")`. Frontend si connette via `@microsoft/signalr`.

### 3.6 MassTransit + RabbitMQ — Recommendation Asincrona

`GenerateRecommendationCommand` è sincrono e chiama fino a 4 LLM (3 path rationale + 1 executive summary). In produzione può durare 15–30 secondi per utente, bloccando il thread HTTP.

**Pattern consigliato**:

```
POST /recommendations/generate
  → pubblica GenerateRecommendationRequested su RabbitMQ
  → risponde 202 Accepted con { jobId }

Consumer (worker background):
  → elabora la raccomandazione async
  → notifica completamento via SignalR
  → client fa GET /recommendations/latest

GET /recommendations/status/{jobId}  → polling alternativo per client non-WS
```

`MassTransit` gestisce retry, dead-letter queue, e consumer scaling orizzontale.

### 3.7 Stripe — Subscription Management

Il tier Free/Premium esiste nel modello ma non genera ricavi e non ha enforcement.

**Flow minimo**:
- `POST /subscriptions/checkout` → crea Stripe Checkout Session, restituisce URL redirect
- `POST /webhooks/stripe` → riceve `customer.subscription.created`, `invoice.paid`, `customer.subscription.deleted` → aggiorna `User.SubscriptionTier` via MediatR command
- Cron job mensile di reconciliation per verificare stato subscription attiva vs DB

Libreria ufficiale: `Stripe.net`.

### 3.8 Semantic Skill Search con pgvector

Oggi `SearchByNameAsync` usa `ILIKE` — un utente che cerca "intelligenza artificiale" non trova "machine-learning". Con gli embedding già previsti su `Skill.EmbeddingVector`, si può abilitare la ricerca semantica.

```csharp
// EmbeddingJob (Hangfire, weekly):
var embedding = await _openAiClient.GetEmbeddingAsync(skill.Name);
skill.SetEmbedding(embedding);
await _skillRepository.UpsertAsync(skill);

// SearchSkillsQuery — ricerca vettoriale:
var queryVector = await _openAiClient.GetEmbeddingAsync(query);
var results = await _context.Skills
    .OrderBy(s => s.EmbeddingVector!.CosineDistance(queryVector))
    .Take(limit)
    .ToListAsync();
```

Richiede `Pgvector.EntityFrameworkCore >= 0.3` (supporto nativo `float[]`).

### 3.9 Health Checks Estesi

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connStr)                                     // già presente
    .AddRedis(redisConn)                                    // già presente
    .AddHangfire(o => o.MinimumAvailableServers = 1)        // verifica Hangfire server
    .AddUrlGroup(new Uri(escoBaseUrl), "esco-api",
        timeout: TimeSpan.FromSeconds(5))                   // ESCO raggiungibile
    .AddCheck<AnthropicHealthCheck>("anthropic-api");       // API AI
```

Con `AspNetCore.HealthChecks.UI` per dashboard visiva a `/health-ui`.

### 3.10 Secrets Management

`appsettings.json` ha placeholder "CHANGE_ME". In produzione i segreti non devono essere in file di configurazione deployati.

| Contesto | Soluzione |
|----------|-----------|
| Azure | `Azure.Extensions.AspNetCore.Configuration.Secrets` + Key Vault |
| AWS | `AWS.Extensions.NETCore.Configuration.SecretsManager` |
| Self-hosted | HashiCorp Vault con `VaultSharp` |
| Minimo accettabile | Variabili d'ambiente (mai file) |

### 3.11 API Versioning Formale

```csharp
builder.Services.AddApiVersioning(opts =>
{
    opts.DefaultApiVersion = new ApiVersion(1, 0);
    opts.AssumeDefaultVersionWhenUnspecified = true;
    opts.ReportApiVersions = true;
    opts.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
});
```

Libreria: `Asp.Versioning.Mvc`. Permette di introdurre v2 di un endpoint senza breaking change per i client v1 esistenti.

### 3.12 Test Coverage

Il progetto ha `Aegis.Application.Tests` e `Aegis.API.Integration.Tests` ma zero test scritti.

**Unit test prioritari** (xUnit + NSubstitute):
- `SkillPrioritizationService.Prioritize()` — verifica SkillROI, ordinamento, classificazione Foundation/Differentiating/Premium
- `ProfileEnrichmentService.ComputeSalaryPercentile()` — verifica interpolazione P25/Median/P75
- `AlertingService.EvaluateForUserAsync()` — verifica ogni threshold e la deduplicazione 7 giorni

**Integration test prioritari** (WebApplicationFactory + Testcontainers):
- Flow completo: Register → Profile → Assessment start/answer/complete → Generate recommendation
- Auth: login con credenziali errate → 401; 11° tentativo → 429
- Rate limiting: dopo 10 richieste su `/auth/login` → 429

Librerie: `xUnit`, `NSubstitute`, `Testcontainers.PostgreSql`, `Microsoft.AspNetCore.Mvc.Testing`.

---

## 4. RIEPILOGO PRIORITÀ IMPLEMENTATIVE

| Priorità | Implementazione | Impatto Business | Effort |
|----------|----------------|-----------------|--------|
| 🔴 1 | Pipeline KPI (Adzuna API + KpiIngestionJob) | Sblocca TUTTE le feature intelligence | Alto |
| 🔴 2 | Forgot/reset password + verifica email | Prerequisito go-live | Medio |
| 🔴 3 | GDPR endpoints (export + delete account) | Prerequisito legale EU | Medio |
| 🟡 4 | Redis caching (KPI, skills, dashboard) | Performance + riduzione costi LLM | Basso |
| 🟡 5 | Email transazionale (FluentEmail + SendGrid) | Alert delivery + UX | Medio |
| 🟡 6 | Stripe subscription + enforcement tier | Monetizzazione | Medio |
| 🟡 7 | Polly resilience su chiamate esterne | Affidabilità produzione | Basso |
| 🟡 8 | Test suite (unit + integration) | Qualità + CI/CD gate | Alto |
| 🟢 9 | OpenTelemetry distributed tracing | Observability | Basso |
| 🟢 10 | SignalR real-time alerts | UX differenziante | Medio |
| 🟢 11 | EmbeddingVector pipeline + semantic search | Qualità ricerca skill | Medio |
| 🟢 12 | MassTransit async recommendation | Scalabilità orizzontale | Alto |
| 🟢 13 | API versioning formale | Manutenibilità long-term | Basso |
| 🟢 14 | Secrets management (Key Vault / Vault) | Security hardening | Basso |
| 🟢 15 | Admin API + Health checks estesi | Operabilità | Medio |

---

## 5. CONCLUSIONI

La piattaforma ha un'architettura solida e il flow utente è percorribile end-to-end. I meccanismi di scoring sono deterministici, la security di base è implementata, i job schedulati sono registrati.

**Il blocco principale alla produzione reale** sono due:
1. **Pipeline KPI** — senza dati reali, il motore intelligence opera su valori di default `5.0` per ogni ruolo. Salary percentile è null. Gli alert non scattano mai. Le raccomandazioni sono cecche sul mercato reale.
2. **Account management GDPR-compliant** — password reset, verifica email, export/delete dati sono prerequisiti legali e operativi irrinunciabili per qualsiasi SaaS europeo.

Tutto il resto — Redis, Polly, OpenTelemetry, SignalR, MassTransit — è qualità, scalabilità e differenziazione competitiva che può essere stratificata progressivamente dopo il go-live iniziale.
