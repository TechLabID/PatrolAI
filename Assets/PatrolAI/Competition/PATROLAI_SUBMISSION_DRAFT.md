# PatrolAI Submission Draft

## Current claim boundary — Phase 8B

Phase 8B audited **16/16 held-out cases and 24/24 original cases** using existing source/results only. Under the conservative rubric, **8/16 held-out cases contain answer-bearing cues**. On the no-cue held-out subset, **bounded Rule v2 = raw Nemotron = hybrid = 8/8 action-correct**; on conflicting no-cue cases, **all three = 2/2**. Both raw-model improvements over Rule v2 (HC09/HC11) occurred in STOP-cued contexts. **NO FAIR DEMO CANDIDATE** met the isolated-superiority criteria. This pilot does not establish AI necessity or Nemotron universal superiority. **External review: PENDING.** HC09 is a functional example, not isolated superiority proof. Historical aggregate counts remain unchanged. See [the cue/fairness audit](PATROLAI_CUE_FAIRNESS_AUDIT.md).

**Status: DRAFT — NOT SUBMITTED**

## Project

**PatrolAI — Autonomous Physical AI for Industrial Safety**

PatrolAI helps a simulated industrial inspector decide when to continue, stop, or actively re-observe. It combines Unity and OpenCV perception with real NVIDIA Nemotron reasoning hosted by Nebius Token Factory, then validates the model output before allowing a bounded agent action.

## Inspiration and problem

Industrial safety patrols face two dangerous failure modes: overlooking a real hazard and acting confidently on incomplete evidence. PatrolAI explores an inspection loop that can explicitly recognize uncertainty, move to inspect again, and expose the evidence behind each action.

## What it does

PatrolAI follows `sense → perceive → reason → decide → act`:

1. The inherited Unity-camera/OpenCV workflow can produce simulation observations. AUTO PATROL HC09 instead uses an existing frozen LOCAL SAMPLE held-out structured observation; its danger context is not a live camera detection.
2. PatrolAI creates a structured observation with evidence quality.
3. Nebius Token Factory runs `nvidia/Nemotron-3_5-Lightning` with a strict JSON Schema.
4. A parser and validator convert only policy-consistent output into a `PatrolDecision`.
5. The agent continues, re-observes through the existing inspector workflow, or stops.
6. The competition overlay displays the truthful runtime mode and the full decision/action state.

## How we built it

- Unity `6000.6.3f1` for the simulated industrial environment and embodied agent.
- OpenCV/Python for visual perception.
- Nebius Token Factory for authenticated chat-completions inference.
- NVIDIA `nvidia/Nemotron-3_5-Lightning` for structured safety reasoning.
- C# data contracts, strict parsing, policy validation, safe fallback, orchestration, scenarios, and evidence UI.

## Significant updates during the hackathon

PatrolAI started from a stable VisionGuardAI baseline. The inherited environment, OpenCV pipeline, mobile inspector, active re-observation movement, fusion/temporal workflows, and original overlay are not claimed as new.

Hackathon-period work added the independent PatrolAI identity; structured observation and decision contracts; Nebius connectivity; real Nemotron reasoning; strict JSON parsing and validation; safe fallback; the decision-to-action Physical AI loop; deterministic SAFE, REOBSERVE, and HAZARD scenarios; an adapter to validated baseline workflows; and the competition evidence interface.

## Challenges

The strict schema was accepted, but Nemotron initially used the entire 180-token and 512-token completion budgets for reasoning and ended with `finish_reason=length` before emitting JSON. We preserved the strict parser and schema, diagnosed the real response shape, and disabled thinking only for this short structured-decision request. The next validation returned `finish_reason=stop` and a valid decision.

The other key challenge was truthful integration: a successful-looking UI must not hide a failed cloud request or present fallback data as real inference. PatrolAI therefore labels `REAL`, `MOCK`, `FALLBACK`, `LOCAL SAMPLE`, `FAILED`, and `NOT VERIFIED` separately.

## Accomplishments

