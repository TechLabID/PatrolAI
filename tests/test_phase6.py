import copy
import io
import json
from pathlib import Path
import tempfile
import unittest

from cloud.aws import lambda_handler
from cloud.aws.package import (
    build_incident_package,
    create_local_package,
    package_artifacts,
)
from cloud.aws.sync import sync_package


INCIDENT_ID = 'inc_0123456789abcdef01234567'
CHANGE_ID = 'chg_0123456789abcdef01234567'
FUSION_ID = 'fusion_0123456789abcdef0123456789abcdef'
TARGET = 'SWG_05/Indicator_03'


def phase5_documents():
    provenance = [
        {
            'camera_id': 'VG_CCTV_02',
            'observation_id': 'obs_green_cctv',
            'condition': 'INDICATOR_GREEN',
            'quality': 'SUFFICIENT',
            'score': 0.91,
            'timestamp': '2026-09-28T06:01:00Z',
        },
        {
            'camera_id': 'VG_MOBILE_01',
            'observation_id': 'obs_green_mobile',
            'condition': 'INDICATOR_GREEN',
            'quality': 'SUFFICIENT',
            'score': 0.94,
            'timestamp': '2026-09-28T06:01:01Z',
        },
    ]
    incident = {
        'schema_version': 1,
        'incident_id': INCIDENT_ID,
        'target_id': TARGET,
        'previous_condition': 'INDICATOR_RED',
        'new_condition': 'INDICATOR_GREEN',
        'change_id': CHANGE_ID,
        'supporting_fusion_ids': [FUSION_ID],
        'supporting_observation_ids': [
            'obs_green_cctv', 'obs_green_mobile'],
        'source_ids': ['VG_CCTV_02', 'VG_MOBILE_01'],
        'verification_status': 'VERIFIED',
        'previous_verified_at': '2026-09-28T06:00:00Z',
        'confirmed_at': '2026-09-28T06:01:01Z',
    }
    fusion = {
        'schema_version': 1,
        'target_id': TARGET,
        'session_id': FUSION_ID,
        'fusion_status': 'CONSISTENT',
        'verification': 'VERIFIED',
        'action': 'RECORD',
        'final_condition': 'INDICATOR_GREEN',
        'reason': 'multiple_sufficient_sources_agree',
        'provenance': provenance,
        'result_path': 'exchange/fusion/' + FUSION_ID + '.json',
        'trace_path': 'exchange/traces/' + FUSION_ID + '.jsonl',
    }
    temporal = {
        'schema_version': 1,
        'target_id': TARGET,
        'fusion_id': FUSION_ID,
        'outcome': 'CONFIRMED_CHANGE',
        'previous_verified_condition': 'INDICATOR_RED',
        'current_verified_condition': 'INDICATOR_GREEN',
        'change_id': CHANGE_ID,
        'incident_id': INCIDENT_ID,
        'incident': copy.deepcopy(incident),
        'result_path': 'exchange/phase5/results/temporal_green.json',
        'state_path': 'exchange/phase5/temporal/target.json',
        'incident_path': 'exchange/phase5/incidents/' + INCIDENT_ID + '.json',
    }
    return incident, fusion, temporal


class FakeS3:
    def __init__(self, fail=False):
        self.objects = {}
        self.fail = fail

    def put_object(self, Bucket, Key, Body, **kwargs):
        if self.fail:
            raise RuntimeError('simulated_cloud_unavailable')
        self.objects[(Bucket, Key)] = bytes(Body)
        return {'ETag': 'fake'}

    def get_object(self, Bucket, Key):
        body = self.objects.get((Bucket, Key))
        if body is None:
            error = RuntimeError('not found')
            error.response = {'Error': {'Code': 'NoSuchKey'}}
            raise error
        return {'Body': io.BytesIO(body)}


def write_inputs(root):
    incident, fusion, temporal = phase5_documents()
    paths = []
    for name, value in (
            ('incident.json', incident), ('fusion.json', fusion),
            ('temporal.json', temporal)):
        path = root / name
        path.write_text(json.dumps(value, indent=2), encoding='utf-8')
        paths.append(path)
    return paths


