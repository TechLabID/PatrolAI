using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatrolAI.NVIDIA;

namespace PatrolAI.PhysicalAI
{
    /// <summary>
    /// Package-free deterministic Phase 4 tests. No network or Unity runtime operation.
    /// </summary>
    public static class PatrolClosedLoopTestHarness
    {
        public static string RunAll()
        {
            var passed = new List<string>();
            Run("SAFE maps to CONTINUE", () => AssertMap("SAFE", "CONTINUE", "CONTINUE"), passed);
            Run("WARNING maps to INSPECT", () => AssertMap("WARNING", "INSPECT", "INSPECT"), passed);
            Run("WARNING maps to REOBSERVE", () => AssertMap("WARNING", "REOBSERVE", "REOBSERVE"), passed);
            Run("DANGER maps to STOP", () => AssertMap("DANGER", "STOP", "STOP"), passed);
            Run("DANGER maps to ALERT", () => AssertMap("DANGER", "ALERT", "ALERT"), passed);
            Run("UNKNOWN maps to REOBSERVE", () => AssertMap("UNKNOWN", "REOBSERVE", "REOBSERVE"), passed);
            Run("invalid decision safely falls back", TestInvalidDecisionFallback, passed);
            Run("controller transitions to simulated action", TestControllerTransitions, passed);
            Run("mock runtime is labeled MOCK", TestMockRuntimeLabel, passed);
            Run("failed reasoning is not REAL success", TestFailedReasoning, passed);

            var report = new StringBuilder();
            foreach (string name in passed)
            {
                report.AppendLine("PASS: " + name);
            }
            report.Append("SUMMARY: ").Append(passed.Count).Append("/10 PASS");
            return report.ToString();
        }

        private static void AssertMap(string risk, string action, string expected)
        {
            PatrolAction mapped = PatrolActionMapper.Map(
                Decision(risk, action),
                out bool usedSafeFallback);
            Assert(!usedSafeFallback, "Expected a supported mapping.");
            Assert(mapped.actionType == expected, "Unexpected action mapping.");
        }

        private static void TestInvalidDecisionFallback()
        {
            PatrolAction mapped = PatrolActionMapper.Map(
                Decision("SAFE", "STOP"),
                out bool usedSafeFallback);
            Assert(usedSafeFallback, "Invalid mapping must use the safe fallback.");
            Assert(mapped.actionType == "REOBSERVE", "Fallback action must be REOBSERVE.");
        }

        private static void TestControllerTransitions()
        {
            PatrolClosedLoopResult result = RunMock();
            string path = string.Join(">", result.statePath);
            Assert(path == "IDLE>OBSERVING>REASONING>DECIDING>ACTING>STOPPED",
                "Unexpected controller state path: " + path);
            Assert(result.action.actionType == "STOP" && result.finalState == "STOPPED",
                "Mock danger decision must produce the simulated STOPPED state.");
        }

        private static void TestMockRuntimeLabel()
        {
            PatrolClosedLoopResult result = RunMock();
            Assert(result.success, "Deterministic mock cycle should complete.");
            Assert(result.runtimeStatus == "MOCK", "Mock reasoning must be labeled MOCK.");
        }

        private static void TestFailedReasoning()
        {
            var controller = new PatrolPhysicalAIController(new FailingReasoningService());
            PatrolClosedLoopResult result = controller.RunAsync(Observation())
                .GetAwaiter().GetResult();
            Assert(!result.success, "Failed reasoning must not succeed.");
            Assert(result.runtimeStatus == "FAILED", "Failure must be labeled FAILED.");
            Assert(result.finalState == "ERROR", "Failed reasoning must end in ERROR.");
        }

        private static PatrolClosedLoopResult RunMock()
        {
            var controller = new PatrolPhysicalAIController(
                new MockNemotronReasoningService());
            return controller.RunAsync(Observation()).GetAwaiter().GetResult();
        }

        private static PatrolObservation Observation()
        {
            return new PatrolObservation
            {
                observationId = "phase4-offline-red",
                detectedCondition = "INDICATOR_RED",
                evidenceStatus = "SUFFICIENT",
                confidence = 0.95f,
                location = "Industrial inspection point A"
            };
        }

        private static PatrolDecision Decision(string risk, string action)
        {
            return new PatrolDecision
            {
                decisionId = "phase4-test-decision",
                riskLevel = risk,
                action = action,
                reason = "Deterministic mapping test.",
                requiresReobservation = action == "REOBSERVE" || risk == "UNKNOWN",
                confidence = 0.9f
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
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class FailingReasoningService : INemotronReasoningService
        {
            public Task<PatrolDecision> ReasonAsync(PatrolObservation observation)
            {
                throw new InvalidOperationException("Deterministic offline failure.");
            }
        }

#if PATROLAI_PHASE4_OFFLINE_TESTS
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
