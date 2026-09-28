"""S3-triggered Phase 6 validator; stdout is captured by CloudWatch Logs."""

from __future__ import annotations

from datetime import datetime, timezone
import json
from urllib.parse import unquote_plus

try:
    from .package import PackageError, validate_package
except ImportError:  # AWS Lambda CodeUri points directly at cloud/aws.
    from package import PackageError, validate_package


_CLIENT = None


def _client():
    global _CLIENT
    if _CLIENT is None:
        import boto3
        _CLIENT = boto3.client('s3')
    return _CLIENT


def _now():
    return datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z')


def _log(event, **fields):
    print(json.dumps(
        {'timestamp': _now(), 'event': event, **fields}, sort_keys=True))


def _validation_key(package_key):
    return package_key[:-len('package.json')] + 'validation.json'


def handler(event, context):
    processed = []
    client = _client()
    for record in event.get('Records', []):
        bucket = record['s3']['bucket']['name']
        key = unquote_plus(record['s3']['object']['key'])
        if not key.endswith('/package.json'):
            continue
        incident_id = key.split('/')[-2] if '/' in key else None
        package_sha256 = None
        status = 'INVALID'
        reason = None
        try:
            response = client.get_object(Bucket=bucket, Key=key)
            package = json.loads(response['Body'].read().decode('utf-8'))
            package_sha256 = package.get('package_sha256')
            validate_package(package)
            incident_id = package['incident_id']
            expected_key = '/'.join(key.split('/')[:-2] + [
                incident_id, 'package.json'])
            if key != expected_key:
                raise PackageError('incident_id_s3_key_mismatch')
            status = 'VALID'
        except (PackageError, ValueError, KeyError, json.JSONDecodeError) as error:
            reason = str(error)

        validation = {
            'schema_version': 1,
            'incident_id': incident_id,
            'package_sha256': package_sha256,
            'validation_status': status,
            'processed_at': _now(),
            'source_bucket': bucket,
            'source_key': key,
            'reason': reason,
        }
        validation_key = _validation_key(key)
        client.put_object(
            Bucket=bucket, Key=validation_key,
            Body=(json.dumps(validation, indent=2, sort_keys=True) + '\n')
                .encode('utf-8'),
            ContentType='application/json', ServerSideEncryption='AES256',
            Metadata={'incident-id': incident_id or 'unknown'})
        _log(
            'INCIDENT_PACKAGE_VALIDATED', incident_id=incident_id,
            bucket=bucket, key=key, validation_key=validation_key,
            validation_successful=status == 'VALID',
            lambda_processing_result=status, reason=reason)
        processed.append(validation)
    return {'statusCode': 200, 'processed': processed}
