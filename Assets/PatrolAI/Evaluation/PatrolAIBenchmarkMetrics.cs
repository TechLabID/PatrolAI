using System;
using System.Collections.Generic;
using System.Linq;

namespace PatrolAI.Evaluation
{
    [Serializable]
    public sealed class PatrolAIBenchmarkMetrics
    {
        public string system;
        public int total;
        public int correct;
        public double decisionAccuracy;
        public int falseStops;
        public double falseStopRate;
        public int missedHazards;
        public double missedHazardRate;
        public int inappropriateContinues;
        public double inappropriateContinueRate;
        public int expectedReobserves;
        public int correctReobserves;
        public double reobserveCorrectness;
        public int invalidStructuredOutputs;
        public double invalidStructuredOutputRate;
        public int fallbacks;
        public double fallbackRate;
        public int failedCalls;
        public double failedCallRate;
        public double averageLatencyMilliseconds;
        public double medianLatencyMilliseconds;

        public static PatrolAIBenchmarkMetrics Calculate(
            string system,
            IEnumerable<PatrolAIEvaluationResult> values,
            bool nemotron)
        {
            List<PatrolAIEvaluationResult> results = values.ToList();
            var metric = new PatrolAIBenchmarkMetrics { system = system, total = results.Count };
            Func<PatrolAIEvaluationResult, string> predicted = r =>
                nemotron ? r.nemotronResult : r.ruleBasedResult;
            Func<PatrolAIEvaluationResult, bool> correct = r =>
                nemotron ? r.nemotronCorrect : r.ruleCorrect;

            metric.correct = results.Count(correct);
            metric.decisionAccuracy = Rate(metric.correct, metric.total);
            metric.falseStops = results.Count(r => Action(predicted(r)) == "STOP"
                && Action(r.expectedResult) != "STOP");
            metric.falseStopRate = Rate(metric.falseStops,
                results.Count(r => Action(r.expectedResult) != "STOP"));
            metric.missedHazards = results.Count(r => Action(r.expectedResult) == "STOP"
                && Action(predicted(r)) != "STOP");
            metric.missedHazardRate = Rate(metric.missedHazards,
                results.Count(r => Action(r.expectedResult) == "STOP"));
            metric.inappropriateContinues = results.Count(r => Action(predicted(r)) == "CONTINUE"
                && Action(r.expectedResult) != "CONTINUE");
            metric.inappropriateContinueRate = Rate(metric.inappropriateContinues,
                results.Count(r => Action(r.expectedResult) != "CONTINUE"));
            metric.expectedReobserves = results.Count(r => Action(r.expectedResult) == "REOBSERVE");
            metric.correctReobserves = results.Count(r => Action(r.expectedResult) == "REOBSERVE"
                && Action(predicted(r)) == "REOBSERVE");
            metric.reobserveCorrectness = Rate(metric.correctReobserves, metric.expectedReobserves);

            if (nemotron)
            {
                metric.invalidStructuredOutputs = results.Count(r => r.invalidStructuredOutput);
                metric.invalidStructuredOutputRate = Rate(metric.invalidStructuredOutputs, metric.total);
                metric.fallbacks = results.Count(r => r.fallbackUsed);
                metric.fallbackRate = Rate(metric.fallbacks, metric.total);
                metric.failedCalls = results.Count(r => r.runtimeStatus == "FAILED");
                metric.failedCallRate = Rate(metric.failedCalls, metric.total);
                List<long> latencies = results.Select(r => r.latencyMilliseconds)
                    .OrderBy(v => v).ToList();
                metric.averageLatencyMilliseconds = latencies.Count == 0
                    ? 0d : latencies.Average(v => (double)v);
                metric.medianLatencyMilliseconds = Median(latencies);
            }
            return metric;
        }

        private static string Action(string disposition)
        {
            int separator = (disposition ?? string.Empty).IndexOf('/');
            return separator < 0 ? string.Empty : disposition.Substring(separator + 1);
        }

        private static double Rate(int numerator, int denominator)
        {
            return denominator == 0 ? 0d : (double)numerator / denominator;
        }

        private static double Median(IReadOnlyList<long> sorted)
        {
            if (sorted.Count == 0) return 0d;
            int middle = sorted.Count / 2;
            return sorted.Count % 2 == 1
                ? sorted[middle]
                : (sorted[middle - 1] + sorted[middle]) / 2d;
        }
    }
}
