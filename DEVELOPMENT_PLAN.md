# AEGIS — AI Career Market Intelligence Platform
## Piano di Sviluppo — Stack .NET

---

## 1. Analisi del Progetto

### Sintesi Strategica

AEGIS è un **sistema di Decision Intelligence per la carriera professionale** nel settore tech.  
Non è un chatbot né un coach generico: è una piattaforma analitica che combina dati di mercato reali, AI strutturata e KPI deterministici per supportare professionisti in decisioni ad alto impatto sulla carriera.

### Problemi Risolti

| Problema Utente | Soluzione AEGIS |
|---|---|
| Non conosce il proprio valore reale di mercato | Career Assessment con seniority scoring validato |
| Segue hype tecnologici senza dati | Market Intelligence Engine con KPI oggettivi |
| Non sa stimare ROI di una transizione | Transition Feasibility con salary uplift forecast |
| Paura di obsolescenza AI | AI Risk Score per ruolo e skill stack |
| Stagnazione salariale senza insight | Salary Strength con geo/remote premium |
| Non distingue skill richieste da premium | Demand vs. Premium skill segmentation |

### Target Utenti

| Persona | Pain Point Principale | Feature Critica |
|---|---|---|
| Mid/Senior Developer | Stagnazione salariale, confusione sui trend | Skill Radar + Salary Trajectory |
| Career Switcher | ROI incerto, perdita seniority | Transition Feasibility + Gap Analysis |
| Tech Lead / Architect | Pianificazione strategica, specializzazione | Market Intelligence + Career Simulation |

---

## 2. Principi Architetturali

### RULE-AI-01 — Separazione dati/AI
Gli LLM **non generano KPI**. I KPI sono calcolati deterministicamente da dati reali tramite servizi .NET dedicati.

### RULE-AI-02 — AI come layer di spiegazione
Claude viene usato esclusivamente per: spiegare, sintetizzare, correlare, generare insight narrativi su KPI già calcolati.

### RULE-AI-03 — Explainability by default
Ogni raccomandazione espone: KPI di supporto, confidence score, fonti dati, reasoning tracciabile.

### RULE-AI-04 — Clean Architecture
Separazione netta tra Domain, Application, Infrastructure e Presentation.  
Il dominio non dipende da framework o infrastrutture esterne.

---

## 3. Stack Tecnologico .NET

### Backend

| Layer | Tecnologia | Motivazione |
|---|---|---|
| Framework API | **ASP.NET Core 8 Web API** | Performance, minimal API, OpenAPI nativo |
| Architecture Pattern | **Clean Architecture + CQRS** | Separazione concern, testabilità, scalabilità |
| ORM | **Entity Framework Core 8** | Code-first, migration, LINQ query |
| Validation | **FluentValidation** | Validazione dichiarativa, regole composabili |
| Mediator | **MediatR** | CQRS handler, pipeline behaviors |
| Background Jobs | **Hangfire** | Crawling pipeline, KPI recompute, alert scheduling |
| Caching | **IMemoryCache + StackExchange.Redis** | Cache distribuita per KPI e session |
| Auth | **ASP.NET Core Identity + JWT Bearer** | Auth nativa, refresh token, roles |
| Mapping | **AutoMapper** | DTO ↔ Domain mapping |
| Logging | **Serilog** | Structured logging, sink multipli |
| Testing | **xUnit + Moq + FluentAssertions** | Unit + integration test |

### Database

| Store | Tecnologia | Uso |
|---|---|---|
| Relational | **PostgreSQL** via EF Core | Utenti, KPI, assessment, report, recommendation |
| Vector Search | **pgvector** (PostgreSQL extension) | Embedding skill, semantic similarity |
| Analytics / Time-series | **ClickHouse** via HTTP client | Trend, aggregazioni mercato, time-series KPI |
| Cache | **Redis** | Session, KPI cache, Hangfire storage |

> **Scelta pgvector**: Elimina la necessità di un vector DB separato (Qdrant/Pinecone).  
> Con PostgreSQL + pgvector si gestiscono sia dati relazionali che embedding in un'unica infrastruttura,  
> riducendo complessità operativa. Sufficiente per la fase iniziale e scalabile fino a milioni di vettori.

### AI Layer

