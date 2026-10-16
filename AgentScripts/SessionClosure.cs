using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class SessionClosure
{
    public static object PreparePlay()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play first.");
        Application.runInBackground=true;EditorApplication.isPaused=false;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).First(t=>t!=null);
        var view=EditorWindow.GetWindow(type);view.Show();view.Focus();
        return new{prepared=true,background=Application.runInBackground};
    }
    public static object Save()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before saving project state.");
        if(EditorUtility.scriptCompilationFailed)throw new Exception("Compilation failed.");
        if(!EditorSceneManager.SaveOpenScenes())throw new Exception("Could not save open scenes.");
        AssetDatabase.SaveAssets();
        return new{saved=true,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray()};
    }
}
