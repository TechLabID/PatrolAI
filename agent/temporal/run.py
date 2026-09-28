"""Persist Phase 5 temporal state and incidents from a Phase 4 fusion JSON."""
import argparse
import json
import os
from pathlib import Path
import uuid

from agent.temporal.policy import TemporalError, TemporalPolicy


ROOT = Path(__file__).resolve().parents[2]


def atomic_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + '.' + uuid.uuid4().hex + '.tmp')
    try:
        temporary.write_text(
            json.dumps(value, indent=2, allow_nan=False) + '\n',
            encoding='utf-8')
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)


def run_fusion(fusion_path, exchange_root=None):
    root = Path(exchange_root) if exchange_root else ROOT / 'exchange'
    fusion = json.loads(Path(fusion_path).read_text(encoding='utf-8'))
    policy = TemporalPolicy()
    policy._validate_fusion(fusion)

    target_key = policy._id('target', fusion['target_id'])
    event_id = policy._id('temporal', fusion['session_id'])
    state_path = root / 'phase5' / 'temporal' / (target_key + '.json')
    result_path = root / 'phase5' / 'results' / (event_id + '.json')

    if result_path.exists():
        return json.loads(result_path.read_text(encoding='utf-8'))

    state = None
    if state_path.exists():
        state = json.loads(state_path.read_text(encoding='utf-8'))
    result = policy.evaluate(fusion, state)
    result['event_id'] = event_id
    result['state_path'] = str(state_path)
    result['result_path'] = str(result_path)
    result['incident_path'] = None

    incident = result.get('incident')
    if incident is not None:
        incident_path = (
            root / 'phase5' / 'incidents' /
            (incident['incident_id'] + '.json'))
        atomic_json(incident_path, incident)
        result['incident_path'] = str(incident_path)
    if result.get('state') is not None:
        atomic_json(state_path, result['state'])
    atomic_json(result_path, result)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fusion', type=Path, required=True)
    parser.add_argument('--exchange-root', type=Path)
    args = parser.parse_args()
    try:
        result = run_fusion(args.fusion, args.exchange_root)
        print(json.dumps({'ok': True, **result}, allow_nan=False))
        return 0
    except (TemporalError, OSError, ValueError, KeyError,
            json.JSONDecodeError) as exc:
        print(json.dumps({
            'ok': False,
            'outcome': 'UNRESOLVED',
            'reason': str(exc),
        }, allow_nan=False))
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
