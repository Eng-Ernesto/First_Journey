import bpy, json
from pathlib import Path
from mathutils import Vector
result=[]
for s in bpy.data.scenes:
    objects=[]
    for o in s.objects:
        if o.type=='MESH':
            p=[o.matrix_world @ Vector(v) for v in o.bound_box]
            objects.append({'name':o.name,'faces':len(o.data.polygons),'bounds_min':[min(v[i] for v in p) for i in range(3)],'bounds_max':[max(v[i] for v in p) for i in range(3)]})
    result.append({'scene':s.name,'objects':objects})
Path(r'C:\Ernesto\unity\First_Journey\Docs\Airplane_Source_Inventory.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print('OTHER_SOURCE='+json.dumps([{'scene':s['scene'],'names':[o['name'] for o in s['objects']]} for s in result]))
