# PatrolAI Significant Hackathon Updates

## Current claim boundary — Phase 8B

Phase 8B audited **16/16 held-out cases and 24/24 original cases** using existing source/results only. Under the conservative rubric, **8/16 held-out cases contain answer-bearing cues**. On the no-cue held-out subset, **bounded Rule v2 = raw Nemotron = hybrid = 8/8 action-correct**; on conflicting no-cue cases, **all three = 2/2**. Both raw-model improvements over Rule v2 (HC09/HC11) occurred in STOP-cued contexts. **NO FAIR DEMO CANDIDATE** met the isolated-superiority criteria. This pilot does not establish AI necessity or Nemotron universal superiority. **External review: PENDING.** HC09 is a functional example, not isolated superiority proof. Historical aggregate counts remain unchanged. See [the cue/fairness audit](PATROLAI_CUE_FAIRNESS_AUDIT.md).

This record separates inherited VisionGuardAI capabilities from work created for the Nebius × NVIDIA Global AI Hackathon. It prevents existing behavior from being presented as new.

## Protected inherited baseline

The tag `v0.1-patrolai-baseline` already contained:

- the industrial environment;
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

## Phase 7 — AI necessity evidence (2026-10-05)

New work adds a bounded rule baseline, 24 fixed-label synthetic cases, REAL Token Factory/Nemotron evaluation and metrics, and a second reasoning call after inherited agentic re-observation. Measured exact accuracy: rules 18/24 (75%), Nemotron 16/24 (66.67%). Conflicting cases: rules 0/6, Nemotron 3/6. Missed required hazard STOPs: rules 2/8, Nemotron 0/8; neither produced an inappropriate CONTINUE. All 24 calls were REAL, with zero invalid output, fallback or failed calls; mean/median latency 719.75/645 ms. Uncertainty risk labels remain inconsistent. Historical technical classification: MODERATE; Phase 8B supersedes any AI-necessity interpretation. AI necessity is not established.

Offline validation and source compilation PASS. Phase 7/7B manual Unity validation was subsequently owner-reported PASS. AUTO PATROL HC09 manual Unity validation is owner-reported PASS in Phase 8; these approvals concern simulation, not hardware. Current 16-case pilot: bounded Rule v2 14/16 action-correct; raw Nemotron 14/16; Nemotron + Safety Envelope 15/16. HC16 is corrected by the envelope; HC04 remains a missed required STOP. External label review PENDING. [Full methodology, metrics, failure analysis and limitations](PATROLAI_AI_NECESSITY_EVALUATION.md).

## Validation summary

- Nemotron parser/validator offline tests: 12/12 PASS.
- Phase 4 closed-loop targeted tests: 10/10 PASS.
- Phase 5 integrated targeted tests: 22/22 PASS.
- Live Nemotron validation: HTTP 200, response JSON emitted, parser PASS, validator PASS.
- Live hazard closed loop: `REAL`, `DANGER / STOP`, final state `STOPPED`.
- Phase 5 scenarios and UI: manually validated in Unity by the project owner.

These are engineering validation observations, not safety certification, production deployment, or physical-hardware evidence.
