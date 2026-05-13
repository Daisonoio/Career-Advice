# FA-002 — Analisi Funzionale: Specializzazione di Carriera e Piano di Sviluppo Personale

**Tipo:** Functional Analysis  
**Stato:** Completata  
**Data:** 2026-05-13  
**Ruolo:** Analista Funzionale  
**Scope:** Sezione "Career Specialization" — direzione chiara di crescita con KPI, roadmap e consigli per la posizione attuale  

---

## 1. Analisi del Codice Esistente

### 1.1 Cosa esiste già

Il sistema attuale implementa una logica di raccomandazione di carriera con le seguenti capacità:

**`GenerateRecommendationCommand`**
- Valuta 6 ruoli candidati tramite scoring composito deterministico
- Calcola skill gap come lista di canonical name mancanti (es. `["kubernetes", "terraform"]`)
- Stima difficoltà di transizione (Easy/Moderate/Challenging/Hard) e mesi necessari
- Genera rationale narrativo via LLM su KPI pre-calcolati

**`RecommendationPath`**
- Contiene: TargetRole, SalaryUpliftPct, TransitionDifficulty, EstimatedMonths, SkillOverlapPct
- `SkillGaps` è una stringa JSON piatta — lista di nomi, senza struttura, priorità o sequenza

**`AssessmentResult`**
- Contiene: ValidatedSkills (con livello e evidence), WeakSignals, SuspectedInflations
- Il LlmSummary è testo libero, non strutturato in sezioni actionable

**`MarketKpi`**
- 6 KPI per ruolo/skill: DemandScore, SalaryStrength, Saturation, AIRisk, GrowthMomentum, CareerStability
- I KPI esistono per **ruolo** ma non per **skill singola** (salvo SkillId che è opzionale e non usato nei path)

### 1.2 Gap funzionali identificati

| Funzionalità | Stato attuale | Gap |
|---|---|---|
| Skill gap come lista piatta | ✅ Esiste | ❌ Nessuna priorità, ROI, sequenza |
| Roadmap con milestone | ❌ Assente | Da costruire completamente |
| Specializzazione all'interno di un ruolo | ❌ Assente | Solo ruoli generici, no specializzazioni |
| Consigli per la posizione attuale | ❌ Assente | Nessuna sezione "migliora dove sei" |
| Prioritizzazione skill per impatto salariale | ❌ Assente | Tutti i gap sono equivalenti |
| Depth analysis per skill (non solo presenza/assenza) | ❌ Parziale | SelfRatedLevel esiste ma non usato nel gap |
| Competitive positioning (come mi confronto con i peer) | ❌ Assente | SalaryPercentile esiste ma non breakdown |
| Timeline per fase (non solo totale mesi) | ❌ Assente | Solo numero totale mesi |
| Consigli skill premium (pay extra vs richieste base) | ❌ Assente | Non differenziato |
| Scenario "rimango qui" vs "mi muovo" | ❌ Assente | Solo path di transizione |

---

## 2. Obiettivo della Nuova Sezione

La sezione **Career Specialization** deve rispondere a tre domande concrete:

> **1. Dove voglio arrivare?** → Direzione di specializzazione con KPI di mercato
> **2. Come ci arrivo?** → Roadmap a fasi con skill prioritizzate per ROI
> **3. Come miglioro dove sono adesso?** → Consigli immediati sulla posizione corrente

Non è una funzionalità decorativa. È il **core differenziante del prodotto**: trasformare l'assessment e i KPI di mercato in un piano operativo personale.

---

## 3. Struttura Funzionale Proposta

### 3.1 Modulo A — Specialization Profile

**Concetto:** Ogni ruolo generico ha 2-4 percorsi di specializzazione. La piattaforma aiuta l'utente a scegliere la specializzazione con il miglior fit tra profilo personale e domanda di mercato.

**Esempi di specializzazioni per ruolo:**

