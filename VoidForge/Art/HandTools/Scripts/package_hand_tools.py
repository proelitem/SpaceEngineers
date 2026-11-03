"""Compile owned editable parts into low-draw-count assemblies and export.
The native authoring parts remain, hidden in tagged SourceParts collections.
Exports select only the root, three assembly meshes, and attachment empties.
Does not overwrite a foreign output directory or save over Blender's open file.
"""
import bpy, json, hashlib
from pathlib import Path
from mathutils import Vector
P=Path(__file__).with_name('build_hand_tools.py')
ns={'__file__':str(P),'__name__':'library'}
exec(compile(P.read_text(encoding='utf-8'),str(P),'exec'),ns)
OWNER=ns['OWNER']; BASE=ns['BASE']; tag=ns['tag']; enum_set=ns['enum_set']
KEYS=['MiningDrill','Grinder','Welder']

def compile_assemblies():
    scene=bpy.data.scenes[ns['SCENE']]; bpy.context.window.scene=scene
    deps=bpy.context.evaluated_depsgraph_get()
    pivots={'MiningDrill':{'Rotor':(0,.157,.099),'Trigger':(0,.023,.046)},
            'Grinder':{'Disc':(0,.229,-.045),'Trigger':(0,.032,-.02)},
            'Welder':{'Nozzle':(0,.144,.082),'Trigger':(0,.023,.046)}}
    for key in KEYS:
        coll=bpy.data.collections['VF_HT_'+key]; root=bpy.data.objects['VF_'+key]
        if root.get('vf_compiled'): continue
        assert coll.get('vf_owner')==OWNER and root.get('vf_owner')==OWNER
        originals=[o for o in coll.objects if o.type=='MESH']
        assert all(o.get('vf_owner')==OWNER for o in originals)
        source=tag(bpy.data.collections.new('VF_HT_'+key+'_SourceParts')); coll.children.link(source)
        for group in sorted({o['vf_group'] for o in originals}):
            parts=[o for o in originals if o['vf_group']==group]
            pivot=Vector(pivots[key].get(group,(0,0,0)))
            vertices=[]; faces=[]; normals=[]; smooth=[]; indices=[]; materials=[]
            for ob in parts:
                ev=ob.evaluated_get(deps); me=ev.to_mesh()
                matrix=root.matrix_world.inverted()@ob.matrix_world; normalmatrix=matrix.to_3x3().inverted().transposed()
                offset=len(vertices); vertices.extend([matrix@v.co-pivot for v in me.vertices])
                localmat=[]
                for evaluated_mat in me.materials:
                    # Evaluated meshes may expose evaluated material IDs. Never
                    # retain those in persistent meshes after to_mesh_clear().
                    mat=bpy.data.materials[evaluated_mat.name]
                    if mat not in materials: materials.append(mat)
                    localmat.append(materials.index(mat))
                for poly in me.polygons:
                    faces.append(tuple(offset+i for i in poly.vertices)); smooth.append(poly.use_smooth)
                    indices.append(localmat[poly.material_index])
                    normals.extend([tuple((normalmatrix@me.corner_normals[i].vector).normalized()) for i in poly.loop_indices])
                ev.to_mesh_clear()
            data=tag(bpy.data.meshes.new('VF_'+key+'_'+group+'_Mesh'))
            data.from_pydata(vertices,[],faces); data.update()
            for mat in materials: data.materials.append(mat)
            for i,poly in enumerate(data.polygons): poly.material_index=indices[i]; poly.use_smooth=smooth[i]
            data.normals_split_custom_set(normals)
            ob=tag(bpy.data.objects.new('VF_'+key+'_'+group,data));coll.objects.link(ob)
            ob.parent=root; ob.location=pivot; ob['vf_export']=True;ob['vf_group']=group
            ob['vf_source_roles']=json.dumps([o['vf_role'] for o in parts]); ob['vf_source_objects']=json.dumps([o.name for o in parts])
        for ob in originals: coll.objects.unlink(ob); source.objects.link(ob)
        source.hide_render=True; source.hide_viewport=True
        for ob in coll.objects:
            if ob.type=='EMPTY': ob['vf_export']=True
        root['vf_compiled']=True
    bpy.context.view_layer.update()
    print('Compiled three mesh assemblies per tool; editable parts retained hidden')

def metrics():
    result={}
    for key in KEYS:
        coll=bpy.data.collections['VF_HT_'+key]; root=bpy.data.objects['VF_'+key]
        points=[]; assemblies=[]; triangles=0
        for ob in coll.objects:
            if ob.type!='MESH' or not ob.get('vf_export'): continue
            me=ob.data; me.calc_loop_triangles(); triangles+=len(me.loop_triangles)
            transform=root.matrix_world.inverted()@ob.matrix_world
            points.extend([transform@v.co for v in me.vertices])
            assemblies.append({'name':ob.name,'role':ob['vf_group'],'triangles':len(me.loop_triangles),'pivot_m':list(ob.location),'material_slots':[m.name for m in me.materials],'source_roles':json.loads(ob['vf_source_roles'])})
        mn=[min(v[i] for v in points) for i in range(3)]; mx=[max(v[i] for v in points) for i in range(3)]
        assert triangles<=6000
        attachments={ob['vf_attachment']:{'name':ob.name,'position_m':list(ob.location)} for ob in coll.objects if ob.get('vf_attachment')}
        result[key]={'root':root.name,'triangles':triangles,'dimensions_m':[mx[i]-mn[i] for i in range(3)],'bounds_min_m':mn,'bounds_max_m':mx,'assemblies':assemblies,'attachments':attachments}
    return result

