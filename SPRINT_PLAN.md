# AEGIS — Piano Sprint Dettagliato
## Stack: .NET 8 | Clean Architecture | CQRS

---

## Overview Roadmap

| Fase | Sprint | Durata | Output Principale |
|---|---|---|---|
| **FASE 1** — Foundations | S01–S03 | 6 settimane | Auth, profilo, DB, infrastruttura |
| **FASE 2** — Assessment Engine | S04–S06 | 6 settimane | Assessment adattivo funzionante |
| **FASE 3** — Market Intelligence | S07–S10 | 8 settimane | Pipeline dati + KPI deterministici |
| **FASE 4** — Recommendation Layer | S11–S13 | 6 settimane | Raccomandazioni con ROI |
| **FASE 5** — Explainability & Trust | S14–S15 | 4 settimane | Source attribution + dashboards |
| **FASE 6** — Monitoring Platform | S16–S17 | 4 settimane | Alert continui + tracking |
| **FASE 7** — Advanced AI | S18–S20 | 6 settimane | Career simulation, roadmap personalizzata |

**Durata totale stimata: ~40 settimane (10 mesi)**  
Sprint da 2 settimane ciascuno.

---

## FASE 1 — Foundations (Sprint 1–3)

### Sprint 1 — Infrastructure & Auth

**Goal:** Soluzione .NET funzionante con auth JWT e DB configurato.

**Tasks:**
- [ ] Setup solution `Aegis.sln` con 4 progetti (Domain, Application, Infrastructure, API)
- [ ] Configurazione PostgreSQL + pgvector via Docker Compose
- [ ] EF Core `AegisDbContext` con migrations iniziali
- [ ] Tabelle: `users`, `user_profiles`, `skill_categories`, `skills`
- [ ] `POST /api/v1/auth/register` — registrazione con hashing bcrypt
- [ ] `POST /api/v1/auth/login` — JWT access + refresh token
- [ ] `POST /api/v1/auth/refresh` — rotation refresh token
- [ ] Middleware `ExceptionMiddleware` — error handling uniforme
- [ ] Rate limiting con `AspNetCoreRateLimit`
- [ ] Health checks: PostgreSQL + Redis
- [ ] Serilog structured logging configurato
- [ ] GitHub Actions CI: build + unit test
- [ ] Swagger / OpenAPI endpoint attivo

**Definition of Done:**
- Register + Login funzionanti via Swagger
- Token JWT valido ritornato
- CI verde

---

### Sprint 2 — User Profiling Module

**Goal:** Onboarding multi-step completo con skill taxonomy.

**Tasks:**
- [ ] `UserProfile` entity con `CalculateCompleteness()`
- [ ] `UserSkill` entity con validazione livello 1–5
- [ ] `Skill` + `SkillCategory` entities + seed data (50+ skill canoniche)
- [ ] `CreateProfileCommand` + `UpdateProfileCommand` (MediatR)
- [ ] `AddSkillCommand` + `RemoveSkillCommand`
- [ ] `GetProfileQuery` con DTO completo
- [ ] `GET/PUT /api/v1/profile` — CRUD profilo
- [ ] `POST/DELETE /api/v1/profile/skills` — gestione skill
- [ ] `ValidationBehavior` MediatR pipeline per FluentValidation
- [ ] `LoggingBehavior` MediatR pipeline
- [ ] AutoMapper profile per DTO ↔ Entity
- [ ] Unit test: `UserProfile.CalculateCompleteness()`
- [ ] Unit test: `UserSkill.Create()` con livello non valido

**Definition of Done:**
- Onboarding flow completo via API
- Completeness calcolata correttamente (test passa)
- Validazione input funzionante (400 su dati errati)

---

### Sprint 3 — Skill Taxonomy & Normalization

**Goal:** Engine di normalizzazione skill con embedding per similarity search.

**Tasks:**
- [ ] Seed data: 150+ skill con `canonical_name`, categoria, alias
- [ ] `SkillNormalizationService` — mappa varianti a canonical (es. "k8s" → "Kubernetes")
- [ ] `OpenAIEmbeddingService` — genera embeddings via `text-embedding-3-small`
- [ ] Hangfire: job `EmbedSkillsJob` — batch embedding per skill senza vettore
- [ ] pgvector: `vector(1536)` su `skills.EmbeddingVector`
- [ ] `SkillSimilarityService` — cosine similarity tra skill vettori
- [ ] `GET /api/v1/skills/search?q=...` — ricerca semantica
- [ ] `GET /api/v1/skills/similar/{id}` — skill simili
- [ ] Redis cache per query skill frequenti
- [ ] Integration test: normalization "node.js" → "Node.js canonical"

