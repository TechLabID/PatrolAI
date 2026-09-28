"""One-shot local manifest -> evidence CLI. Errors emit no condition/evidence."""
import argparse
import json
import os
from pathlib import Path
import sys
import uuid
import cv2
from camera.adapters.unity import UnityCameraAdapter, read_manifest
from camera.protocol.observation import ObservationError
from vision.opencv.frames import load_frame
from vision.detection.indicator import analyze_indicator


def event(name, **fields):
    print(json.dumps(dict(event=name, **fields), allow_nan=False), file=sys.stderr)


def atomic_write(path, data):
    temporary = path.with_name(path.name + '.' + uuid.uuid4().hex + '.tmp')
    try:
        temporary.write_bytes(data)
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)


def process(manifest, output, *, fixture=False, annotated=False):
    if fixture:
        observation, path = read_manifest(manifest)
        if observation.source_type != 'synthetic_fixture':
            raise ObservationError('--fixture accepts synthetic fixtures only')
    else:
        observation, path = UnityCameraAdapter(manifest).read_observation()
    frame = load_frame(observation, path)
    evidence = analyze_indicator(observation, frame).to_dict()
    output = Path(output)
    output.mkdir(parents=True, exist_ok=True)
    # Raw observation remains untouched. JSON does not depend on visualization.
    if annotated:
        debug = frame.copy()
        x,y,w,h = evidence['roi']
        cv2.rectangle(debug, (x,y), (x+w-1,y+h-1), (255,255,0), 1)
        cv2.putText(debug, evidence['condition'], (max(0,x-20), max(15,y-8)),
                    cv2.FONT_HERSHEY_SIMPLEX, .5, (255,255,0), 1)
        ok, png = cv2.imencode('.png', debug)
        if not ok:
            raise ObservationError('Failed to encode debug image')
        atomic_write(output / (observation.observation_id+'.annotated.png'), png.tobytes())
    destination = output / (observation.observation_id+'.evidence.json')
    atomic_write(destination, (json.dumps(evidence, indent=2, allow_nan=False)+'\n').encode())
    event('OPENCV_EVIDENCE_CREATED', observation_id=observation.observation_id,
          source_type=observation.source_type, evidence=str(destination))
    return evidence


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('observation', type=Path)
    parser.add_argument('--output', type=Path, default=Path('exchange/evidence'))
    parser.add_argument('--fixture', action='store_true', help='Explicit synthetic-test input; not Unity validation')
    parser.add_argument('--annotated', action='store_true')
    args = parser.parse_args()
    try:
        process(args.observation, args.output, fixture=args.fixture, annotated=args.annotated)
        return 0
    except (ObservationError, OSError, ValueError, cv2.error) as exc:
        event('PERCEPTION_FAILED', error_type=type(exc).__name__, message=str(exc))
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