| Componente | Tecnologia | Uso |
|---|---|---|
| Frontier LLM | **Claude claude-sonnet-4-6** via Anthropic SDK | Reasoning, spiegazioni, domande assessment, insight |
| Embeddings | **OpenAI text-embedding-3-small** | Skill similarity, clustering semantico |
| SDK .NET | **Anthropic.SDK (NuGet)** | Client ufficiale per Claude |
| Bluff Detection | **Rule-based C# + ML.NET** | Incoerenze risposte, pattern detection |
| NLP / Skill Extraction | **ML.NET + custom NER** | Parsing skill da job listing |

### Frontend

| Layer | Tecnologia | Motivazione |
|---|---|---|
| Framework | **Blazor WebAssembly** (o Blazor Server) | Full .NET stack, no JS framework aggiuntivo |
| UI Components | **MudBlazor** | Component library professionale per Blazor |
| Charts | **ApexCharts for Blazor** | Visualizzazioni analitiche, time-series, radar |
| State | **Fluxor** (Redux pattern per Blazor) | State management prevedibile |
| Auth | **Microsoft.AspNetCore.Components.WebAssembly.Authentication** | Integrato con Identity Server |

> **Alternativa**: Blazor Server per latenza inferiore in produzione, con SignalR per real-time alerts.

### Infrastructure

| Componente | Tecnologia |
|---|---|
| Container | **Docker + Docker Compose** |
| Orchestration | **Kubernetes** (produzione) |
| CI/CD | **GitHub Actions** con dotnet build/test/publish |
| Monitoring | **Prometheus + Grafana** (metrics via prometheus-net) |
| Health Checks | **ASP.NET Core HealthChecks** |
| Error Tracking | **Sentry SDK for .NET** |
| API Docs | **Swagger / Scalar** (OpenAPI 3.0) |

---

## 4. Architettura della Solution .NET

```
Aegis.sln
│
├── src/
│   ├── Aegis.Domain/                  # Entities, Value Objects, Domain Events
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── UserProfile.cs
│   │   │   ├── Skill.cs
│   │   │   ├── Assessment.cs
│   │   │   ├── MarketKpi.cs
│   │   │   ├── Recommendation.cs
│   │   │   └── JobListing.cs
│   │   ├── ValueObjects/
│   │   │   ├── SeniorityLevel.cs
│   │   │   ├── SalaryRange.cs
│   │   │   └── SkillVector.cs
│   │   ├── Enums/
│   │   ├── Events/
│   │   └── Interfaces/
│   │       ├── IAssessmentRepository.cs
│   │       ├── IMarketKpiRepository.cs
│   │       └── IRecommendationRepository.cs
│   │
│   ├── Aegis.Application/             # Use Cases (CQRS), DTOs, Validators
│   │   ├── Assessment/
│   │   │   ├── Commands/
│   │   │   │   ├── StartAssessmentCommand.cs
│   │   │   │   └── SubmitAnswerCommand.cs
│   │   │   └── Queries/
│   │   │       └── GetAssessmentResultQuery.cs
│   │   ├── Market/
│   │   │   ├── Queries/
│   │   │   │   ├── GetSkillKpisQuery.cs
│   │   │   │   └── GetMarketTrendsQuery.cs
│   │   │   └── Services/
│   │   │       └── KpiComputationService.cs
│   │   ├── Recommendations/
│   │   │   ├── Commands/
│   │   │   │   └── GenerateRecommendationCommand.cs
│   │   │   └── Services/
│   │   │       ├── RecommendationEngine.cs
│   │   │       └── TransitionFeasibilityService.cs
│   │   ├── Profile/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── Monitoring/
│   │   │   └── Services/
│   │   │       └── AlertingService.cs
│   │   ├── Common/
│   │   │   ├── Behaviors/           # MediatR pipeline behaviors
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   └── CachingBehavior.cs
│   │   │   └── DTOs/
│   │   └── Interfaces/
│   │       ├── IAIOrchestrator.cs
│   │       └── IEmbeddingService.cs
│   │
│   ├── Aegis.Infrastructure/          # EF Core, Repos, External Services
│   │   ├── Persistence/
│   │   │   ├── AegisDbContext.cs
│   │   │   ├── Configurations/       # EF Fluent API configs
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   ├── AI/
│   │   │   ├── AnthropicOrchestrator.cs
│   │   │   └── OpenAIEmbeddingService.cs
│   │   ├── Crawlers/
│   │   │   ├── LinkedInCrawler.cs
│   │   │   ├── IndeedCrawler.cs
│   │   │   └── SalarySourceCrawler.cs
│   │   ├── Analytics/
│   │   │   └── ClickHouseAnalyticsService.cs
│   │   ├── Cache/
│   │   │   └── RedisCacheService.cs
│   │   └── BackgroundJobs/
│   │       ├── CrawlerJob.cs
│   │       ├── KpiRecomputeJob.cs
│   │       └── AlertingJob.cs
│   │
│   ├── Aegis.API/                     # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── ProfileController.cs
│   │   │   ├── AssessmentController.cs
│   │   │   ├── MarketController.cs
│   │   │   ├── RecommendationsController.cs
│   │   │   └── MonitoringController.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionMiddleware.cs
│   │   │   └── RateLimitingMiddleware.cs
│   │   └── Program.cs
│   │
│   └── Aegis.Web/                     # Blazor WebAssembly (opzionale)
│       ├── Pages/
│       │   ├── Dashboard/
│       │   ├── Assessment/
│       │   ├── Market/
│       │   └── Simulation/
│       └── Components/
│
└── tests/
    ├── Aegis.Domain.Tests/
    ├── Aegis.Application.Tests/
    └── Aegis.API.Integration.Tests/
```

