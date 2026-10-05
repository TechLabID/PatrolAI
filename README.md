# PatrolAI

**Autonomous Physical AI for Industrial Safety**

PatrolAI is a Unity-based industrial inspection prototype that closes the loop from visual observation to a validated safety action. It combines inherited OpenCV perception and mobile-inspector behavior with new Nebius Token Factory and NVIDIA Nemotron reasoning developed for the Nebius × NVIDIA Global AI Hackathon.

## Problem

Industrial patrols must distinguish normal conditions, confirmed hazards, and insufficient evidence. A detector alone cannot safely decide whether to continue, stop, or inspect again, while a language-model response cannot be trusted without a strict contract and policy validation.

## Solution

PatrolAI converts perception into a structured observation, requests a short safety decision from NVIDIA Nemotron through Nebius Token Factory, parses and validates the response, and maps it to a bounded physical-agent action. When evidence is insufficient, the agent can re-observe and verify before continuing.

```text
Unity industrial environment
  → camera / virtual sensor
  → OpenCV perception
  → build state as structured PatrolObservation
  → Nebius Token Factory
  → nvidia/Nemotron-3_5-Lightning
  → strict JSON decision
  → parser + validator
  → PatrolDecision
  → PatrolAction: CONTINUE / REOBSERVE / STOP
  → physical-agent state and mobile-inspector response
  → re-observe / multi-source fusion / temporal verification
```

## Core Architecture

- **Sense and perceive:** the inherited Unity/OpenCV pipeline produces observable evidence.
- **Reason:** `NebiusNemotronReasoningService` sends only structured observation state to Nebius Token Factory.
- **Validate:** `NemotronDecisionParser` and `NemotronDecisionValidator` treat model output as untrusted input. Invalid output becomes the explicit safe fallback `UNKNOWN / REOBSERVE`.
- **Act and verify:** the Phase 4 closed loop maps validated decisions to bounded `PatrolAction` and physical-agent states. Phase 5 Scenario B delegates re-observation to the inherited public inspection workflow instead of duplicating movement logic. Inherited multi-source fusion and temporal evaluation remain available for verification workflows.
- **Explain:** the competition evidence panel shows the actual runtime mode, provider, model, observation, decision, action, final state, and latency.

More detail is available in [the competition architecture](Assets/PatrolAI/Competition/PATROLAI_ARCHITECTURE.md).

## REAL Runtime Integration

- **Provider:** Nebius Token Factory
- **Endpoint family:** OpenAI-compatible chat completions
- **Model:** `nvidia/Nemotron-3_5-Lightning`
- **Output contract:** strict JSON Schema
- **Model setting for this short decision task:** thinking disabled with `chat_template_kwargs.enable_thinking=false`
- **Maximum completion tokens:** 512

The authenticated validation path has returned HTTP 200, `finish_reason=stop`, non-empty decision JSON, parser PASS, validator PASS, and a valid `PatrolDecision`. This proves the `REAL` path; it does not imply that every future run succeeds.

## Runtime Truthfulness

PatrolAI reports the path that actually ran:

- `REAL`: a current Nebius/NVIDIA response was parsed, validated, and used.
- `MOCK`: an offline test implementation produced the result.
- `FALLBACK`: live reasoning failed and the safe local policy was used.
- `LOCAL SAMPLE`: static/local data produced the result.
- `FAILED`: the intended operation failed without a successful result.
- `NOT VERIFIED`: no supporting runtime evidence exists yet.

A failed cloud call is never relabeled as `REAL`.

## Demo Scenarios

| Scenario | Observation | Expected validated flow | Agentic inspection |
|---|---|---|---|
| A — SAFE | `NORMAL / SUFFICIENT` | `SAFE → CONTINUE → CONTINUING` | No |
| B — REOBSERVE | `INDICATOR_UNCLEAR / INSUFFICIENT` | `UNKNOWN → REOBSERVE → REOBSERVING` | Yes; delegates to the inherited active re-observation workflow |
| C — HAZARD | `INDICATOR_RED / SUFFICIENT` | `DANGER → STOP → STOPPED` | No |

These expected flows are validation criteria, not prerecorded model answers. A `REAL` display requires a successful current request.

## Baseline Workflow Controls

The Phase 5 menu exposes inherited, validated workflows through a thin adapter:

- `RUN AGENTIC INSPECTION` → `VGPhase3AgentController.RunAgenticInspection()`
- `RUN MULTI SOURCE FUSION` → `VGPhase4FusionController.RunMultiSourceFusion()`
- `EVALUATE LATEST VERIFIED FUSION` → `VGPhase5TemporalController.EvaluateLatestFusion()`

These algorithms predate the hackathon work and are not claimed as new PatrolAI logic.

## Competition Evidence Overlay

The overlay keeps decision evidence visible without exposing credentials. It reports runtime mode, provider/model, observation, evidence quality, confidence, risk/decision, action, final state, request status, and latency. Baseline workflow details are context-sensitive and only report completion after the underlying controller does.

## Requirements

- Windows 10/11
- Unity `6000.6.3f1`
- Python 3.12 x64
- Python packages in `requirements-phase2.txt`
- A Nebius account with Token Factory access to `nvidia/Nemotron-3_5-Lightning`
- `NEBIUS_API_KEY` supplied through the process environment

## Setup

