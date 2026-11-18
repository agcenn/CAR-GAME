using UnityEngine;
using OriginalKartRacer.Replay;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>原创合成发动机循环，随车速与油门改变音高，不依赖下载音频。</summary>
    public sealed class KartAudio : MonoBehaviour
    {
        private AudioSource engine;
        private AudioClip clip;
        private KartController kart;
        private ReplayTelemetry telemetry;
        private void Start()
        {
            kart=GetComponent<KartController>();telemetry=GetComponent<ReplayTelemetry>();
            const int rate=22050;var samples=new float[rate];
            for(int i=0;i<rate;i++){float t=(float)i/rate;samples[i]=.26f*Mathf.Sin(t*2*Mathf.PI*60)+.12f*Mathf.Sin(t*2*Mathf.PI*120)+.055f*Mathf.Sin(t*2*Mathf.PI*180);}
            clip=AudioClip.Create("Original engine harmonics",rate,1,rate,false);clip.SetData(samples,0);
            engine=gameObject.AddComponent<AudioSource>();engine.clip=clip;engine.loop=true;engine.spatialBlend=telemetry!=null?0:1;engine.minDistance=2;engine.maxDistance=24;engine.volume=.12f;engine.Play();
        }
        private void Update()
        {
            if(engine==null)return;bool replay=telemetry!=null&&telemetry.IsPlaying;
            float speed=replay?telemetry.CurrentFrame.speedKph:kart.SpeedKph;
            float throttle=replay?telemetry.CurrentFrame.throttle:kart.ThrottleInput;
            float rpm=replay?telemetry.CurrentFrame.engineRpm:kart.EngineRpm;
            engine.pitch=Mathf.Lerp(engine.pitch,.65f+rpm/4000,Time.deltaTime*8);
            engine.volume=Mathf.Lerp(engine.volume,.045f+Mathf.Abs(throttle)*.13f,Time.deltaTime*5);
        }
        private void OnDestroy(){if(clip!=null)Destroy(clip);}
    }
}
