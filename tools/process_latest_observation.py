"""Consume the latest manually captured Unity observation; optionally save a replay fixture."""
from pathlib import Path
import argparse
import json
import shutil
import sys
import cv2

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from camera.adapters.unity import UnityCameraAdapter
from camera.protocol.observation import ObservationError
from vision.opencv.run import process, event


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--save-fixture',action='store_true')
    args=parser.parse_args()
    try:
        captures=list((ROOT/'exchange/observations').glob('*.json'))
        if not captures: raise ObservationError('No Unity capture yet. Invoke Capture One Observation in Unity Play Mode.')
        manifest=max(captures,key=lambda p:p.stat().st_mtime_ns)
        evidence=process(manifest,ROOT/'exchange/evidence',annotated=True)
        if args.save_fixture:
            observation,frame=UnityCameraAdapter(manifest).read_observation()
            dest=ROOT/'tests/fixtures/phase2/unity'/observation.observation_id
            if dest.exists(): raise ObservationError('Replay fixture already exists; not overwriting it')
            dest.mkdir(parents=True)
            shutil.copy2(frame,dest/frame.name)
            shutil.copy2(manifest,dest/'observation.json')
            (dest/'captured_evidence.json').write_text(json.dumps(evidence,indent=2)+'\n')
            print(f'Saved Unity replay fixture: {dest}')
        print(f"{evidence['condition']} | {evidence['evidence_quality']} | score={evidence['confidence']}")
        return 0
    except (OSError,ValueError,cv2.error) as exc:
        event('PERCEPTION_FAILED',error_type=type(exc).__name__,message=str(exc))
        return 2


if __name__=='__main__': raise SystemExit(main())
