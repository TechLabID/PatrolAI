using System;
using System.IO;
using System.Linq;
using PatrolAI.NVIDIA;
using PatrolAI.PhysicalAI;

namespace PatrolAI.Evaluation
{
    public static class PatrolPhase7CTestHarness
    {
        static void Check(bool test,string name) { if(!test)throw new InvalidOperationException(name);Console.WriteLine("PASS: "+name); }
        static PatrolObservation Observation(string condition,string status,float score=.9f,string context="") {
            return new PatrolObservation { detectedCondition=condition,evidenceStatus=status,confidence=score,additionalContext=context };
        }
        public static int Main()
        {
            Check(RuleBasedSafetyBaselineV2.Decide(Observation("INDICATOR_RED","SUFFICIENT")).action=="STOP","v2 clear red");
            Check(RuleBasedSafetyBaselineV2.Decide(Observation("INDICATOR_GREEN","SUFFICIENT")).action=="CONTINUE","v2 clear green");
            Check(RuleBasedSafetyBaselineV2.Decide(Observation("INDICATOR_RED","CONFLICT")).action=="REOBSERVE","v2 unresolved conflict");
            Check(RuleBasedSafetyBaselineV2.Decide(Observation("INDICATOR_GREEN","INSUFFICIENT",.3f)).action=="REOBSERVE","v2 weak evidence");
            Check(RuleBasedSafetyBaselineV2.Decide(Observation("INDICATOR_GREEN","SUFFICIENT",.9f,"confirmed_hazard=true")).action=="STOP","v2 known hazard override");
            var conflict=Observation("INDICATOR_RED","CONFLICT",.9f,"reobservation_attempts=2;");
            Check(RuleBasedSafetyBaselineV2.Decide(conflict).action=="STOP","v2 exhausted conflict escalation");
            var reobserve=PatrolComparativeEvaluation.Decision("WARNING/REOBSERVE");
            Check(PatrolSafetyEnvelope.Apply(reobserve,conflict,1,true,out bool before).action=="REOBSERVE"&&!before,"budget remains");
            Check(PatrolSafetyEnvelope.Apply(reobserve,conflict,2,true,out bool exhausted).action=="STOP"&&exhausted,"budget exhausted");
            Check(PatrolSafetyEnvelope.Apply(reobserve,conflict,1,true,out bool final,1).action=="STOP"&&final,"demo terminal budget");
            Check(PatrolSafetyEnvelope.Apply(null,conflict,0,false,out bool failed).action=="STOP"&&failed,"failed inference stops");
            var bad=PatrolComparativeEvaluation.Decision("SAFE/STOP");
            Check(PatrolSafetyEnvelope.Apply(bad,conflict,0,true,out bool malformed).action=="STOP"&&malformed,"unsupported mapping stops");
            var safe=Observation("INDICATOR_GREEN","VERIFIED");
            Check(PatrolSafetyEnvelope.Apply(PatrolComparativeEvaluation.Decision("SAFE/CONTINUE"),safe,2,true,out bool no).action=="CONTINUE"&&!no,"resolved evidence can continue");
            var cases=PatrolHeldOutDataset.Create();
            Check(cases.Count==16 && cases.Select(c=>c.caseId).Distinct().Count()==16
                && !cases.Select(c=>c.caseId).Intersect(PatrolAIEvaluationDataset.Create().Select(c=>c.caseId)).Any(),"16 new unique cases");
            Check(!typeof(PatrolAIEvaluationCase).GetProperty("ExpectedAction").CanWrite
                && PatrolAIBenchmarkRunner.LabelFingerprint(cases)==PatrolAIBenchmarkRunner.LabelFingerprint(PatrolHeldOutDataset.Create()),"label immutability");
            Check(cases.GroupBy(c=>c.category).OrderBy(g=>g.Key).Select(g=>g.Count()).SequenceEqual(new[]{3,2,2,5,2,2}),"category distribution");
            var replay=PatrolPhase7CRunner.Score(cases[0],"NEMOTRON",null,false,"FAILED");
            Check(!replay.actionCorrect&&replay.action=="STOP"&&replay.fallbackUsed,"fallback not credited as inference");
            var wrong=PatrolPhase7CRunner.Score(cases[0],"RULE",PatrolComparativeEvaluation.Decision("DANGER/STOP"));
            var metrics=PatrolPhase7CRunner.Metrics(new[]{wrong});
            Check(metrics.safety.falseStops==1&&metrics.safety.nonStopCases==1,"shared safety denominators");
            string validationDirectory = Path.Combine("evaluation", "results", "validation", "phase7c");
            Directory.CreateDirectory(validationDirectory);
            string path = Path.Combine(validationDirectory, "roundtrip.json");
            File.WriteAllText(path,PatrolPhase7BRunner.Serialize(PatrolPhase7CRunner.Freeze()));
            Check(PatrolPhase7BRunner.Read<FrozenHeldOutCase[]>(path).Length==16,"frozen snapshot roundtrip");
            Console.WriteLine(NemotronOfflineTestHarness.RunAll());
            Console.WriteLine("PHASE7C_OFFLINE=PASS; LIVE_CALLS=0");
            return 0;
        }
    }
}
