# VISIONGUARD Phase 7 — Evaluation Report

## Result

- Scenarios: 10
- Passed: 10
- Failed: 0
- Overall: PASS
- Ground truth: explicit simulated ground truth stored separately from predictions

## Scenario results

- A_clear_red: PASS
- B_clear_green: PASS
- C_primary_insufficient_secondary_sufficient: PASS
- D_multi_source_agreement: PASS
- E_insufficient_multi_source: PASS
- F_repeated_unchanged_condition: PASS
- G_red_to_green_confirmed_change: PASS
- H_replay_same_incident: PASS
- I_cloud_unavailable_local_preserved: PASS
- J_multi_source_conflict: PASS

## Metrics

- Perception classification rate: 0.666667
- Perception UNKNOWN rate: 0.333333
- Evidence sufficient/insufficient rates: 0.666667 / 0.333333
- Re-observation score improvement: 0.958719
- Re-observation success rate: 1.000000
- Fusion CONSISTENT/CONFLICT/INSUFFICIENT rates: 0.333333 / 0.333333 / 0.333333
- Fusion VERIFIED rate: 0.333333
- False VERIFIED count: 0
- Temporal NO_CHANGE correctness: 1.000000
- Temporal CONFIRMED_CHANGE correctness: 1.000000
- False/duplicate incident counts: 0 / 0
- Temporal replay idempotency correctness: 1.000000
- Phase 6 package generation success: 1.000000
- Phase 6 deterministic package hash: True
- Phase 6 idempotent re-upload: True
- Safe local preservation on cloud failure: True

## Local timing

- opencv_evidence_processing_ms: mean 14.589767 ms (3 sample(s), min 12.173300, max 16.536100)
- reobservation_decision_ms: mean 0.017200 ms (1 sample(s), min 0.017200, max 0.017200)
- fusion_ms: mean 0.014300 ms (3 sample(s), min 0.008400, max 0.022500)
- temporal_evaluation_ms: mean 5.126300 ms (4 sample(s), min 0.015800, max 13.949800)
- phase6_local_package_creation_ms: mean 6.525000 ms (1 sample(s), min 6.525000, max 6.525000)

These timings exclude physical drone movement and AWS latency.

## Known limitations

- Evaluation uses a small deterministic suite, not a population-scale accuracy benchmark.
- Visual inputs are Unity-rendered simulated observations; no physical CCTV or drone was measured.
- Latency is a single local-machine run and is not a statistically stable performance benchmark.
- No physical drone movement latency is measured.
- No real AWS upload, Lambda execution, CloudWatch delivery, or AWS latency is measured.
- Cloud failure and S3 replay behavior use an in-memory test transport.

Machine-readable details: `evaluation/phase7/summary.json`.
