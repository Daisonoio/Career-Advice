# FA-001 — Analisi di Fattibilità: Raccolta Dati di Mercato

**Tipo:** Feasibility Analysis  
**Stato:** Completata  
**Data:** 2026-05-13  
**Autore:** Team AEGIS  
**Scope:** Raccolta dati per domanda di ruolo, annunci di lavoro, skill richieste  

---

## 1. Contesto e Obiettivo

Il Market Intelligence Engine di AEGIS richiede dati reali e aggiornati su:

1. **Domanda del ruolo** — quanti annunci esistono per un dato ruolo, in quale area geografica, con quale trend temporale
2. **Annunci di lavoro** — testo strutturato degli annunci per estrarre skill richieste, seniority, salary range, tipo di contratto, remote policy
3. **Skill richieste** — quali competenze tecnica vengono richieste per ogni ruolo, con quale frequenza, con quale peso salariale

Attualmente il sistema usa **seed data statici** (6 ruoli, dati EU 2024 hardcoded). Questa analisi valuta le opzioni per sostituirli con dati reali e aggiornati.

---

## 2. Fonti Dati Candidate

### 2.1 Job Boards — Accesso diretto

| Fonte | API Ufficiale | Qualità Dati | Costo | Note Legali |
|---|---|---|---|---|
| **LinkedIn Jobs** | ✅ Partner API | Alta | Alto (enterprise) | ToS severi su scraping; API richiede approvazione LinkedIn |
| **Indeed** | ✅ Publisher API | Alta | Gratuita (limitata) | Indeed Publisher Program, richiede registrazione |
| **Glassdoor** | ✅ API (deprecated 2024) | Media-Alta | N/D | API per partner; dati salary molto utili |
| **Wellfound (AngelList)** | ❌ No API pubblica | Alta (startup) | — | Solo scraping, ToS proibisce |
| **RemoteOK** | ✅ API JSON pubblica | Media | Gratuita | JSON feed pubblico: `remoteok.com/remote-jobs.json` |
| **Himalayas** | ✅ API pubblica | Media | Gratuita | Feed RSS + API semplice |
| **JobsPikr** | ✅ API commerciale | Alta | ~$200/mese | Aggrega 20+ fonti, dati normalizzati |
| **Adzuna** | ✅ API pubblica | Alta | Freemium | API REST con 50k richieste/mese gratuite, dati EU ottimi |
| **The Muse** | ✅ API pubblica | Media | Gratuita | Focus culture aziendale |

### 2.2 Salary Intelligence

| Fonte | Accesso | Qualità | Costo | Note |
|---|---|---|---|---|
| **Levels.fyi** | ❌ No API pubblica | Alta (tech) | — | Dati crowd-sourced accurati; nessuna API ufficiale |
| **Glassdoor Salaries** | ✅ API (limitata) | Alta | Partnership | Dati storici ottimi; API in renegoziation dopo acquisizione Indeed |
| **Payscale** | ✅ API Enterprise | Alta | Alto | Target HR enterprise, costi elevati |
| **LinkedIn Salary Insights** | ✅ Partner only | Alta | Enterprise | Solo per partner approvati |
| **Stack Overflow Survey** | ✅ Dataset pubblico | Alta (dev) | Gratuito | Dati annuali aggregati per linguaggio/ruolo/geo |
| **ISTAT / Eurostat** | ✅ Open Data | Bassa (granular) | Gratuito | Utile per contesto macro EU, non granulare per ruolo |
| **EU Jobs & Skills** | ✅ ESCO API | Media | Gratuito | Dataset EU ufficiale su skill e occupazioni |

### 2.3 Trend Tecnologici (Growth Momentum)

