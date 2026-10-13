# PatrolAI — Nebius × NVIDIA Global AI Hackathon Submission Checklist

> Purpose: keep competition requirements, recorded implementation evidence, and remaining submission assets organized before final Devpost submission.

**Public repository: PUBLISHED** — [TechLabID/PatrolAI](https://github.com/TechLabID/PatrolAI), commit `a32ee2188b90c0e6d17c39b21dc462b8e15f52b3`, tag `v0.8-20261013-phase8b-fairness-public-snapshot-pass`. Final public video, Devpost submission and external label review remain pending.

Completed implementation checks below summarize the existing [README evidence](README.md), [Windows validation](Assets/PatrolAI/Competition/PATROLAI_WINDOWS_BUILD_VALIDATION.md) and milestone records; they do not claim a new runtime run. Follow the [Phase 8B claim boundary](Assets/PatrolAI/Competition/PATROLAI_CUE_FAIRNESS_AUDIT.md): no established AI necessity or universal Nemotron superiority, and HC09 is a functional example only.

## 1. Competition Identity

- **Project name:** PatrolAI
- **Track:** Physical AI Track
- **Descriptor:** Autonomous Physical AI for Industrial Safety
- **Core flow:** Sense → Perceive → Reason → Decide → Act
- **Existing project status:** PatrolAI is derived from a pre-existing stable baseline and must clearly document significant updates made during the hackathon period.

## 2. Mandatory Technical Requirements

PatrolAI must satisfy all of the following before submission:

- [x] Project runs on **Nebius Token Factory** or **Nebius AI Cloud**.
- [x] Project makes a **real runtime call** to Nebius Token Factory inference API, or is deployed/run using Nebius AI Cloud compute.
- [x] Project uses at least one **NVIDIA open-source model**.
- [x] Preferred model for PatrolAI reasoning: **NVIDIA Nemotron**.
- [x] Nebius/NVIDIA integration is real and functional, not simulated or hard-coded.
- [ ] Project functions consistently as depicted in the demo video and written description.

## 3. Physical AI Track Requirements

PatrolAI should demonstrate an embodied/edge agent workflow that senses and acts.

Target architecture:

```text
Industrial Simulation
        ↓
Camera / Virtual Sensors
        ↓
OpenCV / Perception
        ↓
Structured Observation
        ↓
Nebius Token Factory
        ↓
NVIDIA Nemotron
        ↓
Risk Reasoning
        ↓
PatrolAI Decision
        ↓
Continue / Reobserve / Stop / Alert / Reroute / Inspect
```

Required final-video behavior (capture remains pending):

- [ ] Sense environment state.
- [ ] Detect a condition/hazard.
- [ ] Send a real inference request through Nebius.
- [ ] Use NVIDIA model output meaningfully.
- [ ] Produce a structured safety decision.
- [ ] Perform or display an autonomous response.
- [ ] Show a closed loop: **sense → reason → act**.

For a Physical AI submission with **no physical hardware component**, the video should show the **key application modules in action**.

## 4. Significant Update Documentation

Because PatrolAI originates from an existing baseline, the submission must explain what was significantly updated during the hackathon.

### Existing / inherited baseline

- Industrial environment
- OpenCV perception pipeline
- Mobile inspector
- Active re-observation behavior
- Existing runtime logic
- Existing validated UI/overlay

### New Nebius × NVIDIA hackathon development

Keep this section updated as work progresses:

- [x] PatrolAI independent project identity
- [x] PatrolAI competition architecture
- [x] Structured observation contract
- [x] Structured decision contract
- [x] Nebius connectivity layer implementation
- [x] Live Nebius Token Factory connectivity validated
- [x] NVIDIA Nemotron reasoning integration
- [x] Structured Nemotron → PatrolDecision parsing
- [x] Physical AI closed-loop integration
- [x] Competition scenarios
- [x] Competition-specific UI/status evidence
- [ ] Final competition demo workflow
- [ ] Final submission documentation

## 5. Current Git Milestones

Selected protected milestones:

```text
v0.1-patrolai-baseline
v0.2-patrolai-phase1-complete
v0.3-patrolai-phase2-complete
v0.8-20261013-phase8b-fairness-public-snapshot-pass
```

Before final submission:

- [x] Create milestone tag after live Nebius validation.
- [x] Create milestone tag after Nemotron reasoning PASS.
- [x] Create milestone tag after Physical AI closed-loop PASS.
- [ ] Create final submission tag.

## 6. Nebius API / Credential Safety

- [ ] `NEBIUS_API_KEY` stored only in environment variable or equivalent secure local configuration.
- [ ] No API key committed to Git.
- [ ] No API key printed in logs.
- [ ] No API key visible in screenshots or demo video.
- [ ] No Authorization header shown publicly.
- [ ] Public README uses only placeholders such as `NEBIUS_API_KEY`.
- [ ] Zero data retention / opt-out remains enabled if desired.

Recommended Windows current-session setup:

```powershell
$env:NEBIUS_API_KEY="YOUR_KEY_HERE"
```

Connectivity check:

```powershell
Invoke-RestMethod -Method Get `
  -Uri "https://api.tokenfactory.nebius.com/v1/models" `
  -Headers @{ Authorization = "Bearer $env:NEBIUS_API_KEY" }
```

## 7. Model / EULA Check

Before locking the final NVIDIA model:

- [ ] Confirm the selected NVIDIA model is available in the current Nebius account.
- [ ] Read the model card.
- [ ] Read and comply with the model-specific EULA.
- [x] Record exact model ID used in production/demo.
- [x] Record why the model is used in PatrolAI.

Selected model:

NVIDIA Nemotron 3.5 Lightning

Model ID:

`nvidia/Nemotron-3_5-Lightning`

Purpose:

Structured safety-decision proposals through Nebius Token Factory, parsed and validated before bounded simulated action. The Phase 8B pilot does not establish AI necessity or universal superiority.

## 8. Safety / Output Validation

Do not treat model output as automatically correct.

Recommended flow:

```text
Perception
   ↓
Nebius / NVIDIA model output
   ↓
PatrolAI validation / policy
   ↓
Action
```

Before final demo:

- [ ] Handle malformed model output.
- [ ] Handle timeout.
- [ ] Handle authentication failure.
- [ ] Handle non-2xx response.
- [ ] Handle unknown/uncertain safety result.
- [ ] Provide safe fallback behavior.
- [ ] Avoid presenting probabilistic model output as guaranteed truth.

## 9. Public Repository Requirements

Submission requires a publicly accessible repository on GitHub, GitLab, or Bitbucket.

Recorded public-source snapshot checklist:

- [x] Repository contains necessary source code.
- [x] Required project assets are included or documented.
- [x] Setup instructions are complete.
- [x] Public open-source license is present.
- [x] License is visible at repository top level.
- [x] README explains how to run PatrolAI.
- [x] README explains Nebius usage.
- [x] README explains NVIDIA model usage.
- [x] README explains significant hackathon updates.
- [x] README explains environment-variable setup.
- [x] No secret files are tracked.
- [x] No private credentials exist in Git history.

Recommended license options mentioned in the rules include:

- MIT
- Apache 2.0
- MPL 2.0

Final license choice:

`MIT`

## 10. README Requirements

Published README coverage:

- [x] PatrolAI overview
- [x] Problem statement
- [x] Physical AI concept
- [x] Architecture diagram
- [x] Technology stack
- [x] Nebius Token Factory integration
- [x] NVIDIA model integration
- [x] OpenCV/perception role
- [x] Sense → Perceive → Reason → Decide → Act workflow
- [x] Setup instructions
- [x] `NEBIUS_API_KEY` setup
- [x] Run instructions
- [x] Demo scenarios
- [x] Significant updates during hackathon
- [x] Limitations
- [x] License

## 11. Demo Video Requirements

Final demo video must:

- [ ] Be **under 3 minutes**.
- [ ] Be uploaded to **YouTube**.
- [ ] Be publicly visible.
- [ ] Show the project actually functioning.
- [ ] Explain use of Nebius.
- [ ] Explain use of NVIDIA model(s).
- [ ] For Physical AI without hardware, show key application modules in action.
- [ ] Avoid unauthorized third-party trademarks/copyrighted music/material.
- [ ] Use English, or provide English translation/subtitles if needed.

Suggested PatrolAI demo structure:

### 0:00–0:20 — Problem
Industrial safety patrol problem and PatrolAI concept.

### 0:20–0:45 — Architecture
Show Sense → Perceive → Reason → Decide → Act.

### 0:45–1:45 — Live Scenario
Show simulation, detection, real Nebius call, NVIDIA reasoning, structured decision, and PatrolAI response.

### 1:45–2:30 — Second Scenario / Robustness
Show re-observation, warning/danger, reroute, stop, or safe fallback.

### 2:30–2:55 — Technology & Impact
Nebius Token Factory + NVIDIA open-source model + OpenCV + Unity.

### 2:55–3:00 — Closing
PatrolAI identity and repository/submission reference.

## 12. Devpost Submission Requirements

Before clicking final Submit:

- [ ] Project title entered.
- [ ] Physical AI Track selected.
- [ ] Text description complete.
- [ ] Public repository URL added.
- [ ] Public YouTube demo URL added.
- [ ] Working demo/test build URL provided if needed; note Physical AI has special handling.
- [ ] Nebius tools/services clearly described.
- [ ] NVIDIA model clearly identified.
- [ ] Significant updates clearly documented.
- [ ] Feedback section completed.
- [ ] Submission material is in English or includes English translation.

## 13. Judging Readiness

Stage 1 should clearly demonstrate that PatrolAI is not a superficial rebrand and genuinely uses required technology.

Before submission, verify that the project visibly supports the judging areas:

### Technological Implementation

- [ ] Real Nebius integration
- [ ] Real NVIDIA model integration
- [ ] Reliable parsing / fallback
- [ ] Stable runtime

### Design

- [ ] Coherent product experience
- [ ] Clear status/decision display
- [ ] Demo is understandable without explanation from developer

### Potential Impact

- [ ] Real industrial safety problem
- [ ] Clear target audience
- [ ] Demonstrated solution addresses the stated problem

### Quality of the Idea

- [ ] Physical AI reasoning is meaningful
- [ ] NVIDIA model use is non-trivial
- [ ] Nebius use is integral to the solution
- [ ] PatrolAI is more than a chatbot or static dashboard

## 14. Intellectual Property / Submission Hygiene

- [ ] Submission is original work or appropriately licensed.
- [ ] Third-party SDK/API usage is authorized.
- [ ] Open-source licenses are respected.
- [ ] No unauthorized logos/trademarks appear in demo assets.
- [ ] No copyrighted music without permission.
- [ ] No private or sensitive personal data is included.

## 15. Competition Timeline

- **Submission deadline shown on Devpost:** 31 Oct 2026 @ 12:00am GMT+7
- **Judging period:** December 2026
- **Winner announcement:** around January 2027

Final internal target:

- [ ] Finish live API validation well before final week.
- [ ] Freeze architecture before final demo week.
- [ ] Record video before final submission day.
- [x] Publish audited source repository after the public-snapshot secret scan.
- [ ] Submit before deadline with buffer.

## 16. Pre-Submission Final Gate

Do not submit until all items below are PASS:

```text
[x] Nebius live connectivity PASS
[x] NVIDIA model runtime use PASS
[x] Physical AI closed loop PASS
[x] Demo scenarios PASS
[x] Public source snapshot ready and repository published
[x] LICENSE present
[x] README complete
[x] Secret scan PASS
[ ] YouTube demo public
[ ] Devpost description complete
[x] Significant-update statement complete
[ ] Feedback complete
[ ] Final Git tag created
```

---

[ ] Do not expose NEBIUS_API_KEY
[ ] No confidential/private third-party information
[ ] All assets/code have valid ownership or licenses
[ ] Credit third-party/open-source components where required
[ ] Submission description accurately represents what PatrolAI actually does
[ ] Keep Git/local backup independent from Devpost
[ ] Verify public repo before submission
[ ] Keep Devpost draft until final validation
[ ] Make sure judges can reproduce/test relevant functionality
[ ] Avoid copyrighted music/trademarks/assets without permission




## Source Basis

This checklist is based on the Nebius × NVIDIA Global AI Hackathon Official Rules provided by the user, including the requirements that the project run on Nebius Token Factory or Nebius AI Cloud, use at least one NVIDIA open-source model, satisfy the Physical AI track requirements, provide a public repository and demo video, and document significant updates for pre-existing projects.
