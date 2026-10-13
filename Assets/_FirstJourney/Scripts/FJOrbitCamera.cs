using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstJourney
{
    public class FJOrbitCamera : MonoBehaviour
    {
        public Transform target;
        public float distance=5.2f, pitch=28, yaw=55, followHeight=1.15f;
        public float gamepadLookSpeed=130;
        bool looking, initialized;
        readonly RaycastHit[] hits=new RaycastHit[24];
        void LateUpdate()
        {
            if(!target)return;
            var mouse=Mouse.current;
            if(mouse!=null)
            {
                if(mouse.rightButton.wasPressedThisFrame) { looking=true; Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
                if(mouse.rightButton.wasReleasedThisFrame || Keyboard.current?.escapeKey.wasPressedThisFrame==true) ReleaseCursor();
                if(looking) { var delta=mouse.delta.ReadValue(); yaw+=delta.x*.13f; pitch=Mathf.Clamp(pitch-delta.y*.13f,14,65); }
                distance=Mathf.Clamp(distance-mouse.scroll.ReadValue().y*.008f,3,8);
            }
            var gamepad=Gamepad.current;
            if(gamepad!=null && gamepad.enabled)
            {
                Vector2 stick=gamepad.rightStick.ReadValue();
                yaw+=stick.x*gamepadLookSpeed*Time.deltaTime;
                pitch=Mathf.Clamp(pitch-stick.y*gamepadLookSpeed*Time.deltaTime,14,65);
                float zoom=(gamepad.leftShoulder.isPressed?1:0)-(gamepad.rightShoulder.isPressed?1:0);
                distance=Mathf.Clamp(distance+zoom*2*Time.deltaTime,3,8);
                if(gamepad.startButton.wasPressedThisFrame)ReleaseCursor();
            }
            Vector3 aim=target.position+Vector3.up*followHeight;
            Vector3 offset=Quaternion.Euler(pitch,yaw,0)*Vector3.back;
            float actualDistance=distance;
            int count=Physics.SphereCastNonAlloc(aim,.18f,offset,hits,distance,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
                if(hits[i].collider.transform.root!=target.root && hits[i].distance>.1f)
                    actualDistance=Mathf.Min(actualDistance,Mathf.Max(.6f,hits[i].distance-.08f));
            Vector3 desired=aim+offset*actualDistance;
            if(!initialized || actualDistance<distance-.1f) { transform.position=desired; initialized=true; }
            else transform.position=Vector3.Lerp(transform.position,desired,1-Mathf.Exp(-12*Time.deltaTime));
            transform.LookAt(aim);
        }
        public void Snap() { initialized=false; }
        void ReleaseCursor() { looking=false; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        void OnDisable() { ReleaseCursor(); }
    }
}
