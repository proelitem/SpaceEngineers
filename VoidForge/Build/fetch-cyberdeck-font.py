"""Fetch one unmodified free font and its notices from the official release archive."""
import hashlib
import io
import json
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'Art/UI/Cyberdeck/2026-11-03-v1/fonts'
URL = 'https://github.com/dejavu-fonts/dejavu-fonts/releases/download/version_2_37/dejavu-fonts-ttf-2.37.zip'


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    if (OUT/'DejaVuSans-Bold.ttf').exists():
        print('Official DejaVu Sans Bold already staged.')
        return
    request = urllib.request.Request(URL, headers={'User-Agent': 'VoidForge-Cyberdeck-Asset-Staging'})
    with urllib.request.urlopen(request, timeout=30) as response:
        data = response.read()
    records = []
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        for name in archive.namelist():
            basename = name.rsplit('/', 1)[-1]
            if basename in ['DejaVuSans-Bold.ttf', 'LICENSE', 'README']:
                content = archive.read(name)
                (OUT/basename).write_bytes(content)
                records.append(dict(file=basename, sha256=hashlib.sha256(content).hexdigest()))
    if not (OUT/'DejaVuSans-Bold.ttf').exists() or not (OUT/'LICENSE').exists():
        raise RuntimeError('Expected font and licensing notices missing from official archive.')
    (OUT/'provenance.json').write_text(json.dumps(dict(source=URL, license='https://dejavu-fonts.github.io/License.html',
        archive_sha256=hashlib.sha256(data).hexdigest(), files=records), indent=2), encoding='utf-8')
    print(json.dumps(records))


if __name__ == '__main__':
    main()
