using System;

namespace PatrolAI.PhysicalAI
{
    /// <summary>
    /// Serializable command produced from a validated PatrolDecision.
    /// It contains no Unity movement implementation.
    /// </summary>
    [Serializable]
    public sealed class PatrolAction
    {
        public string actionType;
        public string reason;
        public string sourceDecisionId;
        public float confidence;
        public string timestamp;
    }
}