---

## 5. Architettura del Sistema

```
┌──────────────────────────────────────────────────────────────┐
│              FRONTEND (Blazor WASM / SPA esterna)             │
│   Dashboard │ Assessment │ Simulation │ Monitoring │ Reports   │
└──────────────────────────┬───────────────────────────────────┘
                           │ HTTPS / REST + SignalR (alerts)
┌──────────────────────────▼───────────────────────────────────┐
│              ASP.NET Core 8 Web API (Aegis.API)               │
│   JWT Auth │ Rate Limiting │ OpenAPI │ Health Checks           │
└──┬───────────┬──────────┬──────────┬──────────┬──────────────┘
   │           │          │          │          │
   ▼           ▼          ▼          ▼          ▼
┌──────┐  ┌───────┐  ┌────────┐ ┌───────┐ ┌──────────┐
│Profile│  │Tech   │  │Market  │ │Rec    │ │Monitor   │
│Module │  │Valid. │  │Intel.  │ │Engine │ │& Alerts  │
│       │  │Engine │  │Engine  │ │       │ │(Hangfire)│
└──┬────┘  └───┬───┘  └───┬────┘ └───┬───┘ └────┬─────┘
   │           │          │          │           │
   └───────────┴──────────┴──────────┴───────────┘
                          │
              ┌───────────▼──────────────┐
              │   Aegis.Application      │
              │   (CQRS via MediatR)     │
              │   KPI Computation Service│
              └───────────┬──────────────┘
                          │
              ┌───────────▼──────────────┐
              │   Aegis.Infrastructure   │
              │   EF Core │ AI │ Cache   │
              └───────────┬──────────────┘
                          │
        ┌─────────────────┼───────────────────┐
        ▼                 ▼                   ▼
   ┌──────────┐    ┌────────────┐    ┌──────────────┐
   │PostgreSQL │    │  Redis     │    │  ClickHouse  │
   │+ pgvector │    │  (Cache +  │    │  (Analytics, │
   │(Users,KPI,│    │  Hangfire) │    │  Time-series)│
   │ Vectors)  │    └────────────┘    └──────────────┘
   └──────────┘

              ┌──────────────────────────────┐
              │  DATA PIPELINE (Hangfire)    │
              │  Crawl → Clean → Normalize → │
              │  Embed → Aggregate → KPI     │
              └──────────────────────────────┘
```

---

## 6. Moduli Funzionali

### M01 — User Profiling Module

**Responsabilità:** Raccogliere e strutturare il profilo professionale.

**CQRS Commands/Queries:**
- `CreateProfileCommand` — crea profilo iniziale
- `UpdateProfileCommand` — aggiorna campi profilo
- `AddSkillCommand` — aggiunge skill con self-rating
- `GetProfileQuery` — recupera profilo completo con score completezza
- `GetProfileCompletenessQuery` — percentuale completamento onboarding

**Domain Entity:**
```csharp
public class UserProfile : Entity
{
    public string CurrentRole { get; private set; }
    public int YearsExperience { get; private set; }
    public string LocationCountry { get; private set; }
    public EnglishLevel EnglishLevel { get; private set; }
    public RemotePreference RemotePreference { get; private set; }
    public SalaryRange SalaryExpectation { get; private set; }
    public IReadOnlyList<UserSkill> Skills { get; private set; }
    public double ProfileCompleteness { get; private set; }

    public void AddSkill(Skill skill, int selfRatedLevel, double yearsExp) { ... }
    public double CalculateCompleteness() { ... }
}
```

