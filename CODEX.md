# PATROLAI — CODEX / RF90 DEVELOPMENT RULES

## Project Purpose

**PatrolAI** is an autonomous Physical AI safety patrol and inspection system developed for the **Nebius × NVIDIA Global AI Hackathon 2026**.

PatrolAI is forked from a stable VisionGuardAI baseline, but is now an independent project, repository, product identity, and competition submission.

Primary competition direction:

**Sense → Perceive → Reason → Decide → Act**

Target track:

**Physical AI Track**

Core competition technologies:

- Unity simulation
- Computer vision / OpenCV perception
- Nebius Token Factory and/or Nebius AI Cloud
- At least one NVIDIA open-source model
- NVIDIA Nemotron as the preferred reasoning model unless explicitly changed
- Autonomous patrol / inspection behavior

---

## RF90 Core

Mandatory principle:

**Minimum Change + Additive Development + Minimum Necessary Test + Protect Stable Baseline.**

- Apply RF90 to reference-based visual work.
- Static match first where visual reference matching is required.
- No later animation/simulation phase before `PASS_STATIC` or `USER_APPROVED_STATIC` when RF90 visual matching applies.
- Approved assets, scenes, features, and behavior are SEALED.
- New work must be additive, minimal, regression-safe, and token-efficient.
- Never redesign, regenerate, refactor, rename, or improve unrelated validated work.

---

## Current PatrolAI Baseline

Git repository:

`PatrolAI`

Baseline tag:

`v0.1-patrolai-baseline`

The baseline was created from the stable VisionGuardAI project and is now treated as the initial stable PatrolAI foundation.

Existing validated functionality inherited from the baseline includes:

- RF90 industrial environment
- OpenCV perception pipeline
- agentic active re-observation workflow
- mobile inspector movement
- inspection-pose behavior
- validated status overlay/UI
- existing detection and reasoning pipeline
- existing Unity scene and runtime behavior

Treat inherited PASS behavior as immutable unless the user explicitly authorizes modification.

---

## Competition Development Strategy

All Nebius × NVIDIA competition functionality should be implemented **additively** on top of the PatrolAI baseline.

Preferred structure:

`Assets/PatrolAI/`

Suggested isolated modules:

- `Assets/PatrolAI/Nebius/`
- `Assets/PatrolAI/NVIDIA/`
- `Assets/PatrolAI/PhysicalAI/`
- `Assets/PatrolAI/Competition/`
- `Assets/PatrolAI/UI/`
- `Assets/PatrolAI/Scenarios/`

Do not move or rename inherited working assets merely to make the directory structure cleaner.

New PatrolAI modules may reference stable baseline systems through minimal interfaces.

---

## Nebius Requirement

PatrolAI competition features must include a genuine runtime integration with:

- **Nebius Token Factory**, and/or
- **Nebius AI Cloud**

A fake UI label, hard-coded result, prerecorded output, or simulated API response must not be presented as a real Nebius inference call.

When a real Nebius call is required:

1. send relevant perception/sensor state;
2. receive model inference;
3. parse the response;
4. expose the result to PatrolAI;
5. allow the system to use the result in its reasoning or action pipeline.

Credentials and API keys must never be committed to Git.

Use environment variables, local configuration excluded by `.gitignore`, or another secure mechanism.

---

## NVIDIA Model Requirement

PatrolAI must use at least one NVIDIA open-source AI model according to the hackathon requirement.

Preferred reasoning model:

**NVIDIA Nemotron**

Nemotron should serve a meaningful role in the system.

Preferred use:

`Perception → structured scene state → Nemotron risk reasoning → structured safety decision → autonomous response`

Do not use Nemotron merely to generate decorative text.

Its output should contribute to actual PatrolAI reasoning, decision-making, explanation, prioritization, or action selection.

---

## Physical AI Architecture

Target architecture:

```text
Virtual Industrial Environment
        ↓
Virtual Camera / Sensors
        ↓
OpenCV / Perception
        ↓
Structured Observation
        ↓
Nebius Token Factory
        ↓
NVIDIA Nemotron
        ↓
Risk / Situation Reasoning
        ↓
PatrolAI Decision
        ↓
SAFE / WARNING / DANGER
        ↓
Continue / Reobserve / Stop / Alert / Reroute / Inspect
```

The competition demo should demonstrate a clear closed loop:

**sense → reason → act**

Avoid turning PatrolAI into a simple chatbot or dashboard-only application.

---

## Physical AI Simulation

PatrolAI may use a Unity-based simulated industrial environment.

Simulation should demonstrate meaningful autonomous behavior such as:

- patrol;
- observation;
- hazard detection;
- re-observation;
- risk reasoning;
- autonomous decision;
- movement or route adaptation;
- warning generation;
- stop/continue decisions;
- inspection behavior.

Possible competition scenarios include:

- PPE violation;
- unsafe proximity to active machinery;
- blocked patrol route;
- suspicious machine indicator;
- incomplete visual evidence requiring re-observation;
- normal safe operating condition.

Scenario logic must remain deterministic enough for a reliable competition demonstration unless explicitly designed otherwise.

---

## Additive + Minimum Change

Prefer new isolated:

- scripts;
- components;
- ScriptableObjects;
- scenes;
- prefabs;
- UI panels;
- service clients;
- adapters;
- tests;
- configuration files.

Do NOT modify a sealed component merely for convenience.

If a requested feature requires changing a sealed baseline file:

1. STOP before editing.
2. Report the exact file/component.
3. Explain why the change is unavoidable.
4. Propose the smallest possible change.
5. Wait for explicit user authorization.

No unrelated cleanup, refactoring, redesign, regeneration, package changes, project-setting changes, or architecture changes.

