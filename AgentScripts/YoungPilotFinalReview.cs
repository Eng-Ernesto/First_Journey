using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FirstJourney;

public static class YoungPilotFinalReview
{
    public static object Inspect()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before final review.");
        var actor=GameObject.Find("HeroSidekick");var animator=actor.GetComponentInChildren<Animator>();
        var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();
        int missingMaterials=skins.Sum(s=>s.sharedMaterials.Count(m=>!m));
        var logType=typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries");
        var counts=new object[]{0,0,0};logType.GetMethod("GetCountsByType",BindingFlags.Static|BindingFlags.Public).Invoke(null,counts);
        AssetDatabase.ImportAsset("Assets/_FirstJourney/Review/PilotoAventureiro_GameView_v02.png");
        Selection.activeGameObject=actor;
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(actor.transform.position+Vector3.up*.9f,Quaternion.Euler(12,158,0),2.1f,true);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        var result=new{
            date="2026-10-16",character="Piloto aventureiro v02",scene=SceneManager.GetActiveScene().path,
            prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor),avatar=animator.avatar.name,avatarValid=animator.avatar.isValid,avatarHuman=animator.avatar.isHuman,
            activeMotors=UnityEngine.Object.FindObjectsByType<FJHeroMotor>(FindObjectsSortMode.None).Length,
            renderers=skins.Length,vertices=skins.Sum(s=>s.sharedMesh.vertexCount),triangles=skins.Sum(s=>s.sharedMesh.triangles.Length/3),missingMaterials,
            cameraFollowsHero=Camera.main.GetComponent<FJOrbitCamera>().target==actor.transform,
            baseRenderers=GameObject.Find("LuaBase_ArteOriginal").GetComponentsInChildren<Renderer>().Length,
            compilationFailed=EditorUtility.scriptCompilationFailed,consoleErrors=counts[0],consoleWarnings=counts[1],sceneSaved=!SceneManager.GetActiveScene().isDirty,
            screenshot="Assets/_FirstJourney/Review/PilotoAventureiro_GameView_v02.png"
        };
        if(!animator.avatar.isValid||!animator.avatar.isHuman||missingMaterials!=0||EditorUtility.scriptCompilationFailed)throw new Exception("Final hero validation failed.");
        File.WriteAllText("Docs/Sidekick_v02_Final_Validation.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
}
