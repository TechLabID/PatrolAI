# VISIONGUARD Phase 5 — Temporal Monitoring and Verified Change Detection

## Sealed boundary

Phase 5 is an additive consumer of completed Phase 4 fusion JSON. It does not call or alter the detector, threshold, observation exporter, Phase 3 re-observation path, or Phase 4 fusion policy. A fusion result is considered verified only when it is `CONSISTENT`, `VERIFIED`, and carries an accepted final indicator condition.

## Deterministic temporal state

One state record is stored per target under `exchange/phase5/temporal`. It records the previous and current verified conditions and fusion IDs, the first verified timestamp, the latest verified timestamp, the confirmed-change flag, and the deterministic change ID. A pending candidate is retained separately and never updates the confirmed condition.

- `NO_CHANGE`: establishes the first verified baseline or records another verified result with the same condition.
- `CHANGE_CANDIDATE`: exactly one sufficient provenance condition differs from the confirmed condition, but the fusion result is not verified. Confirmed state is unchanged.
- `CONFIRMED_CHANGE`: a different condition arrives through a `CONSISTENT / VERIFIED` Phase 4 result. Only this outcome advances confirmed state and emits an incident.
- `UNRESOLVED`: evidence is insufficient, conflicting, malformed, or lacks a verified baseline. Confirmed state is unchanged.

Change and incident identifiers are SHA-256-derived from actual target, condition, and fusion identifiers. Reprocessing the same fusion ID returns its existing Phase 5 result and cannot duplicate an incident.

## Incident record

Confirmed incidents are stored under `exchange/phase5/incidents`. Each record includes `incident_id`, `target_id`, previous and new conditions, `change_id`, supporting fusion and observation IDs, supporting source IDs, verification status, the prior verified timestamp, and the confirmation timestamp. Timestamps and provenance are copied from the fusion input; Phase 5 does not generate observation facts.

Per-evaluation records are stored under `exchange/phase5/results`.

## Runtime flow

1. Phase 4 captures and fuses evidence normally.
2. `VGPhase5TemporalController` passes the resulting fusion JSON path to `python -m agent.temporal.run`.
3. The temporal runner loads the target state, evaluates the verified result, and atomically persists the Phase 5 result and state.
4. Only `CONFIRMED_CHANGE` writes an incident record.

The targeted replay test demonstrates baseline establishment, repeated-condition `NO_CHANGE`, an unverified `CHANGE_CANDIDATE`, verified `CONFIRMED_CHANGE`, one persisted incident, and rejection of insufficient/conflicting confirmation.

## Manual Unity review

1. Run `VISIONGUARD > Phase 5 > Build Runtime Review Scene`.
2. Open `Assets/VisionGuard/Phase5/Scenes/VISIONGUARD_IndustrialRoom_Phase5_v01.unity` and enter Play Mode.
3. Run the existing Phase 4 multi-source fusion control.
4. Select `VG_Phase5_TemporalController` and click **EVALUATE LATEST VERIFIED FUSION**.
5. Repeat a verified unchanged capture to observe `NO_CHANGE` and inspect `exchange/phase5`.
6. For change review, supply a real changed condition through the existing Phase 4 verification path before evaluating again. Confirm that only the independently verified changed fusion produces `CONFIRMED_CHANGE` and one incident.

Unity runtime validation is intentionally left to the user.