**Definition of Done:**
- Ricerca semantica funzionante con pgvector
- Embedding generato per tutte le skill di seed
- Similarity restituisce risultati plausibili

---

## FASE 2 — Assessment Engine (Sprint 4–6)

### Sprint 4 — Assessment Core + AI Integration

**Goal:** Assessment adattivo Layer 1 (Screening) funzionante.

**Tasks:**
- [ ] `Assessment`, `AssessmentQuestion`, `AssessmentResult` entities
- [ ] EF migration per tabelle assessment
- [ ] `StartAssessmentCommand` — crea assessment, genera prima domanda via Claude
- [ ] `AnthropicOrchestrator.GenerateAssessmentQuestionAsync()` — integrazione Anthropic SDK
- [ ] `SubmitAnswerCommand` — salva risposta, chiama `EvaluateAnswerAsync()` su Claude
- [ ] `AssessmentRepository` con EF Core
- [ ] `POST /api/v1/assessment/start`
- [ ] `POST /api/v1/assessment/{id}/answer`
- [ ] Conversation memory: ultimi 3 QA passati come context a Claude
- [ ] Unit test: `SeniorityScorer.Score()` con fixture risposte
- [ ] Unit test: `AnthropicOrchestrator` mockato — verifica non genera KPI

**Definition of Done:**
- Assessment Layer 1 (5 domande screening) completabile via API
- Risposte valutate, score persistito
- Mock di Claude funzionante in test

---

### Sprint 5 — Adaptive Interview Logic

**Goal:** Logica adattiva Layer 2 + Layer 3. Decision tree dinamico.

**Tasks:**
- [ ] `AdaptiveInterviewService` — decide prossimo layer basandosi su score corrente
- [ ] Layer 2 (Deep Dive): attivato se screening score > 0.4
- [ ] Layer 3 (Tradeoff): attivato se deep dive score > 0.6
- [ ] Difficoltà domanda aumenta progressivamente
- [ ] `BluffDetectionService` — coerenza cross-domanda (rule-based)
  - Contraddizioni temporali (es. "3 anni di K8s" ma risposta base)
  - Mancanza di dettagli concreti su claim di seniority alta
- [ ] `CompleteAssessmentCommand` — consolida risultato, genera `AssessmentResult`
- [ ] `SeniorityScorer` integrato: pesi Layer 1/2/3 = 20%/50%/30%
- [ ] LLM Summary: `GenerateCareerInsightAsync` su dati assessment già calcolati
- [ ] `GET /api/v1/assessment/{id}/result`
- [ ] Integration test: assessment completo end-to-end con mock LLM

**Definition of Done:**
- Assessment completo a 3 layer funzionante
- Seniority score calcolato deterministicamente
- Bluff detection attiva su almeno 2 pattern

---

### Sprint 6 — Assessment Refinement + History

**Goal:** Assessment storico, retry logic, edge cases.

**Tasks:**
- [ ] `GET /api/v1/assessment/history` — lista assessment con risultati
- [ ] Limite: 1 assessment/mese per utente Free tier
- [ ] `AssessmentSessionService` — gestione timeout (60 min max per assessment)
- [ ] Validation: max 20 domande per assessment
- [ ] `CachingBehavior` MediatR per query pesanti
- [ ] Test: seniority scoring con tutte le combinazioni layer
- [ ] Test: bluff detection su fixture risposte incoerenti

**Definition of Done:**
- Assessment robusto su edge cases
- Rate limit free tier funzionante
- Coverage test > 80% su Application layer

---

## FASE 3 — Market Intelligence Engine (Sprint 7–10)

### Sprint 7 — Data Pipeline Infrastructure

**Goal:** Pipeline Hangfire per crawling + storing job listings.

**Tasks:**
- [ ] `JobListing` entity + EF migration
- [ ] `SalaryDataPoint` entity + EF migration
- [ ] `CrawlerJob` Hangfire — esecuzione ogni 6 ore
- [ ] `IndeedCrawler` — parser annunci (rispetta ToS + rate limits)
- [ ] `WellfoundCrawler` — startup jobs
- [ ] `JobCleaningService` — deduplication via `DescriptionHash`
- [ ] `FakeJobDetector` — rule-based (pattern titoli, salary assenti, etc.)
- [ ] ClickHouse: tabella `job_listings_archive` per time-series
- [ ] `ClickHouseAnalyticsService` — bulk insert batch
- [ ] Monitoring: Prometheus counter per job crawlati/puliti/scartati

**Definition of Done:**
- Pipeline attiva, crawla 100+ listing in test
- Deduplication funzionante
- Fake job detection con precision > 70% su dataset test

