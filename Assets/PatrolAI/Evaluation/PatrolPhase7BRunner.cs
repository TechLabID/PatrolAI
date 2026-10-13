using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using PatrolAI.NVIDIA;

namespace PatrolAI.Evaluation
{
    public static class PatrolPhase7BRunner
    {
        public const int MaximumConsistencyCalls = 36;
        public static T Read<T>(string path)
        {
            using (var stream = File.OpenRead(path))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }

        public static string Serialize<T>(T value)
        {
            using (var stream = new MemoryStream()) {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static PatrolComparativeRun[] Recompute(string originalPath)
        {
            var report = Read<PatrolAIBenchmarkReport>(originalPath);
            var cases = PatrolAIEvaluationDataset.Create().ToDictionary(c => c.caseId);
            if (report.cases.Length != 24 || report.cases.Select(r => r.caseId).Distinct().Count() != 24)
                throw new InvalidOperationException("Original evidence must contain 24 unique cases.");
            return report.cases.Select(r => {
                var c = cases[r.caseId];
                string computedRule = PatrolComparativeEvaluation.Disposition(RuleBasedSafetyBaseline.Decide(c.observation));
                if (c.ExpectedDisposition != r.expectedResult || computedRule != r.ruleBasedResult)
                    throw new InvalidOperationException("Dataset or rule evidence drift detected.");
                var row = new PatrolComparativeRun {
                    timestamp = report.generatedAtUtc, caseId = r.caseId, category = r.category,
                    runIndex = 0, modelId = report.model, expectedDecision = r.expectedResult,
                    ruleDecision = r.ruleBasedResult, modelDecision = PatrolComparativeEvaluation.Decision(r.nemotronResult, r.nemotronReason),
                    runtimeStatus = r.runtimeStatus, validOutput = r.validStructuredOutput,
                    decisionReconstructedFromStoredPair = true,
                    fallbackUsed = r.fallbackUsed, httpStatusCode = r.httpStatusCode,
                    httpSuccess = r.httpStatusCode >= 200 && r.httpStatusCode < 300,
                    latencyMilliseconds = r.latencyMilliseconds
                };
                PatrolComparativeEvaluation.Score(row);
                return row;
            }).ToArray();
        }

        public static async Task RunConsistencyAsync(string outputDirectory)
        {
            var cases = PatrolAIEvaluationDataset.Create().Where(c =>
                c.category == "AMBIGUOUS" || c.category == "CONFLICTING" || c.category == "DEGRADED").ToArray();
            if (cases.Length != 12 || cases.Length * 3 != MaximumConsistencyCalls)
                throw new InvalidOperationException("Invalid difficult subset.");
            Directory.CreateDirectory(outputDirectory);
            string marker = Path.Combine(outputDirectory, "consistency_started.json");
            using (var stream = new FileStream(marker, FileMode.CreateNew, FileAccess.Write)) {
                byte[] manifest = Encoding.UTF8.GetBytes(Serialize(new[] {
                    "plannedRequests=36", "enable_thinking=false", "retries=0",
                    DateTime.UtcNow.ToString("O"),
                    PatrolAIBenchmarkRunner.LabelFingerprint(cases)
                }));
                stream.Write(manifest, 0, manifest.Length);
            }
            var results = new List<PatrolComparativeRun>();
            using (var service = new NebiusNemotronReasoningService()) {
                // Round-major order exposes inter-run variability without prompt changes.
                for (int run = 1; run <= 3; run++) foreach (var c in cases) {
                    var live = await service.ReasonWithDiagnosticsAsync(c.observation).ConfigureAwait(false);
                    bool valid = live.requestStarted && live.httpSuccess && live.parserSuccess
                        && live.validatorSuccess && !live.validatorUsedSafeNormalization;
                    var row = new PatrolComparativeRun {
                        timestamp = DateTime.UtcNow.ToString("O"), caseId = c.caseId, category = c.category,
                        runIndex = run, modelId = live.modelId, expectedDecision = c.ExpectedDisposition,
                        ruleDecision = PatrolComparativeEvaluation.Disposition(RuleBasedSafetyBaseline.Decide(c.observation)),
                        modelDecision = live.decision, runtimeStatus = valid ? "REAL" : live.httpSuccess ? "FALLBACK" : "FAILED",
                        validOutput = live.parserSuccess && live.validatorSuccess && !live.validatorUsedSafeNormalization,
                        fallbackUsed = !valid, httpSuccess = live.httpSuccess, httpStatusCode = live.statusCode,
                        latencyMilliseconds = live.elapsedMilliseconds
                    };
                    PatrolComparativeEvaluation.Score(row);
                    results.Add(row);
                    File.WriteAllText(Path.Combine(outputDirectory, c.caseId + "_run" + run + ".json"), Serialize(row));
                    File.WriteAllText(Path.Combine(outputDirectory, "consistency_results.json"), Serialize(results.ToArray()));
                }
            }
            File.WriteAllText(Path.Combine(outputDirectory, "consistency_summary.json"),
                Serialize(PatrolComparativeEvaluation.Consistency(results)));
            Console.WriteLine("CONSISTENCY_COMPLETED: " + results.Count + " calls; REAL="
                + results.Count(r => r.runtimeStatus == "REAL"));
            WriteAnalysis(outputDirectory);
        }

        public static void WriteAnalysis(string directory)
        {
            var original = Read<PatrolComparativeRun[]>(Path.Combine(directory, "original_recomputed.json"));
            var repeated = Read<PatrolComparativeRun[]>(Path.Combine(directory, "consistency_results.json"));
            foreach (var r in original.Concat(repeated)) PatrolComparativeEvaluation.Score(r);
            var consistency = PatrolComparativeEvaluation.Consistency(repeated);
            var sorted = repeated.Select(r => r.latencyMilliseconds).OrderBy(v => v).ToArray();
            var report = new PatrolPhase7BReport {
                originalRule = PatrolComparativeEvaluation.Metrics(original, false),
                originalNemotron = PatrolComparativeEvaluation.Metrics(original, true),
                repeatedRule = PatrolComparativeEvaluation.Metrics(repeated, false),
                repeatedNemotron = PatrolComparativeEvaluation.Metrics(repeated, true),
                consistency = consistency,
                decisionConsistencyFraction = (double)consistency.Count(c => c.runCount == 3 && c.sameDecisionCount == 3) / consistency.Length,
                actionConsistencyFraction = (double)consistency.Count(c => c.runCount == 3 && c.sameActionCount == 3) / consistency.Length,
                meanLatencyMilliseconds = sorted.Average(v => (double)v),
                medianLatencyMilliseconds = (sorted[(sorted.Length - 1) / 2] + sorted[sorted.Length / 2]) / 2d,
                mismatchAudit = original.Where(r => !r.nemotronExactCorrect).ToArray(),
                categoryComparison = original.GroupBy(r => r.category).OrderBy(g => g.Key).Select(g =>
                    new PatrolComparativeCategory { category = g.Key,
                        rule = PatrolComparativeEvaluation.Metrics(g, false),
                        nemotron = PatrolComparativeEvaluation.Metrics(g, true) }).ToArray()
            };
            File.WriteAllText(Path.Combine(directory, "comparison_summary.json"), Serialize(report));
            File.WriteAllText(Path.Combine(directory, "mismatch_audit.json"), Serialize(report.mismatchAudit));
        }

#if PATROLAI_PHASE7B_LIVE
        public static int Main()
        {
            RunConsistencyAsync(Path.Combine("evaluation", "results", "phase7b"))
                .GetAwaiter().GetResult();
            return 0;
        }
#endif
    }

    [Serializable]
    public sealed class PatrolPhase7BReport
    {
        public PatrolSafetyMetrics originalRule, originalNemotron, repeatedRule, repeatedNemotron;
        public PatrolConsistencyCase[] consistency;
        public PatrolComparativeRun[] mismatchAudit;
        public PatrolComparativeCategory[] categoryComparison;
        public double decisionConsistencyFraction, actionConsistencyFraction, meanLatencyMilliseconds, medianLatencyMilliseconds;
    }
    [Serializable]
    public sealed class PatrolComparativeCategory
    {
        public string category;
        public PatrolSafetyMetrics rule, nemotron;
    }
}
