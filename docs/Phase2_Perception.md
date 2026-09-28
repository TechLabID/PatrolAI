# Phase 2 — OpenCV perception foundation

**USER_APPROVED_STATIC** was supplied in the Phase 2 user brief. This supersedes the earlier pending status in the historical Phase1A handoff. Phase1A is sealed; no geometry, material, lighting, package, project setting, or RF90 camera change was made. No Phase3 implementation is included.

## Implementation

- Scene: `Assets/VisionGuard/Phase2/Scenes/VISIONGUARD_IndustrialRoom_Phase2_v01.unity`.
- Camera: **VG_CCTV_01**, fixed at `(-0.4, 2.2, 5.27)`, yaw -90°, vertical FOV 36°. It faces SWG_05; Indicator_03 is the first calibrated target. Camera coordinates are derived from the approved serialized geometry, not yet verified in a render.
- The camera is disabled for ordinary rendering, has no AudioListener, no mesh, no automatic capture and no motion. Explicit URP render requests produce observations without replacing the unchanged RF90 Game-view camera.
- `Assets/VisionGuard/Phase2/Scripts/VGObservationExporter.cs`: manual one-shot export.
- `camera/protocol/observation.py`: validated VisualObservation v1. `camera/adapters/unity.py`: ICameraSource protocol and UnityCameraAdapter.
- `vision/opencv/`: strict OpenCV 5 version guard, PNG loading and one-shot CLI; `vision/detection/indicator.py`: color measurement; `vision/evidence/model.py`: machine-readable VisualEvidence.
- `tools/prepare_phase2_scene.py`: additive offline copy, refuses overwrite; **never rerun the Phase1A generator**. `tools/validate_phase2_scene.py` and `docs/Phase2_Baseline_Hashes.json`: sealed baseline checks.
- `tests/test_phase2.py`, `tests/fixtures/phase2/`: focused failure cases and explicitly synthetic fixture.

## Transport and contract

File exchange is the smallest local, inspectable, replayable transport for a manually triggered observation. There is no server, network port, polling daemon or cloud dependency. A later camera adapter can replace it without changing the detector.

Unity writes a unique `exchange/observations/obs_<uuid>.png` and publishes matching `.json` **last**, through same-directory temporary files and renames. Readers only consume completed `.json` manifests and verify the PNG SHA256. IDs are unique per acquisition; replay of the same observation produces identical evidence. Timestamp is acquisition UTC, not Python processing time.

JSON v1 has exactly: `schema_version`, `observation_id`, `camera_id`, `timestamp` (ISO8601 with timezone), `source_type` (`unity` in production), `frame` (relative PNG basename), `frame_sha256`, integer `width`/`height`, nullable `camera_pose` (`position` in Unity world metres and unit `rotation_xyzw`), and `metadata` (`target_id`, `roi`, `provenance`). ROI is integer **[x,y,width,height] in top-left-origin decoded image pixels**, fully inside the frame. No target color, anomaly label or hidden ground truth crosses the boundary. Initial ROI is `[604,324,72,72]` at 1280×720.

The manifest is size-limited to 64 KiB, image to 32 MiB and 4096×4096. Duplicate keys, unknown schema fields, invalid timestamps, invalid poses, unsupported sources, invalid ROI, traversal paths, mismatched hashes/dimensions and decode failures are rejected. A failure exits 2 with `PERCEPTION_FAILED`; it does not emit a new condition/evidence record. Previously generated evidence retains its original ID/hash and is not a response to a failed acquisition. `UNKNOWN` means valid pixels but insufficient visual support, not a fault or an unlit-lamp conclusion.

## Measurement and score

OpenCV 5 decodes PNG to BGR, crops the calibrated ROI, converts to HSV, thresholds red/amber/green, removes isolated pixels with a 3×3 morphological opening, and measures connected components. OpenCV hue ranges (0–179): red 0–10 or 170–179; amber 11–35; green 36–89. Saturation >=90 and value >=65 suppress gray panels and dark noise.

For each color, the largest component supplies pixel area, ROI area fraction, bounding-box fill/aspect ratio, center score and border contact. The candidate score is:

`min(1, area_fraction/0.12) × dominant_color_area_share × aspect_ratio × min(1, fill_ratio/0.65) × center_score`

