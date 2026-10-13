using System.Collections.Generic;

namespace PatrolAI.Evaluation
{
    /// <summary>New frozen pilot cases; all labels authored before inference, without external review.</summary>
    public static class PatrolHeldOutDataset
    {
        public static IReadOnlyList<PatrolAIEvaluationCase> Create()
        {
            var list = new List<PatrolAIEvaluationCase>();
            Add(list,"HC01","CLEAR_SAFE","AREA_CLEAR","VERIFIED",.96f,
                "sources=2; agreement=agree; conveyor locked out for cleaning, corridor clear; no conflicting signals.","SAFE/CONTINUE");
            Add(list,"HC02","CLEAR_SAFE","INDICATOR_GREEN","SUFFICIENT",.83f,
                "sources=1; agreement=agree; ventilation running within limits; routine occupancy permitted.","SAFE/CONTINUE");
            Add(list,"HC03","CLEAR_HAZARD","INDICATOR_RED","VERIFIED",.89f,
                "sources=2; agreement=agree; current pump overpressure alarm, pressure independently confirmed high.","DANGER/STOP");
            Add(list,"HC04","CLEAR_HAZARD","INDICATOR_GREEN","SUFFICIENT",.97f,
                "sources=2; agreement=agree; confirmed_hazard=true; current electrical insulation monitor confirms exposed live conductor despite green drive indicator.","DANGER/STOP");
            Add(list,"HC05","AMBIGUOUS","INDICATOR_RED","INSUFFICIENT",.73f,
                "sources=1; primary red visual candidate; wet lens creates colored reflections, only 6 visible lamp pixels; confirm by another view.","UNKNOWN/REOBSERVE");
            Add(list,"HC06","AMBIGUOUS","INDICATOR_GREEN","UNCLEAR",.58f,
                "sources=2; green camera candidate low confidence; acoustic sensor heard a single unclassified click, no confirmed equipment fault.","UNKNOWN/REOBSERVE");
            Add(list,"HC07","AMBIGUOUS","UNKNOWN","SUFFICIENT",.92f,
                "sources=1; complete sharp image but multiple unlabeled lamps cannot be associated with the target compressor.","UNKNOWN/REOBSERVE");
            Add(list,"HC08","CONFLICTING","INDICATOR_RED","CONFLICT",.94f,
                "sources=2; agreement=conflict; current same-target camera RED confidence 0.94, independent secondary normal vibration confidence 0.82; neither source is authoritative, color alone does not resolve disagreement.","UNKNOWN/REOBSERVE");
            Add(list,"HC09","CONFLICTING","INDICATOR_GREEN","SUFFICIENT",.93f,
                "sources=2; green conveyor indicator reports drive available. A current verified guard inspection reports a person inside the unguarded moving-belt pinch zone. Drive readiness does not establish personnel safety; confirmed direct danger requires immediate stop.","DANGER/STOP");
            Add(list,"HC10","CONFLICTING","INDICATOR_RED","CONFLICT",.88f,
                "sources=3; agreement=conflict; red lamp is credible but only 10 minutes old; two current cameras show green. No authorized restart record exists; age alone cannot establish hazard clearance.","UNKNOWN/REOBSERVE");
            Add(list,"HC11","CONFLICTING","INDICATOR_GREEN","SUFFICIENT",.90f,
                "sources=2; green indicator means motor stopped, not safe access. Current verified temperature probe shows a 160 degree C exposed vessel surface on the patrol path; contact hazard confirmed, stop the patrol.","DANGER/STOP");
            Add(list,"HC12","CONFLICTING","INDICATOR_RED","CONFLICT",.91f,
                "sources=2; agreement=conflict; same-target red camera and normal control telemetry remain contradictory after two inspections; reobservation_attempts=2; no authoritative resolution; budget exhausted, conservative escalation is required.","DANGER/STOP");
            Add(list,"HC13","DEGRADED","INDICATOR_RED","STALE",.96f,
                "sources=1; camera feed frozen at yesterday's frame; current equipment state unavailable. Historic alarm alone does not prove current hazard or safety.","UNKNOWN/REOBSERVE");
            Add(list,"HC14","DEGRADED","INDICATOR_GREEN","MISSING_CONTEXT",.87f,
                "sources=2; requested thermal safety source failed to arrive and camera target ID is absent; current green cannot authorize access.","UNKNOWN/REOBSERVE");
            Add(list,"HC15","REOBSERVATION","INDICATOR_GREEN","VERIFIED",.91f,
                "sources=2; agreement=agree; prior pump hazard was isolated by a logged authorized reset. Two current independent same-target views confirm green after the reset; reobservation_attempts=1; conflict resolved.","SAFE/CONTINUE");
            Add(list,"HC16","REOBSERVATION","UNKNOWN","INSUFFICIENT",.39f,
                "sources=2; smoke still obscures identification on a third observation; prior hazard status cannot be verified clear; reobservation_attempts=2; last two attempts failed to resolve state; conservative stop and human review required.","DANGER/STOP");
            return list.AsReadOnly();
        }

        private static void Add(ICollection<PatrolAIEvaluationCase> list,string id,string category,
            string condition,string quality,float confidence,string context,string expected)
        {
            string[] label = expected.Split('/');
            list.Add(new PatrolAIEvaluationCase(id,category,new PatrolObservation {
                observationId="heldout-"+id, timestamp="2026-10-08T00:00:00Z",
                source="Pilot synthetic/local held-out scenario", location="Pump/conveyor/ventilation inspection zone",
                detectedCondition=condition,evidenceStatus=quality,confidence=confidence,additionalContext=context
            },label[0],label[1],"Internally proposed label, frozen before inference; external review pending."));
        }
    }
}
