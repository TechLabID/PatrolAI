# HC09 fairness audit

**Classification: POTENTIALLY BIASED.** Audit of existing frozen data, production code and the actual current serializer; no inference or benchmark rerun. Visual polish, reviewer-material changes and recording are stopped at the requested fairness gate.

The broader [Phase 8B audit](PATROLAI_CUE_FAIRNESS_AUDIT.md) supersedes any isolated-superiority interpretation: no-cue held-out actions tie at 8/8, no-cue conflicts tie at 2/2, and no fair superiority demo candidate qualifies. HC09 remains a functional example; AI necessity is not established.

Both systems receive the same seven substantive observation fields, including the complete textual context. No expected-label field is secretly sent only to Nemotron. However, the shared context explicitly states **“confirmed direct danger requires immediate stop”**, which is an answer-bearing instruction. Nemotron can follow that instruction; the deterministic baseline does not translate it into its recognized `confirmed_hazard=true` predicate. Shared bytes therefore do not establish equivalent decision representation, or isolate contextual reasoning from following an explicit action cue. This is a potential validity confound, not proof of deliberately withheld evidence or of causation from that phrase alone.

## Observation and exact inputs

Frozen proposed label: **DANGER/STOP**; proposed action: **STOP**. Observation source: **LOCAL SAMPLE — HELD-OUT HC09**. Environment/action: **UNITY — SIMULATED**. Prior successful Nemotron inference was REAL via Nebius Token Factory; this audit sends no requests. External label review remains **PENDING**.

Rule input is the `PatrolObservation` object below. Nemotron's actual serialized user-message observation is:

```json
{
  "observationId": "heldout-HC09",
  "source": "Pilot synthetic/local held-out scenario",
  "detectedCondition": "INDICATOR_GREEN",
  "evidenceStatus": "SUFFICIENT",
  "confidence": 0.93,
  "location": "Pump/conveyor/ventilation inspection zone",
  "additionalContext": "sources=2; green conveyor indicator reports drive available. A current verified guard inspection reports a person inside the unguarded moving-belt pinch zone. Drive readiness does not establish personnel safety; confirmed direct danger requires immediate stop."
}
```

The Rule object additionally has `timestamp="2026-10-08T00:00:00Z"`; the serializer omits timestamp. AUTO PATROL refreshes that timestamp when making a new copy, but retains every serialized field verbatim. The UI computes its baseline comparison from a separate copy of the same HC09 data; it does not receive a reduced observation. Rules do not consult timestamps in this case.

Nemotron's user-message prefix is exactly:

> Return only the required PatrolDecision JSON for this observation:

It is followed by the JSON above. Its production system message is exactly:

```text
You are the PatrolAI industrial safety reasoning component. Use only the supplied observation; do not invent evidence. Return exactly one JSON object matching the response schema. Do not return markdown, code fences, analysis, reasoning traces, or prose outside the JSON object. Required fields: riskLevel, action, reason, requiresReobservation, confidence. riskLevel must be SAFE, WARNING, DANGER, or UNKNOWN. action must be CONTINUE, REOBSERVE, INSPECT, STOP, ALERT, or REROUTE. Use UNKNOWN and prefer REOBSERVE when evidence is insufficient. Keep reason short and confidence between 0.0 and 1.0.
```

The request uses model `nvidia/Nemotron-3_5-Lightning`, temperature 0.1, maximum completion tokens 512, `stream=false`, thinking disabled. The strict `patrol_decision` response schema permits riskLevel SAFE/WARNING/DANGER/UNKNOWN and action CONTINUE/REOBSERVE/INSPECT/STOP/ALERT/REROUTE, plus reason, requiresReobservation and confidence [0,1]; all five are required and extra properties are prohibited. The schema permits STOP but does not select STOP or encode HC09's expected label. No prior chat history, retrieval, image or previous decision is added to this request. The `NemotronReasoningRequest.systemInstruction` default exists in memory but is NOT serialized or used as the production system message.

## Field-by-field availability and transformation audit

YES in an availability column means supplied to the decision path, not necessarily interpreted by it. Textual elements are sub-elements of the unchanged `additionalContext` string, not separately extracted sensor fields. Transformation means a pre-decision normalization/derivation; unchanged JSON escaping is a wire representation, not new evidence.

