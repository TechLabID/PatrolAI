# VISIONGUARD AI --- Architecture v1.0

**Project:** VISIONGUARD AI\
**Competition:** OpenCV AI Competition 2026\
**MVP:** Autonomous Agentic Visual Inspection of a Simulated Industrial
Electrical Room\
**Architecture principle:** Camera-source-agnostic Agentic Visual
Inspection & Monitoring Platform\
**Core loop:** **SEE → REASON → ACT → OBSERVE AGAIN → VERIFY**

------------------------------------------------------------------------

## 1. Purpose

VISIONGUARD AI is an agentic visual inspection and monitoring platform
in which OpenCV-derived visual evidence can change what the system
decides to observe and do next.

The OpenCV AI Competition 2026 MVP is intentionally focused on a
simulated industrial electrical room in Unity. The system uses fixed
CCTV-style cameras and a movable inspection camera/drone to inspect
electrical equipment. OpenCV 5 provides the core visual perception
layer, an agent selects subsequent inspection actions, AWS provides a
meaningful cloud component, and Unity supplies controlled ground truth
for quantitative evaluation.

The competition prototype is simulation-first. It does not require
physical drones, robots, CCTV hardware, or access to a live high-voltage
facility.

------------------------------------------------------------------------

## 2. Scope Lock v1.0

### Required for the competition MVP

-   Unity industrial electrical room simulator.
-   Fixed CCTV-style camera.
-   Movable inspection camera / simulated drone.
-   Camera adapter abstraction.
-   OpenCV 5 as a substantive visual perception component.
-   Structured visual evidence with confidence/uncertainty.
-   Agentic perception-decision-action loop.
-   Active re-observation from another viewpoint.
-   Ground-truth scenario system hidden from the agent.
-   Automated evaluation and measurable metrics.
-   Meaningful AWS component.
-   Decision/evidence observability.
-   Human-in-the-loop escalation for potentially safety-critical
    findings.
-   Reproducible tests and documented failure cases.

### Optional only after the core MVP passes

-   OpenCV COOL / AWS Graviton benchmark.
-   Energy-aware inspection and battery simulation.
-   More sophisticated path planning.
-   Additional anomaly categories.
-   Additional simulated camera types.

### Future, not required for the competition MVP

-   Physical drone.
-   Real CCTV/IP camera.
-   Mobile robot.
-   Smartphone/webcam input.
-   Home, office, school, factory, public-infrastructure deployments.
-   Wildfire / forest and peatland fire monitoring.
-   Thermal or other specialized imaging sensors.

Do not expand future features into the competition critical path unless
explicitly approved.

------------------------------------------------------------------------

## 3. High-Level Architecture

