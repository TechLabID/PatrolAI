using System.Collections.Generic;
using PatrolAI.Competition;
using PatrolAI.Scenarios;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PatrolAI.UI
{
    /// <summary>
    /// Additive 1920x1080-friendly IMGUI evidence and action panel.
    /// The object is created at runtime so the protected scene asset is not edited.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PatrolCompetitionEvidenceView : MonoBehaviour
    {
        private const string CompetitionScene = "VISIONGUARD_IndustrialRoom_Phase5_v01";
        private PatrolDemoScenarioRunner scenarioRunner;
        private PatrolBaselineActionAdapter baselineAdapter;
        private readonly PatrolBaselineWorkflowDetailState workflowDetail =
            new PatrolBaselineWorkflowDetailState();
        private GUIStyle labelStyle;
        private GUIStyle headingStyle;
        private GUIStyle smallLabelStyle;
        private GUIStyle valueStyle;
        private GUIStyle brandStyle;
        private GUIStyle panelStyle;
        private GUIStyle headerStyle;
        private GUIStyle neutralButtonStyle;
        private GUIStyle selectedButtonStyle;
        private GUIStyle safeButtonStyle;
        private GUIStyle reobserveButtonStyle;
        private GUIStyle hazardButtonStyle;
        private readonly List<Texture2D> generatedTextures = new List<Texture2D>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AddCompetitionPanel()
        {
            if (SceneManager.GetActiveScene().name != CompetitionScene
                || FindAnyObjectByType<PatrolCompetitionEvidenceView>() != null)
            {
                return;
            }

            var panel = new GameObject("PatrolAI_Competition_Demo");
            panel.AddComponent<PatrolBaselineActionAdapter>();
            panel.AddComponent<PatrolDemoScenarioRunner>();
            panel.AddComponent<PatrolCompetitionEvidenceView>();
        }

        private void Awake()
        {
            scenarioRunner = GetComponent<PatrolDemoScenarioRunner>();
            baselineAdapter = GetComponent<PatrolBaselineActionAdapter>();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || scenarioRunner == null || baselineAdapter == null)
            {
                return;
            }

            EnsureStyles();
            Matrix4x4 originalMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            float offsetX = (Screen.width - 1920f * scale) * 0.5f;
            float offsetY = (Screen.height - 1080f * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(offsetX, offsetY, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

            DrawHeader(scenarioRunner.Evidence);
            DrawScenarioMenu();
            DrawBaselineMenu();
            DrawWorkflowDetailPanel();
            DrawEvidencePanel(scenarioRunner.Evidence);
            DrawExitButton();
            GUI.matrix = originalMatrix;
        }

        private void DrawExitButton()
        {
            if (GUI.Button(new Rect(1662f, 1014f, 240f, 48f),
                "EXIT PATROLAI", neutralButtonStyle))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        private void DrawHeader(PatrolCompetitionEvidenceState evidence)
        {
            var rect = new Rect(18f, 18f, 1884f, 72f);
            GUI.Box(rect, string.Empty, headerStyle);
            FillRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f),
                new Color(0.02f, 0.5f, 0.95f, 0.95f));

            GUI.Label(new Rect(38f, 25f, 250f, 34f), "PatrolAI", brandStyle);
            GUI.Label(new Rect(40f, 58f, 255f, 18f),
                "AUTONOMOUS INSPECTION & SAFETY MONITORING", smallLabelStyle);
            GUI.Label(new Rect(318f, 29f, 700f, 24f),
                "A  SAFE → CONTINUE    |    B  REOBSERVE → AGENTIC    |    C  HAZARD → STOP",
                labelStyle);
            GUI.Label(new Rect(318f, 57f, 700f, 18f),
                "SENSE  →  REASON  →  DECIDE  →  ACT", smallLabelStyle);

            Color runtimeColor = RuntimeColor(evidence.RuntimeStatus);
            FillRect(new Rect(1192f, 30f, 160f, 42f),
                new Color(runtimeColor.r, runtimeColor.g, runtimeColor.b, 0.3f));
            DrawColoredLabel(new Rect(1206f, 38f, 138f, 26f),
                "RUNTIME: " + evidence.RuntimeStatus, runtimeColor, headingStyle);
            GUI.Label(new Rect(1372f, 28f, 500f, 22f),
                "PROVIDER: " + evidence.Provider, labelStyle);
            GUI.Label(new Rect(1372f, 53f, 500f, 22f),
                "MODEL: " + evidence.Model, labelStyle);
        }

        private void DrawEvidencePanel(PatrolCompetitionEvidenceState evidence)
        {
            var rect = new Rect(306f, 862f, 1298f, 200f);
            GUI.Box(rect, string.Empty, panelStyle);
            FillRect(new Rect(rect.x, rect.y, rect.width, 4f),
                new Color(0.02f, 0.55f, 1f, 1f));
            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, 25f),
                "COMPETITION EVIDENCE — LATEST RESULT", headingStyle);

            float tileTop = rect.y + 44f;
            float tileHeight = 82f;
            float x = rect.x + 16f;
            DrawEvidenceTile(ref x, tileTop, 142f, tileHeight,
                "RUNTIME", evidence.RuntimeStatus, RuntimeColor(evidence.RuntimeStatus));
            DrawEvidenceTile(ref x, tileTop, 184f, tileHeight,
                "PROVIDER", evidence.Provider, new Color(0.15f, 0.8f, 1f));
            DrawEvidenceTile(ref x, tileTop, 270f, tileHeight,
                "MODEL", evidence.Model, Color.white);
            DrawEvidenceTile(ref x, tileTop, 180f, tileHeight,
                "PATROL DECISION", evidence.PatrolDecision,
                DecisionColor(evidence.PatrolDecision));
            DrawEvidenceTile(ref x, tileTop, 142f, tileHeight,
                "PATROL ACTION", evidence.PatrolAction,
                DecisionColor(evidence.PatrolAction));
            DrawEvidenceTile(ref x, tileTop, 142f, tileHeight,
                "FINAL STATE", evidence.FinalState,
                DecisionColor(evidence.FinalState));
            DrawEvidenceTile(ref x, tileTop, 142f, tileHeight,
                "LATENCY", evidence.Latency, Color.white);

            GUI.Label(new Rect(rect.x + 18f, rect.y + 137f, rect.width - 36f, 22f),
                "OBSERVATION: " + evidence.Observation
                + "    |    EVIDENCE: " + evidence.Evidence
                + "    |    CONFIDENCE: " + evidence.PerceptionConfidence,
                labelStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 166f, rect.width - 36f, 22f),
                evidence.Detail, smallLabelStyle);
        }

        private void DrawBaselineMenu()
        {
            var rect = new Rect(1248f, 108f, 654f, 98f);
            GUI.Box(rect, string.Empty, panelStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 22f),
                "BASELINE WORKFLOW — SELECT ONE", headingStyle);

            const float buttonWidth = 199f;
            const float gap = 10f;
            float left = rect.x + 14f;
            float top = rect.y + 35f;
            GUIStyle agenticStyle = IsSelected(PatrolBaselineAction.AgenticInspection)
                ? selectedButtonStyle : neutralButtonStyle;
            GUIStyle fusionStyle = IsSelected(PatrolBaselineAction.MultiSourceFusion)
                ? selectedButtonStyle : neutralButtonStyle;
            GUIStyle temporalStyle = IsSelected(PatrolBaselineAction.LatestVerifiedFusion)
                ? selectedButtonStyle : neutralButtonStyle;

            if (GUI.Button(new Rect(left, top, buttonWidth, 38f),
                "AGENTIC INSPECTION", agenticStyle))
                baselineAdapter.RunAgenticInspection();
            if (GUI.Button(new Rect(left + buttonWidth + gap, top, buttonWidth, 38f),
                "MULTI SOURCE FUSION", fusionStyle))
                baselineAdapter.RunMultiSourceFusion();
            if (GUI.Button(new Rect(left + (buttonWidth + gap) * 2f, top,
                buttonWidth, 38f), "LATEST VERIFIED FUSION", temporalStyle))
                baselineAdapter.EvaluateLatestVerifiedFusion();

            GUI.Label(new Rect(left, rect.y + 76f, buttonWidth, 16f),
                baselineAdapter.AgenticStatus, smallLabelStyle);
            GUI.Label(new Rect(left + buttonWidth + gap, rect.y + 76f, buttonWidth, 16f),
                baselineAdapter.FusionStatus, smallLabelStyle);
            GUI.Label(new Rect(left + (buttonWidth + gap) * 2f, rect.y + 76f,
                buttonWidth, 16f), baselineAdapter.TemporalStatus, smallLabelStyle);
        }

        private void DrawScenarioMenu()
        {
            var rect = new Rect(18f, 108f, 270f, 410f);
            GUI.Box(rect, string.Empty, panelStyle);
            FillRect(new Rect(rect.x, rect.y, 4f, rect.height),
                new Color(1f, 0.72f, 0.05f, 1f));
            GUI.Label(new Rect(rect.x + 16f, rect.y + 14f, rect.width - 32f, 26f),
                "DEMO SCENARIOS", headingStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 42f, rect.width - 32f, 18f),
                "SELECT A VALIDATED LIVE FLOW", smallLabelStyle);

            float buttonLeft = rect.x + 16f;
            float buttonWidth = rect.width - 32f;
            GUI.enabled = !scenarioRunner.IsRunning;
            if (GUI.Button(new Rect(buttonLeft, rect.y + 70f, buttonWidth, 94f),
                "A — SAFE\n→ CONTINUE", safeButtonStyle))
                scenarioRunner.RunSafeScenario();
            if (GUI.Button(new Rect(buttonLeft, rect.y + 176f, buttonWidth, 94f),
                "B — REOBSERVE\n→ AGENTIC RE-OBSERVATION", reobserveButtonStyle))
                scenarioRunner.RunInsufficientEvidenceScenario();
            if (GUI.Button(new Rect(buttonLeft, rect.y + 282f, buttonWidth, 94f),
                "C — HAZARD\n→ STOP", hazardButtonStyle))
                scenarioRunner.RunHazardScenario();
            GUI.enabled = true;
        }

        private void DrawWorkflowDetailPanel()
        {
            var rect = new Rect(1248f, 218f, 654f, 400f);
            GUI.Box(rect, string.Empty, panelStyle);
            FillRect(new Rect(rect.x, rect.y, 4f, rect.height),
                WorkflowAccentColor());

            if (!baselineAdapter.HasActiveAction)
            {
                GUI.Label(new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 30f),
                    "BASELINE WORKFLOW DETAIL", headingStyle);
                GUI.Label(new Rect(rect.x + 20f, rect.y + 62f, rect.width - 40f, 24f),
                    "Select one workflow above to inspect its live controller state.",
                    labelStyle);
                return;
            }

            if (!workflowDetail.HasSelection
                || workflowDetail.ActiveAction != baselineAdapter.ActiveAction)
                workflowDetail.Select(baselineAdapter.ActiveAction);

            GUI.Label(new Rect(rect.x + 20f, rect.y + 16f, 430f, 30f),
                workflowDetail.Title, headingStyle);
            string displayState = ActiveWorkflowDisplayState();
            Color stateColor = DecisionColor(displayState);
            FillRect(new Rect(rect.xMax - 176f, rect.y + 13f, 154f, 34f),
                new Color(stateColor.r, stateColor.g, stateColor.b, 0.25f));
            DrawColoredLabel(new Rect(rect.xMax - 166f, rect.y + 18f, 136f, 24f),
                displayState, stateColor, headingStyle);

            string[] lines = ActiveWorkflowLines();
            for (int index = 0; index < lines.Length; index++)
                DrawDetailRow(rect, index, lines[index]);
        }

        private string[] ActiveWorkflowLines()
        {
            switch (workflowDetail.ActiveAction)
            {
                case PatrolBaselineAction.AgenticInspection:
                    return new[]
                    {
                        "STATE: " + baselineAdapter.AgenticDisplayState,
                        "ACTIVE PHASE: " + baselineAdapter.AgenticControllerState,
                        "WORKFLOW RESULT: " + baselineAdapter.AgenticStatus
                    };
                case PatrolBaselineAction.MultiSourceFusion:
                    return new[]
                    {
                        "STATE: " + baselineAdapter.FusionDisplayState,
                        "ACTIVE PHASE: " + baselineAdapter.FusionControllerState,
                        "FUSION STATUS: " + baselineAdapter.FusionResultStatus,
                        "VERIFICATION: " + baselineAdapter.FusionVerification,
                        "ACTION: " + baselineAdapter.FusionAction,
                        "FINAL CONDITION: " + baselineAdapter.FusionFinalCondition,
                        "REASON: " + baselineAdapter.FusionReason,
                        "RESULT: " + baselineAdapter.FusionResult
                    };
                case PatrolBaselineAction.LatestVerifiedFusion:
                    return new[]
                    {
                        "STATE: " + baselineAdapter.TemporalDisplayState,
                        "ACTIVE PHASE: " + baselineAdapter.TemporalControllerState,
                        "TEMPORAL STATUS: " + baselineAdapter.TemporalOutcome,
                        "PREVIOUS CONDITION: " + baselineAdapter.TemporalPreviousCondition,
                        "LATEST CONDITION: " + baselineAdapter.TemporalCurrentCondition,
                        "EVALUATION RUN: " + baselineAdapter.TemporalEvaluationRun,
                        "CHANGE ID: " + baselineAdapter.TemporalChangeId,
                        "INCIDENT ID: " + baselineAdapter.TemporalIncidentId,
                        "REASON: " + baselineAdapter.TemporalReason,
                        "RESULT: " + baselineAdapter.TemporalResult
                    };
                default:
                    return new string[0];
            }
        }

        private string ActiveWorkflowDisplayState()
        {
            switch (workflowDetail.ActiveAction)
            {
                case PatrolBaselineAction.AgenticInspection:
                    return baselineAdapter.AgenticDisplayState;
                case PatrolBaselineAction.MultiSourceFusion:
                    return baselineAdapter.FusionDisplayState;
                case PatrolBaselineAction.LatestVerifiedFusion:
                    return baselineAdapter.TemporalDisplayState;
                default:
                    return "IDLE";
            }
        }

        private void DrawDetailRow(Rect panel, int index, string line)
        {
            int separator = line.IndexOf(':');
            string key = separator < 0 ? line : line.Substring(0, separator);
            string value = separator < 0 ? string.Empty : line.Substring(separator + 1).Trim();
            float top = panel.y + 62f + index * 29f;
            FillRect(new Rect(panel.x + 18f, top + 26f, panel.width - 36f, 1f),
                new Color(0.2f, 0.45f, 0.65f, 0.35f));
            GUI.Label(new Rect(panel.x + 22f, top, 190f, 24f), key, smallLabelStyle);
            GUI.Label(new Rect(panel.x + 220f, top, panel.width - 244f, 24f),
                value, labelStyle);
        }

        private void DrawEvidenceTile(
            ref float left,
            float top,
            float width,
            float height,
            string label,
            string value,
            Color color)
        {
            FillRect(new Rect(left, top, width, height),
                new Color(0.02f, 0.12f, 0.21f, 0.94f));
            FillRect(new Rect(left, top, 3f, height),
                new Color(color.r, color.g, color.b, 0.9f));
            GUI.Label(new Rect(left + 12f, top + 10f, width - 20f, 18f),
                label, smallLabelStyle);
            DrawColoredLabel(new Rect(left + 12f, top + 34f, width - 20f, 38f),
                value, color, valueStyle);
            left += width + 8f;
        }

        private bool IsSelected(PatrolBaselineAction action)
        {
            return baselineAdapter.HasActiveAction && baselineAdapter.ActiveAction == action;
        }

        private Color WorkflowAccentColor()
        {
            if (!baselineAdapter.HasActiveAction)
                return new Color(0.2f, 0.55f, 0.9f, 1f);
            switch (baselineAdapter.ActiveAction)
            {
                case PatrolBaselineAction.AgenticInspection:
                    return new Color(0.72f, 0.25f, 1f, 1f);
                case PatrolBaselineAction.MultiSourceFusion:
                    return new Color(0.05f, 0.65f, 1f, 1f);
                case PatrolBaselineAction.LatestVerifiedFusion:
                    return new Color(1f, 0.63f, 0.05f, 1f);
                default:
                    return Color.white;
            }
        }

        private static Color RuntimeColor(string status)
        {
            switch ((status ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "REAL":
                    return new Color(0.1f, 1f, 0.55f, 1f);
                case "MOCK":
                case "LOCAL SAMPLE":
                    return new Color(0.2f, 0.72f, 1f, 1f);
                case "FALLBACK":
                    return new Color(1f, 0.68f, 0.1f, 1f);
                case "FAILED":
                    return new Color(1f, 0.25f, 0.3f, 1f);
                default:
                    return new Color(0.7f, 0.75f, 0.8f, 1f);
            }
        }

        private static Color DecisionColor(string value)
        {
            string normalized = (value ?? string.Empty).ToUpperInvariant();
            if (normalized.Contains("FAILED") || normalized.Contains("DANGER")
                || normalized.Contains("STOP"))
                return new Color(1f, 0.3f, 0.34f, 1f);
            if (normalized.Contains("REOBSERV") || normalized.Contains("FALLBACK"))
                return new Color(0.2f, 0.72f, 1f, 1f);
            if (normalized.Contains("REAL") || normalized.Contains("SAFE")
                || normalized.Contains("CONTINUE") || normalized.Contains("COMPLETE")
                || normalized.Contains("VERIFIED"))
                return new Color(0.12f, 1f, 0.55f, 1f);
            if (normalized.Contains("RUNNING"))
                return new Color(0.2f, 0.72f, 1f, 1f);
            return Color.white;
        }

        private static void FillRect(Rect rect, Color color)
        {
            Color original = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = original;
        }

        private static void DrawColoredLabel(
            Rect rect,
            string text,
            Color color,
            GUIStyle style)
        {
            Color original = style.normal.textColor;
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
            style.normal.textColor = original;
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
                return;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                normal = { textColor = Color.white },
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft
            };
            headingStyle = new GUIStyle(labelStyle)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            smallLabelStyle = new GUIStyle(labelStyle)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.68f, 0.79f, 0.9f, 1f) }
            };
            valueStyle = new GUIStyle(labelStyle)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            brandStyle = new GUIStyle(headingStyle)
            {
                fontSize = 30,
                normal = { textColor = new Color(0.75f, 0.84f, 1f, 1f) }
            };

            panelStyle = BoxStyle(new Color(0.008f, 0.045f, 0.085f, 0.94f));
            headerStyle = BoxStyle(new Color(0.008f, 0.04f, 0.075f, 0.97f));
            neutralButtonStyle = ButtonStyle(new Color(0.035f, 0.13f, 0.23f, 0.97f));
            selectedButtonStyle = ButtonStyle(new Color(0.05f, 0.38f, 0.62f, 1f));
            safeButtonStyle = ButtonStyle(new Color(0.02f, 0.38f, 0.23f, 0.96f));
            reobserveButtonStyle = ButtonStyle(new Color(0.02f, 0.28f, 0.62f, 0.96f));
            hazardButtonStyle = ButtonStyle(new Color(0.5f, 0.08f, 0.14f, 0.96f));
        }

        private GUIStyle BoxStyle(Color color)
        {
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = MakeTexture(color);
            return style;
        }

        private GUIStyle ButtonStyle(Color color)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white },
                focused = { textColor = Color.white }
            };
            Texture2D normal = MakeTexture(color);
            Texture2D hover = MakeTexture(new Color(
                Mathf.Min(1f, color.r + 0.08f),
                Mathf.Min(1f, color.g + 0.08f),
                Mathf.Min(1f, color.b + 0.08f),
                color.a));
            style.normal.background = normal;
            style.hover.background = hover;
            style.active.background = hover;
            style.focused.background = normal;
            return style;
        }

        private Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            generatedTextures.Add(texture);
            return texture;
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in generatedTextures)
            {
                if (texture != null)
                    Destroy(texture);
            }
            generatedTextures.Clear();
        }
    }
}