| Field/context element | Exact value or absence | Rule available | Nemotron available | Transformed/derived before decision | Expected-answer cue or leakage risk |
|---|---|---|---|---|---|
| observationId | `heldout-HC09` | YES, not consulted | YES | NO | Case identity, not an action label; held-out here does not guarantee absence from model pretraining |
| source | `Pilot synthetic/local held-out scenario` | YES, not consulted | YES | NO | Provenance; no target action |
| detectedCondition / indicator | `INDICATOR_GREEN` | YES | YES | YES: rules normalize case; model value unchanged | Favors CONTINUE under predefined rules; does not describe personnel safety |
| evidenceStatus / sufficiency | `SUFFICIENT` | YES | YES | YES: rules normalize case; model value unchanged | Predetermined sufficiency assessment; no direct STOP label; structured status is NOT `CONFLICT` |
| confidence | `0.93` (float32 representation) | YES | YES | NO: threshold comparison does not change value | High assigned confidence can strengthen scenario claims; not a model probability or sensor measurement |
| location | `Pump/conveyor/ventilation inspection zone` | YES, not consulted | YES | NO | Equipment context; not an expected action |
| additionalContext | Complete JSON string above, verbatim | YES | YES | YES: rules uppercase it for token tests; model text unchanged | Contains facts AND an explicit action recommendation |
| source count | `sources=2` | YES, within text; no count parser | YES, within text | YES: enclosing rule text uppercase only; NO separate count derivation | Supports multi-source interpretation; no direct expected action |
| source 1 description / conveyor readiness | `green conveyor indicator reports drive available.` | YES, within text | YES, within text | YES: rule text uppercase only | Operational availability claim; not safe-access evidence |
| source 2 description / guard-person hazard | `A current verified guard inspection reports a person inside the unguarded moving-belt pinch zone.` | YES, within text | YES, within text | YES: rule text uppercase only | Hazard and verification are asserted in the sample; strongly implies STOP under the proposed safety policy |
| interpretation / policy text | `Drive readiness does not establish personnel safety;` | YES, within text | YES, within text | YES: rule text uppercase only | Supplies the relationship the demo attributes to contextual reasoning |
| direct action instruction | `confirmed direct danger requires immediate stop.` | YES, within text | YES, within text | YES: rule text uppercase only | **Explicit expected-action cue; principal confound** |
| explicit hazard predicate | `confirmed_hazard=true` is ABSENT | NO present token | NO present token | NO | Baseline's recognized override is missing although natural-language danger is asserted |
| structured source agreement | `agreement=conflict` / `agreement=disagree` are ABSENT | NO present tokens | NO present tokens | NO | Wrapper category says conflicting, but observation status/tokens do not set the rules' conflict condition |
| prior state / previous decision | ABSENT | NO | NO | NO | No prior-state cue |
| re-observation count | `reobservation_attempts` token ABSENT | NO explicit count; envelope derives 0 | NO explicit count | YES for rules/envelope: missing token defaults to 0 | No budget-exhaustion instruction in HC09 |
| generic reasoning/safety system prompt | Full production system message above | NO as prompt; separate rules embody policy | YES | NO | Method-specific instructions; generic, not an HC09 STOP answer |
| response schema / output constraints | Full allowed enums and required fields described above | NO as JSON schema; mapper/whitelist used separately | YES | NO | Allows multiple actions; no selected expected action |
| request's default systemInstruction | Default text in `NemotronReasoningRequest` | NO | NO on wire | NO | Not part of actual live input; must not be mistaken for production prompt |
| expected risk / decision label | `DANGER` / `DANGER/STOP`, outside observation | NO | NO | NO | Used in scoring after decision, not in input |
| expected action | `STOP`, outside observation | NO | NO | NO | Not serialized; STOP recommendation still appears inside context |
| benchmark case/category metadata | `caseId=HC09`, `category=CONFLICTING`, outside observation | NO as wrapper metadata | NO as wrapper metadata | NO | Category is not sent; case identity remains in observationId |
| benchmark policyRationale | `Internally proposed label, frozen before inference; external review pending.` | NO | NO | NO | Review metadata outside observation, not sent |
| timestamp | Frozen `2026-10-08T00:00:00Z`; demo refreshes timestamp | YES, not consulted | NO, omitted | YES in demo only: current timestamp | No action cue; timestamp asymmetry does not explain the result |
| Unity camera image / physical sensor input | ABSENT for initial HC09 | NO | NO | NO | Scene is a switchgear illustration, not a conveyor measurement |

## Shared and asymmetric inputs

`PatrolPhase7CRunner.RunAsync` passes `c.observation` to the live reasoning service, Rule v1 `Decide`, and Rule v2 `Propose`. Labels/category/policyRationale stay in the case wrapper and scoring functions. The actual serializer was invoked read-only through official Unity Pipeline and its seven emitted fields were matched to `frozen_cases.json` without constructing a service or making HTTP calls.