| Ruolo Generico | Specializzazioni |
|---|---|
| Backend Engineer | Distributed Systems · API/Integration · High-Performance · Security-focused |
| Platform Engineer | Cloud Infrastructure · Internal Developer Platform · SRE/Reliability · FinOps |
| Software Architect | Enterprise Architecture · Microservices · Event-Driven · API-first |
| AI/ML Engineer | LLM/GenAI · Computer Vision · MLOps · Recommendation Systems |
| DevOps Engineer | GitOps/IaC · Observability · Security (DevSecOps) · Platform Engineering |
| Tech Lead | Engineering Management · Principal Engineer · Staff Engineer · CTO track |

**Output atteso — Specialization Profile:**
```json
{
  "currentRole": "Backend Engineer",
  "suggestedSpecialization": "Distributed Systems",
  "specializationFit": 0.78,
  "marketDemandForSpecialization": 8.2,
  "salaryPremiumForSpecialization": "+18%",
  "aiRiskForSpecialization": 2.4,
  "keyDifferentiatingSkills": ["Apache Kafka", "Distributed Tracing", "Consensus Algorithms"],
  "rationale": "Il tuo stack (PostgreSQL, Redis, Docker) è la base ideale per Distributed Systems. La domanda EU è in crescita del 34% YoY con saturazione bassa."
}
```

---

### 3.2 Modulo B — Skill Prioritization Engine

**Concetto:** Non tutti i gap di skill hanno lo stesso valore. Il sistema deve ordinare le skill da acquisire in base a un ROI calcolato, non in ordine alfabetico o casuale.

**Formula ROI per skill (deterministica):**

```
SkillROI = (salary_premium_pct × 0.35)
         + (market_demand_score × 0.25)
         + (growth_momentum × 0.20)
         - (learning_difficulty × 0.10)
         - (ai_risk_score × 0.10)
```

**Classificazione skill per tipo:**

| Tipo | Definizione | Esempio |
|---|---|---|
| **Foundation** | Prerequisito non negoziabile per il ruolo | Docker per Platform Engineer |
| **Differentiating** | Aumenta competitività e salary sopra mediana | Kafka per Backend Engineer |
| **Premium** | Richiesta dal top 20% degli annunci, salary P75+ | eBPF per SRE, LLM Integration per Backend |
| **Nice-to-have** | Frequente ma non determinante | REST API (già diffusissimo) |
| **Declining** | Presente ma in calo di domanda | XML/SOAP, monolithic patterns |

**Output atteso — Prioritized Skill Plan:**
```json
{
  "prioritizedGaps": [
    {
      "skill": "Apache Kafka",
      "type": "Differentiating",
      "skillRoi": 7.8,
      "salaryPremiumPct": 14.0,
      "marketDemandScore": 8.1,
      "estimatedWeeksToCompetency": 10,
      "learningDifficulty": "Medium",
      "whyNow": "Crescita YoY +28%, bassa saturazione di profili con questa skill"
    },
    {
      "skill": "Distributed Tracing",
      "type": "Premium",
      "skillRoi": 6.9,
      "salaryPremiumPct": 11.0,
      "estimatedWeeksToCompetency": 6,
      "learningDifficulty": "Low-Medium"
    }
  ]
}
```

---

### 3.3 Modulo C — Structured Roadmap

**Concetto:** Una roadmap a fasi con obiettivi concreti, non un elenco generico. Ogni fase ha: skill target, KPI di completamento, salary step atteso, durata.

**Struttura a 3 fasi:**

```
FASE 1 — Foundation (0–3 mesi)
└── Consolida le skill Foundation mancanti
└── Obiettivo: diventare credibile nel ruolo target
└── KPI: AssessmentScore layer 1 > 0.7 sulle skill Foundation
└── Salary impact: accesso alla mediana di mercato

FASE 2 — Differentiation (3–9 mesi)
└── Acquisisce le skill Differentiating prioritizzate per ROI
└── Obiettivo: superare il candidato medio, uscire dalla mediana
└── KPI: almeno 2 skill Differentiating validate > livello 3
└── Salary impact: accesso al P60–P70 di mercato

FASE 3 — Premium (9–18 mesi)
└── Acquisisce 1–2 skill Premium ad alto impatto salariale
└── Obiettivo: essere nel top 20% del proprio ruolo/specializzazione
└── KPI: almeno 1 skill Premium validata, portfolio di progetti concreti
└── Salary impact: accesso al P75–P85 di mercato
```

