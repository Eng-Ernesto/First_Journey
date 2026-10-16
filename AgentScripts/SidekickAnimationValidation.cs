using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class SidekickAnimationValidation
{
    public static object ValidateAll()
    {
        if(EditorApplication.isPlaying)throw new Exception("Run outside Play Mode.");
        const string directory="Assets/_FirstJourney/Art/Characters/HeroAJ/Animations";
        var clips=AssetDatabase.FindAssets("t:AnimationClip",new[]{directory}).Select(AssetDatabase.GUIDToAssetPath).Distinct()
            .SelectMany(p=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__"))).OrderBy(c=>c.name).ToArray();
        if(clips.Length!=17)throw new Exception("Expected 17 clips.");
        var scene=EditorSceneManager.NewPreviewScene();GameObject model=null;
        var reports=new List<object>();
        try
        {
            model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_FirstJourney/Prefabs/PilotoSidekick_Visual_Jogo_v01.prefab"));
            SceneManager.MoveGameObjectToScene(model,scene);
            var animator=model.GetComponent<Animator>();animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            if(!animator.avatar||!animator.avatar.isValid||!animator.avatar.isHuman)throw new Exception("Invalid Sidekick Avatar.");
            foreach(var clip in clips)
            {
                animator.Rebind();
                var graph=PlayableGraph.Create("Sidekick_Clip_Validation");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                try
                {
                    var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
                    var output=AnimationPlayableOutput.Create(graph,"Humanoid",animator);output.SetSourcePlayable(playable);graph.Play();
                    var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}.Select(animator.GetBoneTransform).ToArray();
                    if(bones.Any(b=>!b))throw new Exception("Missing required human bone.");
                    Quaternion[] firstPose=null;float maxMotion=0,minHeight=float.MaxValue,maxHeight=0,maxVertexRadius=0;
                    for(int sample=0;sample<5;sample++)
                    {
                        playable.SetTime(clip.length*sample/5d);graph.Evaluate(0);
                        if(firstPose==null)firstPose=bones.Select(b=>b.localRotation).ToArray();
                        for(int i=0;i<bones.Length;i++)maxMotion=Mathf.Max(maxMotion,Quaternion.Angle(firstPose[i],bones[i].localRotation));
                        foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var mesh=new Mesh();skin.BakeMesh(mesh);var vertices=mesh.vertices;
                            if(vertices.Length==0)throw new Exception("No deformed vertices.");
                            float low=float.MaxValue,high=float.MinValue;
                            foreach(var v in vertices)
                            {
                                if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.sqrMagnitude))throw new Exception("Invalid vertex in "+clip.name);
                                low=Mathf.Min(low,v.y);high=Mathf.Max(high,v.y);maxVertexRadius=Mathf.Max(maxVertexRadius,v.magnitude);
                            }
                            minHeight=Mathf.Min(minHeight,high-low);maxHeight=Mathf.Max(maxHeight,high-low);UnityEngine.Object.DestroyImmediate(mesh);
                        }
                    }
                    bool passed=clip.isHumanMotion&&Mathf.Abs(clip.frameRate-60)<.01f&&minHeight>.3f&&maxHeight<3.5f&&maxVertexRadius<5;
                    reports.Add(new{clip=clip.name,passed,humanoid=clip.isHumanMotion,fps=clip.frameRate,seconds=clip.length,samples=5,minHeight,maxHeight,maxVertexRadius,maxBoneRotationChange=maxMotion});
                    if(!passed)throw new Exception("Retarget deformation failed: "+clip.name);
                }
                finally{graph.Destroy();}
            }
            var result=new{passed=true,character="Piloto Sidekick v01",clips=clips.Length,avatarHuman=animator.avatar.isHuman,avatarValid=animator.avatar.isValid,method="Humanoid PlayableGraph evaluated on the actual Sidekick rig; five deformed-mesh samples per clip",entries=reports};
            File.WriteAllText("Docs/Sidekick_Animations_Test.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
        }
        finally{if(model)UnityEngine.Object.DestroyImmediate(model);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