``` text
┌───────────────────────────────────────────────────────────────┐
│                    UNITY SIMULATION                           │
│                                                               │
│   INDUSTRIAL ELECTRICAL ROOM                                  │
│                                                               │
│   Panels │ MCC │ Cables │ Indicators │ Visual Hazards         │
│                                                               │
│      ┌─────────────┐              ┌──────────────────┐         │
│      │ Fixed CCTV  │              │ Mobile Inspector │         │
│      │ Camera      │              │ Camera / Drone   │         │
│      └──────┬──────┘              └────────┬─────────┘         │
│             │                              │                   │
└─────────────┼──────────────────────────────┼───────────────────┘
              │       RGB observations       │
              └──────────────┬───────────────┘
                             ▼
┌───────────────────────────────────────────────────────────────┐
│                   CAMERA ADAPTER LAYER                        │
│                                                               │
│                     UnityCameraAdapter                        │
│                                                               │
│   Future: RTSP │ Drone │ Robot │ Smartphone │ Webcam Adapter  │
└──────────────────────────────┬────────────────────────────────┘
                               ▼
┌───────────────────────────────────────────────────────────────┐
│                    OPENCV 5 PERCEPTION                        │
│                                                               │
│  Frame Processing                                             │
│        ↓                                                      │
│  ROI / Detection / Tracking / Geometry                        │
│        ↓                                                      │
│  Temporal / Multi-view Evidence                               │
│        ↓                                                      │
│  Confidence + Structured Visual Evidence                      │
└──────────────────────────────┬────────────────────────────────┘
                               ▼
                     VISUAL OBSERVATION
                               │
                               ▼
┌───────────────────────────────────────────────────────────────┐
│                  VISIONGUARD AGENT CORE                       │
│                                                               │
│                     PERCEIVE                                  │
│                        ↓                                      │
│                     ASSESS                                    │
│                        ↓                                      │
│                Evidence sufficient?                           │
│                  ↙             ↘                              │
│                NO               YES                            │
│                ↓                 ↓                             │
│          PLAN NEXT VIEW      VERIFY                            │
│                ↓                 ↓                             │
│          SELECT ACTION       CLASSIFY                          │
│                ↓                 ↓                             │
│             EXECUTE          ESCALATE                          │
└───────────────┬───────────────────────────────────────────────┘
                │ ActionCommand
                ▼
┌───────────────────────────────────────────────────────────────┐
│                    ACTION / CONTROL LAYER                     │
│                                                               │
│ SELECT_CAMERA │ MOVE │ ROTATE │ APPROACH │ OBSERVE_AGAIN      │
│ VERIFY │ CONTINUE │ RECORD │ REQUEST_HUMAN_REVIEW             │
└───────────────┬──────────────────────────────┬────────────────┘
                │                              │
                ▼                              ▼
         UNITY SIMULATION              HUMAN-IN-THE-LOOP
                │
                │ new viewpoint          ACKNOWLEDGE /
                │                        INSPECT AGAIN /
                │                        DISMISS
                └───────────┐
                            ▼
                         OpenCV 5
                      OBSERVE AGAIN
```

------------------------------------------------------------------------

## 4. Unity Simulation Layer

Unity represents the simulated physical world.

### Responsibilities

-   Render the industrial electrical room.
-   Represent electrical equipment and visual inspection targets.
-   Provide fixed and movable virtual cameras.
-   Execute movement/action commands from the VISIONGUARD agent.
-   Maintain scenario state.
-   Maintain hidden ground truth.
-   Produce repeatable inspection episodes.
-   Expose observations without exposing hidden labels to the agent.

### Initial environment

The MVP should remain compact. It does not need to be a visually
elaborate game environment.

Suggested initial objects:

-   Electrical panels.
-   MCC-style cabinets.
-   Cables/conduits.
-   Indicator lights.
-   Breaker/switch visual representations.
-   Safety signage where useful.
-   Walkable/flyable inspection area.

### Initial visual conditions

Start with a small set of visually distinguishable scenarios, for
example:

-   NORMAL.
-   OPEN_COVER.
-   DISCONNECTED_OR_LOOSE_CABLE_VISUAL.
-   ABNORMAL_INDICATOR_STATE.
-   OBSTRUCTED_PANEL.
-   OTHER_CONTROLLED_VISUAL_ANOMALY.

Do not create real electrical hazards. Smoke/fire-like effects, if ever
used, are simulation-only.

------------------------------------------------------------------------

## 5. Camera-Source-Agnostic Architecture

VISIONGUARD must not tightly couple the perception/agent core to Unity.

Conceptual interface:

``` text
ICameraSource
    │
    ├── UnityCameraAdapter          # competition MVP
    ├── FixedUnityCamera
    └── MobileUnityCamera

Future:
    ├── RTSPCameraAdapter           # CCTV/IP camera
    ├── DroneCameraAdapter
    ├── RobotCameraAdapter
    ├── MobileCameraAdapter         # smartphone
    └── WebcamAdapter
```

A visual observation should conceptually contain:

``` text
VisualObservation
- observation_id
- camera_id
- timestamp
- frame/image reference
- source_type
- camera_pose (when available)
- target/ROI metadata (when available)
- acquisition metadata
```

The VISIONGUARD core should not need to know whether an image originated
from Unity, CCTV, a drone, or another compatible source.

### Different camera capabilities

Different camera systems support different active-inspection
capabilities:

-   Fixed CCTV: continuous observation, temporal comparison, or
    selection of another camera.
-   PTZ camera: pan, tilt, and zoom.
-   Drone/mobile robot: physical viewpoint changes.
-   Smartphone/webcam: manually or programmatically supplied
    observations.

