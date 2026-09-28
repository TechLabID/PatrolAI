using System;
using System.Collections.Generic;
using System.Text;

namespace PatrolAI.NVIDIA
{
    /// <summary>
    /// Package-free targeted tests for the Phase 2.5 offline preparation layer.
    /// This harness performs no network, scene, movement, UI, or API operation.
    /// </summary>
    public static class NemotronOfflineTestHarness
    {
        public static string RunAll()
        {
            var passed = new List<string>();

            Run("valid SAFE response", TestValidSafe, passed);
            Run("valid WARNING response", TestValidWarning, passed);
            Run("valid DANGER response", TestValidDanger, passed);
            Run("JSON code fence response", TestJsonCodeFence, passed);
            Run("surrounding prose rejected", TestSurroundingProseRejected, passed);
            Run("malformed JSON", TestMalformedJson, passed);
            Run("unsupported risk level", TestUnsupportedRisk, passed);
            Run("unsupported action", TestUnsupportedAction, passed);
            Run("missing field", TestMissingField, passed);
            Run("confidence outside valid range", TestInvalidConfidence, passed);
            Run("insufficient evidence safe fallback", TestInsufficientEvidence, passed);
            Run("deterministic mock reasoning", TestDeterministicMock, passed);

            var report = new StringBuilder();
            foreach (string testName in passed)
            {
                report.AppendLine("PASS: " + testName);
            }

            report.Append("SUMMARY: ").Append(passed.Count).Append("/12 PASS");
            return report.ToString();
        }

        private static void TestValidSafe()
        {
            PatrolDecision decision = ParseAndValidate(
                "{\"riskLevel\":\"safe\",\"action\":\"continue\","
                + "\"reason\":\"Area is clear.\",\"requiresReobservation\":false,"
                + "\"confidence\":0.95}");
            AssertDecision(decision, "SAFE", "CONTINUE", false);
        }

        private static void TestValidWarning()
        {
            PatrolDecision decision = ParseAndValidate(
                "{\"riskLevel\":\"WARNING\",\"action\":\"INSPECT\","
                + "\"reason\":\"Indicator needs inspection.\","
                + "\"requiresReobservation\":false,\"confidence\":0.8}");
            AssertDecision(decision, "WARNING", "INSPECT", false);
        }

        private static void TestValidDanger()
        {
            PatrolDecision decision = ParseAndValidate(
                "{\"riskLevel\":\"DANGER\",\"action\":\"STOP\","
                + "\"reason\":\"Immediate unsafe condition.\","
                + "\"requiresReobservation\":false,\"confidence\":0.99}");
            AssertDecision(decision, "DANGER", "STOP", false);
        }

        private static void TestJsonCodeFence()
        {
            PatrolDecision decision = ParseAndValidate(
                "```json\n{\"riskLevel\":\"WARNING\",\"action\":\"INSPECT\"," 
                + "\"reason\":\"Inspect red indicator.\"," 
                + "\"requiresReobservation\":false,\"confidence\":0.9}\n```");
            AssertDecision(decision, "WARNING", "INSPECT", false);
        }

        private static void TestSurroundingProseRejected()
        {
            bool parsed = NemotronDecisionParser.TryParse(
                "Decision: {\"riskLevel\":\"DANGER\",\"action\":\"STOP\"," 
                + "\"reason\":\"Red indicator.\",\"requiresReobservation\":false," 
                + "\"confidence\":0.9}",
                out NemotronReasoningResponse response,
                out string errorMessage);
            Assert(!parsed && response == null && !string.IsNullOrWhiteSpace(errorMessage),
                "Surrounding prose must be rejected.");
        }

        private static void TestMalformedJson()
        {
            bool parsed = NemotronDecisionParser.TryParse(
                "{not-json}",
                out NemotronReasoningResponse response,
                out string errorMessage);
            Assert(!parsed && response == null && !string.IsNullOrWhiteSpace(errorMessage),
                "Malformed JSON must fail clearly.");
        }

