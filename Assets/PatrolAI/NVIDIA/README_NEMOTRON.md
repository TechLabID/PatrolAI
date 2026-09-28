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