`center_score = max(0, 1 - distance(component_bbox_center, ROI_center)/(min(ROI_width,ROI_height)/2))`.

SUFFICIENT requires >=16 component pixels, area fraction 0.025–0.65, color dominance >=0.8, aspect >=0.65, no border contact and score >=0.6. Accepted conditions are `INDICATOR_RED`, `INDICATOR_AMBER`, `INDICATOR_GREEN`; all other valid inputs produce `UNKNOWN / INSUFFICIENT / confidence=0`, retaining the candidate score in measurements. Confidence is a **defined deterministic visual score, not calibrated probability**. It measures color/shape support in a known ROI, not physical energization or electrical safety.

Evidence includes acquisition identity, target, timestamp, ROI, measurements, quality, source type, PNG hash, method/version and `domain=simulated_visual_condition`. Optional annotation is a separate file; raw image remains unchanged. Unity logs `OBSERVATION_CAPTURED`; Python emits JSON trace events `OPENCV_EVIDENCE_CREATED` or `PERCEPTION_FAILED` to stderr. Redirect stderr to a local JSONL file if persistence is needed; no broader observability system was added.

## Environment and quick checks

Installed on Python 3.12 x64 in `.venv-phase2`: **opencv-python 5.0.0.93**, runtime **cv2 5.0.0**, **NumPy 2.2.6**. OpenCV 4 is explicitly rejected. Recreate with:

```powershell
python -m venv .venv-phase2
.\.venv-phase2\Scripts\python.exe -m pip install -r requirements-phase2.txt
.\.venv-phase2\Scripts\python.exe -m unittest discover -s tests -p test_phase2.py -v
.\.venv-phase2\Scripts\python.exe -m vision.opencv.run tests\fixtures\phase2\synthetic_red_001.json --fixture --annotated
python tools\validate_phase2_scene.py
```

Runtime metadata verified directly after installation; distribution reference: [OpenCV Python on PyPI](https://pypi.org/project/opencv-python/5.0.0.93/). Capture follows the documented [Unity 6.6 RenderPipeline.SubmitRenderRequest API](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rendering/renderpipeline/submitrenderrequest). Offline compilation uses existing installed Unity/URP assemblies, not an Editor-control mechanism.

## Minimum manual Unity validation

1. Open the Phase2 scene in **Unity 6000.6.3f1**, allow import/compile, and check Console. Keep the CCTV Camera component disabled. Enter Play Mode.
2. Select **VG_CCTV_01**. In **VG Observation Exporter (Script)**, click the clearly labeled **CAPTURE ONE OBSERVATION** button. The button becomes active in Play Mode. The Console should report `OBSERVATION_CAPTURED` with a manifest path. No command sent by Codex operates the Editor.
3. From the project root, run:

```powershell
.\.venv-phase2\Scripts\python.exe tools\process_latest_observation.py --save-fixture
```

Inspect the raw PNG, annotated PNG and JSON in `exchange/`. Confirm image orientation and that the ROI encloses only the intended lamp. If it does not, calibrate only the Phase2 camera/ROI and recapture; leave Phase1A unchanged. Stop Play Mode when done. The saved Unity fixture can then be replayed without Unity (see fixture README).

## Validation boundary

Executed: Python syntax/import, dependency/version checks, targeted unit tests, synthetic PNG → OpenCV → evidence/annotation, baseline SHA256/component comparison, and standalone C# compilation against installed Unity 6.6 references.

**Not executed:** Unity Editor import/Console validation, scene opening, URP render-request/readback correctness, actual camera framing/ROI alignment and end-to-end Unity-render → OpenCV. The supplied fixture is synthetic; no Unity-origin fixture is claimed yet. Lighting/color response and thresholds may require calibration from the first actual render. Close fixed camera/ROI calibration is intentional; whole-room detection, arbitrary camera poses, color constancy and real electrical fault diagnosis are outside this phase.

Status: **PHASE 2 OPENCV PERCEPTION FOUNDATION READY FOR REVIEW.** Files and offline tests are ready; full Phase2 PASS remains pending the manual Unity integration checks above. Stop here; no reasoning, movement, active re-observation, AWS or evaluation engine is implemented.
