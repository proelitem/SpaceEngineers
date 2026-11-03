"""Read native evaluated geometry, and set repeatable inspection viewpoints."""
import bpy, json
from mathutils import Vector
from pathlib import Path
P=Path(__file__).with_name('build_hand_tools.py')
ns={'__file__':str(P),'__name__':'library'}
exec(compile(P.read_text(encoding='utf-8'),str(P),'exec'),ns)

def inspect():
    deps=bpy.context.evaluated_depsgraph_get(); report={}
    for key in ['MiningDrill','Grinder','Welder']:
        coll=bpy.data.collections['VF_HT_'+key]; root=bpy.data.objects['VF_'+key]
        points=[]; tris=0; pieces=[]
        for ob in coll.objects:
            if ob.type!='MESH': continue
            ev=ob.evaluated_get(deps); me=ev.to_mesh(); me.calc_loop_triangles()
            transform=root.matrix_world.inverted()@ob.matrix_world
            points += [transform@v.co for v in me.vertices]
            count=len(me.loop_triangles); tris+=count; pieces.append({'object':ob.name,'triangles':count})
            ev.to_mesh_clear()
        mn=[min(v[i] for v in points) for i in range(3)]; mx=[max(v[i] for v in points) for i in range(3)]
        report[key]={'triangles':tris,'bounds_min_m':mn,'bounds_max_m':mx,'dimensions_m':[mx[i]-mn[i] for i in range(3)],'pieces':pieces}
    print(json.dumps(report,indent=2)); return report

def view_tool(key,angle='three_quarter'):
    root=bpy.data.objects['VF_'+key]
    center=root.location+Vector((0,.11,.04 if key!='Grinder' else 0))
    dirs={'three_quarter':Vector((.8,-.4,.43)),'front':Vector((0,1,.2)),'opposite':Vector((-.8,.4,.36)),'side':Vector((1,0,0))}
    ns['view'](eye=center+dirs[angle],target=center,distance=.57 if key!='MiningDrill' else .67)

if __name__=='__main__': inspect()
