# PatrolAI Evaluation — Measured Results and Limitations

## Current claim boundary — Phase 8B

Phase 8B audited **16/16 held-out cases and 24/24 original cases** using existing source/results only. Under the conservative rubric, **8/16 held-out cases contain answer-bearing cues**. On the no-cue held-out subset, **bounded Rule v2 = raw Nemotron = hybrid = 8/8 action-correct**; on conflicting no-cue cases, **all three = 2/2**. Both raw-model improvements over Rule v2 (HC09/HC11) occurred in STOP-cued contexts. **NO FAIR DEMO CANDIDATE** met the isolated-superiority criteria. This pilot does not establish AI necessity or Nemotron universal superiority. **External review: PENDING.** HC09 is a functional example, not isolated superiority proof. Historical aggregate counts remain unchanged. See [the cue/fairness audit](PATROLAI_CUE_FAIRNESS_AUDIT.md).

Status: Phase 7/7B manual validation PASS (owner reported); AUTO PATROL HC09 manual Unity validation PASS (owner reported in Phase 8). External label review PENDING. Conflict Demo V2 has a separate approval boundary.
Original measured run: 2026-10-05, 24 REAL requests, no retries. Phase 7 manual validation was subsequently reported PASS by the owner; Phase 7B adds the comparison and consistency evidence below.

## Submission summary — Phase 8

| Configuration | Action-correct / 16 | Scope |
|---|---:|---|
| Rule v2 | 14/16 (87.50%) | Existing bounded Rule v2, including deterministic budget/safety rules |
| Raw Nemotron | 14/16 (87.50%) | Validated model action before deterministic safety-envelope overrides |
| Nemotron + Safety Envelope | 15/16 (93.75%) | Hybrid operational action after deterministic safety rules |

Raw Nemotron missed required STOPs on HC04 and HC16. The safety envelope corrected HC16 from REOBSERVE to STOP; **HC04 remained WARNING/REOBSERVE and incorrect**, so the hybrid still missed one required STOP. The 15/16 result is not raw model performance. Rule v2's 14/16 is its public bounded configuration; the pre-budget v2 proposals were 12/16 and are retained separately below.

On the five conflicting cases, Rule v2 was 3/5 action-correct; raw Nemotron and the hybrid were 5/5. Inappropriate CONTINUE occurred in Rule v2 2/13 expected non-CONTINUE cases, versus raw Nemotron and hybrid 0/13. Raw Nemotron and Rule v2 tie overall action correctness; the hybrid's one-case gain is a deterministic correction. Exact risk/action-pair correctness is a different measure and must not be substituted for action correctness.

The held-out comparison is a directional pilot result and is not statistically conclusive industrial validation.

Rules for clear cases. Nemotron for contextual cases. Deterministic safety envelope for bounded execution. Nemotron provides contextual reasoning; deterministic validation and safety rules bound execution. Model output cannot control the system unconstrained. Invalid decisions and unresolved evidence at the re-observation limit escalate to STOP / HUMAN REVIEW; while budget remains, unresolved evidence can request re-observation. The envelope does not guarantee hazard detection.

### AUTO PATROL / HC09 provenance and selection

- **Observation Source:** LOCAL SAMPLE — HELD-OUT HC09 (frozen, internally authored synthetic/structured data).
- **Environment:** UNITY SIMULATION; movement and actions are simulated.
- **AI Runtime:** REAL — Nebius Token Factory, only after a successful current parsed/validated inference; otherwise FAILED/FALLBACK/NOT VERIFIED as applicable.
- **Model:** `nvidia/Nemotron-3_5-Lightning`.
- Stored HC09 result: Rule v2 SAFE/CONTINUE; raw Nemotron DANGER/STOP; proposed expected DANGER/STOP.
- AUTO PATROL manual Unity validation: **PASS — project owner reported in the Phase 8 request**. This is simulation approval, not hardware approval. New Phase 8 provenance labels have offline validation, not separate visual approval.

HC09 was selected for the demo because it is an existing frozen held-out case where Rule v2 and Nemotron produce different actions. Aggregate held-out results are reported separately.

HC09 is not asserted representative of every case. Its personnel-danger context comes from the held-out observation, not a live CCTV or industrial-camera detection. Optional secondary evidence is collected through the existing Unity/OpenCV re-observation path and does not turn the original HC09 context into a physical sensor measurement.

