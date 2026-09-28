"""Phase 4 manifest -> existing OpenCV evidence -> deterministic fusion CLI."""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import uuid

import cv2

from agent.fusion.policy import EvidenceFusionPolicy, FusionError
from camera.protocol.observation import ObservationError
from vision.opencv.run import process


ROOT = Path(__file__).resolve().parents[2]


def atomic_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + '.' + uuid.uuid4().hex + '.tmp')
    try:
        temporary.write_text(json.dumps(value, indent=2, allow_nan=False) + '\n', encoding='utf-8')
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)


def trace(path, event, **fields):
    record = {'timestamp': datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z'),
              'event': event, **fields}
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('a', encoding='utf-8') as stream:
        stream.write(json.dumps(record, allow_nan=False) + '\n')


def run_manifests(manifests, session_id, exchange_root=None):
    root = Path(exchange_root) if exchange_root else ROOT / 'exchange'
    trace_path = root / 'traces' / (session_id + '.jsonl')
    result_path = root / 'fusion' / (session_id + '.json')
    evidence_records = []

    for manifest in manifests:
        evidence = process(Path(manifest), root / 'evidence', annotated=True)
        evidence_records.append(evidence)
        trace(trace_path, 'FUSION_EVIDENCE_ACCEPTED',
              camera_id=evidence['camera_id'], observation_id=evidence['observation_id'],
              condition=evidence['condition'], quality=evidence['evidence_quality'],
              score=evidence['confidence'])

    result = EvidenceFusionPolicy().fuse(evidence_records)
    result['session_id'] = session_id
    result['result_path'] = str(result_path)
    result['trace_path'] = str(trace_path)
    trace(trace_path, 'EVIDENCE_FUSION_RESULT',
          fusion_status=result['fusion_status'], verification=result['verification'],
          action=result['action'], final_condition=result['final_condition'],
          reason=result['reason'])
    atomic_json(result_path, result)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=Path, action='append', required=True)
    parser.add_argument('--session', required=True)
    args = parser.parse_args()

    if not re.fullmatch(r'fusion_[a-f0-9]{32}', args.session):
        print(json.dumps({'ok': False, 'reason': 'invalid_session_id'}))
        return 2

    try:
        result = run_manifests(args.manifest, args.session)
        print(json.dumps({'ok': True, **result}, allow_nan=False))
        return 0
    except (FusionError, ObservationError, OSError, ValueError, KeyError,
            json.JSONDecodeError, cv2.error) as exc:
        print(json.dumps({'ok': False, 'session_id': args.session,
                          'fusion_status': 'INSUFFICIENT', 'verification': 'UNRESOLVED',
                          'action': 'REQUEST_ADDITIONAL_EVIDENCE',
                          'final_condition': 'UNKNOWN', 'reason': str(exc)}, allow_nan=False))
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
