using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using PatrolAI.Competition;
using PatrolAI.PhysicalAI;
using PatrolAI.UI;
using VisionGuard.Phase3;
using VisionGuard.Phase4;
using VisionGuard.Phase5;

namespace PatrolAI.Scenarios
{
    public static class PatrolPhase5TestHarness
    {
        public static string RunAll()
        {
            var passed = new List<string>();
            Run("evidence state accepts REAL result", TestRealEvidence, passed);
            Run("FAILED result clears stale REAL success", TestFailureClearsReal, passed);
            Run("FALLBACK is visibly distinguished", TestFallbackVisible, passed);
            Run("MOCK cannot be reported as REAL", TestMockNotReal, passed);
            Run("SAFE scenario mapping", () => TestScenario(PatrolDemoScenario.Safe()), passed);
            Run("insufficient-evidence scenario mapping",
                () => TestScenario(PatrolDemoScenario.InsufficientEvidence()), passed);
            Run("hazard scenario mapping", () => TestScenario(PatrolDemoScenario.Hazard()), passed);
            Run("baseline adapter resolves required public entry points",
                TestBaselineEntryPoints, passed);
            Run("button callbacks call correct baseline entry points",
                TestButtonDispatch, passed);
            Run("baseline algorithms are not duplicated", TestDelegationOnly, passed);
            Run("A does not trigger agentic inspection", TestSafeDoesNotTriggerAgentic, passed);
            Run("B triggers agentic inspection", TestReobserveTriggersAgentic, passed);
            Run("C does not trigger agentic inspection", TestHazardDoesNotTriggerAgentic, passed);
            Run("B remains REOBSERVING while inspection runs",
                TestReobservingStateWhileInspectionRuns, passed);
            Run("agentic inspection failure is reported truthfully",
                TestAgenticFailureVisible, passed);

            var report = new StringBuilder();
            foreach (string name in passed)
                report.AppendLine("PASS: " + name);
            report.Append("SUMMARY: ").Append(passed.Count).Append("/15 PASS");
            return report.ToString();
        }

        private static void TestRealEvidence()
        {
            var state = new PatrolCompetitionEvidenceState();
            state.Apply(Observation("INDICATOR_RED", "SUFFICIENT", 0.95f),
                Result("REAL", "DANGER", "STOP", "STOPPED", true));
            Assert(state.RuntimeStatus == "REAL", "Verified REAL result was not accepted.");
            Assert(state.PatrolDecision == "DANGER / STOP", "Decision was not displayed.");
        }

        private static void TestFailureClearsReal()
        {
            var state = new PatrolCompetitionEvidenceState();
            PatrolObservation observation = Observation("INDICATOR_RED", "SUFFICIENT", 0.95f);
            state.Apply(observation, Result("REAL", "DANGER", "STOP", "STOPPED", true));
            state.Apply(observation, new PatrolClosedLoopResult
            {
                runtimeStatus = "FAILED",
                message = "network unavailable"
            });
            Assert(state.RuntimeStatus == "FAILED", "Failure was not visible.");
            Assert(state.PatrolDecision == "NOT VERIFIED", "Stale REAL decision remained.");
            Assert(state.PatrolAction == "NOT EXECUTED", "Stale action remained.");
        }

        private static void TestFallbackVisible()
        {
            var state = new PatrolCompetitionEvidenceState();
            PatrolClosedLoopResult result = Result(
                "FALLBACK", "UNKNOWN", "REOBSERVE", "REOBSERVING", false);
            result.usedSafeFallback = true;
            state.Apply(Observation("INDICATOR_UNCLEAR", "INSUFFICIENT", 0.3f), result);
            Assert(state.RuntimeStatus == "FALLBACK", "Fallback status was hidden.");
            Assert(state.Detail.Contains("Fallback source"), "Fallback source was absent.");
        }

        private static void TestMockNotReal()
        {
            var state = new PatrolCompetitionEvidenceState();
            state.Apply(Observation("INDICATOR_RED", "SUFFICIENT", 0.95f),
                Result("MOCK", "DANGER", "STOP", "STOPPED", true));
            Assert(state.RuntimeStatus == "MOCK", "Mock result was mislabeled.");
        }

        private static void TestScenario(PatrolDemoScenario scenario)
        {
            PatrolAction action = PatrolActionMapper.Map(new PatrolDecision
            {
                decisionId = "phase5-offline",
                riskLevel = scenario.expectedRiskLevel,
                action = scenario.expectedAction,
                reason = "Deterministic scenario mapping test.",
                requiresReobservation = scenario.expectedAction == "REOBSERVE",
                confidence = scenario.observation.confidence
            }, out bool usedFallback);
            Assert(!usedFallback, "Expected direction unexpectedly used fallback.");
            Assert(action.actionType == scenario.expectedAction, "Action mapping mismatch.");
        }

        private static void TestBaselineEntryPoints()
        {
            Assert(PublicMethod(typeof(VGPhase3AgentController), "RunAgenticInspection"),
                "Agentic inspection entry point missing.");
            Assert(PublicMethod(typeof(VGPhase4FusionController), "RunMultiSourceFusion"),
                "Fusion entry point missing.");
            Assert(PublicMethod(typeof(VGPhase5TemporalController), "EvaluateLatestFusion"),
                "Temporal entry point missing.");
        }

