# VISIONGUARD Phase 4 — Multi-Source Evidence Fusion

## Scope and sealed boundary

Phase 4 is additive. It does not change the Phase 1A environment, Phase 2 observation/OpenCV implementation, or Phase 3 controller, movement, scene, and overlay. Each Unity source still calls `VGObservationExporter`, and each manifest still passes through `vision.opencv.run.process` to produce a normal `VisualEvidence` record before fusion.

Initial source IDs are `VG_CCTV_01`, `VG_CCTV_02`, and `VG_MOBILE_01`. All evidence in one fusion run must describe the same target, and a camera ID may appear only once. Unsupported or duplicate sources and target mismatches are rejected rather than guessed or silently merged.

## Deterministic policy

The existing `EvidencePolicy` defines whether each record is sufficient: quality must be `SUFFICIENT`, condition must be one of the accepted indicator conditions, and the deterministic visual score must be at least `0.6`.

- `CONSISTENT`: at least two distinct sources are sufficient and every sufficient source reports the same condition. Result: `VERIFIED`, `RECORD`, and that condition.
- `CONFLICT`: at least two distinct sources are sufficient but their conditions differ. Result: `UNRESOLVED`, `REQUEST_ADDITIONAL_EVIDENCE`, `UNKNOWN`.
- `INSUFFICIENT`: fewer than two distinct sources are sufficient. Result: `UNRESOLVED`, `REQUEST_ADDITIONAL_EVIDENCE`, `UNKNOWN`.

An insufficient record cannot vote a fused condition. A majority does not override any sufficient conflict. This deliberately prevents silent verification when strong sources disagree.

Every result carries ordered provenance copied from each actual `VisualEvidence`: camera ID, observation ID, condition, quality, score, and acquisition timestamp when present. Fusion does not invent measurements or conditions.

## Runtime flow

The Phase 4 Unity controller captures one observation from each configured exporter, then invokes `python -m agent.fusion.run` with the three manifest paths. The Python runner processes every manifest through the existing OpenCV pipeline, emits evidence and annotation files under `exchange/evidence`, writes the fusion record under `exchange/fusion`, and writes JSONL trace events under `exchange/traces`.

Use `VISIONGUARD > Phase 4 > Build Runtime Review Scene` once to populate the additive Phase 4 scene. The builder copies the sealed Phase 3 scene only when the Phase 4 scene does not yet exist, then adds `VG_CCTV_02` and `VG_Phase4_FusionController` only to the Phase 4 scene.

## Manual Unity review

1. Run `VISIONGUARD > Phase 4 > Build Runtime Review Scene`.
2. Open `Assets/VisionGuard/Phase4/Scenes/VISIONGUARD_IndustrialRoom_Phase4_v01.unity` and enter Play Mode.
3. Select `VG_Phase4_FusionController` and click **RUN MULTI-SOURCE FUSION**.
4. Inspect its read-only State/Fusion Status/Verification/Action/Final Condition fields and the corresponding JSON in `exchange/fusion`.
5. Confirm a conflict or weak evidence returns `REQUEST_ADDITIONAL_EVIDENCE` and never `VERIFIED`.

Unity runtime validation is intentionally left to the user. Targeted automated validation uses replay/synthetic evidence and does not recapture or modify the sealed baseline.
