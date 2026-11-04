"""Correct the delivered gloves' chirality in a LOCAL derivative, preserving the source.

Run in an isolated background Blender. The production rig renamed Left/Right after
fitting donor hands; consequently each wrist carries the opposite anatomical glove.
Reflect the HVR skin and finger rest bones through the palm plane, leaving the
authored dorsal armour in place. This corrects handedness AND the palm side without
changing the body, wrist, skin weights, joint names or the curl control direction.
The derived geometry remains subject to the local HVR licence.
"""
import argparse
import json
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

args = argparse.ArgumentParser()
args.add_argument('--source-root', required=True)
args.add_argument('--project-root', required=True)
opt = args.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(opt.source_root) / 'meshy_output/astronaut-20261103/VoidForge_Astronaut_Rigged.blend'
output = Path(opt.project_root) / 'Assets/ThirdParty/VoidForgeIntegration/CorrectedAstronaut'
output.mkdir(parents=True, exist_ok=True)
assert bpy.app.background
bpy.ops.wm.open_mainfile(filepath=str(source))
arm = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and any(m.type == 'ARMATURE' and m.object == arm for m in o.modifiers)]
report = {'source': str(source), 'source_unchanged': True, 'hands': []}
mirrors = {}
for side, label in [('L', 'Left'), ('R', 'Right')]:
    wrist = arm.matrix_world @ arm.data.bones[label + 'Hand'].head_local
    middle = arm.matrix_world @ arm.data.bones[side + 'MiddleFinger1'].head_local
    forward = (middle - wrist).normalized()
    back = Vector((0, 1, 0))
    normal = back
    linear = Matrix.Identity(3)
    for i in range(3):
        for j in range(3):
            linear[i][j] -= 2 * normal[i] * normal[j]
    reflection = linear.to_4x4()
    reflection.translation = wrist - linear @ wrist
    mirrors[side] = reflection
    glove = next(o for o in meshes if o.name == 'VF_Glove_' + label + '_FiveDigits')
    skin_slots = {i for i, mat in enumerate(glove.data.materials) if 'Graphite' in mat.name or 'Joints' in mat.name}
    skin_vertices = {v for polygon in glove.data.polygons if polygon.material_index in skin_slots for v in polygon.vertices}
    assert len(skin_vertices) == 1183, ('Unexpected HVR topology', len(skin_vertices))
    local_reflection = glove.matrix_world.inverted() @ reflection @ glove.matrix_world
    for i in skin_vertices:
        glove.data.vertices[i].co = local_reflection @ glove.data.vertices[i].co
    bm = bmesh.new()
    bm.from_mesh(glove.data)
    bmesh.ops.reverse_faces(bm, faces=[f for f in bm.faces if f.material_index in skin_slots])
    bm.normal_update()
    bm.to_mesh(glove.data)
    bm.free()
    glove.data.normals_split_custom_set([(0., 0., 0.)] * len(glove.data.loops))
    before = arm.matrix_world @ arm.data.bones[side + 'Thumb1'].head_local
    after = reflection @ before
    report['hands'].append({'side': side, 'thumb_before': list(before), 'thumb_after': list(after),
                            'wrist': list(wrist), 'vertices': len(glove.data.vertices)})
bpy.ops.object.select_all(action='DESELECT')
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='EDIT')
for bone in arm.data.edit_bones:
    if bone.name[0:1] not in mirrors or not any(bone.name.startswith(bone.name[0] + f) for f in ('Thumb', 'IndexFinger', 'MiddleFinger', 'RingFinger', 'PinkyFinger')):
        continue
    reflect = arm.matrix_world.inverted() @ mirrors[bone.name[0]] @ arm.matrix_world
    bone.head = reflect @ bone.head
    bone.tail = reflect @ bone.tail
    bone.align_roll(Vector((0, 1, 0)))
bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.view_layer.update()
for hand in report['hands']:
    # Palms face forward in the rest pose: both thumbs must point laterally.
    wrist = Vector(hand['wrist'])
    thumb = Vector(hand['thumb_after'])
    middle = arm.matrix_world @ arm.data.bones[hand['side'] + 'MiddleFinger1'].head_local
    radial = thumb - wrist
    radial -= (middle - wrist).normalized() * radial.dot((middle - wrist).normalized())
    assert radial.x * wrist.x > 0, ('Wrong anatomical glove', hand)
    hand['thumb_lateral_with_forward_palm'] = True
bpy.ops.object.select_all(action='DESELECT')
for obj in [arm] + meshes:
    obj.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=str(output / 'Astronaut_AnatomicalHands.fbx'), use_selection=True,
                          add_leaf_bones=False, bake_anim=False, path_mode='AUTO', axis_forward='-Z', axis_up='Y')
(output / 'hands-validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('VOIDFORGE_ANATOMICAL_HANDS_OK', json.dumps(report))
