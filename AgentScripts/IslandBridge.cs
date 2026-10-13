using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IslandBridge
{
    const string Root="Assets/_FirstJourney";
    const string Art=Root+"/Art/IslandBridge";
    const string ScenePath=Root+"/Scenes/FirstJourney_LuaBase.unity";
    static readonly Vector3 Start=new Vector3(8,.22f,4.5f);
    static readonly Vector3 End=new Vector3(16.65f,.22f,4.5f);
    static readonly Vector3[] Approach={new Vector3(.2f,.22f,6.7f),new Vector3(2.9f,.22f,6.9f),new Vector3(5.1f,.22f,6.8f),new Vector3(6.3f,.22f,6.3f),new Vector3(7.2f,.22f,5.3f),Start};
    static float Length => Vector3.Distance(Start,End);
    static float Floor(float z) => .05f+.17f*Mathf.Sin(Mathf.PI*Mathf.Clamp01(z/Length));
    static Material Wood(bool light=false) => AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/BaseMoon/Materials/ANG_Madeira"+(light?"Clara":"")+".mat");

    static void Face(List<Vector3> vertices,List<int> triangles,List<Vector3> normals,Vector3[] points,Vector3 normal)
    {
        normal.Normalize();
        Vector3 center=points.Aggregate(Vector3.zero,(a,p)=>a+p)/points.Length;
        Vector3 u=Vector3.Cross(normal,Mathf.Abs(normal.y)<.9f?Vector3.up:Vector3.right).normalized;
        Vector3 v=Vector3.Cross(normal,u);
        points=points.OrderBy(p=>Mathf.Atan2(Vector3.Dot(p-center,v),Vector3.Dot(p-center,u))).ToArray();
        int offset=vertices.Count; vertices.AddRange(points); normals.AddRange(points.Select(p=>normal));
        for(int i=1;i<points.Length-1;i++) triangles.AddRange(new[]{offset,offset+i,offset+i+1});
    }

    static Vector3 BevelCorner(int axis,Vector3 signs,Vector3 half,float bevel)
    {
        var p=Vector3.Scale(signs,half-Vector3.one*bevel); p[axis]=signs[axis]*half[axis]; return p;
    }

    static Mesh Timber(string name,Vector3 size,float bevel)
    {
        Vector3 half=size*.5f;
        bevel=Mathf.Min(bevel,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.45f);
        var vertices=new List<Vector3>(); var triangles=new List<int>(); var normals=new List<Vector3>();
        for(int axis=0;axis<3;axis++) foreach(int sign in new[]{-1,1})
        {
            int b=(axis+1)%3,c=(axis+2)%3;
            var points=new List<Vector3>();
            foreach(int sb in new[]{-1,1}) foreach(int sc in new[]{-1,1})
            { var s=Vector3.zero; s[axis]=sign; s[b]=sb; s[c]=sc; points.Add(BevelCorner(axis,s,half,bevel)); }
            var normal=Vector3.zero; normal[axis]=sign; Face(vertices,triangles,normals,points.ToArray(),normal);
        }
        for(int a=0;a<3;a++) for(int b=a+1;b<3;b++) foreach(int sa in new[]{-1,1}) foreach(int sb in new[]{-1,1})
        {
            int c=3-a-b; var points=new List<Vector3>();
            foreach(int sc in new[]{-1,1}) { var s=Vector3.zero; s[a]=sa; s[b]=sb; s[c]=sc; points.Add(BevelCorner(a,s,half,bevel)); points.Add(BevelCorner(b,s,half,bevel)); }
            var normal=Vector3.zero; normal[a]=sa; normal[b]=sb; Face(vertices,triangles,normals,points.ToArray(),normal);
        }
        foreach(int sx in new[]{-1,1}) foreach(int sy in new[]{-1,1}) foreach(int sz in new[]{-1,1})
        {
            var s=new Vector3(sx,sy,sz); Face(vertices,triangles,normals,Enumerable.Range(0,3).Select(a=>BevelCorner(a,s,half,bevel)).ToArray(),s);
        }
        var mesh=new Mesh{name=name}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.SetNormals(normals);
        mesh.SetUVs(0,vertices.Select(p=>new Vector2(p.x+p.z,p.y)).ToList()); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Art+"/Meshes/"+name+".asset"); return mesh;
    }

    static GameObject Piece(Transform parent,string name,Mesh mesh,Material material,Vector3 position)
    {
        var obj=new GameObject(name); obj.transform.SetParent(parent,false); obj.transform.localPosition=position;
        obj.AddComponent<MeshFilter>().sharedMesh=mesh; obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        return obj;
    }

    static GameObject Beam(Transform parent,string name,Mesh mesh,Material material,Vector3 a,Vector3 b)
    {
        var obj=Piece(parent,name,mesh,material,(a+b)*.5f);
        obj.transform.localRotation=Quaternion.LookRotation(b-a,Vector3.up);
        obj.transform.localScale=new Vector3(1,1,Vector3.Distance(a,b)); return obj;
    }

    static Mesh DeckColliderMesh()
    {
        var vertices=new List<Vector3>(); var triangles=new List<int>(); var normals=new List<Vector3>();
        int stations=60;
        for(int i=0;i<stations;i++)
        {
            float a=Length*i/stations,b=Length*(i+1)/stations;
            Vector3 la=new Vector3(-1.19f,Floor(a),a),ra=new Vector3(1.19f,Floor(a),a),lb=new Vector3(-1.19f,Floor(b),b),rb=new Vector3(1.19f,Floor(b),b);
            Face(vertices,triangles,normals,new[]{la,ra,lb,rb},Vector3.up);
            Face(vertices,triangles,normals,new[]{la-Vector3.up*.15f,ra-Vector3.up*.15f,lb-Vector3.up*.15f,rb-Vector3.up*.15f},Vector3.down);
            Face(vertices,triangles,normals,new[]{la,lb,la-Vector3.up*.15f,lb-Vector3.up*.15f},Vector3.left);
            Face(vertices,triangles,normals,new[]{ra,rb,ra-Vector3.up*.15f,rb-Vector3.up*.15f},Vector3.right);
            if(i==0) Face(vertices,triangles,normals,new[]{la,ra,la-Vector3.up*.15f,ra-Vector3.up*.15f},Vector3.back);
            if(i==stations-1) Face(vertices,triangles,normals,new[]{lb,rb,lb-Vector3.up*.15f,rb-Vector3.up*.15f},Vector3.forward);
        }
        var mesh=new Mesh{name="Ponte_Colisao_Piso_Continuo"}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.SetNormals(normals); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Art+"/Meshes/"+mesh.name+".asset"); return mesh;
    }

    static void ApproachSurface(Transform parent)
    {
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        for(int i=0;i<Approach.Length;i++)
        {
            var tangent=(i==0?Approach[1]-Approach[0]:i==Approach.Length-1?Approach[i]-Approach[i-1]:Approach[i+1]-Approach[i-1]).normalized;
            var normal=Vector3.Cross(Vector3.up,tangent);
            foreach(float side in new[]{-.6f,.6f}) vertices.Add(parent.InverseTransformPoint(Approach[i]+normal*side+Vector3.up*.016f));
            if(i>0) { int o=2*i; triangles.AddRange(new[]{o-2,o,o-1,o-1,o,o+1}); }
        }
        // Ensure the visible surface faces upward in the bridge's local frame.
        for(int i=0;i<triangles.Count;i+=3) if(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).y<0) { int t=triangles[i+1]; triangles[i+1]=triangles[i+2]; triangles[i+2]=t; }
        var mesh=new Mesh{name="Caminho_Casa_Ponte"}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.SetUVs(0,vertices.Select(p=>new Vector2(p.x,p.z)).ToList()); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Art+"/Meshes/"+mesh.name+".asset");
        Piece(parent,mesh.name,mesh,AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/BaseMoon/Materials/ANG_Caminho.mat"),Vector3.zero);
    }

    public static object Build()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=ScenePath) throw new InvalidOperationException("Open the base scene outside Play Mode.");
        if(GameObject.Find("Ponte_Entre_Ilhas") || File.Exists(Art+"/Meshes/Tabua.asset")) throw new InvalidOperationException("Bridge already exists; inspect before changing it.");
        if(!Wood() || !Wood(true)) throw new InvalidOperationException("Original wood materials unavailable.");
        Inspect();
        if(Obstructions(Approach).Length!=0) throw new InvalidOperationException("The approach is obstructed.");
        Directory.CreateDirectory(Art+"/Meshes"); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string backup=Root+"/Scenes/Versions/FirstJourney_LuaBase_AntesPonte.unity";
        if(!File.Exists(backup) && !AssetDatabase.CopyAsset(ScenePath,backup)) throw new IOException("Scene backup failed.");
        var bridge=new GameObject("Ponte_Entre_Ilhas"); bridge.transform.position=Start; bridge.transform.rotation=Quaternion.LookRotation(End-Start,Vector3.up);
        Undo.RegisterCreatedObjectUndo(bridge,"Criar ponte entre ilhas");
        var deck=new GameObject("Tabuas"); deck.transform.SetParent(bridge.transform,false);
        var structure=new GameObject("Estrutura"); structure.transform.SetParent(bridge.transform,false);
        var rails=new GameObject("Corrimaos"); rails.transform.SetParent(bridge.transform,false);
        var collisions=new GameObject("Colisoes"); collisions.transform.SetParent(bridge.transform,false);
        int boards=26; float spacing=Length/boards;
        var boardMesh=Timber("Tabua",new Vector3(2.38f,.14f,spacing-.012f),.024f);
        var beamMesh=Timber("Viga",new Vector3(.16f,.18f,1),.025f);
        var postMesh=Timber("Poste",new Vector3(.17f,1.1f,.17f),.025f);
        var capMesh=Timber("Topo_Poste",new Vector3(.23f,.075f,.23f),.018f);
        var railMesh=Timber("Corrimao",new Vector3(.12f,.13f,1),.025f);
        var tieMesh=Timber("Travessa",new Vector3(2.26f,.13f,.17f),.025f);
        var anchorMesh=Timber("Apoio",new Vector3(.23f,1,.23f),.032f);
        for(int i=0;i<boards;i++)
        {
            float z=spacing*(i+.5f),derivative=.17f*Mathf.PI/Length*Mathf.Cos(Mathf.PI*z/Length);
            var board=Piece(deck.transform,"Tabua_"+(i+1).ToString("00"),boardMesh,Wood(i%5!=0),new Vector3(0,Floor(z)-.07f,z));
            board.transform.localRotation=Quaternion.LookRotation(new Vector3(0,derivative,1),Vector3.up);
            board.transform.localScale=new Vector3(1-.008f*(i%3),1,1);
            foreach(float side in new[]{-.87f,.87f})
            {
                float a=i*spacing,b=(i+1)*spacing;
                Beam(structure.transform,"Longarina_"+(side<0?"E_":"D_")+i,beamMesh,Wood(),new Vector3(side,Floor(a)-.19f,a),new Vector3(side,Floor(b)-.19f,b));
            }
        }
        float[] stations=Enumerable.Range(0,6).Select(i=>.14f+(Length-.28f)*i/5).ToArray();
        foreach(float side in new[]{-1.13f,1.13f})
        {
            string suffix=side<0?"E":"D";
            for(int i=0;i<stations.Length;i++)
            {
                float z=stations[i];
                Piece(rails.transform,"Poste_"+suffix+"_"+i,postMesh,Wood(),new Vector3(side,Floor(z)+.52f,z));
                Piece(rails.transform,"Topo_"+suffix+"_"+i,capMesh,Wood(true),new Vector3(side,Floor(z)+1.08f,z));
                if(i==stations.Length-1) continue;
                float next=stations[i+1];
                foreach(float height in new[]{.43f,.96f}) Beam(rails.transform,"Corrimao_"+suffix+"_"+i+"_"+height,railMesh,Wood(),new Vector3(side,Floor(z)+height,z),new Vector3(side,Floor(next)+height,next));
                var wall=new GameObject("Protecao_"+suffix+"_"+i); wall.transform.SetParent(collisions.transform,false);
                Vector3 a=new Vector3(side,Floor(z)+.52f,z),b=new Vector3(side,Floor(next)+.52f,next);
                wall.transform.localPosition=(a+b)*.5f; wall.transform.localRotation=Quaternion.LookRotation(b-a,Vector3.up);
                wall.AddComponent<BoxCollider>().size=new Vector3(.20f,1.09f,Vector3.Distance(a,b)+.08f);
            }
        }
        foreach(float z in new[]{Length*.2f,Length*.5f,Length*.8f}) Piece(structure.transform,"Travessa_"+z,tieMesh,Wood(),new Vector3(0,Floor(z)-.26f,z));
        foreach(float z in new[]{.55f,Length-.55f}) foreach(float side in new[]{-.87f,.87f})
        {
            float height=Floor(z)+.65f;
            var anchor=Piece(structure.transform,"Apoio_"+side+"_"+z,anchorMesh,Wood(),new Vector3(side,(Floor(z)-.65f)*.5f,z)); anchor.transform.localScale=new Vector3(1,height,1);
        }
        var floor=new GameObject("Piso_Continuo"); floor.transform.SetParent(collisions.transform,false); floor.AddComponent<MeshCollider>().sharedMesh=DeckColliderMesh();
        ApproachSurface(bridge.transform);
        PrefabUtility.SaveAsPrefabAssetAndConnect(bridge,Root+"/Prefabs/IslandBridge.prefab",InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        Selection.activeGameObject=bridge;
        return Validate();
    }

    public static object Validate()
    {
        var bridge=GameObject.Find("Ponte_Entre_Ilhas"); if(!bridge) throw new InvalidOperationException("Bridge is missing.");
        Physics.SyncTransforms();
        var floor=bridge.GetComponentsInChildren<MeshCollider>().Single(c=>c.name=="Piso_Continuo");
        var failures=new List<string>(); int samples=0;
        for(int i=0;i<=60;i++) foreach(float side in new[]{-.85f,0,.85f})
        {
            float z=Mathf.Lerp(.015f,Length-.015f,i/60f); var p=bridge.transform.TransformPoint(new Vector3(side,Floor(z),z)); RaycastHit hit;
            samples++;
            if(!floor.Raycast(new Ray(p+Vector3.up*5,Vector3.down),out hit,10) || Mathf.Abs(hit.point.y-p.y)>.01f) failures.Add(p.ToString());
        }
        var renderers=bridge.GetComponentsInChildren<Renderer>();
        var result=new{scene=SceneManager.GetActiveScene().path,length=Length,deckWidth=2.38f,clearWidth=2.06f,boards=bridge.transform.Find("Tabuas").childCount,renderers=renderers.Length,colliders=bridge.GetComponentsInChildren<Collider>().Length,walkwaySamples=samples,walkwayFailures=failures.ToArray(),approachObstructions=Obstructions(Approach),missingMaterials=renderers.Sum(r=>r.sharedMaterials.Count(m=>!m)),baseRenderers=GameObject.Find("LuaBase_ArteOriginal").GetComponentsInChildren<Renderer>().Length};
        File.WriteAllText("Docs/IslandBridge_Result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        if(failures.Count!=0 || result.missingMaterials!=0 || result.baseRenderers!=584) throw new InvalidOperationException("Bridge verification failed: "+Newtonsoft.Json.JsonConvert.SerializeObject(result));
        return result;
    }

    public static object WalkTest()
    {
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Run the traversal check in Play Mode.");
        var probe=new GameObject("Verificacao_Temporaria_Travessia");
        try
        {
            probe.transform.position=Approach[0]+Vector3.up*.08f;
            var controller=probe.AddComponent<CharacterController>(); controller.height=1.72f; controller.radius=.28f; controller.center=new Vector3(0,.86f,0); controller.skinWidth=.02f; controller.stepOffset=.25f; controller.slopeLimit=45; controller.minMoveDistance=0;
            Physics.SyncTransforms();
            int steps=0,groundContacts=0; float minY=100;
            var targets=Approach.Skip(1).Concat(new[]{End+Vector3.right*.8f}).ToArray();
            foreach(var target in targets.Concat(targets.Reverse().Skip(1)).Concat(new[]{Approach[0]}))
            {
                int budget=1000;
                while(true)
                {
                    var delta=target-probe.transform.position; delta.y=0;
                    if(delta.magnitude<.055f) break;
                    if(--budget==0) throw new InvalidOperationException("Traversal blocked near "+probe.transform.position+" going to "+target);
                    var flags=controller.Move(delta.normalized*Mathf.Min(.04f,delta.magnitude)+Vector3.down*.035f);
                    if((flags&CollisionFlags.Below)!=0) groundContacts++;
                    steps++; minY=Mathf.Min(minY,probe.transform.position.y);
                    if(probe.transform.position.y<.10f) throw new InvalidOperationException("Traversal fell through the ground or bridge.");
                }
            }
            var lateral=new List<float>();
            foreach(int sign in new[]{-1,1})
            {
                controller.enabled=false; probe.transform.position=Vector3.Lerp(Start,End,.5f)+Vector3.up*(Floor(Length*.5f)+.02f); controller.enabled=true; Physics.SyncTransforms();
                for(int i=0;i<70;i++) controller.Move(new Vector3(0,-.035f,sign*.04f));
                float displacement=Mathf.Abs(probe.transform.position.z-Start.z); lateral.Add(displacement);
                if(displacement>1) throw new InvalidOperationException("A side protection did not stop the controller.");
            }
            var result=new{passed=true,controllerHeight=1.72f,controllerRadius=.28f,roundTrip=true,steps,groundContacts,minFootHeight=minY,sideProtectionDisplacements=lateral.ToArray()};
            File.WriteAllText("Docs/IslandBridge_Traversal.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented)); return result;
        }
        finally { UnityEngine.Object.DestroyImmediate(probe); }
    }

    public static void DetailCamera()
    {
        Camera.main.transform.position=new Vector3(18,9,15);
        Camera.main.transform.LookAt(new Vector3(12.3f,.7f,4.5f)); Camera.main.fieldOfView=43;
    }

    static string[] Obstructions(Vector3[] points)
    {
        var names=new HashSet<string>();
        for(int i=0;i<points.Length-1;i++)
        {
            var delta=points[i+1]-points[i];
            var lower=points[i]+Vector3.up*.32f;
            var upper=points[i]+Vector3.up*1.45f;
            foreach(var h in Physics.CapsuleCastAll(lower,upper,.28f,delta.normalized,delta.magnitude))
                if(h.collider.name!="ANG_Terreno" && h.collider.name!="IlhaAviao_Terreno" && h.collider.name!="Piso_Continuo") names.Add(h.collider.name);
        }
        return names.ToArray();
    }

    public static object Inspect()
    {
        Physics.SyncTransforms();
        var baseRoot=GameObject.Find("LuaBase_ArteOriginal"); var island=GameObject.Find("Ilha_Do_Aviao");
        if(!baseRoot || !island) throw new InvalidOperationException("Both islands must be open.");
        var baseTerrain=baseRoot.GetComponentsInChildren<MeshCollider>().Single(c=>c.name=="ANG_Terreno");
        var islandTerrain=island.GetComponentsInChildren<MeshCollider>().Single(c=>c.name=="IlhaAviao_Terreno");
        var support=new List<object>();
        foreach(var endpoint in new[]{Start,End})
        foreach(float side in new[]{-1.15f,0,1.15f})
        {
            RaycastHit hit;
            var c=endpoint==Start?baseTerrain:islandTerrain;
            bool ok=c.Raycast(new Ray(endpoint+new Vector3(0,10,side),Vector3.down),out hit,20);
            support.Add(new{point=(endpoint+new Vector3(0,0,side)).ToString(),ok,height=hit.point.y});
        }
        var pathSupport=new List<object>();
        for(int i=0;i<Approach.Length-1;i++)
        {
            var tangent=(Approach[i+1]-Approach[i]).normalized;
            var normal=Vector3.Cross(Vector3.up,tangent);
            for(int j=0;j<=10;j++) foreach(float side in new[]{-.6f,0,.6f})
            {
                var p=Vector3.Lerp(Approach[i],Approach[i+1],j/10f)+normal*side;
                RaycastHit hit;
                if(!baseTerrain.Raycast(new Ray(p+Vector3.up*10,Vector3.down),out hit,20)) pathSupport.Add(new{point=p.ToString()});
            }
        }
        return new{scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,start=Start.ToString(),end=End.ToString(),support,approachObstructions=Obstructions(Approach),unsupportedPathPoints=pathSupport,baseRenderers=baseRoot.GetComponentsInChildren<Renderer>().Length};
    }
}
