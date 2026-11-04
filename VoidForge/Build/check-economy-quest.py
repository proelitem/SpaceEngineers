"""Technical Quest smoke test. Installs only with --install; never clears app data.

Uses Unity Hub's Android SDK. --restart verifies persistence across an app restart.
It does not simulate controller actions or certify human headset usability.
"""
import argparse
from datetime import datetime, timezone
from decimal import Decimal
import hashlib
import json
from pathlib import Path
import re
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
ADB = Path('D:/Unity/6000.4.0f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe')
PACKAGE = 'com.proelitem.voidforge'
ACTIVITY = PACKAGE + '/com.unity3d.player.UnityPlayerGameActivity'
OUT = ROOT / 'docs/validation/economy-2026-11-03/quest-validation.json'
SAVE = '/sdcard/Android/data/' + PACKAGE + '/files/economy-world-v1.json'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--serial', required=True)
    parser.add_argument('--apk', type=Path, required=True)
    parser.add_argument('--install', action='store_true')
    parser.add_argument('--restart', action='store_true')
    args = parser.parse_args()
    report = {'success': False, 'error': '', 'utc': datetime.now(timezone.utc).isoformat(),
              'package': PACKAGE, 'headsetTested': False, 'checks': []}

    def adb(*parts, required=True, timeout=60):
        proc = subprocess.run([str(ADB), '-s', args.serial, *parts], capture_output=True,
                              encoding='utf-8', errors='replace', timeout=timeout)
        if required and proc.returncode:
            raise RuntimeError('adb ' + ' '.join(parts[:3]) + ': ' + (proc.stdout + proc.stderr)[-1600:])
        return proc

    def check(condition, description):
        if not condition:
            raise RuntimeError(description)
        report['checks'].append(description)
        print('PASS ' + description, flush=True)

    def startup(label):
        adb('shell', 'am', 'start', '-n', ACTIVITY)
        deadline = time.monotonic() + 90
        logs = ''
        while time.monotonic() < deadline:
            pid = adb('shell', 'pidof', PACKAGE, required=False).stdout.strip()
            if pid:
                logs = adb('logcat', '-d', '--pid=' + pid.split()[0], '-v', 'threadtime').stdout
                if '[VoidForge Economy] Live inventory' in logs and 'SELFTEST OK' in logs:
                    # Let the adapter take over after the original Digger callback.
                    time.sleep(2)
                    logs = adb('logcat', '-d', '--pid=' + pid.split()[0], '-v', 'threadtime').stdout
                    break
            time.sleep(3)
        (ROOT / 'Logs').mkdir(exist_ok=True)
        (ROOT / 'Logs' / ('economy-quest-' + label + '.log')).write_text(logs, encoding='utf-8')
        check('[VoidForge Economy] Live inventory' in logs, label + ': economy runtime initialized on IL2CPP')
        check('[VoidForge Cyberdeck] Five native 2D views attached' in logs, label + ': five Cyberdeck views attached to the wrist')
        check('SELFTEST OK' in logs, label + ': Digger collider self-test completed')
        errors = re.findall(r'(?:[A-Za-z]+Exception:|FATAL EXCEPTION|Fatal signal|Catalogue or block prefabs missing|SAUVEGARDE INCOMPATIBLE)', logs)
        report[label + 'Errors'] = sorted(set(errors))
        check(not errors, label + ': no managed exception or native crash in captured app log')
        report[label + 'StartupEvidence'] = [line.strip() for line in logs.splitlines()
            if '[VoidForge Economy]' in line or 'SELFTEST OK' in line or '[VoidForge Cyberdeck]' in line][-12:]

    def saved_world(label):
        result = adb('shell', 'run-as', PACKAGE, 'cat', SAVE, required=False)
        if result.returncode:
            result = adb('shell', 'cat', SAVE)
        data = json.loads(result.stdout)
        definition = json.loads((ROOT / 'Assets/Resources/VoidForge/Economy/Catalogue.json').read_text(encoding='utf-8-sig'))
        check(data.get('format') == 1 and data.get('definitionVersion') == definition['version'], label + ': valid native persistent save/profile')
        blocks = data.get('blocks', [])
        check(bool(blocks) and len({b['uid'] for b in blocks}) == len(blocks), label + ': persisted blocks have unique identities')
        for source in ('backpack', 'cargo'):
            amounts = [Decimal(row['amount']) for row in data.get(source, [])]
            check(all(n.is_finite() and n >= 0 for n in amounts), label + ': decimal ' + source + ' deserialized without invalid quantities')
        stable = {key: data.get(key, []) for key in ('backpack', 'cargo', 'blocks', 'deposits', 'committedMining', 'excavations')}
        # Display and verify only game state; do not copy unrelated device app data.
        report[label + 'State'] = {'blocks': len(blocks), 'deposits': len(data.get('deposits', [])),
                                  'backpack': data.get('backpack', []), 'cargo': data.get('cargo', []),
                                  'excavations': len(data.get('excavations', []))}
        return stable

    try:
        check(args.apk.is_file(), 'APK artifact exists')
        with args.apk.open('rb') as artifact:
            apk_hash = hashlib.file_digest(artifact, 'sha256').hexdigest()
        report['apk'] = {'file': str(args.apk.resolve()), 'bytes': args.apk.stat().st_size,
                         'sha256': apk_hash}
        check(adb('get-state').stdout.strip() == 'device', 'target Quest is connected')
        report['model'] = adb('shell', 'getprop', 'ro.product.model').stdout.strip()
        if args.install:
            result = adb('install', '-r', str(args.apk.resolve()), timeout=180)
            check('Success' in result.stdout, 'APK installed with existing app data preserved')
        package = adb('shell', 'dumpsys', 'package', PACKAGE).stdout
        version = re.search(r'versionCode=(\d+)', package)
        report['versionCode'] = int(version.group(1)) if version else None
        check(version is not None, 'installed package version resolves')
        startup('first')
        before = saved_world('first')
        if args.restart:
            adb('shell', 'am', 'force-stop', PACKAGE)
            startup('reload')
            after = saved_world('reload')
            check(before == after, 'stocks, blocks, deposits and mining history preserved across restart without reseeding')
        report['success'] = True
    except Exception as error:
        report['error'] = str(error)
        print('FAIL ' + str(error), flush=True)
    finally:
        OUT.parent.mkdir(parents=True, exist_ok=True)
        OUT.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
        print(OUT, flush=True)
    return 0 if report['success'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
