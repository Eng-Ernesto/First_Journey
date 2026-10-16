using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class FirstAsteroidSurvey
{
    public static object Inspect()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FirstJourney/Prefabs/BaseMoon.prefab");
        var parts=source.GetComponentsInChildren<MeshFilter>().Where(f=>!f.name.StartsWith("ANG_Arvore")&&!f.name.StartsWith("ANG_Erva")&&!f.name.StartsWith("ANG_Pedra")&&!f.name.StartsWith("ANG_Mato")).ToArray();
        return new{scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,play=EditorApplication.isPlaying,parts=parts.Select(f=>new{name=f.name,position=f.transform.position.ToString("F3"),size=f.GetComponent<Renderer>().bounds.size.ToString("F3"),center=f.GetComponent<Renderer>().bounds.center.ToString("F3")}).ToArray()};
    }
    public static object Scenes()
    {
        return new { scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{var s=SceneManager.GetSceneAt(i);return new{path=s.path,loaded=s.isLoaded,dirty=s.isDirty,roots=s.GetRootGameObjects().Select(r=>new{name=r.name,active=r.activeSelf}).ToArray()};}).ToArray(),bases=Resources.FindObjectsOfTypeAll<GameObject>().Where(g=>g.name=="LuaBase_ArteOriginal").Select(g=>new{name=g.name,scene=g.scene.path,validScene=g.scene.IsValid(),flags=g.hideFlags.ToString(),asset=EditorUtility.IsPersistent(g)}).ToArray()};
    }
    public static object Meshes()
    {
        var house=GameObject.Find("Casa_Portal_ArteOriginal");return new{renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length,parts=house.GetComponentsInChildren<MeshFilter>().Take(12).Select(f=>new{name=f.name,mesh=f.sharedMesh.name,vertices=f.sharedMesh.vertexCount,submeshes=f.sharedMesh.subMeshCount,meshBounds=f.sharedMesh.bounds.ToString(),rendererBounds=f.GetComponent<Renderer>().bounds.ToString(),batch=f.GetComponent<MeshRenderer>().isPartOfStaticBatch,path=AssetDatabase.GetAssetPath(f.sharedMesh)}).ToArray()};
    }
    public static object Hero()
    {
        var hero=GameObject.Find("HeroSidekick");var animator=hero.GetComponentInChildren<Animator>();var p=hero.transform.position;RaycastHit hit;bool ground=Physics.Raycast(new Vector3(1.25f,5,4.6f),Vector3.down,out hit,20);
        return new{play=EditorApplication.isPlaying,position=p.ToString(),groundHit=ground,groundY=ground?hit.point.y:-100,groundName=ground?hit.collider.name:"none",animator=animator.avatar.name,skins=hero.GetComponentsInChildren<SkinnedMeshRenderer>().Select(s=>new{name=s.name,enabled=s.enabled,active=s.gameObject.activeInHierarchy,visible=s.isVisible,offscreen=s.updateWhenOffscreen,bounds=s.bounds.ToString(),mesh=AssetDatabase.GetAssetPath(s.sharedMesh),bones=s.bones.Length}).ToArray()};
    }
}
