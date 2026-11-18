using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>放在道路或路肩碰撞体上，为车辆提供当前路面 Profile。</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RoadSurfaceVolume : MonoBehaviour
    {
        [SerializeField] private RoadSurfaceProfile profile;
        public RoadSurfaceProfile Profile => profile;
    }
}
