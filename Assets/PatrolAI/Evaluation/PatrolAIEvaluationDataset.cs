using System;
using System.Collections.Generic;

namespace PatrolAI.Evaluation
{
    /// <summary>
    /// Frozen-before-inference synthetic benchmark. Labels follow the documented
    /// safety policy and are never derived from either evaluated system.
    /// </summary>
    public static class PatrolAIEvaluationDataset
    {
        public static IReadOnlyList<PatrolAIEvaluationCase> Create()
        {
            var cases = new List<PatrolAIEvaluationCase>();

            Add(cases, "A01", "CLEAR_SAFE", "INDICATOR_GREEN", "SUFFICIENT", .97f,
                "sources=1; agreement=agree; current=true; normal operation.", "SAFE", "CONTINUE");
            Add(cases, "A02", "CLEAR_SAFE", "NORMAL", "SUFFICIENT", .94f,
                "sources=2; agreement=agree; route clear and machinery guarded.", "SAFE", "CONTINUE");
            Add(cases, "A03", "CLEAR_SAFE", "INDICATOR_GREEN", "VERIFIED", .91f,
                "sources=3; agreement=agree; current observations.", "SAFE", "CONTINUE");
            Add(cases, "A04", "CLEAR_SAFE", "AREA_CLEAR", "SUFFICIENT", .89f,
                "sources=2; agreement=agree; no hazard cues.", "SAFE", "CONTINUE");

            Add(cases, "B01", "CLEAR_HAZARD", "INDICATOR_RED", "SUFFICIENT", .98f,
                "sources=1; agreement=agree; active equipment fault indicator.", "DANGER", "STOP");
            Add(cases, "B02", "CLEAR_HAZARD", "INDICATOR_RED", "VERIFIED", .96f,
                "sources=2; agreement=agree; current fault confirmed.", "DANGER", "STOP");
            Add(cases, "B03", "CLEAR_HAZARD", "INDICATOR_RED", "SUFFICIENT", .92f,
                "sources=3; agreement=agree; worker is approaching active hazard zone.", "DANGER", "STOP");
            Add(cases, "B04", "CLEAR_HAZARD", "INDICATOR_RED", "SUFFICIENT", .86f,
                "sources=1; agreement=agree; emergency condition is current.", "DANGER", "STOP");

            Add(cases, "C01", "AMBIGUOUS", "INDICATOR_UNCLEAR", "INSUFFICIENT", .42f,
                "sources=1; glare obscures the lamp.", "UNKNOWN", "REOBSERVE");
            Add(cases, "C02", "AMBIGUOUS", "INDICATOR_RED", "INSUFFICIENT", .51f,
                "sources=1; small red candidate below visual quality threshold.", "UNKNOWN", "REOBSERVE");
            Add(cases, "C03", "AMBIGUOUS", "UNKNOWN", "SUFFICIENT", .81f,
                "sources=1; image is sharp but indicator state is not classified.", "UNKNOWN", "REOBSERVE");
            Add(cases, "C04", "AMBIGUOUS", "INDICATOR_GREEN", "UNCLEAR", .66f,
                "sources=1; partial occlusion; safe state cannot be established.", "UNKNOWN", "REOBSERVE");

            Add(cases, "D01", "CONFLICTING", "INDICATOR_RED", "CONFLICT", .88f,
                "sources=2; agreement=conflict; current camera says RED; equally current thermal safety interlock says NORMAL; no source is authoritative.", "UNKNOWN", "REOBSERVE");
            Add(cases, "D02", "CONFLICTING", "INDICATOR_RED", "CONFLICT", .93f,
                "sources=2; agreement=conflict; RED camera frame is stale; current authenticated PLC state is GREEN and authoritative. Safety policy: discard stale evidence.", "SAFE", "CONTINUE");
            Add(cases, "D03", "CONFLICTING", "INDICATOR_GREEN", "CONFLICT", .92f,
                "sources=2; agreement=conflict; camera says GREEN; current authenticated emergency interlock reports ACTIVE and is authoritative. Safety policy: an active interlock is a hazard.", "DANGER", "STOP");
            Add(cases, "D04", "CONFLICTING", "INDICATOR_RED", "CONFLICT", .90f,
                "sources=3; agreement=conflict; RED lamp is a documented maintenance test signal while two current independent sources confirm equipment isolated and safe. Safety policy: documented test signals are not hazards.", "SAFE", "CONTINUE");
            Add(cases, "D05", "CONFLICTING", "INDICATOR_GREEN", "CONFLICT", .85f,
                "sources=3; agreement=conflict; green camera view is current but two independent current gas sensors report dangerous concentration. Safety policy: confirmed environmental hazard overrides indicator color.", "DANGER", "STOP");
            Add(cases, "D06", "CONFLICTING", "INDICATOR_RED", "CONFLICT", .84f,
                "sources=2; agreement=conflict; both sources are current and sufficient but neither has authority or corroboration.", "UNKNOWN", "REOBSERVE");

            Add(cases, "E01", "REOBSERVATION", "INDICATOR_RED", "VERIFIED", .94f,
                "sources=2; agreement=agree; prior observation was unclear; re-observation now confirms current RED.", "DANGER", "STOP");
            Add(cases, "E02", "REOBSERVATION", "INDICATOR_GREEN", "VERIFIED", .95f,
                "sources=2; agreement=agree; prior red candidate was glare; re-observation now confirms current GREEN.", "SAFE", "CONTINUE");
            Add(cases, "E03", "REOBSERVATION", "UNKNOWN", "INSUFFICIENT", .48f,
                "sources=2; agreement=conflict; prior observation unclear and re-observation remains occluded.", "UNKNOWN", "REOBSERVE");
            Add(cases, "E04", "REOBSERVATION", "INDICATOR_GREEN", "CONFLICT", .91f,
                "sources=2; agreement=conflict; prior source was GREEN; re-observation shows current RED after a state change and has the newer timestamp. Safety policy: use the newer verified state; current state is RED.", "DANGER", "STOP");

            Add(cases, "F01", "DEGRADED", "INDICATOR_GREEN", "DEGRADED", .82f,
                "sources=1; timestamp unavailable; sensor health warning active.", "UNKNOWN", "REOBSERVE");
            Add(cases, "F02", "DEGRADED", "INDICATOR_RED", "MISSING_CONTEXT", .87f,
                "sources=1; target identity missing; observation cannot be tied to inspected equipment.", "UNKNOWN", "REOBSERVE");

            return cases.AsReadOnly();
        }

        private static void Add(
            ICollection<PatrolAIEvaluationCase> cases,
            string id,
            string category,
            string condition,
            string evidence,
            float confidence,
            string context,
            string expectedRisk,
            string expectedAction)
        {
            cases.Add(new PatrolAIEvaluationCase(
                id,
                category,
                new PatrolObservation
                {
                    observationId = "phase7-" + id.ToLowerInvariant(),
                    timestamp = "2026-10-05T00:00:00Z",
                    source = "Synthetic structured safety benchmark",
                    detectedCondition = condition,
                    evidenceStatus = evidence,
                    confidence = confidence,
                    location = "Industrial inspection point A",
                    additionalContext = context
                },
                expectedRisk,
                expectedAction,
                "Label fixed by Phase 7 safety policy before inference."));
        }
    }
}
