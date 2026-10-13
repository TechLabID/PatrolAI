using System;
using System.Collections.Generic;
using PatrolAI.Scenarios;
using PatrolAI.NVIDIA;
using PatrolAI.Competition;
using PatrolAI.PhysicalAI;
using PatrolAI.Evaluation;
using UnityEngine;
using VisionGuard.Phase3;

public static class AutoPatrolRegression
{
    private static int passed;
    private static void Check(bool condition, string name) {
        PatrolUIStateRegression.Check(condition, name); passed++; Console.WriteLine("PASS: AUTO " + name);
    }
    private static PatrolAutoPatrolDemoController Setup(string mode) {
        PatrolUIStateRegression.Setup(mode); return TestObjects.Get<PatrolAutoPatrolDemoController>();
    }
    private static PatrolAI.UI.PatrolCompetitionEvidenceState Evidence => TestObjects.Get<PatrolDemoScenarioRunner>().Evidence;
    private static HashSet<string> Finish(PatrolAutoPatrolDemoController auto) {
        var states = new HashSet<string>();
        for (int i = 0; i < 200 && TestObjects.Pending.Count > 0; i++) { states.Add(auto.State); TestObjects.Tick(); }
        states.Add(auto.State); PatrolUIStateRegression.Check(TestObjects.Pending.Count == 0, "Unbounded AUTO fixture"); return states;
    }
    public static void RunAll()
    {
        passed = 0;
        var c = Setup("DELAYED_STOP"); Evidence.Apply(null, PatrolUIStateRegression.Result()); c.StartPatrol();
        Check(c.IsRunning && c.State == "STARTING", "starts");
        Check(Evidence.RuntimeStatus == "NOT VERIFIED", "clears stale REAL before movement");
        var routine = c.Routine; c.StartPatrol(); Check(ReferenceEquals(routine, c.Routine), "duplicate trigger ignored");
        var states = Finish(c);
        Check(states.Contains("MOVING") && TestObjects.Get<VGMobileInspectorMovement>().Moves == 2, "existing movement entry points run");
        Check(states.Contains("OBSERVING") && states.Contains("REASONING") && states.Contains("ACTING") && c.ReasoningInvocations == 1, "observation, reasoning and action execute");
        Check(c.State == "COMPLETE" && Evidence.FinalState == "STOPPED" && !c.IsRunning, "final action and lock release");
        c.StartPatrol(); Finish(c); Check(c.State == "COMPLETE" && c.ReasoningInvocations == 1, "repeatable next patrol");
        var hc = PatrolAutoPatrolDemoController.CreateObservation();
        Check(hc.observationId == "heldout-HC09" && hc.source.Contains("synthetic")
            && RuleBasedSafetyBaselineV2.Decide(hc).action == "CONTINUE", "frozen HC09 and rule comparison preserved");
        var raw = PatrolUIStateRegression.Result();
        Check(PatrolAutoPatrolDemoController.Bound(raw, hc, 1, out bool escalated).action == "STOP" && escalated, "budget exhausted -> STOP / HUMAN REVIEW");
        c = Setup("FAILED"); c.StartPatrol(); Finish(c);
        Check(c.State == "FAILED" && Evidence.RuntimeStatus == "FAILED" && Evidence.FinalState == "STOPPED", "failed inference is visible and stops safely");
        c = Setup("HANG"); c.StartPatrol(); Finish(c);
        Check(c.State == "FAILED" && c.ReasoningInvocations == 1 && !c.IsRunning, "timeout bounded without retry");
        c = Setup("REOBSERVE"); c.StartPatrol(); Finish(c);
        Check(c.State == "FAILED" && c.ReasoningInvocations == 1 && Evidence.FinalState == "STOPPED", "missing fresh evidence -> bounded STOP");
        c = Setup("REOBSERVE"); PatrolReobservationEvidenceReader.Fresh = true; c.StartPatrol(); Finish(c);
        Check(c.State == "COMPLETE" && c.ReasoningInvocations == 2 && Evidence.FinalState == "STOPPED", "fresh evidence -> second reasoning -> bounded STOP");
        var pending = new PatrolAI.UI.PatrolCompetitionEvidenceState(); pending.Apply(hc, raw); pending.BeginSecondReasoning();
        Check(pending.RuntimeStatus == "NOT VERIFIED" && pending.FirstDecision == "UNKNOWN / REOBSERVE"
            && pending.PatrolAction == "NOT EXECUTED", "pending second reasoning truthfulness");
        c = Setup("STOP"); TestObjects.Items.Remove(typeof(VGMobileInspectorMovement)); c.StartPatrol(); Finish(c);
        Check(c.State == "FAILED" && c.ReasoningInvocations == 0, "missing movement fails before reasoning");
        c = Setup("STOP"); TestObjects.Get<VGPhase3AgentController>().NextRun = "RUNNING";
        TestObjects.Get<PatrolBaselineActionAdapter>().RunAgenticInspection(); c.StartPatrol();
        Check(!c.IsRunning && c.ControlsBusy, "active baseline workflow blocks competing flow");
        Console.WriteLine("AUTO PATROL SUMMARY: " + passed + "/16 PASS; OFFLINE FIXTURES ONLY; HTTP REQUESTS=0");
    }
}
