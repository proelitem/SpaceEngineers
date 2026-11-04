"""Original VoidForge meshes, authored through live Blender MCP from approved 2D references.
Run in Blender Text Editor; creates dedicated scenes and never clears existing scenes.
Blender coordinates: X right, Y front, Z up. FBX: -Z forward / Y up, baked.
"""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'Assets/VoidForge/Art/Modules/2026-11-04'
ART = ROOT / 'Art/Modules/2026-11-04'
OUT.mkdir(parents=True, exist_ok=True)
(ART / 'Previews').mkdir(parents=True, exist_ok=True)
PALETTE = {
    'Ivory': ((.67,.62,.51,1), .28, .36, 0),
    'Graphite': ((.035,.046,.059,1), .62, .32, 0),
    'Steel': ((.23,.28,.32,1), .8, .26, 0),
    'Orange': ((1,.18,.003,1), .38, .28, 0),
    'Amber': ((1,.20,.003,1), .15, .28, 1.5),
    'Cyan': ((.008,.72,.8,1), .1, .25, 2),
}
MATS = {}
for key,(color,metal,rough,emit) in PALETTE.items():
    mat = bpy.data.materials.get('VF04_'+key) or bpy.data.materials.new('VF04_'+key)
    mat.use_nodes = True
    mat.diffuse_color = color
    node = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    node.inputs['Base Color'].default_value = color
    node.inputs['Metallic'].default_value = metal
    node.inputs['Roughness'].default_value = rough
    node.inputs['Emission Color'].default_value = color
    node.inputs['Emission Strength'].default_value = emit
    MATS[key] = mat

SCENES = {}
def start(name):
    global SCENE, PARTS, PREFIX
    PREFIX = 'VF_'+name
    SCENE = bpy.data.scenes.new(PREFIX+'_04')
    bpy.context.window.scene = SCENE
    SCENE.unit_settings.system = 'METRIC'
    SCENE.unit_settings.scale_length = 1
    PARTS = []
    SCENES[name] = SCENE

def finish(o,name,mat,bevel=0):
    o.name=PREFIX+'_'+name
    o.data.materials.append(MATS[mat])
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Machined edges','BEVEL'); mod.width=bevel; mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    PARTS.append(o)
    return o

def box(name,p,s,mat='Ivory',b=.012):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p)
    o=bpy.context.object; o.dimensions=s
    return finish(o,name,mat,min(b,min(s)*.35))

def plate(name,p,s,mat='Ivory',cut=.04):
    # Chamfer the silhouette independently of panel thickness, unlike a cube bevel.
    w,d,h=s; x=w/2;z=h/2;c=min(cut,x*.7,z*.7)
    outline=[(-x+c,-z),(x-c,-z),(x,-z+c),(x,z-c),(x-c,z),(-x+c,z),(-x,z-c),(-x,-z+c)]
    verts=[(a,y,b) for y in [-d/2,d/2] for a,b in outline]
    faces=[tuple(reversed(range(8))),tuple(range(8,16))]+[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    o=bpy.data.objects.new(PREFIX+'_'+name,me);SCENE.collection.objects.link(o);o.location=p
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    return finish(o,name,mat,.003)

def cyl(name,p,r,d,mat='Steel',axis=(0,0,1),n=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=d,location=p)
    o=bpy.context.object
    o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    return finish(o,name,mat,min(.006,d*.16,r*.12))

def beam(name,a,b,width,depth,mat='Ivory'):
    va,vb=Vector(a),Vector(b)
    o=box(name,(va+vb)*.5,(width,depth,(vb-va).length),mat,.014)
    o.rotation_euler=(vb-va).to_track_quat('Z','Y').to_euler()
    return o

def tube(name,points,r,mat='Steel',sides=12):
    # Parallel-frame polygon tube: explicit mesh, no curves/modifiers in export.
    pts=[Vector(p) for p in points]; verts=[]; faces=[]
    for i,p in enumerate(pts):
        tangent=pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]
        q=tangent.to_track_quat('Z','Y')
        for j in range(sides):
            v=q@Vector((r*math.cos(j*math.tau/sides),r*math.sin(j*math.tau/sides),0))
            verts.append(tuple(p+v))
    for i in range(len(pts)-1):
        for j in range(sides):
            k=i*sides+j; k1=i*sides+(j+1)%sides
            faces.append((k,k1,k1+sides,k+sides))
    faces.extend([tuple(reversed(range(sides))),tuple(range((len(pts)-1)*sides,len(pts)*sides))])
    me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update()
    o=bpy.data.objects.new(PREFIX+'_'+name,me); SCENE.collection.objects.link(o)
    o.data.materials.append(MATS[mat]); PARTS.append(o)
    return o

