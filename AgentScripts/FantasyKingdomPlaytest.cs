using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using FirstJourney;
public static class FantasyKingdomPlaytest
{
    static FJHeroMotor Hero()=>UnityEngine.Object.FindFirstObjectByType<FJHeroMotor>();
    static async Task Frames(int count)
    {int frame=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(15);while(Time.frameCount-frame<count){if(DateTime.UtcNow>end)throw new Exception("Game View not advancing");await Task.Delay(16);}}
    static async Task WaitScene(string name)
    {var end=DateTime.UtcNow.AddSeconds(40);while(SceneManager.GetActiveScene().name!=name||FJWorldDoor.IsTravelling){if(DateTime.UtcNow>end)throw new Exception("Transition did not complete: "+SceneManager.GetActiveScene().name);await Task.Delay(50);}await Frames(25);}
    static async Task Walk(Vector3 target)
    {var m=Hero();var end=DateTime.UtcNow.AddSeconds(12);while(true){var d=target-m.transform.position;d.y=0;if(d.magnitude<.18f)break;if(DateTime.UtcNow>end)throw new Exception("Walk blocked at "+m.transform.position+" toward "+target);m.SetTestInput(Vector3.ClampMagnitude(d*2,1),true);await Task.Delay(16);}m.SetTestInput(Vector3.zero);await Frames(10);}
    public static async Task<object> RoundTrip()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode");
        Application.runInBackground=true;
        var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).First(t=>t!=null);var view=EditorWindow.GetWindow(t);view.Show();view.Focus();
        var devices=InputSystem.devices.Where(d=>d.enabled&&(d is Keyboard||d is Gamepad)).ToArray();
        var oldKeyboard=Keyboard.current;var oldPad=Gamepad.current;
        foreach(var d in devices)InputSystem.DisableDevice(d);
        var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
        try
        {
            if(SceneManager.GetActiveScene().name!="FirstJourney_Asteroide01")throw new Exception("Start in the asteroid");
            Hero().ResetToSpawn();Hero().SetTestInput(Vector3.zero);await Frames(20);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));await Frames(2);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(4);
            bool rejectedAtDistance=SceneManager.GetActiveScene().name=="FirstJourney_Asteroide01"&&!FJWorldDoor.IsTravelling;
            if(!rejectedAtDistance)throw new Exception("Door was usable too far away");
            await Walk(new Vector3(-1.175f,0,2.15f));Hero().ClearTestInput();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));await Frames(2);InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            await WaitScene("FirstJourney_Reino01_FantasyKingdom");
            var m=Hero();m.SetTestInput(Vector3.zero);int respawns=m.RespawnCount;
            Vector3 arrival=m.transform.position;bool arrivalGrounded=m.Grounded;
            if(!arrivalGrounded||Vector3.Distance(arrival,new Vector3(4,.24f,6))>.5f)throw new Exception("Invalid realm arrival: "+arrival);
            float floor=arrival.y,peak=floor;bool jump=false,fall=false,land=false;
            m.SetTestInput(Vector3.zero,false,true);
            for(int i=0;i<90;i++){await Frames(1);peak=Mathf.Max(peak,m.transform.position.y);var state=m.animator.GetCurrentAnimatorStateInfo(0);jump|=state.IsName("Jump");fall|=state.IsName("Fall");land|=state.IsName("Land");}
            await Walk(new Vector3(4,0,8.4f));
            bool realmGrounded=m.Grounded;var departure=m.transform.position;
            if(peak-floor<.7f||!realmGrounded||m.RespawnCount!=respawns)throw new Exception("Realm movement/jump failed");
            m.ClearTestInput();
            pad.MakeCurrent();InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));await Frames(2);InputSystem.QueueStateEvent(pad,new GamepadState());
            await WaitScene("FirstJourney_Asteroide01");Hero().SetTestInput(Vector3.zero);
            var returned=Hero().transform.position;bool grounded=Hero().Grounded;
            await Frames(70);
            bool stable=SceneManager.GetActiveScene().name=="FirstJourney_Asteroide01"&&!FJWorldDoor.IsTravelling;
            bool passed=grounded&&stable&&Vector3.Distance(returned,new Vector3(-1.175f,.04f,2.15f))<.3f&&jump&&fall&&land;
            var report=new{passed,keyboardEntry=true,virtualGamepadReturn=true,rejectedAtDistance,arrival=arrival.ToString("F3"),arrivalGrounded,realmDeparture=departure.ToString("F3"),realmGrounded,jumpRise=peak-floor,sawJump=jump,sawFall=fall,sawLand=land,unexpectedRealmRespawns=m?m.RespawnCount-respawns:0,returned=returned.ToString("F3"),returnedGrounded=grounded,noAutomaticBounce=stable,activeMotors=UnityEngine.Object.FindObjectsByType<FJHeroMotor>(FindObjectsSortMode.None).Length,activeCameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled),physicalDevicesIsolated=devices.Select(d=>d.displayName).ToArray()};
            File.WriteAllText("Docs/FantasyKingdom_Playtest.json",JsonConvert.SerializeObject(report,Formatting.Indented));
            if(!passed)throw new Exception(JsonConvert.SerializeObject(report));return report;
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);
            foreach(var d in devices)if(d.added)InputSystem.EnableDevice(d);
            if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldPad!=null&&oldPad.added)oldPad.MakeCurrent();
            if(Hero())Hero().ClearTestInput();
        }
    }
}
