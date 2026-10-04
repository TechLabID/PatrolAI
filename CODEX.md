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

## Runtime Truthfulness Rule

PatrolAI competition development must preserve strict truthfulness about runtime execution.

### Runtime Status Definitions

Use the following status labels consistently:

- `REAL`
  - A genuine runtime call was made to Nebius Token Factory and/or Nebius AI Cloud.
  - The configured NVIDIA model actually processed the request and returned the response used by PatrolAI.

- `MOCK`
  - Output was produced by a local mock/test implementation.
  - No live Nebius/NVIDIA inference occurred.

- `FALLBACK`
  - The intended live runtime path failed or was unavailable.
  - PatrolAI used an explicitly defined fallback path instead.

- `LOCAL SAMPLE`
  - Output was produced from local/sample/static data.
  - No live Nebius/NVIDIA runtime call occurred.

- `FAILED`
  - The intended live operation failed and no successful real inference result was produced.

### No Fake Success

Never replace or conceal a failed Nebius/NVIDIA runtime call with:

- hard-coded success data;
- mock output presented as live output;
- local sample output presented as cloud inference;
- fallback output presented as successful NVIDIA inference;
- prerecorded or cached output presented as a current runtime result.

If a real Nebius/NVIDIA call fails, the system must clearly expose the real status:

`FAILED`, `FALLBACK`, `MOCK`, or `LOCAL SAMPLE`

as appropriate.

Never display `REAL` unless a genuine live runtime inference succeeded.

### Demo Integrity

Competition screenshots, recordings, logs, README claims, Devpost descriptions, and demo videos may claim successful Nebius/NVIDIA runtime integration only when the `REAL` path actually succeeds.

Mock, fallback, and local-sample paths must remain visibly and logically distinguishable from real inference.

For competition evidence, prefer displaying:

- runtime provider;
- NVIDIA model ID;
- request status;
- response status;
- inference mode;
- decision result;
- timing/latency where useful.

Never expose API keys, Authorization headers, credentials, or secrets while providing runtime evidence.

### Logging

Where runtime status is shown, use explicit labels such as:

- `RUNTIME: REAL`
- `RUNTIME: FAILED`
- `RUNTIME: FALLBACK`
- `RUNTIME: MOCK`
- `RUNTIME: LOCAL SAMPLE`

Logs must reflect the actual execution path.

A failed real request followed by a fallback must not be logged simply as success.

Preferred representation:

`NEBIUS_RUNTIME: FAILED`
`FALLBACK_RUNTIME: LOCAL SAMPLE`

or equivalent.

### Competition Development Priorities

For PatrolAI Nebius × NVIDIA development, prioritize:

1. Prove real runtime Nebius + NVIDIA integration.
2. Audit public-repository licenses, especially Unity assets and third-party content.
3. Produce a reproducible Windows build.
4. Prepare visible runtime evidence for the demo video under three minutes.
5. Re-check the current Devpost track definition before final submission.
6. Collect concrete Nebius/NVIDIA feedback during development.
7. Preserve Git history and tags demonstrating significant updates made during the hackathon period.

### Enforcement

Coding agents must never silently downgrade a requested live integration to mock, fallback, or local sample.

If live execution is unavailable:

1. report the failure;
2. preserve the failure evidence where useful;
3. use fallback only if explicitly implemented;
4. label fallback truthfully;
5. do not claim the competition runtime requirement has passed.

**Competition runtime PASS requires a demonstrable `REAL` Nebius/NVIDIA execution path.**

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

## Third-Party Asset / Mixamo Publication Rule

Before any public GitHub release or hackathon submission:

1. Run a third-party asset/IP scan.
2. Adobe Mixamo may be used locally in Unity and in finished End Uses
   such as Windows builds, APKs, demos, screenshots, and videos.
3. Do NOT publish raw/reusable Mixamo content in the public repository,
   including:
   - Mixamo FBX animation files
   - Mixamo character files
   - Extracted/reusable animation clips derived from Mixamo
