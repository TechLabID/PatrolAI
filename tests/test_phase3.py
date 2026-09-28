import copy
import cv2
import json
import math
from pathlib import Path
import shutil
import tempfile
import unittest

from agent.policy.evidence_policy import EvidencePolicy
from agent.reasoning.state_machine import ActiveInspectionStateMachine
from agent.reasoning.verification import verify_evidence
from agent.run import run_primary, run_secondary
from camera.adapters.unity import read_manifest
from vision.detection.indicator import analyze_indicator
from vision.opencv.frames import load_frame
from dataclasses import replace


ROOT = Path(__file__).resolve().parents[1]
REAL_EVIDENCE = ROOT/'tests/fixtures/phase2/unity/obs_583f270b8e5a4465ab668f223a4a62db/captured_evidence.json'
REAL_MANIFEST = ROOT/'tests/fixtures/phase2/unity/obs_583f270b8e5a4465ab668f223a4a62db/observation.json'


class Phase3Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.sufficient = json.loads(REAL_EVIDENCE.read_text())
        cls.insufficient = copy.deepcopy(cls.sufficient)
        cls.insufficient.update(condition='UNKNOWN', confidence=0.0, evidence_quality='INSUFFICIENT')
        cls.insufficient['measurements']['candidate_score'] = 0.24

    def test_sufficient_primary_does_not_request_reobservation(self):
        plan = ActiveInspectionStateMachine().assess_primary(self.sufficient)
        self.assertEqual(plan['state'], 'COMPLETE')
        self.assertEqual(plan['action'], 'RECORD')
        self.assertNotIn('REQUEST_REOBSERVATION', plan['state_path'])

    def test_insufficient_primary_requests_reobservation(self):
        assessment = EvidencePolicy().assess(self.insufficient)
        self.assertFalse(assessment.sufficient)
        self.assertEqual(assessment.action, 'REQUEST_REOBSERVATION')
        self.assertEqual(assessment.candidate_condition, 'INDICATOR_RED')

    def test_phase3_wider_fov_predicts_measured_uncertainty_on_real_unity_frame(self):
        observation, frame_path = read_manifest(REAL_MANIFEST)
        frame = load_frame(observation, frame_path)
        scale = math.tan(math.radians(36/2)) / math.tan(math.radians(60/2))
        reduced = cv2.resize(frame, None, fx=scale, fy=scale, interpolation=cv2.INTER_AREA)
        simulated = frame.copy()
        simulated[:] = 0
        y = (frame.shape[0]-reduced.shape[0])//2
        x = (frame.shape[1]-reduced.shape[1])//2
        simulated[y:y+reduced.shape[0], x:x+reduced.shape[1]] = reduced
        evidence = analyze_indicator(observation, simulated).to_dict()
        assessment = EvidencePolicy().assess(evidence)
        self.assertEqual(evidence['evidence_quality'], 'INSUFFICIENT')
        self.assertEqual(assessment.candidate_condition, 'INDICATOR_RED')
        self.assertEqual(assessment.action, 'REQUEST_REOBSERVATION')

    def test_secondary_sufficient_verifies_primary_candidate(self):
        result = verify_evidence(self.insufficient, self.sufficient)
        self.assertEqual(result['verification'], 'VERIFIED')
        self.assertEqual(result['final_condition'], 'INDICATOR_RED')

    def test_secondary_failure_or_ambiguity_is_unresolved(self):
        self.assertEqual(verify_evidence(self.insufficient, failure_reason='capture_failed')['verification'],
                         'UNRESOLVED')
        self.assertEqual(verify_evidence(self.insufficient, self.insufficient)['verification'], 'UNRESOLVED')

    def test_state_machine_reobservation_transition_path(self):
        first = ActiveInspectionStateMachine().assess_primary(self.insufficient)
        second = ActiveInspectionStateMachine().assess_secondary(self.insufficient, self.sufficient)
        self.assertEqual(first['state_path'], ['IDLE', 'OBSERVE_PRIMARY', 'ASSESS_PRIMARY',
                                               'REQUEST_REOBSERVATION'])
        self.assertEqual(second['state_path'], ['MOVE_INSPECTOR', 'OBSERVE_SECONDARY',
                                                'ASSESS_SECONDARY', 'VERIFY', 'COMPLETE'])

    def test_real_unity_replay_smoke_uses_existing_opencv_pipeline(self):
        with tempfile.TemporaryDirectory() as directory:
            session = 'inspection_' + 'a'*32
            plan = run_primary(REAL_MANIFEST, session, Path(directory)/'session.json',
                               Path(directory)/'trace.jsonl')
        self.assertEqual(plan['state'], 'COMPLETE')
        self.assertEqual(plan['final_condition'], 'INDICATOR_RED')

    def test_replay_transport_smoke_runs_both_agent_stages(self):
        source = json.loads(REAL_MANIFEST.read_text())
        source_png = REAL_MANIFEST.parent/source['frame']
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            shutil.copy2(source_png, root/'primary.png')
            shutil.copy2(source_png, root/'secondary.png')
            primary = copy.deepcopy(source)
            primary.update(observation_id='obs_phase3_primary_replay', frame='primary.png')
            primary['metadata']['roi'] = [550, 270, 180, 180]
            secondary = copy.deepcopy(source)
            secondary.update(observation_id='obs_phase3_secondary_replay', camera_id='VG_MOBILE_01',
                             frame='secondary.png')
            (root/'primary.json').write_text(json.dumps(primary))
            (root/'secondary.json').write_text(json.dumps(secondary))
            session_id = 'inspection_' + 'b'*32
            session_path, trace_path = root/'session.json', root/'trace.jsonl'
            first = run_primary(root/'primary.json', session_id, session_path, trace_path)
            second = run_secondary(root/'secondary.json', session_id, session_path, trace_path)
            events = [json.loads(line)['event'] for line in trace_path.read_text().splitlines()]
        self.assertEqual(first['state'], 'REQUEST_REOBSERVATION')
        self.assertEqual(second['verification'], 'VERIFIED')
        for event in ('REOBSERVATION_REQUESTED', 'ACTION_REQUESTED', 'ACTION_COMPLETED',
                      'SECONDARY_OBSERVATION_CAPTURED', 'VERIFICATION_RESULT'):
            self.assertIn(event, events)


if __name__ == '__main__':
    unittest.main()
