using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using FirstJourney;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class YoungPilotPlaytest
{
    public static async Task<object> CaptureActualGame()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode.");
        Application.runInBackground=true;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).First(t=>t!=null);
        var view=EditorWindow.GetWindow(type);view.Show();view.Focus();
        var actor=GameObject.Find("HeroSidekick");actor.GetComponent<FJHeroMotor>().SetTestInput(Vector3.zero);
        var orbit=Camera.main.GetComponent<FJOrbitCamera>();orbit.yaw=actor.transform.eulerAngles.y+180;orbit.pitch=18;orbit.distance=3.6f;orbit.Snap();
        await Task.Delay(500);
        const string path="Assets/_FirstJourney/Review/PilotoAventureiro_GameView_v02.png";
        if(File.Exists(path))File.Delete(path);
        ScreenCapture.CaptureScreenshot(path);
        for(int i=0;i<50&&!File.Exists(path);i++)await Task.Delay(100);
        if(!File.Exists(path))throw new Exception("Game View capture did not finish.");
        return new{path,bytes=new FileInfo(path).Length};
    }
    public static async Task<object> TraverseAndJump()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode.");
        Application.runInBackground=true;
        var actor=GameObject.Find("HeroSidekick");
        var motor=actor.GetComponent<FJHeroMotor>();
        var animator=motor.animator;
        if(!animator.avatar.isHuman || !animator.avatar.isValid)throw new Exception("Humanoid Avatar invalid at runtime.");
        var foot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        if(!foot)throw new Exception("Left foot bone is missing.");
        var start=motor.spawnPosition;
        var clock=Stopwatch.StartNew();int samples=0,groundedSamples=0;
        float minY=100,maxSpeed=0; bool sawFall=false,sawLand=false,sawJump=false;
        float maxBoneMotion=0;Quaternion lastFoot=foot.localRotation;
        try
        {
            motor.SetTestInput(Vector3.zero);
            for(int i=0;i<20;i++)await Task.Delay(16);
            var route=new[]{new Vector3(2.9f,.22f,6.9f),new Vector3(5.1f,.22f,6.8f),new Vector3(6.3f,.22f,6.3f),new Vector3(7.2f,.22f,5.3f),new Vector3(8,.22f,4.5f),new Vector3(17.45f,.22f,4.5f)};
            foreach(var target in route.Concat(route.Reverse().Skip(1)).Concat(new[]{start}))
            {
                int budget=1500;
                while(true)
                {
                    Vector3 delta=target-actor.transform.position;delta.y=0;
                    if(delta.magnitude<.13f)break;
                    if(--budget<=0 || clock.Elapsed.TotalSeconds>65)throw new Exception("AJ traversal blocked: "+actor.transform.position+" => "+target);
                    motor.SetTestInput(Vector3.ClampMagnitude(delta*2,1),true);
                    await Task.Delay(16);
                    samples++;if(motor.Grounded)groundedSamples++;
                    minY=Mathf.Min(minY,actor.transform.position.y);
                    maxSpeed=Mathf.Max(maxSpeed,animator.GetFloat("Speed"));
                    maxBoneMotion=Mathf.Max(maxBoneMotion,Quaternion.Angle(lastFoot,foot.localRotation));lastFoot=foot.localRotation;
                    if(actor.transform.position.y<.1f || motor.RespawnCount!=0)throw new Exception("AJ fell off the traversal route.");
                }
            }
            motor.SetTestInput(Vector3.zero);
            for(int i=0;i<30;i++)await Task.Delay(16);
            float floor=actor.transform.position.y,peak=floor;
            motor.SetTestInput(Vector3.zero,false,true);
            for(int i=0;i<110;i++)
            {
                await Task.Delay(16);peak=Mathf.Max(peak,actor.transform.position.y);
                var state=animator.GetCurrentAnimatorStateInfo(0);
                sawJump|=state.IsName("Jump");sawFall|=state.IsName("Fall");sawLand|=state.IsName("Land");
            }
            if(peak-floor<.65f || !motor.Grounded || !sawJump || !sawFall || !sawLand || maxBoneMotion<.1f || maxSpeed<3)
                throw new Exception("Movement/jump checks failed: peak="+(peak-floor)+" grounded="+motor.Grounded+" states="+sawJump+"/"+sawFall+"/"+sawLand+" bone="+maxBoneMotion);
            var result=new{passed=true,character="Piloto aventureiro v02",roundTrip=true,samples,groundedSamples,minFootHeight=minY,maxSpeed,maxFootRotationChange=maxBoneMotion,jumpRise=peak-floor,sawJump,sawFall,sawLand,respawns=motor.RespawnCount,elapsedSeconds=clock.Elapsed.TotalSeconds,finalPosition=actor.transform.position.ToString(),avatarHuman=animator.isHuman};
            File.WriteAllText("Docs/Sidekick_v02_Playtest.json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
        }
        finally {motor.SetTestInput(Vector3.zero);motor.ClearTestInput();}
    }
    public static void ViewHero()
    {
        var actor=GameObject.Find("HeroSidekick");
        var camera=Camera.main;var orbit=camera.GetComponent<FJOrbitCamera>();
        orbit.yaw=actor.transform.eulerAngles.y+150;orbit.pitch=21;orbit.distance=3.8f;orbit.Snap();
        var motor=actor.GetComponent<FJHeroMotor>();motor.SetTestInput(Vector3.zero);
    }
    public static async Task<object> KeyboardControls()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode.");
        Application.runInBackground=true;
        var actor=GameObject.Find("HeroSidekick");var motor=actor.GetComponent<FJHeroMotor>();
        var gameViewType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).First(t=>t!=null);
        var gameView=EditorWindow.GetWindow(gameViewType);gameView.Show();gameView.Focus();
        await Task.Delay(100);
        var previous=Keyboard.current;
        var enabledPhysical=InputSystem.devices.OfType<Keyboard>().Where(k=>k.enabled).ToArray();
        foreach(var device in enabledPhysical)InputSystem.DisableDevice(device);
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            keyboard.MakeCurrent();motor.ResetToSpawn();motor.ClearTestInput();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            for(int i=0;i<15;i++) { keyboard.MakeCurrent(); await Task.Delay(16); }
            Vector3 start=actor.transform.position;
            Vector3 forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));
            for(int i=0;i<30;i++) { keyboard.MakeCurrent(); await Task.Delay(16); }
            Vector3 moved=actor.transform.position-start;moved.y=0;
            float speed=motor.animator.GetFloat("Speed");
            var result=new{passed=moved.magnitude>.4f && Vector3.Dot(moved.normalized,forward)>.8f && speed>3,keys="W + LeftShift",distance=moved.magnitude,cameraDirectionDot=Vector3.Dot(moved.normalized,forward),speed};
            File.WriteAllText("Docs/Sidekick_v02_Keyboard_Test.json",JsonConvert.SerializeObject(result,Formatting.Indented));
            if(!result.passed)throw new Exception(JsonConvert.SerializeObject(result));return result;
        }
        finally
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();InputSystem.RemoveDevice(keyboard);
            foreach(var device in enabledPhysical)InputSystem.EnableDevice(device);
            if(previous!=null)previous.MakeCurrent();motor.SetTestInput(Vector3.zero);
        }
    }
}



