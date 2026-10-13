import bpy, json, re
from pathlib import Path
out=Path(r'C:\Ernesto\unity\assets\Vehicles\Airplane\Export_v01')
out.mkdir(parents=True,exist_ok=True)
s=next(s for s in bpy.data.scenes if any(o.name.startswith('aviao_aviador_') for o in s.objects))
bpy.context.window.scene=s
parts=[o for o in s.objects if o.type=='MESH' and o.name.startswith('aviao_aviador_')]
assert len(parts)>30 and any(o.name=='aviao_aviador_Fuselagem' for o in parts), 'Incomplete airplane source'
bpy.ops.object.select_all(action='DESELECT')
for o in parts:
    o.hide_set(False)
    o.select_set(True)
mats={m.name:m for o in parts for m in o.data.materials if m}
manifest={'source':'autronauta.mixar','source_scene':s.name,'excluded':['astronauta_lowpoly_*','Cube','cameras','lights'],'objects':[],'materials':[]}
for o in parts: manifest['objects'].append({'name':o.name,'materials':[m.name if m else '' for m in o.data.materials]})
for name,m in mats.items():
    p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.node_tree else None
    c=list(p.inputs['Base Color'].default_value) if p else list(m.diffuse_color)
    record={'name':name,'base_color_linear':c,'roughness':float(p.inputs['Roughness'].default_value) if p else .8,'metallic':float(p.inputs['Metallic'].default_value) if p else 0,'alpha':float(p.inputs['Alpha'].default_value) if p else c[3], 'transmission':float(p.inputs['Transmission Weight'].default_value) if p else 0,'base_texture':None,'procedural_color':False}
    if p and p.inputs['Base Color'].is_linked:
        node=p.inputs['Base Color'].links[0].from_node
        if node.type=='TEX_IMAGE' and node.image:
            (out/'textures').mkdir(exist_ok=True)
            fn=re.sub(r'[^a-zA-Z0-9_-]','_',name)+'_BaseColor.png'
            node.image.save_render(str(out/'textures'/fn))
            record['base_texture']='textures/'+fn
        else: record['procedural_color']=True
    manifest['materials'].append(record)
assert not any(m['procedural_color'] for m in manifest['materials']), 'Procedural material needs baking'
(out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
bpy.ops.export_scene.fbx(filepath=str(out/'FirstJourney_Airplane_v01.fbx'),use_selection=True,object_types={'MESH'},use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',use_space_transform=True,bake_space_transform=True,path_mode='COPY',embed_textures=False,mesh_smooth_type='FACE')
print('PLANE_EXPORT='+json.dumps({'objects':len(parts),'materials':manifest['materials'],'bytes':(out/'FirstJourney_Airplane_v01.fbx').stat().st_size}))
