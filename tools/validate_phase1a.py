"""Targeted offline integrity check. This is not a Unity import or visual test."""
from pathlib import Path
import re
import json
ROOT=Path(__file__).resolve().parents[1]
scene=ROOT/'Assets/VisionGuard/Phase1A/Scenes/VISIONGUARD_IndustrialRoom_RF90_v01.unity'
text=scene.read_text(encoding='utf-8')
ids=re.findall(r'^--- !u!\d+ &(\d+)$',text,re.M)
assert len(ids)==len(set(ids)), 'Duplicate serialized object IDs'
refs=re.findall(r'\{fileID: (\d+)\}',text)
assert not (set(refs)-set(ids)-{'0'}), 'Unresolved internal fileID'
assets={}
for base in (ROOT/'Assets',ROOT/'Library/PackageCache'):
    for path in base.rglob('*.meta'):
        match=re.search(r'^guid: ([a-f0-9]{32})',path.read_text(encoding='utf-8-sig',errors='replace'),re.M)
        if match: assets[match[1]]=path
guids=set(re.findall(r'guid: ([a-f0-9]{32})',text))
for mat in (ROOT/'Assets/VisionGuard/Phase1A/Materials').glob('*.mat'):
    guids.update(re.findall(r'guid: ([a-f0-9]{32})',mat.read_text()))
assert not (guids-set(assets)-{'0000000000000000e000000000000000'}), 'Unresolved asset GUID'
scripts=re.findall(r'm_Script: \{fileID: 11500000, guid: ([a-f0-9]+)',text)
assert set(scripts)=={'a79441f348de89743a2939f4d699eac1','474bcb49853aa07438625e644c072ee6'}, 'Unexpected behavior'
assert text.count('m_Name: RF90_ReviewCamera_Main\n')==1
assert len(re.findall(r'm_Name: (?:SWG|MCC)_\d+',text))==18
assert '6000.6.3f1' in (ROOT/'ProjectSettings/ProjectVersion.txt').read_text()
assert not re.search(r'^--- !u!(?:54|95|111) ',text,re.M), 'Unexpected physics/animation'
print(json.dumps({'offline_integrity':'PASS','serialized_objects':len(ids),'resolved_asset_guids':len(guids),'cabinet_count':18,'review_camera_count':1,'Unity_import_compile_console':'NOT_EXECUTED_AUTHORIZED_EDITOR_ACCESS_UNAVAILABLE','RF90':'AWAITING_UNITY_VISUAL_REVIEW'},indent=2))
