using OriginalKartRacer.Replay;
using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>模型位置来自悬挂行程，转角/滚动来自轮胎求解；回放使用记录值。</summary>
    public sealed class KartVisualMotion : MonoBehaviour
    {
        private readonly Transform[] models=new Transform[4];
        private readonly float[] roll=new float[4];
        private readonly float[] steer=new float[4];
        private KartController kart;
        private ReplayTelemetry telemetry;
        private void Start()
        {
            kart=GetComponent<KartController>();telemetry=GetComponent<ReplayTelemetry>();
            foreach(Transform child in transform)
                if(child.name=="FrontWheel"||child.name=="RearWheel")
                    models[(child.name=="FrontWheel"?0:2)+(child.localPosition.x>0?1:0)]=child;
        }
        private void LateUpdate()
        {
            if(kart==null)return;
            bool playback=telemetry!=null&&telemetry.IsPlaying;
            for(int i=0;i<4;i++)
            {
                if(models[i]==null)continue;
                if(playback&&telemetry.CurrentFrame.wheels!=null&&telemetry.NextFrame.wheels!=null&&telemetry.CurrentFrame.wheels.Length>i&&telemetry.NextFrame.wheels.Length>i)
                {
                    var a=telemetry.CurrentFrame.wheels[i];var b=telemetry.NextFrame.wheels[i];float t=telemetry.Blend;
                    models[i].localPosition=Vector3.Lerp(a.localPosition,b.localPosition,t);
                    steer[i]=Mathf.Lerp(a.steerAngle,b.steerAngle,t);roll[i]=Mathf.Lerp(a.rollAngle,b.rollAngle,t);
                }
                else
                {
                    var state=kart.Wheels[i];models[i].localPosition=state.localMount-Vector3.up*state.length;
                    steer[i]=state.steeringAngle;roll[i]+=state.rpm*6*Time.deltaTime;
                }
                models[i].localRotation=Quaternion.Euler(0,steer[i],0)*Quaternion.Euler(roll[i],0,0);
            }
        }
        public WheelReplayFrame[] CaptureWheels()
        {
            var frames=new WheelReplayFrame[4];
            for(int i=0;i<4;i++)frames[i]=new WheelReplayFrame{localPosition=models[i]==null?Vector3.zero:models[i].localPosition,steerAngle=steer[i],rollAngle=roll[i]};
            return frames;
        }
    }
}
