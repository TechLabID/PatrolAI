using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace PatrolAI.Editor
{
    public static class PatrolAIWindowsBuild
    {
        private const string CompetitionScene =
            "Assets/VisionGuard/Phase5/Scenes/VISIONGUARD_IndustrialRoom_Phase5_v01.unity";
        private const string OutputExecutable = "Builds/Windows/PatrolAI/PatrolAI.exe";

        public static void BuildBatch()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Path.Combine(projectRoot, OutputExecutable);
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { CompetitionScene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    "PatrolAI Windows build failed: " + report.summary.result);
            }
        }

        [PostProcessBuild(100)]
        public static void CopyPythonRuntime(BuildTarget target, string executablePath)
        {
            if (target != BuildTarget.StandaloneWindows64 &&
                target != BuildTarget.StandaloneWindows)
            {
                return;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputRoot = Path.GetDirectoryName(Path.GetFullPath(executablePath));
            foreach (string directory in new[] { "agent", "vision", "camera" })
            {
                CopyPythonFiles(
                    Path.Combine(projectRoot, directory),
                    Path.Combine(outputRoot, directory));
            }

            File.Copy(
                Path.Combine(projectRoot, "requirements-phase2.txt"),
                Path.Combine(outputRoot, "requirements-phase2.txt"),
                true);
            Directory.CreateDirectory(Path.Combine(outputRoot, "exchange"));
            Debug.Log("PATROLAI_STANDALONE_PYTHON_RUNTIME_READY root=" + outputRoot);
        }

        private static void CopyPythonFiles(string sourceRoot, string destinationRoot)
        {
            if (!Directory.Exists(sourceRoot))
            {
                throw new BuildFailedException(
                    "Required PatrolAI Python package is missing: " + sourceRoot);
            }

            foreach (string source in Directory.GetFiles(
                         sourceRoot, "*.py", SearchOption.AllDirectories))
            {
                string relative = source.Substring(sourceRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string destination = Path.Combine(destinationRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination, true);
            }
        }
    }
}
