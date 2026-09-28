"""Offline Unity scene authoring. Does not launch, connect to, or control Unity.
Run from the project root with Python 3.12. Output is restricted to Phase1A.
"""
from pathlib import Path
import math
import re
import uuid
import json

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/VisionGuard/Phase1A'
OUT.mkdir(parents=True, exist_ok=True)
HEADER = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
def guid(name): return uuid.uuid5(uuid.NAMESPACE_URL, 'visionguard/phase1a/' + name).hex
def save(path, text, kind='NativeFormatImporter', main=0):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding='utf-8')
    Path(str(path)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(path.relative_to(ROOT).as_posix())}\n{kind}:\n  externalObjects: {{}}\n  mainObjectFileID: {main}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')

palette = {
 'Cabinet': (.59,.61,.59, .18,.32), 'CabinetLight': (.68,.69,.66,.15,.3),
 'Steel': (.22,.25,.26,.65,.38), 'Dark': (.035,.045,.047,.05,.25),
 'Concrete': (.37,.38,.36,0,.18), 'Floor': (.39,.4,.38,.08,.43),
 'Yellow': (.92,.57,.035,.05,.35), 'Red': (.62,.035,.022,.15,.36),
 'Green': (.03,.6,.18,.05,.3), 'White': (.82,.86,.87,0,.35),
 'Screen': (.17,.32,.38,.15,.5), 'Amber': (1,.27,.025,0,.3),
 'Seam': (.18,.19,.18,0,.15), 'Door': (.19,.23,.25,.25,.3),
}
mats={}
for name,(r,g,b,metal,smooth) in palette.items():
    path=OUT/'Materials'/f'{name}.mat'; mats[name]=guid(path.relative_to(ROOT).as_posix())
    emission = name in ('White','Green','Amber')
    save(path, HEADER+f'''--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_Name: VG_{name}
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_ValidKeywords: {'[_EMISSION]' if emission else '[]'}
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats:
    - _Metallic: {metal}
    - _Smoothness: {smooth}
    - _Surface: 0
    - _Cull: 2
    - _ZWrite: 1
    - _SrcBlend: 1
    - _DstBlend: 0
    m_Colors:
    - _BaseColor: {{r: {r}, g: {g}, b: {b}, a: 1}}
    - _EmissionColor: {{r: {r if emission else 0}, g: {g if emission else 0}, b: {b if emission else 0}, a: 1}}
''',main=2100000)

nodes=[]
def obj(name,parent=None,pos=(0,0,0),scale=(1,1,1),mat=None,mesh=10202,rot=(0,0,0,1),extra=None):
    n=dict(id=1000+len(nodes)*10,name=name,parent=parent,pos=pos,scale=scale,mat=mat,mesh=mesh,rot=rot,extra=extra,children=[])
    nodes.append(n)
    if parent: parent['children'].append(n)
    return n
def box(name,parent,pos,scale,mat): return obj(name,parent,pos,scale,mat)
def cylinder(name,parent,pos,diam,length,mat): return obj(name,parent,pos,(diam,length/2,diam),mat,10206)
root=obj('VISIONGUARD_IndustrialRoom')
env=obj('Environment',root)
floor=obj('Floor',env); walls=obj('Walls',env); ceiling=obj('Ceiling',env); doors=obj('Doors',env)
equipment=obj('ElectricalEquipment',root)
cables=obj('CableInfrastructure',root); trays=obj('OverheadTrays',cables); racks=obj('VerticalRacks',cables); wire=obj('Cables',cables)
safety=obj('Safety',root); marks=obj('FloorMarkings',safety); signs=obj('HazardSigns',safety); fire=obj('FireSafety',safety)
lights=obj('Lighting',root); cams=obj('RF90_ReviewCameras',root)
box('ConcreteSlab_6x12m',floor,(0,-.13,6),(6.3,.26,12.3),'Floor')
for x in (-3.12,3.12): box('ConcreteSideWall',walls,(x,2,6),(.24,4,12.3),'Concrete')
box('RearWall',walls,(0,2,12.12),(6.3,4,.24),'Concrete')
box('CeilingSlab',ceiling,(0,4.12,6),(6.3,.24,12.3),'Concrete')
for z in range(1,13,2):
    box('CeilingCrossBeam',ceiling,(0,3.85,z),(6,.25,.16),'Seam')
    box('FloorExpansionJoint',floor,(0,.002,z),(6,.003,.012),'Seam')