        private static void TestButtonDispatch()
        {
            int agentic = 0;
            int fusion = 0;
            int temporal = 0;
            Action a = () => agentic++;
            Action f = () => fusion++;
            Action t = () => temporal++;

            PatrolBaselineActionAdapter.Dispatch(PatrolBaselineAction.AgenticInspection, a, f, t);
            Assert(agentic == 1 && fusion == 0 && temporal == 0, "Agentic dispatch mismatch.");
            PatrolBaselineActionAdapter.Dispatch(PatrolBaselineAction.MultiSourceFusion, a, f, t);
            Assert(agentic == 1 && fusion == 1 && temporal == 0, "Fusion dispatch mismatch.");
            PatrolBaselineActionAdapter.Dispatch(PatrolBaselineAction.LatestVerifiedFusion, a, f, t);
            Assert(agentic == 1 && fusion == 1 && temporal == 1, "Temporal dispatch mismatch.");
        }

        private static void TestDelegationOnly()
        {
            Type adapter = typeof(PatrolBaselineActionAdapter);
            foreach (Type nested in adapter.GetNestedTypes(
                BindingFlags.Public | BindingFlags.NonPublic))
            {
                Assert(!nested.Name.Contains("Response"),
                    "Adapter contains a copied baseline response algorithm type.");
            }
            foreach (FieldInfo field in adapter.GetFields(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
            {
                Assert(field.FieldType != typeof(Process),
                    "Adapter must not run baseline processes itself.");
            }
        }

        private static void TestSafeDoesNotTriggerAgentic()
        {
            PatrolDemoScenario scenario = PatrolDemoScenario.Safe();
            Assert(!PatrolDemoScenarioRunner.ShouldRunAgenticInspection(
                scenario, Result("REAL", "SAFE", "CONTINUE", "CONTINUING", true)),
                "Scenario A must not trigger agentic inspection.");
        }

        private static void TestReobserveTriggersAgentic()
        {
            PatrolDemoScenario scenario = PatrolDemoScenario.InsufficientEvidence();
            Assert(PatrolDemoScenarioRunner.ShouldRunAgenticInspection(
                scenario, Result("REAL", "UNKNOWN", "REOBSERVE", "REOBSERVING", true)),
                "Validated Scenario B must trigger agentic inspection.");
        }

        private static void TestHazardDoesNotTriggerAgentic()
        {
            PatrolDemoScenario scenario = PatrolDemoScenario.Hazard();
            Assert(!PatrolDemoScenarioRunner.ShouldRunAgenticInspection(
                scenario, Result("REAL", "DANGER", "STOP", "STOPPED", true)),
                "Scenario C must not trigger agentic inspection.");
        }

        private static void TestReobservingStateWhileInspectionRuns()
        {
            var state = new PatrolCompetitionEvidenceState();
            PatrolClosedLoopResult result = Result(
                "REAL", "UNKNOWN", "REOBSERVE", "REOBSERVING", true);
            state.Apply(PatrolDemoScenario.InsufficientEvidence().observation, result);
            state.SetPhysicalWorkflowStatus("PHYSICAL RE-OBSERVATION: RUNNING");
            Assert(state.FinalState == "REOBSERVING",
                "Physical state must remain REOBSERVING during inspection.");
            Assert(state.Detail == "PHYSICAL RE-OBSERVATION: RUNNING",
                "Running inspection status is not visible.");
        }

        private static void TestAgenticFailureVisible()
        {
            var state = new PatrolCompetitionEvidenceState();
            PatrolClosedLoopResult result = Result(
                "REAL", "UNKNOWN", "REOBSERVE", "REOBSERVING", true);
            state.Apply(PatrolDemoScenario.InsufficientEvidence().observation, result);
            state.SetPhysicalWorkflowStatus(
                "PHYSICAL RE-OBSERVATION: FAILED — BASELINE CONTROLLER FAILED");
            Assert(state.RuntimeStatus == "REAL",
                "A physical workflow failure must not falsify the completed reasoning mode.");
            Assert(state.Detail.Contains("FAILED"),
                "Physical workflow failure must be visible.");
        }

        private static bool PublicMethod(Type type, string name)
        {
            return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public,
                null, Type.EmptyTypes, null) != null;
        }

        private static PatrolObservation Observation(string condition, string evidence, float confidence)
        {
            return new PatrolObservation
            {
                observationId = "phase5-test",
                detectedCondition = condition,
                evidenceStatus = evidence,
                confidence = confidence
            };
        }

        private static PatrolClosedLoopResult Result(
            string runtime,
            string risk,
            string action,
            string finalState,
            bool verified)
        {
            return new PatrolClosedLoopResult
            {
                runtimeStatus = runtime,
                success = verified,
                httpSuccess = verified,
                parserSuccess = verified,
                validatorSuccess = verified,
                decision = new PatrolDecision
                {
                    riskLevel = risk,
                    action = action,
                    reason = "test",
                    confidence = 0.95f
                },
                action = new PatrolAction { actionType = action },
                finalState = finalState,
                elapsedMilliseconds = 100
            };
        }

        private static void Run(string name, Action test, ICollection<string> passed)
        {
            try
            {
                test();
                passed.Add(name);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(name + ": " + exception.Message, exception);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

#if PATROLAI_PHASE5_OFFLINE_TESTS
        public static int Main()
        {
            try
            {
                Console.WriteLine(RunAll());
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FAIL: " + exception.Message);
                return 1;
            }
        }
#endif
    }
}
