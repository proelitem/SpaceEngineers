"""Attach actual file evidence and independent readback outcome to the report."""
from pathlib import Path
import json, hashlib
BASE=Path(__file__).resolve().parents[3]
artifact=BASE/'Art/HandTools'
p=artifact/'modeler3d_report.json';report=json.loads(p.read_text(encoding='utf-8'))
assert report['owner']=='VoidForge.HandTools.20261103'
def entry(path):
    assert path.exists() and path.stat().st_size>100
    return {'path':str(path.relative_to(BASE)).replace('\\','/'),'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
report['source_files']=[entry(p) for p in sorted((artifact/'Source').glob('*.blend'))]
report['previews']=[entry(p) for p in sorted((artifact/'Previews').glob('*.png'))]
report['scripts']=[entry(p) for p in sorted((artifact/'Scripts').glob('*.py'))]
readback=artifact/'Validation/export-readback.json'
if readback.exists():
    validation=json.loads(readback.read_text(encoding='utf-8-sig'))
    report['independent_readback']={'success':validation['success'],'evidence':str(readback.relative_to(BASE)).replace('\\','/'),'method':validation['method'],'checked_exports':len(validation['exports'])}
    if validation['success']:
        report['limitations']=[s for s in report['limitations'] if s!='Independent FBX/GLB reimport qualification pending parent agent']
report['native_session_incident']={'message':'Connection to Blender lost: [WinError 10054] An existing connection was forcibly closed by the remote host','observed_trace':'blender.crash.txt: EXCEPTION_ACCESS_VIOLATION (0xc0000005), library_foreach_ID_link / deg_update_eval_copy_datablock','recovery':'Recreated owned scene from authored script, saved authoring checkpoint, changed compilation to resolve original bpy.data.materials IDs instead of temporary evaluated mesh material IDs; compilation and all six exports subsequently succeeded. Exact causal attribution not independently proven.'}
report['inspection'].append('Final native studio gallery and three individual PNGs rendered and visually inspected') if 'Final native studio gallery and three individual PNGs rendered and visually inspected' not in report['inspection'] else None
unity=artifact/'Validation/unity-import.json'
if unity.exists():
    imported=json.loads(unity.read_text(encoding='utf-8-sig'))
    report['unity_import']={'success':imported['success'],'unity_version':imported['unity_version'],
        'evidence':str(unity.relative_to(BASE)).replace('\\','/'),
        'scope':'Isolated geometry import; main gameplay scene not modified',
        'shaders':sorted({shader for tool in imported['tools'] for shader in tool['shaders']}),
        'limitations':'URP material conversion, XR interaction and Quest performance remain unverified'}
report['parent_scene_check']={'scene':'VF_HandTools_20261103','objects_observed':169,
    'viewport_screenshot_error':'WinError 10054 on final parent screenshot request; subsequent get_scene_info succeeded, models remained present. Saved native studio previews inspected instead.'}
p.write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
print('Final evidence manifest updated; production_ready remains false pending engine and headset validation')