def bolt(x,y,z,axis=(0,1,0),r=.016):
    cyl('BoltWasher',(x,y,z),r,.009,'Steel',axis,12)
    p=Vector((x,y,z))+Vector(axis)*.006
    cyl('BoltSocket',p,r*.48,.005,'Graphite',axis,6)

def lamp(name,p,s,color='Amber'):
    box(name+'Socket',p,(s[0]+.025,s[1],s[2]+.025),'Graphite',.014)
    box(name,(p[0],p[1]+.006,p[2]),(s[0],s[1],s[2]),color,.009)

def shell(w,h):
    cx=(w-1)/2; cz=(h-1)/2
    box('RearChassis',(cx,-.405,cz),(w-.055,.17,h-.055),'Graphite',.055)
    for x in [-.43,w-.57]:
        box('SideArmour',(x,-.025,cz),(.14,.75,h-.05),'Ivory',.035)
        box('SideSeam',(x,-.03,cz),(.148,.62,h*.38),'Graphite',.028)
        side=plate('SideServicePanel',(x+(.012 if x>cx else -.012),-.02,cz),(.52,.14,h*.31),'Ivory',.06)
        side.rotation_euler.z=math.pi/2
        for z in [-.39,h-.61]:
            box('CornerGuard',(x,.37,z),(.14,.26,.22),'Graphite',.045)
            bolt(x,.499,z)
        for j in range(3):
            zh=(h-.38)/3
            plate('FrontUpright',(x,.35,-.31+zh*(j+.5)),(.145,.25,zh-.008),'Ivory',.034)
        lamp('RailLight',(x,.484,cz),(.028,.018,min(.28,h*.22)))
    for z in [-.43,h-.57]:
        box('CrossMember',(cx,.0,z),(w-.10,.95,.14),'Graphite',.035)
        box('FrontRail',(cx,.37,z),(w-.23,.24,.13),'Ivory',.028)
        for x in [-.29,w-.71]: bolt(x,.494,z)
    # Four diagonal front chamfers visually frame the open working bay.
    for x,sgn in [(-.35,1),(w-.65,-1)]:
        beam('UpperChamfer',(x,.35,h-.61),(x+sgn*.10,.35,h-.72),.12,.21)
        beam('LowerChamfer',(x,.35,-.39),(x+sgn*.10,.35,-.28),.12,.21)
    box('Roof',(cx,-.02,h-.49),(w-.28,.79,.04),'Ivory',.012)
    for x in [-.27,w-.73]:
        roof=plate('RoofHatch',(x,-.03,h-.451),(.30,.025,.48),'Steel',.04)
        roof.rotation_euler.x=math.pi/2
        for dx in [-.10,.10]:
            for y in [-.20,.14]: bolt(x+dx,y,h-.431,(0,0,1),.013)
        box('RoofIndicator',(x,.25,h-.451),(.18,.035,.025),'Amber',.008)
    for x in [-.505,w-.495]:
        for z in [h-.76,h-.83,h-.90]:
            box('SideVent',(x,-.18,z),(.009,.25,.045),'Graphite',.011)
        box('SideWarning',(x,.16,cz+.26),(.012,.26,.05),'Orange',.008)
    for x in [-.40,w-.60]:
        box('RoofCornerAccent',(x,.25,h-.48),(.13,.12,.06),'Orange',.016)