---

### M02 — Technical Validation Engine

**Responsabilità:** Assessment adattivo a 3 layer per validare competenze dichiarate.

**Layer:**
1. **Screening** — domande rapide binary/multiple choice
2. **Adaptive Deep Dive** — profondità crescente, percorso dinamico
3. **Tradeoff Analysis** — gestione failure, architettura, incident handling

**CQRS Commands/Queries:**
- `StartAssessmentCommand` → genera domanda layer 1 via Claude
- `SubmitAnswerCommand` → valuta risposta, decide prossimo layer/domanda
- `CompleteAssessmentCommand` → consolida risultato, calcola seniority score
- `GetAssessmentResultQuery` → risultato con validated skills e bluff signals

**Seniority Scoring (deterministico):**
```csharp
public class SeniorityScorer
{
    public SeniorityResult Score(IEnumerable<QuestionEvaluation> evaluations)
    {
        // Pesi per layer: Screening 20%, DeepDive 50%, Tradeoff 30%
        var weightedScore = evaluations
            .GroupBy(e => e.Layer)
            .Sum(g => g.Average(e => e.Score) * LayerWeights[g.Key]);

        var seniority = weightedScore switch
        {
            >= 0.85 => SeniorityLevel.Principal,
            >= 0.70 => SeniorityLevel.Senior,
            >= 0.50 => SeniorityLevel.Mid,
            _       => SeniorityLevel.Junior
        };

        return new SeniorityResult(seniority, weightedScore, ComputeConfidence(evaluations));
    }
}
```

---

### M03 — Market Intelligence Engine

**Responsabilità:** Pipeline dati di mercato + calcolo deterministico dei KPI.

**KPI Core (tutti deterministici, mai generati da LLM):**

| KPI | Calcolo | Fonte |
|---|---|---|
| `MarketDemandScore` | `(job_count_30d / baseline) * growth_multiplier` | Job boards |
| `SalaryStrength` | `median_salary * geo_premium_factor * remote_multiplier` | Levels.fyi, Glassdoor |
| `SaturationIndex` | `estimated_applicants / job_count * supply_growth` | Job boards + LinkedIn |
| `AIRiskScore` | `automation_probability * commoditization_weight` | Research data + trends |
| `GrowthMomentum` | `yoy_demand_growth * skill_mention_acceleration_90d` | All sources |
| `CareerStability` | `(1 - layoff_sensitivity) * market_resilience_index` | Economic + layoff.fyi |

**Hangfire Background Jobs:**
```csharp
// Esecuzione schedulata
RecurringJob.AddOrUpdate<CrawlerJob>("crawl-jobs", j => j.ExecuteAsync(), "0 */6 * * *");
RecurringJob.AddOrUpdate<KpiRecomputeJob>("recompute-kpis", j => j.ExecuteAsync(), "0 2 * * *");
RecurringJob.AddOrUpdate<AlertingJob>("check-alerts", j => j.ExecuteAsync(), "0 */1 * * *");
```

---

### M04 — Recommendation Engine

**Responsabilità:** Generare percorsi professionali con ROI stimato, basati su KPI reali.

**Flow:**
1. Recupera KPI di mercato per ruoli candidati (deterministico)
2. Calcola skill overlap tra profilo utente e ruolo target (deterministico)
3. Stima salary uplift, transition difficulty, tempo necessario (deterministico)
4. Passa KPI pre-calcolati a Claude per generare rationale narrativo

**Output DTO:**
```csharp
public record RecommendationPathDto
{
    public string TargetRole { get; init; }
    public int Rank { get; init; }
    public double MarketDemandScore { get; init; }
    public double SalaryUpliftPercent { get; init; }
    public TransitionDifficulty Difficulty { get; init; }
    public int EstimatedMonths { get; init; }
    public double SkillOverlapPercent { get; init; }
    public List<SkillGapDto> SkillGaps { get; init; }
    public double AIRiskScore { get; init; }
    public double GrowthMomentum { get; init; }
    public double Confidence { get; init; }
    public string LlmRationale { get; init; }      // generato da Claude su KPI già calcolati
    public List<KpiSourceDto> SupportingKpis { get; init; }
}
```

---

### M05 — AI Orchestrator (Aegis.Infrastructure/AI)

