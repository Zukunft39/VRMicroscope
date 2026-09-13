"""Compile actual project sources against cached Unity references, then check idle policy.
Does not touch Library outputs or require closing the user's editor. Requires .NET 8.
"""
import json
from pathlib import Path
import re
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Temp/LocalAssistantVerification'
OUT.mkdir(parents=True, exist_ok=True)


def compile_project():
    rsp = max((ROOT / 'Library/Bee').rglob('Assembly-CSharp.rsp'), key=lambda p: p.stat().st_mtime)
    project = (ROOT / 'Assembly-CSharp.csproj').read_text(encoding='utf-8-sig')
    match = re.search(r'<HintPath>(.*?Editor[\\/]Data)[\\/]Managed', project)
    data = Path(match[1])
    runner = [str(data / 'NetCoreRuntime/dotnet.exe'), str(data / 'DotNetSdkRoslyn/csc.dll'), '/nologo']
    for name in ['Assembly-CSharp', 'Assembly-CSharp-Editor']:
        lines = []
        editor = name.endswith('-Editor')
        for line in rsp.with_name(name + '.rsp').read_text(encoding='utf-8-sig').splitlines():
            normalized = line.replace('\\', '/')
            if line.startswith(('-out:', '-refout:')):
                continue
            if 'm_Scripts/Assistant/' in normalized or 'LocalAssistantSmokeCheck.cs' in normalized:
                continue
            # The open editor may still have pre-removal source paths in its cached response file.
            if 'm_Scripts/AITutor/' in normalized or 'Editor/AITutorSetup.cs' in normalized:
                continue
            if editor and line.startswith('-r:') and normalized.endswith('/Assembly-CSharp.ref.dll"'):
                line = '-r:"%s"' % (OUT / 'Assembly-CSharp.dll')
            lines.append(line)
        sources = [ROOT / 'Assets/m_Scripts/Editor/LocalAssistantSmokeCheck.cs'] if editor else sorted((ROOT / 'Assets/m_Scripts/Assistant').glob('*.cs'))
        lines += ['-out:"%s"' % (OUT / (name + '.dll'))] + ['"%s"' % p for p in sources]
        target = OUT / (name + '.rsp')
        target.write_text('\n'.join(lines), encoding='utf-8')
        subprocess.run(runner + ['@' + str(target)], cwd=ROOT, check=True)
    print('Complete runtime and editor compilation passed.', flush=True)


def check_clock():
    dotnet = Path(shutil.which('dotnet'))
    sdk = max((dotnet.parent / 'sdk').glob('8.*'), key=lambda p: tuple(map(int, p.name.split('.'))))
    refs = max((dotnet.parent / 'packs/Microsoft.NETCore.App.Ref').glob('8.*/ref/net8.0'), key=lambda p: tuple(map(int, p.parent.parent.name.split('.'))))
    lines = ['-target:exe', '-out:"%s"' % (OUT / 'IdleClockChecks.dll')]
    lines += ['-r:"%s"' % p for p in refs.glob('*.dll')]
    lines += ['"%s"' % (ROOT / 'Assets/m_Scripts/Assistant/AssistantIdleClock.cs'), '"%s"' % Path(__file__).with_name('IdleClockChecks.cs')]
    lines += ['"%s"' % (ROOT / 'Assets/m_Scripts/Assistant/AssistantWavEncoder.cs'), '"%s"' % Path(__file__).with_name('WavChecks.cs')]
    target = OUT / 'clock.rsp'
    target.write_text('\n'.join(lines), encoding='utf-8')
    subprocess.run([str(dotnet), str(sdk / 'Roslyn/bincore/csc.dll'), '/nologo', '@' + str(target)], check=True)
    (OUT / 'IdleClockChecks.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
    subprocess.run([str(dotnet), str(OUT / 'IdleClockChecks.dll')], check=True)


if __name__ == '__main__':
    compile_project()
    check_clock()