---

### Sprint 8 — Skill Extraction & Normalization Pipeline

**Goal:** Estrazione skill automatica da job listing.

**Tasks:**
- [ ] `SkillExtractionService` — NER rule-based + dizionario skill canoniche
- [ ] Mappa skill estratte a `canonical_name` via `SkillNormalizationService`
- [ ] `SkillFrequencyJob` — aggrega frequenze skill per ruolo/geo ogni 24h
- [ ] Tabella `skill_market_frequency` in ClickHouse
- [ ] `SalarySourcEcrawler` — aggregazione dati da source pubbliche disponibili
- [ ] `SalaryNormalizationService` — normalizzazione PPP per geo
- [ ] Test: estrazione skill corretta su 20 listing fixture

**Definition of Done:**
- Skill estratte correttamente per > 85% listing
- Frequenze aggregate in ClickHouse
- Salary normalizzata per geo

---

### Sprint 9 — KPI Computation Engine

**Goal:** Calcolo deterministico di tutti e 6 i KPI core.

**Tasks:**
- [ ] `KpiComputationService` — implementazione completa 6 KPI
  - KPI-01: `MarketDemandScore` da job_count + hiring_velocity
  - KPI-02: `SalaryStrength` da percentili + geo premium
  - KPI-03: `SaturationIndex` da applicants_per_job
  - KPI-04: `AIRiskScore` da automation_probability + commoditization
  - KPI-05: `GrowthMomentum` da yoy_growth + skill_mention_trend
  - KPI-06: `CareerStability` da layoff_sensitivity inverse
- [ ] `KpiRecomputeJob` Hangfire — ogni notte alle 2:00
- [ ] `MarketKpi` entity upsert con `DataConfidence` score
- [ ] `GET /api/v1/market/skills/{skillId}/kpis`
- [ ] `GET /api/v1/market/roles/{role}/kpis`
- [ ] `GET /api/v1/market/salary?role=...&geo=...`
- [ ] Unit test: ogni KPI con fixture data → valore atteso
- [ ] Unit test: `DataConfidence` decresce con dati vecchi

**Definition of Done:**
- Tutti e 6 i KPI calcolati correttamente (test verdi)
- KPI persistiti e aggiornati ogni notte
- API market funzionanti

---

### Sprint 10 — Trend Analysis & AI Risk

**Goal:** Trend time-series, emerging skills, AI disruption scoring.

**Tasks:**
- [ ] `TrendAnalysisService` — time-series su ClickHouse (90d, 12m)
- [ ] Stagionalità corretta (normalizzazione su periodo)
- [ ] `EmergingSkillsService` — skill con growth_momentum > 7.0 nelle ultime 4 settimane
- [ ] `AIDisruptionScoringService` — combina task automation + commoditization per ruolo
- [ ] `GET /api/v1/market/trends?geo=IT&period=12m`
- [ ] `GET /api/v1/market/emerging-skills` — top movers
- [ ] Redis cache: KPI con TTL 6h
- [ ] Integration test: trend API con seed ClickHouse

**Definition of Done:**
- Emerging skills identificate correttamente
- AI risk score differenziato per ruolo (non uniforme)
- Cache KPI funzionante

---

## FASE 4 — Recommendation Layer (Sprint 11–13)

### Sprint 11 — Skill Gap Analysis & Transition Scoring

**Goal:** Calcolo deterministico skill gap e difficoltà transizione.

**Tasks:**
- [ ] `SkillGapAnalyzer` — delta tra skill utente validate e skill richieste per ruolo target
- [ ] Dati skill richieste: aggregati da job listings per ruolo
- [ ] `TransitionFeasibilityService` — calcola:
  - Skill overlap %
  - Learning curve stimata (settimane per skill)
  - Difficoltà: easy/medium/hard (basato su gap count + complexity)
  - Mesi stimati per diventare competitivo
- [ ] `ROIEstimator` — salary uplift % + employability uplift
- [ ] Unit test: skill gap con profilo fixture vs ruolo target
- [ ] Unit test: transition difficulty con overlap 80% → easy

**Definition of Done:**
- Skill gap calcolato correttamente
- Transition difficulty coerente con overlap
- ROI estimato basato su KPI reali

---

### Sprint 12 — Recommendation Engine

**Goal:** Generazione percorsi raccomandati ordinati per ROI.

**Tasks:**
- [ ] `RecommendationEngine` — genera top 3–5 percorsi per utente
  1. Recupera KPI mercato per ruoli candidati
  2. Calcola skill gap + transition difficulty
  3. Stima ROI (salary uplift × employability uplift × stability)
  4. Ordina per ROI ponderato
  5. Chiama LLM per rationale narrativo (su KPI già calcolati)
