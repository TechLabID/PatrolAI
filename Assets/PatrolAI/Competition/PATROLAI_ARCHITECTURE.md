# PatrolAI Architecture

## Project Purpose

PatrolAI is an autonomous Physical AI system for industrial safety patrol and inspection. Its competition identity is **PatrolAI — Autonomous Physical AI for Industrial Safety**.

PatrolAI extends a stable VisionGuardAI-derived Unity foundation with isolated competition modules. It does not rename or relocate inherited assets, and it does not claim inherited functionality as hackathon-period development.

## Physical AI Competition Objective

PatrolAI targets the **Physical AI** track of the Nebius × NVIDIA Global AI Hackathon. The implemented competition system closes the loop between simulated industrial sensing, perception, AI reasoning, validated safety decisions, and bounded physical-agent actions.

## Inherited Stable Baseline

The protected starting point is Git tag `v0.1-patrolai-baseline`. This baseline was inherited from the stable VisionGuardAI project and remains the foundation beneath the new PatrolAI competition layer.

## Protected Baseline Systems

The following inherited systems remain sealed unless a later phase explicitly authorizes a narrowly scoped change:

- RF90 industrial environment and validated scene;
- OpenCV perception pipeline;
- mobile inspector movement and inspection-pose behavior;
- active re-observation workflow;
- existing runtime and controller logic;
- validated status overlay and UI;
- Unity packages and Project Settings.

## Target Architecture

```text
Industrial Simulation
        ↓
Camera / Virtual Sensors
        ↓
Existing OpenCV Perception
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

The core flow is:

**Sense → Perceive → Reason → Decide → Act**

## Nebius Token Factory Integration

`NebiusClient` and `NebiusNemotronReasoningService` submit a structured observation to the OpenAI-compatible Nebius Token Factory chat-completions endpoint. Authentication is read only from `NEBIUS_API_KEY`. Runtime results remain explicitly labeled `REAL`, `MOCK`, `FALLBACK`, `LOCAL SAMPLE`, or `FAILED`.

## NVIDIA Nemotron Reasoning Layer

`nvidia/Nemotron-3_5-Lightning` interprets structured observations and returns a decision constrained by a strict JSON Schema. `NemotronDecisionParser` and `NemotronDecisionValidator` must both pass before a `PatrolDecision` can select `CONTINUE`, `REOBSERVE`, or `STOP`; invalid output uses the explicit safe fallback.

## Additive Development Strategy

Competition work is isolated under `Assets/PatrolAI/`. Connections to inherited systems use narrow public entry points, including the baseline workflow adapter, rather than duplicating the inherited algorithms. Existing assets, scenes, namespaces, and serialized references remain in place except for the minimum documented standalone-runtime integration.

## Baseline and Competition Separation

The inherited baseline supplies the industrial environment, OpenCV perception, mobile inspector, active re-observation, fusion/temporal workflows, and original runtime/UI foundation. The new PatrolAI competition layer supplies the hackathon-specific identity, structured contracts, Nebius integration, Nemotron reasoning, Physical AI decision loop, deterministic scenarios, evidence UI, and demo workflow.

This separation is maintained so the final submission can clearly identify which capabilities existed at `v0.1-patrolai-baseline` and which significant updates were created during the hackathon period.
