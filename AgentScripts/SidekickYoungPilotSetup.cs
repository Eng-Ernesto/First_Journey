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
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.VisualScripting.YamlDotNet.Serialization;
using Synty.SidekickCharacters;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.Serialization;
using FirstJourney;

public static class SidekickYoungPilotSetup
{
    public const string Art="Assets/_FirstJourney/Art/Characters/PilotoAventureiro";
    public const string VisualPrefab="Assets/_FirstJourney/Prefabs/PilotoAventureiro_Visual_v02.prefab";
    public const string GameVisualPrefab="Assets/_FirstJourney/Prefabs/PilotoAventureiro_Visual_Jogo_v02.prefab";
    public const string HeroPrefab="Assets/_FirstJourney/Prefabs/HeroSidekick.prefab";
    // Garments are authored in the Sidekick rest pose and inherit its skin weights.
    // They are separate from the editable Sidekick face/body definition.
    sealed class Clothing
    {
        public List<Vector3> vertices=new List<Vector3>();
        public List<Vector2> uv=new List<Vector2>();
        public List<BoneWeight> weights=new List<BoneWeight>();
        public List<int> indices=new List<int>();
        public SkinnedMeshRenderer body; public Vector3[] bodyVertices; public BoneWeight[] bodyWeights;
        public Dictionary<int,SidekickColorProperty> palette; public Texture texture;
        public BoneWeight Weight(Vector3 p,int rigid=-1)
        {
            if(rigid>=0)return new BoneWeight{boneIndex0=rigid,weight0=1};
            int best=0;float distance=float.MaxValue;
            for(int i=0;i<bodyVertices.Length;i++){float d=(bodyVertices[i]-p).sqrMagnitude;if(d<distance){distance=d;best=i;}}
            return bodyWeights[best];
        }
        public void Tri(Vector3 a,Vector3 b,Vector3 c,int color,Vector3 outward,int rigid=-1)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),outward)<0){var temp=b;b=c;c=temp;}
            var prop=palette[color];var texel=new Vector2((prop.U*2+1f)/texture.width,(prop.V*2+1f)/texture.height);
            foreach(var p in new[]{a,b,c}){indices.Add(vertices.Count);vertices.Add(p);uv.Add(texel);weights.Add(Weight(p,rigid));}
        }
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int color,Vector3 outward,int rigid=-1){Tri(a,b,c,color,outward,rigid);Tri(a,c,d,color,outward,rigid);}
        public void Tube(Vector3[] centers,Vector2[] radii,bool horizontal,int color,int count=12,int rigid=-1,bool shirt=false,bool cap=true)
        {
            var rings=new List<Vector3[]>();
            for(int j=0;j<centers.Length;j++)
            {
                var ring=new Vector3[count];for(int i=0;i<count;i++){float angle=(i+.5f)*Mathf.PI*2/count;ring[i]=centers[j]+(horizontal?new Vector3(0,Mathf.Sin(angle)*radii[j].x,Mathf.Cos(angle)*radii[j].y):new Vector3(Mathf.Sin(angle)*radii[j].x,0,Mathf.Cos(angle)*radii[j].y));}rings.Add(ring);
            }
            for(int j=0;j<rings.Count-1;j++)for(int i=0;i<count;i++)
            {
                int k=(i+1)%count;var mid=(rings[j][i]+rings[j][k]+rings[j+1][i]+rings[j+1][k])*.25f;var radial=mid-(centers[j]+centers[j+1])*.5f;
                bool front=shirt&&i==count-1;
                Quad(rings[j][i],rings[j][k],rings[j+1][k],rings[j+1][i],front?43:color,radial,rigid);
            }
            if(cap)foreach(int end in new[]{0,rings.Count-1})for(int i=0;i<count;i++)Tri(centers[end],rings[end][i],rings[end][(i+1)%count],color,(horizontal?Vector3.right:Vector3.up)*(end==0?-1:1),rigid);
        }
        public void Box(Vector3 center,Vector3 size,int color,int rigid=-1)
        {
            var p=new Vector3[8];for(int i=0;i<8;i++)p[i]=center+Vector3.Scale(size,new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f));
            int[,] faces={{0,1,3,2},{4,6,7,5},{0,4,5,1},{2,3,7,6},{0,2,6,4},{1,5,7,3}};
            for(int i=0;i<6;i++){var a=p[faces[i,0]];var b=p[faces[i,1]];var c=p[faces[i,2]];var d=p[faces[i,3]];Quad(a,b,c,d,color,(a+b+c+d)*.25f-center,rigid);}
        }
        public void Goggle(Vector3 center,int rigid)
        {
            const int n=12;var outer=new Vector3[n];var inner=new Vector3[n];
            for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;outer[i]=center+new Vector3(Mathf.Cos(a)*.052f,Mathf.Sin(a)*.039f,0);inner[i]=center+new Vector3(Mathf.Cos(a)*.040f,Mathf.Sin(a)*.029f,.009f);}
            for(int i=0;i<n;i++){int k=(i+1)%n;Quad(outer[i],outer[k],inner[k],inner[i],160,Vector3.forward,rigid);Tri(center+Vector3.forward*.008f,inner[i],inner[k],168,Vector3.forward,rigid);}
        }
    }
    static void DressYoungPilot(GameObject model,SidekickRuntime source,Material material,Dictionary<int,SidekickColorProperty> palette)
    {
        var body=model.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=body.sharedMesh;
        var vertices=mesh.vertices;var weights=mesh.boneWeights;
        // Slightly larger head, retaining the Sidekick facial rig and expressions.
        for(int i=0;i<vertices.Length;i++)if(vertices[i].y>1.50f)vertices[i]=new Vector3(0,1.50f,0)+(vertices[i]-new Vector3(0,1.50f,0))*1.10f;
        mesh.vertices=vertices;
        var garment=new Clothing{body=body,bodyVertices=vertices,bodyWeights=weights,palette=palette,texture=material.GetTexture("_ColorMap")};
        int head=Array.FindIndex(body.bones,b=>b.name=="head");
        // Keep the head, neck and bare hands; all armoured source clothing is absent.
        var triangles=mesh.triangles;var keep=new List<int>();
        for(int i=0;i<triangles.Length;i+=3)
        {
            var p=(vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3;
            if(p.y>1.415f||(Mathf.Abs(p.x)>.685f&&p.y>1.22f))keep.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
        }
        mesh.triangles=keep.ToArray();mesh.RecalculateBounds();
        // Short soft jacket with a cream shirt visible along its front opening.
        garment.Tube(new[]{new Vector3(0,.96f,-.005f),new Vector3(0,1.01f,-.005f),new Vector3(0,1.19f,-.015f),new Vector3(0,1.35f,-.035f),new Vector3(0,1.435f,-.035f)},new[]{new Vector2(.172f,.115f),new Vector2(.181f,.123f),new Vector2(.200f,.135f),new Vector2(.226f,.115f),new Vector2(.087f,.072f)},false,42,16,-1,true);
        garment.Tube(new[]{new Vector3(0,.948f,-.005f),new Vector3(0,.985f,-.005f)},new[]{new Vector2(.184f,.127f),new Vector2(.186f,.129f)},false,186);
        // Folded cream collar and lapels: no breastplate or shoulder armour.
        for(int side=-1;side<=1;side+=2)
        {
            garment.Quad(new Vector3(side*.040f,1.422f,.065f),new Vector3(side*.094f,1.432f,.054f),new Vector3(side*.143f,1.325f,.102f),new Vector3(side*.061f,1.355f,.111f),43,Vector3.forward);
            garment.Quad(new Vector3(side*.075f,1.25f,.122f),new Vector3(side*.145f,1.25f,.101f),new Vector3(side*.146f,1.20f,.106f),new Vector3(side*.075f,1.20f,.131f),44,Vector3.forward);
            garment.Quad(new Vector3(side*.075f,1.253f,.125f),new Vector3(side*.146f,1.253f,.104f),new Vector3(side*.146f,1.24f,.109f),new Vector3(side*.075f,1.24f,.134f),186,Vector3.forward);
            // Relaxed sleeves, plain cuffs and fitted trousers.
            garment.Tube(new[]{new Vector3(side*.17f,1.37f,-.047f),new Vector3(side*.30f,1.37f,-.05f),new Vector3(side*.45f,1.36f,-.05f),new Vector3(side*.56f,1.355f,-.052f),new Vector3(side*.69f,1.35f,-.052f)},new[]{new Vector2(.085f,.085f),new Vector2(.074f,.075f),new Vector2(.067f,.065f),new Vector2(.06f,.06f),new Vector2(.046f,.049f)},true,42);
            garment.Tube(new[]{new Vector3(side*.66f,1.35f,-.052f),new Vector3(side*.695f,1.35f,-.052f)},new[]{new Vector2(.049f,.052f),new Vector2(.049f,.052f)},true,43);
            garment.Tube(new[]{new Vector3(side*.095f,.94f,-.025f),new Vector3(side*.103f,.79f,-.026f),new Vector3(side*.104f,.59f,-.023f),new Vector3(side*.105f,.44f,-.020f),new Vector3(side*.108f,.32f,-.032f),new Vector3(side*.110f,.22f,-.035f)},new[]{new Vector2(.092f,.102f),new Vector2(.088f,.094f),new Vector2(.073f,.08f),new Vector2(.066f,.067f),new Vector2(.061f,.065f),new Vector2(.057f,.06f)},false,57);
            garment.Tube(new[]{new Vector3(side*.11f,.08f,-.035f),new Vector3(side*.11f,.16f,-.035f),new Vector3(side*.11f,.255f,-.035f)},new[]{new Vector2(.066f,.074f),new Vector2(.064f,.07f),new Vector2(.067f,.072f)},false,186);
            garment.Tube(new[]{new Vector3(side*.11f,.018f,.035f),new Vector3(side*.11f,.052f,.035f),new Vector3(side*.11f,.10f,.015f)},new[]{new Vector2(.074f,.152f),new Vector2(.075f,.155f),new Vector2(.066f,.125f)},false,186);
            garment.Tube(new[]{new Vector3(side*.11f,.008f,.035f),new Vector3(side*.11f,.035f,.035f)},new[]{new Vector2(.076f,.154f),new Vector2(.076f,.154f)},false,177);
        }
        garment.Tube(new[]{new Vector3(0,.87f,-.022f),new Vector3(0,.95f,-.016f),new Vector3(0,.985f,-.013f)},new[]{new Vector2(.176f,.102f),new Vector2(.174f,.108f),new Vector2(.17f,.108f)},false,57);
        // Small aviator goggles resting on the forehead, with a leather band.
        garment.Tube(new[]{new Vector3(0,1.785f,-.008f),new Vector3(0,1.812f,-.008f)},new[]{new Vector2(.144f,.131f),new Vector2(.144f,.131f)},false,186,16,head,false,false);
        garment.Goggle(new Vector3(-.056f,1.797f,.137f),head);garment.Goggle(new Vector3(.056f,1.797f,.137f),head);
        garment.Box(new Vector3(0,1.797f,.146f),new Vector3(.026f,.014f,.018f),160,head);
        var clothingMesh=new Mesh{name="Roupa_Piloto_Jovem"};clothingMesh.SetVertices(garment.vertices);clothingMesh.SetUVs(0,garment.uv);clothingMesh.SetTriangles(garment.indices,0);clothingMesh.boneWeights=garment.weights.ToArray();clothingMesh.bindposes=mesh.bindposes;clothingMesh.RecalculateNormals();clothingMesh.RecalculateTangents();clothingMesh.RecalculateBounds();
        var go=new GameObject("Roupa_Piloto_Jovem");go.transform.SetParent(body.transform.parent,false);go.transform.localPosition=body.transform.localPosition;go.transform.localRotation=body.transform.localRotation;go.transform.localScale=body.transform.localScale;
        var renderer=go.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=clothingMesh;renderer.sharedMaterial=material;renderer.bones=body.bones;renderer.rootBone=body.rootBone;renderer.updateWhenOffscreen=true;
    }
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
        definition.Name="FirstJourney_Piloto_v02"; foreach(var part in definition.Parts) if(part.Name.StartsWith("SK_SCFI_CIVL_09_")) part.Name=part.Name.Replace("SK_SCFI_CIVL_09_","SK_HUMN_BASE_01_");
        definition.Parts.RemoveAll(p=>p.PartType.ToString().StartsWith("Attachment")||p.PartType==CharacterPartType.FacialHair||p.PartType==CharacterPartType.Wrap);
        var hair=definition.Parts.FirstOrDefault(p=>p.PartType==CharacterPartType.Hair);
        if(hair!=null)hair.Name="SK_HUMN_BASE_06_02HAIR_HU01";
        else definition.Parts.Add(new SerializedPart("SK_HUMN_BASE_06_02HAIR_HU01",CharacterPartType.Hair,"1"));
        definition.BlendShapes=new SerializedBlendShapeValues{BodyTypeValue=-100,BodySizeValue=-15,MuscleValue=-100};
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
            if(row.ColorProperty==57)row.MainColor="3B5963";
            if(row.ColorProperty==168){row.MainColor="78B8C4";row.Opacity="FFFFFF";row.Smoothness="500000";row.Metallic="000000";}
            if(new[]{42,43,44,57,177,186}.Contains(row.ColorProperty)){row.Metallic="000000";row.Smoothness="200000";}
        }
        var material=UnityEngine.Object.Instantiate(source.CurrentMaterial);material.name="M_Piloto_Retro_v02";
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
        material=SaveAsset(material,Art+"/Materials/M_Piloto_Retro_v02.mat");runtime.CurrentMaterial=material;
        runtime.BodyTypeBlendValue=-100;runtime.BodySizeSkinnyBlendValue=15;runtime.BodySizeHeavyBlendValue=0;runtime.MusclesBlendValue=-100;
        var parts=definition.Parts.Select(p=>runtime.DBManager==null?null:source.MappedPartDictionary[p.PartType][p.Name].GetPartModel().GetComponentInChildren<SkinnedMeshRenderer>()).ToList();
        var preview=EditorSceneManager.NewPreviewScene();GameObject model=null;
        try
        {
            model=runtime.CreateCharacter(definition.Name,parts,true,false,null,false,true);SceneManager.MoveGameObjectToScene(model,preview); DressYoungPilot(model,source,material,properties);
            foreach(var mesh in model.GetComponentsInChildren<SkinnedMeshRenderer>()){mesh.sharedMesh.name=mesh.name+"_Mesh";mesh.sharedMesh=SaveAsset(mesh.sharedMesh,Art+"/Meshes/"+mesh.name+"_Mesh.asset");mesh.updateWhenOffscreen=true;}
            var animator=model.GetComponent<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);animator.applyRootMotion=false;
            if(!animator.avatar||!animator.avatar.isHuman||!animator.avatar.isValid)throw new Exception("Invalid pilot Avatar.");
            var prefab=PrefabUtility.SaveAsPrefabAsset(model,VisualPrefab);
            var window=Resources.FindObjectsOfTypeAll<ModularCharacterWindow>().First();
            window.GetType().GetMethod("ExportCharacterModel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{model,Art+"/Meshes/PilotoAventureiro_v02.fbx",GameVisualPrefab,material,animator.avatar});
            var contents=PrefabUtility.LoadPrefabContents(GameVisualPrefab);
            try
            {
                var gameAnimator=contents.GetComponent<Animator>();gameAnimator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);gameAnimator.applyRootMotion=false;
                var clothMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="M_Roupa_Piloto_v02"};clothMaterial.SetTexture("_BaseMap",material.GetTexture("_ColorMap"));clothMaterial.SetFloat("_Smoothness",.15f);clothMaterial.SetFloat("_Metallic",0);
                clothMaterial=SaveAsset(clothMaterial,Art+"/Materials/M_Roupa_Piloto_v02.mat");
                foreach(var skin in contents.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;if(skin.name.Contains("Roupa_Piloto_Jovem"))skin.sharedMaterial=clothMaterial;}
                PrefabUtility.SaveAsPrefabAsset(contents,GameVisualPrefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            string definitionPath=Art+"/Definition/FirstJourney_Piloto_v02.sk";File.WriteAllText(definitionPath,new Serializer().Serialize(definition));AssetDatabase.ImportAsset(definitionPath);
            string recipePath=Art+"/Definition/PilotoAventureiro_v02_Recipe.json";
            File.WriteAllText(recipePath,JsonConvert.SerializeObject(new{version="v02",direction="Jovem piloto aventureiro; casaco curto de aviador, camisa clara, calcas simples, botas e oculos na testa. Sem armadura.",sidekickBaseDefinition=definitionPath,clothingAuthoringScript="AgentScripts/SidekickYoungPilotSetup.cs",clothingMethod="DressYoungPilot",gamePrefab=GameVisualPrefab,headVertexScale=1.10f,material="Universal Render Pipeline/Lit for custom clothing; original Sidekick shader for face",editingNote="The .sk loads the human Sidekick base only. The custom garments and head proportion pass are regenerated by BuildVisual; the exported game prefab contains the complete dressed model."},Formatting.Indented));AssetDatabase.ImportAsset(recipePath);
            AssetDatabase.SaveAssets();
            var result=new{character=definition.Name,prefab=VisualPrefab,definition=definitionPath,parts=parts.Count,vertices=model.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(m=>m.sharedMesh.vertexCount),avatar=animator.avatar.name,avatarHuman=animator.avatar.isHuman,avatarValid=animator.avatar.isValid};
            File.WriteAllText("Docs/Sidekick_Pilot_Build.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
        }
        finally{if(model)UnityEngine.Object.DestroyImmediate(model);EditorSceneManager.ClosePreviewScene(preview);}
    }
    public static object RenderPreview()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(GameVisualPrefab);if(!source)throw new Exception("Build visual first.");
        var utility=new PreviewRenderUtility();GameObject model=null;Texture2D image=null;var bakedMeshes=new List<Mesh>();PlayableGraph graph=default;
        try
        {
            model=UnityEngine.Object.Instantiate(source);utility.AddSingleGO(model);
            var animator=model.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.runtimeAnimatorController=null;animator.Rebind();
            var idle=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/_FirstJourney/Art/Characters/HeroAJ/Animations"}).Select(AssetDatabase.GUIDToAssetPath).Distinct().SelectMany(p=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>()).First(c=>c.name=="AJ_Idle");
            graph=PlayableGraph.Create("PilotReviewPose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,idle);playable.SetTime(1);var output=AnimationPlayableOutput.Create(graph,"PilotPose",animator);output.SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
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
            const string path="Assets/_FirstJourney/Review/PilotoAventureiro_Preview_v02.png";File.WriteAllBytes(path,image.EncodeToPNG());AssetDatabase.ImportAsset(path);return new{path,height=h};
        }
        finally{if(graph.IsValid())graph.Destroy();utility.Cleanup();foreach(var baked in bakedMeshes)UnityEngine.Object.DestroyImmediate(baked);if(image)UnityEngine.Object.DestroyImmediate(image);}
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
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,owner.transform);visual.name="PilotoAventureiro_Visual";
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
        var result=new{character="Piloto aventureiro v02",root=owner.name,prefab=HeroPrefab,backup,height=owner.GetComponent<CharacterController>().height,visualScale=scale,avatarHuman=animator.avatar.isHuman,avatarValid=animator.avatar.isValid,cameraFollow=orbit.target==owner.transform,activeMotors=UnityEngine.Object.FindObjectsByType<FJHeroMotor>(FindObjectsSortMode.None).Length,baseRenderers=GameObject.Find("LuaBase_ArteOriginal").GetComponentsInChildren<Renderer>().Length,sceneSaved=!SceneManager.GetActiveScene().isDirty};
        File.WriteAllText("Docs/Sidekick_Installation.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
    public static object RefreshInstalledVisual()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        var owner=GameObject.Find("HeroSidekick");if(!owner)throw new Exception("Install Sidekick first.");
        foreach(Transform child in owner.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(GameVisualPrefab);
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,owner.transform);visual.name="PilotoAventureiro_Visual";
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
        var result=new{character="Piloto aventureiro v02",visual=GameVisualPrefab,fbx=Art+"/Meshes/PilotoAventureiro_v02.fbx",avatar=animator.avatar.name,avatarHuman=animator.avatar.isHuman,avatarValid=animator.avatar.isValid,visualScale=scale,sceneSaved=!SceneManager.GetActiveScene().isDirty};
        File.WriteAllText("Docs/Sidekick_GameAsset.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
}




