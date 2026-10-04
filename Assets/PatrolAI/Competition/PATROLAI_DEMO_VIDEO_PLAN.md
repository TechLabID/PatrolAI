# PatrolAI Demo Video Plan

**Target:** 2:50 maximum, English narration/subtitles, no physical-hardware claim.

The Physical AI segment must show at least one continuous minute of actual application modules in action. Record a fresh run; do not present cached, mock, fallback, or prerecorded inference as `REAL`.

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

