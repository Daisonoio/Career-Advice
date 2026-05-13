# ADR-001 — Adozione Clean Architecture + CQRS

**Stato:** Accepted  
**Data:** 2026-05-13  

## Decisione
Strutturare il backend con Clean Architecture (4 layer: Domain, Application, Infrastructure, API) e pattern CQRS via MediatR.

## Motivazioni
- Il Domain non deve dipendere da framework o infrastruttura: le regole di business sono testabili in isolamento
- CQRS separa operazioni di lettura da scrittura, rendendo esplicite le intenzioni
- I pipeline behaviors di MediatR (validation, logging) eliminano codice trasversale dai handler

## Alternative scartate
- **Layered Architecture classica (Controller → Service → Repository)**: porta a service class monolitiche difficili da testare e da evolvere
- **Vertical Slice Architecture**: valida, ma meno familiare per team .NET tradizionali

## Conseguenze
- Più file inizialmente, ma scalabilità e testabilità superiori
- Aggiunta di un nuovo use case = aggiunta di un Command/Query senza toccare esistente
