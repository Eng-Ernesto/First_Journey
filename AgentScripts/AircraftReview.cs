using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FirstJourney;
public static class AircraftReview
{
    public static object ValidateSaved()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        EditorSceneManager.OpenScene(FJWorldSession.ExteriorPath,OpenSceneMode.Single);var main=SceneManager.GetActiveScene();
        EditorSceneManager.OpenScene(FJWorldSession.BasePath,OpenSceneMode.Additive);EditorSceneManager.OpenScene(FJWorldSession.AsteroidPath,OpenSceneMode.Additive);SceneManager.SetActiveScene(main);
        var plane=UnityEngine.Object.FindFirstObjectByType<FJAircraftController>();
        var session=UnityEngine.Object.FindFirstObjectByType<FJWorldSession>();
        var roots=Enumerable.Range(0,SceneManager.sceneCount).SelectMany(i=>SceneManager.GetSceneAt(i).GetRootGameObjects()).ToArray();
        string[] paths={"Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity","Assets/_FirstJourney/Scenes/FirstJourney_Asteroide01.unity","Assets/_FirstJourney/Scenes/FirstJourney_Reino01_FantasyKingdom.unity"};
        string[] expected={"31E01E8E14F302A09DEC7C200E9449FF99851D02BC3D21E70389B2C2D0B8758F","EB081FE0C83C533953AC5AA1246F6079EBD67CD0148DF2F6B6093AEAAF4AA75C","F77D10539B60BFE5C6FE8DD78D841F041D318A016CEEBA822D8CDEEE20FA720C"};
        var sources=paths.Select((p,i)=>{using(var sha=SHA256.Create()){var hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","");return new{path=p,hash,unchanged=hash==expected[i]};}}).ToArray();
        int missingScripts=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        int missingMaterials=roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>()).Sum(r=>r.sharedMaterials.Count(m=>!m));
        var logType=typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries");var counts=new object[]{0,0,0};logType.GetMethod("GetCountsByType",BindingFlags.Public|BindingFlags.Static).Invoke(null,counts);
        var view=SceneView.lastActiveSceneView;
        if(view){view.LookAt(plane.transform.position+Vector3.up,Quaternion.Euler(22,140,0),14,false);}
        foreach(var capture in Directory.GetFiles("Assets/_FirstJourney/Review","Aviao*.png"))AssetDatabase.ImportAsset(capture.Replace('\\','/'));
        AssetDatabase.SaveAssets();Selection.activeGameObject=plane.gameObject;
        bool refs=plane.seat&&plane.boardingPoint&&plane.propeller&&plane.propellerParts.Length==3&&plane.startingSite&&session.playerCamera.GetComponent<FJAircraftCamera>();
        int sites=UnityEngine.Object.FindObjectsByType<FJLandingSite>(FindObjectsSortMode.None).Length;
        int cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled);
        bool allSaved=Enumerable.Range(0,SceneManager.sceneCount).All(i=>!SceneManager.GetSceneAt(i).isDirty);
        int staticPlaneParts=plane.GetComponentsInChildren<Transform>(true).Count(t=>t.gameObject.isStatic);
        bool passed=refs&&sites==2&&cameras==1&&allSaved&&missingScripts==0&&missingMaterials==0&&sources.All(s=>s.unchanged)&&!EditorUtility.scriptCompilationFailed&&staticPlaneParts==0&&(int)counts[0]==0;
        var result=new{passed,entryScene=main.path,allSaved,flightInstalled=refs,landingSites=sites,planeParts=plane.GetComponentsInChildren<Renderer>().Length,staticPlaneParts,baseOriginalArtRenderers=GameObject.Find("LuaBase_ArteOriginal").GetComponentsInChildren<Renderer>().Length,activeCameras=cameras,activeListeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled),activeMotors=UnityEngine.Object.FindObjectsByType<FJHeroMotor>(FindObjectsSortMode.None).Count(m=>m.isActiveAndEnabled),missingScripts,missingMaterials,compilationFailed=EditorUtility.scriptCompilationFailed,consoleErrors=counts[0],consoleWarnings=counts[1],sources,standaloneBuildTested=false,physicalControllerTested=false};
        File.WriteAllText("Docs/Aviao_Validacao.json",JsonConvert.SerializeObject(result,Formatting.Indented));
        if(!passed)throw new Exception(JsonConvert.SerializeObject(result));return result;
    }
}
