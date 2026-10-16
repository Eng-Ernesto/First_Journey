using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstJourney
{
    // A single button press cannot open a door and board/leave the aircraft in the same frame.
    public static class FJInteractionInput
    {
        static int usedFrame=-1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){usedFrame=-1;}
        public static bool Pressed=>Keyboard.current?.eKey.wasPressedThisFrame==true ||
            (Gamepad.current!=null&&Gamepad.current.enabled&&Gamepad.current.buttonWest.wasPressedThisFrame);
        public static bool Consume()
        {
            if(usedFrame==Time.frameCount)return false;
            usedFrame=Time.frameCount;return true;
        }
    }
}
