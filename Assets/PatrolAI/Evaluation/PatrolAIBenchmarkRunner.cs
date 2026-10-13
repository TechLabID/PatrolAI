using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using PatrolAI.NVIDIA;

namespace PatrolAI.Evaluation
{
    /// <summary>
    /// Executes exactly one live request per frozen case. There is deliberately no
    /// model-call retry and no substitution of fallback output into REAL metrics.
    /// </summary>
    public static class PatrolAIBenchmarkRunner
    {
        public const int MaximumLiveRequests = 24;

        public static async Task<PatrolAIBenchmarkReport> RunLiveAsync(string outputDirectory)
        {
            IReadOnlyList<PatrolAIEvaluationCase> cases = PatrolAIEvaluationDataset.Create();
            if (cases.Count > MaximumLiveRequests)
                throw new InvalidOperationException("Benchmark exceeds the 24-request limit.");

            string frozenLabels = LabelFingerprint(cases);
            Directory.CreateDirectory(outputDirectory);
            string path = Path.Combine(outputDirectory, "phase7_ai_necessity_results.json");
            if (File.Exists(path))
                throw new InvalidOperationException("Existing benchmark evidence must not be overwritten or rerun.");
            File.WriteAllText(Path.Combine(outputDirectory, "expected_labels.txt"),
                frozenLabels, new UTF8Encoding(false));
            var results = new List<PatrolAIEvaluationResult>(cases.Count);
            using (var reasoning = new NebiusNemotronReasoningService())
            {
                foreach (PatrolAIEvaluationCase evaluationCase in cases)
                {
                    PatrolDecision rule = RuleBasedSafetyBaseline.Decide(evaluationCase.observation);
                    NemotronLiveReasoningResult live = await reasoning
                        .ReasonWithDiagnosticsAsync(evaluationCase.observation)
                        .ConfigureAwait(false);

                    bool validReal = live.requestStarted && live.httpSuccess
                        && live.parserSuccess && live.validatorSuccess
                        && !live.validatorUsedSafeNormalization;
                    string ruleDisposition = Disposition(rule);
                    string modelDisposition = validReal
                        ? Disposition(live.decision)
                        : "NOT_VERIFIED";
                    bool fallback = !validReal && live.decision != null;
                    bool ruleCorrect = ruleDisposition == evaluationCase.ExpectedDisposition;
                    bool modelCorrect = validReal
                        && modelDisposition == evaluationCase.ExpectedDisposition;

                    results.Add(new PatrolAIEvaluationResult
                    {
                        caseId = evaluationCase.caseId,
                        category = evaluationCase.category,
                        expectedResult = evaluationCase.ExpectedDisposition,
                        ruleBasedResult = ruleDisposition,
                        nemotronResult = modelDisposition,
                        validStructuredOutput = live.parserSuccess && live.validatorSuccess,
                        invalidStructuredOutput = live.httpSuccess
                            && (!live.parserSuccess || !live.validatorSuccess),
                        httpStatusCode = live.statusCode,
                        requestStarted = live.requestStarted,
                        nemotronReason = validReal ? live.decision.reason : live.message,
                        runtimeStatus = validReal ? "REAL" : "FAILED",
                        latencyMilliseconds = Math.Max(0L, live.elapsedMilliseconds),
                        fallbackUsed = fallback || live.validatorUsedSafeNormalization,
                        ruleCorrect = ruleCorrect,
                        nemotronCorrect = modelCorrect,
                        errorCategory = ErrorCategory(live, validReal),
                        disagreementClassification = ClassifyDisagreement(
                            ruleCorrect, modelCorrect, ruleDisposition, modelDisposition)
                    });
                    File.WriteAllText(path, Serialize(BuildReport(results)), new UTF8Encoding(false));
                }
            }

            if (LabelFingerprint(cases) != frozenLabels)
                throw new InvalidOperationException("Expected labels changed during evaluation.");

            PatrolAIBenchmarkReport report = BuildReport(results);
            File.WriteAllText(path, Serialize(report), new UTF8Encoding(false));
            report.outputPath = path;
            return report;
        }

