# PatrolAI Architecture

## Project Purpose

PatrolAI is an autonomous Physical AI system for industrial safety patrol and inspection. Its competition identity is **PatrolAI — Autonomous Physical AI for Industrial Safety**.

PatrolAI extends a stable VisionGuardAI-derived Unity foundation with isolated competition modules. It does not rename or relocate inherited assets, and it does not claim inherited functionality as hackathon-period development.

## Physical AI Competition Objective

PatrolAI targets the **Physical AI** track of the Nebius × NVIDIA Global AI Hackathon. The intended competition system will close the loop between simulated industrial sensing, perception, AI reasoning, safety decisions, and physical-agent actions.

Phase 1 establishes architecture and identity only. It does not add API calls, model inference, movement changes, UI integration, or runtime behavior.

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

## Future Nebius Token Factory Integration

A future phase will add a genuine runtime integration with Nebius Token Factory. The planned boundary will accept a structured observation derived from the existing perception output, submit the relevant safety context for inference, receive a real response, and pass validated output to the reasoning layer. Phase 1 contains no Nebius client, credentials, network calls, or simulated inference.

## Future NVIDIA Nemotron Reasoning Layer

A future phase will add NVIDIA Nemotron as a meaningful risk-reasoning layer. Nemotron is intended to interpret structured observations, assess uncertainty and risk, and produce a structured safety decision that PatrolAI can validate before any action is selected. Phase 1 contains no Nemotron integration or reasoning behavior.

## Additive Development Strategy

Competition work is isolated under `Assets/PatrolAI/`. New modules will be introduced as small, independently reviewable additions. Connections to inherited systems will use minimal interfaces and will be deferred until explicitly authorized by a later phase. Existing assets, scenes, scripts, namespaces, serialized components, packages, and settings remain in place.

## Baseline and Competition Separation

The inherited baseline supplies the industrial environment, OpenCV perception, mobile inspector, active re-observation, existing runtime logic, and validated UI. The new PatrolAI competition layer will supply the hackathon-specific identity, structured contracts, Nebius integration, Nemotron reasoning, Physical AI decision loop, competition scenarios, competition UI, and demo workflow.

This separation is maintained so the final submission can clearly identify which capabilities existed at `v0.1-patrolai-baseline` and which significant updates were created during the hackathon period.
