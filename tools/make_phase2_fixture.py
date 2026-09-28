"""Synthetic color fixture, explicitly NOT a Unity render or integration proof."""
from pathlib import Path
import hashlib
import json
import cv2
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'tests/fixtures/phase2'
OUT.mkdir(parents=True,exist_ok=True)
frame=np.full((180,320,3),145,np.uint8)
cv2.rectangle(frame,(30,15),(290,165),(110,115,115),-1)
for x,color in ((85,(20,180,30)),(160,(0,160,245)),(235,(20,20,210))):
    cv2.circle(frame,(x,65),19,(25,25,25),-1)
    cv2.circle(frame,(x,65),13,color,-1)
    cv2.circle(frame,(x-3,61),3,(230,230,230),-1)
cv2.rectangle(frame,(55,105),(110,145),(40,40,40),-1)
ok,data=cv2.imencode('.png',frame)
assert ok
png=OUT/'synthetic_indicators.png'; png.write_bytes(data.tobytes())
manifest=dict(schema_version=1,observation_id='synthetic_red_001',camera_id='FIXTURE_01',
              timestamp='2026-09-27T00:00:00Z',source_type='synthetic_fixture',frame=png.name,
              frame_sha256=hashlib.sha256(png.read_bytes()).hexdigest(),width=320,height=180,
              camera_pose=None,metadata=dict(target_id='fixture/indicator_03',roi=[211,41,48,48],
                                            provenance='synthetic_test_only'))
(OUT/'synthetic_red_001.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Created labeled synthetic fixture; replace/add a captured Unity fixture after manual capture.')
