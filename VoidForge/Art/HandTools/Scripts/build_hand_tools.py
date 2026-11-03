"""Original VoidForge hand tools. Run in the live Blender Python context.

Call setup(), then build_drill(), build_grinder(), build_welder(), then stage().
Owned scenes/collections are tagged; repeated stages return existing geometry.
No external assets, textures, network, or scene resets. Units: metres, +Y tool
forward, +Z up; root at primary grip centre. Export is a separate script.
"""
import bpy, bmesh, math, json
from mathutils import Vector
from pathlib import Path

OWNER = 'VoidForge.HandTools.20261103'
BASE = Path(__file__).resolve().parents[3]
SCENE = 'VF_HandTools_20261103'
MATS = {}
TOOLS = {}
CURRENT = None
GROUP = 'Body'

def enum_set(obj, prop, value):
    valid = {x.identifier for x in obj.bl_rna.properties[prop].enum_items}
    if value not in valid: raise ValueError((prop, value, valid))
    setattr(obj, prop, value)

def tag(obj):
    obj['vf_owner'] = OWNER
    return obj

def setup():
    global scene, MATS
    scene = bpy.data.scenes.get(SCENE)
    if scene and scene.get('vf_owner') != OWNER: raise RuntimeError('Foreign scene name collision')
    if not scene: scene = tag(bpy.data.scenes.new(SCENE))
    bpy.context.window.scene = scene
    enum_set(scene.unit_settings, 'system', 'METRIC')
    scene.unit_settings.scale_length = 1
    colors = {'Ceramic': ((.66,.68,.63,1),.28,.3),
              'Graphite': ((.023,.029,.033,1),.15,.48),
              'Steel': ((.36,.42,.46,1),.9,.23),
              'Safety': ((.9,.43,.035,1),.25,.31)}
    for key,(rgba,metal,rough) in colors.items():
        name='VF_HT_'+key
        mat=bpy.data.materials.get(name)
        if mat and mat.get('vf_owner') != OWNER: raise RuntimeError('Foreign material collision')
        if not mat:
            mat=tag(bpy.data.materials.new(name)); mat.use_nodes=True
        bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        for socket,value in [('Base Color',rgba),('Metallic',metal),('Roughness',rough)]:
            next(s for s in bsdf.inputs if s.identifier==socket).default_value=value
        mat.diffuse_color=rgba
        MATS[key]=mat
    print('Dedicated scene ready:',scene.name)

def begin(key):
    global CURRENT,GROUP
    name='VF_HT_'+key
    coll=bpy.data.collections.get(name)
    if coll:
        if coll.get('vf_owner')!=OWNER: raise RuntimeError('Foreign collection collision')
        root=next(o for o in coll.objects if o.get('vf_root'))
        TOOLS[key]=(coll,root); CURRENT=TOOLS[key]
        return False
    coll=tag(bpy.data.collections.new(name)); scene.collection.children.link(coll)
    root=tag(bpy.data.objects.new('VF_'+key,None)); coll.objects.link(root)
    root['vf_root']=True; root['axes']='metres; +Y forward; +Z up; primary grip origin'
    TOOLS[key]=(coll,root); CURRENT=TOOLS[key]; GROUP='Body'
    return True

def empty(name,pos):
    coll,root=CURRENT
    ob=tag(bpy.data.objects.new(root.name+'_'+name,None)); coll.objects.link(ob)
    ob.parent=root; ob.location=pos; ob.empty_display_size=.018
    ob['vf_attachment']=name
    return ob