4. Keep Mixamo assets in:
   Assets/ThirdParty/Mixamo/
5. Exclude that directory from the public repository via `.gitignore`.
6. Document the Mixamo dependency/credit in README when applicable.
7. Before submission/publication, verify that no restricted third-party
   raw assets are present in Git history, Releases, or downloadable ZIPs.
8. Finished application builds may contain Mixamo as part of the End Use,
   subject to the applicable Adobe/Mixamo terms.

Never remove working local Mixamo assets merely to make the repository
public; separate the distributable source from restricted third-party assets.

## Token Efficiency & Execution Rule

For every task:

1. Use the minimum context, explanation, and output necessary to complete the task correctly.
2. Do not repeat project rules already defined in this `CODEX.md`.
3. Treat rules in this file as persistent and authoritative unless the user explicitly overrides them.
4. Focus each session prompt only on:
   - current task,
   - required changes,
   - acceptance criteria.
5. Do not restate known project history unless required for implementation.
6. Preserve stable features and approved baselines.
7. Prefer additive changes over redesign or refactoring.
8. Do not modify unrelated files.
9. Perform only the validation necessary to establish correctness and prevent regression.
10. Final reporting should be concise:
    - PASS
    - FAIL
    - NOT VERIFIED
11. Never claim PASS for anything not actually verified.
12. If a requirement conflicts with a higher-priority project rule, preserve the higher-priority rule and use the safest valid alternative.
13. Avoid unnecessary exploration, repeated file reads, repeated searches, verbose reasoning, and duplicate validation.
14. Stop when all acceptance criteria are satisfied. Do not continue adding improvements that were not requested.


### Priority Order

1. Safety, license, IP, and platform rules
2. Existing project-specific non-negotiable rules
3. Preserve stable / approved baseline
4. Current task requirements
5. Acceptance criteria
6. Optional cleanup or optimization

### Core Principle

**Minimum necessary work, minimum necessary tokens, maximum correctness.**




# WINNER-LEVEL PROJECT RULE

Version: 1.0  
Scope: Default rule for all projects unless explicitly overridden by project-specific instructions.

---

## 1. Core Principle

Do not build a project merely to function, qualify, or be submitted.

Build it from the beginning so it can produce **credible, measurable, winner-level evidence**.

Use this working formula:

**Important Problem × AI Necessity × Autonomous Action × Real Evidence × Measurable Impact × Excellent Demo**

If one factor is weak, the overall project becomes weaker.

---

## 2. Six Questions Before Major Coding

Before large implementation work begins, answer these six questions:

1. **What important real-world problem is being solved?**
2. **Who specifically experiences the problem?**
3. **Why is AI necessary to the core solution rather than decorative?**
4. **What does the system autonomously sense, understand, decide, act on, observe, or improve?**
5. **Which measurable metrics will prove that the solution works?**
6. **What single demo moment will let judges, users, researchers, or stakeholders understand the value quickly?**

If these six answers are unclear, improve the project concept before adding major features.

---

## 3. AI Must Be Load-Bearing

If the project uses AI, AI should perform an essential role.

Avoid:

**Input → Prompt → LLM → Text Output**

when the project can support a stronger AI-native workflow.

Prefer, when appropriate:

**Sense → Understand → Decide → Act → Observe Result → Learn / Repeat**

AI should meaningfully contribute to one or more of the following:

- perception
- reasoning
- prediction
- adaptation
- planning
- decision making
- autonomous action
- personalization
- optimization
- re-observation
- feedback
- continuous improvement

If removing AI does not materially affect the core product, reconsider the AI role.

---

## 4. Start From the Problem, Not the Technology

Do not begin with:

> We want to use AI, Unity, OpenCV, NVIDIA, Gemini, LLM, drone, robot, or another technology.

Begin with:

> A specific group has a specific problem that causes measurable cost, delay, risk, inefficiency, or lost opportunity.

Then select the minimum technologies required to solve it well.

Technology must support the problem, not define the problem.

---