        public static PatrolAIBenchmarkReport BuildReport(
            IReadOnlyList<PatrolAIEvaluationResult> results)
        {
            var report = new PatrolAIBenchmarkReport
            {
                schemaVersion = 1,
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                provider = "Nebius Token Factory",
                model = NebiusNemotronReasoningService.ModelId,
                enableThinking = false,
                requestCount = results.Count,
                cases = results.ToArray(),
                ruleMetrics = PatrolAIBenchmarkMetrics.Calculate("RULE_BASELINE", results, false),
                nemotronMetrics = PatrolAIBenchmarkMetrics.Calculate("NEMOTRON", results, true)
            };

            report.categoryMetrics = results.GroupBy(r => r.category)
                .OrderBy(group => group.Key)
                .Select(group => new PatrolAICategoryMetrics
                {
                    category = group.Key,
                    caseCount = group.Count(),
                    ruleMetrics = PatrolAIBenchmarkMetrics.Calculate(
                        "RULE_BASELINE", group, false),
                    nemotronMetrics = PatrolAIBenchmarkMetrics.Calculate(
                        "NEMOTRON", group, true)
                }).ToArray();
            return report;
        }

        public static string Serialize(PatrolAIBenchmarkReport report)
        {
            var serializer = new DataContractJsonSerializer(
                typeof(PatrolAIBenchmarkReport),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, report);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static string LabelFingerprint(IEnumerable<PatrolAIEvaluationCase> cases)
        {
            return string.Join("|", cases.Select(c => c.caseId + "=" + c.ExpectedDisposition));
        }

        private static string Disposition(PatrolDecision decision)
        {
            return decision == null
                ? "NOT_VERIFIED"
                : (decision.riskLevel ?? string.Empty).Trim().ToUpperInvariant()
                    + "/" + (decision.action ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string ErrorCategory(NemotronLiveReasoningResult result, bool validReal)
        {
            if (validReal) return "NONE";
            if (!result.requestStarted) return "REQUEST_NOT_STARTED";
            if (!result.httpSuccess) return "TRANSPORT_OR_HTTP_FAILURE";
            if (!result.parserSuccess) return "INVALID_STRUCTURED_OUTPUT";
            if (!result.validatorSuccess) return "VALIDATION_FAILURE";
            if (result.validatorUsedSafeNormalization) return "SAFE_NORMALIZATION_FALLBACK";
            return "UNKNOWN_FAILURE";
        }

        private static string ClassifyDisagreement(
            bool ruleCorrect, bool modelCorrect, string rule, string model)
        {
            if (rule == model)
                return ruleCorrect ? "BOTH_CORRECT" : "BOTH_WRONG";
            if (ruleCorrect && !modelCorrect) return "RULE_CORRECT_NEMOTRON_WRONG";
            if (!ruleCorrect && modelCorrect) return "NEMOTRON_CORRECT_RULE_WRONG";
            if (!ruleCorrect && !modelCorrect) return "BOTH_WRONG";
            return "BOTH_CORRECT_DIFFERENT_ACTION";
        }

#if PATROLAI_PHASE7_LIVE_BENCHMARK
        public static int Main(string[] args)
        {
            try
            {
                string output = args.Length == 0
                    ? Path.Combine("evaluation", "results")
                    : args[0];
                PatrolAIBenchmarkReport report = RunLiveAsync(output)
                    .GetAwaiter().GetResult();
                Console.WriteLine(Serialize(report));
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("PHASE7_BENCHMARK_FAILED: "
                    + exception.GetType().Name + ": " + exception.Message);
                return 1;
            }
        }
#endif
    }

    [Serializable]
    [DataContract]
    public sealed class PatrolAIBenchmarkReport
    {
        [DataMember(Order = 1)] public int schemaVersion;
        [DataMember(Order = 2)] public string generatedAtUtc;
        [DataMember(Order = 3)] public string provider;
        [DataMember(Order = 4)] public string model;
        [DataMember(Order = 5)] public bool enableThinking;
        [DataMember(Order = 6)] public int requestCount;
        [DataMember(Order = 7)] public PatrolAIEvaluationResult[] cases;
        [DataMember(Order = 8)] public PatrolAIBenchmarkMetrics ruleMetrics;
        [DataMember(Order = 9)] public PatrolAIBenchmarkMetrics nemotronMetrics;
        [DataMember(Order = 10)] public PatrolAICategoryMetrics[] categoryMetrics;
        [IgnoreDataMember] public string outputPath;
    }

    [Serializable]
    public sealed class PatrolAICategoryMetrics
    {
        public string category;
        public int caseCount;
        public PatrolAIBenchmarkMetrics ruleMetrics;
        public PatrolAIBenchmarkMetrics nemotronMetrics;
    }
}
