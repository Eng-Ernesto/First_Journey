using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FirstJourney;
public static class ExteriorSourcesSurvey
{
    public static object Inspect()
    {
        var original=SceneManager.GetActiveScene();
        var s=EditorSceneManager.OpenScene("Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity",OpenSceneMode.Additive);
        try
        {
            var all=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            return new{playing=EditorApplication.isPlaying,active=original.path,dirty=original.isDirty,roots=s.GetRootGameObjects().Select(g=>new{name=g.name,children=g.transform.childCount,position=g.transform.position.ToString()}).ToArray(),hero=all.Select(t=>t.GetComponent<FJHeroMotor>()).Where(m=>m).Select(m=>new{position=m.transform.position.ToString(),spawn=m.spawnPosition.ToString(),avatar=m.animator.avatar.name,prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m)}).ToArray(),plane=all.Where(t=>t.name.ToLowerInvariant().Contains("aviao")||t.name.ToLowerInvariant().Contains("airplane")).Select(t=>new{name=t.name,position=t.position.ToString(),children=t.childCount,renderers=t.GetComponentsInChildren<Renderer>().Length}).ToArray()};
        }
        finally{EditorSceneManager.CloseScene(s,true);SceneManager.SetActiveScene(original);}
    }
}
