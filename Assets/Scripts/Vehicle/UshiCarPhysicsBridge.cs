using System.Reflection;
using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>
    /// Ushi Arcade Car Physics 的无编译期依赖桥接层。
    /// 将你自己工程中的 Ushi 控制器拖进 Target Controller 后，按其实际公开 API 填写方法名，
    /// AI、回放和比赛系统即可复用同一份 RaceVehicleInput。
    /// 不包含也不替代 Ushi 的任何专有代码或资源。
    /// </summary>
    public sealed class UshiCarPhysicsBridge : MonoBehaviour
    {
        [Header("可选：导入 Ushi 后指定其车辆控制组件")]
        [SerializeField] private MonoBehaviour targetController;
        [SerializeField] private bool preferUshiController;

        [Header("桥接 API 名称（按你的 Ushi 版本配置）")]
        [SerializeField] private string inputMethodName = "SetInput";
        [SerializeField] private string surfaceMethodName = "SetSurfaceProfile";
        [SerializeField] private string lightsMethodName = "SetLights";

        private KartController fallbackKart;
        private MethodInfo inputMethod;
        private MethodInfo surfaceMethod;
        private MethodInfo lightsMethod;

        public bool IsConnected => targetController != null && inputMethod != null;
        public bool UsingUshi => preferUshiController && IsConnected;

        private void Awake()
        {
            fallbackKart = GetComponent<KartController>();
            CacheMethods();
            UpdateFallbackState();
        }

        private void OnValidate()
        {
            CacheMethods();
            if (Application.isPlaying)
                UpdateFallbackState();
        }

        public void ApplyInput(RaceVehicleInput input)
        {
            if (!UsingUshi)
            {
                fallbackKart.SetExternalInput(input);
                return;
            }

            ParameterInfo[] parameters = inputMethod.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(RaceVehicleInput))
                inputMethod.Invoke(targetController, new object[] { input });
            else if (parameters.Length == 4)
                inputMethod.Invoke(targetController, new object[] { input.Throttle, input.Steering, input.Drift, input.Boost });
            else
                Debug.LogWarning($"{name}: {inputMethodName} 必须接收 RaceVehicleInput，或 4 个参数 (float,float,bool,bool)。", this);
        }

        public void ApplySurface(RoadSurfaceProfile profile)
        {
            if (!UsingUshi || surfaceMethod == null)
                return;
            surfaceMethod.Invoke(targetController, new object[] { profile });
        }

        public void SetLights(bool enabled)
        {
            if (UsingUshi && lightsMethod != null)
                lightsMethod.Invoke(targetController, new object[] { enabled });
        }

        private void CacheMethods()
        {
            inputMethod = FindMethod(inputMethodName);
            surfaceMethod = FindMethod(surfaceMethodName);
            lightsMethod = FindMethod(lightsMethodName);
        }

        private MethodInfo FindMethod(string methodName)
        {
            return targetController == null || string.IsNullOrWhiteSpace(methodName)
                ? null
                : targetController.GetType().GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public);
        }

        private void UpdateFallbackState()
        {
            if (fallbackKart != null)
                fallbackKart.enabled = !UsingUshi;
        }
    }
}
