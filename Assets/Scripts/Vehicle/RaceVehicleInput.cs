using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>
    /// 赛车的统一输入数据。玩家、AI、回放和外部车辆物理插件都使用这份数据，
    /// 让上层比赛系统不依赖某一套具体的车辆物理实现。
    /// </summary>
    public readonly struct RaceVehicleInput
    {
        public RaceVehicleInput(float throttle, float steering, bool drift, bool boost)
        {
            Throttle = Mathf.Clamp(throttle, -1f, 1f);
            Steering = Mathf.Clamp(steering, -1f, 1f);
            Drift = drift;
            Boost = boost;
        }

        public float Throttle { get; }
        public float Steering { get; }
        public bool Drift { get; }
        public bool Boost { get; }

        public static RaceVehicleInput PlayerKeyboard => new RaceVehicleInput(
            Input.GetAxisRaw("Vertical"),
            Input.GetAxisRaw("Horizontal"),
            Input.GetKey(KeyCode.Space),
            Input.GetKey(KeyCode.LeftShift));
    }
}
