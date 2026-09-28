import copy
import json
from pathlib import Path
import shutil
import tempfile
import unittest

from agent.fusion.policy import EvidenceFusionPolicy, FusionError
from agent.fusion.run import run_manifests


ROOT = Path(__file__).resolve().parents[1]
REAL_EVIDENCE = ROOT / 'tests/fixtures/phase2/unity/obs_583f270b8e5a4465ab668f223a4a62db/captured_evidence.json'
REAL_MANIFEST = ROOT / 'tests/fixtures/phase2/unity/obs_583f270b8e5a4465ab668f223a4a62db/observation.json'


class Phase4FusionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = json.loads(REAL_EVIDENCE.read_text(encoding='utf-8'))

    def evidence(self, source, observation, condition='INDICATOR_RED',
                 quality='SUFFICIENT', score=0.92):
        value = copy.deepcopy(self.base)
        value.update(camera_id=source, observation_id=observation, condition=condition,
                     evidence_quality=quality, confidence=score)
        return value

    def test_case_a_agreement_is_consistent_and_verified(self):
        result = EvidenceFusionPolicy().fuse([
            self.evidence('VG_CCTV_01', 'obs_cctv_01'),
            self.evidence('VG_MOBILE_01', 'obs_mobile_01'),
        ])
        self.assertEqual(result['fusion_status'], 'CONSISTENT')
        self.assertEqual(result['verification'], 'VERIFIED')
        self.assertEqual(result['final_condition'], 'INDICATOR_RED')
        self.assertEqual(result['action'], 'RECORD')

    def test_case_b_disagreement_cannot_verify(self):
        result = EvidenceFusionPolicy().fuse([
            self.evidence('VG_CCTV_01', 'obs_cctv_01', 'INDICATOR_RED'),
            self.evidence('VG_CCTV_02', 'obs_cctv_02', 'INDICATOR_GREEN'),
            self.evidence('VG_MOBILE_01', 'obs_mobile_01', 'INDICATOR_RED'),
        ])
        self.assertEqual(result['fusion_status'], 'CONFLICT')
        self.assertEqual(result['verification'], 'UNRESOLVED')
        self.assertEqual(result['action'], 'REQUEST_ADDITIONAL_EVIDENCE')
        self.assertEqual(result['final_condition'], 'UNKNOWN')

    def test_ambiguous_records_request_more_evidence(self):
        result = EvidenceFusionPolicy().fuse([
            self.evidence('VG_CCTV_01', 'obs_cctv_01'),
            self.evidence('VG_CCTV_02', 'obs_cctv_02', 'UNKNOWN', 'INSUFFICIENT', 0.0),
        ])
        self.assertEqual(result['fusion_status'], 'INSUFFICIENT')
        self.assertEqual(result['verification'], 'UNRESOLVED')
        self.assertEqual(result['action'], 'REQUEST_ADDITIONAL_EVIDENCE')

    def test_provenance_is_preserved_without_invented_fields(self):
        evidence = self.evidence('VG_CCTV_01', 'obs_cctv_01', score=0.91)
        result = EvidenceFusionPolicy().fuse([
            evidence, self.evidence('VG_MOBILE_01', 'obs_mobile_01', score=0.94),
        ])
        first = result['provenance'][0]
        self.assertEqual(first, {
            'camera_id': evidence['camera_id'],
            'observation_id': evidence['observation_id'],
            'condition': evidence['condition'],
            'quality': evidence['evidence_quality'],
            'score': evidence['confidence'],
            'timestamp': evidence['timestamp'],
        })

    def test_duplicate_source_and_target_mismatch_are_rejected(self):
        policy = EvidenceFusionPolicy()
        with self.assertRaises(FusionError):
            policy.fuse([self.evidence('VG_CCTV_01', 'obs_1'),
                         self.evidence('VG_CCTV_01', 'obs_2')])
        different = self.evidence('VG_CCTV_02', 'obs_2')
        different['target_id'] = 'OTHER_TARGET'
        with self.assertRaises(FusionError):
            policy.fuse([self.evidence('VG_CCTV_01', 'obs_1'), different])

    def test_replay_manifests_use_existing_opencv_pipeline_before_fusion(self):
        source = json.loads(REAL_MANIFEST.read_text(encoding='utf-8'))
        source_png = REAL_MANIFEST.parent / source['frame']
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            manifests = []
            for index, camera_id in enumerate(('VG_CCTV_01', 'VG_MOBILE_01'), 1):
                frame_name = f'frame_{index}.png'
                shutil.copy2(source_png, root / frame_name)
                manifest = copy.deepcopy(source)
                manifest.update(observation_id=f'obs_phase4_replay_{index}',
                                camera_id=camera_id, frame=frame_name)
                manifest_path = root / f'observation_{index}.json'
                manifest_path.write_text(json.dumps(manifest), encoding='utf-8')
                manifests.append(manifest_path)
            result = run_manifests(manifests, 'fusion_' + 'a' * 32, root / 'exchange')
        self.assertEqual(result['source_count'], 2)
        self.assertEqual(result['fusion_status'], 'CONSISTENT')
        self.assertEqual(result['verification'], 'VERIFIED')


if __name__ == '__main__':
    unittest.main()
