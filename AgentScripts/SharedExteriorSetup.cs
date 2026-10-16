using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using FirstJourney;
public static class SharedExteriorSetup
{
    static readonly string[] Sources={"Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity","Assets/_FirstJourney/Scenes/FirstJourney_Asteroide01.unity","Assets/_FirstJourney/Scenes/FirstJourney_Reino01_FantasyKingdom.unity"};
    static readonly Vector3 Destination=new Vector3(0,0,180);
    static string Hash(string p){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","");}
    static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;Folder(Path.GetDirectoryName(p).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\','/'),Path.GetFileName(p));}
    static void RemoveRoot(Scene s,string n){var g=s.GetRootGameObjects().FirstOrDefault(r=>r.name==n);if(g)UnityEngine.Object.DestroyImmediate(g);}
    static GameObject Marker(string name,Vector3 point,Quaternion rotation){var g=new GameObject(name);g.transform.SetPositionAndRotation(point,rotation);return g;}
    public static object Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene: "+SceneManager.GetSceneAt(i).path);
        var paths=new[]{FJWorldSession.ExteriorPath,FJWorldSession.BasePath,FJWorldSession.AsteroidPath,FJWorldSession.WorldPath};
        if(paths.Any(File.Exists))throw new Exception("Exterior already exists; inspect instead of rebuilding");
        var hashes=Sources.Select(Hash).ToArray();
        Folder("Assets/_FirstJourney/Scenes/Exterior");Folder("Assets/_FirstJourney/Scenes/Worlds");Folder("Assets/_FirstJourney/Prefabs/Space");
        if(!AssetDatabase.CopyAsset(Sources[0],FJWorldSession.BasePath))throw new Exception("Base copy failed");
        var baseScene=EditorSceneManager.OpenScene(FJWorldSession.BasePath,OpenSceneMode.Single);
        var originalHero=baseScene.GetRootGameObjects().First(g=>g.name=="HeroSidekick");
        Vector3 spawn=originalHero.transform.position;Quaternion facing=originalHero.transform.rotation;
        foreach(var n in new[]{"HeroSidekick","Main Camera","Sun","Soft Fill"})RemoveRoot(baseScene,n);
        Marker("Inicio_Na_Base",spawn,facing);EditorSceneManager.SaveScene(baseScene);

        if(!AssetDatabase.CopyAsset(Sources[1],FJWorldSession.AsteroidPath))throw new Exception("Asteroid copy failed");
        var asteroidScene=EditorSceneManager.OpenScene(FJWorldSession.AsteroidPath,OpenSceneMode.Single);
        var sky=GameObject.Find("Espaco_Visual");
        const string skyPrefab="Assets/_FirstJourney/Prefabs/Space/Ceu_Exterior.prefab";
        PrefabUtility.SaveAsPrefabAsset(sky,skyPrefab);
        foreach(var g in asteroidScene.GetRootGameObjects())if(g.name!="Asteroide01_Exterior")UnityEngine.Object.DestroyImmediate(g);
        var asteroid=GameObject.Find("Asteroide01_Exterior");asteroid.transform.position+=Destination;
        var entrance=UnityEngine.Object.FindFirstObjectByType<FJWorldDoor>();entrance.destinationScene=FJWorldSession.WorldPath;entrance.actionLabel="Abrir porta · entrar no mundo";
        PrefabUtility.RecordPrefabInstancePropertyModifications(entrance);
        EditorSceneManager.MarkSceneDirty(asteroidScene);EditorSceneManager.SaveScene(asteroidScene);

        if(!AssetDatabase.CopyAsset(Sources[2],FJWorldSession.WorldPath))throw new Exception("World copy failed");
        var worldScene=EditorSceneManager.OpenScene(FJWorldSession.WorldPath,OpenSceneMode.Single);
        RemoveRoot(worldScene,"SceneIDMap");
        foreach(var n in new[]{"HeroSidekick","Main Camera","Vista_Geral_Reino01"})RemoveRoot(worldScene,n);
        var setup=GameObject.Find("SceneSetup");
        foreach(var t in setup.transform.Cast<Transform>().ToArray())if(t.name!="WindZone")UnityEngine.Object.DestroyImmediate(t.gameObject);
        foreach(var d in UnityEngine.Object.FindObjectsByType<FJWorldDoor>(FindObjectsSortMode.None))
        {d.destinationScene=FJWorldSession.ExteriorPath;EditorUtility.SetDirty(d);}
        EditorSceneManager.MarkSceneDirty(worldScene);EditorSceneManager.SaveScene(worldScene);

        var boot=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var global=new GameObject("Espaco_Ceu_e_Luzes");
        var background=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(skyPrefab),boot);
        background.transform.SetParent(global.transform,false);background.transform.localScale=Vector3.one*2.7f;PrefabUtility.RecordPrefabInstancePropertyModifications(background.transform);
        var sun=new GameObject("Sol_Exterior").AddComponent<Light>();sun.transform.SetParent(global.transform,false);sun.type=LightType.Directional;sun.intensity=1.6f;sun.color=new Color(1,.89f,.72f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(36,140,0);RenderSettings.sun=sun;
        var fill=new GameObject("Luz_Azul_Exterior").AddComponent<Light>();fill.transform.SetParent(global.transform,false);fill.type=LightType.Directional;fill.intensity=.55f;fill.color=new Color(.53f,.69f,1);fill.transform.rotation=Quaternion.Euler(-20,-35,0);
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.38f,.43f,.53f);RenderSettings.ambientEquatorColor=new Color(.27f,.29f,.34f);RenderSettings.ambientGroundColor=new Color(.15f,.12f,.16f);RenderSettings.fog=false;
        var hero=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FirstJourney/Prefabs/HeroSidekick.prefab"),boot);
        hero.name="HeroSidekick";hero.transform.SetPositionAndRotation(spawn,facing);
        var cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.fieldOfView=48;cam.nearClipPlane=.08f;cam.farClipPlane=1400;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.03f,.05f,.10f);cam.allowHDR=true;cam.gameObject.AddComponent<AudioListener>();cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var orbit=cam.gameObject.AddComponent<FJOrbitCamera>();orbit.target=hero.transform;orbit.distance=5.2f;orbit.pitch=28;orbit.yaw=55;
        var motor=hero.GetComponent<FJHeroMotor>();motor.movementCamera=cam;motor.spawnPosition=spawn;motor.fallResetHeight=-6;
        PrefabUtility.RecordPrefabInstancePropertyModifications(hero.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
        cam.transform.position=spawn+Vector3.up*1.15f+Quaternion.Euler(28,55,0)*Vector3.back*5.2f;cam.transform.LookAt(spawn+Vector3.up*1.15f);
        var session=new GameObject("Sessao_FirstJourney").AddComponent<FJWorldSession>();session.player=motor;session.playerCamera=cam;session.exteriorSkyAndLights=global;
        var overview=new GameObject("Vista_Espaco_Exterior").AddComponent<Camera>();overview.CopyFrom(cam);overview.enabled=false;overview.transform.position=new Vector3(195,130,130);overview.transform.LookAt(new Vector3(5,-1,82));overview.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var route=new GameObject("Rota_Voo_Base_Asteroide01");
        foreach(var pair in new[]{("Saida_Base",new Vector3(24.4f,5,15)),("Cruzeiro",new Vector3(10,10,90)),("Aproximacao_Asteroide",new Vector3(2.1f,5,171))})Marker(pair.Item1,pair.Item2,Quaternion.identity).transform.SetParent(route.transform,true);
        EditorSceneManager.SaveScene(boot,FJWorldSession.ExteriorPath);
        var build=paths.Select(p=>new EditorBuildSettingsScene(p,true)).Concat(EditorBuildSettings.scenes.Where(s=>!paths.Contains(s.path))).ToArray();EditorBuildSettings.scenes=build;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(FJWorldSession.BasePath,OpenSceneMode.Additive);EditorSceneManager.OpenScene(FJWorldSession.AsteroidPath,OpenSceneMode.Additive);SceneManager.SetActiveScene(boot);
        var sourceUnchanged=Sources.Select((p,i)=>new{path=p,hash=Hash(p),unchanged=hashes[i]==Hash(p)}).ToArray();
        if(sourceUnchanged.Any(s=>!s.unchanged))throw new Exception("Source scene changed unexpectedly");
        var report=new{bootstrap=FJWorldSession.ExteriorPath,contentScenes=paths.Skip(1).ToArray(),baseCenter=Vector3.zero.ToString(),asteroidCenter=Destination.ToString(),centerDistance=Destination.magnitude,onePersistentPlayer=true,planeStaysInBaseContent=true,flightImplemented=false,sources=sourceUnchanged};
        File.WriteAllText("Docs/Exterior_Construcao.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        var view=SceneView.lastActiveSceneView;if(view){view.LookAt(new Vector3(10,-1,85),overview.transform.rotation,120,false);view.Repaint();}
        return report;
    }
    public static object RemoveStaleSampleMetadata()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        var previous=SceneManager.GetActiveScene();var world=EditorSceneManager.OpenScene(FJWorldSession.WorldPath,OpenSceneMode.Additive);
        RemoveRoot(world,"SceneIDMap");EditorSceneManager.MarkSceneDirty(world);EditorSceneManager.SaveScene(world);EditorSceneManager.CloseScene(world,true);SceneManager.SetActiveScene(previous);
        return "Removed unused sample SceneIDMap with missing script from content copy only";
    }
}