def mesh(name,verts,faces,mat='Graphite',bevel=0,smooth=False):
    coll,root=CURRENT
    data=tag(bpy.data.meshes.new(root.name+'_'+name)); data.from_pydata(verts,[],faces); data.update()
    bm=bmesh.new(); bm.from_mesh(data); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(data); bm.free()
    ob=tag(bpy.data.objects.new(root.name+'_'+name,data)); coll.objects.link(ob); ob.parent=root
    ob['vf_group']=GROUP; ob['vf_role']=name; data.materials.append(MATS[mat])
    for p in data.polygons: p.use_smooth=smooth
    if bevel:
        mod=ob.modifiers.new('Manufactured edge radius','BEVEL'); mod.width=bevel; mod.segments=2
        enum_set(mod,'limit_method','ANGLE'); mod.angle_limit=.55
        mod.harden_normals=True
        norm=ob.modifiers.new('Weighted face normals','WEIGHTED_NORMAL'); norm.keep_sharp=True
    return ob

def box(name,pos,size,mat='Graphite',bevel=.002):
    x,y,z=pos; a,b,c=[s/2 for s in size]
    vs=[(x+i*a,y+j*b,z+k*c) for i,j,k in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]]
    return mesh(name,vs,[(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)],mat,bevel)

def profile(name,yz,width,mat='Ceramic',bevel=.0025,xc=0):
    n=len(yz); vs=[(xc+x,y,z) for x in [-width/2,width/2] for y,z in yz]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,mat,bevel)

def lathe(name,sections,center=(0,0,0),axis='Y',mat='Steel',n=24,smooth=True):
    # (axial distance,radius) profile can turn inward to form an open bore.
    def coord(t,r,a):
        v={'Y':(r*math.cos(a),t,r*math.sin(a)), 'Z':(r*math.cos(a),r*math.sin(a),t), 'X':(t,r*math.cos(a),r*math.sin(a))}[axis]
        return tuple(center[i]+v[i] for i in range(3))
    vs=[coord(t,r,2*math.pi*j/n) for t,r in sections for j in range(n)]
    fs=[tuple(range(n-1,-1,-1)),tuple((len(sections)-1)*n+j for j in range(n))]
    fs += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j) for k in range(len(sections)-1) for j in range(n)]
    ob=mesh(name,vs,fs,mat,0,smooth)
    ob.data.polygons[0].use_smooth=False; ob.data.polygons[1].use_smooth=False
    return ob

def rod(name,a,b,r,mat='Steel',n=12):
    a,b=Vector(a),Vector(b); d=b-a; q=d.to_track_quat('Z','Y')
    vs=[tuple(a+q@Vector((r*math.cos(2*math.pi*i/n),r*math.sin(2*math.pi*i/n),z))) for z in [0,d.length] for i in range(n)]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,mat,.0006)

def bolt(name,x,y,z):
    return lathe(name,[(-.001,.0032),(.001,.0032),(.0015,.0023)],(x,y,z),'X','Steel',6,False)

def pistol(prefix='Grip'):
    profile(prefix,[(-.031,-.055),(.002,-.055),(.021,.063),(-.012,.063)],.035,'Graphite',.004)
    for i in range(5):
        z=-.035+i*.015; y=-.014+(z+.035)*.16
        box(prefix+'Tread'+str(i),(0,y,z),(.036,.033,.003),'Graphite',.0007)
    profile('BatteryRail',[(-.053,-.049),(.025,-.049),(.025,-.062),(-.053,-.062)],.047,'Steel',.001)
    profile('PowerPack',[(-.069,-.063),(.027,-.063),(.035,-.08),(.021,-.096),(-.067,-.096),(-.078,-.085)],.065,'Graphite',.003)
    box('PowerPackCap',(0,-.022,-.094),(.061,.091,.008),'Ceramic',.002)
    box('BatteryRelease',(0,.029,-.073),(.025,.006,.013),'Safety',.001)
    # Trigger sits in an actually open finger space ahead of the pistol grip.
    profile('TriggerGuard',[(.023,.053),(.069,.043),(.075,-.014),(.012,-.035),(.01,-.025),(.064,-.008),(.06,.035),(.021,.043)],.014,'Graphite',.0015)
    global GROUP
    prior=GROUP; GROUP='Trigger'
    profile('Trigger',[(.019,.046),(.033,.04),(.032,.015),(.024,.009),(.023,.029)],.019,'Safety',.0015)
    GROUP=prior

