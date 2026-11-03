"""Save a recovery copy of the tagged tool scene only; preserves loaded file."""
import bpy
from pathlib import Path
BASE=Path(__file__).resolve().parents[3]
scene=bpy.data.scenes['VF_HandTools_20261103']
assert scene.get('vf_owner')=='VoidForge.HandTools.20261103'
path=BASE/'Art/HandTools/Source/VoidForge_HandTools_Authoring.blend'
path.parent.mkdir(parents=True,exist_ok=True)
bpy.data.libraries.write(str(path),{scene},compress=True)
print('Authoring checkpoint:',path,path.stat().st_size)