**EXTERNAL LABEL REVIEW: PENDING.** Proposed labels were frozen before inference, but independent domain approval has not been obtained. Use the [15-minute review packet](PATROLAI_EXTERNAL_REVIEW_PACKET.md), and [safe submission claims](PATROLAI_SAFE_SUBMISSION_CLAIMS.md). Reviewer corrections must be recorded separately; historical scores and frozen labels must not be overwritten.

## Historical evaluation details

The following sections retain the original Phase 7/7B measurements and the Phase 7C pilot. Their different case counts and configurations are not pooled. Internal evidence classifications below are technical notes, not submission headline claims.

## Question and methodology

Why use NVIDIA Nemotron rather than a simple rule table? Compare a fixed safety-first rule table with the existing real Nebius reasoning service on 24 deterministic synthetic structured observations. Both receive the same seven substantive observation fields, including context; Nemotron serialization omits timestamp. Ground-truth risk/action pairs were defined before inference by the safety policy in Evaluation/README_AI_NECESSITY.md and recorded before the first request. No labels were revised after evaluation.

Provider: Nebius Token Factory. Model: nvidia/Nemotron-3_5-Lightning. enable_thinking=false; temperature=0.1; maximum completion tokens=512. Existing schema, parser and validator are reused. Exactly one inference per case; no retry or full rerun. Generated machine-readable evidence remains local and ignored.

The baseline stops on sufficient red, continues on sufficient green/normal, re-observes on weak/degraded/unknown evidence, and treats red-containing conflicts conservatively as STOP; other conflicts require REOBSERVE. Confidence threshold: 0.75. This is a bounded rule table, not a semantic context interpreter. More extensive deterministic rules could address these cases; this experiment does not prove that an LLM is the only possible solution.

## Metrics

Exact decision accuracy requires BOTH risk and action to match the frozen label. Action-only accuracy is a separate descriptive calculation; it does not replace the exact score.

| Metric | Rule baseline | Nemotron |
|---|---:|---:|
| Exact disposition / overall correctness | 18/24 (75%) | 16/24 (66.67%) |
| Action-only correctness | 18/24 (75%) | 23/24 (95.83%) |
| False STOP / non-STOP cases | 4/16 (25%) | 0/16 (0%) |
| Missed required STOP / hazard cases | 2/8 (25%) | 0/8 (0%) |
| Inappropriate CONTINUE / non-CONTINUE cases | 0/17 (0%) | 0/17 (0%) |
| REOBSERVE action correctness | 7/9 (77.78%) | 9/9 (100%) |
| Invalid structured output | N/A | 0/24 (0%) |
| Fallback | N/A | 0/24 (0%) |
| Failed calls | N/A | 0/24 (0%) |
| Average inference latency | not measured | 719.75 ms |
| Median inference latency | not measured | 645 ms |

Missed required STOP counts any non-STOP on a hazard label. The two rule misses were REOBSERVE, not unsafe CONTINUE. REOBSERVE still pauses for further evidence, but fails the scenario's required immediate STOP disposition. Denominators are explicit; zero-denominator category rates should be read as N/A.

## Category comparison

| Category | Cases | Rule exact | Nemotron exact | Nemotron action |
|---|---:|---:|---:|---:|
| Clear safe | 4 | 4/4 | 4/4 | 4/4 |
| Clear hazard | 4 | 4/4 | 4/4 | 4/4 |
| Ambiguous | 4 | 4/4 | 1/4 | 4/4 |
| Conflicting multi-source evidence | 6 | 0/6 | 3/6 | 5/6 |
| Re-observation/state change | 4 | 4/4 | 4/4 | 4/4 |
| Degraded/missing context | 2 | 2/2 | 0/2 | 2/2 |

Rules are sufficient on clear observations and all four benchmark state-change examples. Nemotron showed contextual benefits for authoritative interlocks, environmental hazards, and documented maintenance signals. Its exact score is worse overall because of risk-label inconsistency.

## Per-case disagreement and failure analysis