def side_handle(y,z):
    lathe('AuxHandleMount',[(-.058,.023),(-.024,.023)],(0,y,z),'X','Steel',16)
    lathe('AuxHandleGrip',[(-.165,.02),(-.156,.022),(-.149,.0175),(-.069,.0175),(-.063,.023),(-.054,.023)],(0,y,z),'X','Graphite',16)
    for i in range(5):
        x=-.145+i*.014
        lathe('AuxGripTread'+str(i),[(x,.0185),(x+.003,.0185)],(0,y,z),'X','Graphite',16)
    lathe('AuxHandleEnd',[(-.166,.019),(-.163,.02)],(0,y,z),'X','Safety',16)
    empty('GripSecondary',(-.11,y,z))

def build_drill():
    global GROUP
    if not begin('MiningDrill'): return
    # Structural magnesium spine, shell halves and front seal form one fitted body.
    profile('MotorSpine',[(-.095,.064),(-.095,.125),(-.071,.143),(.09,.143),(.135,.119),(.135,.073),(.07,.054),(-.066,.054)],.068,'Graphite')
    for sign in [-1,1]:
        profile('ArmourL' if sign<0 else 'ArmourR',[(-.086,.073),(-.086,.12),(-.063,.134),(.06,.134),(.109,.113),(.105,.079),(.057,.063),(-.06,.063)],.011,'Ceramic',.002,sign*.038)
        profile('SafetyCheek'+str(sign),[(.065,.132),(.1,.119),(.098,.105),(.079,.114)],.012,'Safety',.001,sign*.038)
        for j in range(4):
            box('CoolingSlot'+str(sign)+'_'+str(j),(sign*.044,-.06+j*.012,.103),(.002,.004,.023),'Graphite',.0005)
        for y,z in [(-.069,.078),(-.065,.124),(.053,.122),(.075,.081)]: bolt('ShellFastener',sign*.045,y,z)
    box('TopServiceRail',(0,.016,.143),(.036,.116,.01),'Steel',.0015)
    box('ModeSwitch',(0,-.043,.151),(.025,.026,.01),'Safety',.0015)
    pistol(); side_handle(.103,.092)
    lathe('GearCollar',[(.115,.034),(.128,.041),(.158,.041),(.166,.035)],(0,0,.099),'Y','Graphite')
    lathe('FrontSafetyRing',[(.142,.042),(.151,.042)],(0,0,.099),'Y','Safety')
    GROUP='Rotor'
    lathe('RotatingChuck',[(.157,.031),(.168,.033),(.188,.033),(.199,.022)],(0,0,.099),'Y','Steel')
    lathe('CutterCore',[(.19,.017),(.218,.021),(.321,.013),(.36,.002)],(0,0,.099),'Y','Graphite')
    # Three swept solid flutes: visible chip channels and a tapered carbide nose.
    for blade in range(3):
        vs=[]; N=30
        for i in range(N+1):
            t=i/N; y=.203+.15*t; a=blade*2*math.pi/3+t*2*math.pi*1.05
            inner=.019*(1-.72*t); outer=.039*(1-.78*t)
            for rad,da,dy in [(inner,0,-.003),(outer,0,-.003),(outer,.11,.003),(inner,.11,.003)]:
                vs.append((rad*math.cos(a+da),y+dy,.099+rad*math.sin(a+da)))
        fs=[(3,2,1,0),(4*N,4*N+1,4*N+2,4*N+3)]
        fs += [(4*i+j,4*i+(j+1)%4,4*(i+1)+(j+1)%4,4*(i+1)+j) for i in range(N) for j in range(4)]
        mesh('HelicalCarbideFlute'+str(blade),vs,fs,'Steel')
    lathe('CarbidePilot',[(.345,.009),(.365,.002)],(0,0,.099),'Y','Steel',12,False)
    GROUP='Body'; empty('GripPrimary',(0,0,0)); empty('ToolTip',(0,.365,.099))
    print('Mining drill built')

