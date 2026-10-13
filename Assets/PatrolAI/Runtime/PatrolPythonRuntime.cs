using System;
using System.IO;
using UnityEngine;

namespace PatrolAI.Runtime
{
    /// <summary>
    /// Resolves the existing Python workflow without embedding a developer path.
    /// In Editor the runtime root is the project root; in a player it is the
    /// directory containing the executable.
    /// </summary>
    public static class PatrolPythonRuntime
    {
        public const string PythonOverrideEnvironmentVariable = "PATROLAI_PYTHON";

        private static readonly string[] RequiredPackages =
        {
            "agent",
            "vision",
            "camera"
        };

        public static void ResolveOrThrow(out string runtimeRoot, out string pythonExecutable)
        {
            runtimeRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            ResolveOrThrow(runtimeRoot, Environment.GetEnvironmentVariable(
                PythonOverrideEnvironmentVariable), out pythonExecutable);
        }

        // Explicit root/override inputs keep discovery deterministic and independently testable.
        public static void ResolveOrThrow(string runtimeRoot, string configuredPython,
            out string pythonExecutable)
        {
            runtimeRoot = Path.GetFullPath(runtimeRoot);
            foreach (string package in RequiredPackages)
            {
                string packagePath = Path.Combine(runtimeRoot, package);
                if (!Directory.Exists(packagePath))
                {
                    throw new DirectoryNotFoundException(
                        "PatrolAI Python runtime package is missing: " + packagePath);
                }
            }

            if (!string.IsNullOrWhiteSpace(configuredPython))
            {
                if (!Path.IsPathRooted(configuredPython))
                {
                    throw new InvalidOperationException(
                        PythonOverrideEnvironmentVariable + " must be an absolute path.");
                }
                pythonExecutable = Path.GetFullPath(configuredPython);
            }
            else
            {
                pythonExecutable = Path.Combine(
                    runtimeRoot, ".venv-phase2", "Scripts", "python.exe");
                if (!File.Exists(pythonExecutable))
                    pythonExecutable = Path.Combine(runtimeRoot, ".patrolai-python", "python.exe");
            }

            if (!File.Exists(pythonExecutable))
            {
                throw new FileNotFoundException(
                    "Phase 2 Python environment is missing. Create executable-relative " +
                    ".venv-phase2 with Python 3.12 and requirements-phase2.txt, build with the " +
                    "validated project environment to provision .patrolai-python, or set " +
                    PythonOverrideEnvironmentVariable + " to an absolute Python executable.",
                    pythonExecutable);
            }

            Directory.CreateDirectory(Path.Combine(runtimeRoot, "exchange"));
        }
    }
}
