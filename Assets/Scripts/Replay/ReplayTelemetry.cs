using UnityEngine;
namespace OriginalKartRacer.Replay
{
    /// <summary>回放直接重现已记录仪表，独立文件确保 Unity 正常序列化组件。</summary>
    public sealed class ReplayTelemetry : MonoBehaviour
    {
        public ReplayFrame CurrentFrame { get; private set; }
        public bool IsPlaying { get; private set; }
        public ReplayFrame NextFrame { get; private set; }
        public float Blend { get; private set; }
        public void SetFrame(ReplayFrame frame) { CurrentFrame=frame; NextFrame=frame;Blend=0;IsPlaying=true; }
        public void SetInterpolatedFrame(ReplayFrame frame,ReplayFrame next,float blend)
        {CurrentFrame=frame;NextFrame=next;Blend=blend;IsPlaying=true;}
        public void Stop() => IsPlaying=false;
    }
}
