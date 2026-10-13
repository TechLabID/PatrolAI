# PatrolAI Competition Demo Scenarios

This additive demo layer consumes the existing Phase 4 Physical AI closed loop. Scenario inputs are deterministic; decisions shown as `REAL` still come from a current Nebius Token Factory request to `nvidia/Nemotron-3_5-Lightning` with thinking disabled and the strict decision schema.

## Scenarios

- **A — Normal / Safe:** `NORMAL`, `SUFFICIENT`, confidence `0.98`; expected direction `SAFE → CONTINUE → CONTINUING`.
- **B — Insufficient Evidence:** `INDICATOR_UNCLEAR`, `INSUFFICIENT`, confidence `0.35`; expected direction `UNKNOWN → REOBSERVE → REOBSERVING`.
- **C — Confirmed Hazard:** `INDICATOR_RED`, `SUFFICIENT`, confidence `0.95`; expected direction `DANGER → STOP → STOPPED`.
- **AUTO PATROL — HC09:** moves through existing simulated inspection entry points, then reasons on the frozen LOCAL SAMPLE held-out HC09 context. At most two reasoning invocations and one re-observation; failure or exhausted unresolved evidence escalates conservatively. HC09 contains STOP guidance and is a functional example, not isolated superiority evidence. See the [Phase 8B audit](../Competition/PATROLAI_CUE_FAIRNESS_AUDIT.md).

The expected direction is validation guidance, not a prerecorded model response. A live model response is displayed exactly according to its validated decision and action mapping.

## Runtime truthfulness

The evidence panel distinguishes `REAL`, `MOCK`, `FALLBACK`, `FAILED`, `LOCAL SAMPLE`, and `NOT VERIFIED`. Starting a new run clears the previous result. A failed request cannot leave an earlier `REAL` decision visible, and fallback output identifies the safe re-observation policy as its source.

## Baseline menu mapping

- `RUN AGENTIC INSPECTION` → inherited `VGPhase3AgentController.RunAgenticInspection()` active re-observation workflow.
- `RUN MULTI SOURCE FUSION` → inherited `VGPhase4FusionController.RunMultiSourceFusion()` evidence fusion workflow.
- `EVALUATE LATEST VERIFIED FUSION` → inherited `VGPhase5TemporalController.EvaluateLatestFusion()` temporal evaluation workflow.

The adapter delegates to those public entry points and does not reproduce their algorithms. Completion is displayed only after the underlying controller reports `COMPLETE`; otherwise the menu reports failure.

## Competition demo sequence

1. Open `VISIONGUARD_IndustrialRoom_Phase5_v01` and enter Play Mode.
2. Use the baseline buttons to run agentic inspection, fusion, and latest verified evaluation as required.
3. Use Scenario A, B, C, or AUTO PATROL from the competition panel. Scenario C sends the fixed hazard observation through the live reasoning path; AUTO PATROL uses explicitly labeled local HC09 scenario evidence.
4. Record the evidence panel showing runtime, provider, model, observation, evidence, decision, action, final state, and latency.

## Manual validation gate

The existing A/B/C and baseline-control manual validation is recorded in [Windows build validation](../Competition/PATROLAI_WINDOWS_BUILD_VALIDATION.md); AUTO PATROL HC09 manual Unity PASS is owner-reported in the [evaluation](../Competition/PATROLAI_AI_NECESSITY_EVALUATION.md). These historical approvals concern simulation. A fresh environment still needs its own visual/runtime confirmation; offline compilation and tests alone do not establish acceptance. External label review remains PENDING.
