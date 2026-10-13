using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PatrolAI.Evaluation
{
    public static class PatrolAIEvaluationTestHarness
    {
        public static string RunAll()
        {
            var passed = new List<string>();
            Run("dataset has exactly 24 frozen cases", TestDatasetCount, passed);
            Run("category distribution is 4/4/4/6/4/2", TestCategories, passed);
            Run("expected labels are immutable", TestExpectedLabelImmutability, passed);
            Run("clear red rule stops", TestClearRed, passed);
            Run("clear green rule continues", TestClearGreen, passed);
            Run("insufficient rule re-observes", TestInsufficient, passed);
            Run("conflicting red rule is conservatively stopped", TestConflict, passed);
            Run("metric calculations distinguish safety errors", TestMetrics, passed);
            Run("benchmark result serialization is public-safe", TestSerialization, passed);
            Run("benchmark request ceiling is 24", TestRequestLimit, passed);

            var report = new StringBuilder();
            foreach (string name in passed) report.AppendLine("PASS: " + name);
            report.Append("SUMMARY: ").Append(passed.Count).Append("/10 PASS");
            return report.ToString();
        }

        private static void TestDatasetCount()
        {
            Assert(PatrolAIEvaluationDataset.Create().Count == 24, "Expected 24 cases.");
        }

        private static void TestCategories()
        {
            string actual = string.Join(",", PatrolAIEvaluationDataset.Create()
                .GroupBy(c => c.category).OrderBy(g => g.Key)
                .Select(g => g.Key + "=" + g.Count()));
            const string expected = "AMBIGUOUS=4,CLEAR_HAZARD=4,CLEAR_SAFE=4,"
                + "CONFLICTING=6,DEGRADED=2,REOBSERVATION=4";
            Assert(actual == expected, "Unexpected categories: " + actual);
        }

        private static void TestExpectedLabelImmutability()
        {
            Type type = typeof(PatrolAIEvaluationCase);
            Assert(type.GetProperty("ExpectedRiskLevel").CanWrite == false,
                "Expected risk must not have a setter.");
            Assert(type.GetProperty("ExpectedAction").CanWrite == false,
                "Expected action must not have a setter.");
            string before = PatrolAIBenchmarkRunner.LabelFingerprint(
                PatrolAIEvaluationDataset.Create());
            string after = PatrolAIBenchmarkRunner.LabelFingerprint(
                PatrolAIEvaluationDataset.Create());
            Assert(before == after, "Dataset labels are not deterministic.");
        }

        private static void TestClearRed()
        {
            AssertDisposition(Case("INDICATOR_RED", "SUFFICIENT", .9f, "sources=1"),
                "DANGER/STOP");
        }

        private static void TestClearGreen()
        {
            AssertDisposition(Case("INDICATOR_GREEN", "SUFFICIENT", .9f, "sources=1"),
                "SAFE/CONTINUE");
        }

        private static void TestInsufficient()
        {
            AssertDisposition(Case("INDICATOR_RED", "INSUFFICIENT", .5f, "sources=1"),
                "UNKNOWN/REOBSERVE");
        }

        private static void TestConflict()
        {
            AssertDisposition(Case("INDICATOR_RED", "CONFLICT", .9f,
                "sources=2; agreement=conflict; current RED and current GREEN"),
                "DANGER/STOP");
        }

        private static void TestMetrics()
        {
            var values = new[]
            {
                Result("SAFE/CONTINUE", "SAFE/CONTINUE"),
                Result("DANGER/STOP", "SAFE/CONTINUE"),
                Result("UNKNOWN/REOBSERVE", "DANGER/STOP")
            };
            PatrolAIBenchmarkMetrics metrics = PatrolAIBenchmarkMetrics.Calculate(
                "RULE", values, false);
            Assert(metrics.correct == 1 && metrics.falseStops == 1
                && metrics.missedHazards == 1 && metrics.inappropriateContinues == 1,
                "Safety error metrics were calculated incorrectly.");
        }

        private static void TestSerialization()
        {
            var results = new[] { Result("SAFE/CONTINUE", "SAFE/CONTINUE") };
            string json = PatrolAIBenchmarkRunner.Serialize(
                PatrolAIBenchmarkRunner.BuildReport(results));
            Assert(json.Contains("Nebius Token Factory"), "Provider missing from JSON.");
            Assert(!json.Contains("NEBIUS_API_KEY") && !json.Contains("Authorization")
                && !json.Contains("C:\\"), "Sensitive material appeared in JSON.");
        }

        private static void TestRequestLimit()
        {
            Assert(PatrolAIBenchmarkRunner.MaximumLiveRequests == 24,
                "Live request ceiling changed.");
        }

        private static PatrolObservation Case(
            string condition, string evidence, float confidence, string context)
        {
            return new PatrolObservation
            {
                detectedCondition = condition,
                evidenceStatus = evidence,
                confidence = confidence,
                additionalContext = context
            };
        }

        private static PatrolAIEvaluationResult Result(string expected, string rule)
        {
            return new PatrolAIEvaluationResult
            {
                caseId = "test",
                category = "TEST",
                expectedResult = expected,
                ruleBasedResult = rule,
                nemotronResult = rule,
                ruleCorrect = expected == rule,
                nemotronCorrect = expected == rule,
                validStructuredOutput = true,
                runtimeStatus = "REAL"
            };
        }

        private static void AssertDisposition(PatrolObservation observation, string expected)
        {
            PatrolDecision decision = RuleBasedSafetyBaseline.Decide(observation);
            Assert(decision.riskLevel + "/" + decision.action == expected,
                "Unexpected rule disposition.");
        }

        private static void Run(string name, Action test, ICollection<string> passed)
        {
            try { test(); passed.Add(name); }
            catch (Exception exception)
            {
                throw new InvalidOperationException(name + ": " + exception.Message, exception);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

#if PATROLAI_PHASE7_OFFLINE_TESTS
        public static int Main()
        {
            try { Console.WriteLine(RunAll()); return 0; }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FAIL: " + exception.Message);
                return 1;
            }
        }
#endif
    }
}
