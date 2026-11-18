using System;
using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>
    /// 四轮射线悬挂街机车辆。参考 RVP 的模块划分，适配现有输入、AI、氮气及回放接口。
    /// 方向由转向轮的接触力改变，车身不再通过 MoveRotation 强制转弯。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class KartController : MonoBehaviour
    {
        [Header("速度 / m/s")]
        [SerializeField,Min(1)] private float maxForwardSpeed=34;
        [SerializeField,Min(1)] private float maxReverseSpeed=9;
        [SerializeField,Min(0)] private float acceleration=24;
        [SerializeField,Min(0)] private float braking=30;
        [SerializeField,Min(0)] private float coastingDrag=5;
        [SerializeField] private ArcadeDrivetrain drivetrain=new ArcadeDrivetrain();
        [SerializeField,Range(0,1)] private float frontDriveBias=.4f;

        [Header("转向与轮胎")]
        [SerializeField,Range(5,45)] private float maximumSteerAngle=32;
        [SerializeField,Min(.1f)] private float steeringResponse=8;
        [SerializeField] private AnimationCurve speedSteering=new AnimationCurve(new Keyframe(0,1),new Keyframe(15,.6f),new Keyframe(34,.3f));
        [SerializeField,Min(0)] private float lateralGrip=11;
        [SerializeField,Min(0)] private float driftLateralGrip=2.4f;
        [SerializeField,Range(.2f,3)] private float tireFriction=1.65f;

        [Header("悬挂 / N 与 m")]
        [SerializeField,Min(.05f)] private float suspensionRestLength=.22f;
        [SerializeField,Min(100)] private float springRate=6500;
        [SerializeField,Min(0)] private float springDamping=720;
        [SerializeField,Min(0)] private float antiRollStiffness=1100;
        [SerializeField] private LayerMask groundMask=~0;
        [SerializeField] private RaycastWheel[] wheels;
        [SerializeField] private bool fitDemoChassisCollider=true;

        private Rigidbody body;
        private PhysicMaterial contactMaterial;
        private float throttleInput,steeringInput,steerAngle,boostMultiplier=1;
        private bool controlsEnabled,driftActive,hasExternalInput;
        private RaceVehicleInput externalInput,currentInput;
        private float surfaceGripMultiplier=1,surfaceAccelerationMultiplier=1;

        public event Action<float> SpeedChanged;
        public float SpeedKph { get; private set; }
        public float ForwardSpeed => body==null?0:Vector3.Dot(body.velocity,transform.forward);
        public float LateralSpeed => body==null?0:Vector3.Dot(body.velocity,transform.right);
        public bool IsGrounded => GroundedWheelCount>0;
        public int GroundedWheelCount { get; private set; }
        public Vector3 GroundNormal { get; private set; }=Vector3.up;
        public bool ControlsEnabled => controlsEnabled;
        public float SteeringInput => steeringInput;
        public float ThrottleInput => throttleInput;
        public RaceVehicleInput CurrentInput => currentInput;
        public float EngineRpm => drivetrain.EngineRpm;
        public int Gear => drivetrain.Gear;
        public RaycastWheel[] Wheels => wheels;

        public float SteeringInputForTarget(Vector3 worldTarget)
        {
            Vector3 local=transform.InverseTransformPoint(worldTarget);
            float wheelbase=Mathf.Abs(wheels[0].localMount.z-wheels[2].localMount.z);
            float desired=Mathf.Atan2(2*wheelbase*local.x,Mathf.Max(1,local.x*local.x+local.z*local.z))*Mathf.Rad2Deg;
            float limit=maximumSteerAngle*(speedSteering==null?1:speedSteering.Evaluate(Mathf.Abs(ForwardSpeed)));
            return Mathf.Clamp(desired/Mathf.Max(1,limit),-1,1);
        }

        private void Awake()
        {
            body=GetComponent<Rigidbody>();body.centerOfMass=new Vector3(0,-.08f,0);
            body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            // 悬挂负责支撑、俯仰和侧倾，解除旧版生成器留下的 X/Z 旋转冻结。
            body.constraints&=~(RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ);
            body.angularDrag=1.1f;body.maxAngularVelocity=4;body.solverIterations=10;body.solverVelocityIterations=4;
            EnsureWheelLayout();
            contactMaterial=new PhysicMaterial("Frictionless Chassis")
            {dynamicFriction=0,staticFriction=0,bounciness=0,frictionCombine=PhysicMaterialCombine.Minimum,bounceCombine=PhysicMaterialCombine.Minimum};
            var chassis=GetComponent<Collider>();if(chassis!=null)chassis.sharedMaterial=contactMaterial;
            if(fitDemoChassisCollider&&chassis is BoxCollider box)
            {box.size=new Vector3(1.7f,.35f,2.45f);box.center=new Vector3(0,.04f,0);}
        }

        /// <summary>顺序：前左、前右、后左、后右。手工装配车辆可在 Inspector 修改安装点。</summary>
        public void EnsureWheelLayout()
        {
            if(wheels!=null&&wheels.Length==4&&Array.TrueForAll(wheels,w=>w!=null))return;
            wheels=new RaycastWheel[4];
            for(int i=0;i<4;i++)wheels[i]=new RaycastWheel
            {localMount=new Vector3(i%2==0?-.91f:.91f,.185f,i<2?.85f:-.85f),steering=i<2,radius=.315f,length=suspensionRestLength};
        }

        private void Update()
        {
            currentInput=controlsEnabled?(hasExternalInput?externalInput:RaceVehicleInput.PlayerKeyboard):default;
            throttleInput=currentInput.Throttle;steeringInput=currentInput.Steering;
        }

        private void FixedUpdate()
        {
            if(body.isKinematic)return;
            float dt=Time.fixedDeltaTime;
            float speed=ForwardSpeed;
            float steerLimit=speedSteering==null?1:speedSteering.Evaluate(Mathf.Abs(speed));
            steerAngle=Mathf.Lerp(steerAngle,steeringInput*maximumSteerAngle*steerLimit,1-Mathf.Exp(-steeringResponse*dt));
            GroundedWheelCount=0;Vector3 normals=Vector3.zero;float averageRpm=0;
            foreach(var wheel in wheels)
            {
                wheel.Sample(body,transform,groundMask,suspensionRestLength,springRate,springDamping);
                if(wheel.grounded){GroundedWheelCount++;normals+=wheel.normal;}
                averageRpm+=Mathf.Abs(wheel.rpm)*.25f;
                wheel.steeringAngle=wheel.steering?AckermannAngle(wheel.localMount.x):0;
            }
            GroundNormal=GroundedWheelCount>0?normals.normalized:transform.up;
            // 初始帧尚无车轮 RPM 时，用车速作为反馈种子。
            if(averageRpm<1)averageRpm=Mathf.Abs(speed)/(2*Mathf.PI*.315f)*60;
            float drive=drivetrain.Step(speed,averageRpm,throttleInput,.315f,boostMultiplier,controlsEnabled,dt);
            drive=Mathf.Clamp(drive,-body.mass*acceleration,body.mass*acceleration);
            if((speed>=maxForwardSpeed*boostMultiplier&&drive>0)||(speed<=-maxReverseSpeed&&drive<0))drive=0;
            float brakeDemand=drivetrain.Braking?Mathf.Abs(throttleInput)*braking:0;
            if(!controlsEnabled)brakeDemand=braking;
            else if(Mathf.Abs(throttleInput)<.01f)brakeDemand=coastingDrag*.12f+.002f*speed*speed;
            foreach(var wheel in wheels)
            {
                float surfaceGrip=wheel.surface==null?surfaceGripMultiplier:wheel.surface.lateralGripMultiplier;
                float surfacePower=wheel.surface==null?surfaceAccelerationMultiplier:wheel.surface.accelerationMultiplier;
                float rearSlide=driftActive&&!wheel.steering?Mathf.Clamp(driftLateralGrip/Mathf.Max(1,lateralGrip),.35f,.8f):1;
                float driveShare=(wheel.steering?frontDriveBias:1-frontDriveBias)*.5f;
                wheel.ApplyTireForce(body,transform,drive*driveShare*surfacePower,brakeDemand*body.mass*.25f,lateralGrip*rearSlide,tireFriction,surfaceGrip*rearSlide,dt);
            }
            AntiRoll(wheels[0],wheels[1]);AntiRoll(wheels[2],wheels[3]);
            if(GroundedWheelCount>=2)
            {
                // 轻量稳定辅助：保留自然侧倾，抑制连续翻滚，不改变偏航转向。
                Vector3 tilt=Vector3.Cross(transform.up,GroundNormal)*5;
                Vector3 tiltRate=Vector3.ProjectOnPlane(body.angularVelocity,GroundNormal);
                body.AddTorque(tilt-tiltRate*1.2f,ForceMode.Acceleration);
            }
            // 空气阻力与有限下压力随速度变化，停车时不施加额外贴地重力。
            Vector3 planar=Vector3.ProjectOnPlane(body.velocity,GroundNormal);
            body.AddForce(-planar*planar.magnitude*.002f,ForceMode.Acceleration);
            if(IsGrounded)body.AddForce(-GroundNormal*Mathf.Min(3,planar.sqrMagnitude*.003f),ForceMode.Acceleration);
            float limit=maxForwardSpeed*boostMultiplier*1.08f;
            if(planar.magnitude>limit)body.velocity=planar.normalized*limit+Vector3.Project(body.velocity,GroundNormal);
            SpeedKph=planar.magnitude*3.6f;SpeedChanged?.Invoke(SpeedKph);
        }

        private float AckermannAngle(float side)
        {
            if(Mathf.Abs(steerAngle)<.01f)return 0;
            float wheelbase=Mathf.Abs(wheels[0].localMount.z-wheels[2].localMount.z);
            float radius=wheelbase/Mathf.Tan(Mathf.Abs(steerAngle)*Mathf.Deg2Rad);
            float adjusted=Mathf.Max(.5f,radius-Mathf.Sign(steerAngle)*side);
            return Mathf.Sign(steerAngle)*Mathf.Atan(wheelbase/adjusted)*Mathf.Rad2Deg;
        }
        private void AntiRoll(RaycastWheel left,RaycastWheel right)
        {
            // 两边都接地才做轴间平衡，避免单侧悬空时把车身推向空中。
            if(!left.grounded||!right.grounded)return;
            float force=(left.compression-right.compression)*antiRollStiffness;
            body.AddForceAtPosition(transform.up*force,transform.TransformPoint(left.localMount));
            body.AddForceAtPosition(-transform.up*force,transform.TransformPoint(right.localMount));
        }
        public void SetControlsEnabled(bool enabled)
        {
            controlsEnabled=enabled;
            if(!enabled){throttleInput=0;steeringInput=0;currentInput=default;if(body!=null&&!body.isKinematic){body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;}}
        }
        public void SetDrifting(bool active)=>driftActive=active;
        public void SetBoostMultiplier(float multiplier)=>boostMultiplier=Mathf.Max(1,multiplier);
        public void SetExternalInput(RaceVehicleInput input){externalInput=input;hasExternalInput=true;}
        public void ClearExternalInput()=>hasExternalInput=false;
        public void SetSurfaceModifiers(float gripMultiplier,float accelerationMultiplier)
        {surfaceGripMultiplier=Mathf.Clamp(gripMultiplier,.1f,2);surfaceAccelerationMultiplier=Mathf.Clamp(accelerationMultiplier,.1f,2);}
        private void OnDestroy(){if(contactMaterial!=null)Destroy(contactMaterial);}
        private void OnDrawGizmosSelected()
        {
            if(wheels==null)return;Gizmos.color=Color.cyan;
            foreach(var wheel in wheels)if(wheel!=null)
            {Vector3 mount=transform.TransformPoint(wheel.localMount);Gizmos.DrawLine(mount,mount-transform.up*(suspensionRestLength+wheel.radius));Gizmos.DrawWireSphere(mount-transform.up*suspensionRestLength,wheel.radius);}
        }
    }
}
