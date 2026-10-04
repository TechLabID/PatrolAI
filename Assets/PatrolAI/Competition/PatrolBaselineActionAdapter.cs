using System;
using System.Collections;
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

        public string AgenticStatus { get; private set; } = "IDLE";
        public string FusionStatus { get; private set; } = "IDLE";
        public string TemporalStatus { get; private set; } = "IDLE";

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
            StartCoroutine(ObserveAgenticCompletion());
        }

        public void RunMultiSourceFusion()
        {
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
            StartCoroutine(ObserveFusionCompletion());
        }

        public void EvaluateLatestVerifiedFusion()
        {
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

        private IEnumerator ObserveAgenticCompletion()
        {
            yield return null;
            while (agenticController != null && agenticController.IsRunning)
                yield return null;
            AgenticStatus = agenticController != null && agenticController.State == "COMPLETE"
                ? "COMPLETE"
                : "FAILED — " + (agenticController == null ? "CONTROLLER LOST" : agenticController.State);
        }

        private IEnumerator ObserveFusionCompletion()
        {
            yield return null;
            while (fusionController != null && fusionController.IsRunning)
                yield return null;
            FusionStatus = fusionController != null && fusionController.State == "COMPLETE"
                ? "COMPLETE"
                : "FAILED — " + (fusionController == null ? "CONTROLLER LOST" : fusionController.State);
        }

        private static void NoOp()
        {
        }
    }
}
