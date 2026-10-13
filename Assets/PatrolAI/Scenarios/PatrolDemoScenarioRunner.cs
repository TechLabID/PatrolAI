using System;
using System.Collections;
using PatrolAI.Competition;
using PatrolAI.NVIDIA;
using PatrolAI.PhysicalAI;
using PatrolAI.UI;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using PatrolAI.Evaluation;

namespace PatrolAI.Scenarios
{
    /// <summary>
    /// Runs deterministic inputs through the actual Phase 4 closed loop.
    /// Live output is never substituted with a prerecorded decision.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PatrolDemoScenarioRunner : MonoBehaviour
    {
        public PatrolCompetitionEvidenceState Evidence { get; private set; }
            = new PatrolCompetitionEvidenceState();
        public PatrolDemoScenarioResult LatestResult { get; private set; }
        public bool IsRunning { get; private set; }
        public string ActiveFlow { get; private set; } = "IDLE";
        private PatrolBaselineActionAdapter baselineAdapter;
        private DateTime reobservationStartedUtc;
        private string reobservationSessionId;
        private PatrolConflictReservationLifetime conflictLifetime;
        private Task conflictPendingTask;
        private bool conflictAborted;
        private bool conflictAsyncActive;
        private Coroutine conflictCoroutine;

        private void Awake()
        {
            baselineAdapter = GetComponent<PatrolBaselineActionAdapter>();
        }

        public void BeginFlow(string flow, PatrolObservation observation)
        {
            ActiveFlow = flow;
            LatestResult = null;
            if (baselineAdapter == null) baselineAdapter = GetComponent<PatrolBaselineActionAdapter>();
            if (baselineAdapter != null) baselineAdapter.ClearWorkflowPresentation();
            Evidence.Begin(observation);
        }

        public void RunSafeScenario()
        {
            Run(PatrolDemoScenario.Safe());
        }

        public void RunInsufficientEvidenceScenario()
        {
            Run(PatrolDemoScenario.InsufficientEvidence());
        }

        public void RunHazardScenario()
        {
            Run(PatrolDemoScenario.Hazard());
        }

        public void RunConflictingEvidenceScenario()
        {
            LogConflictDiagnostic("FLOW_REQUESTED");
            if (IsRunning || conflictLifetime != null) {
                LogConflictDiagnostic("START_BLOCKED reason=RUN_ALREADY_ACTIVE", true);
                return;
            }
            var scenario = PatrolConflictDemoScenario.Create();
            PatrolConflictRunReservation reservation = null;
            bool evidenceStorageReady = false;
            try {
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "..", "evaluation", "results", "phase7c"));
                Directory.CreateDirectory(directory);
                evidenceStorageReady = true;
                LogConflictDiagnostic("RESERVATION_ACQUISITION attempted=true path="
                    + Path.Combine(directory, "demo_started.json"));
                reservation = PatrolConflictRunReservation.Acquire(directory);
                conflictAborted = false;
                conflictPendingTask = null;
                conflictLifetime = PatrolConflictReservationLifetime.Hold(reservation,
                    () => !conflictAsyncActive && (conflictPendingTask == null || conflictPendingTask.IsCompleted)
                        && (baselineAdapter == null || !baselineAdapter.IsWorkflowRunning),
                    () => { conflictLifetime = null; conflictPendingTask = null; });
                LogConflictDiagnostic("RESERVATION_ACQUISITION result=true staleRecovered="
                    + reservation.RecoveredStaleMarker.ToString().ToLowerInvariant());
            }
            catch (Exception exception) {
                try { reservation?.Dispose(); }
                catch (Exception releaseException) { LogConflictDiagnostic("RESERVATION_RELEASE_FAILED type="
                    + releaseException.GetType().Name, true); }
                LogConflictDiagnostic("START_GATE reservationReady=false evidenceStorageReady="
                    + evidenceStorageReady.ToString().ToLowerInvariant(), true);
                LogConflictDiagnostic("START_BLOCKED reason=" + exception.GetType().Name
                    + " message=" + exception.Message, true);
                BeginFlow(scenario.scenarioId, scenario.observation);
                Evidence.SetReobservationFailed("CONFLICT DEMO NOT STARTED: existing run reservation or evidence storage unavailable");
                Evidence.SetBoundedOutcome(PatrolActionMapper.Map(PatrolSafetyEnvelope.Stop(
                    "Demo reservation unavailable; conservative stop."),out bool ignored),true);
                return;
            }
            LogConflictDiagnostic("START_GATE reservationReady=true evidenceStorageReady=true");
            LogConflictDiagnostic("START_ALLOWED");
            Run(scenario);
        }

        private void CompleteConflictReservation() { conflictLifetime?.Complete(); }
        private void OnDisable() { AbortConflictRun(); }
        private void OnDestroy() { AbortConflictRun(); }
        private void AbortConflictRun()
        {
            if (conflictLifetime == null) return;
            conflictAborted = true;
            try {
                if (conflictCoroutine != null) StopCoroutine(conflictCoroutine);
                Evidence.SetReobservationFailed("CONFLICT DEMO ABORTED");
                Evidence.SetBoundedOutcome(PatrolActionMapper.Map(PatrolSafetyEnvelope.Stop(
                    "Conflict run aborted; conservative stop."), out bool ignored), true);
                IsRunning = false;
                LogConflictDiagnostic("ABORTED pending work retains reservation until settled", true);
            }
            finally { CompleteConflictReservation(); }
        }

        private static void LogConflictDiagnostic(string message, bool warning = false)
        {
            // A logging failure must not affect reservation, storage, or the safety outcome.
            try {
                if (warning) Debug.LogWarning("[PATROLAI-D-CONFLICT] " + message);
                else Debug.Log("[PATROLAI-D-CONFLICT] " + message);
            }
            catch (Exception) { }
        }

        private async void Run(PatrolDemoScenario scenario)
        {
            if (IsRunning)
                return;

            IsRunning = true;
            bool observingAgenticInspection = false;
            if (scenario.scenarioId == "D") conflictAsyncActive = true;
            if (scenario.scenarioId != "D") BeginFlow(scenario.scenarioId, scenario.observation);
            try
            {
                if (scenario.scenarioId == "D") BeginFlow(scenario.scenarioId, scenario.observation);
                PatrolClosedLoopResult result;
                using (var reasoning = new NebiusNemotronReasoningService())
                {
                    var controller = new PatrolPhysicalAIController(reasoning);
                    var task = controller.RunAsync(scenario.observation);
                    if (scenario.scenarioId == "D") conflictPendingTask = task;
                    result = await task;
                    if (scenario.scenarioId == "D" && conflictAborted) return;
                }

                bool matches = result != null
                    && result.decision != null
                    && result.action != null
                    && string.Equals(result.decision.riskLevel,
                        scenario.expectedRiskLevel, StringComparison.Ordinal)
                    && string.Equals(result.action.actionType,
                        scenario.expectedAction, StringComparison.Ordinal)
                    && string.Equals(result.finalState,
                        scenario.expectedFinalState, StringComparison.Ordinal);

                LatestResult = new PatrolDemoScenarioResult
                {
                    scenario = scenario,
                    closedLoopResult = result,
                    matchesExpectedDirection = matches,
                    message = matches
                        ? "Scenario matched its expected safety direction."
                        : "Scenario completed; inspect the real model decision shown."
                };
                Evidence.Apply(scenario.observation, result);
                if (scenario.scenarioId == "D") {
                    LatestResult.ruleInitialDecision = RuleBasedSafetyBaseline.Decide(scenario.observation);
                    LatestResult.ruleV2InitialDecision = RuleBasedSafetyBaselineV2.Decide(scenario.observation);
                    Evidence.SetComparison("SYNTHETIC SCENARIO EVIDENCE — " + result.runtimeStatus + " NEMOTRON INFERENCE | v1: "
                        + PatrolComparativeEvaluation.Disposition(LatestResult.ruleInitialDecision)
                        + " | v2: " + PatrolComparativeEvaluation.Disposition(LatestResult.ruleV2InitialDecision));
                    if (result.runtimeStatus != "REAL" || !result.success)
                        ApplyConflictTerminal(scenario.observation, result);
                }

                if (ShouldRunAgenticInspection(scenario, result))
                {
                    if (baselineAdapter == null)
                        baselineAdapter = GetComponent<PatrolBaselineActionAdapter>();

                    if (baselineAdapter == null)
                    {
                        LatestResult.message =
                            "Physical re-observation failed: baseline adapter unavailable.";
                        Evidence.SetReobservationFailed(
                            "PHYSICAL RE-OBSERVATION: FAILED — ADAPTER UNAVAILABLE");
                        if (scenario.scenarioId == "D") ApplyConflictTerminal(scenario.observation, null);
                    }
                    else
                    {
                        reobservationStartedUtc = DateTime.UtcNow;
                        baselineAdapter.RunAgenticInspection();
                        reobservationSessionId = PatrolReobservationEvidenceReader.CurrentSessionId();
                        Evidence.SetPhysicalWorkflowStatus(
                            "PHYSICAL RE-OBSERVATION: RUNNING");
                        var coroutine = StartCoroutine(ObserveAgenticInspection(scenario));
                        if (scenario.scenarioId == "D") conflictCoroutine = coroutine;
                        observingAgenticInspection = true;
                    }
                }
            }
            catch (Exception exception)
            {
                var failed = new PatrolClosedLoopResult
                {
                    runtimeStatus = "FAILED",
                    success = false,
                    message = "Scenario failed: " + exception.GetType().Name
                };
                LatestResult = new PatrolDemoScenarioResult
                {
                    scenario = scenario,
                    closedLoopResult = failed,
                    matchesExpectedDirection = false,
                    message = failed.message
                };
                Evidence.Apply(scenario.observation, failed);
                if (scenario.scenarioId == "D") ApplyConflictTerminal(scenario.observation, failed);
            }
            finally
            {
                if (scenario.scenarioId == "D") conflictAsyncActive = false;
                if (!observingAgenticInspection) {
                    IsRunning = false;
                    if (scenario.scenarioId == "D") CompleteConflictReservation();
                }
            }
        }

        public static bool ShouldRunAgenticInspection(
            PatrolDemoScenario scenario,
            PatrolClosedLoopResult result)
        {
            return scenario != null
                && (string.Equals(scenario.scenarioId, "B", StringComparison.Ordinal)
                    || string.Equals(scenario.scenarioId, "D", StringComparison.Ordinal))
                && result != null
                && result.success
                && result.runtimeStatus == "REAL"
                && result.validatorSuccess
                && result.decision != null
                && (scenario.scenarioId == "D" || (
                (string.Equals(result.decision.riskLevel, "UNKNOWN", StringComparison.Ordinal)
                    || string.Equals(result.decision.riskLevel, "WARNING", StringComparison.Ordinal))
                && result.action != null
                && string.Equals(result.action.actionType, "REOBSERVE", StringComparison.Ordinal)
                && string.Equals(result.finalState, "REOBSERVING", StringComparison.Ordinal)));
        }

        private IEnumerator ObserveAgenticInspection(PatrolDemoScenario scenario)
        {
            if (scenario.scenarioId != "D") {
                var unchanged = ObserveAgenticInspectionCore(scenario);
                while (unchanged.MoveNext()) yield return unchanged.Current;
                yield break;
            }
            var workflow = ObserveAgenticInspectionCore(scenario);
            try {
                while (!conflictAborted) {
                    bool more;
                    try { more = workflow.MoveNext(); }
                    catch (Exception exception) {
                        LatestResult.message = "Conflict re-observation failed: " + exception.GetType().Name;
                        Evidence.SetReobservationFailed(LatestResult.message);
                        ApplyConflictTerminal(scenario.observation, null);
                        IsRunning = false;
                        break;
                    }
                    if (!more) break;
                    yield return workflow.Current;
                }
            }
            finally {
                try { (workflow as IDisposable)?.Dispose(); }
                finally { CompleteConflictReservation(); }
            }
        }

        private IEnumerator ObserveAgenticInspectionCore(PatrolDemoScenario scenario)
        {
            yield return null;
            while (baselineAdapter != null
                && baselineAdapter.AgenticStatus == "RUNNING")
            {
                if (DateTime.UtcNow - reobservationStartedUtc > TimeSpan.FromMinutes(2))
                {
                    Evidence.SetReobservationFailed("PHYSICAL RE-OBSERVATION TIMEOUT");
                    if (scenario.scenarioId == "D") ApplyConflictTerminal(scenario.observation, null);
                    IsRunning = false;
                    yield break;
                }
                yield return null;
            }

            bool complete = baselineAdapter != null
                && baselineAdapter.AgenticStatus == "COMPLETE";
            string status = baselineAdapter == null
                ? "FAILED — ADAPTER LOST"
                : baselineAdapter.AgenticStatus;
            LatestResult.message = complete
                ? "Physical re-observation completed through the inherited agentic workflow."
                : "Physical re-observation failed: " + status;
            Evidence.SetPhysicalWorkflowStatus(
                "PHYSICAL RE-OBSERVATION: " + (complete ? "COMPLETE" : status));

            if (!complete)
            {
                Evidence.SetReobservationFailed(LatestResult.message);
                if (scenario.scenarioId == "D") ApplyConflictTerminal(scenario.observation, null);
                IsRunning = false;
                yield break;
            }

            if (!PatrolReobservationEvidenceReader.TryReadLatestSince(
                reobservationStartedUtc,
                reobservationSessionId,
                scenario.observation,
                out PatrolObservation updatedObservation,
                out string evidenceError))
            {
                LatestResult.message = evidenceError;
                Evidence.SetReobservationFailed(evidenceError);
                if (scenario.scenarioId == "D") ApplyConflictTerminal(scenario.observation, null);
                IsRunning = false;
                yield break;
            }

            if (scenario.scenarioId == "D")
                updatedObservation = PatrolConflictDemoScenario.AddResolvedContext(updatedObservation);
            LatestResult.reobservation = updatedObservation;
            Evidence.SetReobservationComplete(updatedObservation);
            Task<PatrolClosedLoopResult> secondTask = RunSecondReasoningAsync(updatedObservation);
            if (scenario.scenarioId == "D") conflictPendingTask = secondTask;
            while (!secondTask.IsCompleted)
                yield return null;

            if (secondTask.IsFaulted || secondTask.IsCanceled)
            {
                LatestResult.message = "Second Nemotron reasoning failed before a result was returned.";
                Evidence.SetReobservationFailed(LatestResult.message);
                if (scenario.scenarioId == "D") ApplyConflictTerminal(updatedObservation, null);
                IsRunning = false;
                yield break;
            }

            PatrolClosedLoopResult second = secondTask.Result;
            LatestResult.secondClosedLoopResult = second;
            LatestResult.decisionChanged = LatestResult.closedLoopResult != null
                && LatestResult.closedLoopResult.decision != null
                && second != null && second.decision != null
                && (!string.Equals(LatestResult.closedLoopResult.decision.riskLevel,
                        second.decision.riskLevel, StringComparison.Ordinal)
                    || !string.Equals(LatestResult.closedLoopResult.decision.action,
                        second.decision.action, StringComparison.Ordinal));
            LatestResult.message = second != null && second.runtimeStatus == "REAL"
                ? "Re-observation completed and a second REAL Nemotron decision was applied."
                : "Second Nemotron reasoning did not complete as REAL.";
            Evidence.ApplySecond(updatedObservation, second);
            if (scenario.scenarioId == "D") {
                ApplyConflictTerminal(updatedObservation, second);
                LatestResult.ruleFinalDecision = PatrolSafetyEnvelope.Apply(RuleBasedSafetyBaseline.Decide(updatedObservation),
                    updatedObservation,1,true,out bool ignoredV1,PatrolSafetyEnvelope.DemoMaximumReobservations);
                LatestResult.ruleV2FinalDecision = PatrolSafetyEnvelope.Apply(RuleBasedSafetyBaselineV2.Propose(updatedObservation),
                    updatedObservation,1,true,out bool ignoredV2,PatrolSafetyEnvelope.DemoMaximumReobservations);
                Evidence.SetComparison("SYNTHETIC SCENARIO EVIDENCE | v1 FINAL: "
                    + PatrolComparativeEvaluation.Disposition(LatestResult.ruleFinalDecision)
                    + " | v2 FINAL: " + PatrolComparativeEvaluation.Disposition(LatestResult.ruleV2FinalDecision)
                    + (LatestResult.safetyEnvelopeEscalated ? " | ENVELOPE STOP / HUMAN REVIEW" : " | BOUNDED MODEL ACTION"));
            }
            try
            {
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "..", "evaluation", "results"));
                if (scenario.scenarioId == "D") directory = Path.Combine(directory, "phase7c");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory,
                    "phase7_demo_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".json"),
                    JsonUtility.ToJson(LatestResult, true));
            }
            catch (Exception exception)
            {
                Evidence.SetPhysicalWorkflowStatus("DEMO EVIDENCE SAVE FAILED: "
                    + exception.GetType().Name);
            }
            IsRunning = false;
        }

        private static async Task<PatrolClosedLoopResult> RunSecondReasoningAsync(
            PatrolObservation observation)
        {
            using (var reasoning = new NebiusNemotronReasoningService())
            {
                var controller = new PatrolPhysicalAIController(reasoning);
                return await controller.RunAsync(observation).ConfigureAwait(false);
            }
        }

        private void ApplyConflictTerminal(PatrolObservation observation, PatrolClosedLoopResult raw)
        {
            bool valid = raw != null && raw.runtimeStatus == "REAL" && raw.success
                && raw.parserSuccess && raw.validatorSuccess && !raw.usedSafeFallback;
            var bounded = PatrolSafetyEnvelope.Apply(raw == null ? null : raw.decision, observation,
                1, valid, out bool escalated, PatrolSafetyEnvelope.DemoMaximumReobservations);
            LatestResult.finalBoundedDecision = bounded;
            LatestResult.safetyEnvelopeEscalated = escalated;
            Evidence.SetBoundedOutcome(PatrolActionMapper.Map(bounded, out bool ignored), escalated);
        }
    }
}
