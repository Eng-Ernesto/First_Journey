using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstJourney
{
    // Deliberately forgiving space flight: sustained lift, no stall, assisted vertical takeoff.
    // All travel and the final approach move through the same swept collision checks.
    public class FJAircraftController : MonoBehaviour
    {
        public enum FlightMode { Parked, Flying, Landing }
        public Transform seat,boardingPoint,propeller;
        public Transform[] propellerParts;
        public FJLandingSite startingSite;
        public float maxSpeed=16,acceleration=6,braking=12,turnSpeed=65,climbSpeed=4;
        public float boardingRadius=2.4f;
        public bool showHud=true;
        public FlightMode Mode {get;private set;}
        public bool IsPiloted=>pilot;
        public float Speed {get;private set;}
        public float DistanceFlown {get;private set;}
        public int ObstacleStops {get;private set;}
        public FJLandingSite ParkedSite {get;private set;}
        public FJHeroMotor Pilot=>pilot;
        public FJLandingSite NearbyLandingSite {get;private set;}
        public static FJAircraftController PilotedAircraft {get;private set;}
        FJHeroMotor pilot;
        CharacterController pilotBody;
        FJOrbitCamera orbit;
        FJAircraftCamera chase;
        Transform pilotParent;
        Vector3 seatedRootOffset;
        float targetSpeed,yaw,pitch,bank,departureHeight,noticeUntil,scanAt;
        bool departing,previousHud,usingGamepad;
        int landingStep;
        string notice;
        FJLandingSite landingTarget;
        FJLandingSite[] sites;
        readonly RaycastHit[] hits=new RaycastHit[40];
        readonly Collider[] overlaps=new Collider[40];
        // The wheel probes stop the aircraft reaching the ground during free flight.
        static readonly Vector3[] probes={new Vector3(0,1.55f,0),new Vector3(-3.65f,1.4f,.5f),new Vector3(3.65f,1.4f,.5f),new Vector3(0,1.65f,-2.5f),new Vector3(0,1.6f,2.7f),new Vector3(-1.13f,.43f,1.1f),new Vector3(1.13f,.43f,1.1f)};

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState(){PilotedAircraft=null;}
        void Start(){ParkedSite=startingSite;Mode=FlightMode.Parked;yaw=transform.eulerAngles.y;}
        FJHeroMotor FindHero()=>FJWorldSession.Current?FJWorldSession.Current.player:FindFirstObjectByType<FJHeroMotor>();
        public bool CanBoard(FJHeroMotor hero)
        {
            if(pilot||Mode!=FlightMode.Parked||FJWorldDoor.IsTravelling||!hero||!hero.enabled||!hero.Grounded)return false;
            if(FJWorldSession.Current&&(!FJWorldSession.Current.IsReady||FJWorldSession.Current.InsideWorld))return false;
            return boardingPoint&&Vector3.Distance(hero.transform.position,boardingPoint.position)<=boardingRadius;
        }
        bool DoorHasPriority(FJHeroMotor hero)
        {
            float distance=(hero.transform.position-boardingPoint.position).sqrMagnitude;
            foreach(var door in FindObjectsByType<FJWorldDoor>(FindObjectsSortMode.None))
                if(door.isActiveAndEnabled&&door.InRange()&&(hero.transform.position-door.transform.position).sqrMagnitude<distance)return true;
            return false;
        }
        public bool TryBoard(FJHeroMotor hero)
        {
            if(!CanBoard(hero)||PilotedAircraft||DoorHasPriority(hero))return false;
            pilot=hero;PilotedAircraft=this;pilotParent=hero.transform.parent;
            usingGamepad=Keyboard.current?.eKey.wasPressedThisFrame!=true;
            pilotBody=hero.GetComponent<CharacterController>();
            hero.ClearTestInput();hero.enabled=false;previousHud=hero.showControls;hero.showControls=false;pilotBody.enabled=false;
            var animator=hero.animator;
            animator.SetFloat("Speed",0);animator.SetFloat("VerticalSpeed",0);animator.SetBool("Grounded",true);animator.ResetTrigger("Jump");
            animator.Play("PilotIdle",0,0);animator.Update(0);
            var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
            seatedRootOffset=hero.transform.InverseTransformPoint(hips.position);
            // Seat marks the pelvis, so the reused animation fits the original cockpit.
            UpdatePilotPose();
            orbit=hero.movementCamera.GetComponent<FJOrbitCamera>();chase=hero.movementCamera.GetComponent<FJAircraftCamera>();
            orbit.enabled=false;chase.Follow(this);
            targetSpeed=Speed=0;pitch=bank=0;yaw=transform.eulerAngles.y;
            Tell("A bordo · acelera para descolar");return true;
        }
        void UpdatePilotPose()
        {
            if(!pilot)return;
            pilot.transform.rotation=transform.rotation;
            pilot.transform.position=seat.position-transform.rotation*Vector3.Scale(seatedRootOffset,pilot.transform.lossyScale);
        }
        bool Own(Collider c)=>c.transform.IsChildOf(transform)||(pilot&&c.transform.IsChildOf(pilot.transform));
        public bool CanDisembark(out Vector3 position)
        {
            position=Vector3.zero;
            if(!pilot||Mode!=FlightMode.Parked||Speed>.1f||!ParkedSite||!ParkedSite.exitPoint)return false;
            Vector3 desired=ParkedSite.exitPoint.position;
            int count=Physics.RaycastNonAlloc(desired+Vector3.up*.7f,Vector3.down,hits,1.4f,~0,QueryTriggerInteraction.Ignore);
            float best=float.MaxValue;
            for(int i=0;i<count;i++)if(!Own(hits[i].collider)&&hits[i].normal.y>.8f&&hits[i].distance<best)
            {best=hits[i].distance;position=hits[i].point+Vector3.up*.035f;}
            if(best==float.MaxValue)return false;
            count=Physics.OverlapCapsuleNonAlloc(position+Vector3.up*.38f,position+Vector3.up*1.4f,.3f,overlaps,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(!Own(overlaps[i]))return false;
            return true;
        }
        public bool TryDisembark()
        {
            if(!CanDisembark(out var position)){Tell("Aterra antes de sair do avião");return false;}
            var actor=pilot;actor.transform.SetParent(pilotParent,true);
            actor.spawnPosition=position;actor.ResetToSpawn();actor.transform.rotation=ParkedSite.exitPoint.rotation;
            actor.ClearTestInput();actor.showControls=previousHud;actor.enabled=true;
            chase.Release();orbit.yaw=transform.eulerAngles.y+45;orbit.Snap();orbit.enabled=true;
            pilot=null;pilotBody=null;PilotedAircraft=null;targetSpeed=Speed=0;
            Tell("Avião estacionado · podes explorar");return true;
        }
        void Update()
        {
            if(!pilot)
            {
                var hero=FindHero();
                if(FJInteractionInput.Pressed&&CanBoard(hero)&&!DoorHasPriority(hero)&&FJInteractionInput.Consume())TryBoard(hero);
                return;
            }
            if(FJInteractionInput.Pressed&&FJInteractionInput.Consume())TryDisembark();
            if(!pilot)return;
            ReadInput(out float throttle,out float turn,out float lift,out bool land);
            if(Time.time>=scanAt){sites=FindObjectsByType<FJLandingSite>(FindObjectsSortMode.None);scanAt=Time.time+.5f;}
            NearbyLandingSite=FindLandingSite();
            if(land)
            {
                if(Mode==FlightMode.Landing){Mode=FlightMode.Flying;landingTarget=null;Tell("Aterragem cancelada");}
                else if(!TryLand())Tell(Speed>7?"Abranda antes de aterrar":"Aproxima-te de uma lua para aterrar");
            }
            if(Mode==FlightMode.Landing)UpdateLanding(Time.deltaTime);
            else
            {
                if(Mode==FlightMode.Parked&&(throttle>.1f||lift>.1f))
                {Mode=FlightMode.Flying;departureHeight=transform.position.y+4;departing=true;ParkedSite=null;}
                if(Mode==FlightMode.Flying)UpdateFlight(throttle,turn,lift,Time.deltaTime);
            }
            if(propeller&&propellerParts!=null)
                foreach(var part in propellerParts)if(part)part.RotateAround(propeller.position,transform.forward,(Mode==FlightMode.Parked?180:1500+Speed*70)*Time.deltaTime);
        }
        void LateUpdate(){UpdatePilotPose();}
        void ReadInput(out float throttle,out float turn,out float lift,out bool land)
        {
            throttle=turn=lift=0;land=false;var k=Keyboard.current;
            if(k!=null)
            {
                throttle=(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0);
                turn=(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0);
                lift=(k.spaceKey.isPressed?1:0)-(k.leftCtrlKey.isPressed||k.rightCtrlKey.isPressed?1:0);
                land=k.gKey.wasPressedThisFrame;
                if(Mathf.Abs(throttle)+Mathf.Abs(turn)+Mathf.Abs(lift)>0||land)usingGamepad=false;
            }
            var pad=Gamepad.current;
            if(pad!=null&&pad.enabled)
            {
                var stick=pad.leftStick.ReadValue();
                if(Mathf.Abs(turn)<.01f)turn=stick.x;
                if(Mathf.Abs(lift)<.01f)lift=stick.y;
                if(Mathf.Abs(throttle)<.01f)throttle=pad.rightTrigger.ReadValue()-pad.leftTrigger.ReadValue();
                if(pad.buttonSouth.isPressed)lift=Mathf.Max(lift,1);
                land|=pad.buttonEast.wasPressedThisFrame;
                if(stick.sqrMagnitude>.01f||pad.rightTrigger.ReadValue()>.1f||pad.leftTrigger.ReadValue()>.1f||pad.buttonSouth.isPressed||pad.buttonEast.wasPressedThisFrame)usingGamepad=true;
            }
        }
        void UpdateFlight(float throttle,float turn,float lift,float dt)
        {
            targetSpeed=Mathf.Clamp(targetSpeed+throttle*(throttle<0?braking:acceleration)*dt,0,maxSpeed);
            Speed=Mathf.MoveTowards(Speed,targetSpeed,(throttle<0?braking:acceleration)*dt);
            yaw+=turn*turnSpeed*dt;
            float blend=1-Mathf.Exp(-5*dt);
            pitch=Mathf.Lerp(pitch,-lift*12,blend);bank=Mathf.Lerp(bank,-turn*22,blend);
            var rotation=Quaternion.Euler(pitch,yaw,bank);
            float vertical=lift*climbSpeed;
            float forward=Speed;
            if(departing)
            {
                vertical=Mathf.Max(2.8f,vertical);rotation=Quaternion.Euler(-8,yaw,0);
                if(transform.position.y<departureHeight-2)forward=0;
                if(transform.position.y>=departureHeight)departing=false;
            }
            if(transform.position.y>65)vertical=Mathf.Min(0,vertical);
            if(transform.position.y<-15)vertical=Mathf.Max(3,vertical);
            Vector3 delta=(Quaternion.Euler(0,yaw,0)*Vector3.forward*forward+Vector3.up*vertical)*dt;
            MoveAircraft(delta,rotation);
        }
        FJLandingSite FindLandingSite()
        {
            FJLandingSite best=null;float distance=float.MaxValue;
            if(sites==null)return null;
            foreach(var site in sites)
            {
                Vector3 diff=transform.position-site.transform.position;float horizontal=new Vector2(diff.x,diff.z).magnitude;
                if(horizontal<=site.approachRadius&&diff.y>=-1&&diff.y<=18&&horizontal<distance){best=site;distance=horizontal;}
            }
            return best;
        }
        public bool TryLand()
        {
            if(!pilot||Mode!=FlightMode.Flying||Speed>7)return false;
            sites=FindObjectsByType<FJLandingSite>(FindObjectsSortMode.None);NearbyLandingSite=FindLandingSite();
            if(!NearbyLandingSite)return false;
            landingTarget=NearbyLandingSite;landingStep=0;Mode=FlightMode.Landing;departing=false;targetSpeed=0;
            Tell("Aproximação assistida · "+landingTarget.displayName);return true;
        }
        void UpdateLanding(float dt)
        {
            if(!landingTarget){Mode=FlightMode.Flying;return;}
            Vector3 target=landingTarget.transform.position;
            float approachY=target.y+landingTarget.approachHeight;
            Vector3 next;
            if(landingStep==0)
            {
                next=Vector3.MoveTowards(transform.position,new Vector3(transform.position.x,approachY,transform.position.z),climbSpeed*dt);
                if(Mathf.Abs(next.y-approachY)<.01f)landingStep=1;
            }
            else if(landingStep==1)
            {
                next=Vector3.MoveTowards(transform.position,new Vector3(target.x,approachY,target.z),6*dt);
                if(Vector2.Distance(new Vector2(next.x,next.z),new Vector2(target.x,target.z))<.01f)landingStep=2;
            }
            else next=Vector3.MoveTowards(transform.position,target,2.5f*dt);
            yaw=Mathf.MoveTowardsAngle(yaw,landingTarget.transform.eulerAngles.y,80*dt);
            pitch=Mathf.MoveTowards(pitch,0,40*dt);bank=Mathf.MoveTowards(bank,0,60*dt);
            var rotation=Quaternion.Euler(pitch,yaw,bank);
            Speed=Mathf.MoveTowards(Speed,0,braking*dt);
            bool moved=MoveAircraft(next-transform.position,rotation);
            if(!moved){Mode=FlightMode.Flying;landingTarget=null;Tell("Aproximação bloqueada · ganha altura e tenta novamente");return;}
            if(landingStep==2&&Vector3.Distance(transform.position,target)<.015f&&Quaternion.Angle(transform.rotation,landingTarget.transform.rotation)<.1f)
            {
                transform.SetPositionAndRotation(target,landingTarget.transform.rotation);
                ParkedSite=landingTarget;landingTarget=null;Mode=FlightMode.Parked;Speed=targetSpeed=0;
                Tell("Aterragem concluída · E / □ para sair");
            }
        }
        bool MoveAircraft(Vector3 delta,Quaternion rotation)
        {
            // Sweep each part from its old to proposed pose, including rotation at zero airspeed.
            float fraction=1;
            foreach(var local in probes)
            {
                Vector3 from=transform.TransformPoint(local),to=transform.position+delta+rotation*local;
                Vector3 motion=to-from;float length=motion.magnitude;if(length<.00001f)continue;
                float radius=local.y<.5f?.39f:.23f;
                int count=Physics.SphereCastNonAlloc(from,radius,motion/length,hits,length+.025f,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++)if(!Own(hits[i].collider)&&Vector3.Dot(hits[i].normal,motion)<-.0001f)
                    fraction=Mathf.Min(fraction,Mathf.Max(0,hits[i].distance-.025f)/length);
            }
            var before=transform.position;
            transform.SetPositionAndRotation(before+delta*fraction,Quaternion.Slerp(transform.rotation,rotation,fraction));
            DistanceFlown+=Vector3.Distance(before,transform.position);
            if(fraction<.999f)
            {Speed=targetSpeed=0;ObstacleStops++;if(Mode!=FlightMode.Landing)Tell("Obstáculo · sobe ou muda de direção");return false;}
            return true;
        }
        void Tell(string value){notice=value;noticeUntil=Time.time+3;}
        void OnGUI()
        {
            if(!showHud||FJWorldDoor.IsTravelling)return;
            var session=FJWorldSession.Current;if(session&&(!session.IsReady||session.InsideWorld))return;
            var style=new GUIStyle(GUI.skin.box){fontSize=16,alignment=TextAnchor.MiddleCenter};
            if(!pilot)
            {
                var hero=FindHero();if(!hero||!hero.enabled)return;
                if(CanBoard(hero)&&!DoorHasPriority(hero))
                    GUI.Box(new Rect(Screen.width/2f-180,Screen.height-116,360,44),"E / □ · Entrar no avião",style);
                else if(hero.movementCamera&&Vector3.Distance(hero.transform.position,boardingPoint.position)<55)
                {
                    var point=hero.movementCamera.WorldToScreenPoint(boardingPoint.position+Vector3.up*2);
                    if(point.z>0)GUI.Box(new Rect(Mathf.Clamp(point.x-75,8,Screen.width-158),Mathf.Clamp(Screen.height-point.y-25,75,Screen.height-170),150,42),"AVIÃO\n"+Mathf.CeilToInt(Vector3.Distance(hero.transform.position,boardingPoint.position))+" m");
                }
                return;
            }
            bool pad=usingGamepad;
            string controls=pad?"R2 · acelerar   L2 · travar   Analógico esq. · virar / subir / descer\nX · subir / descolar   Analógico dir. · olhar   ○ · aterrar   □ · sair":"W · acelerar   S · travar   A / D · virar   Espaço · subir   Ctrl · descer\nBotão direito · olhar   G · aterrar   E · sair depois de aterrar";
            GUI.Box(new Rect(16,Screen.height-82,Mathf.Min(690,Screen.width-32),66),controls,style);
            string status=Mode==FlightMode.Parked?"ESTACIONADO · "+(ParkedSite?ParkedSite.displayName:""):Mode==FlightMode.Landing?"ATERRAGEM ASSISTIDA":"VOO · "+Mathf.RoundToInt(Speed*3.6f)+" km/h";
            GUI.Box(new Rect(Screen.width/2f-185,16,370,42),status,style);
            if(Mode==FlightMode.Flying&&NearbyLandingSite)
                GUI.Box(new Rect(Screen.width/2f-200,66,400,44),(Speed<=7?"G / ○ · Aterrar em ":"S / L2 · Abranda para aterrar em ")+NearbyLandingSite.displayName,style);
            if(Time.time<noticeUntil)GUI.Box(new Rect(Screen.width/2f-260,Screen.height-142,520,42),notice,style);
            if(sites!=null)
            {
                float top=125;
                foreach(var site in sites)
                {
                    float d=Vector3.Distance(transform.position,site.transform.position);
                    Vector3 relative=Quaternion.Inverse(Quaternion.Euler(0,yaw,0))*(site.transform.position-transform.position);
                    string direction=relative.z<0?"atrás":Mathf.Abs(relative.x)>Mathf.Max(4,relative.z*.12f)?(relative.x>0?"direita":"esquerda"):"em frente";
                    GUI.Box(new Rect(16,top,260,42),site.displayName+" · "+Mathf.CeilToInt(d)+" m · "+direction);top+=48;
                    var screen=pilot.movementCamera.WorldToScreenPoint(site.transform.position+Vector3.up*3);
                    if(screen.z>0&&screen.x>280&&screen.x<Screen.width-80&&screen.y>165&&screen.y<Screen.height-120&&d>25)
                        GUI.Box(new Rect(screen.x-95,Screen.height-screen.y-20,190,42),site.displayName+"\n"+Mathf.CeilToInt(d)+" m");
                }
            }
        }
        void OnDestroy(){if(PilotedAircraft==this)PilotedAircraft=null;}
    }
}