| Cases | Frozen label | Rule result | Nemotron result | Exact classification |
|---|---|---|---|---|
| C02, C03, C04, F01, F02 | UNKNOWN/REOBSERVE | UNKNOWN/REOBSERVE | WARNING/REOBSERVE | Rule correct / Nemotron wrong |
| D01, D06 | UNKNOWN/REOBSERVE | DANGER/STOP | WARNING/REOBSERVE | Both wrong |
| D02 | SAFE/CONTINUE | DANGER/STOP | WARNING/REOBSERVE | Both wrong |
| D03, D05 | DANGER/STOP | UNKNOWN/REOBSERVE | DANGER/STOP | Nemotron correct / Rule wrong |
| D04 | SAFE/CONTINUE | DANGER/STOP | SAFE/CONTINUE | Nemotron correct / Rule wrong |

All other 13 cases are both correct. There are 11 exact disagreements: 5 rule-correct/model-wrong, 3 model-correct/rule-wrong, and 3 both-wrong. No alternate-risk label is retroactively accepted as exact ground truth.

Rule failures: four false stops on unresolved conflict, stale evidence or maintenance-test context (D01, D02, D04, D06); two missed immediate hazard stops when a green visual indicator conflicted with authoritative interlock/gas evidence (D03, D05).

Nemotron failures: seven WARNING-versus-UNKNOWN risk-label mismatches despite correct REOBSERVE actions (C02–C04, D01, D06, F01–F02); one unnecessary REOBSERVE rather than CONTINUE when the red camera frame was stale and the current PLC source authoritative (D02). No false STOP, unsafe CONTINUE, invalid JSON, fallback, or failed call occurred in this run.

## Decision-changing demo implementation

Scenario B now continues through inherited agentic inspection, actual secondary evidence and verification, an updated PatrolObservation, a SECOND Nemotron call, and the existing decision-to-PatrolAction/state mapper. The result is not hard-coded. UNKNOWN/REOBSERVE or WARNING/REOBSERVE can trigger inspection; the loop is bounded to two calls and never silently reruns. Missing, stale, conflicting or failed re-observation is surfaced safely.

The evidence reader binds to the same inspection session using a read-only compatibility bridge to the inherited private session identifier. It checks freshness and uses the actual persisted secondary evidence and verification output. Baseline algorithms remain unchanged. The initial demo observation is deliberately synthetic and insufficient; physical movement and second observation occur in the Unity simulation. REAL describes cloud inference, not physical hardware.

The small existing evidence-panel addition shows first decision, re-observation status and second decision. Local demo JSON records both observations, both model decisions, latency, action and state. STOP/CONTINUE remain the existing simulated PatrolAction/state behavior, not a claim of actuator hardware.

## Validation and remaining approval

- Evaluation harness: 10/10 PASS, including immutable labels, safety metric calculations and serialization.
- Existing Nemotron parser/validator regression: 12/12 PASS.
- Minimal Physical AI regression: 10/10 PASS.
- Scenario/evidence regression: 23/23 PASS.
- Re-observation reader and failure evidence: 6/6 PASS offline.
- Source compilation against actual Unity references: PASS (no Editor invocation).
- Secret-value check: PASS; generated evidence ignored, no staging/publishing.
- Original Phase 7 manual Unity visual validation: PASS (owner reported in the Phase 7B request).

The original Scenario B visual approval has been reported by the owner. UNKNOWN→UNKNOWN remains a truthful possible result. The new Phase 7B Scenario D has a separate pending visual approval requirement described below.

## Phase 7B comparative hardening — 2026-10-07

Phase 7 and Phase 7B manual Unity validation PASS were reported by the owner. The new Phase 7C Conflict Demo V2 requires separate approval.

### Shared action definition

The policy was documented before recomputation. BOTH systems use the existing production PatrolActionMapper whitelist for their decision and the expected disposition. SAFE/CONTINUE maps to CONTINUE, DANGER/STOP to STOP, and both UNKNOWN/REOBSERVE and WARNING/REOBSERVE to REOBSERVE. Exact risk/action labels remain unchanged. Failed inference, fallback and unsupported mappings earn no model correctness. They remain in scheduled-run denominators.

The original validated risk/action pairs are replayed through the mapper; reconstructed confidence/re-observation fields are marked as replay inputs, not recovered model telemetry. The original raw 24-case artifact is preserved. No original benchmark inference was rerun.

