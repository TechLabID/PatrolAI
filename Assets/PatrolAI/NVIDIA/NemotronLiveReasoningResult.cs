using System;

namespace PatrolAI.NVIDIA
{
    /// <summary>
    /// Safe diagnostics for one explicitly requested live reasoning call.
    /// Does not contain credentials, headers, or the full prompt/response.
    /// </summary>
    [Serializable]
    public sealed class NemotronLiveReasoningResult
    {
        public string modelId;
        public bool requestStarted;
        public bool httpSuccess;
        public int statusCode;
        public bool parserSuccess;
        public bool validatorSuccess;
        public bool validatorUsedSafeNormalization;
        public long elapsedMilliseconds;
        public string message;
        public PatrolDecision decision;
    }
}
