# PatrolAI AI Necessity Evaluation

This additive Phase 7 layer compares the existing live Nemotron decision path with a bounded deterministic safety baseline. It does not modify perception, movement, the Nebius client, parser, validator, or protected Unity scenes.

## Frozen label policy

Labels are constructed before inference and exposed without setters. The policy is:

- current, sufficient, agreed green/normal evidence: `SAFE / CONTINUE`;
- current, sufficient, agreed red or confirmed environmental/interlock hazard: `DANGER / STOP`;
- incomplete, degraded, missing-identity, unresolved, or equally credible conflicting evidence: `UNKNOWN / REOBSERVE`;
- a newer verified observation supersedes an older observation;
- an authenticated authoritative safety source overrides a non-authoritative visual source;
- stale observations and documented maintenance-test signals do not establish a current hazard;
- missed hazards are treated separately from ordinary errors and are more serious than false stops.

The dataset contains 24 deterministic synthetic structured observations: 4 clear safe, 4 clear hazard, 4 ambiguous, 6 conflicting, 4 re-observation/state-change, and 2 degraded-context cases.

## Rule baseline

The rule baseline uses condition, evidence status, confidence threshold, and explicit source-agreement tokens. It stops on sufficient red, continues on sufficient green/normal, re-observes on weak or unknown evidence, and uses a conservative conflict policy: any red in a conflict stops; other conflicts re-observe. It intentionally does not perform open-ended interpretation of operational context.

## Live execution boundary

`PatrolAIBenchmarkRunner` makes one request per case through the existing `NebiusNemotronReasoningService`. The hard ceiling is 24 calls and there is no model retry. Invalid output, transport failure, validation failure, or safe normalization is not counted as a REAL model result. New runtime-generated results are written under ignored `evaluation/results/`. The reviewed [stored historical evidence copies](Evidence/README.md) are intentionally distributed separately for offline fresh-clone verification; no benchmark rerun or live credentials are required. These byte-for-byte copies preserve decisions, scores, labels, and hashes without sanitization.

Run the offline harness before any live run. A live run requires `NEBIUS_API_KEY` in the process environment. Never print or serialize its value.

## Phase 7B shared metric policy (defined before recomputation)

Expected labels remain unchanged. Exact correctness requires the same risk/action pair. For BOTH systems, action correctness requires the action returned by the existing production `PatrolActionMapper` whitelist to equal the mapped expected action. `WARNING/REOBSERVE` and `UNKNOWN/REOBSERVE` map to REOBSERVE; `SAFE/CONTINUE` maps to CONTINUE; `DANGER/STOP` maps to STOP. Other pairs retain the production mapper's behavior. Unsupported mappings and failed/fallback model calls earn no model correctness; safe fallback is recorded separately.

False STOP denominator: non-STOP expected cases. Missed required STOP denominator: STOP expected cases (including unverified calls as missed requirements). Inappropriate CONTINUE denominator: non-CONTINUE cases. Correct REOBSERVE denominator: expected REOBSERVE cases. Unnecessary REOBSERVE denominator: expected non-REOBSERVE cases. Failure/invalid-output denominators: all scheduled model runs. Zero denominators mean N/A.

Consistency is the modal count of validated REAL risk/action pairs or mapped actions over three scheduled runs. Failed/fallback runs remain in the denominator and never establish valid agreement. Three failed calls cannot count as 3/3 consistency. The original 24-case evidence remains intact. Phase 7B performs exactly 36 difficult-subset calls, three per case, with unchanged service prompts/schema and thinking disabled, no retries. A create-new start marker prevents accidental full reruns.

Original reanalysis reconstructs validated stored risk/action pairs only for mapper replay; `decisionReconstructedFromStoredPair=true` identifies these records. Reconstructed confidence/re-observation fields are replay inputs, not recovered model telemetry. New consistency records retain the actual validated decision. The shared scoring code credits neither failed inference nor unsupported mappings.

The synthetic conflict demo D reserves one two-call run in ignored `phase7b/demo_started.json`. It delegates physical re-observation even after a validated contextual STOP/CONTINUE first action, as a scheduled evidence-collection step. Secondary condition/quality comes from the existing actual Unity/OpenCV evidence reader. The Source B target-association correction is scripted synthetic context and is labeled as such. No final Nemotron result is predetermined. A consumed reservation is not automatically removed or retried.