**Output atteso — Roadmap Entity:**
```json
{
  "targetRole": "Platform Engineer",
  "targetSpecialization": "Cloud Infrastructure",
  "totalEstimatedMonths": 12,
  "phases": [
    {
      "phase": 1,
      "name": "Foundation",
      "durationMonths": 3,
      "skills": ["Terraform", "Docker avanzato"],
      "completionKpi": "Assessment Layer 1 score > 0.70 su Infrastructure",
      "expectedSalaryRange": "€52k–€60k",
      "milestones": [
        "Deploy di un'infrastruttura IaC su cloud provider reale",
        "Containerizzare un'applicazione esistente con Docker Compose"
      ]
    }
  ]
}
```

---

### 3.4 Modulo D — Current Position Advisor

**Concetto:** Sezione dedicata a "come miglioro dove sono adesso", indipendentemente dalla transizione. Risponde alla domanda: "Cosa posso fare nei prossimi 3 mesi per aumentare il mio valore nel ruolo corrente?"

**Aree di analisi:**

**D1 — Salary gap rispetto ai peer**
- Dove si trova il salario dell'utente rispetto alla distribuzione (P25/mediana/P75) per ruolo+geo+seniority
- Quali skill specifiche sono associate al salary P75 nel suo ruolo
- Stima di salary uplift raggiungibile senza cambiare ruolo

**D2 — Skill depth gap (non solo presenza)**
- Identifica skill che l'utente ha dichiarato ma che l'assessment ha valutato superficialmente
- Prioritizza il deepening delle skill esistenti vs l'acquisizione di skill nuove
- Logica: migliorare da livello 2 a livello 4 su una skill core vale spesso più che aggiungere una skill nuova

**D3 — Visibilità e credibilità**
- Consigli su come rendere verificabile la propria competenza
- GitHub activity, contributi open source, certificazioni rilevanti per il mercato
- Differenza tra skill "dichiarata" e skill "dimostrabile"

**D4 — Rischio stagnazione attuale**
- Se il profilo non evolve nei prossimi 12 mesi, dove si posizionerà?
- Stima del decay del valore di mercato basata su trend delle skill attuali
- Alert su skill nel profilo con `GrowthMomentum < 3.0` (skill in declino)

**Output atteso — Current Position Report:**
```json
{
  "currentSalaryPercentile": 42,
  "salaryGapToMedian": "€8.000/anno",
  "salaryGapToP75": "€18.000/anno",
  "quickWins": [
    {
      "action": "Approfondisci PostgreSQL da livello 2 a livello 4",
      "estimatedSalaryImpact": "+€4.000/anno",
      "estimatedWeeks": 8,
      "rationale": "PostgreSQL avanzato (query optimization, partitioning) è presente nel 67% degli annunci senior EU"
    }
  ],
  "stagnationRisk": {
    "score": 6.2,
    "decliningSkills": ["REST API generiche", "monolithic .NET Framework"],
    "recommendation": "Il tuo stack è solido ma fermo. Nessuna skill con crescita > 5.0 nel profilo."
  },
  "credibilityGaps": [
    "Nessun progetto pubblico verificabile",
    "C# dichiarato livello 5, assessment indica livello 3"
  ]
}
```

---

## 4. KPI della Sezione (misurabili nel prodotto)

### KPI di business (successo della funzionalità)

