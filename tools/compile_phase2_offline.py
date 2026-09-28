"""Compile only Phase2 C# against existing Unity references; never starts Unity."""
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'tests/artifacts/phase3/csharp'
OUT.mkdir(parents=True,exist_ok=True)
ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
refs=[]
for project in ('Assembly-CSharp-Editor.csproj',):
    tree=ET.parse(ROOT/project)
    for hint in tree.findall('.//m:Reference/m:HintPath',ns):
        path=Path(hint.text)
        if not path.is_absolute(): path=ROOT/path
        if path.is_file() and path not in refs: refs.append(path)
compiler=sorted(Path('C:/Program Files/dotnet/sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
sources=[ROOT/'Assets/VisionGuard/Phase2/Scripts/VGObservationExporter.cs',
         ROOT/'Assets/VisionGuard/Phase2/Scripts/Editor/VGObservationExporterEditor.cs',
         ROOT/'Assets/VisionGuard/Phase3/Scripts/VGMobileInspectorMovement.cs',
         ROOT/'Assets/VisionGuard/Phase3/Scripts/VGPhase3AgentController.cs',
         ROOT/'Assets/VisionGuard/Phase3/Scripts/Editor/VGPhase3AgentControllerEditor.cs']
response=OUT/'compile.rsp'
response.write_text('\n'.join(['-nologo','-nostdlib+','-target:library','-langversion:9.0',
                              f'-out:"{OUT / "VisionGuard.Phase3.OfflineCheck.dll"}"',
                              *[f'-r:"{p}"' for p in refs],
                              *[f'"{source}"' for source in sources]]),encoding='utf-8')
result=subprocess.run(['dotnet',str(compiler),'-noconfig','@'+str(response)],cwd=ROOT,capture_output=True,text=True)
(OUT/'compile.log').write_text(result.stdout+result.stderr,encoding='utf-8')
print(result.stdout+result.stderr)
print(f'Offline C# compiler exit: {result.returncode}; Unity Editor import/runtime not executed.')
raise SystemExit(result.returncode)
