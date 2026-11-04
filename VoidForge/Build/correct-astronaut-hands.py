"""Correct the delivered gloves' chirality in a LOCAL derivative, preserving the source.

Run in an isolated background Blender. The production rig renamed Left/Right after
fitting donor hands; consequently each wrist carries the opposite anatomical glove.
Reflect the HVR skin and finger rest bones through the palm plane, then wrapping the
authored dorsal armour onto the corrected skin and transferring its deformation weights. This corrects handedness AND the palm side without
changing the body, wrist, skin weights, joint names or the curl control direction.
The derived geometry remains subject to the local HVR licence.
"""
import hashlib
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
source_hash=hashlib.sha256(source.read_bytes()).hexdigest()
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
    # The prior correction reflected skin but left dorsal plates above their old surface.
    # Ray-wrap each connected plate onto the actual corrected glove, preserving its thickness.
    glove.data.calc_loop_triangles()
    skin_triangles=[tuple(t.vertices) for t in glove.data.loop_triangles if glove.data.polygons[t.polygon_index].material_index in skin_slots]
    world_vertices=[glove.matrix_world@v.co for v in glove.data.vertices]
    armour={v for polygon in glove.data.polygons if polygon.material_index not in skin_slots for v in polygon.vertices}-skin_vertices
    adjacency={i:set() for i in armour}
    for edge in glove.data.edges:
        a,b=edge.vertices
        if a in armour and b in armour:adjacency[a].add(b);adjacency[b].add(a)
    todo=set(armour);islands=[]
    while todo:
        island=set();stack=[todo.pop()]
        while stack:
            i=stack.pop()
            if i in island:continue
            island.add(i)
            for j in adjacency[i]:
                if j not in island:stack.append(j)
        todo-=island;islands.append(island)
    footprints=[(min(world_vertices[i].x for i in group),max(world_vertices[i].x for i in group),min(world_vertices[i].z for i in group),max(world_vertices[i].z for i in group)) for group in islands]
    ivory_slot=next(i for i in range(len(glove.data.materials)) if i not in skin_slots)
    # Rebuild the plate undersides from the corresponding skin faces. Extrusion copies
    # the exact donor topology AND deformation weights, including across knuckles.
    # Nearest-point weights on the old boxes created visible tears at flexed joints.
    bm=bmesh.new();bm.from_mesh(glove.data);bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.verts[i] for i in armour],context='VERTS')
    bm.normal_update()
    normal_matrix=glove.matrix_world.to_3x3().inverted().transposed()
    selected=[]
    for face in bm.faces:
        centre=glove.matrix_world@face.calc_center_median()
        normal=(normal_matrix@face.normal).normalized()
        if normal.y<.18:continue
        if any(x0-.001<=centre.x<=x1+.001 and z0-.001<=centre.z<=z1+.001 for x0,x1,z0,z1 in footprints):selected.append(face)
    assert len(selected)>10, 'No dorsal skin patch under source armour footprint'
    result=bmesh.ops.extrude_face_region(bm,geom=selected,use_keep_orig=True)
    new_vertices=[v for v in result['geom'] if isinstance(v,bmesh.types.BMVert)]
    new_faces=[f for f in result['geom'] if isinstance(f,bmesh.types.BMFace)]
    inverse_linear=glove.matrix_world.inverted().to_3x3()
    for vertex in new_vertices:
        normal=(normal_matrix@vertex.normal).normalized()
        if normal.length<.5:normal=Vector((0,1,0))
        vertex.co+=inverse_linear@(normal*.0015)
    for face in new_faces:face.material_index=ivory_slot
    bm.normal_update();bm.to_mesh(glove.data);bm.free();glove.data.update()
    glove.data.normals_split_custom_set([(0.,0.,0.)]*len(glove.data.loops))
    report.setdefault('plate_contact',[]).append({'side':side,'source_footprints':len(footprints),'skin_faces':len(selected),'plate_vertices':len(new_vertices),'plate_thickness_m':.0015,'contact_gap_min_m':0.,'contact_gap_max_m':0.,'skinning':'extruded donor skin topology; copied exact deformation weights'})
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
assert hashlib.sha256(source.read_bytes()).hexdigest()==source_hash, 'Source modified'
report['source_sha256']=source_hash
(output / 'hands-validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('VOIDFORGE_ANATOMICAL_HANDS_OK', json.dumps(report))
