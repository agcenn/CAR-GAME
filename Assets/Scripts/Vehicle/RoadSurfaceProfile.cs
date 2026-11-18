using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>
    /// 每类道路单独创建一个 Profile，例如 Asphalt、Grass、Sand 或 Ice。
    /// 既可作用于内置 KartController，也会传递给 Ushi 桥接器的可配置回调。
    /// </summary>
    [CreateAssetMenu(menuName = "Neon Harbor/Road Surface Profile", fileName = "RoadSurface_")]
    public sealed class RoadSurfaceProfile : ScriptableObject
    {
        [Min(0.1f)] public float lateralGripMultiplier = 1f;
        [Min(0.1f)] public float accelerationMultiplier = 1f;
        public Color skidColor = new Color(0.12f, 0.12f, 0.12f, 0.6f);
        public ParticleSystem surfaceParticles;
        public AudioClip rollingLoop;
    }
}
