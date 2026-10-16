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

public static class FirstAsteroidReview
{
    public static async Task<object> CaptureGameOverview()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode.");
        Application.runInBackground=true;var viewType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).First(t=>t!=null);var view=EditorWindow.GetWindow(viewType);view.Show();view.Focus();
        var main=GameObject.Find("Main Camera").GetComponent<Camera>();var overview=GameObject.Find("Vista_Chegada_Asteroide01").GetComponent<Camera>();var actor=GameObject.Find("HeroSidekick");var motor=actor.GetComponent<FJHeroMotor>();
        motor.ResetToSpawn();motor.SetTestInput(Vector3.zero);bool hud=motor.showControls;motor.showControls=false;main.enabled=false;overview.enabled=true;
        const string path="Assets/_FirstJourney/Review/Asteroide01_GameView.png";
        try{int frame=Time.frameCount;var deadline=DateTime.UtcNow.AddSeconds(6);while(Time.frameCount-frame<12&&DateTime.UtcNow<deadline)await Task.Delay(16);if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);for(int i=0;i<50&&!File.Exists(path);i++)await Task.Delay(100);if(!File.Exists(path))throw new Exception("Screenshot not written.");return new{path,bytes=new FileInfo(path).Length,heroPosition=actor.transform.position.ToString(),framesSettled=Time.frameCount-frame};}
        finally{main.enabled=true;overview.enabled=false;motor.showControls=hud;motor.ClearTestInput();}
    }
    public static async Task<object> WalkAndJump()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode.");
        Application.runInBackground=true;var actor=GameObject.Find("HeroSidekick");var motor=actor.GetComponent<FJHeroMotor>();var animator=motor.animator;motor.ResetToSpawn();int respawns=motor.RespawnCount;
        var start=actor.transform.position;int grounded=0,samples=0;float maxSpeed=0;
        var started=DateTime.UtcNow;
        try
        {
            motor.SetTestInput(Vector3.zero);await Task.Delay(400);
            var route=new[]{new Vector3(3.8f,0,2.3f),new Vector3(4.6f,0,-.7f),new Vector3(3.3f,0,-4.2f),new Vector3(-1,0,-5.4f),new Vector3(-4.9f,0,-3.3f),new Vector3(-5.2f,0,.7f),new Vector3(-3.7f,0,3.6f),new Vector3(-1.175f,0,2.18f),new Vector3(.5f,0,4.7f)};
            foreach(var target in route)
            {
                int budget=900;
                while(true)
                {
                    var delta=target-actor.transform.position;delta.y=0;if(delta.magnitude<.16f)break;
                    if(--budget<=0||(DateTime.UtcNow-started).TotalSeconds>50)throw new Exception("Walk blocked: "+actor.transform.position+" => "+target);
                    motor.SetTestInput(Vector3.ClampMagnitude(delta*2,1),true);await Task.Delay(16);samples++;if(motor.Grounded)grounded++;maxSpeed=Mathf.Max(maxSpeed,animator.GetFloat("Speed"));
                    if(actor.transform.position.y<-.2f||motor.RespawnCount!=respawns)throw new Exception("Unexpected fall on the playable top.");
                }
            }
            motor.SetTestInput(Vector3.zero);await Task.Delay(400);float floor=actor.transform.position.y,peak=floor;bool jump=false,fall=false,land=false;motor.SetTestInput(Vector3.zero,false,true);
            for(int i=0;i<110;i++){await Task.Delay(16);peak=Mathf.Max(peak,actor.transform.position.y);var s=animator.GetCurrentAnimatorStateInfo(0);jump|=s.IsName("Jump");fall|=s.IsName("Fall");land|=s.IsName("Land");}
            if(peak-floor<.8f||!motor.Grounded||!jump||!fall||!land||maxSpeed<3)throw new Exception("Jump/movement verification failed.");
            var result=new{passed=true,samples,groundedSamples=grounded,maxSpeed,jumpRise=peak-floor,sawJump=jump,sawFall=fall,sawLand=land,unexpectedRespawns=motor.RespawnCount-respawns,avatarValid=animator.avatar.isValid,avatarHuman=animator.avatar.isHuman};
            File.WriteAllText("Docs/Asteroide01_Playtest.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
        }
        finally{motor.SetTestInput(Vector3.zero);motor.ClearTestInput();}
    }
    public static object InspectAndSave()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_FirstJourney/Scenes/FirstJourney_Asteroide01.unity")throw new Exception("Open the asteroid scene.");
        var root=GameObject.Find("Asteroide01_Exterior");var house=GameObject.Find("Casa_Portal_ArteOriginal");var renderer=root.GetComponentsInChildren<Renderer>();
        var entries=typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries");var counts=new object[]{0,0,0};entries.GetMethod("GetCountsByType",BindingFlags.Public|BindingFlags.Static).Invoke(null,counts);
        AssetDatabase.DeleteAsset("Assets/_FirstJourney/Review/Asteroide01_VistaGeral.png");
        AssetDatabase.ImportAsset("Assets/_FirstJourney/Review/Asteroide01_GameView.png");EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Selection.activeGameObject=root;
        var view=SceneView.lastActiveSceneView;if(view){var camera=GameObject.Find("Vista_Chegada_Asteroide01").transform;view.LookAt(new Vector3(0,-2.3f,0),camera.rotation,21,false);view.Repaint();}
        var result=new{scene=scene.path,sceneSaved=!scene.isDirty,houseRenderers=house.GetComponentsInChildren<Renderer>().Length,trees=house.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("ANG_Arvore")),missingMaterials=renderer.Sum(r=>r.sharedMaterials.Count(m=>!m)),activeMotors=UnityEngine.Object.FindObjectsByType<FJHeroMotor>(FindObjectsSortMode.None).Length,activeCameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled),doorCollider=GameObject.Find("Porta_Reino_FantasyKingdom").GetComponent<BoxCollider>()!=null,entrancePrepared=GameObject.Find("Entrada_Reino_FantasyKingdom")!=null,realmTransitionImplemented=false,compilationFailed=EditorUtility.scriptCompilationFailed,consoleErrors=counts[0],consoleWarnings=counts[1]};
        File.WriteAllText("Docs/Asteroide01_Validacao.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
}
