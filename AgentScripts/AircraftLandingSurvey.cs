using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
public static class AircraftLandingSurvey
{
    public static object Inspect()
    {
        Physics.SyncTransforms();var rows=new List<object>();
        for(float z=180;z<=187;z+=1)
        {
            var row=new List<string>();
            for(float x=-6;x<=6;x+=1)
            {
                var hits=Physics.RaycastAll(new Vector3(x,8,z),Vector3.down,12).OrderBy(h=>h.distance).ToArray();
                row.Add(x+":"+(hits.Length==0?"void":hits[0].point.y.ToString("F2")+" "+hits[0].collider.name));
            }
            rows.Add(new {z,row});
        }
        var house=GameObject.Find("Asteroide01_Exterior").GetComponentsInChildren<Renderer>().Where(r=>r.name.Contains("casa")).ToArray();
        return new {rows,house=house.Select(r=>new {r.name,bounds=r.bounds.ToString()})};
    }
}
