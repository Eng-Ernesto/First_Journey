using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PrepareAJHero
{
    public const string ModelPath="Assets/_FirstJourney/Art/Characters/HeroAJ/Source/AJ_Model.fbx";
    public static string InspectSource()
    {
        EnsureFolder(Path.GetDirectoryName(ModelPath).Replace('\\','/'));
        string src=@"C:\Ernesto\unity\assets\Characters\HeroAJ\Source\AJ_WithSkin.fbx";
        if (!File.Exists(src)) throw new Exception("AJ With Skin FBX is required. explorador.fbx is not the selected Mixamo hero.");
        if (!File.Exists(ModelPath)) File.Copy(src,ModelPath);
        else if (!File.ReadAllBytes(src).SequenceEqual(File.ReadAllBytes(ModelPath))) throw new Exception("Different hero model exists.");
        AssetDatabase.ImportAsset(ModelPath,ImportAssetOptions.ForceSynchronousImport);
        var imp=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        imp.animationType=ModelImporterAnimationType.Human;
        imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        imp.SaveAndReimport();
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var av=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        var rends=root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds=new Bounds(); bool first=true;
        foreach(var r in rends) { if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds); }
        string report="Model="+root.name+"; bounds="+bounds+"; Avatar="+(av==null?"missing":av.isHuman+" / "+av.isValid)+
            "\nMeshes="+root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length+
            "; vertices="+AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Mesh>().Sum(m=>m.vertexCount)+
            "\nMaterials="+string.Join("\n",rends.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().Select(m=>m.name+" | "+m.shader.name+" | texture="+(m.mainTexture==null?"none":m.mainTexture.name)))+
            "\nTextures="+string.Join(",",AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Texture>().Select(t=>t.name))+
            "\nClipSettingsProperties="+string.Join(",",typeof(ModelImporterClipAnimation).GetProperties().Select(p=>p.Name))+
            "\nScene="+UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
        Directory.CreateDirectory("Docs"); File.WriteAllText("Docs/HeroAJ_Source_Inspection.txt",report);
        report+="\nHierarchy="+string.Join(",",root.GetComponentsInChildren<Transform>(true).Select(t=>t.name));
        var preview=new PreviewRenderUtility();
        try {
            preview.BeginStaticPreview(new Rect(0,0,800,800));
            preview.AddSingleGO(UnityEngine.Object.Instantiate(root));
            preview.lights[0].intensity=1.2f; preview.lights[0].transform.rotation=Quaternion.Euler(35,125,0);
            preview.lights[1].intensity=.7f; preview.ambientColor=Color.gray;
            preview.camera.backgroundColor=new Color(.12f,.18f,.17f); preview.camera.clearFlags=CameraClearFlags.SolidColor;
            preview.camera.fieldOfView=32; preview.camera.nearClipPlane=.02f;
            preview.camera.transform.position=bounds.center+new Vector3(0,.12f,2.6f*bounds.size.y);
            preview.camera.transform.LookAt(bounds.center); preview.camera.Render();
            var tex=preview.EndStaticPreview();
            File.WriteAllBytes("Docs/HeroAJ_Source_Preview.png",tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        } finally { preview.Cleanup(); }
        return report;
    }
    public static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    public static string ExtractTextures()
    {
        const string folder="Assets/_FirstJourney/Art/Characters/HeroAJ/Textures";
        EnsureFolder(folder);
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        bool extracted=importer.ExtractTextures(folder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        return "Extracted="+extracted+"\nFiles="+string.Join("\n",Directory.GetFiles(folder))+
            "\nMaterials="+string.Join("\n",root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().Select(m=>m.name+" | "+m.shader.name+" | "+string.Join(",",m.GetTexturePropertyNames().Select(p=>p+":"+(m.GetTexture(p)?m.GetTexture(p).name:"none")))));
    }
}
