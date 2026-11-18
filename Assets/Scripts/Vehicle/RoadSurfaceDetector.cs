using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>
    /// 自下而上的射线检测路面，切换内置车辆的抓地/动力系数，并通知可选 Ushi 桥接器。
    /// </summary>
    [RequireComponent(typeof(KartController))]
    public sealed class RoadSurfaceDetector : MonoBehaviour
    {
        [SerializeField] private LayerMask roadMask = ~0;
        [SerializeField, Min(0.1f)] private float rayLength = 1.4f;
        [SerializeField] private RoadSurfaceProfile fallbackProfile;
        [SerializeField] private UshiCarPhysicsBridge ushiBridge;

        private KartController kart;
        private RoadSurfaceProfile currentProfile;

        private void Awake() => kart = GetComponent<KartController>();

        private void FixedUpdate()
        {
            RoadSurfaceProfile next = fallbackProfile;
            Vector3 origin = transform.position + Vector3.up * 0.2f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLength, roadMask,
                    QueryTriggerInteraction.Ignore))
            {
                RoadSurfaceVolume volume = hit.collider.GetComponentInParent<RoadSurfaceVolume>();
                if (volume != null && volume.Profile != null)
                    next = volume.Profile;
            }

            if (next == currentProfile)
                return;

            currentProfile = next;
            float grip = next == null ? 1f : next.lateralGripMultiplier;
            float acceleration = next == null ? 1f : next.accelerationMultiplier;
            kart.SetSurfaceModifiers(grip, acceleration);
            var effects = GetComponent<OriginalKartRacer.Effects.KartEffectsController>();
            if(effects!=null)effects.SetSurfaceColor(next==null?Color.black:next.skidColor);
            if (ushiBridge != null)
                ushiBridge.ApplySurface(next);
        }
    }
}
