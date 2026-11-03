"""First visual review corrections; idempotent on the 20261103 owned scene.
Guard/disc radial clearance increased to 2 mm, handle mounts seated into case,
and rear grinder grip reduced to 36 mm (37.2 mm tread diameter).
Already included in build_hand_tools.py for a fresh build.
"""
import bpy, math
OWNER='VoidForge.HandTools.20261103'
for key in ['MiningDrill','Grinder']:
    ob=bpy.data.objects['VF_'+key+'_AuxHandleMount']
    assert ob.get('vf_owner')==OWNER
    for v in ob.data.vertices:
        if abs(v.co.x+.036)<1e-5: v.co.x=-.024
    ob.data.update()
ob=bpy.data.objects['VF_Grinder_PartialDiscGuard']
assert ob.get('vf_owner')==OWNER
for v in ob.data.vertices:
    x,y=v.co.x,v.co.y-.197; r=math.hypot(x,y)
    if abs(r-.061)<1e-5 or abs(r-.068)<1e-5:
        v.co.x=x*(r+.003)/r; v.co.y=.197+y*(r+.003)/r
ob.data.update()
ob=bpy.data.objects['VF_Grinder_RearPalmGrip']
assert ob.get('vf_owner')==OWNER
for v in ob.data.vertices:
    r=math.hypot(v.co.x,v.co.z)
    new=.018 if abs(r-.022)<1e-5 else (.016 if abs(r-.018)<1e-5 and v.co.y<-.1 else r)
    if r: v.co.x*=new/r; v.co.z*=new/r
ob.data.update()
for ob in bpy.data.collections['VF_HT_Grinder'].objects:
    if ob.get('vf_role','').startswith('RearGripTread'):
        assert ob.get('vf_owner')==OWNER
        for v in ob.data.vertices:
            r=math.hypot(v.co.x,v.co.z)
            if abs(r-.0226)<1e-5: v.co.x*=.0186/r; v.co.z*=.0186/r
        ob.data.update()
bpy.context.view_layer.update()
print('Reviewed clearances and grip corrected')
