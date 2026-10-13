# PatrolAI external review packet

## Current claim boundary — Phase 8B

Phase 8B audited **16/16 held-out cases and 24/24 original cases** using existing source/results only. Under the conservative rubric, **8/16 held-out cases contain answer-bearing cues**. On the no-cue held-out subset, **bounded Rule v2 = raw Nemotron = hybrid = 8/8 action-correct**; on conflicting no-cue cases, **all three = 2/2**. Both raw-model improvements over Rule v2 (HC09/HC11) occurred in STOP-cued contexts. **NO FAIR DEMO CANDIDATE** met the isolated-superiority criteria. This pilot does not establish AI necessity or Nemotron universal superiority. **External review: PENDING.** HC09 is a functional example, not isolated superiority proof. Historical aggregate counts remain unchanged. See [the cue/fairness audit](PATROLAI_CUE_FAIRNESS_AUDIT.md).

**Target: approximately 15 minutes. EXTERNAL LABEL REVIEW: PENDING.** No software installation is required. Review the five cases below; a short demo video can be shared separately. Model/rule outputs are disclosed for comparison, not as an answer key. Assess evidence and proposed labels independently before considering the outputs.

## A. Short PatrolAI Description

PatrolAI is a simulated industrial patrol prototype: an inspector moves in Unity, uses structured observations, requests contextual NVIDIA Nemotron reasoning through Nebius Token Factory, and maps validated results to bounded simulated actions. The reviewed cases are frozen, internally authored LOCAL SAMPLE held-out inputs, not measurements from real industrial sensors. Cloud inference is REAL when a current request succeeds; environment and actions remain UNITY SIMULATION. Model: `nvidia/Nemotron-3_5-Lightning`. We seek independent assessment of proposed labels before broader real-world testing.

## B. Cases to Review

These five cases prioritize HC09, the HC04 failure, and three conflicting cases (HC08, HC10, HC11). Scores are measured against **proposed** labels frozen before inference, not externally certified ground truth. CONTINUE proceeds; REOBSERVE pauses for more evidence; STOP holds the simulated patrol and permits human review. Verified immediate danger proposes DANGER/STOP; unresolved evidence with budget remaining proposes UNKNOWN/REOBSERVE. Reviewer corrections will be recorded separately, without rewriting historical labels or scores.

| Case ID | Concise local-sample evidence summary | Proposed expected decision | Proposed action | Rule v2 bounded decision | Raw Nemotron decision | Reviewer agree/disagree | Suggested corrected action | Comment |
|---|---|---|---|---|---|---|---|---|
| HC09 | Green conveyor indicator means drive available; structured context reports a person inside an unguarded moving-belt pinch zone. Readiness does not establish personnel safety. | DANGER/STOP | STOP | SAFE/CONTINUE | DANGER/STOP | ____ | ____ | ____ |
| HC04 | Green drive indication, but structured context has confirmed_hazard=true and reports an exposed live conductor confirmed by the insulation monitor. | DANGER/STOP | STOP | DANGER/STOP | WARNING/REOBSERVE | ____ | ____ | ____ |
| HC08 | Same-target current RED camera claim (0.94) conflicts with independent normal-vibration claim (0.82). Neither source is authoritative. | UNKNOWN/REOBSERVE | REOBSERVE | UNKNOWN/REOBSERVE | WARNING/REOBSERVE | ____ | ____ | ____ |
| HC10 | Credible RED lamp claim is 10 minutes old; two current camera claims are green. No authorized restart record; age alone cannot establish clearance. | UNKNOWN/REOBSERVE | REOBSERVE | UNKNOWN/REOBSERVE | WARNING/REOBSERVE | ____ | ____ | ____ |
| HC11 | Green means motor stopped, not safe access. Structured temperature-probe context reports an exposed 160°C vessel surface on the patrol path. | DANGER/STOP | STOP | SAFE/CONTINUE | DANGER/STOP | ____ | ____ | ____ |

All camera, monitor, inspection and probe descriptions in this table are **scenario-authored evidence claims**, not actual hardware readings. “Nemotron decision” means the stored raw model output from the existing pilot. The hybrid actions for these five cases are unchanged by the envelope. HC08/HC10 are action-correct but not exact risk-label matches.

Aggregate action correctness: bounded Rule v2 **14/16**, raw Nemotron **14/16**, Nemotron + Safety Envelope **15/16**. Raw Nemotron misses HC04 and HC16; the envelope corrects HC16, while **HC04 remains incorrect**. Rule v2's score includes its budget/safety rules. The held-out comparison is a directional pilot result and is not statistically conclusive industrial validation.

HC09 is a functional demo example whose frozen context includes explicit STOP guidance; it is not isolated superiority proof. Aggregate held-out results are reported separately. HC09 is not claimed representative of every case. AUTO PATROL HC09 manual Unity validation is owner-reported PASS; independent label review remains pending.

## C. Three Reviewer Questions

1. Is the expected safety decision/action reasonable? Please note any case-specific disagreement or missing assumption.
2. Is re-observation/contextual reasoning useful in real inspection work?
3. What is the most important limitation before real-world testing?

Answers (short comments are sufficient):
1. ____
2. ____
3. ____

## D. Reviewer Metadata

Optional, non-identifying: role ____; field ____; years of experience ____. No name, employer, contact details or sensitive personal data are required.

## E. Permission

Choose one; blank means no quotation permission:
- [ ] I allow this feedback to be quoted anonymously in the competition submission.
- [ ] I do not allow my feedback to be quoted.

Returning feedback does not certify safety or endorse deployment. Review status stays PENDING until a reviewer actually responds. Permission to quote is separate from agreement with labels.
