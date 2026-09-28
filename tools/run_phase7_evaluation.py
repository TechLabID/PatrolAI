"""Run the additive VISIONGUARD Phase 7 deterministic evaluation suite."""

from __future__ import annotations

import argparse
import copy
import hashlib
import io
import json
import math
import os
from pathlib import Path
import shutil
import statistics
import sys
import tempfile
import time

PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

import cv2

from agent.fusion.policy import EvidenceFusionPolicy
from agent.policy.evidence_policy import EvidencePolicy
from agent.reasoning.state_machine import ActiveInspectionStateMachine
from agent.temporal.policy import TemporalPolicy
from agent.temporal.run import run_fusion
from camera.adapters.unity import read_manifest
from cloud.aws.package import create_local_package
from cloud.aws.sync import sync_package
from vision.opencv.frames import load_frame
from vision.opencv.run import process


ROOT = PROJECT_ROOT
DEFAULT_SCENARIOS = ROOT / 'evaluation/phase7/scenarios/scenarios.json'
DEFAULT_OUTPUT = ROOT / 'evaluation/phase7'
DEFAULT_REPORT = ROOT / 'docs/Phase7_Evaluation_Report.md'


def _timed(call):
    started = time.perf_counter_ns()
    value = call()
    elapsed_ms = (time.perf_counter_ns() - started) / 1_000_000
    return value, round(elapsed_ms, 6)


def _mean(values):
    return round(statistics.fmean(values), 6) if values else 0.0


def _rate(count, total):
    return round(count / total, 6) if total else 0.0


def _write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + '.tmp')
    try:
        temporary.write_text(
            json.dumps(value, indent=2, sort_keys=True, allow_nan=False) + '\n',
            encoding='utf-8')
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)


def _evidence_as(evidence, camera_id, observation_id, condition=None,
                 quality=None, score=None):
    value = copy.deepcopy(evidence)
    value['camera_id'] = camera_id
    value['observation_id'] = observation_id
    if condition is not None:
        value['condition'] = condition
    if quality is not None:
        value['evidence_quality'] = quality
    if score is not None:
        value['confidence'] = score
    return value


def _verified_fusion(session_id, condition, minute, score_a=0.91,
                     score_b=0.94):
    timestamp_a = f'2026-09-28T07:{minute:02d}:00Z'
    timestamp_b = f'2026-09-28T07:{minute:02d}:01Z'
    return {
        'schema_version': 1,
        'target_id': 'SWG_05/Indicator_03',
        'session_id': session_id,
        'fusion_status': 'CONSISTENT',
        'verification': 'VERIFIED',
        'action': 'RECORD',
        'final_condition': condition,
        'reason': 'multiple_sufficient_sources_agree',
        'provenance': [
            {
                'camera_id': 'VG_CCTV_02',
                'observation_id': 'obs_' + session_id + '_cctv',
                'condition': condition,
                'quality': 'SUFFICIENT',
                'score': score_a,
                'timestamp': timestamp_a,
            },
            {
                'camera_id': 'VG_MOBILE_01',
                'observation_id': 'obs_' + session_id + '_mobile',
                'condition': condition,
                'quality': 'SUFFICIENT',
                'score': score_b,
                'timestamp': timestamp_b,
            },
        ],
    }


class MemoryS3:
    def __init__(self, fail=False):
        self.objects = {}
        self.fail = fail

    def put_object(self, Bucket, Key, Body, **kwargs):
        if self.fail:
            raise RuntimeError('simulated_cloud_unavailable')
        self.objects[(Bucket, Key)] = bytes(Body)
        return {'ETag': 'phase7-memory'}

    def get_object(self, Bucket, Key):
        body = self.objects.get((Bucket, Key))
        if body is None:
            error = RuntimeError('not found')
            error.response = {'Error': {'Code': 'NoSuchKey'}}
            raise error
        return {'Body': io.BytesIO(body)}


