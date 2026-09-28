"""Read-only replay of the failed real frame; never writes exchange evidence."""
import copy
import json
import sys
from dataclasses import asdict
from pathlib import Path

import cv2
import numpy as np

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT))
from camera.protocol.observation import VisualObservation
from vision.opencv.frames import load_frame
from vision.detection.indicator import analyze_indicator

OUT = Path(__file__).parent
ID = 'obs_9988e2b9143f4525826558dbe2f46e03'
manifest = ROOT / 'exchange/observations' / (ID + '.json')
raw = json.loads(manifest.read_text())
observation = VisualObservation.from_dict(raw)
frame = load_frame(observation, manifest.with_suffix('.png'))
actual = asdict(analyze_indicator(observation, frame))
calibrated = copy.deepcopy(raw)
calibrated['metadata']['roi'] = [592, 312, 96, 96]
replay = asdict(analyze_indicator(VisualObservation.from_dict(calibrated), frame))
assert actual['evidence_quality'] == 'INSUFFICIENT'
assert replay['evidence_quality'] == 'SUFFICIENT'

# Mirror only the detector's red-mask visualization; the measurements above
# are produced by the unchanged production detector itself.
x, y, w, h = raw['metadata']['roi']
hsv = cv2.cvtColor(frame[y:y+h, x:x+w], cv2.COLOR_BGR2HSV)
mask = cv2.inRange(hsv, (0,90,65), (10,255,255)) | cv2.inRange(hsv, (170,90,65), (179,255,255))
mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, np.ones((3,3), np.uint8))
count, labels, stats, centers = cv2.connectedComponentsWithStats(mask, connectivity=8)
marked = frame.copy()
crop = marked[y:y+h, x:x+w]
crop[mask > 0] = (crop[mask > 0] * .45 + np.array([255,0,255]) * .55).astype(np.uint8)
for left, top, cw, ch, area in stats[1:]:
    cv2.rectangle(marked, (x+left,y+top), (x+left+cw-1,y+top+ch-1), (255,0,255), 1)
cv2.rectangle(marked, (592,312), (687,407), (0,255,0), 1)
cv2.rectangle(marked, (x,y), (x+w-1,y+h-1), (255,255,0), 2)
# SWG_05 red lamp world center (-1.77, 2.2, 5.27) lies on
# the recorded camera's -X optical axis. Projection is frame center.
cv2.drawMarker(marked, (640,360), (255,255,255), cv2.MARKER_CROSS, 16, 1)
inset = cv2.resize(marked[296:424,576:704], (384,384), interpolation=cv2.INTER_NEAREST)
canvas = np.zeros((900,1280,3), np.uint8)
canvas[:720] = marked
canvas[24:408,24:408] = inset
lines = [ID + ' | actual Unity PNG 1280x720 | OpenCV ' + cv2.__version__,
         'Cyan: actual ROI [604,324,72,72]; magenta: detected red mask/components',
         'White: expected target (640,360); green: existing calibration [592,312,96,96]',
         'Actual: red=4341px, fraction=0.837384, border=True, score=0.972415 -> INSUFFICIENT',
         'Same-frame replay only: 96x96 ROI -> ' + replay['evidence_quality'] + '; no new Unity capture']
for i, line in enumerate(lines):
    cv2.putText(canvas,line,(16,748+i*29),cv2.FONT_HERSHEY_SIMPLEX,.56,(240,240,240),1,cv2.LINE_AA)
assert cv2.imwrite(str(OUT/'secondary_capture_diagnostic.png'),canvas)
report = dict(original_manifest=raw, decoded_dimensions=[frame.shape[1],frame.shape[0]],
              sha256_verified=True, actual_detector=actual, calibrated_roi_replay_only=replay,
              red_components=stats[1:].tolist(), expected_target_pixel=[640,360],
              target_world_position=[-1.77,2.2,5.27],
              y_flip_note='Both centered ROIs are invariant under y -> 720-y-height; Y inversion cannot explain this failure.',
              instance_note='Scene controller references exporter 16892 on InspectionCamera 16888. Camera ID and recorded pose agree; manifest cannot identify an in-memory Unity instance.',
              cause='Exported ROI is still default 72x72, clipping the visible target and exceeding area_fraction <=0.65. Disk scene has 96x96; exact in-memory serialization/loading cause is not observable offline.')
(OUT/'diagnostic.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