def cargo():
    start('Cargo')
    box('PressureBody',(0,0,0),(.93,.94,.93),'Graphite',.065)
    plate('DoorSeal',(0,.46,0),(.83,.055,.83),'Steel',.09)
    plate('DoorSurround',(0,.474,0),(.78,.035,.78),'Ivory',.08)
    for z in [-.195,.195]:
        plate('DoorPanelSeam',(0,.495,z),(.68,.01,.36),'Graphite',.06)
        plate('DoorPanel',(0,.505,z),(.655,.012,.335),'Ivory',.06)
    for x in [-.435,.435]:
        box('EdgeRail',(x,0,0),(.12,.90,.92),'Ivory',.026)
        box('SideStripe',(x,.43,0),(.055,.04,.21),'Orange',.015)
    for x in [-.40,.40]:
        for y in [-.39,.39]:
            for z in [-.40,.40]:
                box('ArmouredCorner',(x,y,z),(.20,.22,.20),'Graphite',.052)
                if y>0: bolt(x,.497,z,r=.021)
    for side in [-1,1]:
        panel=plate('SideGasket',(side*.468,0,0),(.72,.025,.70),'Steel',.08);panel.rotation_euler.z=math.pi/2
        panel=plate('SideInset',(side*.485,0,0),(.64,.025,.62),'Ivory',.07);panel.rotation_euler.z=math.pi/2
        for y in [-.27,.27]:
            for z in [-.27,.27]: bolt(side*.497,y,z,(side,0,0),.015)
    box('RoofGasket',(0,0,.466),(.73,.75,.036),'Steel',.05)
    box('RoofPlate',(0,0,.485),(.67,.68,.03),'Ivory',.045)
    for x in [-.25,.25]:
        box('RoofRecess',(x,0,.501),(.045,.39,.007),'Graphite',.01)
        for y in [-.26,.26]: bolt(x,y,.497,(0,0,1),.016)
    for z in [-.41,.41]: box('OrangeCatch',(0,.488,z),(.20,.018,.045),'Orange',.012)
    plate('LockWell',(0,.519,-.03),(.25,.012,.32),'Graphite',.055)
    plate('LockShoulder',(0,.533,.065),(.20,.025,.09),'Steel',.022)
    box('Latch',(0,.546,-.045),(.115,.035,.19),'Orange',.018)
    for z in [-.105,-.077,-.049]: box('LatchGrip',(0,.567,z),(.075,.008,.009),'Graphite',.002)
    lamp('Status',(0,.523,.285),(.24,.018,.018),'Cyan')
    for x in [-.275,.275]:
        bolt(x,.523,-.29)
    # Normalize to strict 1m envelope after details are complete.
    normalize(1,1)

