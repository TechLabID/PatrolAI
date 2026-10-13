"""Compile and run PatrolAI deterministic offline checks. No Unity Editor or model invocation."""
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
TESTS = Path(__file__).resolve().parent
OUT = ROOT / "tests/artifacts/phase10a_ui"


def command(args, name):
    result = subprocess.run([str(a) for a in args], cwd=ROOT, capture_output=True, text=True)
    (OUT / (name + ".log")).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout, end="")
    if result.returncode:
        print(result.stderr, file=sys.stderr)
        raise RuntimeError(name + " failed, exit=" + str(result.returncode))
    return result.stdout


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise RuntimeError(".NET 10 SDK is required")
    sdks = subprocess.check_output([dotnet, "--list-sdks"], text=True)
    entries = re.findall(r"^(10\.[^ ]+) \[(.+)\]$", sdks, re.M)
    if not entries:
        raise RuntimeError(".NET 10 SDK is required")
    version, base = entries[-1]
    compiler = Path(base) / version / "Roslyn/bincore/csc.dll"
    packs = Path(base).parent / "packs/Microsoft.NETCore.App.Ref"
    pack = sorted((p for p in packs.iterdir() if p.name.startswith("10.")),
                  key=lambda p: tuple(map(int, p.name.split("."))))[-1]
    refs = list((pack / "ref/net10.0").glob("*.dll"))
    runtime = {"runtimeOptions": {"tfm": "net10.0", "framework": {
        "name": "Microsoft.NETCore.App", "version": pack.name}}}
    patrol = ROOT / "Assets/PatrolAI"
    sources = []
    for folder in ("Core", "Nebius", "NVIDIA", "PhysicalAI", "Evaluation", "Scenarios", "UI", "Competition", "Runtime"):
        sources.extend(sorted((patrol / folder).glob("*.cs")))
    sources = [p for p in sources if p.name != "PatrolCompetitionEvidenceView.cs"]

    def build(name, source_files, startup, defines=""):
        output = OUT / (name + ".dll")
        flags = ["/nologo", "/target:exe", "/langversion:9.0", "/nostdlib+",
                 '/out:"' + str(output) + '"', "/main:" + startup]
        if defines:
            flags.append("/define:" + defines)
        flags += ['/reference:"' + str(p) + '"' for p in refs]
        flags += ['"' + str(p) + '"' for p in source_files]
        response = OUT / (name + ".rsp")
        response.write_text("\n".join(flags), encoding="utf-8")
        command([dotnet, compiler, "@" + str(response)], name + "-compile")
        output.with_suffix(".runtimeconfig.json").write_text(json.dumps(runtime), encoding="utf-8")
        return output

    suite = OUT / "OfflineSuite.cs"
    suite.write_text('''using System;
class OfflineSuite {
    static int Main() {
        Console.WriteLine(PatrolAI.Scenarios.PatrolPhase5TestHarness.RunAll());
        Console.WriteLine(PatrolAI.PhysicalAI.PatrolClosedLoopTestHarness.RunAll());
        Console.WriteLine(PatrolAI.NVIDIA.NemotronOfflineTestHarness.RunAll());
        Console.WriteLine(PatrolAI.Evaluation.PatrolAIEvaluationTestHarness.RunAll());
        PatrolAI.Evaluation.PatrolPhase7BTestHarness.RunAll();
        int code = PatrolAI.Evaluation.PatrolPhase7CTestHarness.Main();
        Console.WriteLine("OFFLINE PRODUCTION DEPENDENCIES; LIVE CALLS=0; BENCHMARK RERUNS=0");
        return code;
    }
}
''', encoding="utf-8")
    production = build("OfflineSuite", sources + [TESTS / "UnityFixtures.cs", suite], "OfflineSuite")
    command([dotnet, production], "offline-harnesses")
    fixture_sources = [p for p in sources if p.name not in
                       ("NebiusNemotronReasoningService.cs", "PatrolReobservationEvidenceReader.cs")]
    fixture_sources += list(TESTS.glob("*.cs"))
    fixtures = build("UIStateRegression", fixture_sources, "PatrolUIStateRegression")
    command([dotnet, fixtures], "ui-auto-regressions")
    command([sys.executable, patrol / "Evaluation/Evidence/verify_stored_evidence.py"], "public-evidence")
    print("ALL OFFLINE CHECKS: PASS; HTTP REQUESTS=0; BENCHMARK RERUNS=0")


if __name__ == "__main__":
    try:
        main()
    except (OSError, RuntimeError, subprocess.CalledProcessError, IndexError) as error:
        print("OFFLINE CHECKS: FAIL - " + str(error), file=sys.stderr)
        sys.exit(1)
