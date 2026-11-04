"""Render the derived glove itself, open and flexed, with its actual armature deformation."""
from pathlib import Path
import bpy
from mathutils import Vector,Quaternion
project=Path(__file__).resolve().parents[1];out=project/'Builds/HandContact04';out.mkdir(parents=True,exist_ok=True)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
glove=bpy.data.objects['VF_Glove_Right_FiveDigits']
for obj in bpy.context.scene.objects:
 if obj.type=='MESH':obj.hide_render=obj!=glove
for material in glove.data.materials:
 material.use_nodes=True
 node=material.node_tree.nodes.get('Principled BSDF')
 if node:node.inputs['Base Color'].default_value=(.72,.74,.7,1) if 'Ivory' in material.name else (.065,.085,.10,1)
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
scene.world.color=(.08,.08,.08)
centre=arm.matrix_world@arm.data.bones['RightHand'].head_local+Vector((-.015,0,-.05))
cam_data=bpy.data.cameras.new('Hand QA');cam=bpy.data.objects.new('Hand QA',cam_data);scene.collection.objects.link(cam)
cam.location=centre+Vector((-.04,.35,.16));cam.rotation_euler=(centre-cam.location).to_track_quat('-Z','Y').to_euler();cam_data.type='ORTHO';cam_data.ortho_scale=.25;scene.camera=cam
for location,power in [(centre+Vector((.1,.4,.3)),45),(centre+Vector((-.2,-.1,.1)),25)]:
 data=bpy.data.lights.new('Hand QA light','AREA');data.energy=power;data.shape='DISK';data.size=.3
 obj=bpy.data.objects.new('Hand QA light',data);scene.collection.objects.link(obj);obj.location=location;obj.rotation_euler=(centre-obj.location).to_track_quat('-Z','Y').to_euler()
for name,curl in [('open',0),('closed',.65)]:
 for finger in ('IndexFinger','MiddleFinger','RingFinger','PinkyFinger'):
  for joint in range(1,4):
   bone=arm.pose.bones['R'+finger+str(joint)]
   axis=bone.bone.matrix_local.to_3x3().inverted()@(arm.matrix_world.to_3x3().inverted()@Vector((-1,0,0)))
   bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion(axis.normalized(),curl*1.5)
 bpy.context.view_layer.update();scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
print('HAND_CONTACT_RENDERED',str(out))
