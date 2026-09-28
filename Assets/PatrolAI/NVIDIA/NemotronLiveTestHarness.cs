using System;
using System.IO;
using System.Threading.Tasks;
using PatrolAI.Nebius;

namespace PatrolAI.NVIDIA
{
    /// <summary>
    /// Explicit manual live test only. It never runs automatically.
    /// Compile with PATROLAI_LIVE_TESTS to expose the command-line entry point.
    /// </summary>
    public static class NemotronLiveTestHarness
    {
        public static async Task<int> RunOnceAsync(TextWriter output)
        {
            output.WriteLine("Model ID: " + NebiusNemotronReasoningService.ModelId);

            var config = new NebiusConfig();
            if (!config.TryGetApiKey(out string apiKey, out string configurationError))
            {
                output.WriteLine("Request started: NO");
                output.WriteLine("HTTP result: NOT RUN");
                output.WriteLine("Validation result: NOT RUN");
                output.WriteLine("Configuration: " + configurationError);
                return 2;
            }

            apiKey = string.Empty;
            var observation = new PatrolObservation
            {
                observationId = "phase3-live-indicator-red",
                timestamp = DateTime.UtcNow.ToString("o"),
                source = "PatrolAI Phase 3 explicit live test",
                detectedCondition = "INDICATOR_RED",
                evidenceStatus = "SUFFICIENT",
                confidence = 0.95f,
                location = "Industrial inspection point A",
                additionalContext = "Red machine status indicator observed during patrol."
            };

            using (var service = new NebiusNemotronReasoningService(config))
            {
                NemotronLiveReasoningResult result = await service
                    .ReasonWithDiagnosticsAsync(observation)
                    .ConfigureAwait(false);

                output.WriteLine("Request started: " + YesNo(result.requestStarted));
                output.WriteLine("HTTP result: "
                    + (result.httpSuccess ? "PASS" : "FAIL")
                    + " (status " + result.statusCode + ")");
                output.WriteLine("Parser result: "
                    + (result.parserSuccess ? "PASS" : "FAIL"));
                output.WriteLine("Parsed riskLevel: "
                    + (result.decision == null ? "UNAVAILABLE" : result.decision.riskLevel));
                output.WriteLine("Parsed action: "
                    + (result.decision == null ? "UNAVAILABLE" : result.decision.action));
                output.WriteLine("Validation result: "
                    + (result.validatorSuccess
                        ? (result.validatorUsedSafeNormalization
                            ? "SAFE-NORMALIZED PASS"
                            : "PASS")
                        : "FAIL"));
                output.WriteLine("Elapsed milliseconds: " + result.elapsedMilliseconds);

                return result.httpSuccess
                    && result.parserSuccess
                    && result.validatorSuccess
                    ? 0
                    : 1;
            }
        }

        private static string YesNo(bool value)
        {
            return value ? "YES" : "NO";
        }

#if PATROLAI_LIVE_TESTS
        public static int Main()
        {
            try
            {
                return RunOnceAsync(Console.Out).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Live test failed safely: " + exception.GetType().Name);
                return 1;
            }
        }
#endif
    }
}
