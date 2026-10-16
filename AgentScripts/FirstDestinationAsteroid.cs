using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using FirstJourney;

public static class FirstDestinationAsteroid
{
    public const string ScenePath="Assets/_FirstJourney/Scenes/FirstJourney_Asteroide01.unity";
    const string Art="Assets/_FirstJourney/Art/Worlds/Asteroide01";
    const string PrefabPath="Assets/_FirstJourney/Prefabs/Asteroide01_Exterior.prefab";
    const string BaseScene="Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity";
    static readonly Vector3 HousePosition=new Vector3(-1,0,-1.3f);
    static readonly Vector3 HousePivot=new Vector3(1,.22f,.6f);
    static readonly Vector3 Spawn=new Vector3(1.25f,.06f,4.6f);
    static void Folder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    static T Save<T>(T asset,string path) where T:UnityEngine.Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old){EditorUtility.CopySerialized(asset,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(asset);return old;}
        AssetDatabase.CreateAsset(asset,path);return asset;
    }
    static Material Mat(string name,string hex,bool unlit=false)
    {
        Color color;if(!ColorUtility.TryParseHtmlString("#"+hex,out color))throw new Exception(hex);
        var m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);
        if(!unlit){m.SetFloat("_Smoothness",.12f);m.SetFloat("_Metallic",0);}
        return Save(m,Art+"/Materials/"+name+".mat");
    }
    sealed class Geometry
    {
        public List<Vector3> points=new List<Vector3>();public List<int>[] triangles;
        public Geometry(int materials){triangles=Enumerable.Range(0,materials).Select(i=>new List<int>()).ToArray();}
        public void Tri(Vector3 a,Vector3 b,Vector3 c,int material,Vector3 normal)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var t=b;b=c;c=t;}
            foreach(var p in new[]{a,b,c}){triangles[material].Add(points.Count);points.Add(p);}
        }
        public Mesh Mesh(string name)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(points);mesh.subMeshCount=triangles.Length;
            for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i);
            mesh.SetUVs(0,points.Select(p=>new Vector2(p.x*.15f,p.z*.15f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();
            return Save(mesh,Art+"/Meshes/"+name+".asset");
        }
    }
    static GameObject Draw(string name,Transform parent,Mesh mesh,Material[] materials,bool collider)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=materials;
        if(collider)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
    }
    static Mesh AsteroidMesh()
    {
        const int n=28;var geo=new Geometry(8);var rings=new List<Vector3[]>();
        float[] heights={0,-.70f,-2.4f,-4.5f,-6.7f,-8.5f,-9.7f};float[] radii={7.2f,7.72f,8.05f,7.91f,6.8f,4.65f,2.1f};
        for(int j=0;j<heights.Length;j++)
        {
            var ring=new Vector3[n];for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n+(j%2)*.045f;
                float r=radii[j]*(1+.025f*Mathf.Sin(3*a+.3f)+.018f*Mathf.Sin(7*a+j*.6f));
                float y=heights[j]+(j==0?0:.11f*Mathf.Sin(5*a+j));
                ring[i]=new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r*.94f);
            }
            rings.Add(ring);
        }
        var inner=new Vector3[n];for(int i=0;i<n;i++)inner[i]=rings[0][i]*.52f;
        for(int i=0;i<n;i++)
        {
            int k=(i+1)%n;geo.Tri(Vector3.zero,inner[i],inner[k],0,Vector3.up);
            geo.Tri(inner[i],rings[0][i],rings[0][k],0,Vector3.up);geo.Tri(inner[i],rings[0][k],inner[k],0,Vector3.up);
        }
        for(int j=0;j<rings.Count-1;j++)for(int i=0;i<n;i++)
        {
            int k=(i+1)%n;var center=(rings[j][i]+rings[j][k]+rings[j+1][i]+rings[j+1][k])*.25f;
            var normal=center-new Vector3(0,-3.5f,0);int mat=3+(i*7+j*3)%5;
            geo.Tri(rings[j][i],rings[j+1][i],rings[j+1][k],mat,normal);
            geo.Tri(rings[j][i],rings[j+1][k],rings[j][k],3+(i*3+j)%5,normal);
        }
        for(int i=0;i<n;i++)geo.Tri(rings.Last()[i],new Vector3(0,-10.25f,0),rings.Last()[(i+1)%n],3+i%5,Vector3.down);
        return geo.Mesh("Asteroide01_Rocha_Facetada");
    }
    static GameObject House(Transform parent)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FirstJourney/Prefabs/BaseMoon.prefab");if(!source)throw new Exception("Original house source missing.");
        string[] groups={"ANG_Cabana_","ANG_Parede_","ANG_Porta_","ANG_Janela_","ANG_Telhado_","ANG_Beiral_","ANG_Caibro_","ANG_Capim_","ANG_Cumeeira_","ANG_Fibras_Capim_","ANG_Empena_","ANG_Esteio_","ANG_Varanda_","ANG_Degrau"};
        var house=new GameObject("Casa_Portal_ArteOriginal");house.transform.SetParent(parent,false);
        var door=new GameObject("Porta_Reino_FantasyKingdom");door.transform.SetParent(house.transform,false);door.transform.localPosition=new Vector3(.4f,.3f,2.04f);
        foreach(var part in source.GetComponentsInChildren<MeshFilter>().Where(f=>groups.Any(p=>f.name.StartsWith(p))))
        {
            bool leaf=part.name.StartsWith("ANG_Porta_Tabua")||part.name.StartsWith("ANG_Porta_Trava")||part.name=="ANG_Porta_Puxador";
            var go=new GameObject(part.name);go.transform.SetParent(house.transform,false);go.transform.localPosition=part.transform.position-HousePivot;go.transform.localRotation=part.transform.rotation;go.transform.localScale=part.transform.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh=part.sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterials=part.GetComponent<MeshRenderer>().sharedMaterials;
            if(part.GetComponent<MeshCollider>())go.AddComponent<MeshCollider>().sharedMesh=part.sharedMesh;
            if(leaf)go.transform.SetParent(door.transform,true);
        }
        var doorCollider=door.AddComponent<BoxCollider>();doorCollider.center=new Vector3(-.55f,.985f,.015f);doorCollider.size=new Vector3(1.10f,1.97f,.12f);
        house.transform.localPosition=HousePosition;
        var entry=new GameObject("Entrada_Reino_FantasyKingdom");entry.transform.SetParent(parent,false);entry.transform.localPosition=new Vector3(-1.175f,.32f,.87f);entry.transform.localRotation=Quaternion.Euler(0,180,0);
        var exit=new GameObject("Regresso_Do_Reino");exit.transform.SetParent(parent,false);exit.transform.localPosition=new Vector3(-1.175f,.04f,2.15f);
        var arrival=new GameObject("Chegada_Do_Aviao_Reserva");arrival.transform.SetParent(parent,false);arrival.transform.localPosition=new Vector3(2.1f,.04f,3.8f);
        return house;
    }
    static void Space(Scene scene)
    {
        var root=new GameObject("Espaco_Visual");SceneManager.MoveGameObjectToScene(root,scene);
        var texture=new Texture2D(512,256,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Repeat;
        var bottom=new Color(.025f,.040f,.085f);var middle=new Color(.075f,.12f,.18f);var top=new Color(.025f,.035f,.085f);
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
        {
            float v=(float)y/(texture.height-1);Color c=v<.5f?Color.Lerp(bottom,middle,v*2):Color.Lerp(middle,top,(v-.5f)*2);
            float mist=.5f+.5f*Mathf.Sin(x*.016f+y*.009f);c+=new Color(.015f,.006f,.025f)*mist;texture.SetPixel(x,y,c);
        }
        texture.Apply();texture.name="Ceu_Asteroide01";texture=Save(texture,Art+"/Textures/Ceu_Asteroide01.asset");
        var sky=GameObject.CreatePrimitive(PrimitiveType.Sphere);sky.name="Ceu_Profundo";sky.transform.SetParent(root.transform,false);sky.transform.localScale=Vector3.one*700;
        UnityEngine.Object.DestroyImmediate(sky.GetComponent<Collider>());
        var mat=Mat("M_Ceu_Asteroide01","FFFFFF",true);mat.SetTexture("_BaseMap",texture);mat.SetFloat("_Cull",0);EditorUtility.SetDirty(mat);sky.GetComponent<MeshRenderer>().sharedMaterial=mat;sky.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        var stars=new Geometry(3);var rng=new System.Random(61201);
        for(int i=0;i<310;i++)
        {
            float y=(float)rng.NextDouble()*2-1,a=(float)rng.NextDouble()*Mathf.PI*2;var dir=new Vector3(Mathf.Sqrt(1-y*y)*Mathf.Cos(a),y,Mathf.Sqrt(1-y*y)*Mathf.Sin(a));
            var p=dir*190;var side=Vector3.Cross(dir,Mathf.Abs(y)>.9f?Vector3.right:Vector3.up).normalized;var up=Vector3.Cross(side,dir).normalized;
            float s=.12f+(float)rng.NextDouble()*.32f;if(i%39==0)s=.9f;
            var l=p-side*s;var r=p+side*s;var t=p+up*s*(i%39==0?1.5f:1);var b=p-up*s*(i%39==0?1.5f:1);
            stars.Tri(l,b,r,i%3,-dir);stars.Tri(l,r,t,i%3,-dir);
        }
        var star=Draw("Estrelas",root.transform,stars.Mesh("Estrelas_Asteroide01"),new[]{Mat("M_Estrela_Marfim","F2DFB5",true),Mat("M_Estrela_Azul","B0CFDD",true),Mat("M_Estrela_Dourada","CBB278",true)},false);star.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static void LightScene(Scene scene)
    {
        var sun=new GameObject("Sol_Suave");SceneManager.MoveGameObjectToScene(sun,scene);var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.color=new Color(1,.89f,.72f);light.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(36,140,0);RenderSettings.sun=light;
        var fill=new GameObject("Luz_Azul_do_Espaco");SceneManager.MoveGameObjectToScene(fill,scene);var f=fill.AddComponent<Light>();f.type=LightType.Directional;f.intensity=.55f;f.color=new Color(.53f,.69f,1);fill.transform.rotation=Quaternion.Euler(-20,-35,0);
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.38f,.43f,.53f);RenderSettings.ambientEquatorColor=new Color(.27f,.29f,.34f);RenderSettings.ambientGroundColor=new Color(.15f,.12f,.16f);RenderSettings.fog=false;
    }
    public static object Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        if(File.Exists(ScenePath))throw new Exception("Asteroid scene already exists; refusing to replace it.");
        if(Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s=>s.isDirty))throw new Exception("An open scene has unsaved changes; preserve it before building.");
        Folder(Art+"/Meshes");Folder(Art+"/Materials");Folder(Art+"/Textures");
        string baseHash=Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(BaseScene)));
        var oldScenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var root=new GameObject("Asteroide01_Exterior");SceneManager.MoveGameObjectToScene(root,scene);
        var rock=Draw("Rocha_Asteroide01",root.transform,AsteroidMesh(),new[]{Mat("M_Superficie_Areia","B7A67D"),Mat("M_Superficie_Clara","BCAE8A"),Mat("M_Superficie_Oliva","AEA482"),Mat("M_Rocha_01","8D8073"),Mat("M_Rocha_02","95877A"),Mat("M_Rocha_03","7F786F"),Mat("M_Rocha_04","A09281"),Mat("M_Rocha_05","8D8580")},true);
        House(root.transform);PrefabUtility.SaveAsPrefabAssetAndConnect(root,PrefabPath,InteractionMode.AutomatedAction);
        Space(scene);LightScene(scene);
        var hero=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FirstJourney/Prefabs/HeroSidekick.prefab"),scene);hero.name="HeroSidekick";hero.transform.position=Spawn;hero.transform.rotation=Quaternion.Euler(0,180,0);
        var cameraGo=new GameObject("Main Camera");SceneManager.MoveGameObjectToScene(cameraGo,scene);cameraGo.tag="MainCamera";var camera=cameraGo.AddComponent<Camera>();cameraGo.AddComponent<AudioListener>();camera.fieldOfView=48;camera.nearClipPlane=.08f;camera.farClipPlane=650;camera.backgroundColor=new Color(.03f,.05f,.10f);camera.clearFlags=CameraClearFlags.SolidColor;
        var extra=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");if(extra!=null)cameraGo.AddComponent(extra);
        var orbit=cameraGo.AddComponent<FJOrbitCamera>();orbit.target=hero.transform;orbit.yaw=180;orbit.pitch=26;orbit.distance=7.2f;camera.transform.position=hero.transform.position+Vector3.up*orbit.followHeight+Quaternion.Euler(orbit.pitch,orbit.yaw,0)*Vector3.back*orbit.distance;camera.transform.LookAt(hero.transform.position+Vector3.up*orbit.followHeight);
        var motor=hero.GetComponent<FJHeroMotor>();motor.movementCamera=camera;motor.spawnPosition=Spawn;
        var overview=new GameObject("Vista_Chegada_Asteroide01");SceneManager.MoveGameObjectToScene(overview,scene);var view=overview.AddComponent<Camera>();view.enabled=false;view.fieldOfView=36;view.nearClipPlane=.1f;view.farClipPlane=650;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=camera.backgroundColor;if(extra!=null)overview.AddComponent(extra);overview.transform.position=new Vector3(-22,13,28);overview.transform.LookAt(new Vector3(0,-2.3f,0));
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();foreach(var s in oldScenes)EditorSceneManager.CloseScene(s,true);
        Selection.activeGameObject=root;if(SceneView.lastActiveSceneView){SceneView.lastActiveSceneView.LookAt(new Vector3(0,-2.3f,0),overview.transform.rotation,21,false);SceneView.lastActiveSceneView.Repaint();}
        var result=new{scene=ScenePath,prefab=PrefabPath,diameterMeters=rock.GetComponent<Renderer>().bounds.size.x,heightMeters=rock.GetComponent<Renderer>().bounds.size.y,houseRenderers=root.GetComponentsInChildren<Renderer>().Length-1,trees=0,hero="Piloto aventureiro v02",kingdom="Fantasy Kingdom — escolhido pelo criador; integração pendente",doorFunctional=false,gravity="Constant downward on the upper surface; radial gravity is not enabled",spawn=Spawn.ToString(),baseSceneUnchanged=baseHash==Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(BaseScene))),sceneSaved=!scene.isDirty};
        File.WriteAllText("Docs/Asteroide01_Construcao.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
    public static object RefineSurface()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open the asteroid scene outside Play Mode.");
        var root=GameObject.Find("Asteroide01_Exterior");var rock=GameObject.Find("Rocha_Asteroide01");var mesh=AsteroidMesh();rock.GetComponent<MeshFilter>().sharedMesh=mesh;rock.GetComponent<MeshCollider>().sharedMesh=mesh;
        PrefabUtility.SaveAsPrefabAssetAndConnect(root,PrefabPath,InteractionMode.AutomatedAction);EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return new{surface="Uniform sandy cap; faceted rock below",sceneSaved=!SceneManager.GetActiveScene().isDirty};
    }
}