for x in (-1.5,0,1.5): box('CeilingJoint',ceiling,(x,3.991,6),(.015,.01,12),'Seam')
for y in (1,2,3): box('RearFormworkJoint',walls,(0,y,11.994),(6,.012,.008),'Seam')
for side in (-1,1):
    box('YellowSafetyBoundary',marks,(side*1.53,.007,5.65),(.075,.012,10.8),'Yellow')
box('RearSafetyBoundary',marks,(0,.007,11.05),(3.13,.012,.075),'Yellow')
box('TrenchRecess',floor,(0,.008,5.7),(.66,.018,10.5),'Dark')
for x in (-.34,.34): box('TrenchFrame',floor,(x,.022,5.7),(.035,.03,10.5),'Steel')
for i in range(12): box('GratingLongitudinalBar',floor,(-.3+i*.055,.027,5.7),(.014,.025,10.5),'Steel')
for i in range(142): box('GratingCrossBar',floor,(0,.034,.5+i*.074),(.64,.014,.013),'Steel')

# Panel local X is width, local Z faces aisle; parents rotate each bank inward.
def hazard(p,x,y,z):
    # Filled triangular warning, constructed as thin horizontal strips; black bolt above it.
    for j in range(16):
        box('HazardTriangle',p,(x,y+j*.012,z),(.22*(1-j/17),.013,.008),'Yellow')
    for dx,dy,angle in ((.015,.125,-28),(-.015,.079,62),(.01,.035,-28)):
        n=box('LightningBolt',p,(x+dx,y+dy,z+.009),(.018,.065,.009),'Dark')
        a=math.radians(angle)/2;n['rot']=(0,0,math.sin(a),math.cos(a))
for side in (-1,1):
    bank=obj('LeftBank' if side<0 else 'RightBank',equipment)
    for i in range(9):
        a=math.radians(90 if side<0 else -90)/2
        h=2.45 if side<0 else (2.35 if i%3 else 2.55)
        p=obj(f'{"SWG" if side<0 else "MCC"}_{i+1:02}',bank,(side*2.36,0,1.05+i*1.08),rot=(0,math.sin(a),0,math.cos(a)))
        box('Plinth',p,(0,.1,0),(1.055,.2,1.02),'Steel')
        box('CabinetShell',p,(0,h/2+.2,0),(1.045,h,1),'Steel')
        for j,(yy,hh) in enumerate(((.63,.83),(1.62,1.1),(h+.09,.16))):
            box('InsetDoor' if j<2 else 'Header',p,(0,yy,.515),(1.005,hh,.045),'CabinetLight' if i%3==0 else 'Cabinet')
        box('UpperFace',p,(0,h-.1,.515),(1.005,.3,.045),'Cabinet')
        for yy in (.36,1.07,2.03):
            box('Hinge',p,(-.485,yy,.55),(.025,.085,.025),'Steel')
        box('HandleMount',p,(.4,1.43,.55),(.075,.23,.022),'Dark')
        box('Handle',p,(.4,1.43,.59),(.035,.15,.055),'Steel')
        box('MeterBezel',p,(-.12,1.86,.56),(.29,.3,.055),'Dark')
        box('MeterGlass',p,(-.12,1.87,.592),(.235,.225,.014),'Screen' if i%2 else 'White')
        for k in range(4): box('MeterReadout',p,(-.18+k*.045,1.9,.602),(.022,.05,.009),'Steel')
        box('BreakerHousing',p,(-.12,1.43,.565),(.22,.16,.065),'Dark')
        box('BreakerLever',p,(-.12,1.44,.615),(.06,.11,.075),'Steel')
        for k,m in enumerate(('Green','Amber','Red')):
            obj('IndicatorBezel',p,(-.26+k*.18,2.2,.566),(.075,.075,.035),'Dark',10207)
            obj('Indicator_'+m,p,(-.26+k*.18,2.2,.59),(.046,.046,.024),m,10207)
            obj('Pushbutton',p,(-.23+k*.12,1.19,.565),(.045,.045,.035),m,10207)
        box('VentRecess',p,(.17,.64,.545),(.31,.32,.016),'Dark')
        for k in range(8): box('VentLouver',p,(.17,.5+k*.038,.563),(.29,.014,.023),'Steel')
        hazard(p,-.25,.42,.554)
        box('AssetLabel',p,(-.28,h+.085,.545),(.24,.055,.008),'White')

