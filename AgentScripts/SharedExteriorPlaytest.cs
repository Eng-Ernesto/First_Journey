using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using FirstJourney;
public static class SharedExteriorPlaytest
{
    static async Task Frames(int n)
    {int f=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(10);while(Time.frameCount-f<n){if(DateTime.UtcNow>end)throw new Exception("Game View did not advance");await Task.Delay(16);}}
    static async Task Until(Func<bool> condition,string error)
    {var end=DateTime.UtcNow.AddSeconds(20);while(!condition()){if(DateTime.UtcNow>end)throw new Exception(error);await Task.Delay(20);}await Frames(20);}
    public static object ColdStart()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
        EditorSceneManager.OpenScene(FJWorldSession.ExteriorPath,OpenSceneMode.Single);
        return new{loadedScenes=SceneManager.sceneCount,path=SceneManager.GetActiveScene().path};
    }
    public static async Task<object> RoundTrips()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode");Application.runInBackground=true;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).First(t=>t!=null);var view=EditorWindow.GetWindow(type);view.Show();view.Focus();
        await Until(()=>FJWorldSession.Current&&FJWorldSession.Current.IsReady,"Exterior did not initialize");
        var session=FJWorldSession.Current;var actor=session.player;actor.SetTestInput(Vector3.zero);await Frames(25);
        int actorId=actor.GetInstanceID(),cameraId=session.playerCamera.GetInstanceID();bool initialGround=actor.Grounded;
        if(!initialGround)throw new Exception("Base arrival is not grounded");
        var baseScene=SceneManager.GetSceneByPath(FJWorldSession.BasePath);var asteroidScene=SceneManager.GetSceneByPath(FJWorldSession.AsteroidPath);
        var plane=GameObject.Find("Aviao_Original_SemPersonagem").transform;int planeId=plane.GetInstanceID();var originalPlane=plane.position;var originalRotation=plane.rotation;
        var physical=InputSystem.devices.Where(d=>d.enabled&&(d is Keyboard||d is Gamepad)).ToArray();var oldKeyboard=Keyboard.current;var oldPad=Gamepad.current;foreach(var d in physical)InputSystem.DisableDevice(d);var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
        var reports=new System.Collections.Generic.List<object>();
        try
        {
            // Simulate an aircraft already parked at the destination. This is a state test, not flight.
            var landing=GameObject.Find("Chegada_Do_Aviao_Reserva").transform;
            plane.SetPositionAndRotation(landing.position+Vector3.up*.2f,Quaternion.Euler(0,180,0));var parked=plane.position;
            session.PlacePlayer("Regresso_Do_Reino",180);actor.SetTestInput(Vector3.zero);await Frames(20);
            for(int region=1;region<=3;region++)
            {
                actor.ClearTestInput();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));await Frames(2);InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                await Until(()=>session.InsideWorld&&!FJWorldDoor.IsTravelling,"Could not enter world");
                actor.SetTestInput(Vector3.zero);
                bool insideGround=actor.Grounded;bool exteriorHidden=!plane.gameObject.activeInHierarchy;
                bool noInteriorActor=SceneManager.GetSceneByPath(FJWorldSession.WorldPath).GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<FJHeroMotor>(true)).Count()==0;
                if(!insideGround||!exteriorHidden||!noInteriorActor)throw new Exception("Interior context invalid");
                session.PlacePlayer("Aproximacao_Porta_Regiao_"+region,270);actor.SetTestInput(Vector3.zero);await Frames(30);actor.ClearTestInput();
                if(region==3){pad.MakeCurrent();InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));}else InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));
                await Frames(2);InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(pad,new GamepadState());
                await Until(()=>!session.InsideWorld&&!FJWorldDoor.IsTravelling,"Could not return from region "+region);actor.SetTestInput(Vector3.zero);
                bool retained=plane&&plane.GetInstanceID()==planeId&&Vector3.Distance(plane.position,parked)<.001f;
                bool sameActor=actor.GetInstanceID()==actorId&&session.playerCamera.GetInstanceID()==cameraId;
                bool sameExterior=SceneManager.GetSceneByPath(FJWorldSession.BasePath).handle==baseScene.handle&&SceneManager.GetSceneByPath(FJWorldSession.AsteroidPath).handle==asteroidScene.handle;
                bool worldUnloaded=!SceneManager.GetSceneByPath(FJWorldSession.WorldPath).isLoaded;
                bool atDestination=Vector3.Distance(actor.transform.position,new Vector3(-1.175f,.04f,182.15f))<.3f;
                float planeDistance=Vector3.Distance(actor.transform.position,plane.position);
                int motors=UnityEngine.Object.FindObjectsByType<FJHeroMotor>(FindObjectsSortMode.None).Length;
                int cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled);
                var door=UnityEngine.Object.FindFirstObjectByType<FJWorldDoor>();bool closed=Quaternion.Angle(door.doorLeaf.localRotation,Quaternion.identity)<.1f;
                bool passed=retained&&sameActor&&sameExterior&&worldUnloaded&&atDestination&&actor.Grounded&&motors==1&&cameras==1&&planeDistance<5&&closed;
                reports.Add(new{region,passed,initialWorldGrounded=insideGround,exteriorHidden,noInteriorActor,planeInstanceAndPoseRetained=retained,sameActorAndCamera=sameActor,sameExteriorSceneInstances=sameExterior,worldUnloaded,returnedToDestinationAsteroid=atDestination,grounded=actor.Grounded,planeDistance,activeMotors=motors,activeCameras=cameras,doorClosedForNextTrip=closed,virtualGamepadReturn=region==3});
                if(!passed)throw new Exception(JsonConvert.SerializeObject(reports));
            }
            var result=new{passed=true,coldStartBaseGrounded=initialGround,exteriorSceneCount=3,baseAndDestinationDistance=180,flightSimulatedByTestOnly=true,flightImplemented=false,roundTrips=reports};
            File.WriteAllText("Docs/Exterior_Playtest.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
        }
        finally
        {
            if(plane)plane.SetPositionAndRotation(originalPlane,originalRotation);
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldPad!=null&&oldPad.added)oldPad.MakeCurrent();
            if(actor){actor.ClearTestInput();}
        }
    }
}
