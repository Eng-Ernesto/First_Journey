using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;
public static class BaseMoonValidate
{
    public static object Materials()
    {
        var root=GameObject.Find("LuaBase_ArteOriginal");
        var expected=JObject.Parse(File.ReadAllText("Assets/_FirstJourney/Art/BaseMoon/Data/manifest.json"))["objects"];
        var renderers=root.GetComponentsInChildren<Renderer>().ToDictionary(r=>r.name,r=>r);
        var mismatch=new List<object>();
        foreach(var item in expected)
        {
            string name=(string)item["name"];
            Renderer renderer;
            if(!renderers.TryGetValue(name,out renderer)){mismatch.Add(new{name,problem="renderer absent"});continue;}
            var wanted=item["materials"].Values<string>().Where(x=>!String.IsNullOrEmpty(x)).OrderBy(x=>x).ToArray();
            var actual=renderer.sharedMaterials.Where(m=>m).Select(m=>m.name).OrderBy(x=>x).ToArray();
            if(!wanted.SequenceEqual(actual))mismatch.Add(new{name,expected=wanted,actual});
        }
        return new { mismatches=mismatch.Count, details=mismatch.Take(10).ToArray(), used_materials=renderers.Values.SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).Distinct().OrderBy(x=>x).ToArray() };
    }
}

