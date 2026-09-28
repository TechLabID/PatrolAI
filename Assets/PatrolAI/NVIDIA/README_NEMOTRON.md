# PatrolAI NVIDIA Nemotron Offline Preparation

## Purpose

Phase 2.5 prepares PatrolAI contracts, parsing, validation, and deterministic offline testing for future NVIDIA Nemotron reasoning before live Token Factory access is available.

## Status

**OFFLINE PREPARATION ONLY**

**LIVE NEBIUS / NVIDIA INFERENCE NOT YET VALIDATED**

No file in this phase calls Nebius, selects a final model ID, invokes NVIDIA inference, or changes runtime patrol behavior.

## Future Runtime Architecture

```text
PatrolObservation
    → NebiusClient
    → NVIDIA Nemotron
    → NemotronDecisionParser
    → NemotronDecisionValidator
    → PatrolDecision
```

The future provider implementation will use `INemotronReasoningService` while keeping scene objects, OpenCV components, movement, controllers, and UI outside the reasoning contract.

## Safety Principle

Model output is untrusted external inference until it is parsed and validated. Malformed JSON, missing fields, unsupported values, invalid confidence, empty reasons, and inconsistent risk/action combinations must not become successful decisions.

Unsafe or invalid output falls back to:

```text
riskLevel: UNKNOWN
action: REOBSERVE
requiresReobservation: true
```

This fallback is a data decision only. Phase 2.5 does not trigger movement, alerts, re-observation, or other runtime behavior.

## Offline Mock

`MockNemotronReasoningService` is labeled **OFFLINE TEST ONLY** and provides deterministic mappings for local validation without an API key. It is not a simulated claim of model inference and is not connected to the application runtime.

## Competition Integrity

Mock responses must never be represented as live NVIDIA or Nebius inference in:

- demo video;
- screenshots;
- README claims;
- Devpost descriptions;
- judging material.

Only a later authenticated and evidenced Token Factory request may support a live-inference claim.

## Targeted Offline Tests

`NemotronOfflineTestHarness` covers valid SAFE, WARNING, and DANGER responses; malformed JSON; unsupported risk and action values; missing fields; out-of-range confidence; insufficient evidence fallback; and deterministic mock reasoning. The harness uses no third-party test package and performs no network or Unity scene operation.

## Phase 3 — Live Reasoning Adapter

Phase 3 adds an explicit live reasoning path through Nebius Token Factory using exactly:

```text
nvidia/Nemotron-3_5-Lightning
```

The runtime path is:

```text
PatrolObservation
    → NebiusNemotronReasoningService
    → Nebius Token Factory /v1/chat/completions
    → NVIDIA Nemotron 3.5 Lightning
    → NemotronDecisionParser
    → NemotronDecisionValidator
    → PatrolDecision
```

The live adapter requires `NEBIUS_API_KEY` in the calling process environment. It never logs or serializes the key or Authorization header. The request uses a low temperature, a compact observation-only payload, JSON-only instructions, and a small output-token limit. The manual harness sends at most one request per invocation and never runs automatically.

Model output remains untrusted. A successful HTTP response must still produce non-empty choice content, parse successfully, and pass validation or safe normalization. Failure at any stage returns `UNKNOWN / REOBSERVE` with no movement, alert, scene, controller, OpenCV, or UI effect.

`MockNemotronReasoningService` remains **OFFLINE TEST ONLY**. `NebiusNemotronReasoningService` is the only Phase 3 live provider, and a live-inference claim requires an evidenced successful request. Current repository status: the adapter is implemented; live inference validation must be recorded separately when `NEBIUS_API_KEY` is available to the executing process.