| Fonte | Accesso | Granularità | Costo |
|---|---|---|---|
| **GitHub Archive** | ✅ BigQuery public | Alta | Gratuito (query cost) |
| **Stack Overflow Trends** | ✅ Pubblico | Alta | Gratuito |
| **Google Trends API** | ✅ Pytrends (unofficiale) | Media | Gratuito |
| **NPM/PyPI download stats** | ✅ API pubblica | Alta (linguaggi) | Gratuito |
| **TIOBE Index** | ❌ No API | Media | — |
| **RedMonk Rankings** | ❌ No API | Media | — |
| **HN Hiring Trends** | ✅ Scraping HN thread mensile | Alta (tech) | Gratuito |

---

## 3. Vincoli Legali e ToS

### 3.1 Rischi per categoria

**Alto rischio (scraping proibito)**
- LinkedIn: ToS esplicito contro scraping automatizzato. Classe action precedente (hiQ Labs v. LinkedIn). Uso API ufficiale obbligatorio.
- Indeed: Permette solo crawling tramite Publisher Program ufficiale.
- Wellfound: Nessuna API, ToS proibisce scraping automatizzato.
- Glassdoor: ToS restrictivo post-acquisizione Indeed.

**Medio rischio (zona grigia)**
- Google Trends: `pytrends` è una libreria non ufficiale; Google non ha API pubblica per Trends. Uso tollerato con rate limiting molto basso.
- Hacker News: Tecnicamente pubblico, ma carico eccessivo viola le norme di buon uso.

**Basso rischio / permesso**
- RemoteOK: Feed JSON esplicitamente pubblico, con attribution.
- Adzuna: API con termini commerciali chiari e livello gratuito.
- Stack Overflow Survey: Dataset pubblico con licenza CC BY-SA.
- GitHub Archive: Dati pubblici su BigQuery.
- ESCO API: API EU pubblica con licenza aperta.
- NPM/PyPI stats: Open API.

### 3.2 GDPR e privacy
Gli annunci di lavoro non contengono PII (dati personali) se si escludono i recruiter nominativi.
I dati salariali crowd-sourced (Glassdoor, Levels.fyi) sono aggregati e anonimi.
**Nessun vincolo GDPR materiale** sulla raccolta di dati di mercato pubblici, purché non si traccino singoli individui.

---

## 4. Analisi Tecnica per Modalità di Accesso

### 4.1 API Ufficiali — Raccomandato

**Adzuna API** — candidato primario per job listings EU
```
GET https://api.adzuna.com/v1/api/jobs/{country}/search/{page}
    ?app_id={id}&app_key={key}
    &results_per_page=50
    &what=backend+engineer
    &where=italy
    &salary_min=30000
    &full_time=1
```
- 50.000 richieste/mese gratuite
- Dati strutturati: title, company, location, salary_min/max, description, category
- Copertura: IT, DE, FR, GB, US, AU, CA
- Aggiornamento: quasi real-time

**RemoteOK JSON Feed** — per ruoli remote-first
```
GET https://remoteok.com/remote-jobs.json
```
- Gratuito, pubblico, attribution richiesta
- 200-400 annunci aggiornati quotidianamente
- Skill tag pre-estratti dagli annunci
- Ideale per dati remote premium

**Indeed Publisher Program** — opzione secondaria
- Richiede registrazione come Publisher
- Feed XML/JSON per query specifiche
- Limitazioni su volume e uso commerciale

### 4.2 Dataset Pubblici — Per arricchimento

**Stack Overflow Annual Developer Survey**
- URL: `https://insights.stackoverflow.com/survey`
- Dataset CSV annuale con ~90.000 rispondenti
- Colonne utili: `LanguageHaveWorkedWith`, `DatabaseHaveWorkedWith`, `PlatformHaveWorkedWith`, `ConvertedCompYearly`, `Country`, `DevType`, `YearsCodePro`
- Frequenza: annuale (pubblicato ogni maggio)
- **Usabilità immediata**: eccellente per salary benchmark per tecnologia

