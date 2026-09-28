"""Offline preservation, serialization and attached-script checks; no Unity control."""
from pathlib import Path
import hashlib
import json
import re

ROOT=Path(__file__).resolve().parents[1]
baseline=ROOT/'Assets/VisionGuard/Phase1A/Scenes/VISIONGUARD_IndustrialRoom_RF90_v01.unity'
phase2=ROOT/'Assets/VisionGuard/Phase2/Scenes/VISIONGUARD_IndustrialRoom_Phase2_v01.unity'
hashes=json.loads((ROOT/'docs/Phase2_Baseline_Hashes.json').read_text())
for name,expected in hashes.items():
    assert hashlib.sha256((ROOT/name).read_bytes()).hexdigest()==expected, f'Protected file changed: {name}'
def blocks(path):
    return {int(re.search(r'&(\d+)',b).group(1)):b for b in re.split(r'(?=^--- !u!)',path.read_text(encoding='utf-8-sig'),flags=re.M) if b.startswith('--- !u!')}
old,new=blocks(baseline),blocks(phase2)
for key,block in old.items():
    if key!=9223372036854775807:
        assert new[key]==block,f'Baseline component changed: {key}'
text=phase2.read_text()
ids=re.findall(r'^--- !u!\d+ &(\d+)$',text,re.M)
assert len(ids)==len(set(ids))
assert not (set(re.findall(r'\{fileID: (\d+)\}',text))-set(ids)-{'0'})
assert text.count('m_Name: VG_CCTV_01\n')==1
assert text.count('m_Name: RF90_ReviewCamera_Main\n')==1
extra={k:v for k,v in new.items() if k not in old}
assert len(extra)==5, 'Only camera GameObject, Transform, Camera, URP data and exporter are allowed'
camera=next(v for v in extra.values() if v.startswith('--- !u!20 '))
assert 'm_Enabled: 0' in camera, 'CCTV must not take over Game view'
script=(ROOT/'Assets/VisionGuard/Phase2/Scripts/VGObservationExporter.cs').read_text()
assert not re.search(r'\bvoid (?:Update|FixedUpdate|Start|Awake|OnEnable)\(',script)
print(json.dumps(dict(protected_files=len(hashes),baseline_components_preserved=len(old)-1,
                     phase2_new_components=len(extra),offline_scene_validation='PASS',
                     Unity_runtime='NOT_EXECUTED'),indent=2))
