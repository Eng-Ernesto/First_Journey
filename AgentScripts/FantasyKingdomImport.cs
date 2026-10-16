using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
public static class FantasyKingdomImport
{
    public static async Task<object> Dependencies()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode before import.");
        var results = new System.Collections.Generic.List<string>();
        foreach(var id in new[]{"com.unity.cinemachine@3.1.7","com.unity.visualeffectgraph@17.3.0"})
        {
            var request=UnityEditor.PackageManager.Client.Add(id);
            while(!request.IsCompleted) await Task.Delay(200);
            if(request.Error!=null) throw new Exception(request.Error.message);
            results.Add(request.Result.name+"@"+request.Result.version);
        }
        return results.ToArray();
    }
    public static object Import()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play Mode before import.");
        var path=System.IO.Path.GetFullPath("Temp/FantasyKingdom_Import.unitypackage");
        if(!System.IO.File.Exists(path)) throw new Exception("Filtered package not prepared");
        AssetDatabase.importPackageFailed += (name,error)=>System.IO.File.WriteAllText("Temp/FantasyImportError.txt",name+": "+error);
        AssetDatabase.importPackageCompleted += name=>System.IO.File.WriteAllText("Temp/FantasyImportCompleted.txt",name);
        var m=typeof(AssetDatabase).GetMethod("ImportPackageImmediately",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
        var result=m.Invoke(null,new object[]{path});
        return new{called=true,path,result,exists=System.IO.File.Exists("Assets/Scenes/Demo.unity")};
    }
}