### Side-by-side safety metrics: original 24 cases

| Metric | Rule-Based | Nemotron |
|---|---:|---:|
| Exact decision accuracy | 18/24 (75%) | 16/24 (66.67%) |
| Action correctness | 18/24 (75%) | 23/24 (95.83%) |
| False STOP | 4/16 (25%) | 0/16 (0%) |
| Missed required hazard STOP | 2/8 (25%) | 0/8 (0%) |
| Inappropriate CONTINUE | 0/17 (0%) | 0/17 (0%) |
| Correct REOBSERVE | 7/9 (77.78%) | 9/9 (100%) |
| Unnecessary REOBSERVE | 2/15 (13.33%) | 1/15 (6.67%) |
| Invalid output | N/A | 0/24 (0%) |
| Failed runtime | N/A | 0/24 (0%) |
| Fallback | N/A | 0/24 (0%) |

False STOP is divided by expected non-STOP cases; missed STOP by expected STOP cases; inappropriate CONTINUE by non-CONTINUE cases; correct REOBSERVE by expected REOBSERVE cases; unnecessary REOBSERVE by non-REOBSERVE cases. These denominator definitions are identical for both systems. Category denominators of zero are N/A. Rules have no external runtime, so runtime/JSON metrics are N/A. The rule missed STOPs are pauses for REOBSERVE, not unsafe CONTINUE.

### Category exact and action comparison

| Category | Rule exact | Nemotron exact | Rule action | Nemotron action |
|---|---:|---:|---:|---:|
| Safe | 4/4 | 4/4 | 4/4 | 4/4 |
| Hazard | 4/4 | 4/4 | 4/4 | 4/4 |
| Ambiguous | 4/4 | 1/4 | 4/4 | 4/4 |
| Conflicting | 0/6 | 3/6 | 0/6 | 5/6 |
| Re-observation | 4/4 | 4/4 | 4/4 | 4/4 |
| Degraded | 2/2 | 0/2 | 2/2 | 2/2 |

Rules remain strong for clear deterministic cases and match Nemotron's actions in ambiguous/degraded cases. The measured action advantage is concentrated in conflicting context.

### Exact-versus-action audit

All seven risk-label mismatches have expected UNKNOWN/REOBSERVE and model WARNING/REOBSERVE. The production mapper returns REOBSERVE for both.

| Case | Category | Expected decision | Model decision | Expected/model action | Exact correct | Action correct | Safety impact and explanation |
|---|---|---|---|---|---|---|---|
| C02 | Ambiguous | UNKNOWN/REOBSERVE | WARNING/REOBSERVE | REOBSERVE/REOBSERVE | NO | YES | Pauses correctly; weak red evidence labeled WARNING |
| C03 | Ambiguous | UNKNOWN/REOBSERVE | WARNING/REOBSERVE | REOBSERVE/REOBSERVE | NO | YES | Pauses correctly; unclassified indicator labeled WARNING |
| C04 | Ambiguous | UNKNOWN/REOBSERVE | WARNING/REOBSERVE | REOBSERVE/REOBSERVE | NO | YES | Pauses correctly; occlusion labeled WARNING |
| D01 | Conflicting | UNKNOWN/REOBSERVE | WARNING/REOBSERVE | REOBSERVE/REOBSERVE | NO | YES | Pauses correctly; competing current sources labeled WARNING |
| D06 | Conflicting | UNKNOWN/REOBSERVE | WARNING/REOBSERVE | REOBSERVE/REOBSERVE | NO | YES | Pauses correctly; unresolved authority labeled WARNING |
| F01 | Degraded | UNKNOWN/REOBSERVE | WARNING/REOBSERVE | REOBSERVE/REOBSERVE | NO | YES | Pauses correctly; health/timestamp uncertainty labeled WARNING |
| F02 | Degraded | UNKNOWN/REOBSERVE | WARNING/REOBSERVE | REOBSERVE/REOBSERVE | NO | YES | Pauses correctly; missing target identity labeled WARNING |
| D02 | Conflicting | SAFE/CONTINUE | WARNING/REOBSERVE | CONTINUE/REOBSERVE | NO | NO | Unnecessary pause despite stale red frame and authoritative current green PLC |