def build_grinder():
    global GROUP
    if not begin('Grinder'): return
    lathe('RearPalmGrip',[(-.103,.016),(-.095,.018),(.012,.018),(.033,.027)],(0,0,0),'Y','Graphite',20)
    for i in range(7):
        y=-.086+i*.013
        lathe('RearGripTread'+str(i),[(y,.0186),(y+.003,.0186)],axis='Y',mat='Graphite',n=20)
    lathe('MotorChassis',[(.019,.024),(.04,.033),(.115,.033),(.149,.026)],axis='Y',mat='Graphite',n=24)
    for sign in [-1,1]:
        profile('MotorArmour'+str(sign),[(.03,-.015),(.045,-.026),(.113,-.026),(.139,-.01),(.139,.017),(.113,.028),(.047,.028),(.03,.015)],.009,'Ceramic',.002,sign*.029)
        for i in range(4): box('MotorVent'+str(sign)+'_'+str(i),(sign*.034,.062+i*.012,.004),(.002,.004,.022),'Graphite',.0005)
        bolt('MotorFastener',sign*.035,.044,.009); bolt('MotorFastener',sign*.035,.121,.009)
    box('SafetyLatch',(0,.025,.031),(.021,.033,.01),'Safety',.0015)
    profile('GearHead',[(.13,-.015),(.152,-.028),(.204,-.028),(.225,-.005),(.214,.023),(.164,.038),(.139,.02)],.055,'Steel',.004)
    profile('HeadCap',[(.148,.028),(.164,.04),(.212,.025),(.215,.015),(.195,.02),(.16,.034)],.044,'Ceramic',.001)
    side_handle(.168,.012)
    lathe('SpindleBearing',[(-.041,.022),(-.018,.024)],(0,.197,0),'Z','Graphite',24)
    # A 190-degree rear shield: working front half of the abrasive disc remains exposed.
    vs=[]; steps=28
    for z in [-.049,-.033]:
        for r in [.064,.071]:
            for i in range(steps+1):
                a=math.radians(175+190*i/steps)
                vs.append((r*math.cos(a),.197+r*math.sin(a),z))
    q=steps+1; fs=[]
    for i in range(steps):
        fs.extend([(i,i+1,q+i+1,q+i),(2*q+i,3*q+i,3*q+i+1,2*q+i+1),(i,2*q+i,2*q+i+1,i+1),(q+i,q+i+1,3*q+i+1,3*q+i)])
    fs.extend([(0,q,3*q,2*q),(steps,2*q+steps,3*q+steps,q+steps)])
    mesh('PartialDiscGuard',vs,fs,'Graphite',.0007)
    rod('GuardBracket',(-.025,.165,-.018),(-.025,.145,-.035),.008,'Steel')
    GROUP='Disc'
    lathe('AbrasiveDisc',[(-.05,.058),(-.047,.062),(-.043,.062),(-.04,.058)],(0,.197,0),'Z','Graphite',48,False)
    lathe('DiscLabel',[(-.0398,.037),(-.039,.037)],(0,.197,0),'Z','Safety',32)
    lathe('DiscLockNut',[(-.053,.012),(-.037,.012),(-.035,.009)],(0,.197,0),'Z','Steel',12,False)
    for j in range(6):
        a=j*math.pi/3
        box('LabelTick'+str(j),(.026*math.cos(a),.197+.026*math.sin(a),-.0385),(.004,.006,.001),'Graphite',0)
    GROUP='Trigger'
    profile('PaddleTrigger',[(-.07,-.02),(-.05,-.029),(.015,-.029),(.023,-.023),(.013,-.018),(-.05,-.021)],.017,'Safety',.001)
    GROUP='Body'; empty('GripPrimary',(0,-.032,0)); empty('ToolTip',(0,.259,-.045))
    # Root is the middle of the palm grip; shift geometry and attachments together.
    for o in CURRENT[0].objects:
        if o.parent==CURRENT[1]: o.location.y += .032
    print('Angle grinder built')