For the competition MVP, fixed CCTV plus a movable Unity inspection
camera provides the clearest demonstration.

------------------------------------------------------------------------

## 6. OpenCV 5 Perception Layer

OpenCV 5 is a core competition requirement and must be substantive.

### Responsibilities

-   Receive frames from the camera adapter.
-   Perform required preprocessing.
-   Detect/measure relevant visual conditions.
-   Track or associate targets where useful.
-   Perform geometric or spatial processing where useful.
-   Compare temporal or multi-view observations.
-   Produce confidence/uncertainty.
-   Emit structured evidence for the agent.

Conceptual output:

``` text
VisualEvidence
- observation_id
- target_id
- condition
- confidence
- bounding_region / ROI
- supporting_measurements
- camera_id
- camera_pose
- timestamp
- evidence_quality
```

The exact OpenCV algorithms/models must be selected based on the
implemented anomaly scenarios. Do not add algorithms merely to increase
the technology count.

Critical requirement:

> OpenCV output must influence the next agent decision/action. It must
> not be used only to draw bounding boxes or display video.

------------------------------------------------------------------------

## 7. VISIONGUARD Agent Core

The agent transforms visual evidence into inspection decisions.

### Core state cycle

``` text
PERCEIVE
   ↓
ASSESS
   ↓
Is evidence sufficient?
   ├── NO  → PLAN NEXT OBSERVATION → SELECT ACTION → EXECUTE
   │                                          ↓
   │                                    OBSERVE AGAIN
   │                                          ↓
   └────────────────────────────────────── PERCEIVE
   ↓
YES
   ↓
VERIFY
   ↓
CLASSIFY / RECORD
   ↓
ESCALATE TO HUMAN WHEN REQUIRED
```

### Initial action vocabulary

Keep the action set deterministic and inspectable:

``` text
CONTINUE
SELECT_CAMERA
MOVE
ROTATE
APPROACH
OBSERVE_AGAIN
VERIFY
RECORD
REQUEST_HUMAN_REVIEW
```

More actions may be added only when required.

### Example

``` text
Observation #1
Source: CCTV-02
Target: Panel-07
Possible anomaly confidence: 0.54

Agent:
Evidence insufficient.
Action: inspect Panel-07 using MobileInspector-01.

Mobile inspector approaches Panel-07.

Observation #2
Source: MobileInspector-01
Target: Panel-07
Possible anomaly confidence: 0.91

Agent:
Evidence now sufficient.
Action: VERIFY and REQUEST_HUMAN_REVIEW.
```

This closed loop is the central Agentic Vision demonstration.

------------------------------------------------------------------------

## 8. Action / Control Layer

The action layer translates agent decisions into simulator operations.

### Responsibilities

-   Validate requested actions.
-   Translate abstract actions into Unity commands.
-   Enforce simulation safety/limits.
-   Report action success/failure.
-   Produce new observation opportunities.

Example:

``` text
AgentAction:
APPROACH target=Panel_07

        ↓

Action Controller

        ↓

Unity:
Move MobileInspector toward Panel_07

        ↓

ActionResult:
SUCCESS
new_pose=...
```

Failures must be observable and logged.

------------------------------------------------------------------------

## 9. Human-in-the-Loop

VISIONGUARD is an inspection and decision-support system, not an
autonomous controller of live high-voltage equipment.

Potentially critical findings should be escalated.

Example UI:

``` text
INSPECTION ALERT

Target: Panel-07
Finding: Possible visual anomaly

Evidence:
CCTV-02          54%
MobileInspector  91%

Recommended:
Physical inspection required

[ ACKNOWLEDGE ]
[ INSPECT AGAIN ]
[ DISMISS ]
```

The competition prototype must not simulate or claim autonomous control
of dangerous electrical equipment as a required capability.

------------------------------------------------------------------------

## 10. Ground Truth and Scenario Engine

Unity maintains hidden ground truth.

Example:

``` text
Scenario_037

Panel_01 = NORMAL
Panel_02 = NORMAL
Panel_03 = OPEN_COVER
Panel_04 = NORMAL

GroundTruth:
Panel_03.anomaly = TRUE
Panel_03.condition = OPEN_COVER
```

The agent must not receive this answer directly.