def refinery():
    start('Refinery'); shell(1,2)
    box('InteriorBack',(0,-.28,.49),(.72,.06,1.64),'Graphite',.03)
    for x in [-.19,.19]:
        cyl('ReactionVessel',(x,.17,.56),.145,.75,'Steel',n=32)
        for z in [.16,.27,.85,.96]:
            cyl('VesselCollar',(x,.17,z),.158,.065,'Graphite',n=24)
            cyl('CollarLip',(x,.17,z+.024),.16,.022,'Steel',n=24)
        cyl('UpperShoulder',(x,.17,1.015),.12,.09,'Steel')
        cyl('FeedNeck',(x,.17,1.13),.059,.15,'Steel')
        cyl('FlowBand',(x,.17,1.14),.063,.035,'Amber')
        tube('IntakeElbow',[(x,.17,1.19),(x,.17,1.25),(x,.13,1.29),(x,.02,1.30),(x,-.15,1.30)],.052)
        lamp('ReactionWindow',(x,.309,.56),(.066,.016,.31))
        for dx in [-.102,.102]:
            box('VesselClamp',(x+dx,.265,.29),(.04,.08,.18),'Steel',.009)
        box('DrainValve',(x,.34,.15),(.085,.065,.10),'Orange',.013)
        box('ValveSlot',(x,.376,.15),(.038,.008,.022),'Graphite',.004)
        for z in [.31,.79]: box('VesselMark',(x,.319,z),(.055,.018,.026),'Orange',.005)
        path=[(x,.17,.12),(x,.17,-.02),(x*.85,.20,-.09),(x*.5,.22,-.12),(0,.22,-.12)]
        tube('DrainManifold',path,.047)
        tube('HeatedDrain',[(a,b+.044,c) for a,b,c in path],.013,'Amber',8)
    box('Pump',(0,.23,-.12),(.19,.20,.19),'Graphite',.028)
    lamp('PumpStatus',(0,.34,-.075),(.09,.016,.025))
    lamp('UpperStatus',(0,.391,1.255),(.26,.02,.024))
    for x in [-.43,.43]:
        box('LowerStripe',(x,.482,-.12),(.046,.025,.26),'Orange',.009)
        box('UpperStripe',(x,.48,1.21),(.14,.025,.065),'Orange',.01)
    normalize(1,2)

def assembler():
    start('Assembler'); shell(2,1)
    hatch=plate('CentralRoofGasket',(.5,-.01,.542),(.62,.016,.60),'Graphite',.045);hatch.rotation_euler.x=math.pi/2
    hatch=plate('CentralRoofPlate',(.5,-.01,.554),(.59,.012,.57),'Ivory',.045);hatch.rotation_euler.x=math.pi/2
    box('BayBack',(.5,-.27,0),(1.67,.07,.70),'Graphite',.025)
    box('Workbed',(.72,.10,-.29),(1.0,.62,.10),'Steel',.022)
    for x in [.45,.73,1.01]:
        box('BedPlate',(x,.1,-.23),(.27,.54,.022),'Steel',.008)
    for x in [.28,1.15]:
        for y in [-.13,.34]: box('BedLocator',(x,y,-.21),(.10,.025,.015),'Amber',.004)
    arm_start=len(PARTS)
    cyl('TurretBase',(-.14,.06,-.27),.18,.16,'Graphite',n=32)
    cyl('TurretRing',(-.14,.06,-.19),.184,.025,'Amber',n=32)
    cyl('TurretUpper',(-.14,.06,-.155),.15,.06,'Steel')
    a=(-.14,.10,-.13); b=(.19,.10,.245); c=(.48,.10,.06)
    beam('RobotUpper',a,b,.15,.15,'Steel')
    beam('RobotArmour',(-.14,.19,-.13),(.19,.19,.245),.12,.045)
    beam('RobotForearm',b,c,.14,.14,'Ivory')
    beam('ArmInset',(.02,.22,.06),(.12,.22,.17),.03,.016,'Orange')
    beam('ForearmIndicator',(.28,.183,.20),(.40,.183,.12),.022,.016,'Amber')
    for p in [a,b,c]:
        cyl('Servo',p,.091,.18,'Steel',(0,1,0))
        cyl('ServoRing',(p[0],.203,p[2]),.064,.032,'Graphite',(0,1,0))
        cyl('ServoHub',(p[0],.224,p[2]),.036,.018,'Orange',(0,1,0))
        bolt(p[0],.239,p[2],r=.020)
    box('Wrist',(.50,.10,.008),(.13,.14,.09),'Steel',.015)
    for dx in [-.042,.042]:
        beam('GripperFinger',(.5+dx,.10,-.02),(.5+dx*.6,.10,-.11),.022,.05,'Orange')
        box('GripperTip',(.5+dx*.6,.10,-.105),(.018,.045,.035),'Steel',.004)
    tube('ArmServiceHose',[(-.26,.04,-.1),(-.27,.04,.04),(-.16,.04,.21),(.03,.04,.32),(.19,.04,.29)],.022,'Graphite')
    for o in PARTS[arm_start:]:
        o.location.z-=.06
        o.location.y+=.08
    for x in [.80,1.19]:
        box('RearAccess',(x,-.217,.08),(.30,.03,.38),'Steel',.016)
        lamp('RearWorklight',(x,-.19,.25),(.20,.025,.025))
        for i in range(5):
            for j in range(4):
                cyl('VentPerforation',(x+(i-2)*.044,-.195,(j-1.5)*.045+.04),.013,.006,'Graphite',(0,1,0),8)
    lamp('FrontStatus',(.5,.494,.423),(.32,.016,.022),'Cyan')
    box('LowerIntake',(.5,.495,-.426),(.75,.015,.063),'Graphite',.014)
    for i in range(18): box('IntakeFin',(.17+i*.038,.505,-.426),(.012,.013,.045),'Steel',.003)
    # The reference places the robot on the viewer's left when facing +Y.
    bpy.context.view_layer.update()
    for o in PARTS:
        world=o.matrix_world.copy()
        for v in o.data.vertices:
            p=world@v.co;v.co=(1-p.x,p.y,p.z)
        o.matrix_world.identity()
        bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    normalize(2,1)