**ESCO (European Skills, Competences, Qualifications and Occupations)**
- API: `https://ec.europa.eu/esco/api`
- Tassonomia ufficiale EU di ~3.000 occupazioni e ~13.000 skill
- Endpoint chiave: `/occupation`, `/skill`, `/iscoGroup`
- Permette di mappare ruoli canonici → skill richieste con fonte autorevole
- **Usabilità immediata**: ottima per arricchire la skill taxonomy

**GitHub Archive su BigQuery**
```sql
SELECT repo.name, COUNT(*) as pushes
FROM `githubarchive.month.*`
WHERE _TABLE_SUFFIX BETWEEN '202401' AND '202412'
  AND type = 'PushEvent'
GROUP BY repo.name
ORDER BY pushes DESC
```
- Dati mensili open source su BigQuery (costi query minimi)
- Misura adoption di linguaggi/framework tramite repository activity

### 4.3 Scraping controllato — Solo fonti senza API

Per fonti senza API e con ToS permissivo (es. HN "Who is Hiring" mensile):
- User-agent identificativo ("AegisBot/1.0 +https://aegis.io/bot")
- Rispetto di `robots.txt`
- Rate limit: max 1 request/10s per dominio
- Caching aggressivo (non riscaricare dati già acquisiti)
- Jitter randomico sulle richieste

**Non adottare** scraping su LinkedIn, Indeed, Glassdoor, Wellfound. Il rischio legale supera il beneficio.

---

## 5. Qualità dei Dati — Problemi Noti

### 5.1 Problemi strutturali degli annunci di lavoro

| Problema | Prevalenza | Impatto |
|---|---|---|
| Annunci duplicati (stesso annuncio su più board) | Alta (30-50%) | KPI inflazionati |
| Salary range assente | Alta (60-70% in EU) | Salary strength non calcolabile |
| Titolo non standardizzato ("Ninja Developer", "Rockstar Engineer") | Media | Classificazione ruolo errata |
| Skill buzzword generiche ("agile", "problem solving") | Alta | Noise nel skill extraction |
| Annunci fake/scaduti non rimossi | Bassa-Media | Domanda sovrastimata |
| Geolocalizzazione imprecisa ("Italy" vs "Milan, Italy") | Media | Geo-KPI aggregati errati |

### 5.2 Problemi dei dati salariali EU

- In Europa la pubblicazione del salary range non è obbligatoria (a differenza di alcuni stati USA)
- La **Pay Transparency Directive EU (2023/970)** — in recepimento entro giugno 2026 — obbligherà a pubblicare salary range negli annunci: **opportunità futura rilevante**
- I dati Glassdoor/Levels.fyi sono sbilanciati verso tech hub (Milano, Berlino, Londra)
- I dati crowd-sourced hanno selezione bias verso chi è soddisfatto o insoddisfatto del proprio salario

### 5.3 Skill extraction — complessità

Estrarre skill strutturate da testo libero di annunci è un problema NLP non banale:
- "esperienza con sistemi distribuiti" ≠ skill singola, ma cluster (Kafka, Kubernetes, Cassandra...)
- Abbreviazioni variabili: "k8s", "Kube", "Kubernetes" → stessa skill
- Skill implicite: "build a CI/CD pipeline" → richiede Docker + GitHub Actions + conoscenza scripting
- Skill falsamente richieste: molti annunci listano skill "nice to have" come obbligatorie

---

## 6. Pipeline di Raccolta Proposta

