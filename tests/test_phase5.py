import json
from pathlib import Path
import tempfile
import unittest

from agent.temporal.policy import TemporalPolicy
from agent.temporal.run import run_fusion


TARGET = 'SWG_05/Indicator_03'


def provenance(source, observation, condition, quality, score, timestamp):
    return {
        'camera_id': source,
        'observation_id': observation,
        'condition': condition,
        'quality': quality,
        'score': score,
        'timestamp': timestamp,
    }


def verified_fusion(session, condition, minute):
    timestamp = f'2026-09-28T00:{minute:02d}:00Z'
    return {
        'schema_version': 1,
        'target_id': TARGET,
        'session_id': session,
        'fusion_status': 'CONSISTENT',
        'verification': 'VERIFIED',
        'action': 'RECORD',
        'final_condition': condition,
        'reason': 'multiple_sufficient_sources_agree',
        'provenance': [
            provenance('VG_CCTV_02', f'obs_{session}_cctv', condition,
                       'SUFFICIENT', 0.91, timestamp),
            provenance('VG_MOBILE_01', f'obs_{session}_mobile', condition,
                       'SUFFICIENT', 0.94, timestamp),
        ],
    }


def candidate_fusion(session, condition, minute):
    timestamp = f'2026-09-28T00:{minute:02d}:00Z'
    return {
        'schema_version': 1,
        'target_id': TARGET,
        'session_id': session,
        'fusion_status': 'INSUFFICIENT',
        'verification': 'UNRESOLVED',
        'action': 'REQUEST_ADDITIONAL_EVIDENCE',
        'final_condition': 'UNKNOWN',
        'reason': 'fewer_than_two_sufficient_sources',
        'provenance': [
            provenance('VG_CCTV_02', f'obs_{session}_cctv', condition,
                       'SUFFICIENT', 0.91, timestamp),
            provenance('VG_MOBILE_01', f'obs_{session}_mobile', 'UNKNOWN',
                       'INSUFFICIENT', 0.0, timestamp),
        ],
    }


class Phase5TemporalTests(unittest.TestCase):
    def test_baseline_no_change_candidate_and_confirmed_change(self):
        policy = TemporalPolicy()

        baseline = policy.evaluate(
            verified_fusion('fusion_green_baseline', 'INDICATOR_GREEN', 1))
        self.assertEqual(baseline['outcome'], 'NO_CHANGE')
        self.assertEqual(
            baseline['state']['current_verified_condition'], 'INDICATOR_GREEN')

        repeated = policy.evaluate(
            verified_fusion('fusion_green_repeat', 'INDICATOR_GREEN', 2),
            baseline['state'])
        self.assertEqual(repeated['outcome'], 'NO_CHANGE')
        self.assertFalse(repeated['change_detected'])

        candidate = policy.evaluate(
            candidate_fusion('fusion_red_candidate', 'INDICATOR_RED', 3),
            repeated['state'])
        self.assertEqual(candidate['outcome'], 'CHANGE_CANDIDATE')
        self.assertEqual(candidate['candidate_condition'], 'INDICATOR_RED')
        self.assertFalse(candidate['change_detected'])
        self.assertIsNone(candidate['incident'])

        confirmed = policy.evaluate(
            verified_fusion('fusion_red_verified', 'INDICATOR_RED', 4),
            candidate['state'])
        self.assertEqual(confirmed['outcome'], 'CONFIRMED_CHANGE')
        self.assertTrue(confirmed['change_detected'])
        self.assertEqual(confirmed['change_id'], candidate['change_id'])
        self.assertEqual(
            confirmed['state']['previous_verified_condition'],
            'INDICATOR_GREEN')
        self.assertEqual(
            confirmed['state']['current_verified_condition'],
            'INDICATOR_RED')
        self.assertEqual(
            confirmed['incident']['supporting_fusion_ids'],
            ['fusion_red_verified'])
        self.assertEqual(
            confirmed['incident']['source_ids'],
            ['VG_CCTV_02', 'VG_MOBILE_01'])

    def test_insufficient_and_conflicting_inputs_never_confirm(self):
        policy = TemporalPolicy()
        baseline = policy.evaluate(
            verified_fusion('fusion_green_baseline', 'INDICATOR_GREEN', 1))

        insufficient = candidate_fusion(
            'fusion_weak_red', 'INDICATOR_RED', 2)
        for item in insufficient['provenance']:
            item.update(condition='UNKNOWN', quality='INSUFFICIENT', score=0.0)
        weak = policy.evaluate(insufficient, baseline['state'])
        self.assertEqual(weak['outcome'], 'UNRESOLVED')
        self.assertFalse(weak['change_detected'])
        self.assertIsNone(weak['incident'])

        conflict = candidate_fusion(
            'fusion_conflict', 'INDICATOR_RED', 3)
        conflict.update(
            fusion_status='CONFLICT',
            reason='sufficient_sources_disagree')
        conflict['provenance'][1].update(
            condition='INDICATOR_AMBER', quality='SUFFICIENT', score=0.9)
        unresolved = policy.evaluate(conflict, baseline['state'])
        self.assertEqual(unresolved['outcome'], 'UNRESOLVED')
        self.assertFalse(unresolved['change_detected'])
        self.assertIsNone(unresolved['incident'])
        self.assertEqual(
            unresolved['state']['current_verified_condition'],
            'INDICATOR_GREEN')

    def test_persistence_writes_phase5_state_result_and_one_incident(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            exchange = root / 'exchange'

            def execute(value):
                path = root / (value['session_id'] + '.json')
                path.write_text(json.dumps(value), encoding='utf-8')
                return run_fusion(path, exchange)

            execute(verified_fusion(
                'fusion_green_baseline', 'INDICATOR_GREEN', 1))
            execute(candidate_fusion(
                'fusion_red_candidate', 'INDICATOR_RED', 2))
            confirmed = execute(verified_fusion(
                'fusion_red_verified', 'INDICATOR_RED', 3))

            self.assertEqual(confirmed['outcome'], 'CONFIRMED_CHANGE')
            self.assertTrue(Path(confirmed['state_path']).is_file())
            self.assertTrue(Path(confirmed['result_path']).is_file())
            self.assertTrue(Path(confirmed['incident_path']).is_file())
            incidents = list((exchange / 'phase5' / 'incidents').glob('*.json'))
            self.assertEqual(len(incidents), 1)
            replay = execute(verified_fusion(
                'fusion_red_verified', 'INDICATOR_RED', 3))
            self.assertEqual(replay['incident_id'], confirmed['incident_id'])
            self.assertEqual(len(list(
                (exchange / 'phase5' / 'incidents').glob('*.json'))), 1)


if __name__ == '__main__':
    unittest.main()
