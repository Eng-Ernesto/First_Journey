using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class FantasyKingdomSurvey
{
    public static object Open()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        if(SceneManager.GetActiveScene().isDirty)throw new Exception("Save the current scene first");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Demo.unity",OpenSceneMode.Single);
        return Describe();
    }
    public static object Describe()
    {
        var s=SceneManager.GetActiveScene();
        var transforms=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        return new{scene=s.path,dirty=s.isDirty,compiling=EditorApplication.isCompiling,compileFailed=EditorUtility.scriptCompilationFailed,roots=s.GetRootGameObjects().Select(g=>new{name=g.name,active=g.activeSelf,children=g.transform.childCount,position=g.transform.position.ToString()}).ToArray(),terrains=Terrain.activeTerrains.Select(t=>new{name=t.name,position=t.transform.position.ToString(),size=t.terrainData.size.ToString(),collider=t.GetComponent<TerrainCollider>()!=null}).ToArray(),cameras=transforms.Select(t=>t.GetComponent<Camera>()).Where(c=>c).Select(c=>new{name=c.name,pos=c.transform.position.ToString(),rotation=c.transform.eulerAngles.ToString(),active=c.isActiveAndEnabled}).ToArray(),scripts=transforms.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(m=>m).GroupBy(m=>m.GetType().FullName).Select(g=>new{type=g.Key,count=g.Count()}).ToArray(),landmarks=transforms.Where(t=>t.name.IndexOf("gate",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("bridge",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("castle",StringComparison.OrdinalIgnoreCase)>=0).Take(65).Select(t=>new{name=t.name,pos=t.position.ToString(),children=t.childCount}).ToArray()};
    }
}
