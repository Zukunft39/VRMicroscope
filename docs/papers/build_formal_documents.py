"""Build editable, two-column working papers from the two Formal Markdown sources.

Dependencies: python -m pip install --target .paper_tools pypandoc_binary python-docx
Run from the project root: python docs/papers/build_formal_documents.py
"""
from pathlib import Path
import re
import sys
import json
import argparse
import html

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / '.paper_tools'))
import pypandoc
from docx import Document

OUT = ROOT / 'docs/papers'
AUTHORS = json.loads((OUT / 'authors.json').read_text(encoding='utf-8'))


def plain_html(value):
    return html.unescape(re.sub(r'<[^>]+>', '', value)).strip()


def extract_figures(source, english):
    figures = {}

    def replace(match):
        block = match.group()
        caption = plain_html(re.search(r'<figcaption>(.*?)</figcaption>', block, re.S).group(1))
        number = int(re.search(r'(?:Fig\.|图)\s*(\d+)', caption).group(1))
        panels = []
        for cell in re.findall(r'<td[^>]*>(.*?)</td>', block, re.S):
            img = re.search(r'<img\s+src="([^"]+)"[^>]*>', cell)
            if img:
                panels.append((img.group(1), plain_html(cell[img.end():])))
        figures[number] = {'caption': caption, 'panels': panels}
        return f'\nFIGUREGROUP{number}\n'

    source = re.sub(r'<figure\b[^>]*>.*?</figure>', replace, source, flags=re.S)
    # Place the compact activity table before the environment figure. This
    # allows the remaining space after Section III-A to be used by the table
    # rather than leaving half a page empty in front of an indivisible figure.
    if 'FIGUREGROUP1' in source:
        source = source.replace('FIGUREGROUP1', '')
        source = re.sub(r'(^\|[^\n]+\n(?:\|[^\n]+\n)+)',
                        lambda m: m.group(1) + '\nFIGUREGROUP1\n\n',
                        source, count=1, flags=re.M)
    # The wide overview already identifies the microscope. Remove the
    # redundant close-up so the environment figure fits a compact 2x2 panel.
    if 1 in figures:
        figures[1]['panels'].pop(1)
        figures[1]['panels'] = [(path, re.sub(r'^\([a-z]\)', '(' + chr(97+i) + ')', label))
                                 for i, (path, label) in enumerate(figures[1]['panels'])]
    # Integrate the speech screenshot into the interaction figure instead of
    # leaving an unnumbered, full-size screenshot in the architecture section.
    source = re.sub(r'\*\*(?:Runtime speech-input evidence|语音输入运行时证据).*?\n\n!\[.*?\]\(<.*?>\)', '', source, flags=re.S)
    if 4 in figures:
        figures[4]['panels'][-1] = (
            'Pic/3.5 语音转文字.png',
            '(d) Speech-input interface.' if english else '(d) 语音输入界面。')
        figures[4]['caption'] = (
            'Fig. 4. Representative assistant interfaces: (a) component description and experiment entry; '
            '(b) contextual explanation; (c) spatial target guidance; and (d) speech input. '
            'These views illustrate interface states, not one uninterrupted session. '
            'The associated NA experiment feedback is shown in Fig. 2(c).'
            if english else
            '图 4. 助手的代表性交互界面：(a) 部件说明与实验入口；(b) 上下文解释；'
            '(c) 空间目标引导；(d) 语音输入。各图表示界面状态，不代表一次连续会话。关联 NA 实验的反馈见图 2(c)。')
    return source, figures


def clean(source, english):
    source, figures = extract_figures(source, english)
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
                figure = re.search(r'(?:FIGURE |插图 )([1-4])(?:\s|｜)', line)
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
    else:
        extra = ('后续将以固定状态情境开展真实模型指引评价，区分动作可用性与学习目标匹配性，'
                 '报告包含修复和审核的最终结果及端到端延迟，并单独分析范围外问题。该评价尚未实施。\n\n')
        marker = '## 研究伦理与生成式 AI 使用说明'
        text = text.replace(marker, extra + marker)
        text = re.sub(r'请?根据实际使用情况.*?。', '', text)
        text = text.replace('若还使用其他生成式工具制作正文、图像或代码，应补充工具和用途；最终声明应按实际使用情况确认。', '')
    # IEEE uses an inline abstract lead-in rather than a section heading.
    text = re.sub(r'## (Abstract|摘要)\s*\n\s*\n', lambda m: '**' + m.group(1) + '—**', text)
    text = text.replace('**Keywords:**', '**Index Terms—**').replace('**关键词：**', '**关键词—**')
    return text, notes + '\n\n' + protocol, figures


def anonymize(text, english):
    """Remove identity-bearing front matter and institutional wording for review."""
    if english:
        front = r'^\*\*Authors:\*\*.*\n\*\*Affiliations:\*\*.*\n\*\*Corresponding Authors:\*\*.*$'
        text, count = re.subn(front, 'Anonymous authors', text, count=1, flags=re.M)
        text = text.replace(
            'In accordance with institutional research ethics guidelines at UESTC,',
            "In accordance with the research ethics guidelines of the authors' institution,")
    else:
        front = r'^\*\*作者：\*\*.*\n\*\*单位：\*\*.*\n\*\*通讯作者：\*\*.*$'
        text, count = re.subn(front, '匿名作者', text, count=1, flags=re.M)
        text = text.replace(
            '根据电子科技大学（UESTC）机构研究伦理指南，',
            '根据作者所在机构的研究伦理指南，')
    if count != 1:
        raise ValueError('Could not locate the author block for anonymous export.')
    return text


def main():
    from ieee_layout import style_document
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--anonymous', action='store_true', help='Generate a review copy without author identities.')
    args = parser.parse_args()
    checklist = ['# 论文排版与待补材料\n',
        '已生成中英文双栏 Word 排版稿，公式为可编辑 Word 数学对象；图 3 已同步至 Formal.md。',
        '图 1、图 2 和图 4 为运行时截图；图 3 已由 draw.io 绘制并嵌入 SVG（附 PNG 兼容图）。伦理声明沿用作者提供的文本，研究记录仍由作者核对；最终页数另见 FINAL_REVIEW.md。',
        '完整的未实施 AI 评价协议移到本清单，正文后续工作仅保留计划摘要。图 5 暂不进入论文，除非取得真实模型测试数据。\n']
    for name, english in [('AIxVR2027_VRMicroscope_Draft_Formal', False), ('AIxVR2027_VRMicroscope_Paper_EN_Formal', True)]:
        source = (OUT / (name+'.md')).read_text(encoding='utf-8-sig')
        content, notes, figures = clean(source, english)
        if args.anonymous:
            content = anonymize(content, english)
        for figure in figures.values():
            figure['root'] = OUT
        suffix = '_Anonymous' if args.anonymous else ''
        target = OUT / (name+suffix+'.docx')
        pypandoc.convert_text(content, 'docx', format='markdown+tex_math_dollars', outputfile=str(target))
        style_document(target, english, figures, AUTHORS, anonymous=args.anonymous)
        checklist.extend(['\n## '+name+'\n', notes])
        d = Document(target)
        print(f'{target.name}: paragraphs={len(d.paragraphs)}, tables={len(d.tables)}, display_equations={len(d.element.xpath(".//m:oMathPara"))}')
    (OUT / 'AUTHOR_CHECKLIST.md').write_text('\n\n'.join(checklist), encoding='utf-8')


if __name__ == '__main__':
    main()
