# ADR-002 — Separazione LLM da KPI Computation

**Stato:** Accepted  
**Data:** 2026-05-13  

## Decisione
Gli LLM (Claude) non generano mai KPI numerici. Il loro ruolo è esclusivamente narrativo: spiegare, sintetizzare, correlare dati già calcolati deterministicamente.

## Motivazioni
- Un KPI generato da LLM è non-verificabile e soggetto ad hallucination
- La credibilità della piattaforma dipende da dati tracciabili e riproducibili
- Separazione permette di sostituire il modello LLM senza impattare la logica di business

## Implementazione
- `KpiComputationService`: solo matematica su dati aggregati, zero LLM
- `IAIOrchestrator`: riceve KPI già calcolati come parametri, produce solo testo
- `SeniorityScorer`: pesi e soglie deterministici, nessuna chiamata AI

## Regola operativa
Prima di ogni chiamata LLM verificare: "Sto passando dati già calcolati, o sto chiedendo al LLM di inventarli?"
