# PatrolAI Phase 10A UI/runtime-state regression

Run from the repository root with Python (standard library only) and the .NET 10 SDK installed:

~~~powershell
python tests/patrol_ui_state/run_offline.py
~~~

The command compiles production source, runs the requested offline harnesses, then runs the production adapter/scenario/AUTO orchestration against isolated deterministic Unity, movement, reader and reasoning fixtures. Fixtures are outside Assets and are never part of a Unity player. They have no network transport or credentials; any REAL-shaped result in these tests is synthetic verification input, not live inference evidence. Generated binaries and logs are under ignored tests/artifacts/phase10a_ui/. There is no package restore, live inference or benchmark execution.

## Authoritative state and presentation

| Field | Runtime authority | Presentation |
| --- | --- | --- |
| Runtime mode | PatrolClosedLoopResult runtimeStatus and success/HTTP/parser/validator/fallback flags | PatrolCompetitionEvidenceState.Apply rejects unverified REAL; Begin clears old mode |
| Scenario ownership | PatrolDemoScenarioRunner.ActiveFlow, set at BeginFlow for A/B/C/D/AUTO or baseline selection | Previous LatestResult, evidence and baseline selection are cleared |
| Agentic/Fusion/Temporal state and active phase | Inherited controller IsRunning and State; explicit adapter dispatch failure | Adapter projects both fields from the same current state; its completion observer is not the authority |
| Physical re-observation | Adapter terminal state, timeout, adapter availability and fresh session evidence reader | RUNNING / COMPLETE / FAILED in evidence; terminal physical failure has final FAILED |
| Patrol decision and action | Current closed-loop decision and existing PatrolActionMapper; existing bounded outcome for D/AUTO | Current decision clears before second reasoning; FirstDecision remains explicitly labeled |
| Final state and completion/failure | Closed-loop final state, physical terminal outcome, or existing bounded STOP | B failure is FAILED; failed reasoning is ERROR; bounded STOP remains STOPPED |
| Observation, evidence and confidence | Current scenario observation or actual fresh re-observation | Begin/Apply reset the current values; no earlier workflow values survive ownership change |
| AUTO state | PatrolAutoPatrolDemoController.State / IsRunning | Status footer and reviewer panel are current only while ActiveFlow is AUTO PATROL |

## Causes and correction

The adapter cached RUNNING until its completion coroutine resumed, even after a controller reported a terminal FAILED phase. The adapter now reads current controller state directly. A controller that is actually running a new startup can still retain its previous terminal State while returning home; that obsolete phase is presented as RUNNING until a current phase is available. A stopped controller's FAILED or COMPLETE phase is used immediately, even before the observer resumes. Explicit dispatch failure cannot display as COMPLETE. IDLE ownership cannot expose a previous terminal phase.

HasActiveAction and the view's detail selection previously survived a new scenario. BeginFlow now clears presentation-only workflow ownership and all current competition evidence. Revision checks prevent an old observer from restoring obsolete UI evidence. Underlying controller state/history and physical execution are preserved, and actual execution flags keep competing controls locked while a timed-out baseline settles.

SetReobservationFailed previously only changed detail and re-observation status. It now sets final FAILED, leaves the completed inference's runtime label intact, and suppresses an unexecuted second decision. The missing-adapter branch uses the same terminal path. Existing D/AUTO safety-envelope STOP outcomes still override final state to STOPPED and retain the failure detail. This does not add or change a safety action.

Manual baseline selection also clears previous competition evidence and displays NO NEMOTRON INFERENCE. Second reasoning shows NOT VERIFIED as its current mode/decision until a result arrives; the explicitly labeled first decision is preserved.

## Validation scope

- UI/state ownership and truthfulness: 22/22 deterministic checks.
- AUTO PATROL: 16/16 deterministic checks, including movement, action, repeat runs, timeouts, fresh evidence, bounded STOP and control locking.
- Phase 5: 24/24; closed loop: 10/10; Nemotron offline: 12/12; Phase 7 evaluation: 10/10.
- Phase 7B: PASS for the existing mapping/metric/audit/consistency/serialization assertions (the harness does not report a numerical total).
- Phase 7C: 18/18 checks plus its existing nested Nemotron offline harness.
- Public evidence verifier: Phase 7B stored results PASS; held-out freeze/hash 9/9 MATCH.

Offline compilation and fixture tests do not verify rendered standalone UI, real Python/OpenCV/movement execution, or real model inference. No post-change player build or launch is performed by this command. The prior standalone evidence remains historical evidence supplied by the user.

For a later standalone visual check, exercise failed Agentic -> A, failed Agentic -> C, B while running, and B terminal failure. Confirm ownership clears prior failure, STATE/ACTIVE PHASE agree, and final state leaves REOBSERVING on failure. Live Nemotron requests are not part of this offline regression command.
