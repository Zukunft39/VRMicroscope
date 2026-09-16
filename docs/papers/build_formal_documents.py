"""Build editable, two-column working papers from the two Formal Markdown sources.

Dependencies: python -m pip install --target .paper_tools pypandoc_binary python-docx
Run from the project root: python docs/papers/build_formal_documents.py
"""
from pathlib import Path
import re
import sys
from copy import deepcopy

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / '.paper_tools'))
import pypandoc
from docx import Document
from docx.shared import Inches, Pt
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.enum.text import WD_ALIGN_PARAGRAPH

OUT = ROOT / 'docs/papers/formatted'
OUT.mkdir(exist_ok=True)


def clean(source, english):
    notes = '\n'.join(line for line in source.splitlines() if line.startswith('>'))
    protocol = re.search(r'### 6\.6[\s\S]*?(?=## 7\.)', source).group()
    source = source.replace(protocol, '')
    lines = []
    for line in source.splitlines():
        if line.startswith('>'):
            caption = re.match(r'> \*\*(?:Proposed caption:|拟图注：)\*\* (.*)', line)
            if caption:
                lines.extend(['', caption.group(1), ''])
            else:
                figure = re.search(r'(?:FIGURE |插图 )([1-4]) ', line)
                if figure:
                    n = figure.group(1)
                    lines.extend(['', f'FIGURE_SLOT_{n}', ''])
            continue
        lines.append(line)
    text = '\n'.join(lines)
    if english:
        text = text.replace('This draft retains completion counts and questionnaire means as descriptive results.',
                            'Completion counts and questionnaire means are reported descriptively.')
        text = text.replace('Identify any additional generative tools used for text, figures, or code and confirm the final disclosure against actual use.', '')
        extra = ('A planned follow-up will evaluate live-model guidance using fixed state scenarios, '
                 'distinguishing action availability from correspondence to the learning goal. '
                 'It will report final outcomes and end-to-end latency, including repair and review, '
                 'with out-of-scope requests analyzed separately. This evaluation has not yet been conducted.\n\n')
        marker = '## Research Ethics and Generative AI Disclosure'
        text = text.replace(marker, extra + marker)
        text = text.replace(marker, marker + '\n\n[Ethics review and participant-consent statement to be completed from the study records.]')
    else:
        extra = ('后续将以固定状态情境开展真实模型指引评价，区分动作可用性与学习目标匹配性，'
                 '报告包含修复和审核的最终结果及端到端延迟，并单独分析范围外问题。该评价尚未实施。\n\n')
        marker = '## 研究伦理与生成式 AI 使用说明'
        text = text.replace(marker, extra + marker)
        text = text.replace(marker, marker + '\n\n[伦理审查与参与者知情同意说明：待依据原始研究记录补齐。]')
        text = re.sub(r'请?根据实际使用情况.*?。', '', text)
        text = text.replace('若还使用其他生成式工具制作正文、图像或代码，应补充工具和用途；最终声明应按实际使用情况确认。', '')
    return text, notes + '\n\n' + protocol


def section_props(base, columns):
    sect = deepcopy(base)
    kind = sect.find(qn('w:type'))
    if kind is None:
        kind = OxmlElement('w:type')
        sect.insert(0, kind)
    kind.set(qn('w:val'), 'continuous')
    cols = sect.find(qn('w:cols'))
    if cols is None:
        cols = OxmlElement('w:cols')
        sect.append(cols)
    cols.set(qn('w:num'), str(columns))
    cols.set(qn('w:space'), '360')
    return sect


def break_after(element, base, columns):
    p = OxmlElement('w:p')
    pr = OxmlElement('w:pPr')
    pr.append(section_props(base, columns))
    spacing = OxmlElement('w:spacing')
    spacing.set(qn('w:after'), '0')
    spacing.set(qn('w:before'), '0')
    pr.append(spacing)
    p.append(pr)
    element.addnext(p)