def export_all():
    scene=bpy.data.scenes[ns['SCENE']]; assert bpy.context.scene==scene
    artifact=BASE/'Art/HandTools'; artifact.mkdir(parents=True,exist_ok=True)
    marker=artifact/'.vf_hand_tools_owner.json'
    if marker.exists(): assert json.loads(marker.read_text())['owner']==OWNER
    else:
        destinations=[BASE/'Assets/VoidForge/Art/HandTools'/k/('VF_'+k+e) for k in KEYS for e in ['.fbx','.glb']]
        assert not any(p.exists() for p in destinations), 'Existing foreign output'
        marker.write_text(json.dumps({'owner':OWNER,'outputs':'VF_MiningDrill, VF_Grinder, VF_Welder'}),encoding='utf-8')
    exports=[]
    for key in KEYS:
        coll=bpy.data.collections['VF_HT_'+key]; root=bpy.data.objects['VF_'+key]
        pose=root.matrix_world.copy(); root.location=(0,0,0); root.rotation_euler=(0,0,0);root.scale=(1,1,1)
        bpy.context.view_layer.update()
        for ob in bpy.context.view_layer.objects: ob.select_set(False)
        selected=[o for o in coll.objects if o.get('vf_export')]
        for ob in selected: ob.select_set(True)
        bpy.context.view_layer.objects.active=root
        directory=BASE/'Assets/VoidForge/Art/HandTools'/key; directory.mkdir(parents=True,exist_ok=True)
        try:
            fbx=directory/('VF_'+key+'.fbx')
            bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'EMPTY','MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_space_transform=False,bake_anim=False,use_custom_props=True,use_triangles=True,add_leaf_bones=False)
            glb=directory/('VF_'+key+'.glb')
            bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,use_active_scene=True,export_apply=True,export_yup=True,export_extras=True,export_materials='EXPORT',export_animations=False)
            for p in [fbx,glb]:
                assert p.exists() and p.stat().st_size>1000
                exports.append({'path':str(p.relative_to(BASE)).replace('\\','/'),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
        finally:
            root.matrix_world=pose; bpy.context.view_layer.update()
    material=[]
    for name in ['Ceramic','Graphite','Steel','Safety']:
        m=bpy.data.materials['VF_HT_'+name]; n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        material.append({'name':m.name,'base_color_linear':list(n.inputs['Base Color'].default_value),'metallic':n.inputs['Metallic'].default_value,'roughness':n.inputs['Roughness'].default_value})
    report={'owner':OWNER,'agent':'GPT-Astra via Codex native Blender MCP; Jarvis Modeler3D contract reused, paid HTTP endpoint not invoked','blender':bpy.app.version_string,'production_ready':False,'source':'Art/HandTools/Source/VoidForge_HandTools.blend','axes':{'authoring':'metres; +Y forward; +Z up; primary grip root at origin in individual exports','fbx':'axis_forward=-Z, axis_up=Y, FBX_SCALE_UNITS, units centimetres metadata with metre source; root conversion rotation may be present; bake_space_transform=False','glb':'glTF standard +Y up; Blender +Y forward maps to -Z; loader performs engine convention conversion'},'models':metrics(),'materials':material,'exports':exports,'inspection':['Live scene and initial viewport inspected before mutation','Three-quarter assembly views','Opposite drill and grinder view','Front welder bore close-up','Guard/disc clearance and handle seating corrected after visual inspection'],'limitations':['Not integrated into Unity, XR or Quest; visual asset delivery only','No animations, colliders, LODs, lightmap UVs or gameplay scripts','Solid PBR factors, no bitmap texture or texture baking required; FBX material interpretation still needs Unity URP verification','Independent FBX/GLB reimport qualification pending parent agent','Original ship DRILL-R was only visually referenced; not imported, recreated or modified']}
    (artifact/'modeler3d_report.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
    print(json.dumps({'models':{k:{'triangles':v['triangles'],'dimensions_m':v['dimensions_m']} for k,v in report['models'].items()},'exports':exports},indent=2))
    return report

def save_source():
    source=BASE/'Art/HandTools/Source/VoidForge_HandTools.blend';source.parent.mkdir(parents=True,exist_ok=True)
    marker=BASE/'Art/HandTools/.vf_hand_tools_owner.json'
    assert marker.exists() and json.loads(marker.read_text())['owner']==OWNER
    # Library writer saves only this tagged scene and referenced data. The live
    # original Scene and current filepath remain untouched.
    bpy.data.libraries.write(str(source),{bpy.data.scenes[ns['SCENE']]},compress=True)
    assert source.stat().st_size>1000
    print('Dedicated source saved:',source)

if __name__=='__main__':
    compile_assemblies();export_all();save_source()
