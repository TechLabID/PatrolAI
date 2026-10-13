using System;
using System.Reflection;
using PatrolAI.Competition;
using PatrolAI.NVIDIA;
using PatrolAI.PhysicalAI;
using PatrolAI.Scenarios;
using PatrolAI.UI;
using UnityEngine;
using VisionGuard.Phase3;
using VisionGuard.Phase4;
using VisionGuard.Phase5;

// Runs production adapter, scenario runner, evidence state and AUTO controller with offline boundaries.
public static class PatrolUIStateRegression
{
    private static int passed;
    public static PatrolDemoScenarioRunner Setup(string mode = "SCENARIO", string nextRun = "COMPLETE")
    {
        TestObjects.Items.Clear(); TestObjects.Pending.Clear(); Time.realtimeSinceStartup = 0;
        NebiusNemotronReasoningService.Mode = mode; NebiusNemotronReasoningService.Calls = 0;
        PatrolReobservationEvidenceReader.Fresh = false;
        NebiusNemotronReasoningService.PendingCompletion = null;
        TestObjects.BeforeTick = () => {
            var completion = NebiusNemotronReasoningService.PendingCompletion;
            NebiusNemotronReasoningService.PendingCompletion = null;
            completion?.Invoke();
        };
        TestObjects.Items[typeof(VGPhase3AgentController)] = new VGPhase3AgentController { NextRun = nextRun };
        TestObjects.Items[typeof(VGPhase4FusionController)] = new VGPhase4FusionController();
        TestObjects.Items[typeof(VGPhase5TemporalController)] = new VGPhase5TemporalController();
        TestObjects.Items[typeof(VGMobileInspectorMovement)] = new VGMobileInspectorMovement();
        var adapter = new PatrolBaselineActionAdapter(); TestObjects.Items[typeof(PatrolBaselineActionAdapter)] = adapter;
        adapter.ResolveControllers();
        var runner = new PatrolDemoScenarioRunner(); TestObjects.Items[typeof(PatrolDemoScenarioRunner)] = runner;
        Invoke(runner, "Awake");
        var auto = new PatrolAutoPatrolDemoController(); TestObjects.Items[typeof(PatrolAutoPatrolDemoController)] = auto;
        Invoke(auto, "Awake"); return runner;
    }
    public static void Invoke(object target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    private static PatrolBaselineActionAdapter Adapter => TestObjects.Get<PatrolBaselineActionAdapter>();
    private static VGPhase3AgentController Controller => TestObjects.Get<VGPhase3AgentController>();
    public static void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    private static void Run(string name, Action test) { test(); passed++; Console.WriteLine("PASS: " + name); }
    public static PatrolClosedLoopResult Result(string status = "REAL") => new PatrolClosedLoopResult {
        runtimeStatus = status, success = true, httpSuccess = true, parserSuccess = true, validatorSuccess = true,
        decision = new PatrolDecision { riskLevel = "UNKNOWN", action = "REOBSERVE", reason = "OFFLINE fixture" },
        action = new PatrolAction { actionType = "REOBSERVE" }, finalState = "REOBSERVING"
    };
    private static void PreviousFailure() { Controller.NextRun = "FAILED"; Adapter.RunAgenticInspection(); TestObjects.Finish(); }
    public static void RunAll()
    {
        passed = 0;
        Run("previous Agentic FAILED -> A SAFE clears current failure", () => {
            var r = Setup(); PreviousFailure(); r.RunSafeScenario();
            Check(r.ActiveFlow == "A" && !Adapter.HasActiveAction && Adapter.AgenticStatus == "IDLE"
                && Adapter.AgenticControllerState == "IDLE" && r.Evidence.FinalState == "CONTINUING", "A inherited failure");
            Check(Controller.State == "FAILED", "Protected controller history was modified");
        });
        Run("previous Agentic FAILED -> C HAZARD clears current failure", () => {
            var r = Setup(); PreviousFailure(); r.RunHazardScenario();
            Check(r.ActiveFlow == "C" && !Adapter.HasActiveAction && Adapter.AgenticDisplayState == "IDLE"
                && r.Evidence.FinalState == "STOPPED", "C inherited failure");
        });
        Run("B running STATE and ACTIVE PHASE are consistent", () => {
            var r = Setup(nextRun: "RUNNING"); r.RunInsufficientEvidenceScenario();
            Check(r.IsRunning && Adapter.AgenticStatus == "RUNNING" && Adapter.AgenticDisplayState == "RUNNING"
                && Adapter.AgenticControllerState == "RUNNING" && r.Evidence.ReobservationStatus == "RUNNING"
                && r.Evidence.FinalState == "REOBSERVING", "B running fields disagree");
        });
        Run("B terminal physical failure stops REOBSERVING without falsifying REAL", () => {
            var r = Setup(nextRun: "RUNNING"); r.RunInsufficientEvidenceScenario();
            Controller.State = "FAILED"; Controller.IsRunning = false;
            Check(Adapter.AgenticStatus.StartsWith("FAILED") && Adapter.AgenticDisplayState == "FAILED"
                && Adapter.AgenticControllerState == "FAILED", "Terminal failure waits on stale observer cache");
            TestObjects.Finish();
            Check(!r.IsRunning && r.Evidence.ReobservationStatus == "FAILED" && r.Evidence.FinalState == "FAILED"
                && r.Evidence.RuntimeStatus == "REAL" && r.Evidence.SecondDecision == "NOT RUN"
                && r.Evidence.Detail.Contains("FAILED"), "Failure remains running or erases inference provenance");
            Check(NebiusNemotronReasoningService.Calls == 1, "Failure started second inference");
        });
        Run("B adapter unavailable is a terminal physical failure", () => {
            var r = Setup(); TestObjects.Items.Remove(typeof(PatrolBaselineActionAdapter));
            typeof(PatrolDemoScenarioRunner).GetField("baselineAdapter", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(r, null);
            r.RunInsufficientEvidenceScenario();
            Check(!r.IsRunning && r.Evidence.FinalState == "FAILED" && r.Evidence.ReobservationStatus == "FAILED"
                && r.Evidence.RuntimeStatus == "REAL", "Missing adapter left REOBSERVING");
        });
        Run("B timeout is terminal and does not cancel protected movement", () => {
            var r = Setup(nextRun: "RUNNING"); r.RunInsufficientEvidenceScenario();
            typeof(PatrolDemoScenarioRunner).GetField("reobservationStartedUtc", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(r, DateTime.UtcNow.AddMinutes(-3)); TestObjects.Tick();
            Check(!r.IsRunning && r.Evidence.FinalState == "FAILED" && r.Evidence.Detail.Contains("TIMEOUT")
                && Adapter.IsWorkflowRunning, "Timeout hid failure or changed physical execution");
            Check(TestObjects.Get<PatrolAutoPatrolDemoController>().ControlsBusy, "Settling baseline was unlocked");
            Controller.IsRunning = false; Controller.State = "FAILED"; TestObjects.Finish();
        });
        Run("B missing fresh evidence is terminal without a second call", () => {
            var r = Setup(); r.RunInsufficientEvidenceScenario(); TestObjects.Finish();
            Check(r.Evidence.FinalState == "FAILED" && r.Evidence.ReobservationStatus == "FAILED"
                && NebiusNemotronReasoningService.Calls == 1, "Missing evidence was reported as ongoing");
        });
        Run("FAILED cannot display as COMPLETE", () => {
            Check(PatrolBaselineActionAdapter.DisplayState(false, "COMPLETE", "FAILED — CONTROLLER LOST") == "FAILED", "Cached failure earned completion");
            Check(PatrolBaselineActionAdapter.DisplayState(false, "FAILED", "COMPLETE") == "FAILED", "Controller failure earned completion");
            Check(PatrolBaselineActionAdapter.DisplayPhase(false, "COMPLETE", "FAILED") == "FAILED", "Phase disagreed with failure");
        });
        Run("IDLE hides previous terminal phase after ownership reset", () => {
            var r = Setup(); PreviousFailure(); r.BeginFlow("A", PatrolDemoScenario.Safe().observation);
            Check(Adapter.AgenticDisplayState == "IDLE" && Adapter.AgenticControllerState == "IDLE"
                && Adapter.FusionStatus == "IDLE" && Adapter.TemporalStatus == "IDLE", "Old terminal field is current");
        });
        Run("retry returning home does not expose previous FAILED as active phase", () => {
            Setup(); PreviousFailure(); Controller.NextRun = "STARTUP"; Adapter.RunAgenticInspection();
            Check(Adapter.AgenticDisplayState == "RUNNING" && Adapter.AgenticControllerState == "RUNNING", "Startup exposed old failure");
            Controller.State = "OBSERVE_PRIMARY";
            Check(Adapter.AgenticControllerState == "OBSERVE_PRIMARY", "Current phase was not propagated");
            Controller.IsRunning = false; Controller.State = "COMPLETE"; TestObjects.Finish();
        });
        Run("obsolete completion observer cannot resurrect old flow evidence", () => {
            var r = Setup(nextRun: "RUNNING"); Adapter.RunAgenticInspection(); r.BeginFlow("C", PatrolDemoScenario.Hazard().observation);
            Controller.IsRunning = false; Controller.State = "FAILED"; TestObjects.Finish();
            Check(!Adapter.HasActiveAction && Adapter.AgenticStatus == "IDLE" && r.Evidence.RuntimeStatus == "NOT VERIFIED", "Old callback overwrote new flow");
        });
        Run("detail selection cache clears on ownership reset", () => {
            var detail = new PatrolBaselineWorkflowDetailState(); detail.Select(PatrolBaselineAction.AgenticInspection); detail.Clear();
            Check(!detail.HasSelection, "Detail retained current selection");
        });
        Run("new flow clears decisions, comparison, failure and confidence", () => {
            var r = Setup(); r.Evidence.Apply(PatrolDemoScenario.InsufficientEvidence().observation, Result());
            r.Evidence.SetReobservationFailed("PHYSICAL RE-OBSERVATION FAILED"); r.Evidence.SetComparison("previous flow");
            r.BeginFlow("C", PatrolDemoScenario.Hazard().observation);
            Check(r.LatestResult == null && r.Evidence.RuntimeStatus == "NOT VERIFIED" && r.Evidence.PatrolDecision == "NOT VERIFIED"
                && r.Evidence.FinalState == "NOT VERIFIED" && r.Evidence.ReobservationStatus == "NOT RUN"
                && r.Evidence.SecondDecision == "NOT RUN" && r.Evidence.Comparison == ""
                && r.Evidence.Evidence == "SUFFICIENT" && r.Evidence.PerceptionConfidence == PatrolDemoScenario.Hazard().observation.confidence.ToString("0.00"), "Stale result survived begin");
        });
        Run("REAL requires every existing verification flag", () => {
            foreach (string flag in new[] { "success", "httpSuccess", "parserSuccess", "validatorSuccess", "usedSafeFallback" }) {
                var result = Result(); typeof(PatrolClosedLoopResult).GetField(flag).SetValue(result, flag == "usedSafeFallback");
                var e = new PatrolCompetitionEvidenceState(); e.Apply(null, result);
                Check(e.RuntimeStatus == "FAILED" && e.FinalState == "ERROR", "Unverified REAL was accepted: " + flag);
            }
        });
        Run("REAL / FAILED / FALLBACK / MOCK / LOCAL SAMPLE remain distinct", () => {
            foreach (string status in new[] { "REAL", "FAILED", "FALLBACK", "MOCK", "LOCAL SAMPLE", "NOT VERIFIED" }) {
                var e = new PatrolCompetitionEvidenceState(); e.Apply(null, Result(status));
                Check(e.RuntimeStatus == status, "Runtime relabeled: " + status);
            }
        });
        Run("pending second reasoning retains only explicitly labeled first decision", () => {
            var e = new PatrolCompetitionEvidenceState(); e.Apply(null, Result()); e.BeginSecondReasoning();
            Check(e.RuntimeStatus == "NOT VERIFIED" && e.PatrolDecision == "NOT VERIFIED"
                && e.FirstDecision == "UNKNOWN / REOBSERVE" && e.PatrolAction == "NOT EXECUTED", "Pending second reasoning shows stale current decision");
        });
        Run("bounded STOP retains physical failure detail and REAL provenance", () => {
            var e = new PatrolCompetitionEvidenceState(); e.Apply(null, Result()); e.SetReobservationFailed("PHYSICAL RE-OBSERVATION FAILED");
            e.SetBoundedOutcome(new PatrolAction { actionType = "STOP" }, true);
            Check(e.FinalState == "STOPPED" && e.Detail.Contains("FAILED") && e.Detail.Contains("HUMAN REVIEW")
                && e.RuntimeStatus == "REAL", "Bounded stop concealed failure");
        });
        Run("AUTO selection resets failed baseline ownership before moving", () => {
            var r = Setup("STOP"); PreviousFailure(); var a = TestObjects.Get<PatrolAutoPatrolDemoController>(); a.StartPatrol();
            Check(r.ActiveFlow == "AUTO PATROL" && !Adapter.HasActiveAction && r.LatestResult == null
                && r.Evidence.RuntimeStatus == "NOT VERIFIED", "AUTO inherited stale evidence"); TestObjects.Finish();
        });
        Run("failed AUTO -> A clears current AUTO footer without rewriting history", () => {
            var r = Setup("FAILED"); var a = TestObjects.Get<PatrolAutoPatrolDemoController>(); a.StartPatrol(); TestObjects.Finish();
            NebiusNemotronReasoningService.Mode = "SCENARIO"; r.RunSafeScenario();
            Check(a.State == "FAILED" && a.Status == "AUTO PATROL: IDLE" && r.Evidence.FinalState == "CONTINUING", "Previous AUTO failure remains current");
        });
        Run("second inference failure is terminal without losing first decision", () => {
            var e = new PatrolCompetitionEvidenceState(); e.Apply(null, Result()); e.BeginSecondReasoning(); e.ApplySecond(null, Result("FAILED"));
            Check(e.FinalState == "ERROR" && e.RuntimeStatus == "FAILED" && e.FirstDecision == "UNKNOWN / REOBSERVE"
                && e.SecondDecision == "NOT VERIFIED" && e.ReobservationStatus == "COMPLETE", "Failed inference confused with failed capture or completion");
        });
        Run("D ownership and existing bounded STOP survive physical failure", () => {
            var r = Setup(); PreviousFailure(); Controller.NextRun = "FAILED";
            Invoke(r, "Run", PatrolConflictDemoScenario.Create()); TestObjects.Finish();
            Check(r.ActiveFlow == "D" && r.Evidence.FinalState == "STOPPED" && r.Evidence.ReobservationStatus == "FAILED"
                && r.LatestResult.finalBoundedDecision.action == "STOP" && r.Evidence.Detail.Contains("FAILED"), "D safety semantics or failure evidence changed");
        });
        Run("new B after previous failure reaches its new successful second result", () => {
            var r = Setup(); PreviousFailure(); Controller.NextRun = "COMPLETE"; PatrolReobservationEvidenceReader.Fresh = true;
            NebiusNemotronReasoningService.Mode = "REOBSERVE_THEN_STOP";
            r.RunInsufficientEvidenceScenario(); TestObjects.Finish();
            Check(r.Evidence.FinalState == "STOPPED" && r.Evidence.ReobservationStatus == "COMPLETE"
                && Adapter.AgenticStatus == "COMPLETE" && NebiusNemotronReasoningService.Calls == 2,
                "Successful new B inherited failure");
        });
        Console.WriteLine("UI STATE SUMMARY: " + passed + "/22 PASS; OFFLINE FIXTURES ONLY; HTTP REQUESTS=0");
    }
    public static int Main() { try { RunAll(); AutoPatrolRegression.RunAll(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; } }
}