At episode completion:

``` text
VISIONGUARD RESULT
        ↓
      COMPARE
        ↑
UNITY GROUND TRUTH
        ↓
EVALUATION ENGINE
```

### Scenario requirements

Each scenario should be:

-   reproducible,
-   seedable where randomness is used,
-   labeled with hidden ground truth,
-   runnable automatically,
-   capable of recording all observations/actions.

------------------------------------------------------------------------

## 11. Evaluation Architecture

Quantitative evaluation is a first-class component.

``` text
TEST SCENARIO GENERATOR
          │
          ▼
   UNITY GROUND TRUTH
          │
          │ hidden from agent
          ▼
   INSPECTION EPISODE
          │
    ┌─────┴─────┐
    ▼           ▼
Ground Truth  VISIONGUARD Result
    │           │
    └─────┬─────┘
          ▼
   EVALUATION ENGINE
          │
    ┌─────┼──────────────┐
    ▼     ▼              ▼
Detection Task Success  False Positives
    │     │              │
    ├─────┼──────────────┤
    ▼     ▼              ▼
Latency Observations  Verification
          │
          ▼
         AWS
          │
          ▼
 Evaluation / Dashboard
```

### Planned metrics

-   Inspection task success rate.
-   Detection performance.
-   False-positive rate.
-   Verification accuracy.
-   Number of observations required.
-   Time/latency per inspection.
-   OpenCV processing latency.
-   Agent action/decision traces.
-   Failure rate by scenario type.
-   Benefit of active re-observation.

### Critical experiment

Compare:

``` text
BASELINE:
Single observation → detection → final decision

vs.

VISIONGUARD:
Observation → uncertainty assessment → active re-observation
→ additional evidence → verification → final decision
```

The goal is to measure whether active re-observation improves inspection
outcomes.

Never fabricate metrics. Results must come from reproducible runs.

------------------------------------------------------------------------

## 12. AWS Cloud Layer

A meaningful component must run on AWS.

Do not use AWS merely as decorative storage.

The final AWS design should be selected after the local core works.

Potential responsibilities include:

-   inspection event ingestion,
-   visual/evidence processing where appropriate,
-   decision-trace storage,
-   evaluation-result storage,
-   observability/logging,
-   experiment aggregation,
-   dashboard/backend services,
-   reproducible cloud execution.

Conceptual flow:

``` text
VISIONGUARD CORE
      │
      ├── Inspection Events
      ├── OpenCV Results
      ├── Agent Decisions
      ├── Evidence
      └── Evaluation Results
                │
                ▼
              AWS
                │
       Processing / APIs
                │
        Data / Logs / Evidence
                │
          Observability
                │
      Evaluation Dashboard
```

Do not lock specific AWS services until the implemented requirement is
clear.

------------------------------------------------------------------------

## 13. COOL / AWS Graviton --- Optional

After the core system is working, investigate whether a suitable OpenCV
workload can be benchmarked using the competition's COOL technology on
AWS Graviton.

Possible comparison:

``` text
Standard OpenCV workload
          VS
COOL / AWS Graviton workload
```

Possible measurements:

-   latency,
-   throughput,
-   FPS,
-   CPU utilization,
-   cost efficiency where measurable.

This is optional and must not delay the core Agentic Vision
implementation.

------------------------------------------------------------------------

## 14. Energy-Aware Inspection --- Deferred

Drone/mobile-camera energy efficiency is intentionally outside the MVP
critical path.

A later enhancement may optimize the next viewpoint using factors such
as:

``` text
Next Viewpoint Utility =
Visual Information Gain
+ Inspection Priority
- Travel Cost
- Energy Cost
```

Possible later behavior:

``` text
Low battery
→ prioritize nearest high-risk target
→ return to base
→ recharge
→ resume mission
```

Do not implement this until the required MVP architecture has passed.

------------------------------------------------------------------------

## 15. Proposed Repository Structure

