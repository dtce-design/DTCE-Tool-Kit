"""Validate a rendered report PDF: A4, nominal >=7pt text, and horizontal bounds.
Developer-only dependency: PyMuPDF. No effect on desktop runtime dependencies.
Usage: python scripts/Validate-Report-Pdf.py report.pdf
"""
import json, sys
import fitz

document = fitz.open(sys.argv[1])
fonts = []
for number, page in enumerate(document, 1):
    if abs(page.rect.width - 595.28) > 1 or abs(page.rect.height - 841.89) > 1:
        raise ValueError(f'Page {number}: expected A4 portrait')
    for block in page.get_text('dict')['blocks']:
        for line in block.get('lines', []):
            for span in line['spans']:
                if not span['text'].strip():
                    continue
                fonts.append(span['size'])
                # Chromium rounds nominal 7pt glyph sizes to approximately 6.997pt.
                if span['size'] < 6.98:
                    raise ValueError(f'Page {number}: text below nominal 7pt: {span["text"]}')
                if span['bbox'][0] < 25 or span['bbox'][2] > page.rect.width - 20:
                    raise ValueError(f'Page {number}: text outside printable horizontal bounds')
if not fonts:
    raise ValueError('No selectable text: cannot verify report font sizes')
print(json.dumps({'pages': len(document), 'minimum_font_pt': round(min(fonts), 3),
                  'paper': 'A4', 'status': 'PASS'}))
