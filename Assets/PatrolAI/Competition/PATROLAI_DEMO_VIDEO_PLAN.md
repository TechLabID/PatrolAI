# PatrolAI Demo Video Plan

## Current claim boundary — Phase 8B

Phase 8B audited **16/16 held-out cases and 24/24 original cases** using existing source/results only. Under the conservative rubric, **8/16 held-out cases contain answer-bearing cues**. On the no-cue held-out subset, **bounded Rule v2 = raw Nemotron = hybrid = 8/8 action-correct**; on conflicting no-cue cases, **all three = 2/2**. Both raw-model improvements over Rule v2 (HC09/HC11) occurred in STOP-cued contexts. **NO FAIR DEMO CANDIDATE** met the isolated-superiority criteria. This pilot does not establish AI necessity or Nemotron universal superiority. **External review: PENDING.** HC09 is a functional example, not isolated superiority proof. Historical aggregate counts remain unchanged. See [the cue/fairness audit](PATROLAI_CUE_FAIRNESS_AUDIT.md).

**Target:** 2:50 maximum, English narration/subtitles, no physical-hardware claim.

The Physical AI segment must show at least one continuous minute of actual application modules in action. Record a fresh run; do not present cached, mock, fallback, or prerecorded inference as `REAL`.

## Hero story and potential value

Make Scenario B's uncertainty-driven re-observation the main story: uncertain evidence → a validated AI decision requests re-observation → the mobile inspector moves → a second observation is collected → the workflow verifies the evidence → the actual validated final action is shown. Keep A (clear safe / continue) and C (clear danger / stop) as short supporting demonstrations.

For industrial safety teams, inspection operators, and developers/researchers, the potential value is uncertainty-aware inspection, a traceable observation → decision → action chain, and execution bounded by deterministic validation and safety logic. This is a simulated research prototype, with no production deployment, industrial validation, or safety certification claimed. Do not describe potential value as measured safety improvement, ROI, accident reduction, or deployment impact.

## Provenance and narration boundary

For every application shot, identify the environment as **UNITY SIMULATION** and preserve any **LOCAL SAMPLE** observation label. `REAL` means only successful current Nebius/Nemotron inference, not real-world sensing, hardware execution, or guaranteed safety. Narrate verification only after the workflow reports it; retain incomplete, conflicting, FAILED, or FALLBACK outcomes when that is what the run produces.

For AUTO PATROL / HC09, keep these labels readable: **LOCAL SAMPLE: HELD-OUT HC09**, **UNITY: SIMULATED**, **NEMOTRON: REAL** only after successful current inference (otherwise show the actual FAILED/FALLBACK/NOT VERIFIED status). Provider: Nebius Token Factory. Model: `nvidia/Nemotron-3_5-Lightning`. Say “held-out observation indicates” or “structured observation contains”; do not attribute HC09's scripted danger context to live CCTV, physical sensors, or industrial-camera detection. The inherited Unity/OpenCV secondary observation path is separate simulation evidence.

HC09 was selected for the demo because it is an existing frozen held-out case where Rule v2 and Nemotron produce different actions. Aggregate held-out results are reported separately. Aggregate action-correct results: bounded Rule v2 14/16; raw Nemotron 14/16; hybrid Nemotron + Safety Envelope 15/16. The envelope corrects HC16; HC04 remains incorrect. The held-out comparison is a directional pilot result and is not statistically conclusive industrial validation. External label review: PENDING. AUTO PATROL HC09 manual Unity validation PASS is owner reported; no hardware deployment is claimed.

## Shot plan

| Time | Visual | Narration/evidence |
|---|---|---|
| 0:00–0:15 | Uncertain inspection view in the industrial simulation and PatrolAI title | "Industrial inspection is risky not only when a hazard is missed, but when a system acts confidently on uncertain evidence." |
| 0:15–0:30 | Compact workflow overlay | Show how observation leads to a validated action; name Nebius Token Factory and NVIDIA Nemotron. Introduce the alternative to unjustified confidence: inspect again. |
| 0:30–1:50 | Scenario B live, one continuous take | Show `INDICATOR_UNCLEAR / INSUFFICIENT`, the current request and truthful runtime label, then `UNKNOWN → REOBSERVE → REOBSERVING`. Follow the mobile inspector's movement, second observation, and actual verification status. Finish on the second decision and bounded final action/state actually produced. Keep provider, exact model, parser/validator outcome, and evidence readable. |
| 1:50–2:05 | Scenario A live, supporting example | Clear safe evidence: `NORMAL / SUFFICIENT`, successful current inference labeled `REAL`, `SAFE → CONTINUE → CONTINUING`; no inspection. |
| 2:05–2:20 | Scenario C live, supporting example | Clear danger evidence: `INDICATOR_RED / SUFFICIENT`, successful current inference labeled `REAL`, `DANGER → STOP → STOPPED`; no inspection. |
| 2:20–2:38 | Significant updates and potential value | Separate inherited environment/perception/movement from new Nebius, Nemotron, orchestration, scenarios, and evidence UI. Emphasize traceable decisions and bounded re-observation as potential workflow value, without measured deployment-impact claims. |
| 2:38–2:50 | Closing, with prototype disclosure and repository title | "PatrolAI turns AI reasoning into bounded, observable physical-agent behavior, not just an answer on a screen." Keep **UNITY SIMULATION**, research prototype, no hardware, and no safety certification visible. |

The interval 0:30–2:20 allocates 110 seconds to application modules in action. Re-observation receives the largest share: 80 seconds, versus 15 seconds each for A and C. Preserve at least one continuous minute within the hero take; do not splice different runs or skip movement, collection, verification, or the terminal result to imply a completed loop. The timing is a capture target, not a measured runtime; adjust narration/supporting shots within the 2:50 cap if necessary without fabricating completion.

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
- Hero take reviewed for the complete observation → decision → movement → second observation → verification → final-action sequence; any failure retains its actual label and outcome.
- Recording remains under three minutes and is reviewed once for secrets and truthful labels before upload.