``` text
VisionGuardAI/
│
├── unity/
│   └── VisionGuardSimulator/
│
├── vision/
│   ├── opencv/
│   ├── detection/
│   ├── tracking/
│   └── evidence/
│
├── agent/
│   ├── reasoning/
│   ├── policy/
│   ├── planner/
│   └── actions/
│
├── camera/
│   ├── adapters/
│   └── protocol/
│
├── evaluation/
│   ├── scenarios/
│   ├── ground_truth/
│   ├── metrics/
│   └── reports/
│
├── cloud/
│   └── aws/
│
├── dashboard/
│
├── tests/
│
├── docs/
│
├── Architecture.md
└── README.md
```

Python should handle most of:

-   OpenCV,
-   perception,
-   evidence,
-   agent logic,
-   evaluation,
-   AWS communication.

Unity/C# should primarily handle:

-   simulation,
-   cameras,
-   environment,
-   ground truth,
-   simulator actions,
-   simulator-side communication.

Keep module boundaries explicit.

------------------------------------------------------------------------

## 16. Development Milestones

### Phase 1 --- Simulator Core

Deliver:

-   Compact industrial electrical room.
-   Initial electrical equipment.
-   Fixed CCTV.
-   Movable inspection camera/drone.
-   Scenario definitions.
-   Hidden ground truth.

Exit condition:

-   A deterministic scenario can run and both cameras can provide
    observations.

### Phase 2 --- OpenCV 5 Perception

Deliver:

-   Unity-to-OpenCV observation path.
-   Initial perception pipeline.
-   Structured VisualEvidence.
-   Confidence/uncertainty output.

Exit condition:

-   OpenCV 5 can analyze a Unity observation and return machine-readable
    evidence.

### Phase 3 --- Agentic Loop

Deliver:

``` text
Detect
→ assess uncertainty
→ request another view
→ move/select camera
→ observe again
→ verify
```

Exit condition:

-   Visual evidence demonstrably changes the next agent action.

This is the most important milestone.

### Phase 4 --- Autonomous Inspection

Deliver:

-   Multiple inspection targets.
-   Basic mission progression.
-   Camera selection/movement.
-   Multiple controlled scenarios.

Exit condition:

-   VISIONGUARD can complete an inspection episode without a manually
    scripted camera sequence.

### Phase 5 --- Evaluation Engine

Deliver:

-   Automated episode runner.
-   Ground-truth comparison.
-   Metrics.
-   Baseline vs active-reobservation experiment.
-   Failure-case recording.

Exit condition:

-   Reproducible quantitative results are generated automatically.

### Phase 6 --- AWS + Observability

Deliver:

-   Meaningful AWS component.
-   Inspection/evidence/decision traces.
-   Evaluation data path.
-   Reproducible deployment documentation.

Exit condition:

-   A required part of the demonstrated workflow runs on AWS and is
    verifiable.

### Phase 7 --- Competition Polish

Deliver:

-   Failure testing.
-   Human-in-the-loop UI.
-   Architecture diagrams.
-   Final documentation.
-   Reproducibility instructions.
-   Competition video.
-   Optional COOL benchmark if time remains.

------------------------------------------------------------------------

## 17. Testing Strategy

Use minimum validation appropriate to each change.

### Small/local changes

-   syntax/compile check,
-   targeted unit test,
-   directly affected smoke test.

### Cross-system changes

Run integration tests across the affected boundary, for example:

-   Unity → camera adapter,
-   camera adapter → OpenCV,
-   OpenCV → agent,
-   agent → Unity action,
-   evaluation → AWS.

### Full regression

Run full regression only for:

-   shared/core architecture changes,
-   cross-system releases,
-   major competition milestones,
-   final release validation,
-   or explicit request.

Avoid repeated expensive Unity rebuilds, screenshots, recaptures, and
full test runs when a targeted check is sufficient.

------------------------------------------------------------------------

## 18. Logging and Observability

Every inspection episode should be traceable.

Suggested event types:

``` text
EPISODE_STARTED
OBSERVATION_CAPTURED
OPENCV_EVIDENCE_CREATED
AGENT_DECISION
ACTION_REQUESTED
ACTION_COMPLETED
ACTION_FAILED
REOBSERVATION_REQUESTED
VERIFICATION_RESULT
HUMAN_REVIEW_REQUESTED
EPISODE_COMPLETED
```

A decision trace should make it possible to answer:

