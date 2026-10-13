using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using FirstJourney;

public static class AJGamepadPlaytest
{
    static async Task Hold(Gamepad pad,GamepadState state,int frames)
    {
        InputSystem.QueueStateEvent(pad,state);
        for(int i=0;i<frames;i++) { pad.MakeCurrent();await Task.Delay(16); }
    }
    public static async Task<object> Controls()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode.");
        Application.runInBackground=true;
        var gameViewType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).First(t=>t!=null);
        var gameView=EditorWindow.GetWindow(gameViewType);gameView.Show();gameView.Focus();await Task.Delay(100);
        var actor=GameObject.Find("HeroAJ");var motor=actor.GetComponent<FJHeroMotor>();
        var orbit=Camera.main.GetComponent<FJOrbitCamera>();
        var previous=Gamepad.current;
        var physical=Gamepad.all.Select(p=>new {name=p.displayName,layout=p.layout}).ToArray();
        var enabledPhysical=Gamepad.all.Where(p=>p.enabled).ToArray();
        foreach(var device in enabledPhysical)InputSystem.DisableDevice(device);
        var pad=InputSystem.AddDevice<Gamepad>();
        try
        {
            pad.MakeCurrent();motor.ResetToSpawn();motor.ClearTestInput();
            int initialRespawns=motor.RespawnCount;
            await Hold(pad,new GamepadState(),15);
            Vector3 start=actor.transform.position;
            Vector3 forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();
            await Hold(pad,new GamepadState{leftStick=Vector2.up,rightTrigger=1},30);
            Vector3 moved=actor.transform.position-start;moved.y=0;
            float speed=motor.animator.GetFloat("Speed");
            await Hold(pad,new GamepadState(),25);
            float yaw=orbit.yaw;
            await Hold(pad,new GamepadState{rightStick=new Vector2(.6f,0)},25);
            float yawChange=Mathf.Abs(orbit.yaw-yaw);
            float distance=orbit.distance;
            await Hold(pad,new GamepadState().WithButton(GamepadButton.RightShoulder),20);
            float zoomChange=distance-orbit.distance;
            await Hold(pad,new GamepadState(),25);
            float floor=actor.transform.position.y,peak=floor;
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));
            for(int i=0;i<105;i++)
            {
                pad.MakeCurrent();await Task.Delay(16);peak=Mathf.Max(peak,actor.transform.position.y);
                if(i==3)InputSystem.QueueStateEvent(pad,new GamepadState());
            }
            bool passed=moved.magnitude>.4f && speed>3 && Vector3.Dot(moved.normalized,forward)>.8f && yawChange>5 && zoomChange>.2f && peak-floor>.65f && motor.Grounded && motor.RespawnCount==initialRespawns;
            var result=new{passed,testDevice="Virtual Gamepad",physicalDevices=physical,moveDistance=moved.magnitude,cameraDirectionDot=Vector3.Dot(moved.normalized,forward),sprintSpeed=speed,cameraYawChange=yawChange,zoomChange,jumpRise=peak-floor,grounded=motor.Grounded,respawnsDuringTest=motor.RespawnCount-initialRespawns};
            File.WriteAllText("Docs/HeroAJ_Gamepad_Test.json",JsonConvert.SerializeObject(result,Formatting.Indented));
            if(!passed)throw new Exception(JsonConvert.SerializeObject(result));return result;
        }
        finally
        {
            InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();InputSystem.RemoveDevice(pad);
            foreach(var device in enabledPhysical)InputSystem.EnableDevice(device);
            if(previous!=null)previous.MakeCurrent();motor.SetTestInput(Vector3.zero);
        }
    }
}
