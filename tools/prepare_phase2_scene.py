"""Offline additive scene copy. Never run the Phase1A generator or control Unity."""
from pathlib import Path
import hashlib
import json
import re
import uuid

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT/'Assets/VisionGuard/Phase1A/Scenes/VISIONGUARD_IndustrialRoom_RF90_v01.unity'
OUT = ROOT/'Assets/VisionGuard/Phase2/Scenes/VISIONGUARD_IndustrialRoom_Phase2_v01.unity'
SCRIPT = ROOT/'Assets/VisionGuard/Phase2/Scripts/VGObservationExporter.cs'


def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def guid(path): return uuid.uuid5(uuid.NAMESPACE_URL, 'visionguard/phase2/'+path.relative_to(ROOT).as_posix()).hex
def meta(path, importer):
    Path(str(path)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(path)}\n{importer}:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')


def main():
    if OUT.exists():
        raise SystemExit('Phase2 scene exists; refusing to overwrite possible manual edits.')
    baseline = BASE.read_bytes()
    text = baseline.decode('utf-8-sig').replace('\r\n', '\n')
    # Allocate unused IDs; preserve every baseline serialized object verbatim.
    largest = max(int(n) for n in re.findall(r'^--- !u!\d+ &(\d+)',text,re.M) if int(n)<2**31)
    first = largest + 100
    blocks = re.split(r'(?=^--- !u!)',text,flags=re.M)
    by_id = {int(re.search(r'&(\d+)',b).group(1)): b for b in blocks if b.startswith('--- !u!')}
    review = next(b for b in blocks if '  m_Name: RF90_ReviewCamera_Main\n' in b)
    old_go = int(re.search(r'&(\d+)',review).group(1))
    old_components = [int(n) for n in re.findall(r'component: \{fileID: (\d+)\}',review)]
    selected = [review]+[by_id[n] for n in old_components if not by_id[n].startswith('--- !u!81 ')]
    selected_ids = [int(re.search(r'&(\d+)',b).group(1)) for b in selected]
    remap = dict(zip(selected_ids,range(first,first+len(selected_ids))))
    new_go = remap[old_go]
    transform_id = next(remap[k] for k in old_components if by_id[k].startswith('--- !u!4 '))
    exporter_id = first+len(selected_ids)
    added = ''.join(selected)
    for old,new in remap.items():
        added = re.sub(rf'&{old}(?=\r?\n)', f'&{new}', added)
        added = re.sub(rf'fileID: {old}(?=[,}}])', f'fileID: {new}', added)
    added=added.replace('m_Name: RF90_ReviewCamera_Main','m_Name: VG_CCTV_01').replace('m_TagString: MainCamera','m_TagString: Untagged')
    added=added.replace('  m_Layer: 0',f'  - component: {{fileID: {exporter_id}}}\n  m_Layer: 0',1)
    added=re.sub(r'm_Father: \{fileID: \d+\}', 'm_Father: {fileID: 0}',added)
    added=re.sub(r'm_LocalPosition: .*', 'm_LocalPosition: {x: -0.4, y: 2.2, z: 5.27}',added)
    added=re.sub(r'm_LocalRotation: .*', 'm_LocalRotation: {x: 0, y: -0.7071068, z: 0, w: 0.7071068}',added)
    added=re.sub(r'field of view: .*', 'field of view: 36',added)
    # A disabled perception camera never takes over the RF90 Game-view output.
    added=re.sub(r'(Camera:\r?\n.*?m_Enabled:) 1',r'\1 0',added,count=1,flags=re.S)
    meta(SCRIPT,'MonoImporter')
    added+=f'''--- !u!114 &{exporter_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {new_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(SCRIPT)}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  cameraId: VG_CCTV_01
  targetId: SWG_05/Indicator_03
  width: 1280
  height: 720
  roi:
    m_XMin: 604
    m_YMin: 324
    m_Width: 72
    m_Height: 72
'''
    marker='--- !u!1660057539 '
    head,roots=text.split(marker)
    roots=roots.rstrip()+f'\n  - {{fileID: {transform_id}}}\n'
    OUT.parent.mkdir(parents=True,exist_ok=True)
    OUT.write_text(head+added+marker+roots,encoding='utf-8',newline='')
    meta(OUT,'DefaultImporter')
    protected=[ROOT/'Architecture.md',*sorted((ROOT/'Assets/VisionGuard/Phase1A').rglob('*')),
               *sorted((ROOT/'ProjectSettings').rglob('*')), *sorted((ROOT/'Packages').rglob('*'))]
    hashes={p.relative_to(ROOT).as_posix():digest(p) for p in protected if p.is_file()}
    (ROOT/'docs/Phase2_Baseline_Hashes.json').write_text(json.dumps(hashes,indent=2)+'\n')
    assert BASE.read_bytes()==baseline
    print('Created additive Phase2 scene. Baseline bytes preserved. Camera framing awaits Unity review.')


if __name__=='__main__': main()
