using System;

namespace PatrolAI.PhysicalAI
{
    /// <summary>
    /// Truthful result of one reason-decide-act cycle.
    /// </summary>
    [Serializable]
    public sealed class PatrolClosedLoopResult
    {
        public string observationId;
        public string modelId;
        public string runtimeStatus;
        public PatrolDecision decision;
        public PatrolAction action;
        public string finalState;
        public string[] statePath;
        public bool success;
        public bool httpSuccess;
        public int httpStatusCode;
        public bool parserSuccess;
        public bool validatorSuccess;
        public bool usedSafeFallback;
        public string message;
        public long elapsedMilliseconds;
    }
}
