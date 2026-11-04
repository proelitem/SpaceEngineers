"""Stage the authored graphics without changing pixels; preserve stable Unity GUIDs."""
import json
import shutil
import tarfile
import uuid
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Art/UI/Cyberdeck/2026-11-03-v1'
DEST = ROOT / 'Assets/VoidForge/Art/UI/Cyberdeck'
NAMESPACE = uuid.UUID('947325b9-cc43-43ce-9b86-ced186cb18f4')


def meta(path):
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    guid = uuid.uuid5(NAMESPACE, path.relative_to(ROOT).as_posix()).hex
    target.write_text('fileFormatVersion: 2\nguid: ' + guid + '\n' +
                      ('folderAsset: yes\n' if path.is_dir() else ''), encoding='utf-8')


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    trims = []
    for item in json.loads((SOURCE / 'manifest.json').read_text(encoding='utf-8'))['assets']:
        source = SOURCE / item['file']
        target = DEST / (item['id'] + '.png')
        if not target.exists() or source.read_bytes() != target.read_bytes():
            shutil.copyfile(source, target)
        with Image.open(source) as im:
            channel = im.convert('L') if item['usage_format'] == 'luminance-mask' else im.getchannel('A')
            bbox = channel.point(lambda p: 255 if p > 32 else 0).getbbox()
            # Small optical breathing room, stored as UV coordinates, no raster edit.
            x0, y0, x1, y1 = bbox or (0, 0, im.width, im.height)
            pad = round(max(x1 - x0, y1 - y0) * .035)
            x0, y0 = max(0, x0 - pad), max(0, y0 - pad)
            x1, y1 = min(im.width, x1 + pad), min(im.height, y1 + pad)
            trims.append(dict(id=item['id'], x=x0/im.width, y=1-y1/im.height,
                              w=(x1-x0)/im.width, h=(y1-y0)/im.height))
    (DEST / 'icon-trim.json').write_text(json.dumps(dict(icons=trims), indent=2), encoding='utf-8')
    shader = SOURCE / 'Unity/VoidForgeGlyphMask.shader'
    shutil.copyfile(shader, DEST / shader.name)
    if not (DEST / (shader.name + '.meta')).exists():
        shutil.copyfile(str(shader) + '.meta', DEST / (shader.name + '.meta'))
    fonts = SOURCE/'fonts'
    if (fonts/'DejaVuSans-Bold.ttf').exists():
        shutil.copyfile(fonts/'DejaVuSans-Bold.ttf', DEST/'DejaVuSans-Bold.ttf')
        shutil.copyfile(fonts/'LICENSE', DEST/'DejaVu-LICENSE.txt')
        shutil.copyfile(fonts/'provenance.json', DEST/'font-provenance.json')
    package = next((ROOT / 'Library/PackageCache').glob('com.unity.ugui@*/Package Resources/TMP Essential Resources.unitypackage'))
    with tarfile.open(package) as archive:
        for entry in archive.getmembers():
            if entry.name.endswith('/pathname'):
                name = archive.extractfile(entry).read().decode()
                if name.endswith(('LiberationSans.ttf', 'LiberationSans - OFL.txt')):
                    output = DEST / Path(name).name
                    output.write_bytes(archive.extractfile(entry.name.rsplit('/', 1)[0] + '/asset').read())
    for folder in [ROOT/'Assets/VoidForge/Art/UI', DEST,
                   ROOT/'Assets/VoidForge/Scripts/UI/Cyberdeck']:
        meta(folder)
        for path in sorted(folder.iterdir()):
            if not path.name.endswith('.meta'):
                meta(path)
    print(f'Staged {len(trims)} original graphics, UV trims, shader and licensed Liberation Sans.')


if __name__ == '__main__':
    main()
