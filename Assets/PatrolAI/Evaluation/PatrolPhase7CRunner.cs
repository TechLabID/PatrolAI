using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using PatrolAI.NVIDIA;
using PatrolAI.PhysicalAI;

namespace PatrolAI.Evaluation
{
    public static class PatrolPhase7CRunner
    {
        public const int MaximumLiveCalls = 16;
        public static HeldOutPrediction Score(PatrolAIEvaluationCase c,string system,PatrolDecision raw,
            bool valid = true,string runtime="LOCAL RULE",long latency=0,int http=0,bool invalidOutput=false)
        {
            var effective = PatrolSafetyEnvelope.Apply(raw,c.observation,PatrolSafetyEnvelope.Attempts(c.observation),valid,out bool escalated);
            string action = PatrolActionMapper.Map(effective,out bool ignored).actionType;
            string rawAction = PatrolActionMapper.Map(raw,out bool rawFallback).actionType;
            var result = new HeldOutPrediction {
                system=system, expectedDecision=c.ExpectedDisposition,expectedAction=c.ExpectedAction,
                rawDecision=raw,effectiveDecision=effective, action=action,rawAction=rawFallback?"NOT_VERIFIED":rawAction,
                runtimeStatus=runtime,validOutput=valid,latencyMilliseconds=latency,httpStatusCode=http,
                invalidOutput=invalidOutput,fallbackUsed=!valid||rawFallback,escalated=escalated,
                envelopeChanged=PatrolComparativeEvaluation.Disposition(raw)!=PatrolComparativeEvaluation.Disposition(effective),
                unresolved=action=="REOBSERVE",
                exactCorrect=valid && PatrolComparativeEvaluation.Disposition(effective)==c.ExpectedDisposition,
                actionCorrect=valid && action==c.ExpectedAction,
                rawExactCorrect=valid && !rawFallback && PatrolComparativeEvaluation.Disposition(raw)==c.ExpectedDisposition,
                rawActionCorrect=valid && !rawFallback && rawAction==c.ExpectedAction
            };
            return result;
        }

        public static HeldOutMetrics Metrics(IEnumerable<HeldOutPrediction> predictions)
        {
            var list=predictions.ToArray();
            var rows=list.Select(p=>new PatrolComparativeRun { expectedAction=p.expectedAction,ruleAction=p.action,
                ruleExactCorrect=p.exactCorrect,ruleActionCorrect=p.actionCorrect }).ToArray();
            var safety=PatrolComparativeEvaluation.Metrics(rows,false);
            safety.invalidOutputs=list.Count(p=>p.invalidOutput);
            safety.failedRuntime=list.Count(p=>p.system=="NEMOTRON" && p.runtimeStatus!="REAL");
            safety.fallbacks=list.Count(p=>p.fallbackUsed);
            return new HeldOutMetrics { safety=safety,rawExactCorrect=list.Count(p=>p.rawExactCorrect),
                rawActionCorrect=list.Count(p=>p.rawActionCorrect),unresolved=list.Count(p=>p.unresolved),
                escalated=list.Count(p=>p.escalated),envelopeOverrides=list.Count(p=>p.envelopeChanged) };
        }

        public static FrozenHeldOutCase[] Freeze()
        {
            return PatrolHeldOutDataset.Create().Select(c=>new FrozenHeldOutCase {
                caseId=c.caseId,category=c.category,observation=c.observation,expectedDecision=c.ExpectedDisposition,
                expectedAction=c.ExpectedAction,policyRationale=c.policyRationale }).ToArray();
        }

