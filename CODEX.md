# VISIONGUARD AI — CODEX / RF90 DEVELOPMENT RULES

## RF90 Core
Mandatory principle: **Minimum Change + Additive Development + Minimum Necessary Test + Protect Stable Baseline.**

- Apply RF90 to reference-based visual work.
- Static match first; no later animation/simulation phase before `PASS_STATIC` or `USER_APPROVED_STATIC`.
- Approved assets, scenes, features, and behavior are SEALED.
- New work must be additive, minimal, regression-safe, and token-efficient.
- Never redesign, regenerate, refactor, or improve unrelated validated work.

## Current Sealed Baseline
Tag: `v0.3-phase3-agentic-reobservation-pass`

Validated:
- Phase 1A — RF90 static environment approved.
- Phase 2 — OpenCV perception pipeline PASS.
- Phase 3 — Agentic Active Re-observation runtime PASS.
- Primary: `INDICATOR_RED / INSUFFICIENT`.
- Decision: `REQUEST_REOBSERVATION`.
- `VG_MOBILE_01` moves to inspection pose.
- Secondary: `INDICATOR_RED / SUFFICIENT`.
- Final: `VERIFIED / INDICATOR_RED`.
- Existing Phase 3 status overlay is validated.

Treat all already-PASS behavior as immutable unless the user explicitly authorizes a change.

## Additive + Minimum Change
Prefer new isolated scripts, assets, materials, and components.

Do NOT modify a sealed component for convenience.

If the requested feature requires a sealed-file change:
1. STOP before editing.
2. Report the exact file/component.
3. Explain why the change is unavoidable.
4. Wait for explicit user authorization.

No unrelated cleanup, refactoring, redesign, regeneration, renaming, dependency/package changes, project-setting changes, or architecture changes.

## Phase 3 Protected Scope
Unless explicitly authorized, do NOT modify:
- `Assets/VisionGuard/Phase3/Scenes/VISIONGUARD_IndustrialRoom_Phase3_v01.unity`
- `Assets/VisionGuard/Phase3/Scripts/VGPhase3AgentController.cs`
- existing Phase 3 overlay/UI behavior
- validated mobile-inspector movement/re-observation behavior
- Phase 2 OpenCV perception behavior
- validated agent policy/reasoning behavior
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- Unity Project Settings
- shared/core architecture

Do not regenerate the Phase 3 scene for unrelated work.

## Test Efficiency Rule
Do not run unnecessary regression tests.

Use minimum validation:
- compile/syntax check;
- targeted test for the changed feature;
- quick smoke test only for directly affected stable systems.

Full regression only for core/shared changes, cross-system changes, major release validation when necessary, or explicit user request.

Avoid unnecessary rebuilds, recaptures, screenshots, PlayMode/regression runs, and long reports.

## Token Efficiency
Inspect only relevant files. Avoid broad repository exploration and repeated unaffected tests. Keep reports concise. Stop when the requested task and minimum validation are complete.

## Unity 2026 Agentic Access
Target: **Unity 6.6 / 6000.6.3f1** unless explicitly changed.

Coding agents may edit source, config, tests, docs, and project files.

Direct agent operation/control of Unity Editor or Unity Offering must use **Unity Authorized Agentic Access**. Do not use unofficial MCP/plugin/CLI agentic control unless confirmed as authorized.

When authorized Editor access is unavailable, edit files and perform appropriate offline/targeted validation, then leave Unity Editor/runtime validation to the user.

## Git Safety
Git backup/tagging is manual by the user unless explicitly requested.

Do not automatically commit/tag, destructively reset/restore/revert, or stage unrelated files. Never disturb a validated baseline without explicit authorization.

## Scope Discipline
Implement only the requested feature.

- Cosmetic task → do not alter logic.
- UI task → do not alter perception/agent behavior.
- Perception task → do not redesign environment.
- Additive visual polish → do not alter validated controller, movement, overlay, packages, or project settings.

## Development Gate
Phase 3 is PASS and sealed at `v0.3-phase3-agentic-reobservation-pass`.

Future Phase 3 polish must preserve this behavior. Later phases begin only with explicit user authorization.

**RF90: Protect validated assets and behavior. Changes must be additive, minimal, regression-safe, and token-efficient.**