| KPI | Definizione | Target 6 mesi |
|---|---|---|
| **Roadmap Adoption Rate** | % utenti che aprono la roadmap dopo generazione | > 65% |
| **Roadmap Completion Rate** | % utenti che completano almeno la Fase 1 | > 25% |
| **Salary Uplift Reported** | Utenti che riportano aumento salariale entro 12 mesi | > 15% (premium) |
| **Skill Acquisition Tracking** | % utenti che aggiornano skill dopo la roadmap | > 40% |
| **Current Advisor CTR** | % utenti che consultano la sezione "migliora ora" | > 50% |
| **Assessment Retake Rate** | % utenti che ri-fanno l'assessment dopo 3 mesi | > 30% |
| **Recommendation Trust Score** | Rating medio delle raccomandazioni (survey in-app) | > 4.1/5.0 |

### KPI tecnici (qualità del sistema)

| KPI | Definizione | Target |
|---|---|---|
| **Skill Prioritization Accuracy** | % skill raccomandate che risultano effettivamente richieste in annunci reali | > 80% |
| **Roadmap Salary Accuracy** | Delta tra salary range stimato e salary effettivo utente dopo transizione | < ±15% |
| **Assessment-Roadmap Coherence** | % skill nella roadmap che sono state identificate come gap nell'assessment | > 85% |
| **Phase Duration Accuracy** | Delta tra durata stimata fase 1 e durata effettiva dichiarata dagli utenti | < ±30% |

---

## 5. Analisi dei Gap nel Codice Esistente — Dettaglio

### 5.1 Cosa riusare senza modifiche

| Componente | Riuso |
|---|---|
| `SeniorityScorer` | Riusabile direttamente per calcolare seniority score da dare in input al Roadmap Engine |
| `MarketKpi` entity | Riusabile: aggiungere `SkillId` population nei seed e nei collector |
| `AssessmentResult.ValidatedSkills` | Riusabile come input per il Skill Depth Gap |
| `GenerateRecommendationCommand` — composite scoring | Riusabile come base, da estendere con SkillROI |
| `RecommendationPath` entity | Riusabile: aggiungere colonne per specialization e phase breakdown |

### 5.2 Cosa modificare

**`RecommendationPath`** — aggiungere:
- `SpecializationName` (string?)
- `SkillGaps` deve diventare strutturata: non JSON piatta ma lista di `PrioritizedSkillGap` con ROI, tipo, settimane
- `RoadmapPhases` (JSON): array di fasi con skill, milestone, salary range per fase

**`MarketKpi`** — aggiungere:
- Popolamento del campo `SkillId` nei seed data e nei collector (attualmente è null)
- `SalaryPremiumPct` per skill (quanto vale questa skill in più rispetto alla mediana del ruolo)

**`AssessmentResult`** — aggiungere:
- `SkillDepthGaps`: skill presenti ma sotto il livello atteso per la seniority stimata
- `StagnationRiskScore` (double): calcolato deterministicamente dal trend delle skill validate

**`ProfileResponse`** — aggiungere:
- `SalaryPercentileForCurrentRole` (double?)
- `StagnationRiskScore` (double?)
- `QuickWins` (lista delle 3 azioni immediate con impatto stimato)

### 5.3 Cosa costruire da zero

| Componente | Tipo | Layer |
|---|---|---|
| `CareerSpecialization` entity | Nuovo entity Domain | Domain |
| `SpecializationPath` entity | Nuovo entity Domain | Domain |
| `RoadmapPhase` value object | Value Object | Domain |
| `PrioritizedSkillGap` value object | Value Object | Domain |
| `SkillPrioritizationService` | Service deterministico | Application |
| `SpecializationMatchEngine` | Service deterministico | Application |
| `CurrentPositionAdvisorService` | Service | Application |
| `GenerateRoadmapCommand` | CQRS Command | Application |
| `GetCurrentPositionReportQuery` | CQRS Query | Application |
| `GetSpecializationsForRoleQuery` | CQRS Query | Application |
| `RoadmapController` | Controller | API |
| `SpecializationController` | Controller | API |

---

## 6. Nuovi Endpoint API

