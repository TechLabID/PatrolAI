"""Build deterministic Phase 6 packages from sealed Phase 5 outputs."""

from __future__ import annotations

import copy
import hashlib
import json
import os
from pathlib import Path
import uuid


class PackageError(ValueError):
    """The supplied Phase 5 incident evidence is incomplete or inconsistent."""


REQUIRED_INCIDENT_FIELDS = (
    'incident_id',
    'target_id',
    'previous_condition',
    'new_condition',
    'change_id',
    'verification_status',
    'supporting_fusion_ids',
    'supporting_observation_ids',
    'source_ids',
    'previous_verified_at',
    'confirmed_at',
    'schema_version',
)


def canonical_json(value):
    return json.dumps(
        value, sort_keys=True, separators=(',', ':'), allow_nan=False)


def package_digest(value):
    unsigned = {
        key: item for key, item in value.items()
        if key != 'package_sha256'
    }
    return hashlib.sha256(
        canonical_json(unsigned).encode('utf-8')).hexdigest()


def _require_string(value, field):
    if not isinstance(value, str) or not value:
        raise PackageError('invalid_or_missing_' + field)


def _require_string_list(value, field):
    if (not isinstance(value, list) or not value or
            any(not isinstance(item, str) or not item for item in value)):
        raise PackageError('invalid_or_missing_' + field)


def _read_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8'))


def _trace_references(fusion, temporal):
    references = {}
    for output_name, source, source_name in (
            ('fusion_trace_path', fusion, 'trace_path'),
            ('fusion_result_path', fusion, 'result_path'),
            ('temporal_result_path', temporal, 'result_path'),
            ('temporal_state_path', temporal, 'state_path'),
            ('incident_path', temporal, 'incident_path')):
        value = source.get(source_name)
        if isinstance(value, str) and value:
            references[output_name] = value
    return references


def build_incident_package(incident, fusion, temporal):
    """Return a deterministic package made only from supplied Phase 4/5 facts."""
    if not isinstance(incident, dict):
        raise PackageError('incident_must_be_an_object')
    if not isinstance(fusion, dict):
        raise PackageError('fusion_result_must_be_an_object')
    if not isinstance(temporal, dict):
        raise PackageError('temporal_result_must_be_an_object')

    for field in REQUIRED_INCIDENT_FIELDS:
        if field not in incident:
            raise PackageError('invalid_or_missing_' + field)
    for field in (
            'incident_id', 'target_id', 'previous_condition', 'new_condition',
            'change_id', 'verification_status', 'previous_verified_at',
            'confirmed_at'):
        _require_string(incident[field], field)
    for field in (
            'supporting_fusion_ids', 'supporting_observation_ids', 'source_ids'):
        _require_string_list(incident[field], field)
    if type(incident['schema_version']) is not int:
        raise PackageError('invalid_or_missing_schema_version')

    if temporal.get('outcome') != 'CONFIRMED_CHANGE':
        raise PackageError('confirmed_change_temporal_result_required')
    for field in ('incident_id', 'target_id', 'change_id'):
        if temporal.get(field) != incident[field]:
            raise PackageError('temporal_' + field + '_mismatch')
    if temporal.get('previous_verified_condition') != incident['previous_condition']:
        raise PackageError('temporal_previous_condition_mismatch')
    if temporal.get('current_verified_condition') != incident['new_condition']:
        raise PackageError('temporal_current_condition_mismatch')

    if fusion.get('target_id') != incident['target_id']:
        raise PackageError('fusion_target_id_mismatch')
    if fusion.get('session_id') not in incident['supporting_fusion_ids']:
        raise PackageError('supporting_fusion_id_mismatch')
    if fusion.get('verification') != incident['verification_status']:
        raise PackageError('fusion_verification_status_mismatch')
    if fusion.get('final_condition') != incident['new_condition']:
        raise PackageError('fusion_condition_mismatch')

    provenance = fusion.get('provenance')
    if not isinstance(provenance, list):
        raise PackageError('fusion_provenance_required')
    wanted_observations = set(incident['supporting_observation_ids'])
    evidence = [
        copy.deepcopy(item) for item in provenance
        if isinstance(item, dict)
        and item.get('observation_id') in wanted_observations
    ]
    found_observations = {item.get('observation_id') for item in evidence}
    if found_observations != wanted_observations:
        raise PackageError('supporting_observation_metadata_missing')
    found_sources = {item.get('camera_id') for item in evidence}
    if found_sources != set(incident['source_ids']):
        raise PackageError('supporting_source_metadata_mismatch')

    package = {
        'schema_version': incident['schema_version'],
        'incident_id': incident['incident_id'],
        'target_id': incident['target_id'],
        'previous_condition': incident['previous_condition'],
        'new_condition': incident['new_condition'],
        'change_id': incident['change_id'],
        'verification_status': incident['verification_status'],
        'supporting_fusion_ids': copy.deepcopy(
            incident['supporting_fusion_ids']),
        'supporting_observation_ids': copy.deepcopy(
            incident['supporting_observation_ids']),
        'source_ids': copy.deepcopy(incident['source_ids']),
        'timestamps': {
            'previous_verified_at': incident['previous_verified_at'],
            'confirmed_at': incident['confirmed_at'],
        },
        'trace_references': _trace_references(fusion, temporal),
        'supporting_evidence_metadata': evidence,
        'source_documents': {
            'incident': copy.deepcopy(incident),
            'fusion_result': copy.deepcopy(fusion),
            'temporal_result': copy.deepcopy(temporal),
        },
    }
    package['package_sha256'] = package_digest(package)
    validate_package(package)
    return package


