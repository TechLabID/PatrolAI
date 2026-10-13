# PatrolAI Demo Video Plan

## Current claim boundary — Phase 8B

Phase 8B audited **16/16 held-out cases and 24/24 original cases** using existing source/results only. Under the conservative rubric, **8/16 held-out cases contain answer-bearing cues**. On the no-cue held-out subset, **bounded Rule v2 = raw Nemotron = hybrid = 8/8 action-correct**; on conflicting no-cue cases, **all three = 2/2**. Both raw-model improvements over Rule v2 (HC09/HC11) occurred in STOP-cued contexts. **NO FAIR DEMO CANDIDATE** met the isolated-superiority criteria. This pilot does not establish AI necessity or Nemotron universal superiority. **External review: PENDING.** HC09 is a functional example, not isolated superiority proof. Historical aggregate counts remain unchanged. See [the cue/fairness audit](PATROLAI_CUE_FAIRNESS_AUDIT.md).

**Target:** 2:50 maximum, English narration/subtitles, no physical-hardware claim.

The Physical AI segment must show at least one continuous minute of actual application modules in action. Record a fresh run; do not present cached, mock, fallback, or prerecorded inference as `REAL`.

## Provenance and narration boundary

For AUTO PATROL / HC09, keep these labels readable: **LOCAL SAMPLE: HELD-OUT HC09**, **UNITY: SIMULATED**, **NEMOTRON: REAL** only after successful current inference (otherwise show the actual FAILED/FALLBACK/NOT VERIFIED status). Provider: Nebius Token Factory. Model: `nvidia/Nemotron-3_5-Lightning`. Say “held-out observation indicates” or “structured observation contains”; do not attribute HC09's scripted danger context to live CCTV, physical sensors, or industrial-camera detection. The inherited Unity/OpenCV secondary observation path is separate simulation evidence.

HC09 was selected for the demo because it is an existing frozen held-out case where Rule v2 and Nemotron produce different actions. Aggregate held-out results are reported separately. Aggregate action-correct results: bounded Rule v2 14/16; raw Nemotron 14/16; hybrid Nemotron + Safety Envelope 15/16. The envelope corrects HC16; HC04 remains incorrect. The held-out comparison is a directional pilot result and is not statistically conclusive industrial validation. External label review: PENDING. AUTO PATROL HC09 manual Unity validation PASS is owner reported; no hardware deployment is claimed.

## Shot plan

| Time | Visual | Narration/evidence |
|---|---|---|
| 0:00–0:15 | Industrial room and PatrolAI title | State the safety-patrol problem and the need to act safely on both hazards and uncertainty. |
| 0:15–0:30 | Compact architecture graphic or overlay | `Sense → Perceive → Reason → Decide → Act`; name Nebius Token Factory and NVIDIA Nemotron. |
| 0:30–0:55 | Scenario A live | Show `NORMAL / SUFFICIENT`, current request, `RUNTIME: REAL`, `SAFE → CONTINUE → CONTINUING`; no inspection. |
| 0:55–1:35 | Scenario B live | Show `INDICATOR_UNCLEAR / INSUFFICIENT`, `UNKNOWN → REOBSERVE → REOBSERVING`, then the inherited mobile inspector performs active re-observation and completes truthfully. |
| 1:35–2:00 | Scenario C live | Show `INDICATOR_RED / SUFFICIENT`, `RUNTIME: REAL`, `DANGER → STOP → STOPPED`; no inspection. |
| 2:00–2:20 | Evidence panel close-up | Show provider, exact model, HTTP success, validated decision, final state, and latency. Explain parser/validator/safe fallback. |
| 2:20–2:38 | Significant-updates comparison | Separate inherited environment/perception/movement from new Nebius, Nemotron, orchestration, scenarios, and evidence UI. |
| 2:38–2:50 | Closing | Physical AI track, simulated prototype/no hardware, repository title, concise impact statement. |

The interval 0:30–2:00 provides 90 seconds of application modules in action.

## Capture rules

- Use the competition scene and a stable 16:9 capture.
- Before recording, close terminals, environment-variable dialogs, archive viewers, and any screen containing credentials or private paths.
- Never show `NEBIUS_API_KEY`, an Authorization header, account identifiers, or the local diagnostic ZIP contents.
- Keep the runtime label, exact model, observation, decision, action, final state, and latency readable.
- If a request fails, show the truthful `FAILED`/`FALLBACK` state or restart the recording; never edit fallback into a `REAL` run.
- Do not use copyrighted music, unlicensed logos, or raw reusable third-party assets.
- Add English subtitles if narration is not English.

## Pre-record gate

- Public-source license selected and present.
- Local secret archive removed or sanitized; fresh working-tree and history secret scan passes.
- Media/asset provenance review completed.
- Reproducible Windows build or clearly documented Unity run path validated.
- Nebius account/model availability checked immediately before capture.
- Scenarios A, B, and C manually smoke-tested in the same environment.
- Recording remains under three minutes and is reviewed once for secrets and truthful labels before upload.

