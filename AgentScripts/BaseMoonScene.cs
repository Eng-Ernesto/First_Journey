using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BaseMoonScene
{
    const string Root = "Assets/_FirstJourney";
    const string Art = Root + "/Art/BaseMoon";
    const string ModelPath = Art + "/Models/FirstJourney_BaseMoon_v01.fbx";
    const string ScenePath = Root + "/Scenes/FirstJourney_LuaBase.unity";
    const string PrefabPath = Root + "/Prefabs/BaseMoon.prefab";

    [Serializable] public class Manifest { public MaterialInfo[] materials; public ObjectInfo[] objects; }
    [Serializable] public class MaterialInfo
    {
        public string name;
        public float[] base_color_linear;
        public float roughness;
        public float metallic;
        public string base_texture;
        public string normal_texture;
    }
    [Serializable] public class ObjectInfo { public string name; public string[] materials; }

    public static object Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before importing the scene.");
        if (File.Exists(ScenePath)) throw new InvalidOperationException("The target scene already exists; refusing to overwrite it.");
        var manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(Art + "/Data/manifest.json"));
        var source = File.ReadAllText(Art + "/Data/manifest.json");
        if (!source.Contains("ANG_Arvore_20__")) throw new InvalidOperationException("The export does not contain the realized trees.");
        Directory.CreateDirectory(Art + "/Materials");
        Directory.CreateDirectory(Root + "/Prefabs");
        Directory.CreateDirectory(Root + "/Scenes");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var pipeline = GraphicsSettings.currentRenderPipeline;
        var shader = Shader.Find(pipeline != null ? "Universal Render Pipeline/Lit" : "Standard");
        if (!shader) throw new InvalidOperationException("The installed render pipeline's Lit shader is unavailable.");

        var materials = new Dictionary<string, Material>();
        foreach (var m in manifest.materials)
        {
            string path = Art + "/Materials/" + m.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); material.name = m.name; AssetDatabase.CreateAsset(material, path); }
            var a = m.base_color_linear;
            var color = new Color(a[0], a[1], a[2], a.Length > 3 ? a[3] : 1).gamma;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", m.metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1 - m.roughness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 1 - m.roughness);
            if (!String.IsNullOrEmpty(m.base_texture))
            {
                string texturePath = Art + "/" + m.base_texture;
                var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (textureImporter != null) { textureImporter.sRGBTexture = true; textureImporter.maxTextureSize = 2048; textureImporter.SaveAndReimport(); }
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (!texture) throw new InvalidOperationException("Missing baked base texture: " + texturePath);
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                // A baked albedo already contains the original color.
                material.color = Color.white;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            }
            if (!String.IsNullOrEmpty(m.normal_texture))
            {
                string texturePath = Art + "/" + m.normal_texture;
                var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (textureImporter != null && textureImporter.textureType != TextureImporterType.NormalMap)
                { textureImporter.textureType = TextureImporterType.NormalMap; textureImporter.SaveAndReimport(); }
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (!texture) throw new InvalidOperationException("Missing baked normal texture: " + texturePath);
                if (material.HasProperty("_BumpMap")) { material.SetTexture("_BumpMap", texture); material.EnableKeyword("_NORMALMAP"); }
            }
            EditorUtility.SetDirty(material);
            materials.Add(m.name, material);
        }
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("The FBX importer is unavailable.");
        importer.globalScale = 1;
        importer.importCameras = false; importer.importLights = false; importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.importNormals = ModelImporterNormals.Import;
        importer.importTangents = ModelImporterTangents.CalculateMikk;
        foreach (var pair in materials) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);
        importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new InvalidOperationException("The imported model is missing.");
        var previousScenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var environment = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
        environment.name = "LuaBase_ArteOriginal";
        environment.transform.position = Vector3.zero;
        foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>())
        {
            var mapped = renderer.sharedMaterials.Select(m => m && materials.ContainsKey(m.name) ? materials[m.name] : m).ToArray();
            renderer.sharedMaterials = mapped;
        }
        int colliders = 0;
        foreach (var filter in environment.GetComponentsInChildren<MeshFilter>())
        {
            string n = filter.name;
            bool solid = n == "ANG_Terreno" || n.StartsWith("ANG_Parede_") || n.StartsWith("ANG_Cabana_") || n.StartsWith("ANG_Degrau") || n.StartsWith("ANG_Post") || n.Contains("__ANG_Tronco_");
            if (solid && filter.sharedMesh)
            {
                var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; collider.convex = false; colliders++;
            }
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(environment, PrefabPath, InteractionMode.AutomatedAction);
        var cameraObject = new GameObject("Main Camera"); var camera = cameraObject.AddComponent<Camera>(); cameraObject.tag = "MainCamera";
        camera.transform.position = new Vector3(-12.226f, 17.797f, 24.775f);
        camera.transform.LookAt(new Vector3(0, 2.8f, 0));
        camera.orthographic = false; camera.fieldOfView = 43; camera.nearClipPlane = .1f; camera.farClipPlane = 150;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.14f, .22f, .20f); camera.allowHDR = true;
        var additional = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (pipeline != null && additional != null) cameraObject.AddComponent(additional);
        cameraObject.AddComponent<AudioListener>();
        var keyObject = new GameObject("Sun"); var key = keyObject.AddComponent<Light>(); key.type = LightType.Directional;
        key.transform.rotation = Quaternion.Euler(45,145,0); key.color = new Color(1,.91f,.76f); key.intensity = 1.15f; key.shadows = LightShadows.Soft;
        var fillObject = new GameObject("Soft Fill"); var fill = fillObject.AddComponent<Light>(); fill.type = LightType.Directional;
        fill.transform.rotation = Quaternion.Euler(35,140,0); fill.color = new Color(.70f,.85f,1); fill.intensity = .35f;
        RenderSettings.sun = key; RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.46f,.51f,.48f); RenderSettings.ambientEquatorColor = new Color(.31f,.35f,.30f); RenderSettings.ambientGroundColor = new Color(.18f,.16f,.11f);
        RenderSettings.fog = false;
        var renderers = environment.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        foreach (var old in previousScenes) if (old.IsValid() && !old.isDirty) EditorSceneManager.CloseScene(old, true);
        Selection.activeGameObject = environment;
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null) { sceneView.LookAt(new Vector3(0,2.8f,0), camera.transform.rotation, 22, false); sceneView.Repaint(); }
        var result = new Dictionary<string, object> {
            {"scene", ScenePath}, {"prefab", PrefabPath}, {"render_pipeline",pipeline ? pipeline.name : "Built-in"},
            {"materials",materials.Count}, {"renderers",renderers.Length}, {"colliders",colliders},
            {"bounds_center",new[]{bounds.center.x,bounds.center.y,bounds.center.z}}, {"bounds_size",new[]{bounds.size.x,bounds.size.y,bounds.size.z}},
            {"tree_parts",environment.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("ANG_Arvore_"))},
            {"missing_materials",renderers.Sum(r=>r.sharedMaterials.Count(m=>m==null))}
        };
        File.WriteAllText("Docs/BaseMoon_Import_Result.json", UnityEngine.JsonUtility.ToJson(new Summary { scene=ScenePath, renderers=renderers.Length, materials=materials.Count, colliders=colliders, tree_parts=environment.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("ANG_Arvore_")) },true));
        return result;
    }
    [Serializable] public class Summary { public string scene; public int renderers,materials,colliders,tree_parts; }
    public static object Inspect()
    {
        var root = GameObject.Find("LuaBase_ArteOriginal");
        if (!root) throw new InvalidOperationException("The scene root is missing.");
        var renderers=root.GetComponentsInChildren<Renderer>();
        var missing = renderers.Sum(r=>r.sharedMaterials.Count(m=>!m));
        var shaders = renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.shader.name).Distinct().ToArray();
        return new Dictionary<string,object> {{"scene",SceneManager.GetActiveScene().path},{"renderers",renderers.Length},{"missing_materials",missing},{"shaders",shaders},{"cameras",UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>c.name).ToArray()}};
    }
    public static void CleanBootstrap()
    {
        AssetDatabase.DeleteAsset("Assets/Docs/LuaBase_Unity_v01.png");
        if (AssetDatabase.IsValidFolder("Assets/Docs") && Directory.GetFiles("Assets/Docs").Length == 0) AssetDatabase.DeleteAsset("Assets/Docs");
        AssetDatabase.DeleteAsset(Root + "/Editor/PipelineReconnect.cs");
    }
}