        private static void TestUnsupportedRisk()
        {
            PatrolDecision decision = ParseAndValidate(
                "{\"riskLevel\":\"CRITICAL\",\"action\":\"STOP\","
                + "\"reason\":\"Unsupported risk value.\","
                + "\"requiresReobservation\":false,\"confidence\":0.9}");
            AssertDecision(decision, "UNKNOWN", "REOBSERVE", true);
        }

        private static void TestUnsupportedAction()
        {
            PatrolDecision decision = ParseAndValidate(
                "{\"riskLevel\":\"WARNING\",\"action\":\"FLY\","
                + "\"reason\":\"Unsupported action value.\","
                + "\"requiresReobservation\":false,\"confidence\":0.9}");
            AssertDecision(decision, "UNKNOWN", "REOBSERVE", true);
        }

        private static void TestMissingField()
        {
            bool parsed = NemotronDecisionParser.TryParse(
                "{\"riskLevel\":\"SAFE\",\"action\":\"CONTINUE\","
                + "\"requiresReobservation\":false,\"confidence\":0.9}",
                out NemotronReasoningResponse response,
                out string errorMessage);
            Assert(!parsed && response == null && !string.IsNullOrWhiteSpace(errorMessage),
                "Missing required field must fail clearly.");
        }

        private static void TestInvalidConfidence()
        {
            PatrolDecision decision = ParseAndValidate(
                "{\"riskLevel\":\"SAFE\",\"action\":\"CONTINUE\","
                + "\"reason\":\"Invalid confidence.\","
                + "\"requiresReobservation\":false,\"confidence\":1.5}");
            AssertDecision(decision, "UNKNOWN", "REOBSERVE", true);
        }

        private static void TestInsufficientEvidence()
        {
            var service = new MockNemotronReasoningService();
            PatrolDecision decision = service.ReasonAsync(new PatrolObservation
            {
                observationId = "insufficient-1",
                detectedCondition = "INDICATOR_RED",
                evidenceStatus = "INSUFFICIENT",
                confidence = 0.2f
            }).GetAwaiter().GetResult();

            AssertDecision(decision, "UNKNOWN", "REOBSERVE", true);
        }

        private static void TestDeterministicMock()
        {
            var service = new MockNemotronReasoningService();
            var observation = new PatrolObservation
            {
                observationId = "red-1",
                detectedCondition = "INDICATOR_RED",
                evidenceStatus = "CONFIRMED",
                confidence = 0.9f
            };

            PatrolDecision first = service.ReasonAsync(observation).GetAwaiter().GetResult();
            PatrolDecision second = service.ReasonAsync(observation).GetAwaiter().GetResult();

            AssertDecision(first, "DANGER", "STOP", false);
            Assert(first.riskLevel == second.riskLevel
                && first.action == second.action
                && first.reason == second.reason
                && first.requiresReobservation == second.requiresReobservation
                && Math.Abs(first.confidence - second.confidence) < 0.0001f,
                "Mock reasoning must be deterministic.");
        }

        private static PatrolDecision ParseAndValidate(string json)
        {
            bool parsed = NemotronDecisionParser.TryParse(
                json,
                out NemotronReasoningResponse response,
                out string errorMessage);
            Assert(parsed, "Expected valid parse: " + errorMessage);
            return NemotronDecisionValidator.Validate(response, "offline-test");
        }

        private static void AssertDecision(
            PatrolDecision decision,
            string riskLevel,
            string action,
            bool requiresReobservation)
        {
            Assert(decision != null, "Decision must not be null.");
            Assert(decision.riskLevel == riskLevel, "Unexpected risk level.");
            Assert(decision.action == action, "Unexpected action.");
            Assert(decision.requiresReobservation == requiresReobservation,
                "Unexpected re-observation flag.");
        }

        private static void Run(string testName, Action test, ICollection<string> passed)
        {
            try
            {
                test();
                passed.Add(testName);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(testName + ": " + exception.Message, exception);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

#if PATROLAI_OFFLINE_TESTS
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
