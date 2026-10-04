# PatrolAI Phase 4 — Physical AI Closed Loop

## Objective

Phase 4 proves the additive contract-level loop:

`PatrolObservation → Nemotron reasoning → PatrolDecision → PatrolAction → simulated Physical AI state`

It does not modify or directly drive inherited Unity movement, OpenCV, scenes, controllers, or UI.

## Runtime truthfulness

- `REAL`: the existing Nebius Token Factory / NVIDIA Nemotron live path returned an HTTP-successful, parsed, validated, non-normalized decision.
- `MOCK`: `MockNemotronReasoningService` produced an offline deterministic decision.
- `LOCAL SAMPLE`: another non-live reasoning service produced a decision.
- `FALLBACK`: live reasoning was not started and a safe fallback was selected.
- `FAILED`: a live request, parsing, validation, or controller operation failed.

Mock and fallback results must never be presented as REAL.

## Decision-to-action mapping

| Validated decision | PatrolAction | Simulated final state |
|---|---|---|
| `SAFE / CONTINUE` | `CONTINUE` | `CONTINUING` |
| `WARNING / INSPECT` | `INSPECT` | `INSPECTING` |
| `WARNING / REOBSERVE` | `REOBSERVE` | `REOBSERVING` |
| `DANGER / STOP` | `STOP` | `STOPPED` |
| `DANGER / ALERT` | `ALERT` | `ALERTING` |
| `UNKNOWN / REOBSERVE` | `REOBSERVE` | `REOBSERVING` |

Missing or unsupported combinations map safely to `REOBSERVE` and are explicitly marked as fallback use.

## Current limitation

Action execution is a deterministic simulated state transition. It does not yet command the inherited mobile inspector or any physical hardware.

## Future Unity adapter

A later explicitly authorized phase may consume `PatrolAction` through a small Unity-side adapter. That adapter must preserve the validated movement controller and must not duplicate reasoning or safety validation.

## Evidence

`PatrolClosedLoopResult` records the observation ID, model ID, runtime status, decision, action, final state, state path, HTTP/parser/validator status, fallback use, success, message, and elapsed time. Offline tests remain MOCK; competition runtime evidence requires a successful REAL request.
