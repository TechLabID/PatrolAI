"""Phase 3 one-shot coordinator stages for the Unity controller."""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import sys
import uuid

import cv2

from agent.reasoning.state_machine import ActiveInspectionStateMachine
from camera.protocol.observation import ObservationError
from vision.opencv.run import process


ROOT = Path(__file__).resolve().parents[1]


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


def response(plan, session_id, trace_path):
    result = plan.get('result') or {}
    return {
        'ok': True,
        'session_id': session_id,
        'state': plan['state'],
        'action': plan['action'],
        'reason': plan['reason'],
        'candidate_condition': plan['candidate_condition'],
        'verification': result.get('verification', ''),
        'final_condition': result.get('final_condition', ''),
        'primary_quality': result.get('primary_quality', ''),
        'secondary_quality': result.get('secondary_quality', ''),
        'trace_path': str(trace_path),
    }


def run_primary(manifest, session_id, session_path, trace_path):
    evidence = process(manifest, ROOT/'exchange/evidence', annotated=True)
    if evidence['camera_id'] != 'VG_CCTV_01':
        raise ObservationError('Primary observation must originate from VG_CCTV_01')
    trace(trace_path, 'OBSERVATION_CAPTURED', observation_id=evidence['observation_id'],
          camera_id=evidence['camera_id'])
    trace(trace_path, 'OPENCV_EVIDENCE_CREATED', observation_id=evidence['observation_id'],
          condition=evidence['condition'], quality=evidence['evidence_quality'],
          score=evidence['confidence'])
    plan = ActiveInspectionStateMachine().assess_primary(evidence)
    trace(trace_path, 'AGENT_DECISION', state=plan['state'], action=plan['action'],
          reason=plan['reason'], visual_evidence_id=evidence['observation_id'])
    if plan['state'] == 'REQUEST_REOBSERVATION':
        trace(trace_path, 'REOBSERVATION_REQUESTED', target_id=evidence['target_id'],
              selected_camera='VG_MOBILE_01', reason=plan['reason'])
        trace(trace_path, 'ACTION_REQUESTED', action='MOVE_TO_INSPECTION_POSE',
              platform='VG_MobileInspector_01')
    else:
        trace(trace_path, 'VERIFICATION_RESULT', **plan['result'])
    atomic_json(session_path, {'schema_version': 1, 'session_id': session_id,
                               'primary_evidence': evidence, 'plan': plan})
    value = response(plan, session_id, trace_path)
    value['primary_quality'] = evidence['evidence_quality']
    return value


def run_secondary(manifest, session_id, session_path, trace_path):
    if not session_path.is_file():
        raise ObservationError('Primary agent session does not exist')
    session = json.loads(session_path.read_text(encoding='utf-8'))
    if session.get('session_id') != session_id or session.get('plan', {}).get('state') != 'REQUEST_REOBSERVATION':
        raise ObservationError('Session did not request re-observation')
    evidence = process(manifest, ROOT/'exchange/evidence', annotated=True)
    if evidence['camera_id'] != 'VG_MOBILE_01':
        raise ObservationError('Secondary observation must originate from VG_MOBILE_01')
    if evidence['target_id'] != session['primary_evidence']['target_id']:
        raise ObservationError('Secondary observation target differs from primary target')
    trace(trace_path, 'ACTION_COMPLETED', action='MOVE_TO_INSPECTION_POSE',
          platform='VG_MobileInspector_01')
    trace(trace_path, 'SECONDARY_OBSERVATION_CAPTURED', observation_id=evidence['observation_id'],
          camera_id=evidence['camera_id'])
    trace(trace_path, 'OPENCV_EVIDENCE_CREATED', observation_id=evidence['observation_id'],
          condition=evidence['condition'], quality=evidence['evidence_quality'],
          score=evidence['confidence'])
    plan = ActiveInspectionStateMachine().assess_secondary(session['primary_evidence'], evidence)
    trace(trace_path, 'AGENT_DECISION', state='VERIFY', action='VERIFY', reason=plan['reason'],
          primary_observation=session['primary_evidence']['observation_id'],
          secondary_observation=evidence['observation_id'])
    trace(trace_path, 'VERIFICATION_RESULT', **plan['result'])
    session.update(secondary_evidence=evidence, final_plan=plan)
    atomic_json(session_path, session)
    return response(plan, session_id, trace_path)


def run_failure(session_id, session_path, trace_path, reason):
    if session_path.is_file():
        session = json.loads(session_path.read_text(encoding='utf-8'))
        primary = session.get('primary_evidence')
    else:
        session, primary = {'schema_version': 1, 'session_id': session_id}, None
    result = {
        'target_id': primary.get('target_id', '') if primary else '',
        'primary_observation': primary.get('observation_id', '') if primary else '',
        'secondary_observation': '',
        'primary_quality': primary.get('evidence_quality', 'FAILED') if primary else 'FAILED',
        'secondary_quality': 'FAILED', 'primary_candidate': 'UNKNOWN',
        'final_condition': 'UNKNOWN', 'verification': 'UNRESOLVED', 'reason': reason,
    }
    plan = {'state': 'COMPLETE', 'action': 'RECORD', 'reason': reason,
            'candidate_condition': 'UNKNOWN', 'state_path': ['FAILED', 'COMPLETE'], 'result': result}
    trace(trace_path, 'VERIFICATION_RESULT', **result)
    session['final_plan'] = plan
    atomic_json(session_path, session)
    return response(plan, session_id, trace_path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('stage', choices=('primary', 'secondary', 'fail'))
    parser.add_argument('--manifest', type=Path)
    parser.add_argument('--session', required=True)
    parser.add_argument('--reason', default='')
    args = parser.parse_args()
    if not re.fullmatch(r'inspection_[a-f0-9]{32}', args.session):
        print(json.dumps({'ok': False, 'reason': 'invalid_session_id'}))
        return 2
    session_path = ROOT/'exchange/agent'/(args.session+'.json')
    trace_path = ROOT/'exchange/traces'/(args.session+'.jsonl')
    try:
        if args.stage in ('primary', 'secondary') and args.manifest is None:
            raise ObservationError('Manifest is required for perception stages')
        if args.stage == 'primary':
            value = run_primary(args.manifest, args.session, session_path, trace_path)
        elif args.stage == 'secondary':
            value = run_secondary(args.manifest, args.session, session_path, trace_path)
        else:
            value = run_failure(args.session, session_path, trace_path, args.reason or 'unity_stage_failed')
        print(json.dumps(value, allow_nan=False))
        return 0
    except (ObservationError, OSError, ValueError, KeyError, json.JSONDecodeError, cv2.error) as exc:
        trace(trace_path, 'VERIFICATION_RESULT', verification='UNRESOLVED', reason=str(exc))
        print(json.dumps({'ok': False, 'session_id': args.session, 'state': 'FAILED',
                          'action': 'RECORD', 'reason': str(exc), 'candidate_condition': 'UNKNOWN',
                          'verification': 'UNRESOLVED', 'final_condition': 'UNKNOWN',
                          'primary_quality': '', 'secondary_quality': 'FAILED',
                          'trace_path': str(trace_path)}, allow_nan=False))
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
