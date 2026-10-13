using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using PatrolAI.Competition;
using PatrolAI.Evaluation;
using PatrolAI.NVIDIA;
using PatrolAI.PhysicalAI;
using UnityEngine;
using VisionGuard.Phase3;

namespace PatrolAI.Scenarios
{
    /// <summary>Presentation-only, bounded orchestration of existing movement and reasoning paths.</summary>
    [DisallowMultipleComponent]
    public sealed class PatrolAutoPatrolDemoController : MonoBehaviour
    {
        public bool IsRunning { get; private set; }
        public string State { get; private set; } = "IDLE";
        public string Status => "AUTO PATROL: " + (runner != null && runner.ActiveFlow != "AUTO PATROL"
            ? "IDLE" : State);
        // Counts reasoning invocations, not confirmed HTTP requests (configuration can fail before HTTP).
        public int ReasoningInvocations { get; private set; }
        public PatrolClosedLoopResult LatestResult { get; private set; }
        public const float WorkflowTimeoutSeconds = 45f;
        public bool ControlsBusy => IsRunning || (adapter != null && adapter.IsWorkflowRunning);
        private PatrolDemoScenarioRunner runner;
        private PatrolBaselineActionAdapter adapter;
        private VGMobileInspectorMovement movement;

        private void Awake()
        {
            runner = GetComponent<PatrolDemoScenarioRunner>();
            adapter = GetComponent<PatrolBaselineActionAdapter>();
        }

        public static PatrolObservation CreateObservation()
        {
            // Reuse the frozen held-out input verbatim; only refresh its demo timestamp.
            var observation = PatrolHeldOutDataset.Create().Single(c => c.caseId == "HC09").observation;
            observation.timestamp = DateTime.UtcNow.ToString("O");
            return observation;
        }

        public void StartPatrol()
        {
            if (IsRunning || runner == null || adapter == null || runner.IsRunning
                || adapter.IsWorkflowRunning) return;
            movement = FindAnyObjectByType<VGMobileInspectorMovement>();
            if (movement != null && movement.IsMoving) return;
            IsRunning = true;
            ReasoningInvocations = 0;
            LatestResult = null;
            State = "STARTING";
            runner.BeginFlow("AUTO PATROL", null); // Clear previous flow evidence before movement.
            StartCoroutine(Guard(RunPatrol()));
        }

        private IEnumerator Guard(IEnumerator routine)
        {
            var stack = new System.Collections.Generic.Stack<IEnumerator>();
            stack.Push(routine);
            try
            {
                while (stack.Count > 0)
                {
                    bool next = false;
                    object current = null;
                    Exception failure = null;
                    try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                    catch (Exception exception) { failure = exception; }
                    if (failure != null) { Fail("Patrol failed: " + failure.GetType().Name); yield break; }
                    if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (current is IEnumerator nested) stack.Push(nested);
                    else yield return current;
                }
            }
            finally
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
                IsRunning = false;
            }
        }

