# PatrolAI Submission Draft

**Status: DRAFT — NOT SUBMITTED**

## Project

**PatrolAI — Autonomous Physical AI for Industrial Safety**

PatrolAI helps a simulated industrial inspector decide when to continue, stop, or actively re-observe. It combines Unity and OpenCV perception with real NVIDIA Nemotron reasoning hosted by Nebius Token Factory, then validates the model output before allowing a bounded agent action.

## Inspiration and problem

Industrial safety patrols face two dangerous failure modes: overlooking a real hazard and acting confidently on incomplete evidence. PatrolAI explores an inspection loop that can explicitly recognize uncertainty, move to inspect again, and expose the evidence behind each action.

## What it does

PatrolAI follows `sense → perceive → reason → decide → act`:

1. A Unity camera and OpenCV pipeline observe an industrial condition.
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
- Scenario B triggers physical re-observation through the existing public controller; A and C do not.
- The interface exposes provider, model, request state, decision, action, final state, and latency.
- Targeted suites passed: 12/12 Nemotron, 10/10 Phase 4, and 22/22 integrated Phase 5 tests.

## What we learned

Structured output is a system property, not only a prompt choice. Schema enforcement, reasoning-mode behavior, completion limits, response-shape inspection, parsing, policy validation, and safe state transitions all matter. We also learned that competition evidence is strongest when inherited work, new work, live execution, fallback, and unverified claims are visibly separated.

## What's next

- Select a public-source license and finish the asset provenance review.
- Remove/sanitize all local credential diagnostics before publication.
- Create and validate a reproducible Windows build containing the competition scene.
- Record the final under-three-minute public demo with at least one minute of application modules in action.
- Add the public repository and video URLs, then re-run the final submission gate.

## Track fit

**Physical AI.** PatrolAI is an embodied simulation whose virtual sensor observes the environment and whose validated AI decision changes mobile-agent behavior. It demonstrates key application modules in action because this submission has no physical-hardware component.

## Current limitations

This is a simulated research prototype, not a certified safety system. No physical hardware is claimed. The Windows build, public repository license, third-party media provenance, public video, and final Devpost fields remain incomplete.

## Nebius/NVIDIA feedback

Token Factory's OpenAI-compatible interface and strict schema support fit the project well. The principal issue was discovering that short structured Nemotron tasks need model-specific thinking control; `enable_thinking=false` resolved the observed truncation without weakening validation. Builder-credit and cost tooling were not evaluated.