        public static async Task RunAsync()
        {
            string directory=Path.Combine("evaluation","results","phase7c");
            Directory.CreateDirectory(directory);
            // CreateNew reserves the entire run; never silently resume or overwrite.
            using(var stream=new FileStream(Path.Combine(directory,"run_started.json"),FileMode.CreateNew,FileAccess.Write)) {
                byte[] bytes=Encoding.UTF8.GetBytes("{\"plannedCalls\":16,\"retries\":0}");stream.Write(bytes,0,bytes.Length);
            }
            var frozen=Freeze();
            if(frozen.Length!=MaximumLiveCalls) throw new InvalidOperationException("Held-out count must be 16.");
            string snapshot=PatrolPhase7BRunner.Serialize(frozen);
            File.WriteAllText(Path.Combine(directory,"frozen_cases.json"),snapshot);
            var paths=new[] {"Assets/PatrolAI/Evaluation/PatrolHeldOutDataset.cs","Assets/PatrolAI/Evaluation/RuleBasedSafetyBaseline.cs",
                "Assets/PatrolAI/Evaluation/RuleBasedSafetyBaselineV2.cs","Assets/PatrolAI/PhysicalAI/PatrolSafetyEnvelope.cs",
                "Assets/PatrolAI/NVIDIA/NebiusNemotronReasoningService.cs","Assets/PatrolAI/Nebius/NebiusClient.Chat.cs",
                "Assets/PatrolAI/PhysicalAI/PatrolActionMapper.cs","Assets/PatrolAI/Evaluation/README_PHASE7C.md"};
            string fingerprint=string.Join("\n",paths.Select(p=>p+"="+Hash(File.ReadAllBytes(p))));
            File.WriteAllText(Path.Combine(directory,"frozen_source_hashes.txt"),fingerprint+"\nfrozen_cases="+Hash(Encoding.UTF8.GetBytes(snapshot)));
            var results=new List<HeldOutResult>();
            using(var service=new NebiusNemotronReasoningService()) foreach(var c in PatrolHeldOutDataset.Create()) {
                var live=await service.ReasonWithDiagnosticsAsync(c.observation).ConfigureAwait(false);
                bool valid=live.requestStarted&&live.httpSuccess&&live.parserSuccess&&live.validatorSuccess&&!live.validatorUsedSafeNormalization;
                var row=new HeldOutResult {caseId=c.caseId,category=c.category,timestamp=DateTime.UtcNow.ToString("O"),modelId=live.modelId,
                    v1=Score(c,"RULE_V1",RuleBasedSafetyBaseline.Decide(c.observation)),
                    v2=Score(c,"RULE_V2",RuleBasedSafetyBaselineV2.Propose(c.observation)),
                    nemotron=Score(c,"NEMOTRON",live.decision,valid,valid?"REAL":"FAILED",live.elapsedMilliseconds,live.statusCode,
                        live.httpSuccess&&(!live.parserSuccess||!live.validatorSuccess||live.validatorUsedSafeNormalization))};
                results.Add(row);
                File.WriteAllText(Path.Combine(directory,c.caseId+".json"),PatrolPhase7BRunner.Serialize(row));
                File.WriteAllText(Path.Combine(directory,"heldout_results.json"),PatrolPhase7BRunner.Serialize(results.ToArray()));
            }
            if(fingerprint!=string.Join("\n",paths.Select(p=>p+"="+Hash(File.ReadAllBytes(p))))
                ||snapshot!=PatrolPhase7BRunner.Serialize(Freeze())) throw new InvalidOperationException("Frozen inputs/policy changed during run.");
            WriteSummary(directory);
            Console.WriteLine("HELDOUT_COMPLETE: "+results.Count+" requests; REAL="+results.Count(r=>r.nemotron.runtimeStatus=="REAL"));
        }

        public static void WriteSummary(string directory)
        {
            var rows=PatrolPhase7BRunner.Read<HeldOutResult[]>(Path.Combine(directory,"heldout_results.json"));
            var sorted=rows.Select(r=>r.nemotron.latencyMilliseconds).OrderBy(v=>v).ToArray();
            var summary=new HeldOutReport {caseCount=rows.Length,v1=Metrics(rows.Select(r=>r.v1)),v2=Metrics(rows.Select(r=>r.v2)),
                nemotron=Metrics(rows.Select(r=>r.nemotron)),meanLatencyMilliseconds=sorted.Average(v=>(double)v),
                medianLatencyMilliseconds=(sorted[(sorted.Length-1)/2]+sorted[sorted.Length/2])/2d,
                categories=rows.GroupBy(r=>r.category).OrderBy(g=>g.Key).Select(g=>new HeldOutCategory {
                    category=g.Key,count=g.Count(),v1=Metrics(g.Select(r=>r.v1)),v2=Metrics(g.Select(r=>r.v2)),
                    nemotron=Metrics(g.Select(r=>r.nemotron))}).ToArray()};
            File.WriteAllText(Path.Combine(directory,"heldout_summary.json"),PatrolPhase7BRunner.Serialize(summary));
        }

        static string Hash(byte[] bytes) { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-",""); }
#if PATROLAI_PHASE7C_LIVE
        public static int Main() {RunAsync().GetAwaiter().GetResult();return 0;}
#endif
    }
    [Serializable] public sealed class FrozenHeldOutCase {
        public string caseId,category,expectedDecision,expectedAction,policyRationale;public PatrolObservation observation;
    }
    [Serializable] public sealed class HeldOutPrediction {
        public string system,expectedDecision,expectedAction,action,rawAction,runtimeStatus;
        public PatrolDecision rawDecision,effectiveDecision;
        public bool validOutput,invalidOutput,fallbackUsed,escalated,envelopeChanged,unresolved;
        public bool exactCorrect,actionCorrect,rawExactCorrect,rawActionCorrect;
        public int httpStatusCode;public long latencyMilliseconds;
    }
    [Serializable] public sealed class HeldOutResult {
        public string caseId,category,timestamp,modelId;public HeldOutPrediction v1,v2,nemotron;
    }
    [Serializable] public sealed class HeldOutMetrics {
        public PatrolSafetyMetrics safety;public int rawExactCorrect,rawActionCorrect,unresolved,escalated,envelopeOverrides;
    }
    [Serializable] public sealed class HeldOutCategory {
        public string category;public int count;public HeldOutMetrics v1,v2,nemotron;
    }
    [Serializable] public sealed class HeldOutReport {
        public int caseCount;public HeldOutMetrics v1,v2,nemotron;public HeldOutCategory[] categories;
        public double meanLatencyMilliseconds,medianLatencyMilliseconds;
    }
}
