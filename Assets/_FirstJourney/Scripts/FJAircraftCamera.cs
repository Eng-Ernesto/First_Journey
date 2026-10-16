using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstJourney
{
    [DefaultExecutionOrder(100)]
    public class FJAircraftCamera : MonoBehaviour
    {
        FJAircraftController aircraft;
        Camera view;
        float originalFov,lookYaw,lookPitch;
        bool initialized,looking;
        public void Follow(FJAircraftController target)
        {
            aircraft=target;view=GetComponent<Camera>();originalFov=view.fieldOfView;lookYaw=lookPitch=0;
            initialized=false;enabled=true;
        }
        public void Release()
        {
            if(view)view.fieldOfView=originalFov;
            aircraft=null;enabled=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        void LateUpdate()
        {
            if(!aircraft||!aircraft.IsPiloted)return;
            var mouse=Mouse.current;
            if(mouse!=null)
            {
                if(mouse.rightButton.wasPressedThisFrame){looking=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
                if(mouse.rightButton.wasReleasedThisFrame){looking=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
                if(looking){var d=mouse.delta.ReadValue();lookYaw+=d.x*.15f;lookPitch=Mathf.Clamp(lookPitch-d.y*.15f,-25,35);}
            }
            var pad=Gamepad.current;
            if(pad!=null&&pad.enabled){var v=pad.rightStick.ReadValue();lookYaw+=v.x*100*Time.deltaTime;lookPitch=Mathf.Clamp(lookPitch-v.y*80*Time.deltaTime,-25,35);}
            if(!looking&&(pad==null||pad.rightStick.ReadValue().sqrMagnitude<.01f))
            {lookYaw=Mathf.LerpAngle(lookYaw,0,Time.deltaTime*1.3f);lookPitch=Mathf.Lerp(lookPitch,0,Time.deltaTime*1.3f);}
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true||pad?.startButton.wasPressedThisFrame==true)
            {looking=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            Vector3 aim=aircraft.transform.position+Vector3.up*1.7f;
            var direction=Quaternion.Euler(15+lookPitch,aircraft.transform.eulerAngles.y+lookYaw,0);
            Vector3 desired=aim+direction*new Vector3(0,0,-15);
            transform.position=initialized?Vector3.Lerp(transform.position,desired,1-Mathf.Exp(-6*Time.deltaTime)):desired;
            initialized=true;transform.LookAt(aim+aircraft.transform.forward*3);
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,52+aircraft.Speed*.35f,Time.deltaTime*3);
        }
    }
}
