"""Targeted offline checks for Phase 3 additions; does not operate Unity."""
from pathlib import Path
import hashlib
import json
import re

ROOT=Path(__file__).resolve().parents[1]
base=ROOT/'Assets/VisionGuard/Phase2/Scenes/VISIONGUARD_IndustrialRoom_Phase2_v01.unity'
scene=ROOT/'Assets/VisionGuard/Phase3/Scenes/VISIONGUARD_IndustrialRoom_Phase3_v01.unity'
hashes=json.loads((ROOT/'docs/Phase3_Baseline_Hashes.json').read_text())
for name,expected in hashes.items():
    path=ROOT/name
    assert path.is_file() and hashlib.sha256(path.read_bytes()).hexdigest()==expected, f'Protected file changed: {name}'
text=scene.read_text(encoding='utf-8-sig')
ids=re.findall(r'^--- !u!\d+ &(\d+)$',text,re.M)
assert len(ids)==len(set(ids)), 'Duplicate serialized object IDs'
internal=set(re.findall(r'\{fileID: (\d+)\}',text))-{'0'}
assert not (internal-set(ids)), f'Unresolved internal IDs: {sorted(internal-set(ids))[:5]}'
assert text.count('m_Name: RF90_ReviewCamera_Main\n')==1
assert text.count('m_Name: VG_CCTV_01\n')==1
assert text.count('m_Name: VG_MobileInspector_01\n')==1
assert text.count('m_Name: InspectionCamera\n')==1
assert text.count('m_Name: VG_Phase3_AgentController\n')==1
assert 'cameraId: VG_MOBILE_01' in text
assert 'primaryExporter: {fileID: 16749}' in text
primary_camera=re.search(r'--- !u!20 &16747\n(.*?)(?=--- !u!)',text,re.S).group(1)
assert 'field of view: 60' in primary_camera
assert 'm_XMin: 604\n    m_YMin: 324\n    m_Width: 72\n    m_Height: 72' in text
mobile_exporter=re.search(r'--- !u!114 &16892\n(.*?)(?=--- !u!)',text,re.S).group(1)
assert 'm_XMin: 592\n    m_YMin: 312\n    m_Width: 96\n    m_Height: 96' in mobile_exporter
base_text=base.read_text(encoding='utf-8-sig').replace('\r\n','\n')
assert base_text.count('m_Name: VG_MobileInspector_01\n')==0
assert hashlib.sha256(base.read_bytes()).hexdigest()==hashes[base.relative_to(ROOT).as_posix()]
scripts='\n'.join((ROOT/path).read_text() for path in (
    'Assets/VisionGuard/Phase3/Scripts/VGPhase3AgentController.cs',
    'Assets/VisionGuard/Phase3/Scripts/VGMobileInspectorMovement.cs'))
assert 'REQUEST_REOBSERVATION' in scripts and 'MOVE_TO_INSPECTION_POSE' in scripts
assert not re.search(r'\b(?:NavMesh|Rigidbody|AWS|HttpClient|Socket)\b',scripts)
print(json.dumps({'protected_files':len(hashes),'serialized_objects':len(ids),
                  'phase2_scene_sha256_preserved':True,'phase3_scene_integrity':'PASS',
                  'unity_editor_runtime':'NOT_EXECUTED'},indent=2))
