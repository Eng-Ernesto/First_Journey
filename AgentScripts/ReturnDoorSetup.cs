using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FirstJourney;
public static class ReturnDoorSetup
{
    const string ScenePath="Assets/_FirstJourney/Scenes/FirstJourney_Reino01_FantasyKingdom.unity";
    public static object PersistArrival()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        var motor=GameObject.Find("HeroSidekick").GetComponent<FJHeroMotor>();
        motor.spawnPosition=GameObject.Find("Chegada_No_Reino").transform.position;
        motor.fallResetHeight=-100;motor.movementCamera=Camera.main;
        PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
        EditorSceneManager.MarkSceneDirty(motor.gameObject.scene);EditorSceneManager.SaveScene(motor.gameObject.scene);
        return new{spawn=motor.spawnPosition.ToString(),fallResetHeight=motor.fallResetHeight};
    }
    public static object RefineOpening()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        foreach(var d in UnityEngine.Object.FindObjectsByType<FJWorldDoor>(FindObjectsSortMode.None))
        {d.openingAngle=85;if(d.openDoorSurface){var p=d.openDoorSurface.transform.localPosition;p.z=.18f;d.openDoorSurface.transform.localPosition=p;}}
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return "Saved outward opening on three house doors";
    }
    public static object Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open the playable world");
        foreach(var name in new[]{"Porta_Regresso_Asteroide01","Luz_Porta_Do_Regresso"}){var g=GameObject.Find(name);if(g)UnityEngine.Object.DestroyImmediate(g);}
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FirstJourney/Prefabs/BaseMoon.prefab");
        var parts=source.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("ANG_Porta_")).ToArray();
        const string folder="Assets/_FirstJourney/Art/Worlds/Reino01";
        string glowPath=folder+"/Porta_Asteroide_Luz.asset";
        var glow=AssetDatabase.LoadAssetAtPath<Material>(glowPath);
        if(!glow){glow=new Material(Shader.Find("Universal Render Pipeline/Unlit"));glow.SetColor("_BaseColor",new Color(1,.55f,.12f));AssetDatabase.CreateAsset(glow,glowPath);}
        var results=new System.Collections.Generic.List<object>();
        for(int i=0;i<3;i++)
        {
            string name="Porta_Asteroide_Regiao_"+(i+1);if(GameObject.Find(name))throw new Exception("Door already exists: "+name);
            var town=GameObject.Find(new[]{"Composition_A","Composition_B","Composition_C"}[i]).transform;
            var root=new GameObject(name);root.transform.SetPositionAndRotation(town.TransformPoint(new Vector3(5.56f,1.08f,17.2f)),Quaternion.LookRotation(town.TransformVector(Vector3.right).normalized));root.transform.localScale=new Vector3(1.1f,1.18f,1);
            var leaf=new GameObject("Folha_Abre").transform;leaf.SetParent(root.transform,false);leaf.localPosition=new Vector3(.575f,0,0);
            foreach(var p in parts)
            {
                bool moving=p.name.Contains("Tabua")||p.name.Contains("Trava")||p.name.Contains("Puxador");
                var g=new GameObject(p.name);g.transform.SetParent(moving?leaf:root.transform,false);
                g.transform.localPosition=p.transform.position-new Vector3(.825f,.55f,2.70f)-(moving?leaf.localPosition:Vector3.zero);
                g.transform.localRotation=p.transform.rotation;g.transform.localScale=p.transform.lossyScale;
                g.AddComponent<MeshFilter>().sharedMesh=p.sharedMesh;g.AddComponent<MeshRenderer>().sharedMaterials=p.GetComponent<MeshRenderer>().sharedMaterials;
            }
            var collider=leaf.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(-.55f,.96f,-.04f);collider.size=new Vector3(1.1f,1.9f,.12f);
            var surface=GameObject.CreatePrimitive(PrimitiveType.Cube);surface.name="Passagem_Luminosa";surface.transform.SetParent(root.transform,false);surface.transform.localPosition=new Vector3(0,.96f,-.10f);surface.transform.localScale=new Vector3(1.1f,1.91f,.015f);UnityEngine.Object.DestroyImmediate(surface.GetComponent<Collider>());surface.GetComponent<Renderer>().sharedMaterial=glow;surface.SetActive(false);
            var plaque=GameObject.CreatePrimitive(PrimitiveType.Cube);plaque.name="Placa_ASTEROIDE";plaque.transform.SetParent(root.transform,false);plaque.transform.localPosition=new Vector3(0,2.32f,.02f);plaque.transform.localScale=new Vector3(1.7f,.42f,.07f);UnityEngine.Object.DestroyImmediate(plaque.GetComponent<Collider>());plaque.GetComponent<Renderer>().sharedMaterial=parts[0].GetComponent<Renderer>().sharedMaterial;
            var label=new GameObject("Texto_Regresso").AddComponent<TextMesh>();label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,2.32f,.065f);label.transform.localRotation=Quaternion.Euler(0,180,0);label.text="ASTEROIDE";label.fontSize=48;label.characterSize=.07f;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(1,.92f,.66f);
            var door=root.AddComponent<FJWorldDoor>();door.destinationScene="Assets/_FirstJourney/Scenes/FirstJourney_Asteroide01.unity";door.arrivalMarker="Regresso_Do_Reino";door.actionLabel="Abrir porta · voltar ao asteroide";door.interactionRadius=3.3f;door.arrivalCameraYaw=180;door.doorLeaf=leaf;door.openDoorSurface=surface;door.showWorldMarker=true;door.markerLabel="CASA DE REGRESSO · ASTEROIDE";
            var lamp=new GameObject("Luz_da_Porta").AddComponent<Light>();lamp.transform.SetParent(root.transform,false);lamp.transform.localPosition=new Vector3(0,2.5f,.4f);lamp.type=LightType.Point;lamp.range=3;lamp.intensity=1.2f;lamp.color=new Color(1,.7f,.3f);
            var approach=new GameObject("Aproximacao_Porta_Regiao_"+(i+1));approach.transform.position=town.TransformPoint(new Vector3(8.3f,.4f,17.2f));approach.transform.rotation=Quaternion.LookRotation(-town.TransformVector(Vector3.right));
            results.Add(new{region=i+1,door=root.transform.position.ToString("F3"),approach=approach.transform.position.ToString("F3"),leafParts=leaf.childCount,destination=door.destinationScene});
        }
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        var report=new{sameWorld=true,regions=3,portalsBetweenRegions=false,doors=results};File.WriteAllText("Docs/Portas_Regresso_Construcao.json",JsonConvert.SerializeObject(report,Formatting.Indented));return report;
    }
}