def normalize(w,h):
    # Uniform per-axis normalization guarantees exact grid bounds including raised fittings.
    bpy.context.view_layer.update()
    verts=[o.matrix_world@v.co for o in PARTS for v in o.data.vertices]
    lo=Vector(tuple(min(v[i] for v in verts) for i in range(3)))
    hi=Vector(tuple(max(v[i] for v in verts) for i in range(3)))
    target=Vector((w,1,h)); origin=Vector((-.5,-.5,-.5))
    for o in PARTS:
        mat=o.matrix_world.copy()
        for v in o.data.vertices:
            p=mat@v.co
            v.co=tuple(origin[i]+(p[i]-lo[i])*target[i]/(hi[i]-lo[i]) for i in range(3))
        o.matrix_world.identity()

def export(name):
    scene=SCENES[name]; bpy.context.window.scene=scene
    # Combine each opaque material into one draw mesh while retaining the part descriptions in script.
    for key,mat in MATS.items():
        group=[o for o in scene.objects if o.type=='MESH' and o.data.materials[0]==mat]
        if not group: continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in group:o.select_set(True)
        bpy.context.view_layer.objects.active=group[0]
        bpy.ops.object.join(); group[0].name='VF_'+name+'_'+key
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(OUT/('VF_'+name+'.fbx')),use_selection=True,
        object_types={'MESH'},axis_forward='-Z',axis_up='Y',global_scale=1,
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,
        use_triangles=True,bake_anim=False,add_leaf_bones=False)
    meshes=[o for o in scene.objects if o.type=='MESH']
    for o in meshes:o.data.calc_loop_triangles()
    return {'name':'VF_'+name,'triangles':sum(len(o.data.loop_triangles) for o in meshes),
        'meshes':len(meshes),'materials':[m.name for m in MATS.values() if any(m in list(o.data.materials) for o in meshes)],
        'unity_bounds':{'min':[-.5,-.5,-.5],'max':[1.5 if name=='Assembler' else .5,1.5 if name=='Refinery' else .5,.5]},
        'fbx':'Assets/VoidForge/Art/Modules/2026-11-04/VF_'+name+'.fbx'}