def build_welder():
    global GROUP
    if not begin('Welder'): return
    profile('PowerSpine',[(-.078,.065),(-.078,.118),(-.052,.138),(.084,.126),(.119,.098),(.119,.064),(.049,.054),(-.046,.054)],.057,'Graphite')
    for sign in [-1,1]:
        profile('InsulatedArmour'+str(sign),[(-.069,.075),(-.069,.114),(-.048,.127),(.073,.116),(.106,.093),(.099,.072),(.044,.063),(-.044,.063)],.009,'Ceramic',.002,sign*.032)
        profile('WarningCheek'+str(sign),[(.074,.113),(.105,.093),(.099,.077),(.089,.086)],.010,'Safety',.001,sign*.032)
        for i in range(4): box('InverterVent'+str(sign)+'_'+str(i),(sign*.037,-.047+i*.013,.094),(.002,.004,.021),'Graphite',.0005)
        for y,z in [(-.052,.077),(-.052,.116),(.06,.076)]: bolt('ShellFastener',sign*.038,y,z)
    pistol()
    box('CurrentSelector',(0,-.02,.135),(.026,.033,.012),'Graphite',.0015)
    box('CurrentSelectorLever',(0,-.015,.143),(.018,.008,.006),'Safety',.001)
    lathe('TorchCollar',[(.105,.024),(.12,.027),(.145,.027),(.151,.022)],(0,0,.082),'Y','Graphite',24)
    for y in [.122,.133,.144]: lathe('CollarInsulator',[(y,.028),(y+.003,.028)],(0,0,.082),'Y','Safety',24)
    GROUP='Nozzle'
    # Compact bent neck, insulated rear sleeve, hollow replaceable shielding cup.
    rod('TorchNeck',(0,.144,.082),(0,.197,.054),.012,'Steel',16)
    lathe('NozzleHeatSink',[(.185,.018),(.202,.019),(.22,.015)],(0,0,.054),'Y','Steel',24)
    for y in [.191,.199,.207]: lathe('CoolingFin',[(y,.022),(y+.003,.022)],(0,0,.054),'Y','Steel',24)
    lathe('ShieldingCup',[(.215,.016),(.235,.02),(.253,.018),(.253,.012),(.229,.011)],(0,0,.054),'Y','Safety',32)
    lathe('ContactTip',[(.23,.0035),(.252,.0035)],(0,0,.054),'Y','Steel',12)
    lathe('WeldingWire',[(.251,.0009),(.263,.0009)],(0,0,.054),'Y','Steel',8)
    GROUP='Body'; empty('GripPrimary',(0,0,0)); empty('ToolTip',(0,.263,.054))
    empty('GripSecondary',(0,.072,.082))
    print('Handheld welder built')

def view(eye=(1.2,-1.8,1.4),target=(0,.1,.13),distance=1.5):
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            sp=area.spaces.active
            enum_set(sp.shading,'type','MATERIAL')
            sp.overlay.show_overlays=False
            enum_set(sp.region_3d,'view_perspective','ORTHO')
            sp.region_3d.view_rotation=(Vector(eye)-Vector(target)).to_track_quat('Z','Y')
            sp.region_3d.view_location=target; sp.region_3d.view_distance=distance
            sp.region_3d.update(); area.tag_redraw()

def stage():
    # Translating roots only stages the gallery; export script zeros them temporarily.
    for key,x in [('MiningDrill',-.43),('Grinder',0),('Welder',.43)]:
        TOOLS[key][1].location=(x,0,.16)
    bpy.context.view_layer.update()
    view()

if __name__=='__main__':
    setup(); build_drill(); build_grinder(); build_welder(); stage()
