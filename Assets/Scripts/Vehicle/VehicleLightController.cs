using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>
    /// 通用车辆灯光控制。默认按 L 切换，也可由天气、昼夜或 Ushi 桥接器统一调用 SetLights。
    /// </summary>
    public sealed class VehicleLightController : MonoBehaviour
    {
        [SerializeField] private Light[] vehicleLights;
        [SerializeField] private UshiCarPhysicsBridge ushiBridge;
        [SerializeField] private KeyCode toggleKey = KeyCode.L;
        [SerializeField] private bool lightsOn;

        private void Awake()
        {
            if (vehicleLights == null || vehicleLights.Length == 0)
                vehicleLights = GetComponentsInChildren<Light>(true);
            SetLights(lightsOn);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                SetLights(!lightsOn);
        }

        public void SetLights(bool enabled)
        {
            lightsOn = enabled;
            foreach (Light vehicleLight in vehicleLights)
                if (vehicleLight != null)
                    vehicleLight.enabled = enabled;
            if (ushiBridge != null)
                ushiBridge.SetLights(enabled);
        }
    }
}