- [ ] `GenerateRecommendationCommand`
- [ ] `Recommendation` + `RecommendationPath` entities + migration
- [ ] `POST /api/v1/recommendations/generate`
- [ ] `GET /api/v1/recommendations/latest`
- [ ] `GET /api/v1/recommendations/{id}`
- [ ] Unit test: ordinamento percorsi per ROI
- [ ] Unit test: LLM non chiamato se KPI mancanti (fallback)

**Definition of Done:**
- Top 3 raccomandazioni generate con ROI ordinato
- Ogni path con skill gap, difficulty, salary uplift
- LLM rationale generato su KPI pre-calcolati

---

### Sprint 13 — Career Simulation Engine

**Goal:** Simulazione "what-if" per scenari di carriera alternativi.

**Tasks:**
- [ ] `CareerSimulationService` — proiezione 1/3/5 anni per 2 scenari
  - Scenario A: rimango nel ruolo attuale
  - Scenario B: transizione al ruolo target
- [ ] Proiezione salary basata su KPI trend (non inventata)
- [ ] `GET /api/v1/simulation?targetRole=...` — run simulazione
- [ ] Stima probabilità successo transizione (overlap, market demand, timing)
- [ ] `GET /api/v1/recommendations/{id}/paths/{pathId}/explain` — spiegazione LLM
- [ ] Integration test: simulazione con profilo e mercato fixture

**Definition of Done:**
- Simulazione mostra delta significativo tra scenari
- Proiezione basata su trend reali, non random
- Explain endpoint ritorna testo coerente con KPI passati

---

## FASE 5 — Explainability & Trust (Sprint 14–15)

### Sprint 14 — Source Attribution & Confidence

**Goal:** Ogni output mostra fonti, timestamp e confidence.

**Tasks:**
- [ ] `DataSourceDto` in ogni risposta KPI: `{source, lastUpdated, dataPoints}`
- [ ] Confidence score visibile in ogni recommendation path
- [ ] Warning UI quando `DataConfidence < 0.4`
- [ ] `SourceAttributionService` — traccia quale data source ha contribuito a quale KPI
- [ ] Timestamp KPI visibili in API response
- [ ] `GET /api/v1/market/skills/{id}/kpis` arricchito con sources
- [ ] Test: response con confidence bassa include warning flag

**Definition of Done:**
- Ogni KPI ha fonte e data aggiornamento
- Confidence visibile in tutte le recommendation
- Warning corretto per dati con bassa affidabilità

---

### Sprint 15 — Dashboard Data Layer (API-ready)

**Goal:** API pronte per alimentare tutte le 5 dashboard frontend.

**Tasks:**
- [ ] `GET /api/v1/monitoring/dashboard` — aggregato per Career Competitiveness Dashboard
  - Competitive score, salary percentile, market fit, AI risk, trend 12w
- [ ] `GET /api/v1/profile/skill-radar` — dati per Skill Radar (validated vs market)
- [ ] `GET /api/v1/market/trends/dashboard` — dati Market Intelligence Dashboard
- [ ] `GET /api/v1/simulation/comparison` — dati Career Simulation Dashboard
- [ ] `GET /api/v1/market/ai-risk/dashboard` — dati AI Risk Dashboard
- [ ] Swagger documentazione completa per tutti gli endpoint
- [ ] Integration test: ogni dashboard endpoint con seed data

**Definition of Done:**
- Tutti gli endpoint dashboard funzionanti e documentati
- Response shape stabile (non breaking change)

---

## FASE 6 — Monitoring Platform (Sprint 16–17)

### Sprint 16 — Alert System

**Goal:** Sistema di alert automatici su cambiamenti mercato rilevanti.

**Tasks:**
- [ ] `AlertingJob` Hangfire — ogni ora
- [ ] 5 tipi alert implementati:
  - `MarketDemandAlert` (calo > 20%)
  - `SalaryAlert` (variazione > 15%)
  - `EmergingSkillAlert` (momentum > 8.0)
  - `AIRiskAlert` (risk score supera soglia)
  - `CompetitivenessAlert` (utente scende < percentile 40)
- [ ] `UserAlert` entity + migration
- [ ] `GET /api/v1/monitoring/alerts` — lista alert non letti
- [ ] `PUT /api/v1/monitoring/alerts/{id}/read`
- [ ] De-duplication alert: non inviare stesso alert due volte in 7 giorni
- [ ] Test: alert generato correttamente su variazione soglia

