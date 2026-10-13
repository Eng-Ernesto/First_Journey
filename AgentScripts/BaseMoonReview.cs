using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class BaseMoonReview
{
    public static object OrientCamera()
    {
        var root=GameObject.Find("LuaBase_ArteOriginal");
        var camera=GameObject.Find("Main Camera").GetComponent<Camera>();
        camera.transform.position=new Vector3(-12.226f,17.797f,24.775f);
        camera.transform.LookAt(new Vector3(0,2.8f,0));camera.fieldOfView=43;
        var key=GameObject.Find("Sun").GetComponent<Light>();key.transform.rotation=Quaternion.Euler(45,145,0);key.intensity=1.15f;
        EditorSceneManager.SaveScene(camera.gameObject.scene);
        if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(0,2.8f,0),camera.transform.rotation,22,false);
        return root.GetComponentsInChildren<Renderer>().Where(r=>r.name.Contains("Porta")||r.name.Contains("Parede_Frente")).Select(r=>new {name=r.name,center=r.bounds.center.ToString()}).ToArray();
    }
}

