using System;
using PatrolAI.PhysicalAI;

namespace PatrolAI.Scenarios
{
    [Serializable]
    public sealed class PatrolDemoScenarioResult
    {
        public PatrolDemoScenario scenario;
        public PatrolClosedLoopResult closedLoopResult;
        public bool matchesExpectedDirection;
        public string message;
    }
}
