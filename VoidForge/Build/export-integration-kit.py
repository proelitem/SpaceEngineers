"""Convert delivered local GLBs to Unity FBX; never modify their source scenes."""
import argparse, json, sys, hashlib
from pathlib import Path
import bpy

p = argparse.ArgumentParser()
p.add_argument('--project-root', required=True)
p.add_argument('--source-root', required=True)
p.add_argument('--only', choices=['Cyberdeck'])
args = p.parse_args(sys.argv[sys.argv.index('--') + 1:])
project, source = Path(args.project_root).resolve(), Path(args.source_root).resolve()
out = project / 'Assets/VoidForge/Art/Kit'
out.mkdir(parents=True, exist_ok=True)
assets = {k: source / 'meshy_output/voidforge-kit-20261103-v2/exports' / (v + '.glb') for k, v in {
    'ArmorCube': 'armor_cube_precision', 'ArmorSlope': 'armor_slope_precision',
    'ArmorCorner': 'armor_corner_precision', 'Gyroscope': 'gyroscope_clean',
    'Cyberdeck': 'cyberdeck_clean_blank'}.items()}
assets.update({k.title(): source / f'meshy_output/voidforge-kit-20261103/fitted/{k}/{k}.glb'
               for k in ('cockpit', 'thruster', 'battery')})
manifest = out / 'kit-materials.json'
report = json.loads(manifest.read_text(encoding='utf-8')) if args.only else {'assets': []}
for key, path in assets.items():
    if args.only and key != args.only:
        continue
    if not path.is_file():
        raise FileNotFoundError(path)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(path))
    objects = list(bpy.context.scene.objects)
    # First-person wrist display mounts directly against the sleeve, without fastening
    # straps or the two 21 mm raised saddle feet. Preserve the source GLB intact.
    # The artist's GLB/Blender source remains untouched.
    omitted = [o.name for o in objects if key == 'Cyberdeck' and ('Strap' in o.name or 'Saddle' in o.name)]
    for ob in list(objects):
        if ob.name in omitted:
            bpy.data.objects.remove(ob, do_unlink=True)
            objects.remove(ob)
    if key == 'Cyberdeck' and (sum('Strap' in name for name in omitted) != 2 or sum('Saddle' in name for name in omitted) != 2):
        raise RuntimeError(f'Expected two straps and two raised saddle feet, found {omitted}')
    # Combine by material at export to keep mobile renderer counts bounded.
    # Keep the screen and its anchor separate for the future wrist interface.
    meshes = [o for o in objects if o.type == 'MESH' and 'ScreenBlank' not in o.name]
    if meshes:
        bpy.ops.object.select_all(action='DESELECT')
        for o in meshes:
            o.select_set(True)
        bpy.context.view_layer.objects.active = meshes[0]
        bpy.ops.object.join()
        meshes[0].name = key + '_Geometry'
    mats = []
    for mat in set(m for o in bpy.context.scene.objects if o.type == 'MESH' for m in o.data.materials if m):
        mat.name = key + '_' + mat.name
        color, metallic, roughness = list(mat.diffuse_color), 0., .5
        texture = None
        if mat.use_nodes:
            principled = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
            if principled:
                color = list(principled.inputs['Base Color'].default_value)
                metallic = principled.inputs['Metallic'].default_value
                roughness = principled.inputs['Roughness'].default_value
                links = principled.inputs['Base Color'].links
                image = links[0].from_node.image if links and links[0].from_node.type == 'TEX_IMAGE' else None
                if image:
                    texture = key + '_' + hashlib.sha256(image.name.encode()).hexdigest()[:8] + '.png'
                    image.filepath_raw = str(out / texture)
                    image.file_format = 'PNG'
                    image.save()
        mats.append({'name': mat.name, 'color': color, 'metallic': metallic,
                     'roughness': roughness, 'texture': texture or '', 'unlit': 'ScreenBlank' in mat.name})
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(out / (key + '.fbx')), use_selection=True,
        object_types={'MESH', 'EMPTY'}, apply_unit_scale=True, bake_space_transform=False,
        axis_forward='-Z', axis_up='Y', use_custom_props=False, bake_anim=False, path_mode='STRIP')
    entry = {'id': key, 'source': str(path.relative_to(source)).replace('\\', '/'),
        'sha256': hashlib.sha256(path.read_bytes()).hexdigest(), 'materials': sorted(mats, key=lambda m: m['name'])}
    if omitted:
        entry['omittedParts'] = omitted
    index = next((i for i, e in enumerate(report['assets']) if e['id'] == key), None)
    if index is None: report['assets'].append(entry)
    else: report['assets'][index] = entry
(out / 'kit-materials.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('VF_INTEGRATION_EXPORT_OK', len(report['assets']))
