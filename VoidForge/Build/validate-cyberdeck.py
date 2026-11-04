"""Prepare a private Unity project; never share or delete the live project's caches."""
import json
import re
import shutil
import sys
from pathlib import Path
from importlib.util import spec_from_file_location, module_from_spec

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / 'Library/HudGraphicsValidation20261103'
spec = spec_from_file_location('stage', ROOT / 'Build/stage-cyberdeck.py')
stage = module_from_spec(spec)
spec.loader.exec_module(stage)


def copy_changed(source, target):
    source, target = Path(source), Path(target)
    if target.exists():
        if source.suffix == '.meta':
            a = re.search(r'guid: (\w+)', source.read_text())
            b = re.search(r'guid: (\w+)', target.read_text())
            if a and b and a.group(1) != b.group(1):
                raise RuntimeError('Conflicting GUID: ' + str(target))
            return str(target)  # Retain native importer settings in the private cache.
        if source.read_bytes() == target.read_bytes():
            return str(target)
    return shutil.copy2(source, target)


def prepare():
    for path in [ROOT/'Assets/VoidForge/Editor/CyberdeckWorkshop.cs']:
        stage.meta(path)
    for relative in ['Assets/VoidForge/Scripts/UI/Cyberdeck', 'Assets/VoidForge/Art/UI/Cyberdeck']:
        source = ROOT / relative
        shutil.copytree(source, PROJECT / relative, dirs_exist_ok=True, copy_function=copy_changed)
        (PROJECT / (relative + '.meta')).parent.mkdir(parents=True, exist_ok=True)
        copy_changed(str(source) + '.meta', PROJECT / (relative + '.meta'))
    editor = PROJECT/'Assets/VoidForge/Editor'
    editor.mkdir(parents=True, exist_ok=True)
    for suffix in ['', '.meta']:
        copy_changed(ROOT/('Assets/VoidForge/Editor/CyberdeckWorkshop.cs'+suffix), editor/('CyberdeckWorkshop.cs'+suffix))
    # Canonical resource GUIDs win over earlier private validation experiments.
    relative = 'Assets/Resources/VoidForge/Cyberdeck'
    if (ROOT/relative).exists():
        shutil.copytree(ROOT/relative, PROJECT/relative, dirs_exist_ok=True)
        shutil.copy2(ROOT/(relative+'.meta'), PROJECT/(relative+'.meta'))
    for name in ['CyberdeckReview.unity', 'CyberdeckReview.unity.meta']:
        source = ROOT/'Assets/VoidForge/Scenes'/name
        if source.exists():
            target = PROJECT/'Assets/VoidForge/Scenes'/name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)
    # Previous isolated shader validation used the same shader GUID at Assets/.
    # Archive only those two known files, so the active Assets tree has no duplicate GUID.
    archive = PROJECT/'Library/PreviousMaskCheck'
    archive.mkdir(parents=True, exist_ok=True)
    for suffix in ['', '.meta']:
        old = PROJECT/('Assets/VoidForgeGlyphMask.shader'+suffix)
        if old.exists():
            target = archive/old.name
            if not target.exists():
                shutil.move(str(old), str(target))
    path = PROJECT/'Packages/manifest.json'
    data = json.loads(path.read_text(encoding='utf-8-sig'))
    data['dependencies']['com.unity.inputsystem'] = 'file:' + next((ROOT/'Library/PackageCache').glob('com.unity.inputsystem@*')).as_posix()
    for module in ['xr', 'physics']:
        data['dependencies']['com.unity.modules.'+module] = '1.0.0'
    path.write_text(json.dumps(data, indent=2), encoding='utf-8')
    settings = PROJECT/'ProjectSettings/ProjectSettings.asset'
    content = settings.read_text(encoding='utf-8')
    content = re.sub(r'm_ActiveColorSpace: \d+', 'm_ActiveColorSpace: 1', content)
    if 'activeInputHandler:' in content:
        content = re.sub(r'activeInputHandler: \d+', 'activeInputHandler: 1', content)
    else:
        content += '  activeInputHandler: 1\n'
    settings.write_text(content, encoding='utf-8')
    print('Private Unity project ready:', PROJECT)


def collect():
    report = ROOT/'docs/validation/cyberdeck-2026-11-03/unity-validation.json'
    if not report.exists() or not json.loads(report.read_text())['success']:
        raise RuntimeError('Successful native Unity validation required before collecting generated assets.')
    if Path(json.loads(report.read_text()).get('projectPath', '')).resolve() != PROJECT.resolve():
        raise RuntimeError('The successful report must come from this private validation project, not another Unity run.')
    for relative in ['Assets/Resources/VoidForge/Cyberdeck', 'Assets/VoidForge/Art/UI/Cyberdeck']:
        for path in (PROJECT/relative).rglob('*.meta'):
            canonical = ROOT/path.relative_to(PROJECT)
            if canonical.exists():
                a = re.search(r'guid: (\w+)', path.read_text())
                b = re.search(r'guid: (\w+)', canonical.read_text())
                if a and b and a.group(1) != b.group(1):
                    raise RuntimeError('Refusing to replace canonical GUID: ' + str(canonical))
        shutil.copytree(PROJECT/relative, ROOT/relative, dirs_exist_ok=True)
        if (PROJECT/(relative+'.meta')).exists():
            shutil.copyfile(PROJECT/(relative+'.meta'), ROOT/(relative+'.meta'))
    for name in ['CyberdeckReview.unity', 'CyberdeckReview.unity.meta']:
        shutil.copyfile(PROJECT/'Assets/VoidForge/Scenes'/name, ROOT/'Assets/VoidForge/Scenes'/name)
    print('Collected validated native prefab, graphics, material, review scene and Unity .meta files.')


if __name__ == '__main__':
    collect() if '--collect' in sys.argv else prepare()