-   What did the camera observe?
-   What did OpenCV infer?
-   How confident was the evidence?
-   Why was another observation requested?
-   Which action was selected?
-   What changed after re-observation?
-   What was the final decision?
-   Was it correct relative to ground truth?

------------------------------------------------------------------------

## 19. Safety and Responsible Operation

-   Competition testing is simulation-first.
-   Do not create real electrical faults for testing.
-   Do not control live high-voltage equipment.
-   Critical findings require human review.
-   Confidence must not be presented as certainty.
-   Record failure cases and known limitations.
-   Real-world deployment would require domain-specific validation,
    operational procedures, and appropriate safety controls.

------------------------------------------------------------------------

## 20. Future Expansion

The core architecture is intended to support future adapters and domain
policies without rewriting the VISIONGUARD core.

Potential domains:

``` text
VISIONGUARD Industrial
→ electrical / equipment inspection

VISIONGUARD Building
→ homes / offices / schools / commercial buildings

VISIONGUARD Infrastructure
→ critical public assets

VISIONGUARD Environmental
→ forests / peatlands / wildfire monitoring
```

Example future wildfire workflow:

``` text
Fixed camera / drone
        ↓
Possible smoke
        ↓
OpenCV evidence
        ↓
VISIONGUARD assesses uncertainty
        ↓
Request temporal/new viewpoint evidence
        ↓
Re-observe
        ↓
Verify / reject
        ↓
Record location + evidence
        ↓
Escalate to authorized human responder
```

These future applications are not claims of current
competition-prototype capability.

------------------------------------------------------------------------

## 21. Architecture Principles for Codex

When implementing this architecture:

1.  Preserve the scope lock.
2.  Build the smallest working vertical slice first.
3.  Keep stable features unchanged unless a change is required.
4.  Prefer additive, minimal changes.
5.  Keep Unity simulation separate from the perception/agent core.
6.  Keep camera interfaces generic.
7.  Make OpenCV 5 substantive and measurable.
8.  Ensure visual evidence changes subsequent agent behavior.
9.  Keep ground truth inaccessible to the agent during inference.
10. Make experiments deterministic/reproducible where practical.
11. Log decision traces from the beginning.
12. Never fabricate evaluation results.
13. Keep AWS meaningful but do not over-engineer it.
14. Do not implement optional features before the required MVP passes.
15. Maintain human control for potentially safety-critical outcomes.
16. Use targeted tests first; avoid unnecessary full regressions.
17. Document material architectural changes.

------------------------------------------------------------------------

## 22. Competition Demonstration Target

The primary demonstration should be understandable within seconds:

``` text
1. Fixed CCTV observes Panel-07.

2. OpenCV 5:
   possible anomaly, confidence insufficient.

3. VISIONGUARD:
   decides more evidence is required.

4. Agent:
   selects/dispatches mobile inspection camera.

5. Unity:
   mobile camera moves to a better viewpoint.

6. OpenCV 5:
   analyzes the new observation.

7. VISIONGUARD:
   compares evidence and verifies/rejects the anomaly.

8. Human:
   receives evidence-backed escalation when required.

9. Evaluation:
   compares the decision against hidden Unity ground truth.

10. AWS:
    records/processes the meaningful cloud portion of the workflow.
```

The central claim demonstrated by the MVP is:

> **VISIONGUARD AI does not only see. It uses visual evidence to decide
> when and how to look again before making an inspection decision.**

------------------------------------------------------------------------

## 23. Baseline Status

**Architecture version:** v1.0\
**Status:** DESIGN BASELINE\
**Competition MVP:** Industrial Electrical Room Simulation\
**Primary visual sources:** Simulated fixed CCTV + simulated
mobile/drone inspection camera\
**Perception core:** OpenCV 5\
**Agentic pattern:** Perceive → Assess → Act → Re-observe → Verify\
**Cloud:** AWS, exact services to be selected during implementation\
**Evaluation:** Unity hidden ground truth + automated quantitative
metrics\
**Physical hardware required for MVP:** None\
**Energy-aware inspection:** Deferred\
**COOL/Graviton:** Optional after core PASS

This document is the architecture baseline for implementation. Changes
that materially alter scope, module boundaries, competition
requirements, or the core perception-action loop should be documented
explicitly.
