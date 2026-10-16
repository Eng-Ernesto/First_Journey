using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FirstJourney
{
    // Shared sessions retain the hero and exterior; standalone scenes use the legacy fallback.
    public class FJWorldDoor : MonoBehaviour
    {
        public string destinationScene;
        public string arrivalMarker;
        public string actionLabel="Entrar no reino";
        public float interactionRadius=2.1f;
        public float arrivalCameraYaw;
        public Transform doorLeaf;
        public float openingAngle=-75;
        public GameObject openDoorSurface;
        public bool showWorldMarker;
        public string markerLabel="CASA DE REGRESSO";
        public float markerRange=55;
        FJHeroMotor hero;
        public static bool IsTravelling { get; internal set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetTravel() { IsTravelling=false; }
        public bool InRange()
        {
            if(!hero)hero=FindFirstObjectByType<FJHeroMotor>();
            if(!hero||!hero.enabled||FJAircraftController.PilotedAircraft)return false;
            var delta=hero.transform.position-transform.position;
            return Mathf.Abs(delta.y)<2.5f && new Vector2(delta.x,delta.z).magnitude<=interactionRadius;
        }
        void Update()
        {
            if(IsTravelling||!InRange())return;
            if(FJInteractionInput.Pressed&&FJInteractionInput.Consume())
                TryTravel();
        }
        public bool TryTravel()
        {
            if(IsTravelling||!InRange())return false;
            if(!Application.CanStreamedLevelBeLoaded(destinationScene))
            { Debug.LogError("Destino indisponível: "+destinationScene,this);return false; }
            IsTravelling=true;
            var runner=new GameObject("Viagem_Entre_Mundos").AddComponent<FJWorldTravel>();
            DontDestroyOnLoad(runner.gameObject);
            runner.Begin(this,hero);
            return true;
        }
        void OnGUI()
        {
            if(IsTravelling||FJAircraftController.PilotedAircraft)return;
            if(!InRange())
            {
                if(showWorldMarker&&hero&&Camera.main)
                {
                    float distance=Vector3.Distance(hero.transform.position,transform.position);
                    var point=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*2.8f);
                    if(distance<markerRange&&point.z>0)
                        GUI.Box(new Rect(point.x-115,Screen.height-point.y-38,230,42),markerLabel+"\n"+Mathf.CeilToInt(distance)+" m");
                }
                return;
            }
            var style=new GUIStyle(GUI.skin.box){fontSize=18,alignment=TextAnchor.MiddleCenter};
            GUI.Box(new Rect(Screen.width/2f-180,Screen.height-100,360,44),"E / □  ·  "+actionLabel,style);
        }
    }

    public class FJWorldTravel : MonoBehaviour
    {
        float opacity;
        public void Begin(FJWorldDoor door,FJHeroMotor hero) { StartCoroutine(Travel(door,hero)); }
        IEnumerator Travel(FJWorldDoor door,FJHeroMotor hero)
        {
            string path=door.destinationScene,marker=door.arrivalMarker;
            float yaw=door.arrivalCameraYaw;
            var leaf=door.doorLeaf;
            float openingAngle=door.openingAngle;
            if(door.openDoorSurface)door.openDoorSurface.SetActive(true);
            Quaternion closed=leaf?leaf.localRotation:Quaternion.identity;
            hero.enabled=false;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            for(float t=0;t<1;t+=Time.unscaledDeltaTime/.8f)
            {
                opacity=Mathf.Clamp01((t-.55f)/.45f);
                if(leaf)leaf.localRotation=closed*Quaternion.Euler(0,openingAngle*t,0);
                yield return null;
            }
            opacity=1;
            var session=FJWorldSession.Current;
            if(session&&session.Handles(path))
                yield return session.ChangeContext(path);
            else
            {
                var loading=SceneManager.LoadSceneAsync(path,LoadSceneMode.Single);
                while(!loading.isDone)yield return null;
            }
            var next=FindFirstObjectByType<FJHeroMotor>();
            var spawn=GameObject.Find(marker);
            if(next && spawn)
            {
                next.spawnPosition=spawn.transform.position;
                next.ResetToSpawn();next.transform.rotation=spawn.transform.rotation;
                next.ClearTestInput();
                next.enabled=true;
                var orbit=Camera.main?Camera.main.GetComponent<FJOrbitCamera>():null;
                if(orbit){orbit.yaw=yaw;orbit.Snap();}
            }
            else Debug.LogError("Chegada sem personagem ou ponto de regresso: "+marker);
            // Additive exterior scenes survive the visit; leave their door ready for the next trip.
            if(leaf)leaf.localRotation=closed;
            if(door&&door.openDoorSurface)door.openDoorSurface.SetActive(false);
            yield return null;
            for(float t=1;t>0;t-=Time.unscaledDeltaTime/.35f){opacity=t;yield return null;}
            FJWorldDoor.IsTravelling=false;
            Destroy(gameObject);
        }
        void OnGUI()
        {
            var color=GUI.color;GUI.depth=-1000;GUI.color=new Color(0,0,0,opacity);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.color=new Color(1,1,1,opacity);
            GUI.Label(new Rect(Screen.width/2f-100,Screen.height/2f-20,200,40),"A atravessar a porta…",new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=20});
            GUI.color=color;
        }
        void OnDestroy(){FJWorldDoor.IsTravelling=false;}
    }
}
