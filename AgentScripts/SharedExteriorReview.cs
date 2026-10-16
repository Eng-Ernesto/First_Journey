using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FirstJourney;
public static class SharedExteriorReview
{
    public static async Task<object> Capture()
    {
        if(!EditorApplication.isPlaying||!FJWorldSession.Current.IsReady||FJWorldSession.Current.InsideWorld)throw new Exception("Open the running exterior");
        var session=FJWorldSession.Current;var main=session.playerCamera;var overview=GameObject.Find("Vista_Espaco_Exterior").GetComponent<Camera>();
        var doors=UnityEngine.Object.FindObjectsByType<FJWorldDoor>(FindObjectsSortMode.None).Where(d=>d.enabled).ToArray();
        foreach(var d in doors)d.enabled=false;
        bool hud=session.player.showControls;session.player.showControls=false;session.player.SetTestInput(Vector3.zero);
        overview.fieldOfView=35;main.enabled=false;overview.enabled=true;
        const string path="Assets/_FirstJourney/Review/EspacoExterior_Conjunto.png";
        try
        {
            int frame=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(8);while(Time.frameCount-frame<30&&DateTime.UtcNow<end)await Task.Delay(16);
            if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);for(int i=0;i<50&&!File.Exists(path);i++)await Task.Delay(50);
            return new{path,bytes=new FileInfo(path).Length};
        }
        finally{main.enabled=true;overview.enabled=false;session.player.showControls=hud;session.player.ClearTestInput();foreach(var d in doors)if(d)d.enabled=true;}
    }
    public static object OpenPreviewAndValidate()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        EditorSceneManager.OpenScene(FJWorldSession.ExteriorPath,OpenSceneMode.Single);
        var main=SceneManager.GetActiveScene();
        EditorSceneManager.OpenScene(FJWorldSession.BasePath,OpenSceneMode.Additive);EditorSceneManager.OpenScene(FJWorldSession.AsteroidPath,OpenSceneMode.Additive);SceneManager.SetActiveScene(main);
        var overview=GameObject.Find("Vista_Espaco_Exterior").GetComponent<Camera>();overview.fieldOfView=35;EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);
        AssetDatabase.ImportAsset("Assets/_FirstJourney/Review/EspacoExterior_Conjunto.png");AssetDatabase.SaveAssets();
        var roots=Enumerable.Range(0,SceneManager.sceneCount).SelectMany(i=>SceneManager.GetSceneAt(i).GetRootGameObjects()).ToArray();
        var entries=typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries");var counts=new object[]{0,0,0};entries.GetMethod("GetCountsByType",BindingFlags.Public|BindingFlags.Static).Invoke(null,counts);
        var view=SceneView.lastActiveSceneView;if(view){view.LookAt(new Vector3(10,-1,85),overview.transform.rotation,120,false);view.Focus();}
        var report=new{bootstrap=main.path,allSaved=Enumerable.Range(0,SceneManager.sceneCount).All(i=>!SceneManager.GetSceneAt(i).isDirty),scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray(),baseOriginalArtRenderers=GameObject.Find("LuaBase_ArteOriginal").GetComponentsInChildren<Renderer>().Length,planeRenderers=GameObject.Find("Aviao_Original_SemPersonagem").GetComponentsInChildren<Renderer>().Length,activeMotors=UnityEngine.Object.FindObjectsByType<FJHeroMotor>(FindObjectsSortMode.None).Length,activeCameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled),activeListeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled),missingMaterials=roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>()).Sum(r=>r.sharedMaterials.Count(m=>!m)),missingScripts=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),asteroidPosition=GameObject.Find("Asteroide01_Exterior").transform.position.ToString(),worldLoaded=SceneManager.GetSceneByPath(FJWorldSession.WorldPath).isLoaded,compilationFailed=EditorUtility.scriptCompilationFailed,consoleErrors=counts[0],consoleWarnings=counts[1],flightImplemented=false,standaloneBuildTested=false};
        File.WriteAllText("Docs/Exterior_Validacao.json",JsonConvert.SerializeObject(report,Formatting.Indented));return report;
    }
}
