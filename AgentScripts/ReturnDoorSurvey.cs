using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ReturnDoorSurvey
{
    public static object CaptureHouse()
    {
        var main=Camera.main;var pos=main.transform.position;var rot=main.transform.rotation;float fov=main.fieldOfView;
        var texture=new RenderTexture(1280,720,24);var old=RenderTexture.active;
        try
        {
            main.transform.position=new Vector3(14,2.8f,14);main.transform.LookAt(new Vector3(5,1.5f,14));main.fieldOfView=60;
            main.targetTexture=texture;main.Render();RenderTexture.active=texture;
            var png=new Texture2D(1280,720,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1280,720),0,0);png.Apply();File.WriteAllBytes("Temp/HouseReturnSurvey.png",png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);
            return new{path="Temp/HouseReturnSurvey.png"};
        }
        finally{main.targetTexture=null;RenderTexture.active=old;main.transform.SetPositionAndRotation(pos,rot);main.fieldOfView=fov;UnityEngine.Object.DestroyImmediate(texture);}
    }
    public static object House()
    {
        var t=GameObject.Find("Composition_A/Houses").transform.Cast<Transform>().First(t=>t.name=="Preset_House_01_A"&&t.position.z>0&&t.position.z<20);
        return new{pos=t.position.ToString(),children=t.GetComponentsInChildren<MeshFilter>().Select(f=>new{name=f.name,local=f.transform.localPosition.ToString(),bounds=f.sharedMesh.bounds.ToString()}).ToArray()};
    }
}