## 5. Define Success Metrics Early

Before large-scale implementation, define measurable success criteria.

Possible metrics include:

- accuracy
- precision / recall
- response time
- task completion rate
- reliability
- energy consumption
- cost reduction
- time reduction
- throughput improvement
- queue reduction
- learning gain
- mastery improvement
- intervention success
- failure recovery
- user adoption
- retention
- active users
- transactions
- pilot users
- deployment count
- runtime AI calls
- autonomous decisions
- successful missions
- incident prevention
- before-vs-after improvement

Use metrics appropriate to the project.

Do not invent numbers.

---

## 6. Evidence Is Mandatory

Strong projects must produce verifiable evidence.

Examples:

- automated tests
- benchmark results
- experiment logs
- runtime logs
- screenshots
- videos
- real API calls
- model responses
- telemetry
- user testing
- pilot deployment
- usage statistics
- transaction records
- before/after measurements
- reproducible evaluation
- Git commits and tags
- deployment evidence

Always distinguish clearly between:

- **REAL**
- **SIMULATED**
- **MOCK**
- **FALLBACK**
- **LOCAL SAMPLE**
- **NOT VERIFIED**

Never present simulated, mocked, or unverified behavior as real.

---

## 7. Measure Impact, Not Only Technical Performance

A technically successful system is not automatically impactful.

Where possible, measure:

**Before → After**

or:

**Baseline → Proposed System**

Examples:

- congestion queue before vs after intervention
- learning score before vs after adaptive tutoring
- inspection time before vs after automation
- energy usage before vs after optimization
- error rate before vs after AI assistance
- incident response time before vs after agentic monitoring

The project should answer:

> What changed because this system exists?

---

## 8. Prefer Autonomous Closed Loops

When appropriate, design the system as a closed loop.

Recommended pattern:

**Perceive  
→ Build State  
→ Reason  
→ Decide  
→ Act  
→ Re-observe  
→ Evaluate Result  
→ Update State / Memory  
→ Continue**

This is stronger than a one-shot AI response because the system can verify whether its action actually worked.

---

## 9. Design the Demo Early

A strong project should have one memorable demonstration.

The core value should be understandable quickly.

Recommended demo structure:

**Problem  
→ System detects the situation  
→ AI decides  
→ System acts  
→ Result changes  
→ Evidence appears**

Avoid demos dominated by:

- long setup
- excessive menus
- architecture explanations
- static slides
- hidden AI behavior
- manual intervention that looks autonomous

The most important behavior should be visible.

---

## 10. Product Before Feature Expansion

After the core engineering works, prioritize:

**Product Validation  
→ Real Testing  
→ Evidence  
→ Measurable Impact  
→ User Experience  
→ Demo Quality  
→ Submission / Delivery Clarity**

before unnecessary feature expansion.

Do not keep adding features just because they are technically possible.

Every new feature should answer at least one of these:

- Does it solve an important problem?
- Does it improve a defined metric?
- Does it strengthen evidence?
- Does it improve reliability?
- Does it strengthen the demo?
- Does it strengthen real-world usability?

If not, defer it.

---

## 11. Minimum Necessary Complexity

Use the simplest architecture that can achieve winner-level performance and evidence.

Avoid unnecessary:

- services
- models
- agents
- frameworks
- cloud components
- UI panels
- integrations
- abstractions
- dependencies

More technology does not automatically mean a stronger project.

Complexity must earn its place.

---

## 12. Preserve Stable Baselines

Once a feature, scene, architecture, workflow, or visual baseline is approved and stable:

- do not redesign it without a clear reason
- prefer additive changes
- isolate new features
- avoid unrelated edits
- protect known-good behavior
- keep regression validation focused on affected areas

Stable approved systems should remain stable unless explicitly changed.

---

## 13. RF90 Compatibility

For visual or spatial projects using RF90:

- use the target reference as the visual goal
- achieve static similarity first
- do not add animation, gameplay, simulation, or autonomous behavior before static approval when RF90 requires it
- preserve approved visual assets and scenes
- build new functionality additively after **PASS_STATIC / USER_APPROVED_STATIC**