There is no favorable re-labeling: the seven cases remain exact errors. D02 remains an action error.

### Difficult-subset consistency: 36 new REAL calls

Twelve fixed cases (4 ambiguous, 6 conflicting, 2 degraded) were evaluated in three rounds, once per case per round. Provider: Nebius Token Factory; model: nvidia/Nemotron-3_5-Lightning. The existing service's prompts/schema remained unchanged, enable_thinking=false, temperature=0.1, maximum completion tokens=512. No retries. All 36 calls were REAL HTTP 200, with valid output and no fallback/failure.

Consistency means identical risk/action pairs or identical mapped actions; explanation wording/confidence are not included. Failed runs would remain in the three-run denominator and cannot establish valid agreement.

| Case | Decision consistency | Action consistency | Exact correctness | Action correctness |
|---|---:|---:|---:|---:|
| C01 | 3/3 | 3/3 | 3/3 | 3/3 |
| C02 | 3/3 | 3/3 | 0/3 | 3/3 |
| C03 | 3/3 | 3/3 | 0/3 | 3/3 |
| C04 | 3/3 | 3/3 | 0/3 | 3/3 |
| D01 | 3/3 | 3/3 | 0/3 | 3/3 |
| D02 | 3/3 | 3/3 | 0/3 | 0/3 |
| D03 | 3/3 | 3/3 | 3/3 | 3/3 |
| D04 | 3/3 | 3/3 | 3/3 | 3/3 |
| D05 | 3/3 | 3/3 | 3/3 | 3/3 |
| D06 | 3/3 | 3/3 | 0/3 | 3/3 |
| F01 | 3/3 | 3/3 | 0/3 | 3/3 |
| F02 | 3/3 | 3/3 | 0/3 | 3/3 |

Aggregate decision consistency: 12/12 unique cases (100%). Action consistency: 12/12 (100%). Unique-case action correctness: rules 6/12; Nemotron 11/12. Nemotron repeated-call exact correctness: 12/36 (33.33%); action correctness: 33/36 (91.67%). These 36 calls repeat 12 scenarios; rule repetition is not 36 independent samples. Mean/median latency: 579.08/545.5 ms over the 36 calls.

Unique-case difficult-subset safety: rules false STOP 4/10, missed required STOP 2/2, inappropriate CONTINUE 0/10, correct REOBSERVE 6/8, unnecessary REOBSERVE 2/4. Nemotron: false STOP 0/10, missed STOP 0/2, inappropriate CONTINUE 0/10, correct REOBSERVE 8/8, unnecessary REOBSERVE 1/4. Invalid output/fallback/failed runtime: 0/36 actual calls each.

No decision/action variability was observed in these three runs, but consistent errors persist: 21 repeated risk-label mismatches plus D02's three unnecessary pauses. Consistency does not establish correctness or future determinism.

### Conflicting-evidence demo methodology

New Scenario D starts with explicitly SYNTHETIC / SCENARIO-DEFINED sufficient-looking competing sources: A indicates RED, B reports NORMAL, target association unresolved. The calculated rule baseline stops; the first REAL Nemotron decision is displayed as returned.

The existing agentic inspection collects actual Unity/OpenCV secondary evidence. A scripted, explicitly synthetic target-association check then identifies B as a different equipment target; this contextual correction is not claimed as a measured sensor result. The actual secondary condition, confidence and verification remain unchanged and are sent to Nemotron for the second REAL decision. Final SAFE/DANGER/UNKNOWN outcome is not hard-coded.

A validated contextual STOP/CONTINUE first result still permits this scheduled re-observation step; it is not represented as a model REOBSERVE command. Failure/fallback does not trigger physical inspection. The panel displays conflict, actual first/second model results and computed rule comparisons. Local structured demo evidence records observations, decisions, actions, states and timing. One persistent reservation bounds validation to at most two additional calls (38 total Phase 7B calls). No demo calls were executed by this agent.

The owner reported manual approval of the Phase 7B conflict demo in the Phase 7C request. Conflict Demo V2 has its own pending approval below.

### Validation and conclusion