- Real Nebius/NVIDIA inference controls a bounded Physical AI action.
- Invalid model output safely becomes `UNKNOWN / REOBSERVE` instead of an unsafe command.
- Scenario B triggers re-observation in Unity simulation through the existing public controller; A and C do not.
- The interface exposes provider, model, request state, decision, action, final state, and latency.
- Targeted suites passed: 12/12 Nemotron, 10/10 Phase 4, and 24/24 integrated Phase 5 tests at Phase 10A.
- Previously owner-confirmed Phase 10A validation at `v0.8.5` / `1bcdfa4`: a fresh-clone clean Windows rebuild used bundled executable-relative Python `3.12.4` without `PATROLAI_PYTHON`; two standalone D runs passed without manual reservation cleanup, with `COMPLETE / VERIFIED`, bounded `STOP / STOPPED`, and correct reservation release. The complete prepared build needs no separate destination-machine Python installation; `NEBIUS_API_KEY` remains external. [Windows validation](PATROLAI_WINDOWS_BUILD_VALIDATION.md).

## Evaluation and demo disclosure

Rule v2 (bounded): **14/16 action-correct**. Raw Nemotron: **14/16 action-correct**. Nemotron + Safety Envelope: **15/16 action-correct**. Conflicting subset: Rule v2 3/5 versus raw Nemotron and hybrid 5/5. Inappropriate CONTINUE: Rule v2 2/13 versus raw Nemotron and hybrid 0/13 expected non-CONTINUE cases. Raw model misses HC04 and HC16; deterministic rules correct HC16, while HC04 remains a missed required STOP.

The held-out comparison is a directional pilot result and is not statistically conclusive industrial validation. External label review: **PENDING**. The cases and proposed labels are internally authored synthetic/local data. These figures do not establish production impact or general superiority.

AUTO PATROL HC09 manual Unity validation: **PASS — owner reported**. Observation Source: **LOCAL SAMPLE — HELD-OUT HC09**. Environment: **UNITY SIMULATION**. AI Runtime: **REAL — Nebius Token Factory** only after successful current inference. Model: `nvidia/Nemotron-3_5-Lightning`.

HC09 was selected for the demo because it is an existing frozen held-out case where Rule v2 and Nemotron produce different actions. Aggregate held-out results are reported separately. HC09 is not representative of every case.

Rules for clear cases. Nemotron for contextual cases. Deterministic safety envelope for bounded execution. Model output is validated and bounded; invalid or unresolved-at-budget behavior escalates conservatively. The envelope does not guarantee hazard detection. See [measured results](PATROLAI_AI_NECESSITY_EVALUATION.md) and [claim boundaries](PATROLAI_SAFE_SUBMISSION_CLAIMS.md).

## What we learned

Structured output is a system property, not only a prompt choice. Schema enforcement, reasoning-mode behavior, completion limits, response-shape inspection, parsing, policy validation, and safe state transitions all matter. We also learned that competition evidence is strongest when inherited work, new work, live execution, fallback, and unverified claims are visibly separated.

## What's next

- Public repository published: [TechLabID/PatrolAI](https://github.com/TechLabID/PatrolAI), current verified baseline commit `1bcdfa4`, tag `v0.8.5-20261014-standalone-python-reservation-fix-pass`.
- Record the final under-three-minute public demo with at least one minute of application modules in action.
- Add the public repository and video URLs to Devpost, then re-run the final submission gate.

## Track fit

**Physical AI.** PatrolAI is an embodied simulation whose virtual sensor observes the environment and whose validated AI decision changes mobile-agent behavior. It demonstrates key application modules in action because this submission has no physical-hardware component.

## Current limitations

This is a simulated research prototype, not a certified safety system. No physical hardware is claimed. Windows standalone validation, the MIT source license, and the clean public source snapshot audit are complete; secrets, provenance-unverified media, generated files, and local-only files are excluded. The source repository is published. The public video and final Devpost fields remain incomplete.

## Nebius/NVIDIA feedback

Token Factory's OpenAI-compatible interface and strict schema support fit the project well. The principal issue was discovering that short structured Nemotron tasks need model-specific thinking control; `enable_thinking=false` resolved the observed truncation without weakening validation. Builder-credit and cost tooling were not evaluated.

