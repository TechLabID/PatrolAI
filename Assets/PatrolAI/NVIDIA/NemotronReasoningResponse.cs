using System;
using System.Runtime.Serialization;

namespace PatrolAI.NVIDIA
{
    /// <summary>
    /// Intermediate, untrusted representation of structured model output.
    /// It must pass NemotronDecisionValidator before becoming a PatrolDecision.
    /// </summary>
    [Serializable]
    [DataContract]
    public sealed class NemotronReasoningResponse
    {
        [DataMember(Name = "riskLevel", IsRequired = true)]
        public string riskLevel;

        [DataMember(Name = "action", IsRequired = true)]
        public string action;

        [DataMember(Name = "reason", IsRequired = true)]
        public string reason;

        [DataMember(Name = "requiresReobservation", IsRequired = true)]
        public bool requiresReobservation;

        [DataMember(Name = "confidence", IsRequired = true)]
        public float confidence;
    }
}