Shared mapper, metric denominators, mismatch audit, consistency including failures, serialization roundtrip, and stored-result reconciliation: PASS. Parser/validator regression: 12/12 PASS. Scenario regression including synthetic conflict: 24/24 PASS. Compile against actual Unity references: PASS. Generated sanitized per-run records and analysis stay in ignored local results; no Git publication.

Historical Phase 7B classification was MODERATE. Phase 8B identifies cue and representation confounds, so these repeated data do not independently establish contextual action value or AI necessity. Nemotron is not universally superior: exact label performance is worse, D02 fails consistently, and the experiment compares a bounded conservative rule table on a small hand-authored synthetic dataset. A richer deterministic engine could match these cases. The 36 runs repeat twelve scenarios rather than adding independent scenario coverage. No hardware or new Unity visual-validation evidence is implied.

## Phase 7C robustness and held-out pilot — 2026-10-08

Implementation PASS — awaiting manual Unity validation of Conflict Demo V2. EXTERNAL LABEL REVIEW: PENDING.

### Frozen methodology and stronger v2

This is a **pilot synthetic/local benchmark**. Sixteen new scenarios were authored before model inference; their inputs, internally proposed labels and source/policy SHA-256 hashes were saved before the first request. Labels were not changed after output. No rule, prompt, schema or envelope tuning occurred after evaluation. These cases are held out from earlier PatrolAI evaluations, not asserted absent from model pretraining.

V1 remains preserved. V2 uses sufficient/current quality checks; clear RED -> STOP; GREEN/NORMAL/AREA_CLEAR -> CONTINUE; weak/stale/missing/unclassified evidence -> REOBSERVE; unresolved conflict -> REOBSERVE. An explicit confirmed_hazard=true flag overrides an otherwise sufficient green indication. After two exhausted re-observation attempts, unresolved evidence conservatively stops for human review. Exact preregistered rules and label policy are in [README_PHASE7C](../Evaluation/README_PHASE7C.md).

All systems use the same production action mapper and deterministic safety envelope. Operational actions are CONTINUE, REOBSERVE, STOP. Malformed/unsupported/failed inference stops for human review without model correctness credit. CONTINUE on unresolved evidence becomes REOBSERVE while budget remains; at budget exhaustion, unresolved evidence or proposed REOBSERVE becomes terminal STOP. Raw proposals and operational outcomes remain separately recorded.

One primary evaluation per system per case; exactly 16 new REAL Token Factory Nemotron requests, no retries or old benchmark reruns. Source model, system prompt, schema and request settings remained unchanged.

### Held-out configurations: raw versus bounded execution

| Metric | Rule v2 (bounded) | Raw Nemotron | Nemotron + Safety Envelope |
|---|---:|---:|---:|
| Exact risk/action-pair correctness | 14/16 (87.50%) | 7/16 (43.75%) | 8/16 (50%) |
| Action correctness | 14/16 (87.50%) | 14/16 (87.50%) | 15/16 (93.75%) |
| Conflicting-subset action correctness | 3/5 | 5/5 | 5/5 |
| Missed required STOP / STOP labels | 2/6 | 2/6 (HC04, HC16) | 1/6 (HC04) |
| Inappropriate CONTINUE / non-CONTINUE labels | 2/13 | 0/13 | 0/13 |

Historical v1 comparison and remaining operational metrics use the bounded configurations below; every Nemotron operational column explicitly includes the envelope.

| Metric | Rule v1 + Safety Envelope | Rule v2 (bounded) | Nemotron + Safety Envelope |
|---|---:|---:|---:|
| Exact risk/action-pair correctness | 10/16 (62.50%) | 14/16 (87.50%) | 8/16 (50%) |
| Action correctness | 10/16 (62.50%) | 14/16 (87.50%) | 15/16 (93.75%) |
| False STOP / non-STOP labels | 3/10 (30%) | 0/10 (0%) | 0/10 (0%) |
| Missed required STOP / STOP labels | 3/6 (50%) | 2/6 (33.33%) | 1/6 (16.67%) |
| Inappropriate CONTINUE / non-CONTINUE labels | 3/13 (23.08%) | 2/13 (15.38%) | 0/13 (0%) |
| Correct REOBSERVE / REOBSERVE labels | 4/7 (57.14%) | 7/7 (100%) | 7/7 (100%) |
| Unnecessary REOBSERVE / non-REOBSERVE labels | 0/9 (0%) | 0/9 (0%) | 1/9 (11.11%) |
| Unresolved operational cases | 4/16 | 7/16 | 8/16 |
| Budget-escalated cases | 2/16 | 2/16 | 2/16 |
| Envelope changed the proposal | 1/16 | 2/16 | 1/16 |
| Invalid output / failure / fallback | N/A | N/A | 0/16 each |