The shared hazard prose is available to both rules. The asymmetry is **how it is represented and consumed**: v2 recognizes the literal structured token `CONFIRMED_HAZARD=TRUE`; Nemotron receives prose that both reports danger and instructs immediate STOP. The rules do not implement the semantic relation or the textual recommendation. Model-only generic system instructions and the output schema are normal method-specific differences, but they do not remove the answer-cue confound. Timestamp is rule-object-only and unused. No expected label/action, category, policyRationale or previous model answer was found in the serialized prompt.

## Leakage finding and why the baseline returns CONTINUE

There is **no verified model-only expected-label field leakage**. There IS verified answer-bearing guidance within the shared observation. It remains possible that Nemotron's STOP derives from recognizing the personnel hazard, following the explicit STOP recommendation, or both; the existing run cannot separate those causes. Frozen labels and a held-out case designation do not remove this issue. No input ablation or alternative scenario was run, and no artifact was edited to repair it.

For Rule v2, `PatrolSafetyEnvelope.Unresolved` returns false: GREEN is known, SUFFICIENT has no unresolved-status token, and 0.93 exceeds 0.75. The natural-language hazard does not match `CONFIRMED_HAZARD=TRUE`. The predefined GREEN branch therefore produces SAFE/CONTINUE. Attempts default to 0 and the envelope finds no structured unresolved condition, so it retains CONTINUE. The stored result confirms raw and bounded v2 are identical for HC09. Rule v1 similarly sees sufficient green, no structured conflict token and adequate confidence, and chooses SAFE/CONTINUE.

The Rule-Based Baseline is deterministic and scope-bounded: it follows the explicit predefined predicates implemented here. Its CONTINUE is explainable by that representation, not evidence that deterministic rules cannot handle personnel hazards. Nemotron's stored DANGER/STOP and `decision.reason` are real outputs, but this case does not isolate unaided contextual reasoning.

## Could one simple deterministic rule close the gap?

**YES for this fixed case.** A predefined predicate “verified person inside an active, unguarded pinch zone → STOP” could cover the existing contextual claim, using structured extraction or a bounded text pattern. It would not require an LLM for this known predicate. Generalizing or validating such extraction against noisy real observations is separate work. No new rule, hazard flag, parser, dataset entry or benchmark run is introduced by this audit.

## Limitation and safe judge-facing conclusion

> HC09 shows different actions on a shared local sample that already includes STOP guidance. It is not a clean isolation of contextual reasoning or proof of superiority over deterministic rules. Aggregate evaluation results are reported separately.

The existing aggregate figures remain historical observations: bounded Rule v2 14/16 action-correct; raw Nemotron 14/16; Nemotron + Safety Envelope 15/16. HC16 is corrected by the envelope; HC04 remains a missed required STOP. This HC09 audit does not certify the fairness of every aggregate case, invalidate/rewrite those recorded counts, or establish statistically conclusive industrial validation. External label review remains PENDING.

## Evidence and scope of the stop

- [Frozen dataset definition](../Evaluation/PatrolHeldOutDataset.cs) and the existing frozen HC09 snapshot/result (local evaluation artifacts; not republished).
- [Benchmark input/scoring path](../Evaluation/PatrolPhase7CRunner.cs): `RunAsync`, `Score`, `Freeze`.
- [Rule-Based Baseline v2](../Evaluation/RuleBasedSafetyBaselineV2.cs): `Propose` and `Decide`; [v1](../Evaluation/RuleBasedSafetyBaseline.cs): `Decide`.
- [Safety envelope](../PhysicalAI/PatrolSafetyEnvelope.cs): `Unresolved`, `Attempts`, `Apply`.
- [Nemotron request](../NVIDIA/NemotronReasoningRequest.cs): `TryCreate`; [live service](../NVIDIA/NebiusNemotronReasoningService.cs): `SystemPrompt`, `SerializeObservation`, `ReasonWithDiagnosticsAsync`.
- [Wire request/schema](../Nebius/NebiusClient.Chat.cs): `SendChatCompletionAsync`, `CreatePatrolDecisionResponseFormat`.
- [AUTO PATROL](../Scenarios/PatrolAutoPatrolDemoController.cs): `CreateObservation`, `Reason`; [comparison UI](../UI/PatrolCompetitionEvidenceView.cs): `Awake`, `DrawHc09ReviewerPanel`.

All eight source/policy fingerprints and the frozen-case snapshot matched the pre-inference stored hashes. The current serializer was checked, not a new inference. Editor was ready, Play Mode stopped, console errors 0. No UI/source/README/reviewer claims were changed, no new video recorded and no compile/benchmark suites run. Only this audit document and its Unity metadata are added publicly; local read-only audit helpers/results remain local. No commit, tag, push, publication or staging.
