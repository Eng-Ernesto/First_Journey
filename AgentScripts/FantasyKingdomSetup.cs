using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using FirstJourney;
public static class FantasyKingdomSetup
{
    const string Realm="Assets/_FirstJourney/Scenes/FirstJourney_Reino01_FantasyKingdom.unity";
    const string Asteroid="Assets/_FirstJourney/Scenes/FirstJourney_Asteroide01.unity";
    public static object Prepare()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        if(File.Exists(Realm))throw new Exception("Realm already exists; inspect before rebuilding");
        foreach(var w in Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w=>w.GetType().FullName=="Synty.SidekickCharacters.ModularCharacterWindow"))w.Close();
        if(!AssetDatabase.CopyAsset("Assets/Scenes/Demo.unity",Realm))throw new Exception("Could not duplicate sample scene");
        var s=EditorSceneManager.OpenScene(Realm,OpenSceneMode.Single);
        var all=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        // Keep the sample camera and UI in the copy for reference, disabled for third-person play.
        foreach(var t in all)
        {
            if(t.name=="Physical Camera"||t.name=="RTSCameraController"||t.name=="FogController"||t.name=="QualitySettingsController"||t.name=="Canvas")t.gameObject.SetActive(false);
        }
        foreach(var t in Terrain.activeTerrains)t.GetComponent<TerrainCollider>().enabled=true;
        foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(l.type==LightType.Directional){l.intensity=1.8f;l.shadows=LightShadows.Soft;}
        // The sample's cinematic exposure was tuned for its own renderer configuration.
        GameObject.Find("Volumes").SetActive(false);
        string folder="Assets/_FirstJourney/Art/Worlds/Reino01";
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile,folder+"/Reino01_Atmosfera.asset");
        var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
        var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(0);color.saturation.Override(-5);
        var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.15f);bloom.threshold.Override(1.2f);
        foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);
        var volume=new GameObject("Atmosfera_Reino01").AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;volume.sharedProfile=profile;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.45f,.55f,.66f);RenderSettings.ambientEquatorColor=new Color(.3f,.32f,.3f);RenderSettings.ambientGroundColor=new Color(.18f,.19f,.22f);RenderSettings.ambientIntensity=1;
        Physics.SyncTransforms();
        // Temporary starting position; the exact doorway is placed after a visual survey.
        Vector3 pos=new Vector3(60,17,90);
        var hero=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FirstJourney/Prefabs/HeroSidekick.prefab"),s);
        hero.name="HeroSidekick";hero.transform.position=pos;hero.transform.rotation=Quaternion.Euler(0,230,0);
        var motor=hero.GetComponent<FJHeroMotor>();motor.spawnPosition=pos;motor.fallResetHeight=-100;
        var cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.fieldOfView=55;cam.nearClipPlane=.1f;cam.farClipPlane=1300;cam.allowHDR=true;
        cam.gameObject.AddComponent<AudioListener>();cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var orbit=cam.gameObject.AddComponent<FJOrbitCamera>();orbit.target=hero.transform;orbit.yaw=230;orbit.pitch=23;orbit.distance=5.2f;motor.movementCamera=cam;
        var overview=new GameObject("Vista_Geral_Reino01").AddComponent<Camera>();overview.CopyFrom(cam);overview.transform.SetPositionAndRotation(new Vector3(183,115,160),Quaternion.Euler(28,230,0));overview.enabled=false;overview.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Selection.activeGameObject=hero;
        return new{scene=s.path,renderers=all.Sum(t=>t.GetComponents<Renderer>().Length),hero=hero.transform.position.ToString(),terrainCollider=Terrain.activeTerrain.GetComponent<TerrainCollider>().enabled,baseSceneUntouched=true};
    }
    public static object GroundSurvey()
    {
        Physics.SyncTransforms();
        var pts=new[]{new Vector2(60,90),new Vector2(85,85),new Vector2(110,100),new Vector2(80,110),new Vector2(100,0),new Vector2(0,100),new Vector2(0,0),new Vector2(120,70),new Vector2(130,90),new Vector2(65,20)};
        return pts.Select(p=>new{x=p.x,z=p.y,hits=Physics.RaycastAll(new Vector3(p.x,100,p.y),Vector3.down,250,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Take(4).Select(h=>new{y=h.point.y,name=h.collider.name,type=h.collider.GetType().Name,normal=h.normal.ToString()}).ToArray()}).ToArray();
    }
    public static object WalkableColliders()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        if(SceneManager.GetActiveScene().path!=Realm)throw new Exception("Open the realm copy");
        int disabled=0,added=0;
        foreach(var b in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
            if(b.name.Contains("_Collider_Collider")){b.enabled=false;disabled++;}
        foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(!f.sharedMesh||f.GetComponent<Collider>())continue;
            string p=AssetDatabase.GetAssetPath(f.sharedMesh);
            if(!(p.Contains("/Compositions/")||p.Contains("/Houses/")||p.Contains("/Rocks/")||p.Contains("/Props/")||p.Contains("/Village_Scatter/")))continue;
            if(System.Text.RegularExpressions.Regex.IsMatch(f.name,"LOD[1-9]"))continue;
            var mesh=f.gameObject.AddComponent<MeshCollider>();mesh.sharedMesh=f.sharedMesh;mesh.convex=false;added++;
        }
        Physics.SyncTransforms();
        var hero=GameObject.Find("HeroSidekick");var motor=hero.GetComponent<FJHeroMotor>();
        var pos=new Vector3(0,2,0);RaycastHit hit;
        if(Physics.Raycast(new Vector3(0,8,0),Vector3.down,out hit,30,~0,QueryTriggerInteraction.Ignore))pos.y=hit.point.y+.08f;
        hero.transform.position=pos;motor.spawnPosition=pos;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        return new{disabledProxyBoxes=disabled,accurateMeshColliders=added,spawn=pos.ToString()};
    }
    public static object ConnectDoors()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        if(SceneManager.GetActiveScene().path!=Realm)throw new Exception("Open the realm copy");
        foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            if(AssetDatabase.GetAssetPath(f.sharedMesh).Contains("/Village_Scatter/"))
            {var c=f.GetComponent<MeshCollider>();if(c)c.enabled=false;}
        var hero=GameObject.Find("HeroSidekick");var motor=hero.GetComponent<FJHeroMotor>();
        var spawn=new Vector3(4,.24f,6);hero.transform.position=spawn;hero.transform.rotation=Quaternion.Euler(0,270,0);motor.spawnPosition=spawn;
        var marker=new GameObject("Chegada_No_Reino");marker.transform.SetPositionAndRotation(spawn,hero.transform.rotation);
        var exit=new GameObject("Porta_Regresso_Asteroide01");exit.transform.position=new Vector3(4,.24f,10);
        var door=exit.AddComponent<FJWorldDoor>();door.destinationScene=Asteroid;door.arrivalMarker="Regresso_Do_Reino";door.actionLabel="Regressar ao asteroide";door.arrivalCameraYaw=180;door.interactionRadius=2.25f;
        var orbit=Camera.main.GetComponent<FJOrbitCamera>();orbit.yaw=270;orbit.pitch=24;orbit.distance=5.2f;
        Camera.main.transform.SetPositionAndRotation(spawn+Vector3.up*1.15f+Quaternion.Euler(24,270,0)*Vector3.back*5.2f,Quaternion.Euler(24,270,0));
        // The entrance is the open doorway beneath the red awning in the existing village house.
        var glow=new GameObject("Luz_Porta_Do_Regresso").AddComponent<Light>();glow.type=LightType.Point;glow.color=new Color(1,.67f,.32f);glow.intensity=2;glow.range=4;glow.transform.position=new Vector3(4,2,10.3f);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        var asteroid=EditorSceneManager.OpenScene(Asteroid,OpenSceneMode.Single);
        var entry=GameObject.Find("Entrada_Reino_FantasyKingdom");
        var enter=entry.AddComponent<FJWorldDoor>();enter.destinationScene=Realm;enter.arrivalMarker="Chegada_No_Reino";enter.actionLabel="Entrar no reino";enter.arrivalCameraYaw=270;enter.doorLeaf=GameObject.Find("Porta_Reino_FantasyKingdom").transform;
        EditorSceneManager.SaveScene(asteroid);
        var scenes=EditorBuildSettings.scenes.ToList();
        foreach(var p in new[]{Asteroid,Realm})
        {int i=scenes.FindIndex(s=>s.path==p);if(i>=0)scenes[i]=new EditorBuildSettingsScene(p,true);else scenes.Add(new EditorBuildSettingsScene(p,true));}
        EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
        return new{asteroid=Asteroid,realm=Realm,arrival=spawn.ToString(),returnHouse="Casa com toldo vermelho junto à praça",interaction="E / buttonWest (Square)",sourceScenePreserved=true,baseScenePreserved=true};
    }
}