```
# Specializzazioni disponibili per ruolo
GET /api/v1/specializations?role={roleCanonical}
→ Lista specializzazioni con market fit score per il profilo utente

# Genera roadmap personalizzata
POST /api/v1/roadmap/generate
Body: { targetRole, targetSpecialization (optional) }
→ Roadmap a fasi con skill prioritizzate per ROI

# Recupera roadmap attiva
GET /api/v1/roadmap/current
→ Roadmap con stato avanzamento fasi

# Report posizione attuale
GET /api/v1/career/current-position
→ Salary percentile, quick wins, stagnation risk, credibility gaps

# Dettaglio priorità skill
GET /api/v1/career/skill-priority?targetRole={role}&targetSpecialization={spec}
→ Lista skill prioritizzate con ROI, tipo, settimane stimate
```

---

## 7. Schema Dati Aggiuntivo

```sql
-- Specializzazioni per ruolo
career_specializations (
  Id, RoleCanonical, Name, CanonicalName, Description,
  KeySkills jsonb,        -- skill fondamentali per questa specializzazione
  SalaryPremiumPct float, -- premium medio rispetto al ruolo generico
  DemandScore float,      -- domanda di mercato per questa specializzazione
  AIRiskScore float,
  CreatedAt, UpdatedAt
)

-- Roadmap generata per utente
career_roadmaps (
  Id, UserId, RecommendationId,
  TargetRole, TargetSpecialization,
  TotalEstimatedMonths int,
  CurrentPhase int,
  Status varchar(20),      -- active, completed, abandoned
  GeneratedAt, UpdatedAt
)

-- Fasi della roadmap
roadmap_phases (
  Id, RoadmapId, PhaseNumber, Name, DurationMonths,
  Skills jsonb,              -- [{skill, type, roi, weeks, milestone}]
  CompletionKpiDefinition text,
  ExpectedSalaryMin decimal, ExpectedSalaryMax decimal,
  CompletedAt,
  CreatedAt
)

-- Aggiunta a MarketKpi (nuova colonna)
ALTER TABLE market_kpis ADD COLUMN salary_premium_pct float;
-- Popolare SkillId per ogni skill con KPI individuali
```

---

## 8. Logiche Deterministiche da Implementare

### 8.1 SkillPrioritizationService

```
Input: userSkills (con livelli), targetRole, targetSpecialization, marketKpis
Output: lista PrioritizedSkillGap ordinata per SkillROI

Algoritmo:
1. Calcola set skill richieste per ruolo+specializzazione
2. Per ogni skill mancante o sotto il livello soglia:
   a. Recupera MarketKpi per quella skill
   b. Calcola SkillROI = (salary_premium × 0.35) + (demand × 0.25) + (growth × 0.20)
                        - (learning_difficulty × 0.10) - (ai_risk × 0.10)
   c. Classifica come Foundation / Differentiating / Premium / Nice-to-have
3. Ordina per SkillROI desc
4. Assegna sequenza temporale (Foundation prima, poi Differentiating, poi Premium)
```

### 8.2 StagnationRiskScorer

```
Input: validatedSkills con livelli, marketKpis per quelle skill
Output: StagnationRiskScore (0–10, 10=alto rischio)

Algoritmo:
1. Per ogni skill validata, recupera GrowthMomentum dal MarketKpi
2. Calcola punteggio portfolio:
   - Nessuna skill con GrowthMomentum > 6.0 → rischio alto
   - > 2 skill con GrowthMomentum < 3.0 → rischio medio-alto
   - Anni di esperienza nel ruolo corrente > 4 senza progressione seniority → +2 rischio
3. Combina in score 0–10
```

### 8.3 SalaryPercentileCalculator

```
Input: userSalaryMidpoint, roleCanonical, geoCountry, seniorityLevel
Output: percentile 0–100

Algoritmo:
1. Recupera MarketKpi per ruolo+geo: SalaryP25, SalaryMedian, SalaryP75
2. Interpolazione lineare:
   - Sotto P25: percentile = (salary / P25) × 25
   - P25–Median: percentile = 25 + ((salary - P25) / (Median - P25)) × 25
   - Median–P75: percentile = 50 + ((salary - Median) / (P75 - Median)) × 25
   - Sopra P75: percentile = 75 + min(((salary - P75) / P75) × 25, 25)
```

---

