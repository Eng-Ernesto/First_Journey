using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstJourney
{
    // This scene stays loaded. Exterior art and interior worlds contain no player or camera.
    public class FJWorldSession : MonoBehaviour
    {
        public const string ExteriorPath="Assets/_FirstJourney/Scenes/FirstJourney_EspacoExterior.unity";
        public const string BasePath="Assets/_FirstJourney/Scenes/Exterior/FirstJourney_Base_Conteudo.unity";
        public const string AsteroidPath="Assets/_FirstJourney/Scenes/Exterior/FirstJourney_Asteroide01_Conteudo.unity";
        public const string WorldPath="Assets/_FirstJourney/Scenes/Worlds/FirstJourney_FantasyKingdom_Conteudo.unity";
        public static FJWorldSession Current { get; private set; }
        public FJHeroMotor player;
        public Camera playerCamera;
        public GameObject exteriorSkyAndLights;
        public bool IsReady { get; private set; }
        public bool InsideWorld { get; private set; }
        public string LoadedWorld { get; private set; }
        bool switching;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession(){Current=null;}
        void Awake()
        {
            if(Current&&Current!=this){Debug.LogError("Duplicate world session",this);enabled=false;return;}
            Current=this;
            if(!player)player=GetComponentInChildren<FJHeroMotor>();
            if(player)player.enabled=false;
        }
        IEnumerator Start()
        {
            if(Current!=this)yield break;
            yield return EnsureLoaded(BasePath);
            yield return EnsureLoaded(AsteroidPath);
            SceneManager.SetActiveScene(gameObject.scene);
            SetExteriorVisible(true);
            PlacePlayer("Inicio_Na_Base",55);
            IsReady=true;
            player.enabled=true;
        }
        static IEnumerator EnsureLoaded(string path)
        {
            var scene=SceneManager.GetSceneByPath(path);
            if(scene.IsValid()&&scene.isLoaded)yield break;
            if(!Application.CanStreamedLevelBeLoaded(path))throw new InvalidOperationException("Scene not in build list: "+path);
            var operation=SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);
            if(operation==null)throw new InvalidOperationException("Could not load "+path);
            while(!operation.isDone)yield return null;
        }
        public bool Handles(string target)=>IsReady&&(target==ExteriorPath||target==WorldPath);
        public IEnumerator ChangeContext(string target)
        {
            if(switching)throw new InvalidOperationException("World transition already running");
            switching=true;
            try
            {
                if(target==ExteriorPath)
                {
                    // The plane and both islands keep their original instances and transforms.
                    if(!string.IsNullOrEmpty(LoadedWorld))
                    {
                        var world=SceneManager.GetSceneByPath(LoadedWorld);
                        if(world.IsValid()&&world.isLoaded)
                        {
                            foreach(var root in world.GetRootGameObjects())root.SetActive(false);
                            SceneManager.SetActiveScene(gameObject.scene);
                            var unload=SceneManager.UnloadSceneAsync(world);
                            if(unload!=null)while(!unload.isDone)yield return null;
                        }
                    }
                    LoadedWorld=null;InsideWorld=false;SetExteriorVisible(true);
                    player.fallResetHeight=-6;playerCamera.fieldOfView=48;
                    playerCamera.clearFlags=CameraClearFlags.SolidColor;
                }
                else
                {
                    SetExteriorVisible(false);
                    yield return EnsureLoaded(target);
                    SceneManager.SetActiveScene(SceneManager.GetSceneByPath(target));
                    LoadedWorld=target;InsideWorld=true;
                    player.fallResetHeight=-100;playerCamera.fieldOfView=55;
                    playerCamera.clearFlags=CameraClearFlags.Skybox;
                }
                Physics.SyncTransforms();
            }
            finally{switching=false;}
        }
        void SetExteriorVisible(bool visible)
        {
            foreach(var path in new[]{BasePath,AsteroidPath})
            {
                var scene=SceneManager.GetSceneByPath(path);
                if(scene.IsValid()&&scene.isLoaded)
                    foreach(var root in scene.GetRootGameObjects())root.SetActive(visible);
            }
            if(exteriorSkyAndLights)exteriorSkyAndLights.SetActive(visible);
        }
        public void PlacePlayer(string markerName,float yaw)
        {
            var marker=GameObject.Find(markerName);
            if(!marker)throw new InvalidOperationException("Arrival marker missing: "+markerName);
            player.spawnPosition=marker.transform.position;
            player.ResetToSpawn();player.transform.rotation=marker.transform.rotation;
            player.movementCamera=playerCamera;player.ClearTestInput();
            var orbit=playerCamera.GetComponent<FJOrbitCamera>();orbit.yaw=yaw;orbit.Snap();
        }
        void OnDestroy(){if(Current==this)Current=null;}
        void OnGUI()
        {
            if(IsReady)return;
            GUI.Box(new Rect(Screen.width/2f-150,Screen.height/2f-25,300,50),"A preparar a viagem…");
        }
    }
}
