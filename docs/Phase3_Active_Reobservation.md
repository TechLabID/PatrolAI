# Phase 3 — agentic active re-observation

Status: implementation and targeted offline validation are complete. Unity runtime demonstration remains required before Phase 3 PASS.

## Scene and control

- Scene: `Assets/VisionGuard/Phase3/Scenes/VISIONGUARD_IndustrialRoom_Phase3_v01.unity`
- Controller: **VG_Phase3_AgentController**
- Inspector button: **RUN AGENTIC INSPECTION** (active only in Play Mode)
- Mobile platform: **VG_MobileInspector_01** with child **InspectionCamera**, camera ID `VG_MOBILE_01`

The scene is an additive copy of the Phase 2 scene. Phase 1A and Phase 2 scene files are unchanged. The RF90 room, lighting, materials and `RF90_ReviewCamera_Main` are preserved. The mobile platform uses simple project-native geometry and begins just outside the room behind the review camera.

## Evidence-driven loop

The controller invokes the existing `VGObservationExporter` for both cameras. The exporter received only two read-only outcome properties (`LastManifestPath`, `LastError`) so the controller can use the already proven PNG + JSON + SHA256 path. Capture, OpenCV thresholding and evidence generation are not duplicated.

The Phase 3 primary `VG_CCTV_01` uses a wider 60° vertical FOV while retaining the live-validated `[604,324,72,72]` ROI. Phase 2 remains at 36°. A projection-scale check on the latest real Unity frame predicts 318 red pixels, area fraction `0.061343`, and candidate score `0.496992`, producing `UNKNOWN / INSUFFICIENT / confidence=0`. This is weaker image evidence caused by the indicator occupying fewer pixels; no confidence or evidence result is injected.

`EvidencePolicy` requires `evidence_quality=SUFFICIENT`, an accepted indicator condition and deterministic score >=0.6. A sufficient primary goes directly to `VERIFY → COMPLETE` and does not move the inspector. An insufficient primary records `REQUEST_REOBSERVATION` and `MOVE_TO_INSPECTION_POSE`.

The inspector follows a smooth 2.5-second deterministic path from `(0,2.2,-0.6)` to `(-0.8,2.2,5.27)`. Its camera sits 0.32 m ahead of the body and faces SWG_05. Live secondary capture placed the red lamp at frame center, but the original 72×72 ROI clipped it. The calibrated `[592,312,96,96]` ROI contains the full component with margin. No physics, NavMesh, SLAM or path planner was added.

Unity starts the pinned `.venv-phase2` Python executable for two bounded one-shot stages. `agent.run primary` reuses the existing OpenCV pipeline and returns the action. Only `REQUEST_REOBSERVATION` allows movement and the second capture. `agent.run secondary` accepts only `VG_MOBILE_01`, requires the same target, compares both evidence records and emits `VERIFIED`, `REJECTED` or `UNRESOLVED`. Missing, failed, ambiguous or unsupported secondary evidence yields `UNRESOLVED`; disagreement with an insufficient primary also remains `UNRESOLVED`.

Session state is written to `exchange/agent/inspection_<uuid>.json`; JSONL trace to `exchange/traces/inspection_<uuid>.jsonl`; evidence and annotations remain in `exchange/evidence/`. Required observation, OpenCV, agent decision, request, action, secondary observation and verification events are recorded. A small Game-view overlay exposes the current state and evidence; it is a debug aid only.

## Targeted validation performed

- `python -m unittest tests.test_phase3 -v`: 8/8 PASS.
- Policy branches: sufficient stops; insufficient requests re-observation.
- Preserved real Unity replay: 36° framing stays sufficient; the Phase 3 60° framing projection predicts insufficient evidence with a red candidate.
- Verification: confirming secondary becomes VERIFIED; failure/ambiguity becomes UNRESOLVED.
- State transition path and existing OpenCV integration smoke passed.
- C# runtime and custom Editor scripts compile against installed Unity 6000.6.3f1 assemblies.
- Serialized scene references and protected baseline SHA256 checks pass.

Direct Editor control was unavailable. User-run live validation confirmed the primary decision and movement branch, and supplied the secondary capture used for ROI calibration. The full loop with the revised 96×96 secondary ROI remains to be re-run in Unity.

## Minimum manual validation

1. Open `Assets/VisionGuard/Phase3/Scenes/VISIONGUARD_IndustrialRoom_Phase3_v01.unity` in Unity 6000.6.3f1 and confirm no new red Console errors.
2. Enter Play Mode, select **VG_Phase3_AgentController**, and click **RUN AGENTIC INSPECTION**.
3. Observe the overlay: primary evidence should be insufficient, decision should request re-observation, `VG_MobileInspector_01` should move, and the secondary capture should run automatically through OpenCV.
4. Confirm the overlay/Console ends with `VERIFIED`, `REJECTED`, or `UNRESOLVED`, and inspect the matching JSONL under `exchange/traces/`.

No extra terminal command should be required when `.venv-phase2` remains at the project root. Do not proceed to Phase 4 until the live loop is demonstrated and accepted.
