# PatrolAI Significant Hackathon Updates

This record separates inherited VisionGuardAI capabilities from work created for the Nebius × NVIDIA Global AI Hackathon. It prevents existing behavior from being presented as new.

## Protected inherited baseline

The tag `v0.1-patrolai-baseline` already contained:

- the RF90 industrial environment;
- OpenCV perception;
- mobile-inspector movement and inspection poses;
- active agentic re-observation;
- multi-source fusion and temporal workflow logic;
- the established runtime and status overlay.

Those systems remain baseline dependencies. PatrolAI's adapters call their public entry points; they do not reproduce or relabel the underlying algorithms.

## Hackathon-period additions

| Milestone | New PatrolAI work | Evidence boundary |
|---|---|---|
| `v0.2-patrolai-phase1-complete` | Independent product identity, competition architecture, structured observation and decision contracts | Documentation/contracts; no live claim |
| `v0.3-patrolai-phase2-complete` | Environment-based Nebius client and connectivity layer | Authentication/connectivity validated separately |
| `v0.4-patrolai-phase2.5-complete` | Nemotron parser, validator, offline mock, and safe `UNKNOWN / REOBSERVE` fallback | Offline tests are `MOCK`, never `REAL` |
| `v0.5-patrolai-phase3-live-nemotron-pass` | Strict JSON Schema live reasoning through `nvidia/Nemotron-3_5-Lightning`; model thinking disabled for the short decision task | HTTP 200, `finish_reason=stop`, parser PASS, validator PASS, valid `PatrolDecision` |
| `v0.6-20261004-patrolai-phase4-physical-ai-pass` | Decision-to-action closed loop and evidence-rich result/state contract | Targeted tests plus live `DANGER → STOP → STOPPED` validation |
| `v0.7-20261004-patrolai-phase5-demo-pass` | Deterministic SAFE, REOBSERVE, and HAZARD scenarios and competition demo UI | Manual Unity validation recorded by the project owner |
| `v0.7.1-20261004-patrolai-phase5c-workflow-ui-pass` | Context-sensitive baseline workflow details, truthful running/completion/failure reporting | Adapter tests and manual Unity validation |
| `v0.7.2-20261004-patrolai-phase5d-ui-polish-pass` | Reference-led competition UI polish without controller changes | Compile/test evidence and manual visual validation |

## New closed-loop behavior

PatrolAI adds this bounded competition path:

```text
Structured observation
  → real Nebius Token Factory request
  → NVIDIA Nemotron structured safety decision
  → strict parser and policy validator
  → PatrolDecision
  → CONTINUE / REOBSERVE / STOP
  → truthful final agent state
```

Scenario B integrates with the inherited `VGPhase3AgentController.RunAgenticInspection()` entry point only after a validated `REOBSERVE` decision. Scenarios A and C do not invoke agentic inspection.

## Validation summary

- Nemotron parser/validator offline tests: 12/12 PASS.
- Phase 4 closed-loop targeted tests: 10/10 PASS.
- Phase 5 integrated targeted tests: 22/22 PASS.
- Live Nemotron validation: HTTP 200, response JSON emitted, parser PASS, validator PASS.
- Live hazard closed loop: `REAL`, `DANGER / STOP`, final state `STOPPED`.
- Phase 5 scenarios and UI: manually validated in Unity by the project owner.

These are engineering validation observations, not safety certification, production deployment, or physical-hardware evidence.