def style_document(path, english):
    doc = Document(path)
    sec = doc.sections[0]
    sec.page_width, sec.page_height = Inches(8.5), Inches(11)
    sec.top_margin, sec.bottom_margin = Inches(.75), Inches(1)
    sec.left_margin = sec.right_margin = Inches(.625)
    for name in ['Normal', 'Body Text', 'First Paragraph', 'Compact']:
        style = doc.styles[name] if name in doc.styles else doc.styles['Normal']
        style.font.name = 'Times New Roman'
        style.font.size = Pt(10)
        style.element.get_or_add_rPr().get_or_add_rFonts().set(qn('w:eastAsia'), '宋体')
        style.paragraph_format.space_after = Pt(3)
        style.paragraph_format.line_spacing = 1
        style.paragraph_format.widow_control = True
    for name, size in [('Heading 1', 10), ('Heading 2', 10), ('Heading 3', 10)]:
        st = doc.styles[name]
        st.font.name = 'Times New Roman'
        st.font.size = Pt(size)
        st.font.color.rgb = __import__('docx').shared.RGBColor(0, 0, 0)
        st.paragraph_format.keep_with_next = True
        st.paragraph_format.space_before = Pt(8)
        st.paragraph_format.space_after = Pt(4)
    paragraphs = doc.paragraphs
    paragraphs[0].style = doc.styles['Title']
    paragraphs[0].alignment = WD_ALIGN_PARAGRAPH.CENTER
    for r in paragraphs[0].runs:
        r.font.name = 'Times New Roman'
        r.font.size = Pt(22 if english else 20)
    paragraphs[1].alignment = WD_ALIGN_PARAGRAPH.CENTER
    base = deepcopy(sec._sectPr)
    old_cols = sec._sectPr.find(qn('w:cols'))
    if old_cols is not None:
        sec._sectPr.remove(old_cols)
    sec._sectPr.append(section_props(base, 2).find(qn('w:cols')))
    break_after(paragraphs[1]._p, base, 1)
    romans = ['I', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII']
    for p in paragraphs[2:]:
        t = p.text
        if p.style.name == 'Heading 2':
            match = re.match(r'(\d+)\. (.*)', t)
            if match:
                p.text = romans[int(match.group(1))-1] + '. ' + match.group(2)
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        elif p.style.name == 'Heading 3':
            match = re.match(r'\d+\.(\d+) (.*)', t)
            if match:
                p.text = chr(64+int(match.group(1))) + '. ' + match.group(2)
        elif t.startswith('FIGURE_SLOT_'):
            n = t.rsplit('_', 1)[1]
            p.text = f'[Figure {n}: artwork to be inserted]' if english else f'[图 {n}：待插入图片]'
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p.paragraph_format.space_before = Pt(10)
            p.paragraph_format.space_after = Pt(10)
            p.paragraph_format.keep_with_next = True
            for r in p.runs:
                r.italic = True
                r.font.size = Pt(9)
        elif re.match(r'(Fig\. \d|图 \d|Table \d|表 \d)', t):
            p.paragraph_format.space_after = Pt(6)
            for r in p.runs:
                r.font.size = Pt(8)
            if t.startswith(('Table', '表')):
                p.paragraph_format.keep_with_next = True
        elif re.match(r'^\[\d+\]', t):
            p.paragraph_format.left_indent = Inches(.2)
            p.paragraph_format.first_line_indent = Inches(-.2)
            for r in p.runs:
                r.font.size = Pt(8)
        elif p._p.xpath('.//m:oMathPara'):
            p.paragraph_format.space_before = Pt(4)
            p.paragraph_format.space_after = Pt(4)
        else:
            p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    for table in doc.tables:
        wide = len(table.columns) >= 3
        width = 7.25 if wide else 3.5
        table.autofit = False
        for col in table.columns:
            col.width = Inches(width/len(table.columns))
        for row in table.rows:
            for cell in row.cells:
                cell.width = Inches(width/len(table.columns))
                for p in cell.paragraphs:
                    p.paragraph_format.space_after = Pt(3)
                    for r in p.runs:
                        r.font.size = Pt(8)
            # Permit long rows to flow; do not force a full table onto a fresh page.
        for cell in table.rows[0].cells:
            for p in cell.paragraphs:
                for r in p.runs:
                    r.bold = True
        if wide:
            caption = table._tbl.getprevious()
            previous = caption.getprevious()
            if previous is not None:
                break_after(previous, base, 2)
            break_after(table._tbl, base, 1)
    doc.core_properties.title = paragraphs[0].text
    doc.core_properties.author = 'Anonymous'
    doc.core_properties.comments = 'Formatted working manuscript; figures and author study declarations remain pending.'
    doc.save(path)


def main():
    checklist = ['# 论文排版与待补材料\n',
        '已生成中英文双栏 Word 排版稿，公式为可编辑 Word 数学对象。原始 Formal.md 未改动。',
        '当前文件是正式版式的工作稿：图像、伦理说明及研究记录核对仍需完成；最终页数应在 Word/PDF 中检查，未宣称已经满足投稿页限。',
        '完整的未实施 AI 评价协议移到本清单，正文后续工作仅保留计划摘要。插图 1–4 保留简短占位和图注，图 5 暂不进入论文。\n']
    for name, english in [('AIxVR2027_VRMicroscope_Draft_Formal', False), ('AIxVR2027_VRMicroscope_Paper_EN_Formal', True)]:
        source = (OUT.parent / (name+'.md')).read_text(encoding='utf-8-sig')
        content, notes = clean(source, english)
        target = OUT / (name+'.docx')
        pypandoc.convert_text(content, 'docx', format='markdown+tex_math_dollars', outputfile=str(target))
        style_document(target, english)
        checklist.extend(['\n## '+name+'\n', notes])
        d = Document(target)
        print(f'{target.name}: paragraphs={len(d.paragraphs)}, tables={len(d.tables)}, display_equations={len(d.element.xpath(".//m:oMathPara"))}')
    (OUT / 'AUTHOR_CHECKLIST.md').write_text('\n\n'.join(checklist), encoding='utf-8')


if __name__ == '__main__':
    main()