---

## Protected Baseline Scope

Unless explicitly authorized, do not modify inherited stable systems including:

- validated Phase 3 scene;
- validated mobile inspector movement;
- existing re-observation behavior;
- existing OpenCV perception behavior;
- validated overlay/UI behavior;
- stable baseline controller logic;
- shared/core architecture;
- `Packages/manifest.json`;
- `Packages/packages-lock.json`;
- Unity Project Settings.

Do not regenerate or rebuild stable scenes for unrelated competition work.

If PatrolAI needs a different competition scene, prefer creating a new scene that reuses stable assets.

---

## Product Identity Rule

PatrolAI is an independent project.

New user-facing competition material should use:

**PatrolAI**

Do not unnecessarily rename inherited internal scripts, filenames, namespaces, GUID-linked assets, or serialized components solely because they still contain VisionGuard/VG naming.

Internal legacy names may remain when renaming would risk breaking Unity references.

User-visible competition branding may be changed separately.

---

## Test Efficiency Rule

Do not run unnecessary regression tests.

Use minimum validation appropriate to the change:

- compile/syntax check;
- targeted test for the changed feature;
- API connectivity test when Nebius integration changes;
- structured-response parsing test when Nemotron integration changes;
- quick smoke test only for directly affected stable systems.

Run full regression only for:

- core/shared architecture changes;
- cross-system changes;
- major release validation;
- explicit user request;
- changes that materially affect the sealed baseline.

Avoid unnecessary:

- Windows builds;
- full Unity rebuilds;
- repeated PlayMode runs;
- repeated screenshots;
- recaptures;
- full repository scans;
- long reports.

---

## Token / Agent Efficiency

Inspect only relevant files.

Avoid broad repository exploration unless required to understand a dependency.

Do not repeatedly inspect unaffected systems.

Keep implementation reports concise.

After the requested feature and minimum validation are complete, stop.

Do not continue polishing unrelated systems.

---

## Unity Agentic Access

Target Unity version:

**Unity 6.6 / 6000.6.3f1**

unless explicitly changed by the user.

Coding agents may edit:

- source code;
- configuration;
- tests;
- documentation;
- project files.

Direct agent operation, invocation, or control of Unity Editor or Unity Offering must use **Unity Authorized Agentic Access**.

Do not use unofficial MCP/plugin/CLI agentic control of Unity Editor unless confirmed as an authorized Unity pathway.

When authorized Editor access is unavailable:

- edit project files;
- perform appropriate offline validation;
- provide concise manual Unity validation steps to the user.

---

## Git Safety

Current stable baseline:

`v0.1-patrolai-baseline`

Git backup, commits, and tagging are manual by the user unless explicitly requested.

Do not automatically:

- commit;
- tag;
- push;
- reset;
- restore;
- revert;
- rewrite history;
- remove branches;
- stage unrelated files.

Never disturb the stable baseline without explicit authorization.

Before major competition milestones, recommend a new user-created Git tag.

---

## Secrets and Public Repository Safety

The hackathon repository may eventually be public.

Never commit:

- Nebius API keys;
- access tokens;
- credentials;
- private URLs;
- `.env` secrets;
- personal account information.

Before public release, inspect configuration and documentation for accidental credentials.

Use placeholders such as:

`NEBIUS_API_KEY`

instead of real secrets.

---

## Competition Scope Discipline

Implement only the requested feature.

Examples:

- Nebius task → do not redesign environment.
- Nemotron task → do not alter unrelated movement.
- UI task → do not modify perception logic.
- perception task → do not redesign patrol route.
- patrol task → do not replace stable OpenCV pipeline.
- competition branding → do not rename internal Unity assets unnecessarily.
- visual polish → do not modify validated controllers.
- README task → do not modify runtime code.

Competition requirements take priority only when a change is explicitly requested.

---

## Significant Update Rule

PatrolAI originated from an existing stable system.

Therefore, competition development must clearly document **significant updates created during the Nebius × NVIDIA hackathon period**.

Maintain a concise record of new competition features, for example:

- Nebius Token Factory integration;
- NVIDIA Nemotron reasoning;
- Physical AI patrol agent;
- autonomous risk assessment;
- new competition scenarios;
- new action-selection logic;
- Nebius/NVIDIA architecture;
- competition-specific UI/dashboard;
- new demo workflow.

Do not falsely claim inherited baseline features as newly developed during the hackathon.

---

## Demo Reliability Rule

Competition demo functionality should prioritize:

**working > impressive but fragile**

Demo scenarios should be repeatable.

Do not introduce unnecessary randomness, unstable dependencies, or last-minute architectural changes.

The final demonstration should make it obvious when PatrolAI is:

1. observing;
2. detecting;
3. calling Nebius/NVIDIA;
4. reasoning;
5. deciding;
6. acting.

---

## Development Gate

`v0.1-patrolai-baseline` is the protected starting point.

All Nebius × NVIDIA development begins **after** this baseline.

Future phases proceed only when explicitly requested by the user.

Recommended progression:

```text
v0.1 — PatrolAI Baseline
   ↓
Phase 1 — PatrolAI Competition Identity + Architecture
   ↓
Phase 2 — Nebius Token Factory Connectivity
   ↓
Phase 3 — NVIDIA Nemotron Safety Reasoning
   ↓
Phase 4 — Physical AI Closed Loop
   ↓
Phase 5 — Competition Scenarios
   ↓
Phase 6 — Demo / Submission Polish
```

Do not start later phases automatically.

---

**RF90: Protect validated assets and behavior. PatrolAI competition development must remain additive, minimal, regression-safe, token-efficient, and demonstrably integrated with Nebius and NVIDIA.**