```
┌──────────────────────────────────────────────────────────────┐
│                    INGESTION LAYER                           │
│                                                              │
│  AdzunaCollector      RemoteOKCollector    SOSurveyLoader    │
│  (API, ogni 6h)       (Feed, ogni 6h)      (CSV, annuale)    │
│                                                              │
│  ESCOLoader           GitHubArchiveLoader  HNHiringParser    │
│  (API, settimanale)   (BigQuery, mensile)  (Scraper, mens.)  │
└──────────────────┬───────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────────┐
│                    CLEANING LAYER                            │
│                                                              │
│  • Deduplication (hash titolo+azienda+location)              │
│  • Fake job detection (pattern: no salary, no company, ...)  │
│  • Salary normalization (EUR, PPP adjustment per country)    │
│  • Location geocoding (city → ISO country + lat/lon)         │
│  • Date normalization (posted_at → UTC)                      │
└──────────────────┬───────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────────┐
│                    EXTRACTION LAYER                          │
│                                                              │
│  RoleClassifier        SkillExtractor      SeniorityDetector │
│  (titolo → ruolo       (NER su descrizione (junior/mid/      │
│   canonico)             → canonical skills) senior/lead)     │
└──────────────────┬───────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────────┐
│                    AGGREGATION LAYER                         │
│                                                              │
│  • job_count_by_role_geo (giornaliero)                       │
│  • salary_percentiles_by_role_geo (settimanale)              │
│  • skill_frequency_by_role (giornaliero)                     │
│  • skill_cooccurrence_matrix (settimanale)                   │
│  • trend_30d / trend_90d / trend_yoy (rolling window)        │
└──────────────────┬───────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────────┐
│                    KPI COMPUTATION                           │
│  (KpiComputationService — già implementato)                  │
└──────────────────────────────────────────────────────────────┘
```

---

## 7. Skill Extraction — Approcci Tecnici

### Approccio A — Dizionario + Regex (baseline, implementabile subito)
- Mantieni un dizionario canonico di 500+ skill con alias (già iniziato con i 40 seed skill)
- Regex case-insensitive su titolo e descrizione dell'annuncio
- Complessità: bassa. Precision: ~75%. Recall: ~60%
- **Limite**: non scala, non cattura skill nuove

### Approccio B — NER con spaCy / Flair (raccomandato per POC v2)
- Modello NER pre-trained su dataset tecnici (es. `en_core_web_trf` di spaCy)
- Fine-tuning su dataset annotato di annunci IT (disponibile su HuggingFace: `job-descriptions-ner`)
- Precision attesa: ~85-90%, Recall: ~80%
- **In .NET**: chiamata a microservizio Python via HTTP, oppure ONNX runtime per modelli esportati
- Complessità: media

### Approccio C — LLM-based extraction (alta qualità, costo elevato)
- Passa il testo dell'annuncio a Claude con prompt strutturato
- Output: JSON con lista skill canoniche + livello richiesto (required/nice-to-have)
- Precision: >95%, ma costo ~$0.002/annuncio → $200/100.000 annunci
- **Non adatto** per batch di milioni di annunci; ottimo per enrichment selettivo
- **Regola architetturale rispettata**: LLM estrae skill (classificazione), non genera KPI

### Approccio raccomandato (ibrido)
```
1. Dizionario + Regex  → cattura skill note (veloce, gratuito, batch)
2. NER microservizio    → cattura skill non in dizionario
3. LLM spot-check       → validazione campione + nuove skill emergenti
4. Feedback loop        → skill estratte da LLM alimentano il dizionario
```

---

## 8. Role Classification — Approcci Tecnici

Il problema: normalizzare "Senior Full Stack Developer React/Node" → `backend-engineer` o `frontend-engineer`?

### Tassonomia ruoli AEGIS (proposta)
```
engineering/
├── backend-engineer
├── frontend-engineer
├── fullstack-engineer
├── platform-engineer
├── devops-engineer
├── sre-engineer
├── mobile-engineer
├── embedded-engineer
data/
├── data-engineer
├── data-scientist
├── data-analyst
├── ml-engineer
├── mlops-engineer
architecture/
├── software-architect
├── solutions-architect
├── cloud-architect
leadership/
├── tech-lead
├── engineering-manager
├── cto
security/
└── security-engineer
```

