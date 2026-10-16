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
using Synty.SidekickCharacters;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.Database.DTO;

public static class SidekickAccessProbe
{
    public static object InspectAndTest()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        var scene=SceneManager.GetActiveScene();
        bool dirtyBefore=scene.isDirty;
        var rootsBefore=scene.GetRootGameObjects().Select(x=>x.GetInstanceID()).OrderBy(x=>x).ToArray();
        var window=Resources.FindObjectsOfTypeAll<ModularCharacterWindow>().First();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var runtime=(SidekickRuntime)window.GetType().GetField("_sidekickRuntime",flags).GetValue(window);
        var sourceParts=(Dictionary<CharacterPartType,SkinnedMeshRenderer>)window.GetType().GetField("_partDictionary",flags).GetValue(window);
        var parts=sourceParts.Values.Where(x=>x!=null).ToList();
        if(parts.Count==0)
            parts=runtime.MappedPartDictionary.Where(g=>!g.Key.ToString().StartsWith("Attachment")&&g.Key!=CharacterPartType.Wrap)
                .Select(g=>g.Value.Values.First().GetPartModel()).Where(g=>g!=null)
                .Select(g=>g.GetComponentInChildren<SkinnedMeshRenderer>()).Where(m=>m!=null).ToList();
        if(parts.Count==0)throw new Exception("No selected parts to test.");
        var preview=EditorSceneManager.NewPreviewScene();
        Material material=null;Texture2D testTexture=null;GameObject model=null;
        var generatedMeshes=new List<Mesh>();
        object testResults=null;
        try
        {
            material=UnityEngine.Object.Instantiate(runtime.CurrentMaterial);
            var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_FirstJourney/Animation/AJ_Hero.controller");
            var isolated=new SidekickRuntime(runtime.BaseModel,material,controller,runtime.DBManager);
            isolated.MusclesBlendValue=-20;
            model=isolated.CreateCharacter("__Sidekick_AccessProbe",parts,true,true,null,true,true);
            SceneManager.MoveGameObjectToScene(model,preview);
            var meshes=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            generatedMeshes.AddRange(meshes.Select(x=>x.sharedMesh).Distinct());
            var mesh=meshes.First();
            int muscle=Enumerable.Range(0,mesh.sharedMesh.blendShapeCount).First(i=>mesh.sharedMesh.GetBlendShapeName(i).Contains("defaultBuff"));
            float bodyBefore=mesh.GetBlendShapeWeight(muscle);
            isolated.MusclesBlendValue=60;isolated.UpdateBlendShapes(model);
            float bodyAfter=mesh.GetBlendShapeWeight(muscle);
            int jaw=Enumerable.Range(0,mesh.sharedMesh.blendShapeCount).Where(i=>mesh.sharedMesh.GetBlendShapeName(i).EndsWith(".jawOpen")).DefaultIfEmpty(-1).First();
            bool facialChanged=false;
            if(jaw>=0){mesh.SetBlendShapeWeight(jaw,75);facialChanged=Mathf.Abs(mesh.GetBlendShapeWeight(jaw)-75)<.01f;}
            testTexture=new Texture2D(2,2);
            isolated.UpdateTexture(testTexture,new Color(.15f,.35f,.55f,1),0,0);
            var pixel=testTexture.GetPixel(0,0);
            bool colorChanged=Mathf.Abs(pixel.r-.15f)<.01f && Mathf.Abs(pixel.g-.35f)<.01f;
            var animator=model.GetComponent<Animator>();
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController=controller;animator.Rebind();
            animator.SetBool("Grounded",true);animator.SetFloat("Speed",3.8f);
            animator.Play("Locomotion",0,0);animator.Update(0);
            var leg=animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var initial=leg.localRotation;float maxChange=0;
            for(int i=0;i<15;i++){animator.Update(1f/60f);maxChange=Mathf.Max(maxChange,Quaternion.Angle(initial,leg.localRotation));}
            testResults=new{
                apiCreatedCharacter=true,sourceParts=parts.Count,combinedRenderers=meshes.Length,
                vertices=meshes.Sum(x=>x.sharedMesh.vertexCount),blendShapes=mesh.sharedMesh.blendShapeCount,
                avatarHuman=animator.avatar.isHuman,avatarValid=animator.avatar.isValid,
                bodyWeightBefore=bodyBefore,bodyWeightAfter=bodyAfter,bodyChanged=bodyAfter>bodyBefore,
                facialChanged,colorChanged,shader=material.shader.name,shaderSupported=material.shader.isSupported,
                ajControllerAssigned=animator.runtimeAnimatorController==controller,retargetLegRotationDegrees=maxChange,
                passed=animator.avatar.isValid&&animator.avatar.isHuman&&bodyAfter>bodyBefore&&facialChanged&&colorChanged&&maxChange>.1f
            };
        }
        finally
        {
            if(model)UnityEngine.Object.DestroyImmediate(model);
            foreach(var mesh in generatedMeshes)if(mesh&&!EditorUtility.IsPersistent(mesh))UnityEngine.Object.DestroyImmediate(mesh);
            if(testTexture)UnityEngine.Object.DestroyImmediate(testTexture);
            if(material)UnityEngine.Object.DestroyImmediate(material);
            EditorSceneManager.ClosePreviewScene(preview);
        }
        var rootsAfter=scene.GetRootGameObjects().Select(x=>x.GetInstanceID()).OrderBy(x=>x).ToArray();
        var report=new{
            inspectedOn="2026-10-15",version=File.ReadAllText("Assets/Synty/SidekickCharacters/Scripts/Editor/version.txt").Trim(),
            scene=scene.path,dirtyBefore,dirtyAfter=scene.isDirty,sceneRootsPreserved=rootsBefore.SequenceEqual(rootsAfter),
            partCount=runtime.PartCount,
            availableGroups=runtime.MappedPartDictionary.Select(g=>new{group=g.Key.ToString(),count=g.Value.Count}).ToArray(),
            species=SidekickSpecies.GetAll(runtime.DBManager).Select(s=>s.Name).ToArray(),
            installedFilters=SidekickPresetFilter.GetAll(runtime.DBManager).Select(f=>f.Term).ToArray(),
            testResults
        };
        File.WriteAllText("Docs/Sidekick_Access_Probe.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return report;
    }
}