**Definition of Done:**
- Alert generati correttamente per ogni tipo
- De-duplication funzionante
- API alerts funzionante

---

### Sprint 17 — Competitive Tracking & Snapshots

**Goal:** Tracking storico competitività utente nel tempo.

**Tasks:**
- [ ] `MonitoringSnapshotJob` Hangfire — snapshot settimanale per utente attivo
- [ ] `UserMonitoringSnapshot` entity + migration
- [ ] `GET /api/v1/monitoring/competitiveness-history` — trend 6 mesi
- [ ] `PeriodicReassessmentService` — notifica quando assessment > 3 mesi fa
- [ ] `PersonalTrendService` — variazione score personale nel tempo
- [ ] Integration test: snapshot creato correttamente ogni settimana

**Definition of Done:**
- Snapshot settimanale attivo per utenti premium
- History API ritorna trend corretto
- Notifica reassessment funzionante

---

## FASE 7 — Advanced AI Features (Sprint 18–20)

### Sprint 18 — Personalized Roadmap Generation

**Goal:** Roadmap evolutiva personalizzata con milestone concrete.

**Tasks:**
- [ ] `RoadmapGenerationService` — genera roadmap 6/12/24 mesi
- [ ] Milestone basate su: skill gap, market demand, transition time
- [ ] Prioritizzazione skill: impatto salary uplift × richiesta mercato
- [ ] `LearningPrioritizationEngine` — ordina skill gap per ROI
- [ ] `POST /api/v1/recommendations/{id}/roadmap`
- [ ] Roadmap include: skill, timeframe, motivo (KPI-backed)

---

### Sprint 19 — Conversational Career Strategist

**Goal:** Interfaccia conversazionale per domande strategiche di carriera.

**Tasks:**
- [ ] `CareerConversationService` — multi-turn chat con Claude
- [ ] Context window: profilo utente + KPI + recommendation già generata
- [ ] Guardrail: Claude non può inventare numeri non nel context
- [ ] `POST /api/v1/chat/career` — endpoint conversazione
- [ ] Rate limit: 20 messaggi/giorno free, illimitato premium
- [ ] Streaming response via `text/event-stream`

---

### Sprint 20 — Predictive Employability Forecasting

**Goal:** Previsione employability a 12 mesi basata su trend mercato.

**Tasks:**
- [ ] `EmployabilityForecastService` — modello predittivo (ML.NET o regresso lineare)
- [ ] Input: KPI trend storici + profilo utente
- [ ] Output: employability score predetto a 3/6/12 mesi
- [ ] Scenario worst/base/best case
- [ ] `GET /api/v1/forecast/employability`
- [ ] Confidence interval visibile su ogni previsione

---

## Dipendenze Critiche

```
Sprint 1 (Auth) → Sprint 2 (Profile) → Sprint 3 (Skills)
    ↓                                        ↓
Sprint 4–6 (Assessment)              Sprint 7–10 (Market)
         ↓                                   ↓
              Sprint 11–13 (Recommendations)
                        ↓
              Sprint 14–15 (Explainability)
                        ↓
              Sprint 16–17 (Monitoring)
                        ↓
              Sprint 18–20 (Advanced AI)
```

---

## Priorità MVP (Sprint 1–9)

Il **MVP funzionale** si raggiunge alla fine dello Sprint 9:

| Funzionalità | Sprint |
|---|---|
| Registrazione + Auth | S01 |
| Profilo + Skills | S02–S03 |
| Assessment adattivo | S04–S05 |
| KPI mercato calcolati | S07–S09 |

Questo è il **nucleo differenziante** del prodotto: assessment reale + KPI deterministici.  
Le raccomandazioni (S11–S13) completano il valore minimo commercializzabile.

---

## Note Tecniche per il Team

### Regola Fondamentale AI
Prima di chiamare qualsiasi LLM, assicurarsi che:
1. I dati quantitativi siano già stati calcolati deterministicamente
2. Il LLM riceva solo dati pre-validati come input
3. L'output LLM sia testo narrativo, mai numeri da esporre direttamente

### Naming Convention
- **Commands** terminano in `Command` — mutano stato
- **Queries** terminano in `Query` — solo lettura
- **Services** nel layer Application — logica orchestrazione, no infra
- **Jobs** nel layer Infrastructure — Hangfire, accede a DB direttamente

### Test Strategy
- **Domain**: unit test puri, no mock, no DB
- **Application**: unit test con mock di repo e IAIOrchestrator
- **API Integration**: Testcontainers per PostgreSQL reale, mock LLM

---

*Piano Sprint generato il 2026-05-13 — AEGIS Platform v0.1*
