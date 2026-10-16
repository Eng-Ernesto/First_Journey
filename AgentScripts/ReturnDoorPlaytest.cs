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
public static class ReturnDoorPlaytest
{
    static FJHeroMotor Hero()=>UnityEngine.Object.FindFirstObjectByType<FJHeroMotor>();
    static async Task Frames(int n){int f=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(10);while(Time.frameCount-f<n){if(DateTime.UtcNow>end)throw new Exception("No gameplay frames");await Task.Delay(16);}}
    static async Task Ready(string name){var end=DateTime.UtcNow.AddSeconds(30);while(SceneManager.GetActiveScene().name!=name||FJWorldDoor.IsTravelling){if(DateTime.UtcNow>end)throw new Exception("Scene transition failed");await Task.Delay(20);}await Frames(15);}
    static async Task Walk(Vector3 target)
    {
        var m=Hero();var end=DateTime.UtcNow.AddSeconds(8);
        while(true){var d=target-m.transform.position;d.y=0;if(d.magnitude<.2f)break;if(DateTime.UtcNow>end)throw new Exception("Door approach blocked: "+m.transform.position+" => "+target);m.SetTestInput(Vector3.ClampMagnitude(d*2,1),true);await Task.Delay(16);}
        m.SetTestInput(Vector3.zero);await Frames(10);
    }
    static Vector3[] DoorRoute()
    {
        // Test-only navigation around the stalls; no controller or collision settings change.
        const int width=29,depth=37;var points=new Vector3[width*depth];var clear=new bool[points.Length];
        var actor=Hero().transform;
        for(int z=0;z<depth;z++)for(int x=0;x<width;x++)
        {
            int id=z*width+x;float wx=1+x*.5f,wz=3+z*.5f;RaycastHit h;
            if(!Physics.Raycast(new Vector3(wx,4,wz),Vector3.down,out h,6,~0,QueryTriggerInteraction.Ignore))continue;
            points[id]=h.point;
            clear[id]=h.normal.y>.7f&&!Physics.OverlapCapsule(h.point+Vector3.up*.36f,h.point+Vector3.up*1.45f,.30f,~0,QueryTriggerInteraction.Ignore).Any(c=>c.transform.root!=actor.root);
        }
        int start=Array.IndexOf(points,points.Where((p,i)=>clear[i]).OrderBy(p=>(p-actor.position).sqrMagnitude).First());
        int goal=Array.IndexOf(points,points.Where((p,i)=>clear[i]).OrderBy(p=>(p-new Vector3(8.3f,.4f,17.2f)).sqrMagnitude).First());
        var prev=Enumerable.Repeat(-2,points.Length).ToArray();prev[start]=-1;var queue=new System.Collections.Generic.Queue<int>();queue.Enqueue(start);
        while(queue.Count>0&&prev[goal]==-2)
        {
            int id=queue.Dequeue(),x=id%width,z=id/width;
            foreach(var d in new[]{new Vector2Int(1,0),new Vector2Int(-1,0),new Vector2Int(0,1),new Vector2Int(0,-1)})
            {int nx=x+d.x,nz=z+d.y;if(nx<0||nx>=width||nz<0||nz>=depth)continue;int n=nz*width+nx;if(!clear[n]||prev[n]!=-2||Mathf.Abs(points[n].y-points[id].y)>.28f)continue;prev[n]=id;queue.Enqueue(n);}
        }
        if(prev[goal]==-2)throw new Exception("No walking route to house door");
        var path=new System.Collections.Generic.List<Vector3>();for(int id=goal;id>=0;id=prev[id])path.Add(points[id]);path.Reverse();
        File.WriteAllText("Docs/Porta_Regiao_1_Percurso.json",JsonConvert.SerializeObject(path.Select(p=>new{x=p.x,y=p.y,z=p.z}),Formatting.Indented));return path.ToArray();
    }
    public static Task<object> Region1()=>Test(1);
    public static Task<object> Region2()=>Test(2);
    public static Task<object> Region3()=>Test(3);
    public static async Task<object> CaptureDoor()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode");
        if(SceneManager.GetActiveScene().name=="FirstJourney_Asteroide01")
        {if(!UnityEngine.Object.FindFirstObjectByType<FJWorldDoor>().TryTravel())throw new Exception("Approach asteroid door");await Ready("FirstJourney_Reino01_FantasyKingdom");}
        var motor=Hero();motor.SetTestInput(Vector3.zero);foreach(var p in DoorRoute())await Walk(p);
        motor.transform.rotation=Quaternion.Euler(0,270,0);var orbit=Camera.main.GetComponent<FJOrbitCamera>();orbit.yaw=270;orbit.Snap();await Frames(60);
        const string closedPath="Assets/_FirstJourney/Review/Porta_Asteroide_Casa.png";
        const string openPath="Assets/_FirstJourney/Review/Porta_Asteroide_Aberta.png";
        if(File.Exists(closedPath))File.Delete(closedPath);ScreenCapture.CaptureScreenshot(closedPath);for(int i=0;i<50&&!File.Exists(closedPath);i++)await Task.Delay(20);
        var door=GameObject.Find("Porta_Asteroide_Regiao_1").GetComponent<FJWorldDoor>();var rotation=door.doorLeaf.localRotation;door.TryTravel();
        while(door&&Quaternion.Angle(rotation,door.doorLeaf.localRotation)<32)await Task.Delay(1);
        if(File.Exists(openPath))File.Delete(openPath);ScreenCapture.CaptureScreenshot(openPath);for(int i=0;i<50&&!File.Exists(openPath);i++)await Task.Delay(20);
        await Ready("FirstJourney_Asteroide01");return new{closedPath,openPath};
    }
    static async Task<object> Test(int region)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Run in Play Mode");Application.runInBackground=true;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).First(t=>t!=null);var view=EditorWindow.GetWindow(type);view.Show();view.Focus();
        var physical=InputSystem.devices.Where(d=>d.enabled&&(d is Keyboard||d is Gamepad)).ToArray();var previous=Keyboard.current;var previousPad=Gamepad.current;foreach(var d in physical)InputSystem.DisableDevice(d);var keyboard=InputSystem.AddDevice<Keyboard>();var pad=region==3?InputSystem.AddDevice<Gamepad>():null;
        try
        {
            if(SceneManager.GetActiveScene().name=="FirstJourney_Asteroide01")
            {
                var m=Hero();m.spawnPosition=GameObject.Find("Regresso_Do_Reino").transform.position;m.ResetToSpawn();await Frames(10);m.ClearTestInput();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));await Frames(2);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Ready("FirstJourney_Reino01_FantasyKingdom");
            }
            var actor=Hero();actor.SetTestInput(Vector3.zero);int respawns=actor.RespawnCount;
            if(region==1)
            {
                actor.ResetToSpawn();respawns=actor.RespawnCount;await Frames(30);
                foreach(var p in DoorRoute())await Walk(p);
            }
            else
            {
                var point=GameObject.Find("Aproximacao_Porta_Regiao_"+region).transform;
                actor.spawnPosition=point.position;actor.ResetToSpawn();actor.transform.rotation=point.rotation;respawns=actor.RespawnCount;await Frames(30);
            }
            actor.SetTestInput(Vector3.zero);var door=GameObject.Find("Porta_Asteroide_Regiao_"+region).GetComponent<FJWorldDoor>();
            bool range=door.InRange(),grounded=actor.Grounded;var departure=actor.transform.position;int unexpected=actor.RespawnCount-respawns;
            if(!range||!grounded)throw new Exception("Not at usable house door: "+departure+" grounded="+grounded+" range="+range);
            var leaf=door.doorLeaf;var closed=leaf.localRotation;bool opened=false,glow=false;
            actor.ClearTestInput();if(pad!=null){pad.MakeCurrent();InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));}else InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));await Frames(2);if(pad!=null)InputSystem.QueueStateEvent(pad,new GamepadState());else InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            var end=DateTime.UtcNow.AddSeconds(25);
            while(SceneManager.GetActiveScene().name!="FirstJourney_Asteroide01")
            {
                if(leaf)opened|=Quaternion.Angle(closed,leaf.localRotation)>25;
                if(door)glow|=door.openDoorSurface.activeSelf;
                if(DateTime.UtcNow>end)throw new Exception("E did not return to asteroid");await Task.Delay(10);
            }
            await Ready("FirstJourney_Asteroide01");Hero().SetTestInput(Vector3.zero);await Frames(30);
            var returned=Hero().transform.position;bool passed=opened&&glow&&Hero().Grounded&&Vector3.Distance(returned,new Vector3(-1.175f,.04f,2.15f))<.3f&&unexpected==0;
            var report=new{passed,region,sameWorld=true,doorIsOnHouse=true,walkFromOriginalArrival=region==1,setupTeleportForDistantRegion=region!=1,departure=departure.ToString("F3"),inRange=range,grounded,openedDegreesOver25=opened,visibleOpening=glow,keyboardE=region!=3,virtualGamepadWest=region==3,unexpectedRespawns=unexpected,returned=returned.ToString("F3"),returnedGrounded=Hero().Grounded,noBounce=!FJWorldDoor.IsTravelling};
            File.WriteAllText("Docs/Porta_Regiao_"+region+"_Teste.json",JsonConvert.SerializeObject(report,Formatting.Indented));if(!passed)throw new Exception(JsonConvert.SerializeObject(report));return report;
        }
        finally{InputSystem.RemoveDevice(keyboard);if(pad!=null)InputSystem.RemoveDevice(pad);foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);if(previous!=null&&previous.added)previous.MakeCurrent();if(previousPad!=null&&previousPad.added)previousPad.MakeCurrent();if(Hero())Hero().ClearTestInput();}
    }
}
