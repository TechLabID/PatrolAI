using System;
using PatrolAI.Competition;

namespace PatrolAI.UI
{
    [Serializable]
    public sealed class PatrolBaselineWorkflowDetailState
    {
        public bool HasSelection { get; private set; }
        public PatrolBaselineAction ActiveAction { get; private set; }

        public void Select(PatrolBaselineAction action)
        {
            ActiveAction = action;
            HasSelection = true;
        }

        public void Clear() { HasSelection = false; }

        public string Title
        {
            get
            {
                switch (ActiveAction)
                {
                    case PatrolBaselineAction.AgenticInspection:
                        return "AGENTIC INSPECTION";
                    case PatrolBaselineAction.MultiSourceFusion:
                        return "MULTI SOURCE FUSION";
                    case PatrolBaselineAction.LatestVerifiedFusion:
                        return "LATEST VERIFIED FUSION";
                    default:
                        return "BASELINE WORKFLOW";
                }
            }
        }

        public string[] FieldLabels
        {
            get
            {
                switch (ActiveAction)
                {
                    case PatrolBaselineAction.AgenticInspection:
                        return new[] { "STATE", "ACTIVE PHASE", "RESULT" };
                    case PatrolBaselineAction.MultiSourceFusion:
                        return new[]
                        {
                            "STATE", "ACTIVE PHASE", "FUSION STATUS", "VERIFICATION",
                            "ACTION", "FINAL CONDITION", "REASON", "RESULT"
                        };
                    case PatrolBaselineAction.LatestVerifiedFusion:
                        return new[]
                        {
                            "STATE", "ACTIVE PHASE", "TEMPORAL STATUS",
                            "PREVIOUS CONDITION", "LATEST CONDITION", "EVALUATION RUN",
                            "CHANGE ID", "INCIDENT ID", "REASON", "RESULT"
                        };
                    default:
                        return Array.Empty<string>();
                }
            }
        }
    }
}
