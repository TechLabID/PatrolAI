# PatrolAI Windows Build Validation

**Date:** 2026-10-04
**Overall status:** WINDOWS BUILD AND MANUAL EXECUTABLE WORKFLOW PASS

## Configuration

- Unity version: `6000.6.3f1` (`45d8eee7de74`)
- Intended target: Windows Standalone x86_64
- Product name: `PatrolAI` (already configured; unchanged)
- Enabled build scene: `Assets/VisionGuard/Phase5/Scenes/VISIONGUARD_IndustrialRoom_Phase5_v01.unity`
- Output directory: `Builds/Windows/PatrolAI/`
- Executable: `PatrolAI.exe`
- `Builds/` remains ignored by Git.

`ProjectSettings/EditorBuildSettings.asset` now contains only the validated competition scene. The previous `Assets/Scenes/SampleScene.unity` entry was removed from the build list. No scene content was modified.

## Phase 6B.1 root cause and fix

The inherited controllers resolved their runtime root as `Application.dataPath/..`. That is the project root in Editor and the executable directory in a Windows player. The manual player therefore correctly searched beside `PatrolAI.exe`, but the build contained neither `.venv-phase2` nor the `agent/`, `vision/`, and `camera/` packages. It failed at the first Python executable check with `Phase 2 Python environment is missing`; no perception, fusion, temporal, Nebius, or Nemotron algorithm caused the failure.

The minimum fix adds one shared `PatrolPythonRuntime` resolver. The existing Phase 3, 4, and 5 controllers call it before starting their unchanged Python modules. An Editor-only post-build callback copies exactly these source resources beside the executable:

```text
agent/**/*.py
vision/**/*.py
camera/**/*.py
requirements-phase2.txt
exchange/
```

No Python executable, virtual environment, API key, `.env`, or developer-specific path is copied by the build. The standalone dependency model is external Python 3.12 x64 plus an executable-relative `.venv-phase2` created from `requirements-phase2.txt`. An advanced local override may set `PATROLAI_PYTHON` to an absolute compatible Python executable. Missing Python/packages remain a truthful workflow failure.

## Offline compile and dependency evidence

- Unity batch compilation completed with no build-blocking script error.
- All direct script GUID references inspected in the competition scene resolve to project/package metadata.
- Runtime PatrolAI source outside Editor folders contains no `using UnityEditor` directive.
- Project code contains no direct Unity Recorder API reference.
- The Recorder package remains installed and unchanged.
- `NebiusConfig` reads `NEBIUS_API_KEY` through `Environment.GetEnvironmentVariable`.
- No hard-coded Nebius key candidate was found in `Assets`, `Packages`, or `ProjectSettings` during this validation.
- If the environment variable is absent, `NebiusConfig.TryGetApiKey` returns failure with an explicit missing-key message; it does not synthesize a successful runtime result.

## Unity compile and Windows build

- Unity Editor/batch compile: **PASS**
- Windows Standalone x86_64 build: **PASS**
- Build result: `Success`
- Executable: `Builds/Windows/PatrolAI/PatrolAI.exe`
- Runtime support copy: **PASS** (`PATROLAI_STANDALONE_PYTHON_RUNTIME_READY`)
- Unity Recorder: remained installed; no project runtime Recorder API dependency or build blocker was found.
- Non-blocking player-log warnings: some URP post-processing shaders were stripped/unsupported. No immediate crash or blocking missing-script/dependency exception was logged.

## Executable validation

- Executable launch/process: **PASS** — process started, remained responsive, and initialized Unity/Direct3D without a blocking player-log error.
- Competition scene load: **PASS** — automated/log-level evidence plus user manual validation.
- PatrolAI competition UI: **PASS — USER MANUAL VALIDATION**.
- Agentic Inspection: **COMPLETE — USER MANUAL VALIDATION**.
- Multi Source Fusion: **COMPLETE — USER MANUAL VALIDATION**.
- Latest Verified Fusion: **VERIFIED — USER MANUAL VALIDATION**.
- Scenario B physical re-observation: **PASS — USER MANUAL VALIDATION**.
- Scenario C Nemotron runtime: **REAL / DANGER / STOP / STOPPED — USER MANUAL VALIDATION**.
- `EXIT PATROLAI`: **PASS — USER MANUAL VALIDATION**.
- Immediate-crash check: **PASS**

The user supplied the manual standalone results above after exercising the rebuilt executable. They are intentionally labeled separately from the automated compile, build, process, and Python-backend checks.

## Standalone Python workflow validation

A local executable-relative `.venv-phase2` was created for validation using installed Python `3.12.4`, then the pinned requirements were installed: OpenCV `5.0.0.93` and NumPy `2.2.6`. This ignored local environment is validation/deployment output, not source-controlled or silently bundled by the build.

Using the copied runtime beside the rebuilt executable and prior real Unity observation files:

- Agentic primary: **PASS**, `REQUEST_REOBSERVATION`.
- Agentic secondary: **PASS**, `COMPLETE / VERIFIED`.
- Multi-source fusion: **PASS**, `CONSISTENT / VERIFIED`.
- Latest verified fusion temporal evaluation: **PASS**, `NO_CHANGE`.
- Targeted Phase 3–5 regressions: **17/17 PASS**.

These prove the fixed standalone Python dependency layout. They do not substitute for clicking the three controls or visually observing physical movement in the running executable.

## Live executable runtime

- Nebius/NVIDIA runtime in the standalone executable: **PASS — USER MANUAL VALIDATION**.
- Scenario C: **REAL**, Nebius Token Factory, `nvidia/Nemotron-3_5-Lightning`, `DANGER / STOP / STOPPED`.
- Scenario B physical movement and secondary re-observation: **PASS — USER MANUAL VALIDATION**.
- Runtime truthfulness: **PASS at source/log level**; no failed dependency result was converted to success and no stale/new `REAL` claim was created.

No request count or latency is recorded here because those values were not included in the latest manual evidence.

## Evidence boundary

- Automated/backend evidence: compile, Windows build, executable process startup, runtime-support copy, Python dependency layout, and targeted Phase 3–5 regressions.
- User manual evidence: competition UI and controls, three inherited workflow outcomes, Scenario B movement/re-observation, Scenario C real Nemotron decision/action, and exit-button behavior.
- Not claimed: physical hardware execution, benchmark latency, or universal reproducibility outside the documented environment.

## Blockers

- None for the recorded Windows standalone validation scope.
