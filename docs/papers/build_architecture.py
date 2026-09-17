"""Build the editable draw.io source for Figure 3 from the implemented data flow.

Export with the official draw.io desktop CLI; no API keys or runtime user data
are used to create this architecture figure.
"""
from pathlib import Path
from xml.etree import ElementTree as ET

OUT = Path(__file__).resolve().parent / 'Pic'


def build():
    document = ET.Element('mxfile', host='app.diagrams.net', type='device')
    diagram = ET.SubElement(document, 'diagram', id='vrmicroscope-architecture', name='System architecture')
    model = ET.SubElement(diagram, 'mxGraphModel', dx='1180', dy='810', grid='1', gridSize='10',
                          page='1', pageScale='1', pageWidth='1180', pageHeight='810', background='#ffffff')
    root = ET.SubElement(model, 'root')
    ET.SubElement(root, 'mxCell', id='0')
    ET.SubElement(root, 'mxCell', id='1', parent='0')

    def node(key, text, x, y, w, h, style=''):
        c = ET.SubElement(root, 'mxCell', id=key, value=text, parent='1', vertex='1',
                          style='rounded=0;whiteSpace=wrap;html=0;fontFamily=Arial;fontSize=19;'
                          'fontColor=#17212b;strokeColor=#485462;strokeWidth=1.5;fillColor=#ffffff;'
                          'align=center;verticalAlign=middle;spacing=6;' + style)
        ET.SubElement(c, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h), **{'as': 'geometry'})

    def edge(key, source, target, label='', points=(), dashed=False, exits='exitX=1;exitY=0.5;entryX=0;entryY=0.5;'):
        c = ET.SubElement(root, 'mxCell', id=key, value=label, parent='1', edge='1', source=source, target=target,
                          style='edgeStyle=orthogonalEdgeStyle;rounded=0;html=0;endArrow=block;endFill=1;'
                          'strokeColor=#253745;strokeWidth=1.6;fontFamily=Arial;fontSize=15;'
                          'labelBackgroundColor=#ffffff;'+exits+('dashed=1;' if dashed else ''))
        geom = ET.SubElement(c, 'mxGeometry', relative='1', **{'as': 'geometry'})
        if points:
            arr = ET.SubElement(geom, 'Array', **{'as': 'points'})
            for x, y in points:
                ET.SubElement(arr, 'mxPoint', x=str(x), y=str(y))

    for key, title, x, w, color in [
        ('unity', 'UNITY CLIENT', 10, 350, '#eef4f8'),
        ('gateway', 'PYTHON GATEWAY', 390, 380, '#f4f5f6'),
        ('apis', 'EXTERNAL APIs', 800, 360, '#f4f5f6')]:
        node(key+'-area', '', x, 10, w, 735, 'fillColor='+color+';strokeColor=#aab4bd;')
        node(key+'-title', title, x+5, 18, w-10, 42, 'fillColor=none;strokeColor=none;fontStyle=1;fontSize=22;')

    node('input', 'Typed question or\ndefault microphone', 45, 90, 280, 72)
    node('edit', 'Editable text\nLearner confirms Send', 45, 230, 280, 76)
    node('snapshot', 'Capture current context\nMode, component, action IDs\nPlayer pose + offered targets', 35, 365, 300, 88)
    node('client', 'Validate reply identifiers\nRecheck guide state / target\nShow answer, card or marker', 35, 535, 300, 94)
    node('operate', 'Learner operates the VR scene\nUpdate state / clear stale cues', 35, 665, 300, 62)
    node('audio', 'Validate bounded WAV audio\nIn-memory transcription request', 420, 90, 320, 72)
    node('knowledge', 'Trusted project knowledge\nSystem prompt + interaction facts\nShared action catalog', 420, 225, 320, 86)
    node('context', 'Build grounded model context\nQuestion + history + snapshot', 420, 365, 320, 88)
    node('schema', 'Check schema + permitted IDs\nRender canonical action wording\nValidate spatial guidance', 420, 490, 320, 88)
    node('gate', 'Return answer / refusal / error\nAttach request + snapshot IDs', 420, 645, 320, 70)
    node('groq', 'Groq transcription\nWhisper Large V3 Turbo\nChinese / English, no translation', 825, 90, 310, 82)
    node('generate', 'DeepSeek\nGenerate candidate JSON', 825, 365, 310, 88)
    node('review', 'DeepSeek semantic review\nScope + support + instructions', 825, 545, 310, 82)

    down = 'exitX=0.5;exitY=1;entryX=0.5;entryY=0;'
    left = 'exitX=0;exitY=0.5;entryX=1;entryY=0.5;'
    edge('audio-upload', 'input', 'audio', 'WAV')
    edge('asr-request', 'audio', 'groq')
    edge('asr-text', 'groq', 'edit', 'transcript via gateway', [(980,195),(185,195)], exits=down)
    edge('typed-entry', 'input', 'edit', exits=down)
    edge('send', 'edit', 'snapshot', exits=down)
    edge('chat-request', 'snapshot', 'context')
    edge('facts', 'knowledge', 'context', exits=down)
    edge('generation-request', 'context', 'generate')
    edge('candidate', 'generate', 'schema', 'candidate', [(980,473),(580,473)], exits=down)
    edge('repair', 'schema', 'context', 'one retry', [(405,534),(405,410)], dashed=True,
         exits='exitX=0;exitY=0.5;entryX=0;entryY=0.5;')
    edge('semantic-request', 'schema', 'review', 'non-refusal', [(785,534),(785,586)])
    edge('review-result', 'review', 'gate', 'review result', [(980,680)], exits='exitX=0.5;exitY=1;entryX=1;entryY=0.5;')
    edge('bypass', 'schema', 'gate', 'refusal / final error', dashed=True, exits=down)
    edge('reply', 'gate', 'client', '', [(375,680),(375,582)], exits=left)
    edge('read-and-act', 'client', 'operate', exits=down)
    edge('state-loop', 'operate', 'snapshot', '', [(20,696),(20,409)],
         exits='exitX=0;exitY=0.5;entryX=0;entryY=0.5;')
    node('note', 'Local greetings / idle prompts use no API. Models recommend; learners execute. Markers clear on approach or state change.',
         10, 757, 1150, 38, 'strokeColor=none;fillColor=none;fontSize=16;')
    ET.indent(document)
    OUT.mkdir(exist_ok=True)
    target = OUT / 'fig03_architecture.drawio'
    ET.ElementTree(document).write(target, encoding='utf-8', xml_declaration=True)
    print(target)


if __name__ == '__main__':
    build()