Winner-level functionality must not destroy an approved visual baseline.

---

## 14. Testing Rule

Test what is necessary to prove the current change.

Prefer:

- targeted tests
- regression tests for affected core behavior
- runtime validation
- evidence-producing tests

Avoid unnecessary full-suite testing when the change does not require it.

Always report truthfully:

- PASS
- FAIL
- NOT VERIFIED

Never claim success without evidence.

---

## 15. Runtime Truthfulness

Any integration described as real must actually run.

Examples:

If the project claims:

- Nebius runtime
- NVIDIA model runtime
- Gemini runtime
- OpenAI runtime
- AWS runtime
- physical hardware
- drone integration
- live sensor input
- external API integration

then the demo and logs must clearly show whether it is:

**REAL, MOCK, FALLBACK, LOCAL SAMPLE, FAILED, or NOT VERIFIED.**

Never replace a failed real integration with a fake success.

---

## 16. User and Market Evidence

When relevant, strengthen the project with real-world validation such as:

- user interviews
- pilot users
- test users
- organizations
- schools
- businesses
- domain experts
- adoption
- usage
- retention
- transactions
- letters of interest
- feedback
- deployment trials

A small amount of real evidence can be stronger than many hypothetical claims.

---

## 17. Research Projects

For academic or thesis projects, winner-level does not mean commercial hype.

Use the same principles with research evidence:

**Research Problem  
→ Hypothesis  
→ Method  
→ System  
→ Experiment  
→ Metrics  
→ Results  
→ Limitations  
→ Reproducibility**

Strong research projects should produce defensible evidence, not only impressive demos.

---

## 18. Competition / Hackathon Projects

For competitions, optimize for:

- rule compliance
- clear problem
- AI necessity
- working implementation
- real runtime evidence
- measurable impact
- originality
- usability
- reproducibility
- strong documentation
- clear architecture
- polished demo
- credible claims

Read the competition rules before major development.

Do not assume judges reward technical complexity alone.

---

## 19. IP, Security, and Compliance

Winner-level quality includes compliance.

Before public release or submission:

- check third-party licenses
- remove restricted assets from public repositories
- protect API keys and credentials
- verify dataset permissions
- verify model licenses
- verify competition requirements
- verify repository visibility requirements
- verify required disclosures
- verify attribution
- verify privacy implications

A technically excellent project can fail because of compliance mistakes.

---

## 20. Token and Development Efficiency

For AI coding agents and development assistants:

- read project rules first
- use minimum necessary context
- make minimum necessary edits
- avoid unrelated exploration
- preserve stable code
- prefer additive implementation
- run only necessary validation
- stop when acceptance criteria are satisfied
- report concise evidence

The goal is:

**Best practical result with optimal token and engineering efficiency.**

---

## 21. Required Project Decision Gate

Before moving from concept to major development, confirm that the project has:

- [ ] Important problem
- [ ] Clear target user
- [ ] Strong solution hypothesis
- [ ] Necessary role for AI
- [ ] Autonomous or adaptive behavior where relevant
- [ ] Defined metrics
- [ ] Evidence plan
- [ ] Impact measurement plan
- [ ] Demo moment
- [ ] Validation plan
- [ ] IP / security plan
- [ ] Clear acceptance criteria

If several items are missing, improve the concept first.

---

## 22. Final Rule

**Do not optimize for “finished.”**

Optimize for:

**Useful  
→ Working  
→ Verified  
→ Measurable  
→ Credible  
→ Memorable  
→ Winner-Level**

Build the project so that its strongest claims can be demonstrated with evidence.


## Final Rule

**RF90: Protect validated assets and behavior. PatrolAI competition development must remain additive, minimal, regression-safe, token-efficient, evidence-driven, and demonstrably integrated with Nebius Token Factory / Nebius AI Cloud and NVIDIA open-source models before those integrations are claimed as REAL.**
