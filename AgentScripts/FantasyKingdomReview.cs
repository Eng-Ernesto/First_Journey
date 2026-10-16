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
public static class FantasyKingdomReview
{
    static async Task Settle(int count)
    {int frame=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(10);while(Time.frameCount-frame<count&&DateTime.UtcNow<end)await Task.Delay(16);}
    public static async Task<object> CaptureArrival()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode");
        Application.runInBackground=true;
        if(SceneManager.GetActiveScene().name=="FirstJourney_Asteroide01")
        {
            var d=UnityEngine.Object.FindFirstObjectByType<FJWorldDoor>();if(!d.TryTravel())throw new Exception("Approach the door first");
            var end=DateTime.UtcNow.AddSeconds(40);while(FJWorldDoor.IsTravelling){if(DateTime.UtcNow>end)throw new Exception("Transition timeout");await Task.Delay(50);}
        }
        var hero=UnityEngine.Object.FindFirstObjectByType<FJHeroMotor>();hero.ResetToSpawn();hero.SetTestInput(Vector3.zero);hero.showControls=false;
        var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).First(t=>t!=null);var view=EditorWindow.GetWindow(t);view.Show();view.Focus();
        const string path="Assets/_FirstJourney/Review/FantasyKingdom_GameView.png";
        try{await Settle(100);if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);for(int i=0;i<60&&!File.Exists(path);i++)await Task.Delay(100);if(!File.Exists(path))throw new Exception("Screenshot missing");return new{scene=SceneManager.GetActiveScene().path,path,bytes=new FileInfo(path).Length,hero=hero.transform.position.ToString(),idle=hero.animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion")};}
        finally{hero.showControls=true;hero.ClearTestInput();}
    }
    public static async Task<object> CaptureOverview()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode");
        var hero=UnityEngine.Object.FindFirstObjectByType<FJHeroMotor>();hero.SetTestInput(Vector3.zero);bool hud=hero.showControls;hero.showControls=false;
        var main=Camera.main;var overview=GameObject.Find("Vista_Geral_Reino01").GetComponent<Camera>();
        overview.transform.LookAt(new Vector3(20,4,20));main.enabled=false;overview.enabled=true;
        const string path="Assets/_FirstJourney/Review/FantasyKingdom_VistaGeral.png";
        try{await Settle(30);if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);for(int i=0;i<60&&!File.Exists(path);i++)await Task.Delay(100);return new{path,bytes=new FileInfo(path).Length};}
        finally{main.enabled=true;overview.enabled=false;hero.showControls=hud;hero.ClearTestInput();}
    }
    public static object ValidateAndSave()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        var s=SceneManager.GetActiveScene();if(s.path!="Assets/_FirstJourney/Scenes/FirstJourney_Reino01_FantasyKingdom.unity")s=EditorSceneManager.OpenScene("Assets/_FirstJourney/Scenes/FirstJourney_Reino01_FantasyKingdom.unity",OpenSceneMode.Single);
        var entries=typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries");entries.GetMethod("Clear",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
        AssetDatabase.ImportAsset("Assets/_FirstJourney/Review/FantasyKingdom_GameView.png");
        var objects=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var renderers=objects.SelectMany(t=>t.GetComponents<Renderer>()).ToArray();
        var hero=UnityEngine.Object.FindFirstObjectByType<FJHeroMotor>();
        var counts=new object[]{0,0,0};entries.GetMethod("GetCountsByType",BindingFlags.Public|BindingFlags.Static).Invoke(null,counts);
        EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Selection.activeGameObject=hero.gameObject;
        var sceneView=SceneView.lastActiveSceneView;if(sceneView){sceneView.LookAt(hero.transform.position+Vector3.up,Quaternion.Euler(28,225,0),12,false);sceneView.Repaint();}
        var report=new{scene=s.path,sceneSaved=!s.isDirty,sourceScene="Assets/Scenes/Demo.unity",missingActiveMaterials=renderers.Where(r=>r.gameObject.activeInHierarchy&&r.enabled).Sum(r=>r.sharedMaterials.Count(m=>!m)),inheritedMissingInactiveRockMaterials=renderers.Where(r=>!r.gameObject.activeInHierarchy).Sum(r=>r.sharedMaterials.Count(m=>!m)),unsupportedMaterials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader&&!m.shader.isSupported).Select(m=>m.name).Distinct().ToArray(),missingScripts=objects.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),activeMotors=UnityEngine.Object.FindObjectsByType<FJHeroMotor>(FindObjectsSortMode.None).Length,activeCameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled),terrainCollider=Terrain.activeTerrain.GetComponent<TerrainCollider>().enabled,accurateMeshColliders=objects.SelectMany(t=>t.GetComponents<MeshCollider>()).Count(c=>c.enabled),returnPortal=UnityEngine.Object.FindFirstObjectByType<FJWorldDoor>().destinationScene,fallResetHeight=hero.fallResetHeight,avatarValid=hero.animator.avatar.isValid,avatarHuman=hero.animator.avatar.isHuman,compilationFailed=EditorUtility.scriptCompilationFailed,consoleErrorsAfterClearingResolvedImportMessages=counts[0],consoleWarnings=counts[1],standaloneBuildTested=false};
        File.WriteAllText("Docs/FantasyKingdom_Validacao.json",JsonConvert.SerializeObject(report,Formatting.Indented));return report;
    }
}
