using System;
using PatrolAI.PhysicalAI;

namespace PatrolAI.Scenarios
{
    [Serializable]
    public sealed class PatrolDemoScenarioResult
    {
        public PatrolDemoScenario scenario;
        public PatrolClosedLoopResult closedLoopResult;
        public PatrolObservation reobservation;
        public PatrolClosedLoopResult secondClosedLoopResult;
        public bool decisionChanged;
        public bool matchesExpectedDirection;
        public string message;
        public PatrolDecision ruleInitialDecision;
        public PatrolDecision ruleFinalDecision;
        public PatrolDecision ruleV2InitialDecision;
        public PatrolDecision ruleV2FinalDecision;
        public PatrolDecision finalBoundedDecision;
        public bool safetyEnvelopeEscalated;
    }
}
