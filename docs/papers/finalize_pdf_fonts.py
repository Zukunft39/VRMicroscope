"""Embed omitted Windows TrueType fonts in native Word PDF exports.

Keeps Word's content streams, math layout, Unicode maps and font widths intact.
Requires PyMuPDF and fontTools (also searched in the project's .paper_tools).
This is ordinary PDF output; it does not claim PDF/A conformance.
"""
from pathlib import Path
import argparse
import os
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / '.paper_tools'))
import pymupdf
from fontTools.ttLib import TTFont

FONT_FILES = {
    'TimesNewRomanPSMT': 'times.ttf',
    'TimesNewRomanPS-BoldMT': 'timesbd.ttf',
    'TimesNewRomanPS-ItalicMT': 'timesi.ttf',
    'TimesNewRomanPS-BoldItalicMT': 'timesbi.ttf',
    'ArialMT': 'arial.ttf',
    'Arial-BoldMT': 'arialbd.ttf',
    'Arial-ItalicMT': 'ariali.ttf',
    'Arial-BoldItalicMT': 'arialbi.ttf',
}


def finalize(path):
    path = Path(path).resolve()
    pending = path.with_name(path.stem + '.fonts-pending.pdf')
    doc = pymupdf.open(path)
    original_text = [page.get_text() for page in doc]
    fonts = {f[0]: f for page in doc for f in page.get_fonts(full=True)}
    added = []
    try:
        for xref, item in fonts.items():
            if doc.extract_font(xref)[3]:
                continue
            name = item[3]
            if name not in FONT_FILES or item[2] != 'TrueType':
                raise ValueError(f'Unembedded font needs manual review: {name}')
            font_path = Path(os.environ.get('WINDIR', 'C:/Windows')) / 'Fonts' / FONT_FILES[name]
            with TTFont(font_path) as font:
                if font['name'].getDebugName(6) != name:
                    raise ValueError(f'PostScript font-name mismatch: {font_path}')
                if font['OS/2'].fsType & (0x0002 | 0x0200):
                    raise ValueError(f'Font embedding is restricted: {font_path}')
            kind, reference = doc.xref_get_key(xref, 'FontDescriptor')
            if kind != 'xref':
                raise ValueError(f'Missing FontDescriptor: {name}')
            descriptor = int(reference.split()[0])
            data = font_path.read_bytes()
            stream = doc.get_new_xref()
            doc.update_object(stream, f'<< /Length1 {len(data)} >>')
            doc.update_stream(stream, data, compress=True)
            doc.xref_set_key(descriptor, 'FontFile2', f'{stream} 0 R')
            added.append(name)
        doc.save(pending, garbage=3, deflate=True)
    finally:
        doc.close()
    with pymupdf.open(pending) as check:
        if [p.get_text() for p in check] != original_text:
            raise ValueError('PDF text changed while embedding fonts')
        for xref in {f[0] for p in check for f in p.get_fonts(full=True)}:
            if not check.extract_font(xref)[3]:
                raise ValueError('An unembedded font remains')
    pending.replace(path)
    print(f'{path.name}: embedded {len(added)} missing fonts; text and page count preserved')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('pdf', type=Path, nargs='+')
    for pdf in parser.parse_args().pdf:
        finalize(pdf)
