using System;

namespace PatrolAI.NVIDIA
{
    /// <summary>
    /// Compact structured input reserved for a future Nemotron request.
    /// No provider model ID is selected in Phase 2.5.
    /// </summary>
    [Serializable]
    public sealed class NemotronReasoningRequest
    {
        public const string DefaultSystemInstruction =
            "Assess industrial safety risk from the structured observation. "
            + "Return only JSON with riskLevel, action, reason, "
            + "requiresReobservation, and confidence. "
            + "Allowed risk levels: SAFE, WARNING, DANGER, UNKNOWN. "
            + "Allowed actions: CONTINUE, REOBSERVE, INSPECT, STOP, ALERT, REROUTE.";

        public string systemInstruction = DefaultSystemInstruction;
        public string observationId;
        public string source;
        public string detectedCondition;
        public string evidenceStatus;
        public float confidence;
        public string location;
        public string additionalContext;

        public static bool TryCreate(
            PatrolObservation observation,
            out NemotronReasoningRequest request,
            out string errorMessage)
        {
            request = null;
            errorMessage = string.Empty;

            if (observation == null)
            {
                errorMessage = "PatrolObservation is required.";
                return false;
            }

            request = new NemotronReasoningRequest
            {
                observationId = observation.observationId,
                source = observation.source,
                detectedCondition = observation.detectedCondition,
                evidenceStatus = observation.evidenceStatus,
                confidence = observation.confidence,
                location = observation.location,
                additionalContext = observation.additionalContext
            };
            return true;
        }
    }
}
