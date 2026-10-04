using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using PatrolAI.NVIDIA;

namespace PatrolAI.PhysicalAI
{
    /// <summary>
    /// Additive contract-only orchestration for one simulated Physical AI cycle.
    /// It does not reference scenes, OpenCV, movement, controllers, or UI.
    /// </summary>
    public sealed class PatrolPhysicalAIController
    {
        public const string Idle = "IDLE";
        public const string Observing = "OBSERVING";
        public const string Reasoning = "REASONING";
        public const string Deciding = "DECIDING";
        public const string Acting = "ACTING";
        public const string Continuing = "CONTINUING";
        public const string Reobserving = "REOBSERVING";
        public const string Inspecting = "INSPECTING";
        public const string Stopped = "STOPPED";
        public const string Alerting = "ALERTING";
        public const string Rerouting = "REROUTING";
        public const string Error = "ERROR";

        private readonly INemotronReasoningService reasoningService;

        public string CurrentState { get; private set; }

        public PatrolPhysicalAIController(INemotronReasoningService reasoningService)
        {
            this.reasoningService = reasoningService;
            CurrentState = Idle;
        }

        public async Task<PatrolClosedLoopResult> RunAsync(PatrolObservation observation)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var states = new List<string>();
            SetState(Idle, states);

            var result = new PatrolClosedLoopResult
            {
                observationId = observation == null ? string.Empty : observation.observationId,
                modelId = string.Empty,
                runtimeStatus = "FAILED",
                message = string.Empty
            };

            try
            {
                if (observation == null || reasoningService == null)
                {
                    throw new InvalidOperationException(
                        "Observation and reasoning service are required.");
                }

                SetState(Observing, states);
                SetState(Reasoning, states);

                PatrolDecision decision;
                var liveService = reasoningService as NebiusNemotronReasoningService;
                if (liveService != null)
                {
                    NemotronLiveReasoningResult liveResult = await liveService
                        .ReasonWithDiagnosticsAsync(observation)
                        .ConfigureAwait(false);
                    result.modelId = liveResult.modelId;
                    result.httpSuccess = liveResult.httpSuccess;
                    result.httpStatusCode = liveResult.statusCode;
                    result.parserSuccess = liveResult.parserSuccess;
                    result.validatorSuccess = liveResult.validatorSuccess;
                    result.usedSafeFallback = liveResult.validatorUsedSafeNormalization;
                    decision = liveResult.decision;

                    if (!liveResult.requestStarted
                        || !liveResult.httpSuccess
                        || !liveResult.parserSuccess
                        || !liveResult.validatorSuccess
                        || liveResult.validatorUsedSafeNormalization)
                    {
                        result.decision = decision;
                        result.action = PatrolActionMapper.Map(decision, out bool ignored);
                        result.usedSafeFallback = true;
                        result.runtimeStatus = liveResult.requestStarted ? "FAILED" : "FALLBACK";
                        result.message = "Live reasoning did not produce an unmodified validated decision.";
                        SetState(Error, states);
                        return result;
                    }

                    result.runtimeStatus = "REAL";
                }
                else
                {
                    decision = await reasoningService.ReasonAsync(observation)
                        .ConfigureAwait(false);
                    result.runtimeStatus = reasoningService is MockNemotronReasoningService
                        ? "MOCK"
                        : "LOCAL SAMPLE";
                }

                SetState(Deciding, states);
                result.decision = decision;
                result.action = PatrolActionMapper.Map(decision, out bool usedSafeFallback);
                result.usedSafeFallback = result.usedSafeFallback || usedSafeFallback;

                SetState(Acting, states);
                SetState(ActionToState(result.action.actionType), states);
                result.success = !usedSafeFallback;
                result.message = result.success
                    ? "Closed-loop decision mapped to a simulated Physical AI action."
                    : "Decision was unsupported; safe re-observation fallback selected.";
                return result;
            }
            catch (Exception exception)
            {
                result.runtimeStatus = "FAILED";
                result.success = false;
                result.message = "Closed-loop reasoning failed safely: "
                    + exception.GetType().Name;
                SetState(Error, states);
                return result;
            }
            finally
            {
                stopwatch.Stop();
                result.finalState = CurrentState;
                result.statePath = states.ToArray();
                result.elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        private void SetState(string state, ICollection<string> states)
        {
            CurrentState = state;
            states.Add(state);
        }

        private static string ActionToState(string actionType)
        {
            switch ((actionType ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "CONTINUE":
                    return Continuing;
                case "REOBSERVE":
                    return Reobserving;
                case "INSPECT":
                    return Inspecting;
                case "STOP":
                    return Stopped;
                case "ALERT":
                    return Alerting;
                case "REROUTE":
                    return Rerouting;
                default:
                    return Error;
            }
        }
    }
}