def load_scenarios(path=DEFAULT_SCENARIOS):
    catalog = json.loads(Path(path).read_text(encoding='utf-8'))
    if catalog.get('ground_truth_kind') != 'explicit_simulated_ground_truth':
        raise ValueError('explicit_simulated_ground_truth_required')
    scenarios = catalog.get('scenarios')
    if not isinstance(scenarios, list) or not scenarios:
        raise ValueError('phase7_scenarios_required')
    identifiers = [scenario.get('id') for scenario in scenarios]
    if len(identifiers) != len(set(identifiers)):
        raise ValueError('duplicate_phase7_scenario_id')
    for scenario in scenarios:
        for field in ('id', 'category', 'target_id'):
            if not isinstance(scenario.get(field), str) or not scenario[field]:
                raise ValueError('invalid_scenario_' + field)
        if not any(key.startswith('expected_') for key in scenario):
            raise ValueError('scenario_ground_truth_required:' + scenario['id'])
    return catalog


def run_evaluation(scenario_path=DEFAULT_SCENARIOS, output_root=DEFAULT_OUTPUT,
                   report_path=DEFAULT_REPORT):
    catalog = load_scenarios(scenario_path)
    ground_truth = {item['id']: item for item in catalog['scenarios']}
    results = []
    perception_records = []
    fusion_records = []
    temporal_records = []
    timing = {
        'opencv_evidence_processing_ms': [],
        'reobservation_decision_ms': [],
        'fusion_ms': [],
        'temporal_evaluation_ms': [],
        'phase6_local_package_creation_ms': [],
    }

    with tempfile.TemporaryDirectory() as directory:
        workspace = Path(directory)
        evidence_output = workspace / 'evidence'

        def scenario_result(identifier, passed, observed):
            result = {
                'id': identifier,
                'category': ground_truth[identifier]['category'],
                'passed': bool(passed),
                'ground_truth': ground_truth[identifier],
                'observed': observed,
            }
            results.append(result)
            return result

        red_manifest = ROOT / ground_truth['A_clear_red']['fixture']
        red, red_ms = _timed(
            lambda: process(red_manifest, evidence_output, annotated=False))
        timing['opencv_evidence_processing_ms'].append(red_ms)
        perception_records.append((
            'A_clear_red', ground_truth['A_clear_red']['expected_condition'], red))
        scenario_result(
            'A_clear_red',
            red['condition'] == 'INDICATOR_RED' and
            red['evidence_quality'] == 'SUFFICIENT',
            {'condition': red['condition'], 'quality': red['evidence_quality'],
             'score': red['confidence'], 'latency_ms': red_ms})

        green_manifest = ROOT / ground_truth['B_clear_green']['fixture']
        green, green_ms = _timed(
            lambda: process(green_manifest, evidence_output, annotated=False))
        timing['opencv_evidence_processing_ms'].append(green_ms)
        perception_records.append((
            'B_clear_green',
            ground_truth['B_clear_green']['expected_condition'], green))
        scenario_result(
            'B_clear_green',
            green['condition'] == 'INDICATOR_GREEN' and
            green['evidence_quality'] == 'SUFFICIENT',
            {'condition': green['condition'], 'quality': green['evidence_quality'],
             'score': green['confidence'], 'latency_ms': green_ms})

        observation, red_frame_path = read_manifest(red_manifest)
        frame = load_frame(observation, red_frame_path)
        scale = math.tan(math.radians(36 / 2)) / math.tan(math.radians(60 / 2))
        reduced = cv2.resize(
            frame, None, fx=scale, fy=scale, interpolation=cv2.INTER_AREA)
        simulated = frame.copy()
        simulated[:] = 0
        y = (frame.shape[0] - reduced.shape[0]) // 2
        x = (frame.shape[1] - reduced.shape[1]) // 2
        simulated[y:y + reduced.shape[0], x:x + reduced.shape[1]] = reduced
        ok, encoded = cv2.imencode('.png', simulated)
        if not ok:
            raise RuntimeError('phase7_primary_fixture_encoding_failed')
        primary_frame_path = workspace / 'phase7_primary_wide.png'
        primary_frame_path.write_bytes(encoded.tobytes())
        primary_manifest_value = json.loads(
            red_manifest.read_text(encoding='utf-8'))
        primary_manifest_value.update(
            observation_id='obs_phase7_primary_wide',
            source_type='synthetic_fixture',
            frame=primary_frame_path.name,
            frame_sha256=hashlib.sha256(encoded.tobytes()).hexdigest())
        primary_manifest_value['metadata']['provenance'] = 'synthetic_test_only'
        primary_manifest_path = workspace / 'phase7_primary_wide.json'
        _write_json(primary_manifest_path, primary_manifest_value)
        primary, primary_cv_ms = _timed(
            lambda: process(
                primary_manifest_path, evidence_output, fixture=True,
                annotated=False))
        timing['opencv_evidence_processing_ms'].append(primary_cv_ms)
        perception_records.append((
            'C_primary_insufficient_secondary_sufficient',
            ground_truth[
                'C_primary_insufficient_secondary_sufficient'][
                    'expected_condition'], primary))
        state_machine = ActiveInspectionStateMachine()
        primary_plan, decision_ms = _timed(
            lambda: state_machine.assess_primary(primary))
        timing['reobservation_decision_ms'].append(decision_ms)
        secondary_plan = state_machine.assess_secondary(primary, red)
        improvement = round(red['confidence'] - primary['confidence'], 6)
        reobservation_pass = (
            primary['condition'] == 'UNKNOWN' and
            primary['evidence_quality'] == 'INSUFFICIENT' and
            red['evidence_quality'] == 'SUFFICIENT' and improvement > 0 and
            primary_plan['state'] == 'REQUEST_REOBSERVATION' and
            secondary_plan['result']['verification'] == 'VERIFIED')
        scenario_result(
            'C_primary_insufficient_secondary_sufficient', reobservation_pass,
            {
                'primary_condition': primary['condition'],
                'primary_quality': primary['evidence_quality'],
                'primary_score': primary['confidence'],
                'secondary_condition': red['condition'],
                'secondary_quality': red['evidence_quality'],
                'secondary_score': red['confidence'],
                'score_improvement': improvement,
                'reobservation_action': primary_plan['action'],
                'verification': secondary_plan['result']['verification'],
                'opencv_latency_ms': primary_cv_ms,
                'decision_latency_ms': decision_ms,
            })

        fusion_policy = EvidenceFusionPolicy()
        agreement_input = [
            _evidence_as(red, 'VG_CCTV_02', 'obs_phase7_agree_cctv'),
            _evidence_as(red, 'VG_MOBILE_01', 'obs_phase7_agree_mobile'),
        ]
        agreement, agreement_ms = _timed(
            lambda: fusion_policy.fuse(agreement_input))
        timing['fusion_ms'].append(agreement_ms)
        fusion_records.append(('D_multi_source_agreement', agreement, False))
        scenario_result(
            'D_multi_source_agreement',
            agreement['fusion_status'] == 'CONSISTENT' and
            agreement['verification'] == 'VERIFIED' and
            agreement['final_condition'] == 'INDICATOR_RED',
            {**agreement, 'latency_ms': agreement_ms})

        unknown = _evidence_as(
            red, 'VG_MOBILE_01', 'obs_phase7_unknown', 'UNKNOWN',
            'INSUFFICIENT', 0.0)
        insufficient_input = [
            _evidence_as(red, 'VG_CCTV_02', 'obs_phase7_one_source'), unknown]
        insufficient, insufficient_ms = _timed(
            lambda: fusion_policy.fuse(insufficient_input))
        timing['fusion_ms'].append(insufficient_ms)
        fusion_records.append(('E_insufficient_multi_source', insufficient, True))
        scenario_result(
            'E_insufficient_multi_source',
            insufficient['fusion_status'] == 'INSUFFICIENT' and
            insufficient['verification'] == 'UNRESOLVED',
            {**insufficient, 'latency_ms': insufficient_ms})

        conflict_input = [
            _evidence_as(red, 'VG_CCTV_02', 'obs_phase7_conflict_red'),
            _evidence_as(green, 'VG_MOBILE_01', 'obs_phase7_conflict_green'),
        ]
        conflict, conflict_ms = _timed(
            lambda: fusion_policy.fuse(conflict_input))
        timing['fusion_ms'].append(conflict_ms)
        fusion_records.append(('J_multi_source_conflict', conflict, True))
        scenario_result(
            'J_multi_source_conflict',
            conflict['fusion_status'] == 'CONFLICT' and
            conflict['verification'] == 'UNRESOLVED',
            {**conflict, 'latency_ms': conflict_ms})

        temporal_policy = TemporalPolicy()
        red_baseline = _verified_fusion(
            'phase7_red_baseline', 'INDICATOR_RED', 1)
        red_repeat = _verified_fusion(
            'phase7_red_repeat', 'INDICATOR_RED', 2)
        baseline = temporal_policy.evaluate(red_baseline)
        unchanged, unchanged_ms = _timed(
            lambda: temporal_policy.evaluate(red_repeat, baseline['state']))
        timing['temporal_evaluation_ms'].append(unchanged_ms)
        temporal_records.append(('F_repeated_unchanged_condition', unchanged))
        scenario_result(
            'F_repeated_unchanged_condition',
            unchanged['outcome'] == 'NO_CHANGE' and
            unchanged['incident'] is None,
            {'outcome': unchanged['outcome'],
             'incident_created': unchanged['incident'] is not None,
             'latency_ms': unchanged_ms})

        green_change = _verified_fusion(
            'phase7_green_change', 'INDICATOR_GREEN', 3,
            green['confidence'], green['confidence'])
        changed, changed_ms = _timed(
            lambda: temporal_policy.evaluate(green_change, baseline['state']))
        timing['temporal_evaluation_ms'].append(changed_ms)
        temporal_records.append(('G_red_to_green_confirmed_change', changed))
        scenario_result(
            'G_red_to_green_confirmed_change',
            changed['outcome'] == 'CONFIRMED_CHANGE' and
            changed['previous_verified_condition'] == 'INDICATOR_RED' and
            changed['current_verified_condition'] == 'INDICATOR_GREEN' and
            changed['incident'] is not None,
            {'outcome': changed['outcome'],
             'previous_condition': changed['previous_verified_condition'],
             'current_condition': changed['current_verified_condition'],
             'change_id': changed['change_id'],
             'incident_id': changed['incident_id'],
             'latency_ms': changed_ms})

        exchange = workspace / 'temporal_exchange'
        red_path = workspace / 'red_fusion.json'
        green_path = workspace / 'green_fusion.json'
        _write_json(red_path, red_baseline)
        _write_json(green_path, green_change)
        run_fusion(red_path, exchange)
        confirmed, confirmed_ms = _timed(
            lambda: run_fusion(green_path, exchange))
        timing['temporal_evaluation_ms'].append(confirmed_ms)
        incident_dir = exchange / 'phase5/incidents'
        incident_count_before = len(list(incident_dir.glob('*.json')))
        replay, replay_ms = _timed(lambda: run_fusion(green_path, exchange))
        timing['temporal_evaluation_ms'].append(replay_ms)
        incident_count_after = len(list(incident_dir.glob('*.json')))
        duplicate_incidents = max(0, incident_count_after - incident_count_before)
        replay_pass = (
            replay['incident_id'] == confirmed['incident_id'] and
            incident_count_before == 1 and incident_count_after == 1 and
            duplicate_incidents == 0)
        temporal_records.append(('H_replay_same_incident', replay))
        scenario_result(
            'H_replay_same_incident', replay_pass,
            {'outcome': replay['outcome'],
             'incident_id': replay['incident_id'],
             'incident_count_before_replay': incident_count_before,
             'incident_count_after_replay': incident_count_after,
             'duplicate_incident_count': duplicate_incidents,
             'idempotent_replay': replay_pass,
             'latency_ms': replay_ms})

        incident_path = Path(confirmed['incident_path'])
        temporal_path = Path(confirmed['result_path'])
        incident_before = incident_path.read_bytes()
        phase6_root = workspace / 'phase6'
        package_dir, package_ms = _timed(
            lambda: create_local_package(
                incident_path, green_path, temporal_path, phase6_root))
        timing['phase6_local_package_creation_ms'].append(package_ms)
        first_package = (package_dir / 'package.json').read_bytes()
        create_local_package(
            incident_path, green_path, temporal_path, phase6_root)
        second_package = (package_dir / 'package.json').read_bytes()
        package_value = json.loads(first_package.decode('utf-8'))
        package_generation_success = package_dir.is_dir()

        success_client = MemoryS3()
        sync_package(
            package_dir, 'phase7-memory-bucket', output_root=phase6_root,
            s3_client=success_client)
        logical_keys_after_first = set(success_client.objects)
        sync_package(
            package_dir, 'phase7-memory-bucket', output_root=phase6_root,
            s3_client=success_client)
        duplicate_cloud_incidents = int(
            set(success_client.objects) != logical_keys_after_first)

        failed = sync_package(
            package_dir, 'phase7-unavailable-bucket', output_root=phase6_root,
            s3_client=MemoryS3(fail=True))
        local_preserved = (
            incident_path.read_bytes() == incident_before and
            (package_dir / 'package.json').read_bytes() == second_package)
        package_deterministic = first_package == second_package
        cloud_pass = (
            failed['sync_state'] == 'SYNC_FAILED' and local_preserved and
            package_deterministic and duplicate_cloud_incidents == 0)
        scenario_result(
            'I_cloud_unavailable_local_preserved', cloud_pass,
            {'sync_state': failed['sync_state'],
             'failure_reason': failed['failure_reason'],
             'local_evidence_preserved': local_preserved,
             'package_generation_success': package_generation_success,
             'package_sha256': package_value['package_sha256'],
             'deterministic_package_hash_consistency': package_deterministic,
             'duplicate_cloud_incident_count': duplicate_cloud_incidents,
             'latency_ms': package_ms})

    scenario_order = {
        scenario['id']: index
        for index, scenario in enumerate(catalog['scenarios'])
    }
    results.sort(key=lambda item: scenario_order[item['id']])
    perception_total = len(perception_records)
    perception_correct = sum(
        evidence['condition'] == expected
        for _, expected, evidence in perception_records)
    unknown_count = sum(
        evidence['condition'] == 'UNKNOWN'
        for _, _, evidence in perception_records)
    sufficient_count = sum(
        evidence['evidence_quality'] == 'SUFFICIENT'
        for _, _, evidence in perception_records)

    fusion_total = len(fusion_records)
    false_verified = sum(
        negative and value['verification'] == 'VERIFIED'
        for _, value, negative in fusion_records)
    temporal_no_change = [
        value for identifier, value in temporal_records
        if identifier == 'F_repeated_unchanged_condition']
    temporal_changes = [
        value for identifier, value in temporal_records
        if identifier in ('G_red_to_green_confirmed_change',
                          'H_replay_same_incident')]

    metrics = {
        'perception': {
            'case_count': perception_total,
            'correct_indicator_classification_rate': _rate(
                perception_correct, perception_total),
            'unknown_rate': _rate(unknown_count, perception_total),
            'sufficient_evidence_rate': _rate(
                sufficient_count, perception_total),
            'insufficient_evidence_rate': _rate(
                perception_total - sufficient_count, perception_total),
        },
        'active_reobservation': {
            'case_count': 1,
            'primary_score': primary['confidence'],
            'primary_quality': primary['evidence_quality'],
            'secondary_score': red['confidence'],
            'secondary_quality': red['evidence_quality'],
            'score_improvement': improvement,
            'reobservation_success_rate': 1.0 if reobservation_pass else 0.0,
        },
        'fusion': {
            'case_count': fusion_total,
            'consistent_rate': _rate(sum(
                value['fusion_status'] == 'CONSISTENT'
                for _, value, _ in fusion_records), fusion_total),
            'conflict_rate': _rate(sum(
                value['fusion_status'] == 'CONFLICT'
                for _, value, _ in fusion_records), fusion_total),
            'insufficient_rate': _rate(sum(
                value['fusion_status'] == 'INSUFFICIENT'
                for _, value, _ in fusion_records), fusion_total),
            'verified_rate': _rate(sum(
                value['verification'] == 'VERIFIED'
                for _, value, _ in fusion_records), fusion_total),
            'false_verified_count': false_verified,
        },
        'temporal': {
            'case_count': len(temporal_records),
            'no_change_correctness': _rate(sum(
                value['outcome'] == 'NO_CHANGE'
                for value in temporal_no_change), len(temporal_no_change)),
            'confirmed_change_correctness': _rate(sum(
                value['outcome'] == 'CONFIRMED_CHANGE'
                for value in temporal_changes), len(temporal_changes)),
            'false_incident_count': sum(
                value.get('incident') is not None
                for value in temporal_no_change),
            'duplicate_incident_count': duplicate_incidents,
            'idempotent_replay_correctness': 1.0 if replay_pass else 0.0,
        },
        'cloud_local_first': {
            'case_count': 1,
            'package_generation_success_rate':
                1.0 if package_generation_success else 0.0,
            'deterministic_package_hash_consistency': package_deterministic,
            'idempotent_reupload_correctness':
                duplicate_cloud_incidents == 0,
            'safe_local_preservation_on_cloud_failure': local_preserved,
            'observed_failure_state': failed['sync_state'],
        },
    }
    timing_summary = {
        name: {
            'samples': len(values),
            'mean_ms': _mean(values),
            'min_ms': round(min(values), 6) if values else 0.0,
            'max_ms': round(max(values), 6) if values else 0.0,
            'measurements_ms': values,
        }
        for name, values in timing.items()
    }
    limitations = [
        'Evaluation uses a small deterministic suite, not a population-scale accuracy benchmark.',
        'Visual inputs are Unity-rendered simulated observations; no physical CCTV or drone was measured.',
        'Latency is a single local-machine run and is not a statistically stable performance benchmark.',
        'No physical drone movement latency is measured.',
        'No real AWS upload, Lambda execution, CloudWatch delivery, or AWS latency is measured.',
        'Cloud failure and S3 replay behavior use an in-memory test transport.',
    ]
    summary = {
        'schema_version': 1,
        'suite': 'VISIONGUARD_PHASE7_EVALUATION',
        'ground_truth_kind': catalog['ground_truth_kind'],
        'scenario_count': len(results),
        'passed_scenarios': sum(item['passed'] for item in results),
        'failed_scenarios': sum(not item['passed'] for item in results),
        'all_passed': all(item['passed'] for item in results),
        'scenarios': results,
        'metrics': metrics,
        'timing': timing_summary,
        'known_limitations': limitations,
    }
    output_root = Path(output_root)
    results_root = output_root / 'results'
    results_root.mkdir(parents=True, exist_ok=True)
    for result in results:
        _write_json(results_root / (result['id'] + '.json'), result)
    _write_json(output_root / 'summary.json', summary)
    write_report(summary, report_path)
    return summary