```powershell
git clone <PUBLIC_REPOSITORY_URL>
cd PatrolAI
py -3.12 -m venv .venv-phase2
.\.venv-phase2\Scripts\Activate.ps1
python -m pip install -r requirements-phase2.txt
$env:NEBIUS_API_KEY = "<YOUR_NEBIUS_API_KEY>"
```

For a persistent Windows user variable:

```powershell
[Environment]::SetEnvironmentVariable(
  "NEBIUS_API_KEY",
  "<YOUR_NEBIUS_API_KEY>",
  "User"
)
```

Never commit a real key, `.env` file, Authorization header, screenshot containing credentials, or diagnostic archive containing a key.

## Run in Unity

1. Open this folder in Unity `6000.6.3f1`.
2. Open `Assets/VisionGuard/Phase5/Scenes/VISIONGUARD_IndustrialRoom_Phase5_v01.unity`.
3. Enter Play Mode.
4. Use Scenario A, B, or C in the PatrolAI competition panel.
5. Confirm the evidence panel's runtime label before treating a result as live.

## Build from Source on Windows

1. Confirm the competition scene is enabled in Build Profiles: `Assets/VisionGuard/Phase5/Scenes/VISIONGUARD_IndustrialRoom_Phase5_v01.unity`.
2. Build for Windows Standalone x86_64 to `Builds/Windows/PatrolAI/PatrolAI.exe`.
3. The Editor-only post-build helper copies the required `agent/`, `vision/`, and `camera/` Python source plus `requirements-phase2.txt` beside the executable. It does not copy credentials or a Python environment.
4. In the executable directory, create `.venv-phase2` with Python 3.12 x64 and install the copied requirements.
5. Set `NEBIUS_API_KEY` in the process or user environment, then launch `PatrolAI.exe`.

As an alternative to the executable-relative environment, advanced users may set `PATROLAI_PYTHON` to the absolute path of a compatible Python 3.12 executable with the required packages installed.

The Windows executable and its runtime artifacts are intentionally excluded from the source repository. See [Windows build validation](Assets/PatrolAI/Competition/PATROLAI_WINDOWS_BUILD_VALIDATION.md) for the verified configuration and the separation between automated and manual evidence.

## Validation Evidence

- Nemotron parser/validator offline suite: **12/12 PASS**.
- Phase 4 closed-loop targeted suite: **10/10 PASS**.
- Phase 5 integrated targeted suite after UI/scenario work: **22/22 PASS**.
- Phase 4 live validation: one observed HTTP 200 call produced `DANGER / STOP`, final state `STOPPED`, in 1243 ms.
- Phase 5 live hazard validation: one observed HTTP 200 `REAL` call produced `DANGER / STOP`, final state `STOPPED`, in 1381 ms.
- Windows Standalone x86_64 build and executable launch: **PASS**.
- Manual standalone validation: competition UI, Agentic Inspection, Multi Source Fusion, Latest Verified Fusion, Scenario B re-observation, Scenario C `REAL / DANGER / STOP / STOPPED`, and `EXIT PATROLAI`: **PASS**.

The two latency values are individual validation observations, not a benchmark, SLA, or performance average. See [the demo scenario guide](Assets/PatrolAI/Scenarios/README_DEMO_SCENARIOS.md) and [Windows build validation](Assets/PatrolAI/Competition/PATROLAI_WINDOWS_BUILD_VALIDATION.md) for the documented evidence boundary.

## Significant Hackathon Updates

The industrial scene, OpenCV perception, mobile inspector, active re-observation, fusion/temporal workflows, and their existing movement logic are inherited from the protected `v0.1-patrolai-baseline`.

New competition work includes the PatrolAI identity and contracts, Nebius client, NVIDIA Nemotron structured reasoning, strict parsing and validation, safe fallback, Physical AI decision-to-action orchestration, deterministic scenarios, baseline workflow adapter, and competition evidence UI. The detailed separation is recorded in [PATROLAI_SIGNIFICANT_UPDATES.md](Assets/PatrolAI/Competition/PATROLAI_SIGNIFICANT_UPDATES.md).

## Limitations and Release Status

- The project is a simulated Physical AI prototype; no physical hardware is claimed.
- Cloud inference requires network access, a valid account/key, and current model availability.
- Model output remains probabilistic and is constrained by parser, validator, and safe fallback policy.
- Windows standalone validation proves the tested local configuration; a fresh clone still requires Unity, Python, package installation, a valid Nebius key, and network access.
- The public source snapshot has passed the public-readiness audit; secrets, provenance-unverified media, generated files, and local-only files are excluded.

**Public snapshot readiness: READY.** Repository publication is the next step; this does not mean the repository is already public.

## Third-Party Assets and Dependencies

- No raw Mixamo character, FBX, or reusable animation asset is tracked in the current Git tree or found by the Phase 6 history audit. `Assets/ThirdParty/Mixamo/` is intentionally ignored and must remain local-only if used.
- Unity packages are declared in `Packages/manifest.json`; Python packages are pinned in `requirements-phase2.txt`.
- Synthetic test fixtures are generated by repository tools and documented as synthetic.
- Inherited demo/reference media and the Unity template icon identified by the publication audit are excluded from the public source snapshot pending any separate provenance/license confirmation.
- Third-party components and assets remain subject to their respective licenses and are not relicensed by PatrolAI's MIT license.

## License

Project-owned PatrolAI source code is released under the top-level [MIT License](LICENSE), copyright (c) 2026 TechLabID. This license does not grant rights to third-party assets, packages, models, or other components governed by their own terms.