def setup_preview(name):
    scene=SCENES[name]; bpy.context.window.scene=scene
    try: scene.render.engine='CYCLES'
    except TypeError: pass
    scene.cycles.samples=24
    scene.render.resolution_x=1024; scene.render.resolution_y=1024; scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=True
    scene.world=bpy.data.worlds.new('VF04_Studio_'+name); scene.world.use_nodes=True
    bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND')
    bg.inputs[0].default_value=(.22,.26,.32,1); bg.inputs[1].default_value=.40
    center=Vector((.5 if name=='Assembler' else 0,0,.5 if name=='Refinery' else 0))
    camdata=bpy.data.cameras.new('VF04_PreviewCamera'); camdata.type='ORTHO'; camdata.ortho_scale=2.60 if name!='Cargo' else 1.80
    cam=bpy.data.objects.new('VF04_PreviewCamera',camdata); scene.collection.objects.link(cam)
    cam.location=center+Vector((3.4,6,2.1 if name=='Assembler' else 3.1)); cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam
    for idx,(position,power,size,color) in enumerate([((-3,4,5),700,4,(1,.84,.65)),((4,2,3),550,3,(.68,.83,1)),((1,-3,3),850,3,(.18,.73,1))]):
        ld=bpy.data.lights.new('VF04_StudioLight','AREA');ld.energy=power;ld.shape='DISK';ld.size=size;ld.color=color
        ob=bpy.data.objects.new('VF04_StudioLight',ld);scene.collection.objects.link(ob);ob.location=center+Vector(position);ob.rotation_euler=(center-ob.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(ART/'Previews'/('VF_'+name+'.png'))
    return scene

def validate():
    result=[]
    for name,scene in SCENES.items():
        meshes=[o for o in scene.objects if o.type=='MESH']
        verts=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
        lo=[min(v[i] for v in verts) for i in range(3)]
        hi=[max(v[i] for v in verts) for i in range(3)]
        for o in meshes:o.data.calc_loop_triangles()
        tris=sum(len(o.data.loop_triangles) for o in meshes)
        expected=[1.5 if name=='Assembler' else .5,.5,1.5 if name=='Refinery' else .5]
        assert max(abs(a+.5) for a in lo)<1e-5
        assert max(abs(a-b) for a,b in zip(hi,expected))<1e-5
        assert tris<=15000 and len(meshes)<=6
        assert all(all(math.isfinite(c) for c in v) for v in verts)
        assert all(len(o.data.materials)==1 for o in meshes)
        result.append({'name':name,'blenderBounds':[lo,hi],'triangles':tris,
            'singleMaterialPerMesh':True,'finiteVertices':True})
    (ART/'validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    return result

def build():
    cargo();refinery();assembler()
    records=[export(n) for n in SCENES]
    manifest={'authoring':'GPT-Astra Modeler3D via live Blender MCP; original geometry from approved local 2D reference images','blender_version':bpy.app.version_string,
        'license':'MIT (original geometry and script); no Asset Store geometry copied',
        'reference_directory':'Art/UI/Cyberdeck/2026-11-03-v1/blocs',
        'library_review':'_jarvis_index.json; Sci-Fi_base manifest/container and SciFi_Space_Base container inventories inspected. Existing Asset Store props are not the exact approved silhouettes and are not redistributable as MIT; authored new reference-matching geometry at user request.',
        'coordinates':'Blender X right, +Y front, +Z up. FBX -Z forward / +Y up, bake_space_transform=True. Unity intended front -Z, root origin base-cell centre, import Scale Factor=1, Convert Units=true. Verify importer orientation against bounds before prefab.',
        'materials':{('VF04_'+k):{'baseColorLinear':v[0],'metallic':v[1],'roughness':v[2],'emissionStrength':v[3],'emissionColorLinear':v[0] if v[3] else [0,0,0,1]} for k,v in PALETTE.items()},'models':records}
    (ART/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    validate()
    for n in SCENES:setup_preview(n)
    bpy.data.libraries.write(str(ART/'VF_Modules_04.blend'),set(SCENES.values()),fake_user=True)
    print(json.dumps(records))

if __name__=='__main__': build()
