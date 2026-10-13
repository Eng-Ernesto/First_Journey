using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FirstJourney;

public static class AJHeroSetup
{
    const string Art="Assets/_FirstJourney/Art/Characters/HeroAJ";
    const string Model=Art+"/Source/AJ_Model.fbx";
    const string ControllerPath="Assets/_FirstJourney/Animation/AJ_Hero.controller";
    const string PrefabPath="Assets/_FirstJourney/Prefabs/HeroAJ.prefab";
    const string ScenePath="Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity";
    static Avatar HeroAvatar() { return AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().FirstOrDefault(a=>a.isHuman && a.isValid); }
    static JObject Manifest() { return JObject.Parse(File.ReadAllText(@"C:\Ernesto\unity\assets\Characters\HeroAJ\mixamo_selection_v01.json")); }
    static AnimationClip Clip(string state) { return AssetDatabase.LoadAllAssetsAtPath(Art+"/Animations/AJ_"+state+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__")); }

    public static object ConfigureAnimations()
    {
        Avatar avatar=HeroAvatar();
        if(!avatar)throw new Exception("The rigged AJ model must have a valid Humanoid Avatar.");
        var results=new List<object>();
        foreach(var entry in Manifest()["entries"])
        {
            string state=(string)entry["state"];
            string path=Art+"/Animations/AJ_"+state+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar=avatar;
            importer.importAnimation=true;
            importer.animationCompression=ModelImporterAnimationCompression.Off;
            var definitions=importer.defaultClipAnimations;
            if(definitions.Length!=1)throw new Exception("Expected a single animation in "+path);
            var definition=definitions[0]; definition.name="AJ_"+state;
            definition.loopTime=(bool)entry["loop_recommended"];
            definition.loopPose=definition.loopTime;
            definition.lockRootRotation=true;
            definition.keepOriginalOrientation=false;
            definition.lockRootHeightY=true;
            definition.keepOriginalPositionY=false;
            definition.heightFromFeet=true;
            definition.lockRootPositionXZ=true;
            definition.keepOriginalPositionXZ=false;
            importer.clipAnimations=definitions;
            importer.SaveAndReimport();
            var clip=Clip(state);
            if(!clip.isHumanMotion || Math.Abs(clip.frameRate-60)>.01f || clip.length<=0)throw new Exception("Invalid Humanoid clip "+state);
            results.Add(new {state,clip=clip.name,seconds=clip.length,fps=clip.frameRate,humanoid=clip.isHumanMotion,loop=definition.loopTime,avatar=avatar.name});
        }
        var report=new{character="AJ",avatarValid=avatar.isValid,avatarHuman=avatar.isHuman,clips=results.Count,entries=results};
        File.WriteAllText("Docs/HeroAJ_Humanoid.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        AssetDatabase.SaveAssets(); return report;
    }
    public static object BuildAnimator()
    {
        Folder("Assets/_FirstJourney/Animation");
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)) return new {existing=true,path=ControllerPath};
        var controller=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
        controller.AddParameter("VerticalSpeed",AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded",AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump",AnimatorControllerParameterType.Trigger);
        var parameters=controller.parameters; parameters.First(p=>p.name=="Grounded").defaultBool=true; controller.parameters=parameters;
        var machine=controller.layers[0].stateMachine;
        var locomotion=machine.AddState("Locomotion",new Vector3(250,60)); machine.defaultState=locomotion;
        var tree=new BlendTree{name="AJ_Locomotion",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
        AssetDatabase.AddObjectToAsset(tree,controller);
        tree.AddChild(Clip("Idle"),0); tree.AddChild(Clip("Walk"),1.9f); tree.AddChild(Clip("Run"),3.8f); locomotion.motion=tree;
        var jump=machine.AddState("Jump",new Vector3(500,20));jump.motion=Clip("Jump");jump.speed=2.25f;
        var fall=machine.AddState("Fall",new Vector3(740,80));fall.motion=Clip("Fall");
        var land=machine.AddState("Land",new Vector3(500,180));land.motion=Clip("Land");land.speed=1.35f;
        Transition(locomotion,jump).AddCondition(AnimatorConditionMode.If,0,"Jump");
        var falling=Transition(locomotion,fall);falling.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");falling.AddCondition(AnimatorConditionMode.Less,-.2f,"VerticalSpeed");
        var apex=Transition(jump,fall);apex.AddCondition(AnimatorConditionMode.Less,-.15f,"VerticalSpeed");apex.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
        Transition(jump,land).AddCondition(AnimatorConditionMode.If,0,"Grounded");
        Transition(fall,land).AddCondition(AnimatorConditionMode.If,0,"Grounded");
        Exit(land,locomotion,.8f);
        Transition(land,jump).AddCondition(AnimatorConditionMode.If,0,"Jump");
        var actions=new Dictionary<string,AnimatorState>(); int index=0;
        foreach(var entry in Manifest()["entries"].Where(e=>(string)e["group"]!="Movimento"))
        {
            string name=(string)entry["state"];
            var state=machine.AddState(name,new Vector3(200+(index%4)*240,360+(index/4)*100));index++;
            state.motion=Clip(name);state.tag="Interaction";actions.Add(name,state);
        }
        Exit(actions["PickUp"],locomotion,.95f);
        Exit(actions["MineSwing"],actions["ToolIdle"],.95f);
        Exit(actions["SitDown"],actions["PilotIdle"],.96f);
        Exit(actions["PilotSwitch"],actions["PilotIdle"],.96f);
        Exit(actions["StandUpFromSeat"],locomotion,.96f);
        Exit(actions["KneelDown"],actions["KneelIdle"],.96f);
        Exit(actions["KneelUp"],locomotion,.96f);
        Exit(actions["PetDog"],locomotion,.96f);
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        return new{path=ControllerPath,states=machine.states.Length,clips=17,blendTree="Idle / Walk / Run",rootMotion=false};
    }
    public static object PlaceHero()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before scene authoring.");
        if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Unexpected active scene.");
        if(GameObject.Find("HeroAJ"))throw new Exception("AJ already exists in the scene; refusing to duplicate it.");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        if(!source || source.GetComponentsInChildren<SkinnedMeshRenderer>().Length==0 || !HeroAvatar())throw new Exception("A skinned AJ model is required.");
        const string backup="Assets/_FirstJourney/Scenes/Versions/FirstJourney_LuaBase_AntesHeroiAJ.unity";
        if(!File.Exists(backup))EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),backup,true);
        var owner=new GameObject("HeroAJ");
        owner.transform.position=new Vector3(.2f,.30f,6.7f); owner.transform.rotation=Quaternion.Euler(0,90,0);
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(source);
        visual.name="AJ_Visual";visual.transform.SetParent(owner.transform,false);
        var animator=visual.GetComponent<Animator>(); animator.avatar=HeroAvatar();
        animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
        Bounds geometry=new Bounds();bool first=true;
        foreach(var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh=new Mesh();renderer.BakeMesh(mesh);
            foreach(var vertex in mesh.vertices)
            {
                Vector3 local=owner.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                if(first){geometry=new Bounds(local,Vector3.zero);first=false;}else geometry.Encapsulate(local);
            }
            UnityEngine.Object.DestroyImmediate(mesh);
            renderer.updateWhenOffscreen=true;
            if(renderer.sharedMaterials.Any(m=>!m || m.shader.name!="Universal Render Pipeline/Lit" || !m.GetTexture("_BaseMap"))) throw new Exception("AJ material/texture missing on "+renderer.name);
        }
        float scale=1.72f/geometry.size.y;
        visual.transform.localScale=Vector3.one*scale;visual.transform.localPosition=Vector3.down*(geometry.min.y*scale);
        var body=owner.AddComponent<CharacterController>();body.height=1.72f;body.radius=.28f;body.center=new Vector3(0,.86f,0);body.skinWidth=.025f;body.stepOffset=.28f;body.slopeLimit=50;body.minMoveDistance=0;
        var motor=owner.AddComponent<FJHeroMotor>();motor.animator=animator;motor.movementCamera=Camera.main;motor.spawnPosition=owner.transform.position;
        var camera=Camera.main;
        var orbit=camera.GetComponent<FJOrbitCamera>();if(!orbit)orbit=camera.gameObject.AddComponent<FJOrbitCamera>();
        orbit.target=owner.transform;camera.fieldOfView=52;camera.nearClipPlane=.08f;camera.farClipPlane=250;
        var aim=owner.transform.position+Vector3.up*orbit.followHeight;
        camera.transform.position=aim+Quaternion.Euler(orbit.pitch,orbit.yaw,0)*Vector3.back*orbit.distance;camera.transform.LookAt(aim);
        Folder("Assets/_FirstJourney/Prefabs");PrefabUtility.SaveAsPrefabAssetAndConnect(owner,PrefabPath,InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Selection.activeGameObject=owner;
        var result=new{scene=ScenePath,prefab=PrefabPath,source=Model,character="AJ",height=body.height,scale,skinnedMeshes=visual.GetComponentsInChildren<SkinnedMeshRenderer>().Length,avatarValid=animator.avatar.isValid,controller=ControllerPath,backup,baseRenderers=GameObject.Find("LuaBase_ArteOriginal").GetComponentsInChildren<Renderer>().Length};
        File.WriteAllText("Docs/HeroAJ_Installation.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
    static AnimatorStateTransition Transition(AnimatorState a,AnimatorState b)
    {var transition=a.AddTransition(b);transition.hasExitTime=false;transition.hasFixedDuration=true;transition.duration=.12f;return transition;}
    static void Exit(AnimatorState a,AnimatorState b,float time) {var t=Transition(a,b);t.hasExitTime=true;t.exitTime=time;}
    static void Folder(string path)
    {if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
    public static object FixFacialMaterials()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before material remapping.");
        Folder(Art+"/Materials");
        var textureImporter=(TextureImporter)AssetImporter.GetAtPath(Art+"/Textures/Boy01_FacialAnimMap.png");
        textureImporter.alphaIsTransparency=true;textureImporter.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        var importer=(ModelImporter)AssetImporter.GetAtPath(Model);
        var materials=model.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Where(m=>m.name!="Boy01_Body_MAT1").ToArray();
        foreach(var original in materials)
        {
            string path=Art+"/Materials/"+original.name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(original);material.name=original.name;AssetDatabase.CreateAsset(material,path);}
            material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.25f);material.SetFloat("_Surface",0);material.SetFloat("_ZWrite",1);
            material.EnableKeyword("_ALPHATEST_ON");material.SetOverrideTag("RenderType","TransparentCutout");material.renderQueue=2450;
            EditorUtility.SetDirty(material);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),original.name),material);
        }
        importer.SaveAndReimport();AssetDatabase.SaveAssets();
        var actor=GameObject.Find("HeroAJ");
        PrefabUtility.ApplyPrefabInstance(actor,InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        return new{facialMaterials=materials.Length,alphaClip=true,cutoff=.25f,atlasHasTransparency=true};
    }
    public static object ValidateInstalled()
    {
        var owner=GameObject.Find("HeroAJ");var motor=owner.GetComponent<FJHeroMotor>();var animator=motor.animator;
        var clips=Manifest()["entries"].Select(e=>Clip((string)e["state"])).ToArray();
        var materials=owner.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        var report=new{character="AJ",avatarValid=animator.avatar.isValid,avatarHuman=animator.avatar.isHuman,
            humanClips=clips.Count(c=>c.isHumanMotion),clips=clips.Length,
            missingMaterials=materials.Count(m=>!m || !m.GetTexture("_BaseMap")),
            facialTransparency=materials.Where(m=>m.name!="Boy01_Body_MAT1").All(m=>m.GetFloat("_AlphaClip")==1),
            characterHeight=owner.GetComponent<CharacterController>().height,cameraFollow=Camera.main.GetComponent<FJOrbitCamera>().target==owner.transform,
            baseRenderers=GameObject.Find("LuaBase_ArteOriginal").GetComponentsInChildren<Renderer>().Length,
            scene=SceneManager.GetActiveScene().path,sceneSaved=!SceneManager.GetActiveScene().isDirty};
        if(!report.avatarValid || !report.avatarHuman || report.humanClips!=17 || report.missingMaterials!=0 || !report.facialTransparency || !report.cameraFollow || report.baseRenderers!=584)throw new Exception(JsonConvert.SerializeObject(report));
        File.WriteAllText("Docs/HeroAJ_Final_Validation.json",JsonConvert.SerializeObject(report,Formatting.Indented));return report;
    }
}
