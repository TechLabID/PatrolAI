using System;

namespace PatrolAI.Evaluation
{
    [Serializable]
    public sealed class PatrolAIEvaluationResult
    {
        public string caseId;
        public string category;
        public string expectedResult;
        public string ruleBasedResult;
        public string nemotronResult;
        public bool validStructuredOutput;
        public bool invalidStructuredOutput;
        public int httpStatusCode;
        public bool requestStarted;
        public string nemotronReason;
        public string runtimeStatus;
        public long latencyMilliseconds;
        public bool fallbackUsed;
        public bool ruleCorrect;
        public bool nemotronCorrect;
        public string errorCategory;
        public string disagreementClassification;
    }
}
