using System;
using System.Collections;
using PatrolAI.Competition;
using PatrolAI.NVIDIA;
using PatrolAI.PhysicalAI;
using PatrolAI.UI;
using UnityEngine;

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
                    }
                    else
                    {
                        baselineAdapter.RunAgenticInspection();
                        Evidence.SetPhysicalWorkflowStatus(
                            "PHYSICAL RE-OBSERVATION: RUNNING");
                        StartCoroutine(ObserveAgenticInspection());
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
                && string.Equals(scenario.scenarioId, "B", StringComparison.Ordinal)
                && result != null
                && result.success
                && result.validatorSuccess
                && result.decision != null
                && string.Equals(result.decision.riskLevel, "UNKNOWN", StringComparison.Ordinal)
                && result.action != null
                && string.Equals(result.action.actionType, "REOBSERVE", StringComparison.Ordinal)
                && string.Equals(result.finalState, "REOBSERVING", StringComparison.Ordinal);
        }

        private IEnumerator ObserveAgenticInspection()
        {
            yield return null;
            while (baselineAdapter != null
                && baselineAdapter.AgenticStatus == "RUNNING")
            {
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
            IsRunning = false;
        }
    }
}
