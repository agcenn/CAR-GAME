using OriginalKartRacer.Vehicle;
using OriginalKartRacer.Replay;
using UnityEngine;

namespace OriginalKartRacer.Effects
{
    /// <summary>统一控制轮胎印、尾气和氮气粒子，避免玩法脚本依赖具体美术。</summary>
    public sealed class KartEffectsController : MonoBehaviour
    {
        [SerializeField] private DriftNitroSystem driftNitro;
        [SerializeField] private TrailRenderer[] skidTrails = new TrailRenderer[0];
        [SerializeField] private ParticleSystem[] driftSmoke = new ParticleSystem[0];
        [SerializeField] private ParticleSystem[] boostFlames = new ParticleSystem[0];
        [SerializeField] private ParticleSystem[] exhaust = new ParticleSystem[0];
        private ReplayTelemetry telemetry;
        private KartController kart;

        private void OnEnable()
        {
            // The scene builder can attach this component before its serialized
            // references are written. Resolve the sibling component so the effect
            // layer is also safe to use on hand-built karts.
            if (driftNitro == null)
                driftNitro = GetComponent<DriftNitroSystem>();
            if (driftNitro == null)
                return;

            driftNitro.DriftStateChanged += SetDriftEffects;
            driftNitro.BoostStateChanged += SetBoostEffects;
            SetDriftEffects(false);
            SetBoostEffects(false);
        }

        private void Start()
        {
            telemetry = GetComponent<ReplayTelemetry>();
            kart = GetComponent<KartController>();
            foreach (ParticleSystem particles in exhaust)
                if (particles != null && !particles.isPlaying)
                    particles.Play();
        }

        private void LateUpdate()
        {
            if(driftNitro==null)return;
            bool replay=telemetry!=null&&telemetry.IsPlaying;
            SetDriftEffects(replay?telemetry.CurrentFrame.drifting:driftNitro.IsDrifting);
            SetBoostEffects(replay?telemetry.CurrentFrame.boosting:driftNitro.IsBoosting);
            if(!replay&&kart!=null&&kart.Wheels!=null)
                for(int i=0;i<skidTrails.Length&&i<2;i++)
                {
                    var wheel=kart.Wheels[i+2];
                    if(skidTrails[i]!=null){skidTrails[i].transform.position=wheel.point+wheel.normal*.014f;skidTrails[i].emitting=driftNitro.IsDrifting&&wheel.grounded;}
                    if(i<driftSmoke.Length&&driftSmoke[i]!=null&&wheel.grounded)driftSmoke[i].transform.position=wheel.point+wheel.normal*.09f;
                }
        }

        public void SetSurfaceColor(Color color)
        {foreach(var trail in skidTrails)if(trail!=null){trail.startColor=color;trail.endColor=color;}}

        private void OnDisable()
        {
            if (driftNitro != null)
            {
                driftNitro.DriftStateChanged -= SetDriftEffects;
                driftNitro.BoostStateChanged -= SetBoostEffects;
            }
        }

        private void SetDriftEffects(bool active)
        {
            foreach (TrailRenderer trail in skidTrails)
                if (trail != null)
                    trail.emitting = active;

            SetParticles(driftSmoke, active);
        }

        private void SetBoostEffects(bool active) => SetParticles(boostFlames, active);

        private static void SetParticles(ParticleSystem[] systems, bool active)
        {
            foreach (ParticleSystem particles in systems)
            {
                if (particles == null)
                    continue;
                if (active && !particles.isPlaying)
                    particles.Play();
                else if (!active && particles.isPlaying)
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
