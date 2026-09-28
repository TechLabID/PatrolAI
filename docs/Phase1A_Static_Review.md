# Phase 1A — static review handoff

Target: Unity **6000.6.3f1 (Unity 6.6)**, existing URP 17.6.0. No package or project-setting changes. Architecture.md and SampleScene remain unchanged.

## Files

- `Assets/VisionGuard/Phase1A/Scenes/VISIONGUARD_IndustrialRoom_RF90_v01.unity`
- `Assets/VisionGuard/Phase1A/Materials/`: 14 original generic URP materials, with stable metadata.
- `tools/build_phase1a.py`: offline scene authoring source; running it replaces the generated Phase1A scene/materials. Preserve manual edits before regenerating.
- `tools/validate_phase1a.py`: targeted serialized-reference and scope checks.

Scene: 6 × 12 m floor, 4 m ceiling, 18 inward-facing cabinets with varied face details, approximately 3.1 m aisle between cabinet faces, yellow boundaries, central grating, cable trays/risers/clamps, rear double door, geometric EXIT lettering, fire equipment, vent and five ceiling luminaires. Hierarchy separates environment, equipment, cables, safety, lighting and review camera. Assets use Unity built-in meshes and original geometric details; no purchased or third-party branded assets.

## Minimum manual Unity steps

1. Open this existing project in **Unity 6000.6.3f1** and allow normal import/compilation to finish.
2. Open `Assets/VisionGuard/Phase1A/Scenes/VISIONGUARD_IndustrialRoom_RF90_v01.unity`. Check Console for import/compile errors and missing/pink materials.
3. Select **RF90_ReviewCamera_Main** and inspect its Camera Preview, or use Game view at **1920 × 900** (the reference's main-panel aspect). Play Mode is unnecessary. Compare against the **Main View (Entry Perspective)** panel of `docs/references/RF90_Industrial_Electrical_Room_Static_Reference_v01.png`.

## Validation and limits

Offline validation checks unique IDs, internal references, asset/shader/script GUID resolution, cabinet count, one named review camera, preserved Unity version, and absence of physics/animation components. The only scene scripts are URP camera/light data. No C# source was added.

Unity import/compilation, successful scene opening, Console status and rendered material/lighting correctness are **NOT EXECUTED — authorized Editor access unavailable**. These remain required manual checks; offline checks do not substitute for them.

Visual similarity is **UNVERIFIED**. This is a procedural geometric approximation: cabinet edges are sharp, concrete lacks reference weathering and texture, reflections are simplified, cable bends and small signage are approximate, and illumination has not been tuned from a Unity render. No 90% similarity or PASS_STATIC claim is made. The reference contains differing side/top views; the main entry perspective takes priority.

Status: **STATIC SCENE AUTHORED — UNITY VALIDATION AND RF90 REVIEW PENDING**.

Stop at Phase 1A. Only after Unity validation and visual acceptance should this be labeled “PHASE 1A STATIC ENVIRONMENT READY FOR RF90 REVIEW.” No later phase is authorized until `PASS_STATIC` or `USER_APPROVED_STATIC`.
