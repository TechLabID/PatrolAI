# PatrolAI safe submission claims

## Current claim boundary — Phase 8B

Phase 8B audited **16/16 held-out cases and 24/24 original cases** using existing source/results only. Under the conservative rubric, **8/16 held-out cases contain answer-bearing cues**. On the no-cue held-out subset, **bounded Rule v2 = raw Nemotron = hybrid = 8/8 action-correct**; on conflicting no-cue cases, **all three = 2/2**. Both raw-model improvements over Rule v2 (HC09/HC11) occurred in STOP-cued contexts. **NO FAIR DEMO CANDIDATE** met the isolated-superiority criteria. This pilot does not establish AI necessity or Nemotron universal superiority. **External review: PENDING.** HC09 is a functional example, not isolated superiority proof. Historical aggregate counts remain unchanged. See [the cue/fairness audit](PATROLAI_CUE_FAIRNESS_AUDIT.md).

Scope: existing evidence only; no new Phase 8 inference or benchmark. External label review: **PENDING**. Use the [evaluation](PATROLAI_AI_NECESSITY_EVALUATION.md) for measured configurations and limitations.

## SUPPORTED

- Successful current parsed/validated inference can be labeled **REAL — Nebius Token Factory**, using `nvidia/Nemotron-3_5-Lightning`. Failures remain FAILED/FALLBACK/NOT VERIFIED; REAL does not describe observation provenance or hardware.
- An existing **16-case frozen held-out synthetic/local pilot** used 16 REAL inference requests, without retries. Labels were internally proposed before inference; independent label review is pending.
- **Bounded Rule v2: 14/16 action-correct; raw Nemotron: 14/16; Nemotron + Safety Envelope: 15/16.** Rule v2 includes deterministic budget/safety rules (pre-budget proposals: 12/16). These are distinct configurations, not a raw model win.
- **HC16 is corrected by the deterministic envelope; HC04 remains a missed required STOP.** Raw Nemotron misses both; the hybrid still misses HC04.
- **AUTO PATROL HC09 simulated workflow has manual Unity validation PASS, owner reported.** Observation Source: LOCAL SAMPLE — HELD-OUT HC09. Environment: UNITY SIMULATION. Cloud inference can be REAL; movement and actions are simulated.
- Model output is parsed/validated and mapped to bounded actions. Invalid decisions or unresolved evidence at budget exhaustion escalate to STOP / HUMAN REVIEW. The demo permits at most one re-observation and two reasoning invocations; the envelope is not a complete industrial safety interlock.

HC09 was selected for the demo because it is an existing frozen held-out case where Rule v2 and Nemotron produce different actions. Aggregate held-out results are reported separately. HC09 is not claimed representative of every case.

## LIMITED

- **Contextual comparison:** on five conflicting pilot cases, bounded Rule v2 was 3/5 action-correct; raw Nemotron and hybrid were 5/5. Both incremental model successes contain STOP guidance; the no-cue conflicting subset ties at 2/2. These observations do not isolate contextual advantage.
- **Potential reduction in inappropriate CONTINUE:** observed bounded Rule v2 2/13 versus raw Nemotron and hybrid 0/13 expected non-CONTINUE cases. No production incident reduction or general risk reduction was measured.
- Overall hybrid gain is **one case**, supplied by deterministic safety rules. Raw model action correctness ties bounded Rule v2; exact risk-label correctness differs.

The held-out comparison is a directional pilot result and is not statistically conclusive industrial validation.

Rules for clear cases. Nemotron for contextual cases. Deterministic safety envelope for bounded execution. A richer domain-specific rule engine could change the comparison.

## DO NOT CLAIM

- Real industrial deployment, live CCTV detection for HC09, physical sensor evidence for HC09, or physical hardware integration.
- Established AI necessity, statistically proven superiority, “Nemotron beats rules,” guaranteed determinism, or universally better hazard detection.
- Raw Nemotron achieved 15/16; HC16 was solved by model reasoning; HC04 was corrected.
- Validated production impact, measured incident prevention, safety certification, or deployment readiness.
- External label-review approval or reviewer endorsement while status is PENDING.
- HC09 represents every case or the aggregate result; cached/mock/failed inference is REAL.