class Phase6AwsPipelineTests(unittest.TestCase):
    def test_package_creation_preserves_phase5_provenance(self):
        incident, fusion, temporal = phase5_documents()
        package = build_incident_package(incident, fusion, temporal)
        self.assertEqual(package['incident_id'], incident['incident_id'])
        self.assertEqual(package['supporting_fusion_ids'],
                         incident['supporting_fusion_ids'])
        self.assertEqual(package['supporting_observation_ids'],
                         incident['supporting_observation_ids'])
        self.assertEqual(package['source_ids'], incident['source_ids'])
        self.assertEqual(package['supporting_evidence_metadata'],
                         fusion['provenance'])
        self.assertEqual(package['timestamps'], {
            'previous_verified_at': incident['previous_verified_at'],
            'confirmed_at': incident['confirmed_at'],
        })

    def test_serialization_is_deterministic(self):
        incident, fusion, temporal = phase5_documents()
        first = build_incident_package(incident, fusion, temporal)
        second = build_incident_package(incident, fusion, temporal)
        self.assertEqual(first, second)
        self.assertEqual(package_artifacts(first), package_artifacts(second))

    def test_reupload_is_one_logical_incident(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            inputs = write_inputs(root)
            output = root / 'phase6'
            package_dir = create_local_package(*inputs, output)
            client = FakeS3()
            first = sync_package(
                package_dir, 'phase6-bucket', output_root=output,
                s3_client=client)
            second = sync_package(
                package_dir, 'phase6-bucket', output_root=output,
                s3_client=client)
            self.assertEqual(first['sync_state'], 'SYNC_PENDING')
            self.assertEqual(second['sync_state'], 'SYNC_PENDING')
            self.assertEqual(len(client.objects), 6)
            keys = [key for bucket, key in client.objects]
            self.assertTrue(all(INCIDENT_ID in key for key in keys))

    def test_failed_cloud_preserves_local_incident(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            inputs = write_inputs(root)
            incident_before = inputs[0].read_bytes()
            output = root / 'phase6'
            package_dir = create_local_package(*inputs, output)
            status = sync_package(
                package_dir, 'offline-bucket', output_root=output,
                s3_client=FakeS3(fail=True))
            self.assertEqual(status['sync_state'], 'SYNC_FAILED')
            self.assertEqual(status['failure_reason'],
                             'simulated_cloud_unavailable')
            self.assertEqual(inputs[0].read_bytes(), incident_before)
            self.assertTrue((package_dir / 'package.json').is_file())

    def test_lambda_validates_and_writes_one_stable_result(self):
        incident, fusion, temporal = phase5_documents()
        package = build_incident_package(incident, fusion, temporal)
        client = FakeS3()
        key = f'visionguard/incidents/{INCIDENT_ID}/package.json'
        client.objects[('phase6-bucket', key)] = package_artifacts(package)[
            'package.json']
        prior_client = lambda_handler._CLIENT
        lambda_handler._CLIENT = client
        try:
            event = {'Records': [{'s3': {
                'bucket': {'name': 'phase6-bucket'},
                'object': {'key': key},
            }}]}
            first = lambda_handler.handler(event, None)
            second = lambda_handler.handler(event, None)
        finally:
            lambda_handler._CLIENT = prior_client
        self.assertEqual(first['processed'][0]['validation_status'], 'VALID')
        self.assertEqual(second['processed'][0]['validation_status'], 'VALID')
        validation_key = key.replace('package.json', 'validation.json')
        self.assertIn(('phase6-bucket', validation_key), client.objects)
        self.assertEqual(len(client.objects), 2)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            package_dir = create_local_package(
                *write_inputs(root), root / 'phase6')
            status = sync_package(
                package_dir, 'phase6-bucket', output_root=root / 'phase6',
                wait_seconds=1, poll_interval=0, s3_client=client,
                sleep=lambda seconds: None)
        self.assertEqual(status['sync_state'], 'SYNCED')
        self.assertEqual(status['lambda_processing_result'], 'VALID')


if __name__ == '__main__':
    unittest.main()
