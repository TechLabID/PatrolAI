# PatrolAI

**Autonomous Physical AI for Industrial Safety**

PatrolAI is a Unity-based industrial inspection prototype that closes the loop from visual observation to a validated safety action. It combines inherited OpenCV perception and mobile-inspector behavior with new Nebius Token Factory and NVIDIA Nemotron reasoning developed for the Nebius × NVIDIA Global AI Hackathon.

## Demo Video

https://youtu.be/cFn8F_BaJTM

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

These are source/Editor and build-machine requirements. A prepared Windows build includes executable-relative `.patrolai-python/python.exe` (verified Python `3.12.4`) and its pinned packages; the destination machine does not require a separate Python installation. `NEBIUS_API_KEY` remains external.

- Windows 10/11
- Unity `6000.6.3f1`
- Python 3.12 x64
- Python packages in `requirements-phase2.txt`
- A Nebius account with Token Factory access to `nvidia/Nemotron-3_5-Lightning`
- `NEBIUS_API_KEY` supplied through the process environment

## Setup

```powershell
git clone https://github.com/TechLabID/PatrolAI.git
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
4. Use Scenario A, B, C, or AUTO PATROL from the PatrolAI competition panel.
5. Confirm the evidence panel's runtime label before treating a result as live.

## Build from Source on Windows

1. Confirm the competition scene is enabled in Build Profiles: `Assets/VisionGuard/Phase5/Scenes/VISIONGUARD_IndustrialRoom_Phase5_v01.unity`.
2. Build for Windows Standalone x86_64 to `Builds/Windows/PatrolAI/PatrolAI.exe`.
3. Complete the project Python setup above before building. The post-build helper copies `agent/`, `vision/`, `camera/`, and `requirements-phase2.txt`, creates an empty `exchange/`, and provisions a portable `.patrolai-python/` interpreter with the exact pinned packages. Provisioning validates Python 3.12 x64 and fails the build step if prerequisites or runtime imports fail.
4. Launch `PatrolAI.exe` with `NEBIUS_API_KEY` supplied through the environment. No Python override, executable-relative venv creation, or reservation-marker deletion is needed for a normal prepared build. The complete build directory can be relocated; the destination machine does not require a Python installation.

Discovery order is explicit: a configured `PATROLAI_PYTHON` absolute executable, executable-relative `.venv-phase2/Scripts/python.exe`, then the provisioned `.patrolai-python/python.exe`. An invalid explicit override fails instead of silently selecting another interpreter. PATH, the registry, and unrelated Python installations are not searched. See [standalone runtime and reservation lifecycle](docs/Standalone_Runtime.md) for provisioning, prerequisites, and fail-safe recovery.

The Windows executable and its runtime artifacts are intentionally excluded from the source repository. See [Windows build validation](Assets/PatrolAI/Competition/PATROLAI_WINDOWS_BUILD_VALIDATION.md) for the verified configuration and the separation between automated and manual evidence.

## Validation Evidence

- Nemotron parser/validator offline suite: **12/12 PASS**.
- Phase 4 closed-loop targeted suite: **10/10 PASS**.
- Phase 5 integrated targeted suite at Phase 10A: **24/24 PASS**.
- Phase 4 live validation: one observed HTTP 200 call produced `DANGER / STOP`, final state `STOPPED`, in 1243 ms.
- Phase 5 live hazard validation: one observed HTTP 200 `REAL` call produced `DANGER / STOP`, final state `STOPPED`, in 1381 ms.
- Windows Standalone x86_64 build and executable launch: **PASS**.
- Manual standalone validation: competition UI, Agentic Inspection, Multi Source Fusion, Latest Verified Fusion, Scenario B re-observation, Scenario C `REAL / DANGER / STOP / STOPPED`, and `EXIT PATROLAI`: **PASS**.
- Phase 10A fresh-clone validation at `v0.8.5`: **PASS — previously owner-confirmed**. A clean Windows rebuild used bundled Python `3.12.4` without `PATROLAI_PYTHON`; standalone D Run #1 and Run #2 passed without manual reservation cleanup. Primary OpenCV evidence, physical re-observation in Unity simulation, secondary observation, and `COMPLETE / VERIFIED` passed; the safety envelope produced `STOP`, final state `STOPPED`, and reservation release passed. Runtime Truthfulness was preserved.
- Phase 8B cue/fairness audit: **16/16 held-out cases and 24/24 original benchmark cases audited** using stored data/results only; **0 new inference calls** and **0 benchmark reruns**.
- Phase 8B freeze/integrity verification: **9/9 hashes MATCH** for the held-out freeze manifest; no few-shot contamination was found in the inspected production prompt/client path.
- Phase 8B held-out no-explicit-cue subset: Rule v2, raw Nemotron, and hybrid were each **8/8 action-correct**. In the conflicting no-cue subset, all three were **2/2 action-correct**.
- Phase 8B fair-demo audit: **NO FAIR DEMO CANDIDATE** met every predeclared criterion for an isolated Rule-v2-vs-Nemotron superiority demo.

The two latency values are individual validation observations, not a benchmark, SLA, or performance average. See [the demo scenario guide](Assets/PatrolAI/Scenarios/README_DEMO_SCENARIOS.md) and [Windows build validation](Assets/PatrolAI/Competition/PATROLAI_WINDOWS_BUILD_VALIDATION.md) for the documented evidence boundary.

## Significant Hackathon Updates

The industrial scene, OpenCV perception, mobile inspector, active re-observation, fusion/temporal workflows, and their existing movement logic are inherited from the protected `v0.1-patrolai-baseline`.

New competition work includes the PatrolAI identity and contracts, Nebius client, NVIDIA Nemotron structured reasoning, strict parsing and validation, safe fallback, Physical AI decision-to-action orchestration, deterministic scenarios, baseline workflow adapter, competition evidence UI, held-out comparative evaluation, and the Phase 8B cue/fairness audit. The detailed separation is recorded in [PATROLAI_SIGNIFICANT_UPDATES.md](Assets/PatrolAI/Competition/PATROLAI_SIGNIFICANT_UPDATES.md).

## Limitations and Release Status

- The project is a simulated Physical AI prototype; no physical hardware is claimed.
- Cloud inference requires network access, a valid account/key, and current model availability.
- Model output remains probabilistic and is constrained by parser, validator, and safe fallback policy.
- Phase 10A verified a fresh-clone Windows rebuild and two standalone D runs in the tested configuration. Building from source requires Unity and the documented Python/package setup; running the complete prepared build requires no separate Python installation. A valid external Nebius key and network access remain required for live inference; universal reproducibility is not claimed.
- The public source snapshot has passed the public-readiness audit; secrets, provenance-unverified media, generated files, and local-only files are excluded.
- Phase 8B found explicit or action-implying cues in **8/16 held-out cases** under the conservative rubric. Both stored raw-model improvements over Rule v2 in the conflicting held-out subset occurred in cued cases.
- PatrolAI therefore does **not** claim that Nemotron universally outperforms Rule v2 or that AI necessity has been established by this pilot.

**Public repository status: PUBLISHED.** This snapshot has passed the public-readiness audit. Repository: [TechLabID/PatrolAI](https://github.com/TechLabID/PatrolAI). Current verified baseline: commit `1bcdfa4`, tag `v0.8.5-20261014-standalone-python-reservation-fix-pass`.

## Third-Party Assets and Dependencies

- No raw Mixamo character, FBX, or reusable animation asset is tracked in the current Git tree or found by the Phase 6 history audit. `Assets/ThirdParty/Mixamo/` is intentionally ignored and must remain local-only if used.
- Unity packages are declared in `Packages/manifest.json`; Python packages are pinned in `requirements-phase2.txt`.
- Synthetic test fixtures are generated by repository tools and documented as synthetic.
- Inherited demo/reference media and the Unity template icon identified by the publication audit are excluded from the public source snapshot pending any separate provenance/license confirmation.
- Third-party components and assets remain subject to their respective licenses and are not relicensed by PatrolAI's MIT license.

## Why Nemotron?

PatrolAI uses a **hybrid safety architecture**: deterministic rules remain strong for clear predefined conditions, while Nemotron provides an additional contextual-reasoning path for structured and free-form evidence. Model output is never given unconstrained control; parser/validator checks and a deterministic safety envelope bound execution.

### Held-out pilot results

| Configuration | Held-out action correctness |
|---|---:|
| Rule v2 (bounded deterministic rules) | 14/16 (87.50%) |
| Raw Nemotron | 14/16 (87.50%) |
| Hybrid Nemotron + Safety Envelope | 15/16 (93.75%) |

Across all five conflicting held-out cases, Rule v2 was action-correct on 3/5, while raw Nemotron and hybrid were 5/5. However, Phase 8B found that **both raw-model improvements over Rule v2 in that conflicting subset occurred in cases containing explicit or action-implying STOP guidance**. For the **no-explicit-cue held-out subset**, Rule v2, raw Nemotron, and hybrid were all **8/8 action-correct**; in the conflicting no-cue subset, all three were **2/2**.

That means this pilot supports a narrower conclusion:

- **SUPPORTED:** deterministic rules remain strong for clear predefined cases.
- **LIMITED:** Nemotron can participate in contextual and multi-source reasoning within the PatrolAI architecture, and the hybrid design can add operational flexibility.
- **DO NOT CLAIM:** Nemotron universally outperforms Rule v2.
- **DO NOT CLAIM:** AI necessity has been established by this pilot.

The Phase 8B audit covered **16/16 held-out cases and 24/24 original benchmark cases** using stored data/results only. No new inference or benchmark rerun was performed. The held-out freeze manifest passed **9/9 hash checks**. No few-shot contamination was found in the inspected production prompt/client path. Model-training contamination and independent preregistration of the original benchmark remain **NOT VERIFIED**.

Stored historical evidence for the Phase 7B original-result checks and held-out 9/9 freeze/hash checks is available in [Evaluation/Evidence](Assets/PatrolAI/Evaluation/Evidence/README.md). A fresh clone can verify these stored results/hashes locally with the included read-only Python verifier; no benchmark rerun or live credentials are required. The three historical artifacts are byte-for-byte copies; no sanitization or changes to decisions, scores, labels, or hashes were performed.

Detailed methodology and case-level cue classifications are documented in [PATROLAI_CUE_FAIRNESS_AUDIT.md](Assets/PatrolAI/Competition/PATROLAI_CUE_FAIRNESS_AUDIT.md). The broader action/risk evaluation remains in [PATROLAI_AI_NECESSITY_EVALUATION.md](Assets/PatrolAI/Competition/PATROLAI_AI_NECESSITY_EVALUATION.md).

### HC09 demo status

AUTO PATROL using HC09 has **manual Unity validation PASS (owner reported)**. Observation Source: **LOCAL SAMPLE — HELD-OUT HC09**; Environment: **UNITY SIMULATION**; AI Runtime: **REAL — Nebius Token Factory** only after successful current inference; Model: `nvidia/Nemotron-3_5-Lightning`.

HC09 remains useful as a **functional demonstration** of PatrolAI executing a contextual safety scenario. It is **not** used as isolated evidence that Nemotron is superior to the deterministic baseline, because the frozen HC09 context contains explicit STOP-oriented guidance. The reviewer-facing evidence also states that the reported personnel hazard is structured scenario evidence and is not a live industrial-camera detection or necessarily rendered visually in the Unity scene.

The Phase 8B mechanical demo-candidate audit found **NO FAIR DEMO CANDIDATE** that met every predeclared criterion for an isolated Rule-v2-vs-Nemotron superiority demo. Final submission framing therefore emphasizes the closed-loop Physical AI workflow, bounded re-observation, runtime truthfulness, and the hybrid safety architecture rather than an unsupported superiority claim.

**External label review: PENDING.** Independent academic/practitioner feedback will be reported as feedback, not as safety certification or deployment endorsement.

## License

Project-owned PatrolAI source code is released under the top-level [MIT License](LICENSE), copyright (c) 2026 TechLabID. This license does not grant rights to third-party assets, packages, models, or other components governed by their own terms.
