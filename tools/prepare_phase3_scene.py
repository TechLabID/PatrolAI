"""Create the additive Phase 3 scene without launching or controlling Unity."""
from pathlib import Path
import hashlib
import json
import re
import uuid

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT/'Assets/VisionGuard/Phase2/Scenes/VISIONGUARD_IndustrialRoom_Phase2_v01.unity'
OUT = ROOT/'Assets/VisionGuard/Phase3/Scenes/VISIONGUARD_IndustrialRoom_Phase3_v01.unity'
STEEL = 'e735804f0a645ad389736f73c331f989'
DARK = 'a9c204ba9985559b8f8bc4b794014058'
AMBER = '416f11a0f8c15f2c87bf7835618d5496'
MOVEMENT_SCRIPT = 'e09101d477af4735a2520dce8eb25b37'
CONTROLLER_SCRIPT = 'f142d5ffb6174549a897775f6c1947f0'


def stable_guid(name): return uuid.uuid5(uuid.NAMESPACE_URL, 'visionguard/phase3/'+name).hex
def meta(path, folder=False):
    body = ('folderAsset: yes\nDefaultImporter:' if folder else 'DefaultImporter:')
    Path(str(path)+'.meta').write_text(
        f'fileFormatVersion: 2\nguid: {stable_guid(path.relative_to(ROOT).as_posix())}\n{body}\n'
        '  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')
def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()


class SceneObjects:
    def __init__(self, first):
        self.next = first
        self.blocks = []
    def take(self):
        value=self.next;self.next+=1;return value
    def game_object(self,name,components):
        go=self.take()
        comp=''.join(f'  - component: {{fileID: {value}}}\n' for value in components)
        self.blocks.append(f'''--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comp}  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_StaticEditorFlags: 0
  m_IsActive: 1
''')
        return go
    def transform(self,component,go,parent=0,children=(),pos=(0,0,0),rot=(0,0,0,1),scale=(1,1,1)):
        child_text=' []\n' if not children else '\n'+''.join(f'  - {{fileID: {value}}}\n' for value in children)
        self.blocks.append(f'''--- !u!4 &{component}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: {rot[0]}, y: {rot[1]}, z: {rot[2]}, w: {rot[3]}}}
  m_LocalPosition: {{x: {pos[0]}, y: {pos[1]}, z: {pos[2]}}}
  m_LocalScale: {{x: {scale[0]}, y: {scale[1]}, z: {scale[2]}}}
  m_Children:{child_text}  m_Father: {{fileID: {parent}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
''')
    def visual(self,name,parent,pos,scale,material,mesh=10202,rot=(0,0,0,1)):
        tr,mf,mr=self.take(),self.take(),self.take()
        go=self.game_object(name,[tr,mf,mr])
        self.transform(tr,go,parent,pos=pos,rot=rot,scale=scale)
        self.blocks.append(f'''--- !u!33 &{mf}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Mesh: {{fileID: {mesh}, guid: 0000000000000000e000000000000000, type: 0}}
--- !u!23 &{mr}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_MotionVectors: 0
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RenderingLayerMask: 1
  m_Materials:
  - {{fileID: 2100000, guid: {material}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_SortingLayerID: 0
  m_SortingOrder: 0
''')
        return tr


