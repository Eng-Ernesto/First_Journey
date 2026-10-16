using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FirstJourney;
public static class AircraftSetup
{
    static Transform Marker(string name,Transform parent,Vector3 local)
    {
        var marker=parent.Find(name);
        if(!marker){marker=new GameObject(name).transform;marker.SetParent(parent,false);}
        marker.localPosition=local;marker.localRotation=Quaternion.identity;return marker;
    }
    static FJLandingSite Site(Scene scene,string name,string label,Vector3 position,float yaw)
    {
        var obj=scene.GetRootGameObjects().FirstOrDefault(r=>r.name==name);
        if(!obj){obj=new GameObject(name);SceneManager.MoveGameObjectToScene(obj,scene);}
        obj.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        var site=obj.GetComponent<FJLandingSite>()??obj.AddComponent<FJLandingSite>();site.displayName=label;
        site.exitPoint=Marker("Desembarque_Seguro",obj.transform,new Vector3(-2.65f,.04f,-1.4f));
        site.exitPoint.localRotation=Quaternion.Euler(0,90,0);site.approachHeight=10;
        EditorUtility.SetDirty(site);return site;
    }
    public static object Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        EditorSceneManager.OpenScene(FJWorldSession.ExteriorPath,OpenSceneMode.Single);
        var main=SceneManager.GetActiveScene();
        var baseScene=EditorSceneManager.OpenScene(FJWorldSession.BasePath,OpenSceneMode.Additive);
        var asteroid=EditorSceneManager.OpenScene(FJWorldSession.AsteroidPath,OpenSceneMode.Additive);SceneManager.SetActiveScene(main);
        var plane=GameObject.Find("Aviao_Original_SemPersonagem").transform;
        var source=Site(baseScene,"Aterragem_LuaBase","Lua-base",plane.position,plane.eulerAngles.y);
        var target=Site(asteroid,"Aterragem_Asteroide01","Asteroide 01",new Vector3(3.3f,.025f,183.1f),90);
        var aircraft=plane.GetComponent<FJAircraftController>()??plane.gameObject.AddComponent<FJAircraftController>();
        aircraft.startingSite=source;
        aircraft.seat=Marker("Piloto_Assento",plane,new Vector3(0,1.70f,-.83f));
        aircraft.boardingPoint=Marker("Embarque",plane,new Vector3(-2.65f,.04f,-1.4f));
        aircraft.propeller=Marker("Helice_Animada",plane,new Vector3(0,1.58f,2.8f));
        aircraft.propellerParts=plane.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="aviao_aviador_Helice_Inferior"||t.name=="aviao_aviador_Helice_Superior"||t.name=="aviao_aviador_Cubo_Helice").ToArray();
        foreach(var t in plane.GetComponentsInChildren<Transform>(true)){GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
        var oldMarker=GameObject.Find("Ponto_Interacao_Aviao");if(oldMarker)oldMarker.transform.position=aircraft.boardingPoint.position;
        var session=UnityEngine.Object.FindFirstObjectByType<FJWorldSession>();
        var chase=session.playerCamera.GetComponent<FJAircraftCamera>()??session.playerCamera.gameObject.AddComponent<FJAircraftCamera>();chase.enabled=false;
        EditorUtility.SetDirty(aircraft);PrefabUtility.RecordPrefabInstancePropertyModifications(aircraft);
        Physics.SyncTransforms();
        var originalPos=plane.position;var originalRot=plane.rotation;
        plane.SetPositionAndRotation(target.transform.position,target.transform.rotation);Physics.SyncTransforms();
        var supports=new[]{new Vector3(-1.13f,.43f,1.1f),new Vector3(1.13f,.43f,1.1f)}.Select(p=>{
            var world=plane.TransformPoint(p);var hit=Physics.RaycastAll(world,Vector3.down,2).Where(h=>!h.collider.transform.IsChildOf(plane)).OrderBy(h=>h.distance).First();
            return new {wheel=world.ToString("F3"),ground=hit.point.ToString("F3"),name=hit.collider.name,gap=world.y-.43f-hit.point.y};}).ToArray();
        var collisions=plane.GetComponentsInChildren<MeshCollider>().Where(c=>c.enabled).SelectMany(c=>Physics.OverlapBox(c.bounds.center,c.bounds.extents*.98f,Quaternion.identity).Where(other=>!other.transform.IsChildOf(plane)&&other.name!="Rocha_Asteroide01").Select(other=>new {part=c.name,other=other.name})).ToArray();
        var exit=target.exitPoint.position;
        var exitHits=Physics.RaycastAll(exit+Vector3.up*.7f,Vector3.down,2).Where(h=>!h.collider.transform.IsChildOf(plane)).OrderBy(h=>h.distance).ToArray();
        plane.SetPositionAndRotation(originalPos,originalRot);Physics.SyncTransforms();
        foreach(var scene in new[]{main,baseScene,asteroid}){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        AssetDatabase.SaveAssets();Selection.activeGameObject=plane.gameObject;
        var result=new {installed=true,originalPlaneParts=plane.GetComponentsInChildren<Renderer>().Length,source=source.transform.position.ToString("F3"),destination=target.transform.position.ToString("F3"),destinationYaw=target.transform.eulerAngles.y,destinationWheelSupport=supports,destinationPotentialBoundsOverlaps=collisions,exit=exit.ToString("F3"),exitGround=exitHits.FirstOrDefault().point.ToString("F3"),singleCamera=session.playerCamera.name};
        File.WriteAllText("Docs/Aviao_Instalacao.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
}