def write_report(summary, path):
    metrics = summary['metrics']
    timing = summary['timing']
    lines = [
        '# VISIONGUARD Phase 7 — Evaluation Report',
        '',
        '## Result',
        '',
        f"- Scenarios: {summary['scenario_count']}",
        f"- Passed: {summary['passed_scenarios']}",
        f"- Failed: {summary['failed_scenarios']}",
        f"- Overall: {'PASS' if summary['all_passed'] else 'FAIL'}",
        '- Ground truth: explicit simulated ground truth stored separately from predictions',
        '',
        '## Scenario results',
        '',
    ]
    lines.extend(
        f"- {item['id']}: {'PASS' if item['passed'] else 'FAIL'}"
        for item in summary['scenarios'])
    lines += [
        '',
        '## Metrics',
        '',
        f"- Perception classification rate: {metrics['perception']['correct_indicator_classification_rate']:.6f}",
        f"- Perception UNKNOWN rate: {metrics['perception']['unknown_rate']:.6f}",
        f"- Evidence sufficient/insufficient rates: {metrics['perception']['sufficient_evidence_rate']:.6f} / {metrics['perception']['insufficient_evidence_rate']:.6f}",
        f"- Re-observation score improvement: {metrics['active_reobservation']['score_improvement']:.6f}",
        f"- Re-observation success rate: {metrics['active_reobservation']['reobservation_success_rate']:.6f}",
        f"- Fusion CONSISTENT/CONFLICT/INSUFFICIENT rates: {metrics['fusion']['consistent_rate']:.6f} / {metrics['fusion']['conflict_rate']:.6f} / {metrics['fusion']['insufficient_rate']:.6f}",
        f"- Fusion VERIFIED rate: {metrics['fusion']['verified_rate']:.6f}",
        f"- False VERIFIED count: {metrics['fusion']['false_verified_count']}",
        f"- Temporal NO_CHANGE correctness: {metrics['temporal']['no_change_correctness']:.6f}",
        f"- Temporal CONFIRMED_CHANGE correctness: {metrics['temporal']['confirmed_change_correctness']:.6f}",
        f"- False/duplicate incident counts: {metrics['temporal']['false_incident_count']} / {metrics['temporal']['duplicate_incident_count']}",
        f"- Temporal replay idempotency correctness: {metrics['temporal']['idempotent_replay_correctness']:.6f}",
        f"- Phase 6 package generation success: {metrics['cloud_local_first']['package_generation_success_rate']:.6f}",
        f"- Phase 6 deterministic package hash: {metrics['cloud_local_first']['deterministic_package_hash_consistency']}",
        f"- Phase 6 idempotent re-upload: {metrics['cloud_local_first']['idempotent_reupload_correctness']}",
        f"- Safe local preservation on cloud failure: {metrics['cloud_local_first']['safe_local_preservation_on_cloud_failure']}",
        '',
        '## Local timing',
        '',
    ]
    for name, value in timing.items():
        lines.append(
            f"- {name}: mean {value['mean_ms']:.6f} ms "
            f"({value['samples']} sample(s), min {value['min_ms']:.6f}, "
            f"max {value['max_ms']:.6f})")
    lines += [
        '',
        'These timings exclude physical drone movement and AWS latency.',
        '',
        '## Known limitations',
        '',
    ]
    lines.extend('- ' + item for item in summary['known_limitations'])
    lines += [
        '',
        'Machine-readable details: `evaluation/phase7/summary.json`.',
        '',
    ]
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text('\n'.join(lines), encoding='utf-8')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--scenarios', type=Path, default=DEFAULT_SCENARIOS)
    parser.add_argument('--output', type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument('--report', type=Path, default=DEFAULT_REPORT)
    args = parser.parse_args(argv)
    summary = run_evaluation(args.scenarios, args.output, args.report)
    print(json.dumps({
        'ok': summary['all_passed'],
        'scenario_count': summary['scenario_count'],
        'passed': summary['passed_scenarios'],
        'failed': summary['failed_scenarios'],
        'summary_path': str(Path(args.output) / 'summary.json'),
        'report_path': str(args.report),
    }, sort_keys=True))
    return 0 if summary['all_passed'] else 2


if __name__ == '__main__':
    raise SystemExit(main())
