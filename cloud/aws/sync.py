"""Upload a local Phase 6 package without weakening local evidence safety."""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
import logging
from pathlib import Path
import time

from cloud.aws.package import (
    PackageError,
    atomic_json,
    create_local_package,
    package_artifacts,
    validate_package,
)


LOGGER = logging.getLogger('visionguard.phase6')


def _now():
    return datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z')


def _log(event, **fields):
    LOGGER.info(json.dumps(
        {'timestamp': _now(), 'event': event, **fields}, sort_keys=True))


def _status_path(output_root, incident_id):
    return Path(output_root) / 'sync' / (incident_id + '.json')


def _write_status(output_root, package, state, bucket=None, prefix=None,
                  lambda_result=None, failure_reason=None, attempted_at=None,
                  synced_at=None):
    key = f"{prefix.rstrip('/')}/{package['incident_id']}/package.json" \
        if bucket and prefix else None
    status = {
        'schema_version': 1,
        'incident_id': package['incident_id'],
        'package_sha256': package['package_sha256'],
        'sync_state': state,
        's3_bucket': bucket,
        's3_prefix': prefix,
        's3_package_key': key,
        'lambda_processing_result': lambda_result,
        'last_attempt_at': attempted_at,
        'synced_at': synced_at,
        'failure_reason': failure_reason,
    }
    atomic_json(_status_path(output_root, package['incident_id']), status)
    _log('AWS_SYNC_STATE', **status)
    return status


def _load_package(package_dir):
    path = Path(package_dir) / 'package.json'
    return validate_package(json.loads(path.read_text(encoding='utf-8')))


def _aws_client():
    try:
        import boto3
    except ImportError as exc:
        raise RuntimeError(
            'boto3_required_for_real_aws_sync') from exc
    return boto3.client('s3')


def _read_validation(client, bucket, key):
    try:
        response = client.get_object(Bucket=bucket, Key=key)
    except Exception as error:
        code = getattr(error, 'response', {}).get('Error', {}).get('Code')
        if code in ('NoSuchKey', '404', 'NotFound'):
            return None
        raise
    return json.loads(response['Body'].read().decode('utf-8'))


def sync_package(package_dir, bucket, prefix='visionguard/incidents',
                 output_root='exchange/phase6', wait_seconds=0,
                 poll_interval=1, s3_client=None, sleep=time.sleep):
    """Upload stable keys, then optionally await Lambda validation."""
    package = _load_package(package_dir)
    attempt = _now()
    _write_status(output_root, package, 'SYNC_PENDING', bucket, prefix,
                  lambda_result='PENDING', attempted_at=attempt)
    base_key = f"{prefix.rstrip('/')}/{package['incident_id']}"
    artifacts = package_artifacts(package)
    try:
        client = s3_client or _aws_client()
        # package.json is uploaded last because it is the Lambda trigger.
        for name in sorted(name for name in artifacts if name != 'package.json'):
            key = base_key + '/' + name
            client.put_object(
                Bucket=bucket, Key=key, Body=artifacts[name],
                ContentType='application/json',
                ServerSideEncryption='AES256',
                Metadata={
                    'incident-id': package['incident_id'],
                    'package-sha256': package['package_sha256'],
                })
            _log('S3_OBJECT_UPLOADED', incident_id=package['incident_id'],
                 bucket=bucket, key=key)
        package_key = base_key + '/package.json'
        client.put_object(
            Bucket=bucket, Key=package_key, Body=artifacts['package.json'],
            ContentType='application/json', ServerSideEncryption='AES256',
            Metadata={
                'incident-id': package['incident_id'],
                'package-sha256': package['package_sha256'],
            })
        _log('S3_OBJECT_UPLOADED', incident_id=package['incident_id'],
             bucket=bucket, key=package_key)

        deadline = time.monotonic() + max(0, wait_seconds)
        validation_key = base_key + '/validation.json'
        while wait_seconds > 0:
            validation = _read_validation(client, bucket, validation_key)
            if (validation and validation.get('package_sha256') ==
                    package['package_sha256']):
                result = validation.get('validation_status')
                if result == 'VALID':
                    return _write_status(
                        output_root, package, 'SYNCED', bucket, prefix,
                        lambda_result='VALID', attempted_at=attempt,
                        synced_at=_now())
                return _write_status(
                    output_root, package, 'SYNC_FAILED', bucket, prefix,
                    lambda_result=result or 'INVALID',
                    failure_reason=validation.get('reason'),
                    attempted_at=attempt)
            if time.monotonic() >= deadline:
                break
            sleep(min(poll_interval, max(0, deadline - time.monotonic())))
        return _write_status(
            output_root, package, 'SYNC_PENDING', bucket, prefix,
            lambda_result='PENDING', attempted_at=attempt)
    except Exception as error:
        return _write_status(
            output_root, package, 'SYNC_FAILED', bucket, prefix,
            lambda_result='UPLOAD_FAILED', failure_reason=str(error),
            attempted_at=attempt)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest='command', required=True)
    package_parser = subparsers.add_parser('package')
    package_parser.add_argument('--incident', type=Path, required=True)
    package_parser.add_argument('--fusion', type=Path, required=True)
    package_parser.add_argument('--temporal', type=Path, required=True)
    package_parser.add_argument('--output-root', type=Path,
                                default=Path('exchange/phase6'))
    sync_parser = subparsers.add_parser('sync')
    sync_parser.add_argument('--package-dir', type=Path, required=True)
    sync_parser.add_argument('--bucket', required=True)
    sync_parser.add_argument('--prefix', default='visionguard/incidents')
    sync_parser.add_argument('--output-root', type=Path,
                             default=Path('exchange/phase6'))
    sync_parser.add_argument('--wait-seconds', type=int, default=30)
    args = parser.parse_args(argv)
    logging.basicConfig(level=logging.INFO, format='%(message)s')
    try:
        if args.command == 'package':
            path = create_local_package(
                args.incident, args.fusion, args.temporal, args.output_root)
            print(json.dumps({'ok': True, 'package_dir': str(path),
                              'sync_state': 'LOCAL_ONLY'}))
            return 0
        status = sync_package(
            args.package_dir, args.bucket, args.prefix, args.output_root,
            args.wait_seconds)
        print(json.dumps({'ok': status['sync_state'] in
                          ('SYNCED', 'SYNC_PENDING'), **status}))
        return 0 if status['sync_state'] in ('SYNCED', 'SYNC_PENDING') else 2
    except (PackageError, OSError, ValueError, RuntimeError) as error:
        print(json.dumps({'ok': False, 'reason': str(error)}))
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
