using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VisionGuard.Phase3;
using VisionGuard.Phase4;
using VisionGuard.Phase5;

namespace PatrolAI.Competition
{
    public enum PatrolBaselineAction
    {
        AgenticInspection,
        MultiSourceFusion,
        LatestVerifiedFusion
    }

    /// <summary>
    /// Delegates UI actions to the three inherited public controller entry points.
    /// It contains no inspection, fusion, or temporal evaluation algorithm.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PatrolBaselineActionAdapter : MonoBehaviour
    {
        [SerializeField] private VGPhase3AgentController agenticController;
        [SerializeField] private VGPhase4FusionController fusionController;
        [SerializeField] private VGPhase5TemporalController temporalController;

        private string agenticStatus = "IDLE", fusionStatus = "IDLE", temporalStatus = "IDLE";
        private int workflowRevision;
        public string AgenticStatus {
            get => CurrentStatus(agenticController != null && agenticController.IsRunning,
                RawAgenticState, agenticStatus);
            private set => agenticStatus = value;
        }
        public string FusionStatus {
            get => CurrentStatus(fusionController != null && fusionController.IsRunning,
                RawFusionState, fusionStatus);
            private set => fusionStatus = value;
        }
        public string TemporalStatus {
            get => CurrentStatus(temporalController != null && temporalController.IsRunning,
                RawTemporalState, temporalStatus);
            private set => temporalStatus = value;
        }
        // Physical work can still be settling after a presentation timeout or ownership reset.
        public bool IsWorkflowRunning => (agenticController != null && agenticController.IsRunning)
            || (fusionController != null && fusionController.IsRunning)
            || (temporalController != null && temporalController.IsRunning);
        public bool HasActiveAction { get; private set; }
        public PatrolBaselineAction ActiveAction { get; private set; }

        public string AgenticDisplayState => DisplayState(
            agenticController != null && agenticController.IsRunning,
            RawAgenticState, agenticStatus);
        private string RawAgenticState => ValueOrUnavailable(
            agenticController == null ? null : agenticController.State);
        public string AgenticControllerState => DisplayPhase(
            agenticController != null && agenticController.IsRunning, RawAgenticState, agenticStatus);

        public string FusionDisplayState => DisplayState(
            fusionController != null && fusionController.IsRunning,
            RawFusionState, fusionStatus);
        private string RawFusionState => ValueOrUnavailable(
            fusionController == null ? null : fusionController.State);
        public string FusionControllerState => DisplayPhase(
            fusionController != null && fusionController.IsRunning, RawFusionState, fusionStatus);
        public string FusionResultStatus => ValueOrUnavailable(
            fusionController == null ? null : fusionController.FusionStatus);
        public string FusionVerification => ValueOrUnavailable(
            fusionController == null ? null : fusionController.Verification);
        public string FusionAction => ValueOrUnavailable(
            fusionController == null ? null : fusionController.Action);
        public string FusionFinalCondition => ValueOrUnavailable(
            fusionController == null ? null : fusionController.FinalCondition);
        public string FusionReason => ValueOrUnavailable(
            fusionController == null ? null : fusionController.Reason);
        public string FusionResult => ShortResult(
            fusionController == null ? null : fusionController.ResultPath);

        public string TemporalDisplayState => DisplayState(
            temporalController != null && temporalController.IsRunning,
            RawTemporalState, temporalStatus);
        private string RawTemporalState => ValueOrUnavailable(
            temporalController == null ? null : temporalController.State);
        public string TemporalControllerState => DisplayPhase(
            temporalController != null && temporalController.IsRunning, RawTemporalState, temporalStatus);
        public string TemporalOutcome => ValueOrUnavailable(
            temporalController == null ? null : temporalController.Outcome);
        public string TemporalPreviousCondition => ValueOrUnavailable(
            temporalController == null ? null : temporalController.PreviousCondition);
        public string TemporalCurrentCondition => ValueOrUnavailable(
            temporalController == null ? null : temporalController.CurrentCondition);
        public string TemporalEvaluationRun => temporalController == null
            ? "UNAVAILABLE"
            : temporalController.EvaluationRun.ToString();
        public string TemporalChangeId => ValueOrUnavailable(
            temporalController == null ? null : temporalController.ChangeId);
        public string TemporalIncidentId => ValueOrUnavailable(
            temporalController == null ? null : temporalController.IncidentId);
        public string TemporalReason => ValueOrUnavailable(
            temporalController == null ? null : temporalController.Reason);
        public string TemporalResult => ShortResult(
            temporalController == null ? null : temporalController.ResultPath);

        public bool ResolveControllers()
        {
            if (agenticController == null)
                agenticController = FindAnyObjectByType<VGPhase3AgentController>();
            if (fusionController == null)
                fusionController = FindAnyObjectByType<VGPhase4FusionController>();
            if (temporalController == null)
                temporalController = FindAnyObjectByType<VGPhase5TemporalController>();
            return agenticController != null && fusionController != null
                && temporalController != null;
        }

        public void RunAgenticInspection()
        {
            Select(PatrolBaselineAction.AgenticInspection);
            ResolveControllers();
            if (agenticController == null)
            {
                AgenticStatus = "FAILED — CONTROLLER NOT FOUND";
                return;
            }

            AgenticStatus = "RUNNING";
            Dispatch(PatrolBaselineAction.AgenticInspection,
                agenticController.RunAgenticInspection,
                NoOp,
                NoOp);
            StartCoroutine(ObserveAgenticCompletion(workflowRevision));
        }

        public void RunMultiSourceFusion()
        {
            Select(PatrolBaselineAction.MultiSourceFusion);
            ResolveControllers();
            if (fusionController == null)
            {
                FusionStatus = "FAILED — CONTROLLER NOT FOUND";
                return;
            }

            FusionStatus = "RUNNING";
            Dispatch(PatrolBaselineAction.MultiSourceFusion,
                NoOp,
                fusionController.RunMultiSourceFusion,
                NoOp);
            StartCoroutine(ObserveFusionCompletion(workflowRevision));
        }

        public void EvaluateLatestVerifiedFusion()
        {
            Select(PatrolBaselineAction.LatestVerifiedFusion);
            ResolveControllers();
            if (temporalController == null)
            {
                TemporalStatus = "FAILED — CONTROLLER NOT FOUND";
                return;
            }

            TemporalStatus = "RUNNING";
            Dispatch(PatrolBaselineAction.LatestVerifiedFusion,
                NoOp,
                NoOp,
                temporalController.EvaluateLatestFusion);
            TemporalStatus = temporalController.State == "COMPLETE"
                ? "VERIFIED"
                : "FAILED — " + temporalController.State;
        }

        public static void Dispatch(
            PatrolBaselineAction action,
            Action runAgentic,
            Action runFusion,
            Action evaluateTemporal)
        {
            switch (action)
            {
                case PatrolBaselineAction.AgenticInspection:
                    runAgentic();
                    break;
                case PatrolBaselineAction.MultiSourceFusion:
                    runFusion();
                    break;
                case PatrolBaselineAction.LatestVerifiedFusion:
                    evaluateTemporal();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        public static string DisplayState(
            bool isRunning,
            string controllerState,
            string workflowStatus)
        {
            if ((workflowStatus ?? string.Empty).StartsWith(
                "FAILED", StringComparison.Ordinal))
                return "FAILED";
            if (workflowStatus == "IDLE") return "IDLE";
            if (isRunning) return "RUNNING";
            // A terminal controller takes precedence over the observer's cached RUNNING value.
            return ValueOrUnavailable(controllerState);
        }

        public static string DisplayPhase(bool isRunning, string controllerState, string workflowStatus)
        {
            string state = DisplayState(isRunning, controllerState, workflowStatus);
            if (state != "RUNNING") return state;
            // During startup, the inherited controller can retain its previous terminal phase
            // while returning home. It is not the active phase of this new execution.
            return controllerState == "FAILED" || controllerState == "COMPLETE"
                || controllerState == "IDLE" || controllerState == "UNAVAILABLE"
                ? "RUNNING" : ValueOrUnavailable(controllerState);
        }

        private static string CurrentStatus(bool isRunning, string controllerState, string workflowStatus)
        {
            string state = DisplayState(isRunning, controllerState, workflowStatus);
            if (state == "FAILED") return (workflowStatus ?? "").StartsWith("FAILED", StringComparison.Ordinal)
                ? workflowStatus : "FAILED — " + controllerState;
            return state == "COMPLETE" && workflowStatus == "VERIFIED" ? "VERIFIED" : state;
        }

        public void ClearWorkflowPresentation()
        {
            workflowRevision++;
            HasActiveAction = false;
            agenticStatus = fusionStatus = temporalStatus = "IDLE";
        }

        private IEnumerator ObserveAgenticCompletion(int revision)
        {
            yield return null;
            while (revision == workflowRevision && agenticController != null && agenticController.IsRunning)
                yield return null;
            if (revision != workflowRevision) yield break;
            AgenticStatus = agenticController != null && agenticController.State == "COMPLETE"
                ? "COMPLETE"
                : "FAILED — " + (agenticController == null ? "CONTROLLER LOST" : agenticController.State);
        }

        private IEnumerator ObserveFusionCompletion(int revision)
        {
            yield return null;
            while (revision == workflowRevision && fusionController != null && fusionController.IsRunning)
                yield return null;
            if (revision != workflowRevision) yield break;
            FusionStatus = fusionController != null && fusionController.State == "COMPLETE"
                ? "COMPLETE"
                : "FAILED — " + (fusionController == null ? "CONTROLLER LOST" : fusionController.State);
        }

        private static void NoOp()
        {
        }

        private void Select(PatrolBaselineAction action)
        {
            workflowRevision++;
            ActiveAction = action;
            HasActiveAction = true;
        }

        private static string ValueOrUnavailable(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value == "—"
                ? "UNAVAILABLE"
                : value;
        }

        private static string ShortResult(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "—")
                return "UNAVAILABLE";
            string fileName = Path.GetFileName(path);
            return string.IsNullOrWhiteSpace(fileName) ? "AVAILABLE" : fileName;
        }
    }
}
