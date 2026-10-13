using System;

namespace PatrolAI.Evaluation
{
    [Serializable]
    public sealed class PatrolAIEvaluationCase
    {
        private readonly string expectedRiskLevel;
        private readonly string expectedAction;

        public PatrolAIEvaluationCase(
            string caseId,
            string category,
            PatrolObservation observation,
            string expectedRiskLevel,
            string expectedAction,
            string policyRationale)
        {
            this.caseId = caseId;
            this.category = category;
            this.observation = observation;
            this.expectedRiskLevel = Normalize(expectedRiskLevel);
            this.expectedAction = Normalize(expectedAction);
            this.policyRationale = policyRationale;
        }

        public string caseId { get; }
        public string category { get; }
        public PatrolObservation observation { get; }
        public string ExpectedRiskLevel => expectedRiskLevel;
        public string ExpectedAction => expectedAction;
        public string policyRationale { get; }
        public string ExpectedDisposition => expectedRiskLevel + "/" + expectedAction;

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
