# PatrolAI Public Readiness Audit

**Audit date:** 2026-10-04  
**Protected milestone:** `v0.7.2-20261004-patrolai-phase5d-ui-polish-pass`  
**Overall status:** **BLOCKED — DO NOT MAKE PUBLIC YET**

## Secret audit

- Tracked tree: PASS for the tested patterns (private keys, Bearer tokens, AWS access-key IDs, assigned `NEBIUS_API_KEY`, and common password/client-secret assignments).
- Staging area: no staged files at audit time.
- Git history/tags: PASS for the same high-risk patterns; no matching committed secret was found.
- Local untracked operator note: references the environment-variable name but no likely assigned secret was detected. It is ignored because it is not submission source.
- `docs/references/mix/Check model tersedia di APKEY Nebius.zip`: **BLOCKER**. A nested text file contains a likely assigned Nebius credential. The ZIP is untracked and now ignored, but it must be securely removed or sanitized before public packaging, screen sharing, or backup distribution.

The scan is a targeted repository audit, not a guarantee against every possible secret format. A fresh scan is required immediately before publication.

## Git/publication audit

- No Git remote is configured.
- No Phase 6 commit, tag, push, publication, or submission was performed.
- All requested protected milestone tags through `v0.7.2-20261004-patrolai-phase5d-ui-polish-pass` exist locally.
- Generated IDE files are ignored going forward. Existing tracked `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`, and `VisionGuardAI.sln` remain tracked until the owner deliberately removes them from the index; this audit does not rewrite that history.

## Asset and IP audit

| Material | Classification | Reason/action |
|---|---|---|
| Raw Mixamo FBX/characters/reusable animations | SAFE IN CURRENT GIT TREE/HISTORY | None found by path/extension history audit. `Assets/ThirdParty/Mixamo/` is ignored and must remain local-only. |
| Python-generated synthetic fixtures and annotated diagnostics | SAFE TO PUBLISH | Repository scripts/fixture documentation establish deterministic project generation. |
| `demo/evidence/Phase4_PASS.png`, `Phase5_PASS.png`, and the two tracked MP4s | LICENSE REVIEW REQUIRED | Finished project evidence is tracked, but the underlying scene/asset provenance is not fully documented. |
| `docs/references/RF90_Industrial_Electrical_Room_Static_Reference_v01.png` | LICENSE REVIEW REQUIRED | Source and publication rights are not documented in the repository. |
| `Assets/TutorialInfo/Icons/URP.png` | LICENSE/ATTRIBUTION REVIEW REQUIRED | Appears inherited from the Unity template; confirm applicable Unity terms and attribution requirements. |
| `docs/references/PatrolAI Industrial Inspection UI Spec.png` | LOCAL REFERENCE ONLY | Untracked, provenance not established, and explicitly ignored. |
| `docs/references/mix/` | LOCAL ONLY / CONTAINS SECRET BLOCKER | Do not publish; sanitize or remove securely. |
| Unity packages and Python dependencies | THIRD-PARTY DEPENDENCIES | Keep version declarations and comply with each upstream license; do not claim ownership. |

No top-level asset credits/provenance record currently resolves the items marked for review.

## Source license

No top-level `LICENSE`, `COPYING`, or `NOTICE` file exists. A public open-source license has not been selected. This is a release blocker; the audit intentionally does not choose a license for the owner.

## Windows build audit

Status: **NOT VERIFIED / BLOCKED**.

- No `Build/` or `Builds/` output exists.
- `ProjectSettings/EditorBuildSettings.asset` includes only `Assets/Scenes/SampleScene.unity`.
- The competition scene `Assets/VisionGuard/Phase5/Scenes/VISIONGUARD_IndustrialRoom_Phase5_v01.unity` is not in build settings.
- Project rules prohibit changing `ProjectSettings` in this pass, and authorized Unity Editor access was unavailable.

The owner must add the competition scene through the Unity Editor, produce a Windows standalone build, and validate scenarios/runtime truthfulness in that build before claiming reproducibility.

## Unity Recorder audit

- `com.unity.recorder` `5.1.7` is declared from the Unity registry.
- Repository code contains no direct `Unity.Recorder`/Recorder API reference.
- The package supports Editor Play Mode capture, but standalone compatibility is not proven by its presence.
- The package was not removed because this pass cannot change Packages and it may be used for demo capture.

## Evidence inventory

- Parser/validator offline suite: 12/12 PASS.
- Phase 4 closed-loop targeted suite: 10/10 PASS.
- Current integrated Phase 5 targeted suite: 22/22 PASS.
- Live Nemotron: HTTP 200, strict JSON emitted, `finish_reason=stop`, parser PASS, validator PASS.
- Physical AI: observed `REAL DANGER → STOP → STOPPED` path.
- Scenario/UI: manual Unity validation recorded by the project owner.
- Tracked media: Phase 4 and Phase 5 screenshots/videos under `demo/evidence/`.

## Track recommendation

**Physical AI** is the best fit. PatrolAI is an embodied simulated agent that senses an environment and changes behavior through a validated model decision. Because it has no physical hardware component, the final video must visibly show the key application modules in action rather than imply hardware execution.

## Release blockers

1. Select and add a top-level public-source license.
2. Securely remove or sanitize the local credential-bearing ZIP and run a fresh secret scan.
3. Resolve provenance/license status for inherited evidence, reference media, and Unity template material; add credits where required.
4. Add the competition scene to build settings and validate a reproducible Windows build through authorized Unity access.
5. Record/upload the public demo, finalize URLs and Devpost text, and complete the final checklist/tag manually.

