"""Read-only verification of stored historical evidence; standard library only."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys


def require(value, message):
    if not value:
        raise ValueError(message)


def source_bytes(root, relative):
    working = (root / relative).read_bytes()
    if (root / ".git").exists():
        stored = subprocess.run(
            ["git", "show", "HEAD:" + relative], cwd=root,
            check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        ).stdout
        require(working.replace(b"\r\n", b"\n") == stored.replace(b"\r\n", b"\n"),
                "Working source differs from public Git source: " + relative)
        return stored
    return working


def verify(root):
    evidence = root / "Assets/PatrolAI/Evaluation/Evidence"
    report = json.loads((evidence / "phase7_ai_necessity_results.json").read_bytes())
    rows = report["cases"]
    dataset = source_bytes(root, "Assets/PatrolAI/Evaluation/PatrolAIEvaluationDataset.cs").decode("utf-8")
    labels = re.findall(
        r'Add\(cases,\s*"([^"]+)",\s*"([^"]+)".*?,\s*"(SAFE|DANGER|UNKNOWN)",\s*"(CONTINUE|STOP|REOBSERVE)"\);',
        dataset, re.S,
    )
    expected = {case: (category, risk + "/" + action) for case, category, risk, action in labels}
    require(len(labels) == len(expected) == len(rows) == report["requestCount"] == 24,
            "Expected 24 stored cases and 24 public labels")
    require(len({r["caseId"] for r in rows}) == 24, "Duplicate stored case")
    mapper = source_bytes(root, "Assets/PatrolAI/PhysicalAI/PatrolActionMapper.cs").decode("utf-8")
    actions = dict(re.findall(r'case "([^"]+)":\s*return "([^"]+)";', mapper))
    totals = dict(rule_exact=0, model_exact=0, rule_action=0, model_action=0,
                  action_only_mismatches=0, unnecessary_reobserve=0)
    for row in rows:
        require(expected[row["caseId"]] == (row["category"], row["expectedResult"]),
                "Public label/category mismatch: " + row["caseId"])
        require(row["runtimeStatus"] == "REAL" and row["requestStarted"]
                and row["validStructuredOutput"] and not row["invalidStructuredOutput"]
                and not row["fallbackUsed"] and 200 <= row["httpStatusCode"] < 300,
                "Unverified stored result: " + row["caseId"])
        target, rule, model = (row[k] for k in ("expectedResult", "ruleBasedResult", "nemotronResult"))
        require(all(pair in actions for pair in (target, rule, model)), "Unsupported stored pair")
        rule_exact, model_exact = rule == target, model == target
        rule_action, model_action = actions[rule] == actions[target], actions[model] == actions[target]
        require(row["ruleCorrect"] == rule_exact and row["nemotronCorrect"] == model_exact,
                "Stored exact-correctness flag mismatch")
        totals["rule_exact"] += rule_exact
        totals["model_exact"] += model_exact
        totals["rule_action"] += rule_action
        totals["model_action"] += model_action
        totals["action_only_mismatches"] += model_action and not model_exact
        totals["unnecessary_reobserve"] += actions[model] == "REOBSERVE" and actions[target] != "REOBSERVE"
    require(totals == dict(rule_exact=18, model_exact=16, rule_action=18, model_action=23,
                           action_only_mismatches=7, unnecessary_reobserve=1),
            "Historical Phase 7B stored-result counts differ: " + str(totals))
    require(report["ruleMetrics"]["correct"] == totals["rule_exact"]
            and report["nemotronMetrics"]["correct"] == totals["model_exact"],
            "Stored aggregate differs from case results")
    print("Phase 7B stored-result verification: PASS (24 cases; exact 18/16; action 18/23; 7 action-only mismatches; 1 unnecessary REOBSERVE)")

    names = [
        "Assets/PatrolAI/Evaluation/PatrolHeldOutDataset.cs",
        "Assets/PatrolAI/Evaluation/RuleBasedSafetyBaseline.cs",
        "Assets/PatrolAI/Evaluation/RuleBasedSafetyBaselineV2.cs",
        "Assets/PatrolAI/PhysicalAI/PatrolSafetyEnvelope.cs",
        "Assets/PatrolAI/NVIDIA/NebiusNemotronReasoningService.cs",
        "Assets/PatrolAI/Nebius/NebiusClient.Chat.cs",
        "Assets/PatrolAI/PhysicalAI/PatrolActionMapper.cs",
        "Assets/PatrolAI/Evaluation/README_PHASE7C.md",
        "frozen_cases",
    ]
    entries = [line.split("=", 1) for line in
               (evidence / "frozen_source_hashes.txt").read_text(encoding="utf-8").splitlines()]
    require([entry[0] for entry in entries] == names, "Unexpected historical hash manifest")
    for name, expected_hash in entries:
        data = ((evidence / "frozen_cases.json").read_bytes() if name == "frozen_cases"
                else source_bytes(root, name))
        actual = hashlib.sha256(data).hexdigest().upper()
        require(actual == expected_hash, "Historical hash mismatch: " + name)
        print("MATCH " + name)
    snapshot = json.loads((evidence / "frozen_cases.json").read_bytes())
    require(len(snapshot) == 16 and {r["caseId"] for r in snapshot}
            == {"HC" + str(i).zfill(2) for i in range(1, 17)}, "Invalid frozen case catalog")
    require(all(r["expectedDecision"].split("/")[1] == r["expectedAction"] for r in snapshot),
            "Frozen label/action mismatch")
    print("Held-out 9/9 freeze/hash verification: PASS")
    print("Live inference calls: 0; benchmark reruns: 0")


if __name__ == "__main__":
    try:
        verify(Path(__file__).resolve().parents[4])
    except (OSError, ValueError, KeyError, IndexError, subprocess.CalledProcessError) as error:
        print("Stored evidence verification: FAIL - " + str(error), file=sys.stderr)
        sys.exit(1)