## 9. Integrazione con il LLM (Claude)

Il LLM interviene **solo dopo** che tutti i calcoli deterministici sono completati, in questi punti:

| Punto | Input al LLM | Output atteso |
|---|---|---|
| Rationale specializzazione | Specialization match score + KPI + profilo | Spiegazione narrativa perché questa specializzazione |
| Motivazione priorità skill | SkillROI calcolato + market KPI per skill | "Perché imparare Kafka prima di Terraform in questo momento" |
| Quick wins posizione attuale | Salary gap + skill depth gap + stagnation risk | 3 consigli concreti e prioritizzati |
| Executive summary roadmap | Tutte le fasi + KPI + salary projection | Testo che introduce la roadmap all'utente |

**Regola invariante**: il LLM non produce mai numeri, percentili, score o salary range. Riceve quei dati come input e li trasforma in narrativa comprensibile.

---

## 10. Relazione con Funzionalità Esistenti

```
Assessment (Layer 1–3)
    │
    ├──→ ValidatedSkills + SeniorityScore
    │         │
    │         ▼
    │    SkillPrioritizationService ←── MarketKpi (per skill)
    │         │
    │         ▼
    │    StagnationRiskScorer
    │         │
    │         ▼
    │    CurrentPositionAdvisorService
    │         │
    │         └──→ /api/v1/career/current-position
    │
    └──→ SpecializationMatchEngine ←── CareerSpecializations catalog
              │
              ▼
         GenerateRoadmapCommand ←── SkillPrioritizationService
              │
              ▼
         CareerRoadmap (entity) con RoadmapPhases
              │
              └──→ /api/v1/roadmap/current
```

---

## 11. Dipendenze e Prerequisiti

Prima di implementare questa sezione è necessario che siano disponibili:

| Prerequisito | Stato | Note |
|---|---|---|
| MarketKpi popolati per skill singole (non solo ruolo) | ❌ Mancante | Attualmente SkillId non viene popolato nel seed |
| `salary_premium_pct` per skill nei KPI | ❌ Mancante | Da aggiungere a MarketKpi |
| Tassonomia specializzazioni per ruolo | ❌ Mancante | Seed data da definire |
| Assessment completato (per skill depth) | ✅ Funzionante | Già implementato |
| Profilo utente con salary expectation | ✅ Funzionante | SalaryRange value object presente |

---

## 12. Priorità di Implementazione (ordine Sprint)

| Priorità | Componente | Sprint suggerito | Dipendenze |
|---|---|---|---|
| 1 | Seed specializzazioni + MarketKpi per skill | S09 (Market Intel.) | — |
| 2 | `SkillPrioritizationService` + `PrioritizedSkillGap` | S11 (Gap Analysis) | Seed specializzazioni |
| 3 | `CurrentPositionAdvisorService` + `/career/current-position` | S12 | MarketKpi per skill |
| 4 | `GenerateRoadmapCommand` + entity `CareerRoadmap` | S13 | SkillPrioritization |
| 5 | `SpecializationMatchEngine` + endpoint specializzazioni | S13 | Seed specializzazioni |
| 6 | LLM narrative per roadmap + quick wins | S15 (Explainability) | Tutti i deterministici |

---

## 13. Rischi Funzionali

| Rischio | Impatto | Mitigazione |
|---|---|---|
| SkillROI non percepito come accurato dall'utente | Alto — perde fiducia | Mostrare le fonti e i KPI che compongono il ROI (explainability) |
| Roadmap troppo lunga → demotivazione | Alto | Mostrare sempre la Fase 1 come obiettivo immediato, le altre come orizzonte |
| Skill premium cambiano velocemente | Medio | TTL su classificazione skill, ricalcolo mensile |
| Salary percentile non accurato per geo minori | Medio | Flag `data_confidence` basso + messaggio "dati limitati per questa area" |
| Stagnation risk percepito come giudizio negativo | Medio | UX: "opportunità di crescita" non "stai perdendo valore" |

---

*Documento FA-002 — Versione 1.0 — 2026-05-13*
