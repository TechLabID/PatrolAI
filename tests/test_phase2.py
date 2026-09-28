import copy
from dataclasses import replace
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import cv2
import numpy as np
from camera.protocol.observation import VisualObservation, ObservationError
from camera.adapters.unity import UnityCameraAdapter, read_manifest
from vision.opencv.frames import load_frame, verify_version
from vision.detection.indicator import analyze_indicator
from vision.opencv.run import process

ROOT = Path(__file__).resolve().parents[1]
FIXTURE = ROOT/'tests/fixtures/phase2/synthetic_red_001.json'


class PerceptionTests(unittest.TestCase):
    def setUp(self):
        self.value = json.loads(FIXTURE.read_text())
        self.observation, self.frame_path = read_manifest(FIXTURE)
        self.frame = load_frame(self.observation, self.frame_path)

    def test_opencv_major_five_and_no_fallback(self):
        self.assertEqual(verify_version().split('.')[0], '5')
        with patch.object(cv2, '__version__', '4.99.0'):
            with self.assertRaises(ObservationError): verify_version()

    def test_schema_rejects_invalid_metadata(self):
        changes = [('schema_version', 2), ('width', True), ('height', 0), ('timestamp', 'invalid'),
                   ('timestamp', '2026-09-27T12:00:00'), ('frame', '../escape.png'),
                   ('source_type', 'rtsp'), ('metadata', {}), ('camera_pose', {'position':[0,0,0]}),
                   ('frame_sha256', 'bad')]
        for key, value in changes:
            with self.subTest(key=key, value=value):
                doc=copy.deepcopy(self.value);doc[key]=value
                with self.assertRaises(ObservationError): VisualObservation.from_dict(doc)
        for roi in ([0,0,999,999],[-1,0,48,48],[0,0,7,7],[0,0,48.0,48]):
            doc=copy.deepcopy(self.value);doc['metadata']['roi']=roi
            with self.assertRaises(ObservationError): VisualObservation.from_dict(doc)

    def test_unit_quaternion_and_nonfinite_pose_rejected(self):
        for rotation in ([0,0,0,0],[0,0,0,float('nan')]):
            doc=copy.deepcopy(self.value)
            doc['camera_pose']={'position':[0,0,0], 'rotation_xyzw':rotation}
            with self.assertRaises(ObservationError): VisualObservation.from_dict(doc)

    def test_unity_contract_roundtrip_and_fixture_guard(self):
        # Protocol-shaped synthetic data validates the contract, not Unity rendering.
        doc=copy.deepcopy(self.value);doc['source_type']='unity'
        doc['metadata']['provenance']='unity_render'
        doc['camera_pose']={'position':[-.4,2.2,5.27],'rotation_xyzw':[0,-.7071068,0,.7071068]}
        with tempfile.TemporaryDirectory() as td:
            path=Path(td)/'capture.json';path.write_text(json.dumps(doc))
            result,_=UnityCameraAdapter(path).read_observation()
            self.assertEqual(result.camera_pose, doc['camera_pose'])
        with self.assertRaises(ObservationError): UnityCameraAdapter(FIXTURE).read_observation()

    def test_frame_loading(self):
        self.assertEqual(self.frame.shape, (180,320,3))
        self.assertEqual(self.frame.dtype, np.uint8)

    def test_red_fixture_is_deterministic(self):
        first=analyze_indicator(self.observation,self.frame).to_dict()
        self.assertEqual(first,analyze_indicator(self.observation,self.frame).to_dict())
        self.assertEqual(first['condition'],'INDICATOR_RED')
        self.assertEqual(first['evidence_quality'],'SUFFICIENT')
        self.assertGreaterEqual(first['confidence'],.6)
        self.assertEqual(first['source_type'],'synthetic_fixture')
        self.assertEqual(json.loads(json.dumps(first,allow_nan=False)),first)
        expected=json.loads((ROOT/'tests/fixtures/phase2/expected_red_evidence.json').read_text())
        self.assertEqual(first,expected)

    def test_green_and_amber_without_ground_truth_input(self):
        for x,color in ((61,'GREEN'),(136,'AMBER')):
            meta={**self.observation.metadata,'roi':[x,41,48,48]}
            result=analyze_indicator(replace(self.observation,metadata=meta),self.frame)
            self.assertEqual(result.condition,'INDICATOR_'+color)

    def test_blank_dark_and_colored_rectangle_are_unknown(self):
        for color in ((0,0,0),(140,140,140),(0,0,255)):
            frame=np.full_like(self.frame,color)
            result=analyze_indicator(self.observation,frame)
            self.assertEqual(result.condition,'UNKNOWN')
            self.assertEqual(result.evidence_quality,'INSUFFICIENT')
            self.assertEqual(result.confidence,0.)

    def test_ambiguous_two_colors_and_clipped_lamp_are_unknown(self):
        frame=np.zeros_like(self.frame)
        cv2.circle(frame,(224,65),9,(0,0,220),-1)
        cv2.circle(frame,(246,65),9,(0,220,0),-1)
        self.assertEqual(analyze_indicator(self.observation,frame).condition,'UNKNOWN')
        frame[:]=0;cv2.circle(frame,(212,65),13,(0,0,220),-1)
        self.assertEqual(analyze_indicator(self.observation,frame).condition,'UNKNOWN')

    def test_missing_corrupt_and_hash_mismatch(self):
        with tempfile.TemporaryDirectory() as td:
            path=Path(td)/'bad.png'
            with self.assertRaises(ObservationError): load_frame(self.observation,path)
            path.write_bytes(b'not PNG')
            with self.assertRaises(ObservationError): load_frame(self.observation,path)
            bad=replace(self.observation,frame_sha256=hashlib.sha256(path.read_bytes()).hexdigest())
            with self.assertRaises(ObservationError): load_frame(bad,path)
            # Valid header/dimensions with truncated PNG body exercises actual decoder failure.
            path.write_bytes(self.frame_path.read_bytes()[:40])
            bad=replace(self.observation,frame_sha256=hashlib.sha256(path.read_bytes()).hexdigest())
            with self.assertRaises(ObservationError): load_frame(bad,path)

    def test_dimensions_must_match_png(self):
        with self.assertRaises(ObservationError):
            load_frame(replace(self.observation,width=321),self.frame_path)

    def test_missing_corrupt_duplicate_manifest(self):
        with tempfile.TemporaryDirectory() as td:
            path=Path(td)/'bad.json'
            with self.assertRaises(ObservationError): read_manifest(path)
            for text in ('{', '[]', '{"width":1,"width":2}'):
                path.write_text(text)
                with self.assertRaises(ObservationError): read_manifest(path)

    def test_cli_outputs_and_safe_failure(self):
        with tempfile.TemporaryDirectory() as td:
            output=Path(td)/'results'
            raw_hash=hashlib.sha256(self.frame_path.read_bytes()).hexdigest()
            evidence=process(FIXTURE,output,fixture=True,annotated=True)
            self.assertEqual(json.loads((output/'synthetic_red_001.evidence.json').read_text()),evidence)
            self.assertTrue((output/'synthetic_red_001.annotated.png').is_file())
            self.assertEqual(raw_hash,hashlib.sha256(self.frame_path.read_bytes()).hexdigest())
            failed_output=Path(td)/'failed'
            run=subprocess.run([sys.executable,'-m','vision.opencv.run',str(Path(td)/'missing.json'),
                                '--output',str(failed_output)],cwd=ROOT,capture_output=True,text=True)
            self.assertEqual(run.returncode,2)
            self.assertEqual(json.loads(run.stderr)['event'],'PERCEPTION_FAILED')
            self.assertFalse(failed_output.exists())


if __name__=='__main__': unittest.main()
