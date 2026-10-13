using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public static class ImportAJAnimation
{
    [Serializable] public class ClipInfo
    {
        public string name;
        public float seconds, frameRate;
        public int curveBindings;
        public bool humanoid, loop;
    }
    [Serializable] public class Report
    {
        public string source, asset, sha256, rig;
        public long bytes;
        public int meshes;
        public bool valid;
        public int clipCount, firstClipCurveBindings;
        public string firstClipName;
        public float firstClipSeconds, firstClipFrameRate;
        public bool firstClipHumanoid, firstClipLoop;
        public ClipInfo[] clips;
    }
    public static string Idle() { return Import("Idle"); }
    public static string Walk() { return Import("Walk"); }
    public static string Import(string state)
    {
        if (string.IsNullOrEmpty(state) || !state.All(char.IsLetter))
            throw new ArgumentException("Animation state must contain only letters.");
        string source = @"C:\Ernesto\unity\assets\Characters\HeroAJ\Animations\AJ_"+state+".fbx";
        string asset = "Assets/_FirstJourney/Art/Characters/HeroAJ/Animations/AJ_"+state+".fbx";
        EnsureFolder(Path.GetDirectoryName(asset).Replace('\\', '/'));
        string absolute = Path.GetFullPath(asset);
        byte[] bytes = File.ReadAllBytes(source);
        if (File.Exists(absolute) && !File.ReadAllBytes(absolute).SequenceEqual(bytes))
            throw new InvalidOperationException("Different AJ_"+state+".fbx already exists in Unity.");
        if (!File.Exists(absolute)) File.WriteAllBytes(absolute, bytes);
        AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(asset) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("FBX ModelImporter missing.");
        var objects = AssetDatabase.LoadAllAssetsAtPath(asset);
        var clips = objects.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).Select(c => new ClipInfo {
            name=c.name, seconds=c.length, frameRate=c.frameRate,
            curveBindings=AnimationUtility.GetCurveBindings(c).Length,
            humanoid=c.isHumanMotion, loop=AnimationUtility.GetAnimationClipSettings(c).loopTime
        }).ToArray();
        string hash;
        using(var sha=SHA256.Create()) hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        var report = new Report {source=source, asset=asset, sha256=hash, bytes=bytes.LongLength,
            rig=importer.animationType.ToString(), meshes=objects.OfType<Mesh>().Count(), clips=clips,
            valid=clips.Length>0 && clips.All(c => c.seconds>0 && c.frameRate>0 && c.curveBindings>0)};
        report.clipCount=clips.Length;
        if (clips.Length>0) {
            report.firstClipName=clips[0].name;
            report.firstClipSeconds=clips[0].seconds;
            report.firstClipFrameRate=clips[0].frameRate;
            report.firstClipCurveBindings=clips[0].curveBindings;
            report.firstClipHumanoid=clips[0].humanoid;
            report.firstClipLoop=clips[0].loop;
        }
        Directory.CreateDirectory("Docs");
        var json=JsonUtility.ToJson(report,true);
        File.WriteAllText("Docs/HeroAJ_"+state+"_Import.json",json);
        if (!report.valid) throw new InvalidOperationException("FBX contains no usable animation: "+json);
        return json;
    }
    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        var parent=Path.GetDirectoryName(folder).Replace('\\','/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,Path.GetFileName(folder));
    }
}
