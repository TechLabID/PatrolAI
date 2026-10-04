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
        private GUIStyle labelStyle;
        private GUIStyle headingStyle;

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
            DrawEvidencePanel(scenarioRunner.Evidence);
            DrawBaselineMenu();
            DrawScenarioMenu();
        }

        private void DrawEvidencePanel(PatrolCompetitionEvidenceState evidence)
        {
            float width = Mathf.Min(620f, Screen.width * 0.42f);
            float left = Screen.width - width - 18f;
            GUI.Box(new Rect(left, 18f, width, 330f), string.Empty);
            GUI.Label(new Rect(left + 18f, 30f, width - 36f, 28f),
                "PATROLAI — COMPETITION EVIDENCE", headingStyle);

            string[] lines =
            {
                "RUNTIME: " + evidence.RuntimeStatus,
                "PROVIDER: " + evidence.Provider,
                "MODEL: " + evidence.Model,
                "OBSERVATION: " + evidence.Observation,
                "EVIDENCE: " + evidence.Evidence,
                "PERCEPTION CONFIDENCE: " + evidence.PerceptionConfidence,
                "PATROL DECISION: " + evidence.PatrolDecision,
                "PATROL ACTION: " + evidence.PatrolAction,
                "FINAL STATE: " + evidence.FinalState,
                "LATENCY: " + evidence.Latency
            };

            for (int index = 0; index < lines.Length; index++)
            {
                GUI.Label(new Rect(left + 18f, 64f + index * 24f, width - 36f, 22f),
                    lines[index], labelStyle);
            }
            GUI.Label(new Rect(left + 18f, 304f, width - 36f, 22f),
                evidence.Detail, labelStyle);
        }

        private void DrawBaselineMenu()
        {
            const float left = 18f;
            float top = Screen.height - 224f;
            const float width = 430f;
            GUI.Box(new Rect(left, top, width, 206f), string.Empty);
            GUI.Label(new Rect(left + 16f, top + 12f, width - 32f, 26f),
                "VALIDATED BASELINE ACTIONS", headingStyle);

            if (GUI.Button(new Rect(left + 16f, top + 44f, width - 32f, 32f),
                "RUN AGENTIC INSPECTION"))
                baselineAdapter.RunAgenticInspection();
            GUI.Label(new Rect(left + 16f, top + 76f, width - 32f, 20f),
                "AGENTIC INSPECTION: " + baselineAdapter.AgenticStatus, labelStyle);

            if (GUI.Button(new Rect(left + 16f, top + 100f, width - 32f, 32f),
                "RUN MULTI SOURCE FUSION"))
                baselineAdapter.RunMultiSourceFusion();
            GUI.Label(new Rect(left + 16f, top + 132f, width - 32f, 20f),
                "MULTI SOURCE FUSION: " + baselineAdapter.FusionStatus, labelStyle);

            if (GUI.Button(new Rect(left + 16f, top + 156f, width - 32f, 32f),
                "EVALUATE LATEST VERIFIED FUSION"))
                baselineAdapter.EvaluateLatestVerifiedFusion();
            GUI.Label(new Rect(left + 16f, top + 188f, width - 32f, 20f),
                "LATEST VERIFIED FUSION: " + baselineAdapter.TemporalStatus, labelStyle);
        }

        private void DrawScenarioMenu()
        {
            const float width = 500f;
            float left = Screen.width - width - 18f;
            float top = 366f;
            GUI.Box(new Rect(left, top, width, 96f), string.Empty);
            GUI.Label(new Rect(left + 16f, top + 10f, width - 32f, 26f),
                "PATROLAI LIVE DEMO SCENARIOS", headingStyle);

            GUI.enabled = !scenarioRunner.IsRunning;
            if (GUI.Button(new Rect(left + 16f, top + 46f, 145f, 34f), "A — SAFE"))
                scenarioRunner.RunSafeScenario();
            if (GUI.Button(new Rect(left + 177f, top + 46f, 145f, 34f), "B — REOBSERVE"))
                scenarioRunner.RunInsufficientEvidenceScenario();
            if (GUI.Button(new Rect(left + 338f, top + 46f, 145f, 34f), "C — HAZARD"))
                scenarioRunner.RunHazardScenario();
            GUI.enabled = true;
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white },
                clipping = TextClipping.Clip
            };
            headingStyle = new GUIStyle(labelStyle)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