def main():
    if OUT.exists():
        raise SystemExit('Phase3 scene exists; refusing to overwrite possible manual calibration.')
    text=BASE.read_text(encoding='utf-8-sig').replace('\r\n','\n')
    phase2_sha=digest(BASE)
    blocks=re.split(r'(?=^--- !u!)',text,flags=re.M)
    by_id={int(re.search(r'&(\d+)',block).group(1)):block for block in blocks if block.startswith('--- !u!')}
    # Controlled ambiguity: widen only the Phase 3 primary view so the same lamp
    # contributes fewer real image pixels. Keep the proven 72x72 target ROI.
    by_id[16747]=by_id[16747].replace('field of view: 36','field of view: 60')
    without_roots=[]
    roots=None
    for block in blocks:
        if block.startswith('--- !u!1660057539 '): roots=block
        elif block.startswith('--- !u!'):
            key=int(re.search(r'&(\d+)',block).group(1));without_roots.append(by_id[key])
        else: without_roots.append(block)
    largest=max(key for key in by_id if key < 2**31)
    scene=SceneObjects(largest+100)

    # Allocate mobile root/component first so all child transforms can reference it.
    mobile_transform=scene.take(); movement_component=scene.take()
    mobile_go=scene.game_object('VG_MobileInspector_01',[mobile_transform,movement_component])
    child_transforms=[]
    child_transforms.append(scene.visual('Body',mobile_transform,(0,0,0),(0.5,0.18,0.38),STEEL))
    child_transforms.append(scene.visual('CrossArm_X',mobile_transform,(0,0,.0),(.82,.055,.07),DARK))
    child_transforms.append(scene.visual('CrossArm_Z',mobile_transform,(0,0,.0),(.07,.055,.72),DARK))
    for index,(x,z) in enumerate(((-.37,-.31),(.37,-.31),(-.37,.31),(.37,.31)),1):
        child_transforms.append(scene.visual(f'Rotor_{index:02}',mobile_transform,(x,.08,z),(.2,.025,.2),DARK,10206))
    child_transforms.append(scene.visual('StatusLamp',mobile_transform,(0,.13,.08),(.06,.035,.06),AMBER,10207))
    child_transforms.append(scene.visual('CameraHousing',mobile_transform,(0,0,.25),(.22,.14,.18),DARK))

    # Clone the proven Phase2 camera/URP/exporter components under the mobile root.
    source_ids=(16745,16746,16747,16748,16749)
    camera_ids={old:scene.take() for old in source_ids}
    camera_blocks=''.join(by_id[old] for old in source_ids)
    for old,new in camera_ids.items():
        camera_blocks=re.sub(rf'&{old}(?=\n)',f'&{new}',camera_blocks)
        camera_blocks=re.sub(rf'fileID: {old}(?=[,}}])',f'fileID: {new}',camera_blocks)
    camera_blocks=camera_blocks.replace('m_Name: VG_CCTV_01','m_Name: InspectionCamera')
    camera_blocks=camera_blocks.replace('m_LocalRotation: {x: 0, y: -0.7071068, z: 0, w: 0.7071068}',
                                        'm_LocalRotation: {x: 0, y: 0, z: 0, w: 1}')
    camera_blocks=camera_blocks.replace('m_LocalPosition: {x: -0.4, y: 2.2, z: 5.27}',
                                        'm_LocalPosition: {x: 0, y: 0, z: 0.32}')
    camera_blocks=camera_blocks.replace('m_Father: {fileID: 0}',f'm_Father: {{fileID: {mobile_transform}}}',1)
    camera_blocks=camera_blocks.replace('cameraId: VG_CCTV_01','cameraId: VG_MOBILE_01')
    camera_blocks=camera_blocks.replace('field of view: 60','field of view: 36')
    camera_blocks=camera_blocks.replace('    m_XMin: 604\n    m_YMin: 324\n    m_Width: 72\n    m_Height: 72',
                                        '    m_XMin: 592\n    m_YMin: 312\n    m_Width: 96\n    m_Height: 96')
    scene.blocks.append(camera_blocks)
    child_transforms.append(camera_ids[16746])
    # Add the final root Transform after all child IDs are known.
    scene.transform(mobile_transform,mobile_go,children=child_transforms,pos=(0,2.2,-.6),
                    rot=(0,-.7071068,0,.7071068))
    scene.blocks.append(f'''--- !u!114 &{movement_component}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {mobile_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {MOVEMENT_SCRIPT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  homePosition: {{x: 0, y: 2.2, z: -0.6}}
  homeEuler: {{x: 0, y: -90, z: 0}}
  inspectionPosition: {{x: -0.8, y: 2.2, z: 5.27}}
  inspectionEuler: {{x: 0, y: -90, z: 0}}
  movementDuration: 2.5
''')

    controller_transform=scene.take();controller_component=scene.take()
    controller_go=scene.game_object('VG_Phase3_AgentController',[controller_transform,controller_component])
    scene.transform(controller_transform,controller_go)
    scene.blocks.append(f'''--- !u!114 &{controller_component}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {controller_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {CONTROLLER_SCRIPT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  primaryExporter: {{fileID: 16749}}
  secondaryExporter: {{fileID: {camera_ids[16749]}}}
  movementController: {{fileID: {movement_component}}}
  showStatusOverlay: 1
''')
    assert roots is not None
    roots=roots.rstrip()+f'\n  - {{fileID: {mobile_transform}}}\n  - {{fileID: {controller_transform}}}\n'
    OUT.parent.mkdir(parents=True,exist_ok=True)
    OUT.write_text(''.join(without_roots)+''.join(scene.blocks)+roots,encoding='utf-8',newline='')
    if not (OUT.parent.with_suffix('.meta')).exists(): meta(OUT.parent,folder=True)
    meta(OUT)
    protected=[ROOT/'Architecture.md',*sorted((ROOT/'Assets/VisionGuard/Phase1A').rglob('*')),
               *sorted((ROOT/'Assets/VisionGuard/Phase2').rglob('*')),
               *sorted((ROOT/'Packages').rglob('*')),*sorted((ROOT/'ProjectSettings').rglob('*'))]
    hashes={path.relative_to(ROOT).as_posix():digest(path) for path in protected if path.is_file()}
    (ROOT/'docs/Phase3_Baseline_Hashes.json').write_text(json.dumps(hashes,indent=2)+'\n')
    assert digest(BASE)==phase2_sha
    print(json.dumps({'scene':str(OUT),'phase2_scene_preserved':True,
                      'primary_fov':60, 'primary_roi':[604,324,72,72],
                      'mobile_fov':36, 'mobile_roi':[592,312,96,96],
                      'new_serialized_blocks':len(scene.blocks)},indent=2))


if __name__=='__main__': main()