def validate_package(package):
    """Validate the portable package contract used locally and by Lambda."""
    if not isinstance(package, dict):
        raise PackageError('package_must_be_an_object')
    for field in (
            'incident_id', 'target_id', 'previous_condition', 'new_condition',
            'change_id', 'verification_status', 'package_sha256'):
        _require_string(package.get(field), field)
    if type(package.get('schema_version')) is not int:
        raise PackageError('invalid_or_missing_schema_version')
    for field in (
            'supporting_fusion_ids', 'supporting_observation_ids', 'source_ids'):
        _require_string_list(package.get(field), field)
    if package['verification_status'] != 'VERIFIED':
        raise PackageError('verified_incident_required')
    timestamps = package.get('timestamps')
    if not isinstance(timestamps, dict):
        raise PackageError('timestamps_required')
    _require_string(timestamps.get('previous_verified_at'),
                    'previous_verified_at')
    _require_string(timestamps.get('confirmed_at'), 'confirmed_at')
    if not isinstance(package.get('trace_references'), dict):
        raise PackageError('trace_references_required')
    if not isinstance(package.get('supporting_evidence_metadata'), list):
        raise PackageError('supporting_evidence_metadata_required')
    if not isinstance(package.get('source_documents'), dict):
        raise PackageError('source_documents_required')
    if package['package_sha256'] != package_digest(package):
        raise PackageError('package_sha256_mismatch')
    return package


def package_artifacts(package):
    validate_package(package)
    documents = package['source_documents']
    compact = lambda value: (
        json.dumps(value, indent=2, sort_keys=True, allow_nan=False) + '\n'
    ).encode('utf-8')
    return {
        'incident.json': compact(documents['incident']),
        'evidence.json': compact({
            'schema_version': package['schema_version'],
            'incident_id': package['incident_id'],
            'supporting_evidence_metadata':
                package['supporting_evidence_metadata'],
        }),
        'fusion.json': compact(documents['fusion_result']),
        'temporal.json': compact(documents['temporal_result']),
        'trace.json': compact({
            'schema_version': package['schema_version'],
            'incident_id': package['incident_id'],
            'timestamps': package['timestamps'],
            'trace_references': package['trace_references'],
        }),
        'package.json': compact(package),
    }


def atomic_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + '.' + uuid.uuid4().hex + '.tmp')
    try:
        temporary.write_text(
            json.dumps(value, indent=2, sort_keys=True, allow_nan=False) + '\n',
            encoding='utf-8')
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)


def create_local_package(incident_path, fusion_path, temporal_path,
                         output_root='exchange/phase6'):
    """Materialize an immutable local package beside, never over, Phase 5."""
    package = build_incident_package(
        _read_json(incident_path), _read_json(fusion_path),
        _read_json(temporal_path))
    root = Path(output_root)
    package_dir = root / 'packages' / package['incident_id']
    package_dir.mkdir(parents=True, exist_ok=True)
    for name, body in package_artifacts(package).items():
        destination = package_dir / name
        temporary = destination.with_name(
            destination.name + '.' + uuid.uuid4().hex + '.tmp')
        try:
            temporary.write_bytes(body)
            os.replace(temporary, destination)
        finally:
            temporary.unlink(missing_ok=True)
    atomic_json(root / 'sync' / (package['incident_id'] + '.json'), {
        'schema_version': 1,
        'incident_id': package['incident_id'],
        'package_sha256': package['package_sha256'],
        'sync_state': 'LOCAL_ONLY',
        's3_bucket': None,
        's3_prefix': None,
        's3_package_key': None,
        'lambda_processing_result': None,
        'last_attempt_at': None,
        'synced_at': None,
        'failure_reason': None,
    })
    return package_dir
