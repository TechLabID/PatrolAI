using System;
using System.Collections.Generic;
using System.Linq;
using PatrolAI.PhysicalAI;

namespace PatrolAI.Evaluation
{
    // The same production whitelist is used for expected, rule and model actions.
    // Validated decision pairs in the old artifact can be replayed through this
    // mapper; unknown/missing/failed predictions never earn model correctness.
    public static class PatrolComparativeEvaluation
    {
        public static PatrolDecision Decision(string disposition, string reason = "Stored validated decision")
        {
            string[] pair = (disposition ?? "").Split('/');
            if (pair.Length != 2) return null;
            return new PatrolDecision { riskLevel = pair[0], action = pair[1],
                reason = reason, confidence = 1f, requiresReobservation = pair[1] == "REOBSERVE" };
        }

        public static string MappedAction(PatrolDecision decision, out bool fallback)
        {
            return PatrolActionMapper.Map(decision, out fallback).actionType;
        }

        public static string Disposition(PatrolDecision decision)
        {
            return decision == null ? "NOT_VERIFIED" : decision.riskLevel + "/" + decision.action;
        }

        public static void Score(PatrolComparativeRun row)
        {
            row.expectedAction = MappedAction(Decision(row.expectedDecision), out bool expectedFallback);
            if (expectedFallback) throw new InvalidOperationException("Invalid expected disposition.");
            row.ruleAction = MappedAction(Decision(row.ruleDecision), out row.ruleMappingFallback);
            string mapped = MappedAction(row.modelDecision, out row.modelMappingFallback);
            bool valid = row.runtimeStatus == "REAL" && row.validOutput
                && !row.fallbackUsed && !row.modelMappingFallback;
            row.nemotronDecision = Disposition(row.modelDecision);
            row.nemotronAction = valid ? mapped : "NOT_VERIFIED";
            row.ruleExactCorrect = !row.ruleMappingFallback && row.ruleDecision == row.expectedDecision;
            row.ruleActionCorrect = !row.ruleMappingFallback && row.ruleAction == row.expectedAction;
            row.nemotronExactCorrect = valid && row.nemotronDecision == row.expectedDecision;
            row.nemotronActionCorrect = valid && row.nemotronAction == row.expectedAction;
            row.safetyImpact = !valid ? "Unverified inference; no model action credited"
                : row.nemotronActionCorrect && !row.nemotronExactCorrect
                    ? "Correct bounded action; risk-label mismatch"
                    : row.nemotronActionCorrect ? "Correct disposition and action"
                    : row.nemotronAction == "CONTINUE" ? "Inappropriate CONTINUE"
                    : row.expectedAction == "STOP" ? "Missed required STOP"
                    : row.nemotronAction == "STOP" ? "False STOP"
                    : row.nemotronAction == "REOBSERVE" ? "Unnecessary REOBSERVE" : "Wrong bounded action";
        }

        public static PatrolSafetyMetrics Metrics(IEnumerable<PatrolComparativeRun> rows, bool model)
        {
            var data = rows.ToArray();
            Func<PatrolComparativeRun, string> action = r => model ? r.nemotronAction : r.ruleAction;
            return new PatrolSafetyMetrics {
                total = data.Length,
                exactCorrect = data.Count(r => model ? r.nemotronExactCorrect : r.ruleExactCorrect),
                actionCorrect = data.Count(r => model ? r.nemotronActionCorrect : r.ruleActionCorrect),
                nonStopCases = data.Count(r => r.expectedAction != "STOP"),
                hazardCases = data.Count(r => r.expectedAction == "STOP"),
                nonContinueCases = data.Count(r => r.expectedAction != "CONTINUE"),
                reobserveCases = data.Count(r => r.expectedAction == "REOBSERVE"),
                nonReobserveCases = data.Count(r => r.expectedAction != "REOBSERVE"),
                falseStops = data.Count(r => r.expectedAction != "STOP" && action(r) == "STOP"),
                missedStops = data.Count(r => r.expectedAction == "STOP" && action(r) != "STOP"),
                inappropriateContinues = data.Count(r => r.expectedAction != "CONTINUE" && action(r) == "CONTINUE"),
                correctReobserves = data.Count(r => r.expectedAction == "REOBSERVE" && action(r) == "REOBSERVE"),
                unnecessaryReobserves = data.Count(r => r.expectedAction != "REOBSERVE" && action(r) == "REOBSERVE"),
                invalidOutputs = model ? data.Count(r => r.httpSuccess && !r.validOutput) : 0,
                failedRuntime = model ? data.Count(r => r.runtimeStatus != "REAL") : 0,
                fallbacks = model ? data.Count(r => r.fallbackUsed || r.modelMappingFallback) : 0
            };
        }

        public static PatrolConsistencyCase[] Consistency(IEnumerable<PatrolComparativeRun> rows)
        {
            return rows.GroupBy(r => r.caseId).OrderBy(g => g.Key).Select(g => {
                var all = g.ToArray();
                var valid = all.Where(r => r.runtimeStatus == "REAL" && r.validOutput
                    && !r.fallbackUsed && !r.modelMappingFallback).ToArray();
                return new PatrolConsistencyCase {
                    caseId = g.Key, runCount = all.Length, validRunCount = valid.Length,
                    sameDecisionCount = Modal(valid.Select(r => r.nemotronDecision)),
                    sameActionCount = Modal(valid.Select(r => r.nemotronAction)),
                    exactCorrect = all.Count(r => r.nemotronExactCorrect),
                    actionCorrect = all.Count(r => r.nemotronActionCorrect)
                };
            }).ToArray();
        }

        private static int Modal(IEnumerable<string> values)
        {
            return values.GroupBy(v => v).Select(g => g.Count()).DefaultIfEmpty(0).Max();
        }
    }

    [Serializable]
    public sealed class PatrolComparativeRun
    {
        public string timestamp, caseId, category, modelId, expectedDecision, expectedAction;
        public string ruleDecision, ruleAction, nemotronDecision, nemotronAction, runtimeStatus, safetyImpact;
        public int runIndex, httpStatusCode;
        public long latencyMilliseconds;
        public bool httpSuccess, validOutput, fallbackUsed, ruleMappingFallback, modelMappingFallback;
        public bool ruleExactCorrect, ruleActionCorrect, nemotronExactCorrect, nemotronActionCorrect;
        public bool decisionReconstructedFromStoredPair;
        public PatrolDecision modelDecision;
    }

    [Serializable]
    public sealed class PatrolSafetyMetrics
    {
        public int total, exactCorrect, actionCorrect, nonStopCases, hazardCases, nonContinueCases;
        public int reobserveCases, nonReobserveCases, falseStops, missedStops, inappropriateContinues;
        public int correctReobserves, unnecessaryReobserves, invalidOutputs, failedRuntime, fallbacks;
    }

    [Serializable]
    public sealed class PatrolConsistencyCase
    {
        public string caseId;
        public int runCount, validRunCount, sameDecisionCount, sameActionCount, exactCorrect, actionCorrect;
    }
}
