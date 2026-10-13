using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AirfieldIsland
{
    const string Root = "Assets/_FirstJourney";
    const string Art = Root + "/Art/Airplane";
    const string IslandArt = Root + "/Art/AirfieldIsland";
    const string Source = @"C:\Ernesto\unity\assets\Vehicles\Airplane\Export_v01";
    const string ScenePath = Root + "/Scenes/FirstJourney_LuaBase.unity";
    static readonly Vector3 IslandPosition = new Vector3(24, 0, 4.5f);

    static Material BaseMaterial(string name)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/BaseMoon/Materials/"+name+".mat");
        if (!m) throw new InvalidOperationException("Missing original material: "+name);
        return m;
    }

    static Bounds BoundsOf(GameObject root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>();
        var b=renderers[0].bounds;
        foreach(var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    static Mesh PatchMesh(string name, Vector2[] outline)
    {
        var vertices=new List<Vector3>{Vector3.zero};
        Vector2 center=Vector2.zero;
        foreach(var p in outline) center+=p;
        center/=outline.Length;
        vertices[0]=new Vector3(center.x,0,center.y);
        vertices.AddRange(outline.Select(p=>new Vector3(p.x,0,p.y)));
        var triangles=new List<int>();
        for(int i=0;i<outline.Length;i++) { triangles.Add(0); triangles.Add(1+(i+1)%outline.Length); triangles.Add(1+i); }
        var mesh=new Mesh{name=name};
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0);
        mesh.SetUVs(0,vertices.Select(p=>new Vector2(p.x,p.z)).ToList());
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,IslandArt+"/Meshes/"+name+".asset");
        return mesh;
    }

    static GameObject Patch(Transform parent,string name,Vector3 position,float radiusX,float radiusZ,Material mat,int seed)
    {
        var points=new Vector2[40];
        for(int i=0;i<points.Length;i++)
        {
            float a=i*2*Mathf.PI/points.Length;
            float r=1+.025f*Mathf.Sin(3*a+seed)+.018f*Mathf.Sin(7*a+.3f*seed);
            points[i]=new Vector2(Mathf.Cos(a)*radiusX*r,Mathf.Sin(a)*radiusZ*r);
        }
        return Surface(parent,name,position,PatchMesh(name,points),mat);
    }

    static GameObject Surface(Transform parent,string name,Vector3 position,Mesh mesh,Material material)
    {
        var obj=new GameObject(name); obj.transform.SetParent(parent,false); obj.transform.localPosition=position;
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        return obj;
    }

    static GameObject CopyPart(Transform source,Transform parent)
    {
        var obj=new GameObject(source.name); obj.transform.SetParent(parent,false);
        obj.transform.localPosition=source.position; obj.transform.localRotation=source.rotation; obj.transform.localScale=source.lossyScale;
        obj.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
        obj.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;
        if(source.GetComponent<MeshCollider>()) obj.AddComponent<MeshCollider>().sharedMesh=obj.GetComponent<MeshFilter>().sharedMesh;
        return obj;
    }

    static void CopyGroup(Transform baseRoot,Transform parent,string name,Func<Transform,bool> select,Vector3 target,float scale,float yaw,bool tree)
    {
        var parts=baseRoot.GetComponentsInChildren<Transform>().Where(t=>t.GetComponent<MeshFilter>() && select(t)).ToArray();
        if(parts.Length==0) throw new InvalidOperationException("Missing original vegetation: "+name);
        Vector3 pivot;
        if(tree) pivot=parts[0].position;
        else
        {
            var bounds=parts[0].GetComponent<Renderer>().bounds;
            foreach(var p in parts) bounds.Encapsulate(p.GetComponent<Renderer>().bounds);
            pivot=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        }
        var group=new GameObject(name); group.transform.SetParent(parent,false);
        foreach(var p in parts) { var clone=CopyPart(p,group.transform); clone.transform.localPosition-=pivot; }
        group.transform.localPosition=target; group.transform.localRotation=Quaternion.Euler(0,yaw,0); group.transform.localScale=Vector3.one*scale;
    }

    static GameObject ImportPlane()
    {
        Directory.CreateDirectory(Art+"/Models"); Directory.CreateDirectory(Art+"/Materials"); Directory.CreateDirectory(Art+"/Data");
        File.Copy(Source+"/FirstJourney_Airplane_v01.fbx",Art+"/Models/FirstJourney_Airplane_v01.fbx",false);
        File.Copy(Source+"/manifest.json",Art+"/Data/manifest.json",false);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var manifest=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Art+"/Data/manifest.json"));
        var materials=new Dictionary<string,Material>();
        foreach(var data in manifest["materials"])
        {
            string name=(string)data["name"];
            var a=data["base_color_linear"].Select(c=>(float)c).ToArray();
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
            Color color=new Color(a[0],a[1],a[2],a[3]).gamma;
            // URP transparency approximates the source windshield transmission.
            float transmission=(float)data["transmission"];
            if(transmission>0)
            {
                color.a=1-transmission;
                m.SetFloat("_Surface",1); m.SetFloat("_Blend",0); m.SetFloat("_ZWrite",0);
                m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType","Transparent");
                m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.SetShaderPassEnabled("ShadowCaster",false);
            }
            m.SetColor("_BaseColor",color); m.SetFloat("_Metallic",(float)data["metallic"]); m.SetFloat("_Smoothness",1-(float)data["roughness"]);
            AssetDatabase.CreateAsset(m,Art+"/Materials/"+name+".mat"); materials.Add(name,m);
        }
        var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/Models/FirstJourney_Airplane_v01.fbx");
        importer.importCameras=false; importer.importLights=false; importer.importAnimation=false;
        importer.globalScale=1; importer.importNormals=ModelImporterNormals.Import;
        foreach(var pair in materials) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
        importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/FirstJourney_Airplane_v01.fbx");
        var plane=(GameObject)PrefabUtility.InstantiatePrefab(model);
        plane.name="Aviao_Original_SemPersonagem";
        foreach(var f in plane.GetComponentsInChildren<MeshFilter>())
        {
            string n=f.name;
            if(n.Contains("Fuselagem") || n.Contains("Asa_") || n.Contains("Pneu_")) f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(plane,Root+"/Prefabs/Airplane.prefab",InteractionMode.AutomatedAction);
        return plane;
    }

    public static object Build()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=ScenePath) throw new InvalidOperationException("Open the base scene outside Play Mode.");
        if(GameObject.Find("Ilha_Do_Aviao")) throw new InvalidOperationException("The island already exists; inspect before changing it.");
        var baseRoot=GameObject.Find("LuaBase_ArteOriginal");
        if(!baseRoot) throw new InvalidOperationException("Missing original island.");
        if(File.Exists(Art+"/Models/FirstJourney_Airplane_v01.fbx")) throw new InvalidOperationException("Airplane import already exists; refusing to overwrite.");
        Directory.CreateDirectory(IslandArt+"/Meshes"); Directory.CreateDirectory(Root+"/Scenes/Versions");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string backup=Root+"/Scenes/Versions/FirstJourney_LuaBase_AntesIlhaAviao.unity";
        if(!File.Exists(backup) && !AssetDatabase.CopyAsset(ScenePath,backup)) throw new IOException("Scene backup failed.");
        var plane=ImportPlane();
        var island=new GameObject("Ilha_Do_Aviao"); island.transform.position=IslandPosition;
        Undo.RegisterCreatedObjectUndo(island,"Criar ilha do aviao");
        var terrainSource=baseRoot.GetComponentsInChildren<Transform>().Single(t=>t.name=="ANG_Terreno");
        var terrain=CopyPart(terrainSource,island.transform); terrain.name="IlhaAviao_Terreno";
        terrain.transform.localPosition=Vector3.Scale(terrainSource.position,new Vector3(.83f,1,.78f));
        terrain.transform.localScale=Vector3.Scale(terrainSource.lossyScale,new Vector3(.83f,1,.78f));
        Patch(island.transform,"Clareira_Estacionamento",new Vector3(.4f,.246f,0),5.9f,4.65f,BaseMaterial("ANG_Caminho"),13);
        Surface(island.transform,"Caminho_Entrada_FuturaPonte",new Vector3(0,.247f,0),PatchMesh("Caminho_Entrada_FuturaPonte",new[]{new Vector2(-8.15f,-.95f),new Vector2(-5.4f,-1),new Vector2(-3.4f,-.9f),new Vector2(-3.4f,1.1f),new Vector2(-5.4f,1),new Vector2(-8.15f,.95f)}),BaseMaterial("ANG_Caminho"));
        Vector3[] moss={new Vector3(-6.2f,.235f,-3.1f),new Vector3(-3.3f,.235f,-5.1f),new Vector3(5.6f,.235f,-4.1f),new Vector3(6.5f,.235f,3.1f)};
        for(int i=0;i<moss.Length;i++) Patch(island.transform,"Musgo_"+(i+1),moss[i],1.5f,1,BaseMaterial("ANG_Musgo"),i+31);
        CopyGroup(baseRoot.transform,island.transform,"Arvore_Ilha_01",t=>t.name.StartsWith("ANG_Arvore_01__"),new Vector3(-3.4f,.22f,-5.3f),.78f,35,true);
        CopyGroup(baseRoot.transform,island.transform,"Arvore_Ilha_02",t=>t.name.StartsWith("ANG_Arvore_05__"),new Vector3(5.9f,.22f,-3.8f),.62f,-70,true);
        Vector3[] rocks={new Vector3(-6.5f,.23f,-2.8f),new Vector3(-6.5f,.23f,3.5f),new Vector3(3.4f,.23f,5.5f),new Vector3(5.8f,.23f,3.9f),new Vector3(7.2f,.23f,-1.4f),new Vector3(-.8f,.23f,-6),new Vector3(-4.8f,.23f,-4.5f),new Vector3(.7f,.23f,6)};
        var rockSource=baseRoot.GetComponentsInChildren<Transform>().Single(t=>t.name=="ANG_Pedra_08");
        for(int i=0;i<rocks.Length;i++)
        {
            var rock=CopyPart(rockSource,island.transform); rock.name="Pedra_Ilha_"+(i+1);
            rock.transform.localPosition=rocks[i]; rock.transform.localRotation=Quaternion.Euler(0,41*i,0);
            rock.transform.localScale=rockSource.lossyScale*(.8f+.13f*(i%4));
        }
        Vector3[] plants={new Vector3(-6.9f,.23f,-1.9f),new Vector3(-5.7f,.23f,4.4f),new Vector3(6.8f,.23f,2.8f),new Vector3(4.6f,.23f,-4.8f)};
        for(int i=0;i<plants.Length;i++) CopyGroup(baseRoot.transform,island.transform,"Planta_Ilha_"+(i+1),t=>t.name=="ANG_Planta_Caule_0" || t.name.StartsWith("ANG_Planta_Folha_0_"),plants[i],1.15f,75*i,false);
        plane.transform.SetParent(island.transform,true);
        var pb=BoundsOf(plane);
        plane.transform.position+=new Vector3(IslandPosition.x+.4f-pb.center.x,.247f-pb.min.y,IslandPosition.z-pb.center.z);
        var planeMarker=new GameObject("Ponto_Interacao_Aviao"); planeMarker.transform.SetParent(island.transform,false); planeMarker.transform.localPosition=new Vector3(-2,.25f,1.7f);
        var bridgeMarker=new GameObject("Encaixe_Futura_Ponte"); bridgeMarker.transform.SetParent(island.transform,false); bridgeMarker.transform.localPosition=new Vector3(-8.15f,.22f,0);
        PrefabUtility.SaveAsPrefabAssetAndConnect(island,Root+"/Prefabs/AirfieldIsland.prefab",InteractionMode.AutomatedAction);
        WideCamera();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        Selection.activeGameObject=island;
        var view=SceneView.lastActiveSceneView;
        if(view!=null){ view.LookAt(new Vector3(10,1.6f,2.7f),Camera.main.transform.rotation,30,false); view.Repaint(); }
        return Validate();
    }

    public static void WideCamera()
    {
        Camera.main.transform.position=new Vector3(6,27,37);
        Camera.main.transform.LookAt(new Vector3(10,1.6f,2.7f)); Camera.main.fieldOfView=43;
    }

    public static void DetailCamera()
    {
        Camera.main.transform.position=IslandPosition+new Vector3(-10,13,16);
        Camera.main.transform.LookAt(IslandPosition+new Vector3(0,1.3f,0)); Camera.main.fieldOfView=43;
    }

    public static object Validate()
    {
        var island=GameObject.Find("Ilha_Do_Aviao"); var plane=GameObject.Find("Aviao_Original_SemPersonagem");
        if(!island || !plane) throw new InvalidOperationException("Island or airplane missing.");
        var renderers=island.GetComponentsInChildren<Renderer>();
        var source=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Art+"/Data/manifest.json"));
        // FBX omits unused empty slots, including the fuselage's spare slot.
        var expected=source["objects"].ToDictionary(o=>(string)o["name"],o=>o["materials"].Select(m=>(string)m).Where(m=>!String.IsNullOrEmpty(m)).OrderBy(m=>m).ToArray());
        var planeRenderers=plane.GetComponentsInChildren<Renderer>();
        var materialIssues=planeRenderers.Where(r=>!expected.ContainsKey(r.name) || !expected[r.name].SequenceEqual(r.sharedMaterials.Select(m=>m?m.name:"").OrderBy(m=>m))).Select(r=>r.name).ToArray();
        var pb=BoundsOf(plane);
        var ground=island.transform.Find("IlhaAviao_Terreno").GetComponent<MeshCollider>();
        var points=new[]{new Vector3(pb.min.x+.2f,10,pb.center.z),new Vector3(pb.max.x-.2f,10,pb.center.z),new Vector3(pb.center.x,10,pb.min.z+.2f),new Vector3(pb.center.x,10,pb.max.z-.2f),IslandPosition+new Vector3(-8.15f,10,0)};
        Physics.SyncTransforms();
        var supported=points.Select(p=>{ RaycastHit hit; return ground.Raycast(new Ray(p,Vector3.down),out hit,30); }).ToArray();
        var result=new { scene=SceneManager.GetActiveScene().path,islandPosition=IslandPosition.ToString(),terrainBounds=ground.bounds.ToString(),planeBounds=pb.ToString(),planeParts=planeRenderers.Length,personParts=plane.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("astronauta")),missingMaterials=renderers.Sum(r=>r.sharedMaterials.Count(m=>!m)),planeMaterialIssues=materialIssues,islandRenderers=renderers.Length,colliders=island.GetComponentsInChildren<Collider>().Length,terrainSupportChecks=supported,planeGroundHeight=pb.min.y,trees=island.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Arvore_Ilha_")),bridgeBuilt=GameObject.Find("Ponte_Entre_Ilhas")!=null,baseRenderers=GameObject.Find("LuaBase_ArteOriginal").GetComponentsInChildren<Renderer>().Length };
        File.WriteAllText("Docs/AirfieldIsland_Result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        if(result.personParts!=0 || result.missingMaterials!=0 || materialIssues.Length!=0 || planeRenderers.Length!=expected.Count || supported.Any(s=>!s) || result.baseRenderers!=584) throw new InvalidOperationException("Review validation failed: "+Newtonsoft.Json.JsonConvert.SerializeObject(result));
        return result;
    }
    public static void SaveWide()
    {
        WideCamera(); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    public static object Inspect()
    {
        var root = GameObject.Find("LuaBase_ArteOriginal");
        if (!root) throw new InvalidOperationException("Base island missing.");
        return root.GetComponentsInChildren<MeshFilter>().Where(f => f.name == "ANG_Terreno" || f.name == "ANG_Caminho_Terra" || f.name.StartsWith("ANG_Pedra") || f.name.StartsWith("ANG_Tufo") || f.name.StartsWith("ANG_Arvore_01__") || f.name.StartsWith("ANG_Mancha_Vegetacao_0"))
            .Select(f => new { name=f.name, position=f.transform.position.ToString(), scale=f.transform.lossyScale.ToString(), meshBounds=f.sharedMesh.bounds.ToString(), worldBounds=f.GetComponent<Renderer>().bounds.ToString(), materials=f.GetComponent<Renderer>().sharedMaterials.Select(m=>m.name).ToArray() }).ToArray();
    }
}
