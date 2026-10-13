using System;
using PatrolAI.UI;
using PatrolAI.Scenarios;
using PatrolAI.Evaluation;
using PatrolAI.PhysicalAI;
class ConflictDemoRegression
{
    public static int RunAll()
    {
        var scenario=PatrolConflictDemoScenario.Create();
        if(RuleBasedSafetyBaselineV2.Decide(scenario.observation).action!="REOBSERVE") throw new Exception("v2 comparison");
        var observation=new PatrolObservation {detectedCondition="UNKNOWN",evidenceStatus="CONFLICT",confidence=.8f};
        var raw=PatrolComparativeEvaluation.Decision("WARNING/REOBSERVE");
        var bounded=PatrolSafetyEnvelope.Apply(raw,observation,1,true,out bool escalated,1);
        var view=new PatrolCompetitionEvidenceState();
        var result=new PatrolClosedLoopResult { runtimeStatus="REAL",success=true,httpSuccess=true,
            parserSuccess=true,validatorSuccess=true,decision=raw,action=PatrolActionMapper.Map(raw,out bool ignored),finalState="REOBSERVING" };
        view.Apply(scenario.observation,result);
        view.ApplySecond(observation,result);
        view.SetBoundedOutcome(PatrolActionMapper.Map(bounded,out bool ignored2),escalated);
        if(view.SecondDecision!="WARNING / REOBSERVE"||view.PatrolAction!="STOP"||view.FinalState!="STOPPED"
            ||view.RuntimeStatus!="REAL"||!view.Detail.Contains("DETERMINISTIC")) throw new Exception("Raw model/envelope attribution");
        var green=PatrolConflictDemoScenario.AddResolvedContext(new PatrolObservation {
            detectedCondition="INDICATOR_GREEN",evidenceStatus="VERIFIED",confidence=.9f});
        if(PatrolSafetyEnvelope.Apply(PatrolComparativeEvaluation.Decision("SAFE/CONTINUE"),green,1,true,out bool safe,1).action!="CONTINUE")
            throw new Exception("Final outcome forced STOP");
        var failed=new PatrolClosedLoopResult {runtimeStatus="FAILED"};
        view.ApplySecond(observation,failed);
        view.SetBoundedOutcome(PatrolActionMapper.Map(PatrolSafetyEnvelope.Stop("failed"),out bool ignored3),true);
        if(view.RuntimeStatus!="FAILED"||view.SecondDecision!="NOT VERIFIED"||view.FinalState!="STOPPED") throw new Exception("Failure hidden");
        Console.WriteLine("CONFLICT_DEMO_V2_OFFLINE=PASS: comparison, bounded STOP, resolved CONTINUE, raw decision preservation, failed runtime");
        return 0;
    }
}
