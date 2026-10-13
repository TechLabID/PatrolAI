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
        private PatrolBaselineActionAdapter baselineAdapter;
        private DateTime reobservationStartedUtc;
        private string reobservationSessionId;

        private void Awake()
        {
            baselineAdapter = GetComponent<PatrolBaselineActionAdapter>();
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
            if (IsRunning) return;
            var scenario = PatrolConflictDemoScenario.Create();
            try {
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "..", "evaluation", "results", "phase7c"));
                Directory.CreateDirectory(directory);
                // Reserve the only two demo calls before attempting the first one.
                using (var stream = new FileStream(Path.Combine(directory, "demo_started.json"),
                    FileMode.CreateNew, FileAccess.Write)) {
                    byte[] marker = System.Text.Encoding.UTF8.GetBytes("{\"reservedRequests\":2}");
                    stream.Write(marker, 0, marker.Length);
                }
            }
            catch (Exception) {
                Evidence.Begin(scenario.observation);
                Evidence.SetReobservationFailed("CONFLICT DEMO NOT STARTED: existing run reservation or evidence storage unavailable");
                Evidence.SetBoundedOutcome(PatrolActionMapper.Map(PatrolSafetyEnvelope.Stop(
                    "Demo reservation unavailable; conservative stop."),out bool ignored),true);
                return;
            }
            Run(scenario);
        }

        private async void Run(PatrolDemoScenario scenario)
        {
            if (IsRunning)
                return;

            IsRunning = true;
            bool observingAgenticInspection = false;
            Evidence.Begin(scenario.observation);
            try
            {
                PatrolClosedLoopResult result;
                using (var reasoning = new NebiusNemotronReasoningService())
                {
                    var controller = new PatrolPhysicalAIController(reasoning);
                    result = await controller.RunAsync(scenario.observation);
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
                        Evidence.SetPhysicalWorkflowStatus(
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
                        StartCoroutine(ObserveAgenticInspection(scenario));
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
                if (!observingAgenticInspection)
                    IsRunning = false;
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