### Metodo di classificazione
1. **Regole basate su keyword** (baseline): mapping titolo → categoria tramite pattern matching
2. **Embedding similarity**: calcola embedding del titolo annuncio, trova ruolo canonico più vicino nel vector space (già infrastruttura pgvector disponibile)
3. **ESCO mapping**: mappa a codice ESCO, poi ESCO → ruolo canonico AEGIS

---

## 9. Stima Volumi e Costi Operativi

### Volume dati stimato (mercato EU, focus IT)

| Fonte | Annunci/giorno | Skill estratte | Storage (30gg) |
|---|---|---|---|
| Adzuna EU | ~5.000 | ~25/annuncio | ~150 MB |
| RemoteOK | ~200 | ~10/annuncio | ~6 MB |
| HN Hiring (mensile) | ~800/mese | ~15/annuncio | ~1 MB |
| SO Survey (annuale) | ~90.000/anno | N/A (aggregato) | ~50 MB |
| **Totale** | ~5.200/giorno | — | ~200 MB/mese |

### Stima costi mensili (scenario POC)

| Componente | Costo stimato | Note |
|---|---|---|
| Adzuna API (gratuita) | €0 | Fino a 50k req/mese |
| RemoteOK feed | €0 | Pubblico |
| Storage PostgreSQL (job_listings) | ~€5 | ~200 MB/mese, PostgreSQL managed |
| ClickHouse (aggregazioni) | ~€10 | Dataset aggregato, molto compresso |
| Compute crawlers (Hangfire workers) | ~€20 | 1 worker instance, 2 vCPU |
| NER microservizio (opzionale) | ~€15 | Serverless, pay-per-use |
| **Totale POC** | **~€50/mese** | Senza NER: ~€35/mese |

### Stima costi mensili (scenario produzione, 10k utenti)

| Componente | Costo stimato |
|---|---|
| JobsPikr API (multi-source) | ~€200 |
| Storage + ClickHouse | ~€80 |
| Compute crawlers (scalabile) | ~€60 |
| NER/ML microservizio | ~€50 |
| **Totale produzione** | **~€390/mese** |

---

## 10. Rischi e Mitigazioni

| Rischio | Probabilità | Impatto | Mitigazione |
|---|---|---|---|
| Adzuna cambia pricing/ToS | Bassa | Alto | Astrazione `IJobDataSource`, switch agevole tra provider |
| Qualità skill extraction insufficiente | Media | Alto | Validazione manuale campione; confidence score su ogni skill estratta |
| Salary data assente per EU | Alta | Medio | Fallback su SO Survey aggregato; triangolazione con ESCO + Glassdoor |
| Rate limiting ban da fonte | Media | Medio | Jitter, backoff esponenziale, IP rotation (cloud provider NAT gateway) |
| Dati obsoleti (annunci non rimossi) | Alta | Medio | TTL su job_listings (90 giorni), campo `is_active` con verifica periodica |
| Legal action per scraping | Bassa (se rispettato piano) | Molto Alto | Solo API ufficiali + fonti esplicitamente pubbliche |
| Bias geografico (pochi dati per regioni minori) | Media | Basso | Flag `data_confidence` basso per geo con < 50 annunci/settimana |

---

## 11. Proposta Migliorativa — Beyond MVP

### 11.1 Skill Co-occurrence Graph
Invece di trattare le skill come lista piatta, costruire un grafo di co-occorrenza:
- Nodo: skill canonico
- Arco: frequenza con cui le due skill appaiono nello stesso annuncio
- Peso: probabilità condizionale P(skill_B | skill_A richiesta)

**Beneficio per AEGIS**: il gap analysis diventa molto più intelligente. Se l'utente sa Kubernetes, il grafo suggerisce che probabilmente conosce anche Docker (alta co-occurrence), riducendo il gap apparente.

### 11.2 Salary Signal da Offerte di Lavoro Strutturate
La Pay Transparency Directive EU (2023/970) entrerà in vigore entro giugno 2026 in tutti gli stati membri. Obbligherà le aziende a pubblicare salary range negli annunci. Questo trasformerà la disponibilità di dati salariali EU da ~30% a ~90% degli annunci.

