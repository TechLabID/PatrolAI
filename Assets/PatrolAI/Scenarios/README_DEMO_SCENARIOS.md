# PatrolAI Competition Demo Scenarios

This additive demo layer consumes the existing Phase 4 Physical AI closed loop. Scenario inputs are deterministic; decisions shown as `REAL` still come from a current Nebius Token Factory request to `nvidia/Nemotron-3_5-Lightning` with thinking disabled and the strict decision schema.

## Scenarios

- **A — Normal / Safe:** `NORMAL`, `SUFFICIENT`, confidence `0.98`; expected direction `SAFE → CONTINUE → CONTINUING`.
- **B — Insufficient Evidence:** `INDICATOR_UNCLEAR`, `INSUFFICIENT`, confidence `0.35`; expected direction `UNKNOWN → REOBSERVE → REOBSERVING`.
- **C — Confirmed Hazard:** `INDICATOR_RED`, `SUFFICIENT`, confidence `0.95`; expected direction `DANGER → STOP → STOPPED`.

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
3. Run Scenario C to send the deterministic confirmed-hazard observation through the real PatrolAI reasoning and closed-loop path.
4. Record the evidence panel showing runtime, provider, model, observation, evidence, decision, action, final state, and latency.

## Manual validation gate

`Demo scenarios PASS` must remain unchecked until the user verifies the panel, all three baseline buttons, and scenarios in Unity Play Mode. Offline compilation and tests alone are not visual or runtime-scene acceptance.
