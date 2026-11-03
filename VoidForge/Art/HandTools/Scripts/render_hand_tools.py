"""Native EEVEE studio and repeatable gallery/individual previews.
Only VF_HT_Studio is created. Stage objects are never part of tool exports.
Run setup_studio(), gallery(), render('hand_tools'), or individual(key).
"""
import bpy, math
from pathlib import Path
from mathutils import Vector
P=Path(__file__).with_name('build_hand_tools.py')
ns={'__file__':str(P),'__name__':'library'}
exec(compile(P.read_text(encoding='utf-8'),str(P),'exec'),ns)
OWNER=ns['OWNER']; BASE=ns['BASE']; tag=ns['tag']; enum_set=ns['enum_set']
KEYS=['MiningDrill','Grinder','Welder']

def setup_studio():
    scene=bpy.data.scenes[ns['SCENE']];bpy.context.window.scene=scene
    coll=bpy.data.collections.get('VF_HT_Studio')
    if coll:
        assert coll.get('vf_owner')==OWNER
        return
    coll=tag(bpy.data.collections.new('VF_HT_Studio'));scene.collection.children.link(coll)
    world=tag(bpy.data.worlds.new('VF_HT_StudioWorld'));world.use_nodes=True
    bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
    bg.inputs['Color'].default_value=(.09,.12,.16,1);bg.inputs['Strength'].default_value=.35;scene.world=world
    material=tag(bpy.data.materials.new('VF_HT_StudioFloor'));material.use_nodes=True
    bsdf=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    bsdf.inputs['Base Color'].default_value=(.035,.046,.058,1);bsdf.inputs['Roughness'].default_value=.65
    me=tag(bpy.data.meshes.new('VF_HT_StudioFloor'));me.from_pydata([(-20,-20,0),(20,-20,0),(20,20,0),(-20,20,0)],[],[(0,1,2,3)]);me.materials.append(material)
    ob=tag(bpy.data.objects.new('VF_HT_StudioFloor',me));coll.objects.link(ob)
    cam=tag(bpy.data.cameras.new('VF_HT_StudioCamera'));enum_set(cam,'type','ORTHO');cam.lens=55
    ob=tag(bpy.data.objects.new('VF_HT_StudioCamera',cam));coll.objects.link(ob);scene.camera=ob
    lightvalid={i.identifier for i in bpy.types.BlendDataLights.bl_rna.functions['new'].parameters['type'].enum_items}
    assert 'AREA' in lightvalid
    for name,pos,power,size,color in [('Key',(0,-1.3,1.8),130,1.4,(1,.9,.78)),('Fill',(1.5,.7,1.1),85,1.2,(.7,.84,1)),('Rim',(-1.2,.8,1.6),180,1,(1,1,1))]:
        data=tag(bpy.data.lights.new('VF_HT_'+name,'AREA'));data.energy=power;data.shape='DISK' if 'DISK' in {i.identifier for i in data.bl_rna.properties['shape'].enum_items} else data.shape;data.size=size;data.color=color
        ob=tag(bpy.data.objects.new('VF_HT_'+name,data));coll.objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((0,.1,.12))-ob.location).to_track_quat('-Z','Y').to_euler()
    # Existing dynamic engine identifier was verified live: BLENDER_EEVEE.
    if scene.render.engine!='BLENDER_EEVEE':
        try: scene.render.engine='BLENDER_EEVEE'
        except TypeError: pass
    enum_set(scene.render.image_settings,'file_format','PNG');enum_set(scene.render.image_settings,'color_mode','RGB')
    scene.render.resolution_percentage=100;scene.render.film_transparent=False
    scene.render.image_settings.color_depth='8' if '8' in {i.identifier for i in scene.render.image_settings.bl_rna.properties['color_depth'].enum_items} else scene.render.image_settings.color_depth
    print('Native studio created')

def gallery():
    scene=bpy.data.scenes[ns['SCENE']]
    right=Vector((.52,.854,0)).normalized()
    for i,key in enumerate(KEYS):
        root=bpy.data.objects['VF_'+key];root.location=right*((i-1)*.57);root.location.z=.053 if key=='Grinder' else .098
        bpy.data.collections['VF_HT_'+key].hide_render=False
    target=Vector((0,.115,.135));eye=target+Vector((1.8,-1.1,1.05))
    scene.camera.location=eye;scene.camera.rotation_euler=(target-eye).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=1.87
    scene.render.resolution_x=1800;scene.render.resolution_y=700
    bpy.context.view_layer.update()
    ns['view'](eye=eye,target=target,distance=1.65)

def render(name):
    scene=bpy.data.scenes[ns['SCENE']];dest=BASE/'Art/HandTools/Previews'/(name+'.png');dest.parent.mkdir(parents=True,exist_ok=True)
    scene.eevee.taa_render_samples=256
    scene.render.filepath=str(dest);bpy.ops.render.render(write_still=True)
    assert dest.stat().st_size>1000
    print('Preview:',dest)

def individual(key):
    scene=bpy.data.scenes[ns['SCENE']]
    for other in KEYS: bpy.data.collections['VF_HT_'+other].hide_render=other!=key
    root=bpy.data.objects['VF_'+key]
    target=root.location+Vector((0,.12,.025 if key!='Grinder' else 0))
    eye=target+Vector((.8,-.4,.38))
    scene.camera.location=eye;scene.camera.rotation_euler=(target-eye).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=.59 if key=='MiningDrill' else .49
    scene.render.resolution_x=1200;scene.render.resolution_y=1000
    render(key.lower())
    gallery()

if __name__=='__main__': setup_studio();gallery();render('hand_tools')