**Responsabilità:** Unico punto di accesso al LLM. Applica le regole architetturali AI.

```csharp
public class AnthropicOrchestrator : IAIOrchestrator
{
    // Genera domanda assessment — non genera dati, solo testo della domanda
    Task<string> GenerateAssessmentQuestionAsync(AssessmentContext ctx);

    // Valuta risposta — restituisce score + segnali, mai inventa KPI
    Task<AnswerEvaluation> EvaluateAnswerAsync(string question, string answer, string skill);

    // Genera insight narrativo — riceve KPI pre-calcolati, li sintetizza
    Task<string> GenerateCareerInsightAsync(CareerInsightRequest request);

    // Spiega raccomandazione — basata solo su KPI passati come input
    Task<string> ExplainRecommendationAsync(RecommendationContext ctx);
}
```

**Regola di sicurezza:** ogni metodo riceve KPI già computati come parametro.  
Il LLM non ha accesso diretto al DB né può generare numeri autonomamente.

---

### M06 — Monitoring & Alerts

**Responsabilità:** Tracking continuo competitività utente, alert su cambiamenti di mercato.

**Alert Types:**
- `MarketDemandAlert` — calo domanda > 20% per skill principale utente
- `SalaryAlert` — variazione salary mediana > 15% per ruolo utente
- `EmergingSkillAlert` — skill con growth momentum > 8.0 correlata al profilo
- `AIRiskAlert` — AI risk score supera soglia per ruolo utente
- `CompetitivenessAlert` — score personale scende sotto percentile 40

---

## 7. Schema Database (PostgreSQL + EF Core)

```sql
-- Identity
users (Id, Email, PasswordHash, SubscriptionTier, GdprConsentAt, IsActive, CreatedAt)

-- Profilo
user_profiles (Id, UserId, CurrentRole, YearsExperience, LocationCountry, LocationCity,
               EnglishLevel, RemotePreference, SalaryMin, SalaryMax, SalaryCurrency,
               CareerGoals, ProfileCompleteness, UpdatedAt)

user_skills (Id, UserId, SkillId, SelfRatedLevel, YearsExperience, IsPrimary, LastUsed)

-- Skill Taxonomy
skill_categories (Id, Name, Description)
skills (Id, Name, CanonicalName, CategoryId, ParentId, Aliases, EmbeddingVector vector(1536))

-- Assessment
assessments (Id, UserId, StartedAt, CompletedAt, EstimatedSeniority, Confidence, CurrentLayer)
assessment_questions (Id, AssessmentId, QuestionText, AnswerText, SkillId, DifficultyLevel,
                      Layer, EvaluatedScore, AskedAt, AnsweredAt)
assessment_results (Id, AssessmentId, ValidatedSkills jsonb, WeakSignals jsonb,
                    SuspectedInflations jsonb, SeniorityScore, OverallConfidence, LlmSummary)

-- Market Intelligence
market_kpis (Id, SkillId, RoleCanonical, GeoCountry, GeoCity, ComputedAt,
             DemandScore, JobCount30d, JobCountYoyGrowth,
             SalaryMedian, SalaryP25, SalaryP75, SalaryCurrency, RemotePremiumPct,
             SaturationIndex, EstimatedApplicantsPerJob,
             AIRiskScore, AutomationProbability,
             GrowthMomentum, SkillMentionTrend,
             CareerStability, LayoffSensitivity,
             DataConfidence, DataSources jsonb)

job_listings (Id, Source, SourceId, Title, Company, LocationRaw, LocationCountry,
              IsRemote, SalaryMin, SalaryMax, SalaryCurrency, SkillsRequired jsonb,
              SeniorityLevel, DescriptionHash, IsFakeDetected, PostedAt, CrawledAt)

salary_data_points (Id, Source, RoleCanonical, GeoCountry, GeoCity,
                    SalaryAnnual, Currency, IsRemote, SeniorityLevel, ReportedAt, CrawledAt)

-- Recommendation
recommendations (Id, UserId, AssessmentId, GeneratedAt, MarketFitScore, FutureRiskScore,
                 CompetitiveScore, SalaryPercentile, LlmExecutiveSummary, GenerationConfidence)

recommendation_paths (Id, RecommendationId, TargetRole, TargetRoleCanonical, Rank,
                      SalaryUpliftPct, TransitionDifficulty, EstimatedMonths,
                      SkillOverlapPct, SkillGaps jsonb, MarketDemandScore,
                      AIRiskScore, GrowthMomentum, Confidence,
                      LlmRationale, SupportingKpis jsonb)

-- Monitoring
user_alerts (Id, UserId, AlertType, Title, Payload jsonb, Severity, CreatedAt, ReadAt)
user_monitoring_snapshots (Id, UserId, SnapshotAt, MarketFitScore, SalaryPercentile, CompetitiveScore)
```

