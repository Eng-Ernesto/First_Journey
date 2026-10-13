using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstJourney
{
    [RequireComponent(typeof(CharacterController))]
    public class FJHeroMotor : MonoBehaviour
    {
        public Animator animator;
        public Camera movementCamera;
        public float walkSpeed=1.9f, runSpeed=3.8f, jumpHeight=1.1f, gravity=-16f;
        public Vector3 spawnPosition;
        public bool showControls=true;
        public bool Grounded { get; private set; }
        public float VerticalSpeed { get; private set; }
        public int RespawnCount { get; private set; }
        [NonSerialized] public bool useTestInput;
        Vector3 testDirection, horizontalVelocity;
        bool testSprint, testJump;
        CharacterController body;
        readonly RaycastHit[] groundHits=new RaycastHit[12];
        static readonly int SpeedId=Animator.StringToHash("Speed");
        static readonly int GroundId=Animator.StringToHash("Grounded");
        static readonly int VerticalId=Animator.StringToHash("VerticalSpeed");
        static readonly int JumpId=Animator.StringToHash("Jump");

        void Awake()
        {
            body=GetComponent<CharacterController>();
            if(!animator) animator=GetComponentInChildren<Animator>();
            if(!movementCamera) movementCamera=Camera.main;
            if(spawnPosition==Vector3.zero) spawnPosition=transform.position;
            animator.applyRootMotion=false;
        }
        public void SetTestInput(Vector3 worldDirection,bool sprint=false,bool jump=false)
        { useTestInput=true; testDirection=worldDirection; testSprint=sprint; testJump=jump; }
        public void ClearTestInput()
        { useTestInput=false; testDirection=Vector3.zero; testJump=false; testSprint=false; }

        void Update()
        {
            float dt=Time.deltaTime;
            if(dt<=0 || !body.enabled) return;
            var keyboard=Keyboard.current;
            Vector3 direction=Vector3.zero;
            bool sprint=false, jump=false;
            if(useTestInput) { direction=testDirection; sprint=testSprint; jump=testJump; testJump=false; }
            else
            {
                Vector2 movement=Vector2.zero;
                if(keyboard!=null)
                {
                    movement.x=(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed?1:0);
                    movement.y=(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed?1:0);
                    sprint=keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed;
                    jump=keyboard.spaceKey.wasPressedThisFrame;
                }
                var gamepad=Gamepad.current;
                if(gamepad!=null && gamepad.enabled)
                {
                    if(movement.sqrMagnitude==0)movement=gamepad.leftStick.ReadValue();
                    sprint|=gamepad.rightTrigger.ReadValue()>.25f;
                    jump|=gamepad.buttonSouth.wasPressedThisFrame;
                }
                Vector3 forward=movementCamera?movementCamera.transform.forward:Vector3.forward;
                forward.y=0; forward.Normalize();
                direction=forward*movement.y+Vector3.Cross(Vector3.up,forward)*movement.x;
            }
            direction.y=0; direction=Vector3.ClampMagnitude(direction,1);
            bool interaction=animator.GetCurrentAnimatorStateInfo(0).IsTag("Interaction");
            if(interaction) { direction=Vector3.zero; jump=false; }
            Grounded=CheckGround();
            if(Grounded && VerticalSpeed<0) VerticalSpeed=-2;
            if(jump && Grounded)
            {
                VerticalSpeed=Mathf.Sqrt(jumpHeight*-2*gravity);
                animator.ResetTrigger(JumpId); animator.SetTrigger(JumpId);
                Grounded=false;
            }
            horizontalVelocity=Vector3.MoveTowards(horizontalVelocity,direction*(sprint?runSpeed:walkSpeed),12*dt);
            if(direction.sqrMagnitude>.01f)
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-14*dt));
            VerticalSpeed+=gravity*dt;
            Vector3 before=transform.position;
            CollisionFlags flags=body.Move((horizontalVelocity+Vector3.up*VerticalSpeed)*dt);
            if((flags&CollisionFlags.Above)!=0 && VerticalSpeed>0) VerticalSpeed=0;
            Grounded=(flags&CollisionFlags.Below)!=0 || CheckGround();
            if(Grounded && VerticalSpeed<0) VerticalSpeed=-2;
            Vector3 moved=transform.position-before; moved.y=0;
            animator.SetFloat(SpeedId,moved.magnitude/Mathf.Max(.001f,dt),.12f,dt);
            animator.SetFloat(VerticalId,VerticalSpeed);
            animator.SetBool(GroundId,Grounded);
            if(transform.position.y < -6) ResetToSpawn();
        }
        bool CheckGround()
        {
            if(VerticalSpeed>.1f) return false;
            if(body.isGrounded) return true;
            int count=Physics.RaycastNonAlloc(transform.position+Vector3.up*.16f,Vector3.down,groundHits,.24f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
                if(groundHits[i].collider.transform.root!=transform.root && Vector3.Dot(groundHits[i].normal,Vector3.up)>.65f) return true;
            return false;
        }
        public bool PlayInteraction(string state)
        {
            int id=Animator.StringToHash("Base Layer."+state);
            if(!animator.HasState(0,id) || !Grounded) return false;
            animator.CrossFadeInFixedTime(id,.15f); return true;
        }
        public void StopInteraction() { animator.CrossFadeInFixedTime("Locomotion",.15f); }
        public void ResetToSpawn()
        {
            body.enabled=false; transform.position=spawnPosition; body.enabled=true;
            horizontalVelocity=Vector3.zero; VerticalSpeed=0; RespawnCount++;
            animator.ResetTrigger(JumpId); animator.Play("Locomotion");
        }
        void OnGUI()
        {
            if(!showControls)return;
            Color previous=GUI.color;
            GUI.color=new Color(.05f,.10f,.08f,.72f);
            GUI.DrawTexture(new Rect(16,Screen.height-65,455,48),Texture2D.whiteTexture);
            GUI.color=new Color(.98f,.94f,.81f);
            var style=new GUIStyle(GUI.skin.label){fontSize=14};
            string controls="WASD / Setas · mover    Shift · correr    Espaço · saltar\nBotão direito · olhar    Roda do rato · aproximar";
            var pad=Gamepad.current;
            if(pad!=null && pad.enabled)
            {
                bool playStation=pad is UnityEngine.InputSystem.DualShock.DualShockGamepad;
                controls=playStation?
                    "Analógico esq. · mover    R2 · correr    X · saltar\nAnalógico dir. · olhar    L1 / R1 · distância":
                    "Analógico esq. · mover    RT · correr    A · saltar\nAnalógico dir. · olhar    LB / RB · distância";
            }
            GUI.Label(new Rect(27,Screen.height-61,435,42),controls,style);
            GUI.color=previous;
        }
    }
}