Unresolved means effective REOBSERVE still awaiting evidence, not a claim of safe completion. Budget escalation includes already-proposed STOPs that the envelope confirms at the limit. Runtime fallback is distinct from deterministic policy intervention. Zero-denominator category rates are N/A.

Pre-envelope proposals, excluding the common budget stage: v1 exact/action 9/16; v2 exact/action 12/16; Nemotron exact 7/16 and action 14/16. V2's public bounded decision includes its budget stage. The envelope corrects HC16 for all systems; it also corrects v2's unresolved HC12. Nemotron's effective HC16 success must not be credited to model reasoning: its raw output was UNKNOWN/REOBSERVE, changed to DANGER/STOP by the envelope.

### Category exact and action comparison

| Category | Count | Rule v1 + envelope exact/action | Rule v2 bounded exact/action | Nemotron + Safety Envelope exact/action |
|---|---:|---:|---:|---:|
| Clear safe | 2 | 2/2; 2/2 | 2/2; 2/2 | 2/2; 2/2 |
| Clear hazard | 2 | 1/2; 1/2 | 2/2; 2/2 | 1/2; 1/2 |
| Ambiguous | 3 | 3/3; 3/3 | 3/3; 3/3 | 0/3; 3/3 |
| Conflicting | 5 | 1/5; 1/5 | 3/5; 3/5 | 3/5; 5/5 |
| Degraded/missing | 2 | 1/2; 1/2 | 2/2; 2/2 | 0/2; 2/2 |
| Temporal/re-observation | 2 | 2/2; 2/2 | 2/2; 2/2 | 2/2; 2/2 |

On the difficult subset (ambiguous + conflicting + degraded, 10 unique cases), effective action correctness is v1 5/10, v2 8/10, Nemotron + Safety Envelope 10/10 (raw Nemotron also 10/10 on this subset). Exact correctness is v1 5/10, v2 8/10, Nemotron 3/10. Raw Nemotron ties bounded v2 overall at 14/16; Nemotron + Safety Envelope exceeds bounded v2 by one net case, attributable to the HC16 envelope correction.

### Safety and failure analysis

- HC04 is a required hazard STOP missed by Nemotron: it returned WARNING/REOBSERVE despite the explicit confirmed electrical hazard. V2 stops correctly; v1 inappropriately continues. A pause is safer than continuing, but still misses the required immediate STOP.
- HC09 and HC11 are contextual hazards despite green readiness/stopped-motor indicators. Nemotron stops correctly; both rules inappropriately continue. V2 only recognizes its explicit structured hazard flag, not arbitrary natural-language hazard semantics. A richer rule engine could encode these semantics.
- HC08 and HC10 are unresolved red-versus-normal conflicts. V2 and Nemotron correctly re-observe; v1 falsely stops. Nemotron still uses WARNING rather than expected UNKNOWN.
- HC13 is stale red evidence. V2 and Nemotron re-observe; v1 falsely stops.
- HC05–HC08, HC10, HC13 and HC14 retain seven UNKNOWN-versus-WARNING exact-label errors even though their bounded REOBSERVE actions are correct.
- HC12 and HC16 end STOP under the exhausted budget. HC16 required an actual envelope override; no model-generated resolution is claimed.

Nemotron latency over all 16 calls: mean 703.56 ms, median 652 ms. All 16 were REAL HTTP 200 with valid structured output; zero failed calls, fallback or invalid output.

### Corrected repeatability and sampling statement

The previous Phase 7B difficult subset contains **12 unique cases**, not 36 independent rule samples. Rules: 6/12 action-correct; Nemotron: 11/12 action-correct. Repeatability: 12/12 cases had 3/3 action agreement and 12/12 had 3/3 risk/action-pair agreement. No decision/action variability was observed in those three runs; explanation/confidence equality was not tested.

