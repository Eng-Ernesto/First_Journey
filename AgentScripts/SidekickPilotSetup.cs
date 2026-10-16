using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting.YamlDotNet.Serialization;
using Synty.SidekickCharacters;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.Serialization;
using FirstJourney;

public static class SidekickPilotSetup
{
    public const string Art="Assets/_FirstJourney/Art/Characters/PilotoSidekick";
    public const string VisualPrefab="Assets/_FirstJourney/Prefabs/PilotoSidekick_Visual_v01.prefab";
    public const string GameVisualPrefab="Assets/_FirstJourney/Prefabs/PilotoSidekick_Visual_Jogo_v01.prefab";
    public const string HeroPrefab="Assets/_FirstJourney/Prefabs/HeroSidekick.prefab";
    const string Controller="Assets/_FirstJourney/Animation/AJ_Hero.controller";
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;var parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
    static SidekickRuntime Tool()
    {
        var window=Resources.FindObjectsOfTypeAll<ModularCharacterWindow>().First();
        return (SidekickRuntime)window.GetType().GetField("_sidekickRuntime",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
    }
    static Color ColorOf(string hex){Color c;if(!ColorUtility.TryParseHtmlString("#"+hex,out c))throw new Exception(hex);return c;}
    static Texture2D Readable(Texture source)
    {
        var previous=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        try{Graphics.Blit(source,rt);RenderTexture.active=rt;var texture=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,source.width,source.height),0,0);texture.Apply();return texture;}
        finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
    }
    static T SaveAsset<T>(T asset,string path) where T:UnityEngine.Object
    {
        var existing=AssetDatabase.LoadAssetAtPath<T>(path);
        if(existing){EditorUtility.CopySerialized(asset,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(asset);return existing;}
        AssetDatabase.CreateAsset(asset,path);return asset;
    }
    public static object BuildVisual()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        var source=Tool();Folder(Art+"/Textures");Folder(Art+"/Meshes");Folder(Art+"/Materials");Folder(Art+"/Definition");Folder("Assets/_FirstJourney/Review");
        var definition=new Deserializer().Deserialize<SerializedCharacter>(File.ReadAllText("Assets/Synty/SidekickCharacters/Characters/Starter/Starter_02/Starter_02.sk"));
        definition.Name="FirstJourney_Piloto_v01";
        definition.Parts.RemoveAll(p=>p.PartType.ToString().StartsWith("Attachment")||p.PartType==CharacterPartType.FacialHair||p.PartType==CharacterPartType.Wrap);
        var hair=definition.Parts.FirstOrDefault(p=>p.PartType==CharacterPartType.Hair);
        if(hair!=null)hair.Name="SK_HUMN_BASE_06_02HAIR_HU01";
        else definition.Parts.Add(new SerializedPart("SK_HUMN_BASE_06_02HAIR_HU01",CharacterPartType.Hair,"1"));
        definition.BlendShapes=new SerializedBlendShapeValues{BodyTypeValue=-100,BodySizeValue=-15,MuscleValue=-75};
        var properties=SidekickColorProperty.GetAll(source.DBManager).ToDictionary(p=>p.ID);
        foreach(var row in definition.ColorRows)
        {
            string n=properties[row.ColorProperty].Name;
            if(n.StartsWith("Hair ")||n.StartsWith("Eyebrow"))row.MainColor="58372C";
            else if(n.StartsWith("Eye Color"))row.MainColor="487A72";
            else if(n.StartsWith("Torso Outfit")||n.StartsWith("Arm Left Outfit")||n.StartsWith("Arm Right Outfit"))
                row.MainColor=n.EndsWith("01")?"C47845":n.EndsWith("02")?"E9D6AF":n.EndsWith("03")?"42666A":"6B4938";
            else if(n.StartsWith("Hips Outfit")||n.StartsWith("Leg Left Outfit")||n.StartsWith("Leg Right Outfit"))row.MainColor=n.EndsWith("01")?"344D55":"5C7777";
            else if(n.StartsWith("Feet Left Outfit")||n.StartsWith("Feet Right Outfit"))row.MainColor=n.EndsWith("01")?"674736":"342D29";
            else if(n.StartsWith("Strap "))row.MainColor="674736";
            else if(n.StartsWith("Metal ")){row.MainColor=n.EndsWith("01")?"79614B":"BBA27B";row.Metallic="300000";row.Smoothness="300000";}
            else if(n.StartsWith("Glow ")||n.StartsWith("Digital Screen ")){row.MainColor="D8B878";row.Emission="000000";}
            else if(n.StartsWith("Plastic "))row.MainColor=n.EndsWith("01")?"6B4938":"B8A17E";
        }
        var material=UnityEngine.Object.Instantiate(source.CurrentMaterial);material.name="M_Piloto_Retro_v01";
        var runtime=new SidekickRuntime(source.BaseModel,material,AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller),source.DBManager);
        var maps=new[]{"Color","Metallic","Smoothness","Reflection","Emission","Opacity"};
        var textures=new Dictionary<string,Texture2D>();
        foreach(string map in maps){var original=material.GetTexture("_"+map+"Map");if(!original)throw new Exception("Missing map "+map);var copy=Readable(original);textures.Add(map,copy);material.SetTexture("_"+map+"Map",copy);}
        var colorSet=definition.ColorSet.CreateSidekickColorSet(source.DBManager);
        foreach(var row in definition.ColorRows)
        {
            var value=row.CreateSidekickColorRow(source.DBManager,colorSet);
            foreach(ColorType type in Enum.GetValues(typeof(ColorType)))runtime.UpdateColor(type,value);
        }
        foreach(string map in maps)
        {
            string path=Art+"/Textures/T_Piloto_"+map+"Map.png";
            File.WriteAllBytes(path,textures[map].EncodeToPNG());AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.sRGBTexture=map=="Color";importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=map=="Color";importer.SaveAndReimport();
            material.SetTexture("_"+map+"Map",AssetDatabase.LoadAssetAtPath<Texture2D>(path));UnityEngine.Object.DestroyImmediate(textures[map]);
            definition.ColorSet.GetType().GetProperty("Source"+map+"Path").SetValue(definition.ColorSet,path);
        }
        definition.ColorSet.Name=definition.Name;
        material=SaveAsset(material,Art+"/Materials/M_Piloto_Retro_v01.mat");runtime.CurrentMaterial=material;
        runtime.BodyTypeBlendValue=-100;runtime.BodySizeSkinnyBlendValue=15;runtime.BodySizeHeavyBlendValue=0;runtime.MusclesBlendValue=-75;
        var parts=definition.Parts.Select(p=>runtime.DBManager==null?null:source.MappedPartDictionary[p.PartType][p.Name].GetPartModel().GetComponentInChildren<SkinnedMeshRenderer>()).ToList();
        var preview=EditorSceneManager.NewPreviewScene();GameObject model=null;
        try
        {
            model=runtime.CreateCharacter(definition.Name,parts,true,true,null,true,true);SceneManager.MoveGameObjectToScene(model,preview);
            foreach(var mesh in model.GetComponentsInChildren<SkinnedMeshRenderer>()){mesh.sharedMesh.name="PilotoSidekick_Mesh";mesh.sharedMesh=SaveAsset(mesh.sharedMesh,Art+"/Meshes/PilotoSidekick_Mesh.asset");mesh.updateWhenOffscreen=true;}
            var animator=model.GetComponent<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);animator.applyRootMotion=false;
            if(!animator.avatar||!animator.avatar.isHuman||!animator.avatar.isValid)throw new Exception("Invalid pilot Avatar.");
            var prefab=PrefabUtility.SaveAsPrefabAsset(model,VisualPrefab);
            var window=Resources.FindObjectsOfTypeAll<ModularCharacterWindow>().First();
            window.GetType().GetMethod("ExportCharacterModel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{model,Art+"/Meshes/PilotoSidekick_v01.fbx",GameVisualPrefab,material,animator.avatar});
            var contents=PrefabUtility.LoadPrefabContents(GameVisualPrefab);
            try{var gameAnimator=contents.GetComponent<Animator>();gameAnimator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);gameAnimator.applyRootMotion=false;foreach(var skin in contents.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;PrefabUtility.SaveAsPrefabAsset(contents,GameVisualPrefab);}
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            string definitionPath=Art+"/Definition/FirstJourney_Piloto_v01.sk";File.WriteAllText(definitionPath,new Serializer().Serialize(definition));AssetDatabase.ImportAsset(definitionPath);
            AssetDatabase.SaveAssets();
            var result=new{character=definition.Name,prefab=VisualPrefab,definition=definitionPath,parts=parts.Count,vertices=model.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(m=>m.sharedMesh.vertexCount),avatar=animator.avatar.name,avatarHuman=animator.avatar.isHuman,avatarValid=animator.avatar.isValid};
            File.WriteAllText("Docs/Sidekick_Pilot_Build.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
        }
        finally{if(model)UnityEngine.Object.DestroyImmediate(model);EditorSceneManager.ClosePreviewScene(preview);}
    }
    public static object RenderPreview()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(GameVisualPrefab);if(!source)throw new Exception("Build visual first.");
        var utility=new PreviewRenderUtility();GameObject model=null;Texture2D image=null;var bakedMeshes=new List<Mesh>();
        try
        {
            model=UnityEngine.Object.Instantiate(source);utility.AddSingleGO(model);
            var animator=model.GetComponent<Animator>();animator.applyRootMotion=false;animator.Rebind();animator.SetBool("Grounded",true);animator.Play("Locomotion",0,0);animator.Update(0);
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var baked=new Mesh();skin.BakeMesh(baked);bakedMeshes.Add(baked);
                var go=new GameObject("Preview_BakedMesh");go.transform.SetParent(skin.transform,false);go.AddComponent<MeshFilter>().sharedMesh=baked;go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
            }
            Bounds bounds=new Bounds();bool first=true;foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>()){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
            float h=bounds.size.y;utility.camera.fieldOfView=32;utility.camera.nearClipPlane=.01f;utility.camera.farClipPlane=100;
            utility.camera.transform.position=bounds.center+new Vector3(.65f,.18f,1)*h*2.25f;utility.camera.transform.LookAt(bounds.center);
            utility.camera.backgroundColor=new Color(.19f,.25f,.27f,1);utility.camera.clearFlags=CameraClearFlags.SolidColor;
            utility.lights[0].intensity=1.3f;utility.lights[0].transform.rotation=Quaternion.Euler(40,35,0);utility.lights[1].intensity=.8f;utility.lights[1].transform.rotation=Quaternion.Euler(15,210,0);utility.ambientColor=new Color(.55f,.55f,.55f);
            utility.BeginStaticPreview(new Rect(0,0,1100,1100));utility.Render(true,false);image=utility.EndStaticPreview();
            const string path="Assets/_FirstJourney/Review/PilotoSidekick_Preview_v01.png";File.WriteAllBytes(path,image.EncodeToPNG());AssetDatabase.ImportAsset(path);return new{path,height=h};
        }
        finally{utility.Cleanup();foreach(var baked in bakedMeshes)UnityEngine.Object.DestroyImmediate(baked);if(image)UnityEngine.Object.DestroyImmediate(image);}
    }
    public static object Install()
    {
        const string scenePath="Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity";
        const string backup="Assets/_FirstJourney/Scenes/Versions/FirstJourney_LuaBase_AntesSidekick.unity";
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!=scenePath)throw new Exception("Open the base scene outside Play Mode.");
        if(GameObject.Find("HeroSidekick"))throw new Exception("Sidekick is already installed.");
        var old=GameObject.Find("HeroAJ");if(!old)throw new Exception("Expected the existing AJ actor.");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(GameVisualPrefab);if(!source)throw new Exception("Build the pilot first.");
        if(!File.Exists(backup))EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),backup,true);
        int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Substituir AJ pelo piloto Sidekick");
        var owner=UnityEngine.Object.Instantiate(old);owner.name="HeroSidekick";Undo.RegisterCreatedObjectUndo(owner,"Criar piloto Sidekick");
        if(PrefabUtility.IsPartOfPrefabInstance(owner))PrefabUtility.UnpackPrefabInstance(owner,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        foreach(Transform child in owner.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,owner.transform);visual.name="PilotoSidekick_Visual";
        visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one;
        var animator=visual.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
        Bounds bounds=new Bounds();bool first=true;
        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh=new Mesh();skin.BakeMesh(mesh);
            foreach(var vertex in mesh.vertices){var point=owner.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex));if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
            UnityEngine.Object.DestroyImmediate(mesh);skin.updateWhenOffscreen=true;
        }
        float scale=1.72f/bounds.size.y;visual.transform.localScale=Vector3.one*scale;visual.transform.localPosition=Vector3.down*(bounds.min.y*scale);
        var motor=owner.GetComponent<FJHeroMotor>();motor.animator=animator;motor.movementCamera=Camera.main;motor.spawnPosition=owner.transform.position;motor.ClearTestInput();
        var orbit=Camera.main.GetComponent<FJOrbitCamera>();Undo.RecordObject(orbit,"Seguir piloto Sidekick");orbit.target=owner.transform;orbit.Snap();
        PrefabUtility.SaveAsPrefabAssetAndConnect(owner,HeroPrefab,InteractionMode.AutomatedAction);
        Undo.DestroyObjectImmediate(old);Undo.CollapseUndoOperations(group);
        Selection.activeGameObject=owner;EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        var result=new{character="Piloto Sidekick v01",root=owner.name,prefab=HeroPrefab,backup,height=owner.GetComponent<CharacterController>().height,visualScale=scale,avatarHuman=animator.avatar.isHuman,avatarValid=animator.avatar.isValid,cameraFollow=orbit.target==owner.transform,activeMotors=UnityEngine.Object.FindObjectsByType<FJHeroMotor>(FindObjectsSortMode.None).Length,baseRenderers=GameObject.Find("LuaBase_ArteOriginal").GetComponentsInChildren<Renderer>().Length,sceneSaved=!SceneManager.GetActiveScene().isDirty};
        File.WriteAllText("Docs/Sidekick_Installation.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
    public static object RefreshInstalledVisual()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        var owner=GameObject.Find("HeroSidekick");if(!owner)throw new Exception("Install Sidekick first.");
        foreach(Transform child in owner.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(GameVisualPrefab);
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,owner.transform);visual.name="PilotoSidekick_Visual";
        visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one;
        var animator=visual.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
        Bounds bounds=new Bounds();bool first=true;
        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh=new Mesh();skin.BakeMesh(mesh);foreach(var v in mesh.vertices){var p=owner.transform.InverseTransformPoint(skin.transform.TransformPoint(v));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}UnityEngine.Object.DestroyImmediate(mesh);skin.updateWhenOffscreen=true;
        }
        float scale=1.72f/bounds.size.y;visual.transform.localScale=Vector3.one*scale;visual.transform.localPosition=Vector3.down*(bounds.min.y*scale);
        var motor=owner.GetComponent<FJHeroMotor>();motor.animator=animator;motor.ClearTestInput();EditorUtility.SetDirty(motor);
        PrefabUtility.SaveAsPrefabAssetAndConnect(owner,HeroPrefab,InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        var result=new{character="Piloto Sidekick v01",visual=GameVisualPrefab,fbx=Art+"/Meshes/PilotoSidekick_v01.fbx",avatar=animator.avatar.name,avatarHuman=animator.avatar.isHuman,avatarValid=animator.avatar.isValid,visualScale=scale,sceneSaved=!SceneManager.GetActiveScene().isDirty};
        File.WriteAllText("Docs/Sidekick_GameAsset.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
}