        private IEnumerator RunPatrol()
        {
            try
            {
                yield return null;
                if (movement == null) { Fail("Movement controller unavailable."); yield break; }
                State = "MOVING";
                yield return movement.MoveHomeIfNeeded();
                yield return movement.MoveToInspectionPose();
                if (!movement.IsAtInspectionPose) { Fail("Inspection pose not reached."); yield break; }

                State = "OBSERVING";
                var observation = CreateObservation();
                runner.Evidence.Begin(observation);
                runner.Evidence.SetComparison("LOCAL SAMPLE: HELD-OUT HC09 | live inference pending | Rule v2: "
                    + PatrolComparativeEvaluation.Disposition(RuleBasedSafetyBaselineV2.Decide(observation)));
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Reason(observation, false);
                if (State == "FAILED") yield break;

                var bounded = Bound(LatestResult, observation, 0, out bool escalated);
                var action = PatrolActionMapper.Map(bounded, out bool ignored);
                if (action.actionType == "REOBSERVE")
                {
                    State = "REOBSERVING";
                    var started = DateTime.UtcNow;
                    adapter.RunAgenticInspection();
                    var session = PatrolReobservationEvidenceReader.CurrentSessionId();
                    runner.Evidence.SetPhysicalWorkflowStatus("PHYSICAL RE-OBSERVATION: RUNNING");
                    float deadline = Time.realtimeSinceStartup + WorkflowTimeoutSeconds;
                    yield return null;
                    while (adapter.AgenticStatus == "RUNNING")
                    {
                        if (Time.realtimeSinceStartup >= deadline)
                        {
                            Fail("Re-observation timeout; STOP / HUMAN REVIEW. Waiting for baseline to settle.");
                            // The protected controller has no cancellation API. ControlsBusy
                            // keeps conflicting UI locked until that controller settles.
                            yield break;
                        }
                        yield return null;
                    }
                    if (adapter.AgenticStatus != "COMPLETE")
                    { Fail("Physical re-observation failed."); yield break; }
                    if (!PatrolReobservationEvidenceReader.TryReadLatestSince(started, session,
                        observation, out PatrolObservation actual, out string error))
                    { Fail(error); yield break; }
                    // HC09's confirmed personnel danger is scenario context, not a lamp reading.
                    // Preserve it when adding secondary Unity/OpenCV camera evidence.
                    actual.additionalContext += "; Original SYNTHETIC HC09 context: " + observation.additionalContext;
                    observation = actual;
                    runner.Evidence.SetReobservationComplete(observation);
                    yield return Reason(observation, true);
                    if (State == "FAILED") yield break;
                    bounded = Bound(LatestResult, observation, 1, out escalated);
                    action = PatrolActionMapper.Map(bounded, out ignored);
                }

                State = "ACTING";
                runner.Evidence.SetBoundedOutcome(action, escalated);
                runner.Evidence.SetComparison("LOCAL SAMPLE: HELD-OUT HC09 | "
                    + runner.Evidence.RuntimeStatus + " live inference | Rule v2 initial: CONTINUE"
                    + " | reasoning invocations: " + ReasoningInvocations);
                // STOP holds the inspector at its current pose; no autonomous route planning.
                yield return new WaitForSecondsRealtime(0.5f);
                State = "COMPLETE";
            }
            finally { IsRunning = false; }
        }

        private IEnumerator Reason(PatrolObservation observation, bool second)
        {
            State = "REASONING";
            if (second) runner.Evidence.BeginSecondReasoning();
            using (var service = new NebiusNemotronReasoningService())
            {
                ReasoningInvocations++;
                Task<PatrolClosedLoopResult> task = new PatrolPhysicalAIController(service).RunAsync(observation);
                float deadline = Time.realtimeSinceStartup + WorkflowTimeoutSeconds;
                while (!task.IsCompleted)
                {
                    if (Time.realtimeSinceStartup >= deadline)
                    { Fail("Nemotron timeout; STOP / HUMAN REVIEW."); yield break; }
                    yield return null;
                }
                if (task.IsFaulted || task.IsCanceled) { Fail("Nemotron failed."); yield break; }
                LatestResult = task.Result;
                if (second) runner.Evidence.ApplySecond(observation, LatestResult);
                else runner.Evidence.Apply(observation, LatestResult);
                if (!Valid(LatestResult)) { Fail("Live inference FAILED/FALLBACK; STOP / HUMAN REVIEW.", false); }
            }
        }

        public static bool Valid(PatrolClosedLoopResult result)
        {
            return result != null && result.runtimeStatus == "REAL" && result.success
                && result.httpSuccess && result.parserSuccess && result.validatorSuccess && !result.usedSafeFallback;
        }

        public static PatrolDecision Bound(PatrolClosedLoopResult result, PatrolObservation observation,
            int attempts, out bool escalated)
        {
            return PatrolSafetyEnvelope.Apply(result == null ? null : result.decision, observation,
                attempts, Valid(result), out escalated, PatrolSafetyEnvelope.DemoMaximumReobservations);
        }

        private void Fail(string message, bool clearRuntime = true)
        {
            State = "FAILED";
            if (clearRuntime)
                runner.Evidence.Apply(null, new PatrolClosedLoopResult { runtimeStatus = "FAILED", message = message });
            runner.Evidence.SetReobservationFailed(message);
            runner.Evidence.SetBoundedOutcome(PatrolActionMapper.Map(PatrolSafetyEnvelope.Stop(message), out bool ignored), true);
        }
    }
}
