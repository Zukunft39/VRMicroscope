"""List untranslated display strings, including escaped Unity YAML text."""
import json
import re
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
HAN = re.compile(r'[\u3400-\u9fff]')
QUOTED = re.compile(r'"(?:\\.|[^"\\])*"')
for root in ['Assets/Scene', 'Assets/Scenes', 'Assets/Prefabs', 'Assets/Resources']:
    for p in (ROOT/root).rglob('*'):
        if p.suffix not in ('.unity', '.prefab', '.asset'): continue
        if not p.read_bytes().startswith(b'%YAML'): continue
        for n, line in enumerate(p.read_text(encoding='utf-8-sig').splitlines(), 1):
            if not any(k in line for k in ('m_Text:', 'm_text:', 'title:', 'content:', 'description:', 'instruction:', 'value:', 'greetings:', 'reminderTemplates:')): continue
            decoded = re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m[1], 16)), line)
            if HAN.search(decoded): print(f'{p.relative_to(ROOT)}:{n}:{decoded.strip()}')
for p in (ROOT/'Assets/m_Scripts').rglob('*.cs'):
    if 'Assistant' in p.parts: continue
    for n, line in enumerate(p.read_text(encoding='utf-8-sig').splitlines(),1):
        if line.lstrip().startswith('//'): continue
        for m in QUOTED.finditer(line.split('//')[0]):
            if HAN.search(m.group()): print(f'{p.relative_to(ROOT)}:{n}:{m.group()}')
