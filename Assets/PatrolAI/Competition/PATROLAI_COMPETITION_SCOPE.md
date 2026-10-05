# PatrolAI Competition Scope

PatrolAI is developed for the Nebius × NVIDIA Global AI Hackathon under the descriptor **Autonomous Physical AI for Industrial Safety** and targets the **Physical AI** track.

## Existing / Inherited Baseline

The following capabilities predate the PatrolAI competition-development phases and are inherited from the protected `v0.1-patrolai-baseline` foundation:

- industrial environment;
- OpenCV perception;
- mobile inspector;
- active re-observation;
- existing runtime logic;
- existing validated UI.

These capabilities are protected and must not be presented as new hackathon-period work.

## New Nebius × NVIDIA Competition Development

The following implemented items are competition-specific additions and are tracked separately from the inherited baseline:

- PatrolAI competition identity;
- Nebius Token Factory runtime integration;
- NVIDIA Nemotron reasoning;
- structured observation contract;
- structured safety decision contract;
- Physical AI closed-loop behavior;
- competition scenarios;
- competition-specific UI;
- competition demo workflow.

The implemented competition path has validated real Nebius/NVIDIA inference, strict parsing and policy validation, safe fallback, a bounded Physical AI decision-to-action loop, deterministic A/B/C scenarios, evidence UI, and Windows standalone execution. Detailed milestone separation is recorded in `PATROLAI_SIGNIFICANT_UPDATES.md`.