Actual configured parameters: temperature=0.1, maximum completion tokens=512, enable_thinking=false, stream=false. The client configures no seed or top_p. API determinism is not guaranteed by this configuration; observed agreement must not be generalized to guaranteed future determinism. Repeated calls are clustered by scenario and do not create extra independent scenario coverage.

### Conflict Demo V2 and bounded outcome

The existing synthetic RED-versus-NORMAL scenario and inherited physical re-observation are preserved. First/second model decisions are displayed as actually returned. The Source B target-association correction remains explicitly scenario-defined; secondary visual condition/confidence/verification remain actual Unity/OpenCV evidence, not industrial hardware.

Label: SYNTHETIC SCENARIO EVIDENCE — REAL NEMOTRON INFERENCE when the call succeeds; failed calls retain FAILED status. Rule v1/v2 comparisons are actually computed. No output is hard-coded. A first contextual STOP/CONTINUE, if returned instead of REOBSERVE, remains visible and does not masquerade as a model REOBSERVE; scheduled inspection may still collect secondary evidence.

Default safety-envelope maximum: two re-observation attempts. Demo maximum: **one physical re-observation**, to respect two total model calls. The second call is terminal: sufficient validated SAFE/CONTINUE can continue; validated DANGER/STOP stops; unresolved REOBSERVE, malformed/failed reasoning or failed re-observation stops for human review through the deterministic envelope. Model decision and envelope action are shown separately. No third model call or automatic inspection loop is started. Timeout/missing-adapter/stale-evidence paths also terminate conservatively.

A separate persistent Phase 7C reservation permits one two-call demo run. Zero demo calls were executed by this agent; the 16-call benchmark leaves at most two authorized demo calls. Manual approval is required only for Conflict Demo V2: visible labels/conflict, first reasoning, physical re-observation, new evidence, second reasoning, bounded final action, no infinite REOBSERVE, and truthful v1/v2 comparison.

### External label review and conclusion

[Human label-review template](PATROLAI_LABEL_REVIEW_TEMPLATE.md) includes every held-out case, proposed labels, disagreement/correction fields, reviewer role and quotation permission. EXTERNAL LABEL REVIEW: PENDING. This does not block implementation and is not fabricated approval.

Historical Phase 7C technical classification: **MODERATE**, superseded by the Phase 8B claim boundary; AI necessity is not established. Raw Nemotron and bounded Rule v2 each miss two required STOPs; Nemotron + Safety Envelope misses one. The historical conflicting subset shows differing actions; Phase 8B places both incremental model successes in STOP-cued contexts, so contextual advantage is not isolated. Bounded v2 wins exact-label accuracy. HC04 remains incorrect. The common envelope contributes a measured correction; it is not a complete industrial safety interlock and does not guarantee hazard detection.

The 16 cases and original 24 cases are internally authored synthetic/local pilot samples. They do not establish statistically conclusive industrial generalization, real-hardware safety, or deployment readiness. Broader independently reviewed scenarios and stronger domain-specific rules could change the conclusion. No statistical significance or API determinism claim is made.

Validation: rule v2, shared mapper/envelope, limits, immutable held-out labels, metric denominators, snapshot serialization and parser/validator regression PASS; actual Unity-reference compilation PASS; targeted Conflict Demo V2 offline checks PASS. Manual V2 approval remains pending.

## Limitations and conclusion

This is one small synthetic benchmark, not a representative deployment dataset, safety certification, vision accuracy measurement, or repeated statistical study. Context cases describe trusted source semantics explicitly; the model's results depend on that scenario information. A richer deterministic engine may match or exceed these outcomes. No external rule-engine baseline, hardware, Unity live loop, or serverless job was evaluated.

Public conclusion: raw Nemotron and bounded Rule v2 each achieved 14/16 action-correct; the hybrid achieved 15/16 through the HC16 envelope correction. HC04 remains a missed required STOP. Under the Phase 8B conservative cue rubric, all three tie at 8/8 on no-cue held-out actions and 2/2 on no-cue conflicts. Both raw-model improvements over Rule v2 contain STOP guidance. AI necessity is not established; there is no isolated superiority demo candidate. Earlier MODERATE classifications below are historical interpretations superseded by the Phase 8B claim boundary.
