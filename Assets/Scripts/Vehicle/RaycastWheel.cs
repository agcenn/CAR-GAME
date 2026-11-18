using System;
using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>
    /// 独立车轮接触状态。参考 RVP 的射线接触/接触点速度/悬挂受力模块划分，
    /// 在本工程内重新实现；没有复制其损坏、脱轮或地形系统。
    /// </summary>
    [Serializable]
    public sealed class RaycastWheel
    {
        public Vector3 localMount;
        public bool steering;
        public float radius = .315f;
        [NonSerialized] public bool grounded;
        [NonSerialized] public Vector3 point, normal, relativeVelocity;
        [NonSerialized] public float length, compression, load, steeringAngle, rpm, sidewaysSlip;
        [NonSerialized] public RoadSurfaceProfile surface;
        [NonSerialized] private Rigidbody groundBody;
        private readonly RaycastHit[] hits = new RaycastHit[16];

        public void Sample(Rigidbody body, Transform chassis, LayerMask mask,
            float restLength, float springRate, float damping)
        {
            Vector3 up=chassis.up;
            Vector3 mount=chassis.TransformPoint(localMount);
            int count=Physics.RaycastNonAlloc(mount+up*.06f,-up,hits,restLength+radius+.14f,mask,QueryTriggerInteraction.Ignore);
            int selected=-1;float nearest=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                // 排除自己的底盘与车轮保护碰撞体，避免射线把车体误当成路面。
                if(hits[i].rigidbody==body||Vector3.Dot(hits[i].normal,up)<.25f)continue;
                if(hits[i].distance<nearest){nearest=hits[i].distance;selected=i;}
            }
            grounded=selected>=0;load=0;compression=0;surface=null;groundBody=null;
            if(!grounded){length=restLength+.08f;normal=up;relativeVelocity=Vector3.zero;return;}
            RaycastHit hit=hits[selected];point=hit.point;normal=hit.normal;groundBody=hit.rigidbody;
            length=Mathf.Clamp(hit.distance-.06f-radius,0,restLength+.08f);
            compression=Mathf.Max(0,restLength-length);
            relativeVelocity=body.GetPointVelocity(point)-(groundBody==null?Vector3.zero:groundBody.GetPointVelocity(point));
            var road=hit.collider.GetComponentInParent<RoadSurfaceVolume>();surface=road==null?null:road.Profile;
            // F=kx-cv，阻尼项使用接触点的相对速度；只推不拉，并限制落地峰值。
            load=Mathf.Clamp(springRate*compression-damping*Vector3.Dot(relativeVelocity,up),0,body.mass*9.81f*1.8f);
            ApplyForce(body,normal*load,mount);
        }

        public void ApplyTireForce(Rigidbody body, Transform chassis, float driveForce,
            float brakeForce, float gripResponse, float friction, float gripMultiplier, float deltaTime)
        {
            if(!grounded||load<=0)return;
            Vector3 forward=Vector3.ProjectOnPlane(Quaternion.AngleAxis(steeringAngle,chassis.up)*chassis.forward,normal).normalized;
            Vector3 right=Vector3.Cross(normal,forward);
            float forwardSpeed=Vector3.Dot(relativeVelocity,forward);
            float lateralSpeed=Vector3.Dot(relativeVelocity,right);
            sidewaysSlip=Mathf.Atan2(lateralSpeed,Mathf.Max(3,Mathf.Abs(forwardSpeed)));
            float effectiveMass=body.mass*.25f;
            // 刹车在当前步最多消除已有速度，不会在过零时主动推动车轮倒转。
            float brake=-Mathf.Sign(forwardSpeed)*Mathf.Min(brakeForce,Mathf.Abs(forwardSpeed)*effectiveMass/deltaTime);
            float longitudinal=driveForce+brake;
            float lateral=-lateralSpeed*effectiveMass*Mathf.Min(gripResponse,1f/deltaTime);
            // 大侧滑时由峰值抓地平滑过渡到滑动抓地，纵横向共同受摩擦圆限制。
            float slidingGrip=Mathf.Lerp(1,.72f,Mathf.InverseLerp(.14f,.7f,Mathf.Abs(sidewaysSlip)));
            float capacity=Mathf.Max(0,load*friction*gripMultiplier*slidingGrip);
            Vector2 requested=new Vector2(longitudinal,lateral);
            Vector2 actual=Vector2.ClampMagnitude(requested,capacity);
            // 轮胎力作用在接触点，转向和车身侧倾由刚体力矩自然产生。
            ApplyForce(body,forward*actual.x+right*actual.y,point);
            float wheelSpeed=forwardSpeed+(longitudinal-actual.x)/Mathf.Max(100,effectiveMass*20);
            rpm=wheelSpeed/(2*Mathf.PI*radius)*60;
        }

        private void ApplyForce(Rigidbody body,Vector3 force,Vector3 at)
        {
            body.AddForceAtPosition(force,at,ForceMode.Force);
            if(groundBody!=null&&!groundBody.isKinematic)
                groundBody.AddForceAtPosition(-force,point,ForceMode.Force);
        }
    }
}
