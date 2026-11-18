using System;
using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>轻量发动机/自动变速箱。发动机转速反馈来自车轮，换挡有扭矩中断。</summary>
    [Serializable]
    public sealed class ArcadeDrivetrain
    {
        [Min(1)] public float peakTorque=48f;
        [Min(1)] public float finalDrive=3.6f;
        public float[] ratios={4.2f,3.15f,2.35f,1.8f,1.4f};
        public AnimationCurve torqueCurve=new AnimationCurve(new Keyframe(0,.45f),new Keyframe(.35f,.82f),new Keyframe(.65f,1),new Keyframe(1,.65f));
        public float idleRpm=1300,redlineRpm=8500,upshiftRpm=7200,downshiftRpm=3100;
        [NonSerialized] public float EngineRpm=1300;
        [NonSerialized] public int Gear=1;
        [NonSerialized] public bool Braking;
        private int gearIndex;
        private float shiftRemaining;

        public float Step(float speed,float wheelRpm,float throttle,float radius,float boost,bool enabled,float dt)
        {
            if(ratios==null||ratios.Length==0)ratios=new[]{3f};
            gearIndex=Mathf.Clamp(gearIndex,0,ratios.Length-1);
            Braking=throttle*speed<-.3f;
            bool reverse=speed<-.4f||(throttle<-.05f&&speed<.4f);
            float ratio=(reverse?ratios[0]:ratios[gearIndex])*finalDrive;
            float coupledRpm=Mathf.Abs(wheelRpm)*ratio;
            float target=Mathf.Max(idleRpm,coupledRpm+Mathf.Abs(throttle)*Mathf.Clamp01(1-Mathf.Abs(speed)/4)*1800);
            EngineRpm=Mathf.Lerp(EngineRpm,Mathf.Clamp(target,idleRpm,redlineRpm),1-Mathf.Exp(-12*dt));
            shiftRemaining=Mathf.Max(0,shiftRemaining-dt);
            if(!reverse&&shiftRemaining<=0)
            {
                if(coupledRpm>upshiftRpm&&gearIndex<ratios.Length-1){gearIndex++;shiftRemaining=.15f;}
                else if(coupledRpm<downshiftRpm&&gearIndex>0){gearIndex--;shiftRemaining=.15f;}
            }
            Gear=!enabled?0:reverse?-1:gearIndex+1;
            if(!enabled||Braking||shiftRemaining>0||Mathf.Abs(throttle)<.01f)return 0;
            float curve=torqueCurve==null?1:Mathf.Max(0,torqueCurve.Evaluate(EngineRpm/redlineRpm));
            return Mathf.Sign(throttle)*Mathf.Abs(throttle)*peakTorque*ratio*.88f*curve*boost/Mathf.Max(.05f,radius);
        }
    }
}