---

## 8. API Endpoints (ASP.NET Core)

```
POST   /api/v1/auth/register
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout

GET    /api/v1/profile
PUT    /api/v1/profile
POST   /api/v1/profile/skills
DELETE /api/v1/profile/skills/{skillId}

POST   /api/v1/assessment/start
POST   /api/v1/assessment/{id}/answer
GET    /api/v1/assessment/{id}/result
GET    /api/v1/assessment/history

GET    /api/v1/market/skills/{skillId}/kpis
GET    /api/v1/market/roles/{roleCanonical}/kpis
GET    /api/v1/market/trends?geo=IT&period=12m&skill={skillId}
GET    /api/v1/market/salary?role={role}&geo=IT&seniority=senior
GET    /api/v1/market/emerging-skills

POST   /api/v1/recommendations/generate
GET    /api/v1/recommendations/latest
GET    /api/v1/recommendations/{id}
GET    /api/v1/recommendations/{id}/paths/{pathId}/explain

GET    /api/v1/simulation?targetRole={role}
GET    /api/v1/monitoring/dashboard
GET    /api/v1/monitoring/alerts
PUT    /api/v1/monitoring/alerts/{id}/read
GET    /api/v1/monitoring/competitiveness-history
```

---

## 9. Monetizzazione

### Free Tier
- 1 assessment / mese
- Score competitività base
- 3 insight di mercato
- KPI limitati

### Premium (€29/mese)
- Assessment illimitati
- Report avanzati con tutti i KPI
- Career simulation completa
- Monitoring continuo + alert
- Trend geo-filtrati
- Roadmap personalizzate

### Enterprise (pricing custom)
- API access per HR / Recruiting team
- Workforce intelligence dashboard
- Talent benchmarking anonimizzato
- Employability forecasting

---

## 10. Compliance & Security

### GDPR
- Consenso esplicito per categoria di dato
- Right to deletion tramite `DeleteUserDataCommand`
- Data portability (export JSON profile)
- Salary data anonimizzata in analytics

### Security .NET Best Practices
- JWT con refresh token rotation
- Rate limiting via `AspNetCoreRateLimit`
- Input validation con FluentValidation (no raw SQL)
- EF Core parametric queries (SQL injection prevention)
- HTTPS only + HSTS
- Secrets via `dotnet user-secrets` / Azure Key Vault
- OWASP Top 10 review obbligatorio prima del go-live

### AI Safety
- LLM non ha accesso diretto al DB
- Ogni output LLM validato prima di essere persistito
- Nessun KPI numerico generato da LLM

---

## 11. KPI di Successo del Prodotto

| KPI | Target 6 mesi | Target 12 mesi |
|---|---|---|
| Monthly Active Users | 1.000 | 5.000 |
| Assessment Completion Rate | > 70% | > 75% |
| Recommendation Acceptance Rate | > 40% | > 50% |
| Premium Conversion Rate | > 8% | > 12% |
| Retention 30gg | > 45% | > 55% |
| NPS | > 40 | > 55 |
| Trust Score (survey) | > 4.0/5.0 | > 4.3/5.0 |

---

## 12. Criticità e Mitigazioni

| Criticità | Rischio | Mitigazione |
|---|---|---|
| Data Quality | KPI non affidabili | Cleaning pipeline + confidence scoring + fallback a seed data |
| Trust utente | Output non credibili | Explainability obbligatoria, fonti visibili, zero dati inventati |
| Market Volatility | KPI obsoleti | Hangfire jobs frequenti, timestamp KPI visibili in UI |
| LLM Hallucination | Numeri inventati | LLM riceve solo KPI già calcolati, output testato con fixture |
| Scraping Legal | ToS violation | API ufficiali dove disponibili, rate limiting rispettoso, no PII |
| Scalabilità | Bottleneck crawlers | Worker Hangfire scalabili, ClickHouse per query analitiche pesanti |

---

*Documento generato il 2026-05-13 — AEGIS Platform v0.1 — Stack .NET*