# Trays, suspended supports, ordered cables and exposed entrance risers.
for side in (-1,1):
    for x in (side*2.05,side*2.7):
        box('TraySideRail',trays,(x,3.35,5.8),(.055,.14,11.4),'Steel')
    for i in range(47): box('TrayRung',trays,(side*2.375,3.29,.25+i*.24),(.7,.04,.045),'Steel')
    for z in (1,3,5,7,9,11):
        box('SuspensionRod',trays,(side*2.02,3.7,z),(.035,.58,.035),'Steel')
        box('WallSupportBracket',trays,(side*2.55,3.24,z),(.9,.055,.06),'Steel')
    for k in range(9):
        n=cylinder('LongitudinalCable',wire,(side*(2.11+k*.065),3.4+(k%2)*.035,5.8),.043,11.4,'Yellow' if k<3 else 'Dark')
        n['rot']=(math.sqrt(.5),0,0,math.sqrt(.5))
    for z in (.24,10.75):
        for k in range(8):
            x=side*(2.1+k*.09)
            cylinder('RiserCable',wire,(x,1.85,z),.063,3.05,'Yellow' if k<3 else 'Dark')
        for y in (.4,1.2,2,2.8,3.3): box('RiserClamp',racks,(side*2.42,y,z-.06),(.84,.065,.085),'Steel')
        for x in (side*2.02,side*2.82): box('RiserUpright',racks,(x,1.85,z),(.055,3.25,.09),'Steel')
        box('RiserConcreteKerb',floor,(side*2.42,.09,z),(.95,.18,.34),'Concrete')
    for z in (2.2,4.4,6.6,8.8):
        for k in range(3): cylinder('CabinetCableDrop',wire,(side*(2.2+k*.09),2.97,z),.04,.65,'Dark')

box('ExitDoorFrame',doors,(0,1.25,11.92),(2.04,2.5,.13),'Steel')
for side in (-1,1):
    box('ExitDoorLeaf',doors,(side*.475,1.21,11.82),(.93,2.36,.09),'Door')
    box('DoorLever',doors,(side*.15,1.1,11.74),(.18,.035,.055),'Steel')
    box('DoorCloser',doors,(side*.63,2.29,11.75),(.28,.07,.06),'Steel')
box('ExitSignFrame',safety,(0,2.7,11.84),(.64,.23,.08),'Steel')
box('ExitSignGreen',safety,(0,2.7,11.79),(.6,.19,.025),'Green')
# EXIT lettering as original geometry, so no font or texture dependencies.
glyphs={'E':['111','100','110','100','111'],'X':['101','101','010','101','101'],'I':['111','010','010','010','111'],'T':['111','010','010','010','010']}
for i,char in enumerate('EXIT'):
    for y,row in enumerate(glyphs[char]):
        for x,v in enumerate(row):
            if v=='1': box('ExitLetter',safety,(-.22+i*.12+x*.026,2.753-y*.026,11.77),(.024,.024,.014),'White')
for x in (-1.65,1.65):
    cylinder('FireExtinguisher',fire,(x,.94,11.78),.21,.57,'Red')
    obj('ExtinguisherShoulder',fire,(x,1.22,11.78),(.2,.12,.2),'Red',10207)
    box('ExtinguisherLabel',fire,(x,.96,11.665),(.12,.18,.014),'White')
    box('ExtinguisherValve',fire,(x,1.32,11.78),(.15,.06,.04),'Dark')
    cylinder('ExtinguisherHose',fire,(x+.13,1.07,11.77),.025,.43,'Dark')
box('FireEquipmentCase',fire,(-2.25,1.3,11.8),(.4,.57,.15),'Red')
box('CaseGlass',fire,(-2.25,1.3,11.71),(.28,.43,.018),'Dark')
box('CaseHandle',fire,(-2.12,1.3,11.68),(.025,.12,.035),'Steel')
box('VentilationHousing',env,(1.95,3.25,11.8),(.9,.54,.28),'Steel')
box('VentilationInset',env,(1.95,3.25,11.64),(.79,.43,.02),'Dark')
for i in range(9): box('VentilationLouver',env,(1.95,3.06+i*.048,11.61),(.77,.02,.04),'Cabinet')
for x in (-1.25,2.7): cylinder('RearConduit',cables,(x,2.1,11.85),.035,3.8,'Steel')
for z in (1.4,3.7,6,8.3,10.6):
    box('LuminaireHousing',lights,(0,3.69,z),(.24,.09,1.25),'Steel')
    for x in (-.065,.065): box('LEDStrip',lights,(x,3.63,z),(.057,.035,1.15),'White')
    obj('CeilingIllumination',lights,(0,3.5,z),extra='light',rot=(math.sqrt(.5),0,0,math.sqrt(.5)))
obj('RF90_ReviewCamera_Main',cams,(.05,1.68,.12),extra='camera')

