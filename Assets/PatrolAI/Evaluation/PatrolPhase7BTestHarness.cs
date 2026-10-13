using System;
using System.IO;
using System.Linq;
using PatrolAI.NVIDIA;

namespace PatrolAI.Evaluation
{
    public static class PatrolPhase7BTestHarness
    {
        static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
        static PatrolComparativeRun Row(string expected, string rule, string model, string status = "REAL")
        {
            var r = new PatrolComparativeRun { caseId = "test", expectedDecision = expected,
                ruleDecision = rule, modelDecision = PatrolComparativeEvaluation.Decision(model),
                runtimeStatus = status, validOutput = status == "REAL", httpSuccess = true };
            PatrolComparativeEvaluation.Score(r);
            return r;
        }
        public static void RunAll()
        {
            foreach (string pair in new[] { "SAFE/CONTINUE", "DANGER/STOP", "UNKNOWN/REOBSERVE", "WARNING/REOBSERVE" }) {
                string action = PatrolComparativeEvaluation.MappedAction(PatrolComparativeEvaluation.Decision(pair), out bool fallback);
                Check(!fallback && action == pair.Split('/')[1], "Shared mapping failed");
            }
            PatrolComparativeEvaluation.MappedAction(PatrolComparativeEvaluation.Decision("SAFE/STOP"), out bool unsupported);
            Check(unsupported, "Unsupported action was accepted");
            var mismatch = Row("UNKNOWN/REOBSERVE", "UNKNOWN/REOBSERVE", "WARNING/REOBSERVE");
            Check(!mismatch.nemotronExactCorrect && mismatch.nemotronActionCorrect, "Mismatch audit wrong");
            var unnecessary = Row("SAFE/CONTINUE", "DANGER/STOP", "WARNING/REOBSERVE");
            var missed = Row("DANGER/STOP", "UNKNOWN/REOBSERVE", "SAFE/CONTINUE");
            var metrics = PatrolComparativeEvaluation.Metrics(new[] { mismatch, unnecessary, missed }, true);
            Check(metrics.unnecessaryReobserves == 1 && metrics.nonReobserveCases == 2
                && metrics.missedStops == 1 && metrics.hazardCases == 1
                && metrics.inappropriateContinues == 1 && metrics.nonContinueCases == 2, "Metric denominators wrong");
            var failed = Row("UNKNOWN/REOBSERVE", "UNKNOWN/REOBSERVE", "UNKNOWN/REOBSERVE", "FAILED");
            Check(!failed.nemotronActionCorrect && failed.nemotronAction == "NOT_VERIFIED", "Failure earned credit");
            var same = PatrolComparativeEvaluation.Consistency(new[] { mismatch, mismatch, mismatch })[0];
            Check(same.sameDecisionCount == 3 && same.sameActionCount == 3, "3/3 consistency wrong");
            var different = Row("UNKNOWN/REOBSERVE", "UNKNOWN/REOBSERVE", "UNKNOWN/REOBSERVE");
            var varied = PatrolComparativeEvaluation.Consistency(new[] { mismatch, mismatch, different })[0];
            Check(varied.sameDecisionCount == 2 && varied.sameActionCount == 3, "Decision/action variability hidden");
            var partial = PatrolComparativeEvaluation.Consistency(new[] { mismatch, mismatch, failed })[0];
            Check(partial.sameActionCount == 2 && partial.runCount == 3, "Failed run excluded");
            Check(PatrolComparativeEvaluation.Consistency(new[] { failed, failed, failed })[0].sameActionCount == 0,
                "Failures counted as consistency");
            string json = PatrolPhase7BRunner.Serialize(new[] { mismatch, failed });
            string validationDirectory = Path.Combine("evaluation", "results", "validation", "phase7b");
            Directory.CreateDirectory(validationDirectory);
            string path = Path.Combine(validationDirectory, "serialization.json");
            File.WriteAllText(path, json);
            Check(PatrolPhase7BRunner.Read<PatrolComparativeRun[]>(path).Length == 2, "Serialization roundtrip failed");
            Check(!json.Contains("Authorization") && !json.Contains("NEBIUS_API_KEY") && !json.Contains("C:\\"), "Unsafe serialization");
            var original = PatrolPhase7BRunner.Recompute(Path.Combine("evaluation", "results", "phase7_ai_necessity_results.json"));
            Check(original.Count(r => !r.nemotronExactCorrect && r.nemotronActionCorrect) == 7, "Original mismatch count changed");
            Check(original.Count(r => r.nemotronAction == "REOBSERVE" && r.expectedAction != "REOBSERVE") == 1, "Original unnecessary reobserve count changed");
            Check(PatrolComparativeEvaluation.Metrics(original, false).actionCorrect == 18
                && PatrolComparativeEvaluation.Metrics(original, true).actionCorrect == 23, "Original action scores changed");
            string output = validationDirectory;
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "original_recomputed.json"), PatrolPhase7BRunner.Serialize(original));
            Console.WriteLine("Phase7B shared mapping/metrics/audit/consistency/serialization: PASS");
            Console.WriteLine(NemotronOfflineTestHarness.RunAll());
        }
#if PATROLAI_PHASE7B_TESTS
        public static int Main() { RunAll(); return 0; }
#endif
    }
}
