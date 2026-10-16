using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using FirstJourney;
public static class AircraftPlaytest
{
    static async Task Frames(int n){int f=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(12);while(Time.frameCount-f<n){if(DateTime.UtcNow>end)throw new Exception("Game frames stopped");await Task.Delay(16);}}
    static void Progress(string stage,object data=null){File.WriteAllText("Docs/Aviao_Progresso.json",JsonConvert.SerializeObject(new{stage,data,utc=DateTime.UtcNow},Formatting.Indented));}
    static async Task Until(Func<bool> predicate,string message,int seconds=25)
    {var end=DateTime.UtcNow.AddSeconds(seconds);while(!predicate()){if(DateTime.UtcNow>end)throw new Exception(message);await Task.Delay(16);}await Frames(4);}
    static async Task Walk(FJHeroMotor actor,Vector3 target)
    {
        var end=DateTime.UtcNow.AddSeconds(20);
        while(true){var d=target-actor.transform.position;d.y=0;if(d.magnitude<.14f)break;if(DateTime.UtcNow>end)throw new Exception("Walking blocked "+actor.transform.position+" -> "+target);actor.SetTestInput(Vector3.ClampMagnitude(d*2,1),true);await Task.Delay(16);}
        actor.SetTestInput(Vector3.zero);await Frames(12);
    }
    static async Task KeyPress(Keyboard keyboard,Key key){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));await Frames(2);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(3);}
    static async Task PadPress(Gamepad pad,GamepadButton key){pad.MakeCurrent();InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(key));await Frames(2);InputSystem.QueueStateEvent(pad,new GamepadState());await Frames(3);}
    static async Task Capture(string name)
    {string path="Assets/_FirstJourney/Review/"+name+".png";if(File.Exists(path))File.Delete(path);await Frames(10);ScreenCapture.CaptureScreenshot(path);await Until(()=>File.Exists(path),"Capture missing");}
    static void Pad(Gamepad pad,float turn=0,float lift=0,float throttle=0,float brake=0)
    {pad.MakeCurrent();InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(turn,lift),rightTrigger=throttle,leftTrigger=brake});}
    static async Task Navigate(FJAircraftController plane,FJLandingSite destination,Gamepad pad,bool keyboardFlight,Keyboard keyboard)
    {
        var end=DateTime.UtcNow.AddSeconds(65);bool captured=false;
        // A test pilot steers with the actual input devices; it never sets the aircraft transform.
        while(true)
        {
            var delta=destination.transform.position-plane.transform.position;float horizontal=new Vector2(delta.x,delta.z).magnitude;
            if(horizontal<18&&plane.Speed<6)break;
            if(DateTime.UtcNow>end)throw new Exception("Flight timed out "+plane.transform.position+" -> "+destination.name+" speed="+plane.Speed+" stops="+plane.ObstacleStops);
            float heading=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
            float error=Mathf.DeltaAngle(plane.transform.eulerAngles.y,heading);
            float turn=Mathf.Clamp(error/30,-1,1);
            float lift=Mathf.Clamp((9-plane.transform.position.y)*.65f,-1,1);
            float desired=horizontal>38?(Mathf.Abs(error)<35?13:3):horizontal>20?6:0;
            float throttle=plane.Speed<desired-.25f?1:0,brake=plane.Speed>desired+.25f?1:0;
            if(keyboardFlight)
            {
                var keys=new List<Key>();if(throttle>0)keys.Add(Key.W);if(brake>0)keys.Add(Key.S);
                if(error>1)keys.Add(Key.D);else if(error< -1)keys.Add(Key.A);
                if(lift>.2f)keys.Add(Key.Space);else if(lift<-.2f)keys.Add(Key.LeftCtrl);
                keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));
            }
            else Pad(pad,turn,lift,throttle,brake);
            if(!captured&&horizontal<110&&horizontal>70)
            {captured=true;await Capture(keyboardFlight?"Aviao_Voo_Teclado":"Aviao_Voo_Comando");}
            await Frames(2);
        }
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());Pad(pad,brake:1);await Frames(20);Pad(pad);
        Progress("Final approach",new{position=plane.transform.position.ToString(),destination=destination.name,speed=plane.Speed});
        if(keyboardFlight)await KeyPress(keyboard,Key.G);else await PadPress(pad,GamepadButton.East);
        await Until(()=>plane.Mode==FJAircraftController.FlightMode.Parked,"Landing did not finish: "+plane.transform.position+" mode="+plane.Mode+" stops="+plane.ObstacleStops,35);
        if(plane.ParkedSite!=destination)throw new Exception("Wrong landing site");
        if(Vector3.Distance(plane.transform.position,destination.transform.position)>.02f)throw new Exception("Landing pose mismatch");
    }
    public static async Task<object> Journey()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play");Application.runInBackground=true;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).First(t=>t!=null);var view=EditorWindow.GetWindow(type);view.Show();view.Focus();
        await Until(()=>FJWorldSession.Current&&FJWorldSession.Current.IsReady,"No session");
        var session=FJWorldSession.Current;var hero=session.player;var plane=UnityEngine.Object.FindFirstObjectByType<FJAircraftController>();
        var source=plane.startingSite;var target=UnityEngine.Object.FindObjectsByType<FJLandingSite>(FindObjectsSortMode.None).First(s=>s!=source);
        int heroId=hero.GetInstanceID(),planeId=plane.GetInstanceID(),cameraId=session.playerCamera.GetInstanceID();
        var physical=InputSystem.devices.Where(d=>d.enabled&&(d is Keyboard||d is Gamepad||d is Mouse)).ToArray();var oldKeyboard=Keyboard.current;var oldPad=Gamepad.current;
        foreach(var d in physical)InputSystem.DisableDevice(d);var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
        var started=DateTime.UtcNow;int initialRespawns=hero.RespawnCount;var stages=new List<object>();bool distantBoardBlocked=false,airborneExitBlocked=false,persisted=false,unsafeExitBlocked=false,obstacleBlocked=false;
        try
        {
            Progress("Walk to plane");hero.SetTestInput(Vector3.zero);await Frames(20);
            distantBoardBlocked=!plane.TryBoard(hero);if(!distantBoardBlocked)throw new Exception("Can board remotely");
            var route=new[]{new Vector3(2.9f,.22f,6.9f),new Vector3(5.1f,.22f,6.8f),new Vector3(6.3f,.22f,6.3f),new Vector3(7.2f,.22f,5.3f),new Vector3(8,.22f,4.5f),new Vector3(18.4f,.22f,4.5f),plane.boardingPoint.position};
            foreach(var waypoint in route)await Walk(hero,waypoint);
            hero.ClearTestInput();await KeyPress(keyboard,Key.E);await Until(()=>plane.IsPiloted,"Could not board with E");
            if(hero.enabled||hero.GetComponent<CharacterController>().enabled||session.playerCamera.GetComponent<FJOrbitCamera>().enabled)throw new Exception("On-foot controls active while piloting");
            await Capture("Aviao_Piloto_Cockpit");
            Progress("Outbound keyboard flight");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));await Until(()=>plane.transform.position.y>5,"No takeoff");InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            await KeyPress(keyboard,Key.E);airborneExitBlocked=plane.IsPiloted&&!hero.enabled;if(!airborneExitBlocked)throw new Exception("Airborne exit allowed");
            Progress("Collision sweep check");
            var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.name="TestOnly_FlightBarrier";barrier.GetComponent<Renderer>().enabled=false;
            barrier.transform.position=plane.transform.position+Vector3.forward*7+Vector3.up*1.5f;barrier.transform.localScale=new Vector3(20,10,1);Physics.SyncTransforms();
            try
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
                await Until(()=>plane.ObstacleStops>0,"Aircraft passed through test obstacle",10);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(4);
                obstacleBlocked=plane.Speed<.1f&&plane.transform.position.z<barrier.transform.position.z-2.6f;
                if(!obstacleBlocked)throw new Exception("Collision did not stop before obstacle");
            }
            finally{UnityEngine.Object.Destroy(barrier);await Frames(2);}
            Progress("Outbound keyboard flight");
            await Navigate(plane,target,pad,true,keyboard);
            float outbound=plane.DistanceFlown;
            var goodExit=target.exitPoint.position;target.exitPoint.position+=Vector3.right*100;
            try{unsafeExitBlocked=!plane.TryDisembark()&&plane.IsPiloted;}
            finally{target.exitPoint.position=goodExit;}
            if(!unsafeExitBlocked)throw new Exception("Unsafe egress allowed");
            await KeyPress(keyboard,Key.E);await Until(()=>!plane.IsPiloted,"Could not disembark");hero.SetTestInput(Vector3.zero);await Frames(20);
            if(!hero.Grounded)throw new Exception("No ground after landing");
            stages.Add(new{stage="outbound",passed=true,distance=outbound,position=plane.transform.position.ToString(),landedAt=plane.ParkedSite.displayName,grounded=hero.Grounded});
            await Capture("Aviao_Aterrado_Asteroide");
            Progress("Walk to house and enter world");
            // Park beside the house and walk behind the tail to the front door.
            foreach(var waypoint in new[]{new Vector3(-1.175f,.04f,185.75f),new Vector3(-1.175f,.04f,182.15f)})await Walk(hero,waypoint);
            hero.ClearTestInput();var parked=plane.transform.position;await KeyPress(keyboard,Key.E);
            await Until(()=>session.InsideWorld&&!FJWorldDoor.IsTravelling,"Could not enter world",40);hero.SetTestInput(Vector3.zero);
            bool hidden=!plane.gameObject.activeInHierarchy;
            // The interior route has separate traversal coverage. Isolate the return door here.
            session.PlacePlayer("Aproximacao_Porta_Regiao_1",270);hero.SetTestInput(Vector3.zero);await Frames(25);hero.ClearTestInput();await PadPress(pad,GamepadButton.West);
            await Until(()=>!session.InsideWorld&&!FJWorldDoor.IsTravelling,"No return from world",40);hero.SetTestInput(Vector3.zero);await Frames(20);
            persisted=plane&&plane.GetInstanceID()==planeId&&Vector3.Distance(plane.transform.position,parked)<.001f;
            if(!hidden||!persisted||!hero.Grounded)throw new Exception("Parked plane or return context lost");
            stages.Add(new{stage="world visit",passed=true,exteriorHidden=hidden,planePoseRetained=persisted,grounded=hero.Grounded});
            Progress("Walk back to plane");
            foreach(var waypoint in new[]{new Vector3(-1.175f,.04f,185.75f),plane.boardingPoint.position})await Walk(hero,waypoint);
            hero.ClearTestInput();await PadPress(pad,GamepadButton.West);await Until(()=>plane.IsPiloted,"Could not board with gamepad West");
            Progress("Return gamepad flight");Pad(pad,lift:1);await Until(()=>plane.transform.position.y>5,"No gamepad takeoff");Pad(pad);
            await Navigate(plane,source,pad,false,keyboard);
            await PadPress(pad,GamepadButton.West);await Until(()=>!plane.IsPiloted,"Could not leave at base");hero.SetTestInput(Vector3.zero);await Frames(25);
            if(!hero.Grounded)throw new Exception("Base egress not grounded");
            stages.Add(new{stage="return",passed=true,totalDistance=plane.DistanceFlown,position=plane.transform.position.ToString(),landedAt=plane.ParkedSite.displayName,grounded=hero.Grounded});
            int cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled);
            bool same=hero.GetInstanceID()==heroId&&session.playerCamera.GetInstanceID()==cameraId;
            int listeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled);
            int motors=UnityEngine.Object.FindObjectsByType<FJHeroMotor>(FindObjectsSortMode.None).Count(h=>h.enabled);
            int unexpectedRespawns=hero.RespawnCount-initialRespawns-5;
            if(!same||cameras!=1||listeners!=1||motors!=1||unexpectedRespawns!=0)throw new Exception("Session counts invalid: respawns="+unexpectedRespawns);
            var result=new{passed=true,elapsedSeconds=(DateTime.UtcNow-started).TotalSeconds,distantBoardBlocked,airborneExitBlocked,unsafeExitBlocked,obstacleBlocked,planePersistsDuringWorldVisit=persisted,sameHeroAndCamera=same,activeCameras=cameras,activeListeners=listeners,activeOnFootMotors=motors,unexpectedRespawns,plane.ObstacleStops,totalFlightDistance=plane.DistanceFlown,aircraftTransformTeleportedByTest=false,virtualKeyboardOutbound=true,virtualGamepadReturn=true,interiorReturnDoorApproachPlacedByTest=true,stages};
            File.WriteAllText("Docs/Aviao_Playtest.json",JsonConvert.SerializeObject(result,Formatting.Indented));Progress("Complete",result);return result;
        }
        catch(Exception ex){Progress("Failed",new{error=ex.ToString(),planePosition=plane?plane.transform.position.ToString():"missing",heroPosition=hero?hero.transform.position.ToString():"missing",mode=plane?plane.Mode.ToString():"missing"});throw;}
        finally
        {
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);
            if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldPad!=null&&oldPad.added)oldPad.MakeCurrent();if(hero)hero.ClearTestInput();
        }
    }
}