**Raccomandazione**: progettare il salary aggregator con questo shift in mente, usando un modello dati che gestisce sia annunci con salary disclosure che senza.

### 11.3 Seniority Signal Implicito
Invece di affidarsi solo al titolo ("Senior"), estrarre segnali impliciti dal testo:
- Anni di esperienza richiesti: "5+ years" → Senior
- Responsabilità menzionate: "leading a team" → Lead/Principal
- Complessità sistema: "distributed systems at scale" → Senior+

**Implementazione**: rule engine + dizionario pattern → seniority score implicito da combinare col titolo.

### 11.4 Company Tier Classification
Classificare le aziende per tier (FAANG, Big Tech, Scale-up, Enterprise, SMB, Startup):
- Fonte: LinkedIn company size + Crunchbase (API pubblica limitata)
- Beneficio: salary benchmark per tier (FAANG paga 40-80% sopra mercato in EU)
- Utile per: calibrare le aspettative salariali dell'utente

### 11.5 Real-time Market Pulse (futuro)
Integrazione con LinkedIn Economic Graph Reports (pubblicati trimestralmente, dati aggregati pubblici) e con il report EU Jobs Monitor (Eurofound, semestrale) per avere un contesto macroeconomico che calibra i KPI in periodi di crisi o boom.

---

## 12. Raccomandazione Finale

### Strategia per il POC (Sprint 7–9)

**Fase A — Immediata (settimane 1–2)**
1. Integrare Adzuna API per job listings EU (gratuita, API ben documentata)
2. Integrare RemoteOK feed per dati remote-first
3. Caricare SO Developer Survey 2024 come dataset salary benchmark
4. Implementare Approccio A (dizionario + regex) per skill extraction

**Fase B — POC maturo (settimane 3–6)**
1. Aggiungere ESCO API per arricchire skill taxonomy con fonte autorevole EU
2. Implementare role classifier basato su keyword mapping
3. GitHub Archive query mensile per growth momentum su linguaggi

**Fase C — Verso produzione (post-POC)**
1. Valutare NER microservizio (Python/FastAPI) per skill extraction avanzata
2. Considerare JobsPikr come aggregatore multi-source per scalare
3. Monitorare l'implementazione Pay Transparency Directive per dati salariali EU

### Stack tecnico raccomandato per i collector

```
Hangfire Job (C#)
└── HTTP Client (Adzuna, RemoteOK)
    └── Raw Response → AdzunaJobDto / RemoteOKJobDto
        └── JobListingMapper (dto → JobListing entity)
            └── CleaningPipeline
                ├── DuplicateFilter (hash-based)
                ├── FakeJobFilter (rule-based)
                └── SalaryNormalizer (EUR conversion)
                    └── SkillExtractor (Dictionary + Regex)
                        └── RoleClassifier (keyword mapping)
                            └── Persist to PostgreSQL
                                └── Aggregate to ClickHouse
```

La separazione in step distinti nella pipeline permette di:
- Testare ogni step indipendentemente
- Sostituire un singolo step (es. SkillExtractor) senza impattare gli altri
- Monitorare la qualità a ogni stadio con metriche Prometheus

---

## 13. Open Questions

1. **Quale mercato geografico prioritizzare per il lancio?** (IT, EU-wide, o globale) — impatta quale fonte dati è più rilevante
2. **Budget disponibile per dati commerciali** (JobsPikr, Glassdoor partnership)?
3. **Frequenza aggiornamento KPI accettabile per MVP?** Giornaliero è sufficiente o serve near-real-time?
4. **La Pay Transparency Directive cambia il timing del lancio?** Aspettare il giugno 2026 per avere dati salariali EU affidabili potrebbe essere una scelta strategica

---

*Documento FA-001 — Versione 1.0 — 2026-05-13*
