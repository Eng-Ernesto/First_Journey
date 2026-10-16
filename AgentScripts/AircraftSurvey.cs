using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using FirstJourney;
public static class AircraftSurvey
{
    public static object Inspect()
    {
        var plane=Resources.FindObjectsOfTypeAll<Transform>().First(t=>t.name=="Aviao_Original_SemPersonagem"&&t.gameObject.scene.IsValid());
        var a=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_FirstJourney/Animation/AJ_Hero.controller");
        var report=new {
            playing=Application.isPlaying,
            scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new {path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}),
            plane=new {pos=plane.position.ToString("F3"),rot=plane.eulerAngles.ToString("F2"),scale=plane.lossyScale.ToString("F3"),parent=plane.parent.name,
                parts=plane.GetComponentsInChildren<Renderer>(true).Select(r=>new {name=r.name,pos=r.transform.position.ToString("F3"),bounds=r.bounds.ToString("F2")}),
                colliders=plane.GetComponentsInChildren<Collider>(true).Select(c=>new {name=c.name,type=c.GetType().Name,enabled=c.enabled,bounds=c.bounds.ToString("F2")})},
            states=a.layers[0].stateMachine.states.Select(s=>new {name=s.state.name,tag=s.state.tag,motion=s.state.motion?s.state.motion.name:null}),
            markers=Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene.IsValid()&&(t.name.Contains("Aviao")||t.name.Contains("Regresso")||t.name.Contains("Asteroide01_Exterior"))).Select(t=>new {name=t.name,pos=t.position.ToString("F3"),rot=t.eulerAngles.ToString("F2"),scene=t.gameObject.scene.name})
        };
        File.WriteAllText("Docs/Aviao_Inspecao.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return report;
    }
}
