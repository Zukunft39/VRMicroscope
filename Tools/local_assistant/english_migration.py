"""Apply reviewed exact translations without changing identifiers or Unity GUIDs."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAPPING = json.loads((Path(__file__).with_name('english_text.json')).read_text(encoding='utf-8'))
MAPPING.update(json.loads(Path(__file__).with_name('english_editor_text.json').read_text(encoding='utf-8')))
HAN = re.compile(r'[\u3400-\u9fff]')
STRING = re.compile(r'"(?:\\.|[^"\\\r\n])*"')


def replace(match):
    literal = match.group()
    try:
        value = json.loads(literal)
    except ValueError:
        return literal
    return json.dumps(MAPPING[value], ensure_ascii=False) if value in MAPPING else literal


if __name__ == '__main__':
    interaction_path = ROOT/'docs/assistant_knowledge/interaction_catalog.json'
    interaction = json.loads(interaction_path.read_text(encoding='utf-8-sig'))
    english = json.loads(Path(__file__).with_name('english_interactions.json').read_text(encoding='utf-8'))
    assert {e['id'] for e in interaction['entries']} == set(english)
    fields = ('title', 'location', 'prerequisites', 'desktop_steps', 'xr_steps', 'observation', 'completion_evidence', 'exit_steps', 'limitations')
    array_fields = {'prerequisites', 'desktop_steps', 'xr_steps', 'exit_steps', 'limitations'}
    for entry in interaction['entries']:
        values = english[entry['id']]
        for key, value in zip(fields, values):
            entry[key] = ([value] if value else []) if key in array_fields else value
        entry['keywords'] = [entry['id'].replace('_', ' '), values[0].lower()]
    interaction['approval_policy'] = 'Static audit and device-verification flags are retained. Current operation guidance uses the shared action catalog and GUIDANCE_CONTEXT allowlist. Static confirmation does not establish device-test completion.'
    interaction_path.write_text(json.dumps(interaction, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    guide = ['# Project Interaction Guide',
             'English edition. Static catalog evidence does not grant runtime permissions. Use the current GUIDANCE_CONTEXT allowlist for operating instructions and NAVIGATION_CONTEXT for markers. Player actions are never executed by the assistant.']
    for entry in interaction['entries']:
        guide.append('## '+entry['id']+' - '+entry['title'])
        for key in fields[1:]:
            value = entry[key]
            if value:
                guide.append('**'+key.replace('_', ' ').title()+':** '+('; '.join(value) if isinstance(value, list) else value))
        guide.append('**Verification:** '+entry['verification']+'; runtime_verified='+str(entry['runtime_verified']).lower())
        guide.append('**Sources:** '+', '.join(entry['sources']))
    (interaction_path.parent/'INTERACTIONS.md').write_text('\n\n'.join(guide)+'\n', encoding='utf-8')
    catalog_path = ROOT/'Assets/Resources/AssistantGuidanceActions.json'
    catalog = json.loads(catalog_path.read_text(encoding='utf-8-sig'))
    actions = json.loads(Path(__file__).with_name('english_actions.json').read_text(encoding='utf-8'))
    assert {a['id'].removeprefix('learn:') for a in catalog['actions']} == set(actions)
    for action in catalog['actions']:
        action.update(zip(('desktop', 'xr', 'observation'), actions[action['id'].removeprefix('learn:')]))
    catalog_path.write_text(json.dumps(catalog, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
    knowledge_dir = interaction_path.parent
    pack = ['# DeepSeek Reference Pack - English',
            'Generated from the current system prompt, project knowledge, interaction catalog, and shared operating instructions. The backend loads these sources directly. This static pack never replaces current runtime snapshots.']
    for name in ('SYSTEM_PROMPT.md', 'PROJECT_KNOWLEDGE.md', 'INTERACTIONS.md'):
        pack.append((knowledge_dir/name).read_text(encoding='utf-8-sig'))
    pack.append('## Shared Action Catalog\n\n```json\n'+json.dumps(catalog, ensure_ascii=False, indent=2)+'\n```')
    (knowledge_dir/'deepseek_reference_pack.md').write_text('\n\n'.join(pack)+'\n', encoding='utf-8')
    paths = list((ROOT/'Assets/m_Scripts').rglob('*.cs'))
    paths += [ROOT/'Assets/Resources/LocalAssistantSettings.asset']
    for path in paths:
        original = path.read_text(encoding='utf-8-sig')
        updated = STRING.sub(replace, original)
        if updated != original:
            path.write_text(updated, encoding='utf-8')
            print(path.relative_to(ROOT))
