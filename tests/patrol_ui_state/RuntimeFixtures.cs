// OFFLINE TEST FIXTURES ONLY: no HTTP transport or credentials. Never compiled into Unity.
using System;
using System.Threading.Tasks;
using PatrolAI.Nebius;
namespace PatrolAI.NVIDIA
{
    public sealed class NebiusNemotronReasoningService : INemotronReasoningService, IDisposable
    {
        public const string ModelId = "OFFLINE TEST FIXTURE";
        public static string Mode = "STOP";
        public static int Calls;
        public static Action PendingCompletion;
        public NebiusNemotronReasoningService(NebiusConfig config = null) { }
        public Task<PatrolDecision> ReasonAsync(PatrolObservation observation) => Task.FromResult(new PatrolDecision());
        public Task<NemotronLiveReasoningResult> ReasonWithDiagnosticsAsync(PatrolObservation observation)
        {
            Calls++;
            if (Mode == "HANG") return new TaskCompletionSource<NemotronLiveReasoningResult>().Task;
            string action = Mode == "DELAYED_STOP" ? "STOP" : Mode;
            if (Mode == "REOBSERVE_THEN_STOP") action = Calls == 1 ? "REOBSERVE" : "STOP";
            if (Mode == "SCENARIO") action = observation.evidenceStatus == "SUFFICIENT"
                ? observation.detectedCondition == "INDICATOR_RED" ? "STOP" : "CONTINUE"
                : observation.evidenceStatus == "CONFLICT" ? "STOP" : "REOBSERVE";
            bool ok = Mode != "FAILED" && Mode != "FALLBACK";
            var result = new NemotronLiveReasoningResult {
                modelId = ModelId, requestStarted = Mode != "FALLBACK", httpSuccess = ok,
                parserSuccess = ok, validatorSuccess = ok, statusCode = ok ? 200 : 503,
                decision = new PatrolDecision { riskLevel = action == "STOP" ? "DANGER"
                    : action == "CONTINUE" ? "SAFE" : "UNKNOWN", action = action,
                    confidence = .93f, reason = "OFFLINE FIXTURE: no model inference" }
            };
            if (Mode == "DELAYED_STOP") {
                var pending = new TaskCompletionSource<NemotronLiveReasoningResult>();
                PendingCompletion = () => pending.SetResult(result);
                return pending.Task;
            }
            return Task.FromResult(result);
        }
        public void Dispose() { }
    }
}
namespace PatrolAI.Scenarios
{
    public static class PatrolReobservationEvidenceReader
    {
        public static bool Fresh;
        public static bool Throw;
        public static string CurrentSessionId() => "offline-session";
        public static bool TryReadLatestSince(DateTime started, string session, PatrolObservation initial,
            out PatrolObservation observation, out string error)
        {
            if (Throw) throw new InvalidOperationException("offline evidence read exception");
            observation = new PatrolObservation { detectedCondition = "UNKNOWN", evidenceStatus = "INSUFFICIENT",
                confidence = .35f, additionalContext = "OFFLINE secondary evidence" };
            error = "missing evidence fixture";
            return Fresh;
        }
    }
}