sample=(ROOT/'Assets/Scenes/SampleScene.unity').read_text(encoding='utf-8-sig')
header=sample[:sample.index('--- !u!1 &')]
header=re.sub(r'm_AmbientMode: 0','m_AmbientMode: 3',header)
header=re.sub(r'm_AmbientSkyColor: .*','m_AmbientSkyColor: {r: 0.46, g: 0.48, b: 0.5, a: 1}',header)
header=re.sub(r'm_SkyboxMaterial: .*','m_SkyboxMaterial: {fileID: 0}',header)
header=re.sub(r'm_LightingDataAsset: .*','m_LightingDataAsset: {fileID: 0}',header)
parts=[header]
common='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
def vec(v,keys='xyz'): return '{'+', '.join(f'{k}: {n:.7g}' for k,n in zip(keys,v))+'}'
for n in nodes:
    i=n['id']; components=[i+1]+([i+2,i+3] if n['mat'] else [])+([i+4,i+5] if n['extra'] else [])
    parts.append(f'--- !u!1 &{i}\nGameObject:\n'+common+'  serializedVersion: 6\n  m_Component:\n'+''.join(f'  - component: {{fileID: {c}}}\n' for c in components)+f'  m_Layer: 0\n  m_Name: {n["name"]}\n  m_TagString: {"MainCamera" if n["extra"]=="camera" else "Untagged"}\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n')
    children='\n'+''.join(f'  - {{fileID: {c["id"]+1}}}\n' for c in n['children']) if n['children'] else ' []\n'
    parts.append(f'--- !u!4 &{i+1}\nTransform:\n'+common+f'  m_GameObject: {{fileID: {i}}}\n  serializedVersion: 2\n  m_LocalRotation: {vec(n["rot"],"xyzw")}\n  m_LocalPosition: {vec(n["pos"])}\n  m_LocalScale: {vec(n["scale"])}\n  m_Children:{children}  m_Father: {{fileID: {n["parent"]["id"]+1 if n["parent"] else 0}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}\n')
    if n['mat']:
        parts.append(f'--- !u!33 &{i+2}\nMeshFilter:\n'+common+f'  m_GameObject: {{fileID: {i}}}\n  m_Mesh: {{fileID: {n["mesh"]}, guid: 0000000000000000e000000000000000, type: 0}}\n')
        parts.append(f'''--- !u!23 &{i+3}
MeshRenderer:
{common}  m_GameObject: {{fileID: {i}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_MotionVectors: 0
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RenderingLayerMask: 1
  m_Materials:
  - {{fileID: 2100000, guid: {mats[n['mat']]}, type: 2}}
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
    if n['extra']:
        old,cls,go=(330585545,20,330585543) if n['extra']=='camera' else (410087040,108,410087039)
        block=re.search(rf'--- !u!{cls} &{old}\n.*?(?=--- !u!)',sample,re.S).group()
        block=block.replace(f'&{old}',f'&{i+4}').replace(f'fileID: {go}',f'fileID: {i}')
        if n['extra']=='camera':
            block=block.replace('field of view: 60','field of view: 58').replace('near clip plane: 0.3','near clip plane: 0.05').replace('far clip plane: 1000','far clip plane: 40').replace('m_ClearFlags: 1','m_ClearFlags: 2')
            data=re.search(r'--- !u!114 &330585547\n.*?(?=--- !u!)',sample,re.S).group().replace('&330585547',f'&{i+5}').replace('fileID: 330585543',f'fileID: {i}').replace('m_RenderPostProcessing: 1','m_RenderPostProcessing: 0')
            parts.append(data)
        else:
            block=block.replace('  m_Type: 1','  m_Type: 0').replace('m_Intensity: 2','m_Intensity: 5').replace('m_Range: 10','m_Range: 6').replace('m_SpotAngle: 30','m_SpotAngle: 135').replace('m_InnerSpotAngle: 21.80208','m_InnerSpotAngle: 100').replace('m_NormalBias: 0.4','m_NormalBias: 0.08')
            data=re.search(r'--- !u!114 &410087042\n.*?(?=--- !u!)',sample,re.S).group().replace('&410087042',f'&{i+5}').replace('fileID: 410087039',f'fileID: {i}')
            parts.append(data)
        parts.append(block)
parts.append('--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n  - {fileID: 1001}\n')
scene=OUT/'Scenes/VISIONGUARD_IndustrialRoom_RF90_v01.unity'
save(scene,''.join(parts),'DefaultImporter')
print(json.dumps({'scene':str(scene),'objects':len(nodes),'materials':len(mats),'status':'AUTHORED_OFFLINE_UNITY_REVIEW_PENDING'},indent=2))
