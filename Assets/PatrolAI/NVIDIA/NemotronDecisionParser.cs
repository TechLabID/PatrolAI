using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PatrolAI.NVIDIA
{
    /// <summary>
    /// Safely parses structured Nemotron output without trusting its values.
    /// </summary>
    public static class NemotronDecisionParser
    {
        public static bool TryParse(
            string modelOutput,
            out NemotronReasoningResponse response,
            out string errorMessage)
        {
            response = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(modelOutput))
            {
                errorMessage = "Nemotron output is empty.";
                return false;
            }

            if (!TryExtractJsonObject(modelOutput, out string json, out errorMessage))
            {
                return false;
            }

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(NemotronReasoningResponse));
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    response = serializer.ReadObject(stream) as NemotronReasoningResponse;
                }

                if (response == null)
                {
                    errorMessage = "Nemotron output did not produce a response object.";
                    return false;
                }

                bool supportedRisk = TryNormalizeRiskLevel(response.riskLevel, out string riskLevel);
                bool supportedAction = TryNormalizeAction(response.action, out string action);

                response.riskLevel = supportedRisk ? riskLevel : "UNKNOWN";
                response.action = supportedAction ? action : "REOBSERVE";
                response.reason = response.reason == null ? string.Empty : response.reason.Trim();

                if (!supportedRisk || !supportedAction)
                {
                    response.riskLevel = "UNKNOWN";
                    response.action = "REOBSERVE";
                    response.requiresReobservation = true;
                }

                return true;
            }
            catch (Exception exception)
            {
                response = null;
                errorMessage = "Nemotron JSON parse failed: " + exception.Message;
                return false;
            }
        }

        private static bool TryExtractJsonObject(
            string modelOutput,
            out string json,
            out string errorMessage)
        {
            json = string.Empty;
            errorMessage = string.Empty;

            string trimmed = modelOutput.Trim();
            int firstBrace = trimmed.IndexOf('{');
            int lastBrace = trimmed.LastIndexOf('}');

            if (firstBrace < 0 || lastBrace <= firstBrace)
            {
                errorMessage = "Nemotron output does not contain a complete JSON object.";
                return false;
            }

            json = trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
            return true;
        }

        private static bool TryNormalizeRiskLevel(string value, out string normalized)
        {
            normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
            switch (normalized)
            {
                case "SAFE":
                case "WARNING":
                case "DANGER":
                case "UNKNOWN":
                    return true;
                default:
                    normalized = "UNKNOWN";
                    return false;
            }
        }

        private static bool TryNormalizeAction(string value, out string normalized)
        {
            normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
            switch (normalized)
            {
                case "CONTINUE":
                case "REOBSERVE":
                case "INSPECT":
                case "STOP":
                case "ALERT":
                case "REROUTE":
                    return true;
                default:
                    normalized = "REOBSERVE";
                    return false;
            }
        }
    }
}
