using System;
using UnityEngine;

namespace OriginalKartRacer.Vehicle
{
    /// <summary>
    /// 空格漂移、漂移充能、左 Shift 释放氮气。
    /// 小漂和长拖漂由持续时间自然区分，松开漂移键即结束本次漂移。
    /// </summary>
    [RequireComponent(typeof(KartController))]
    public sealed class DriftNitroSystem : MonoBehaviour
    {
        [Header("漂移")]
        [SerializeField, Min(0f)] private float minimumDriftSpeedKph = 18f;
        [SerializeField, Range(0f, 1f)] private float minimumSteering = 0.25f;
        [SerializeField, Min(0f)] private float driftSideImpulse = 5.5f;
        [SerializeField, Min(0f)] private float chargePerSecond = 24f;
        [SerializeField, Min(0f)] private float lateralChargeBonus = 1.8f;

        [Header("氮气")]
        [SerializeField, Range(1f, 3f)] private float boostMultiplier = 1.38f;
        [SerializeField, Min(0.1f)] private float boostDuration = 2.1f;
        [SerializeField, Range(1f, 100f)] private float chargeCost = 100f;

        private KartController kart;
        private Rigidbody body;
        private bool drifting;
        private float boostRemaining;

        public event Action<float> ChargeChanged;
        public event Action<bool> DriftStateChanged;
        public event Action<bool> BoostStateChanged;
        public float Charge { get; private set; }
        public bool IsDrifting => drifting;
        public bool IsBoosting => boostRemaining > 0f;

        private void Awake()
        {
            kart = GetComponent<KartController>();
            body = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            bool wantsDrift = kart.ControlsEnabled && GetRequestedInput().Drift;
            bool canDrift = kart.IsGrounded
                            && kart.SpeedKph >= minimumDriftSpeedKph
                            && Mathf.Abs(kart.SteeringInput) >= minimumSteering;

            SetDrifting(wantsDrift && canDrift);

            if (drifting)
            {
                float slipBonus = 1f + Mathf.Abs(kart.LateralSpeed) * 0.05f * lateralChargeBonus;
                AddCharge(chargePerSecond * slipBonus * Time.deltaTime);
            }

            if (kart.ControlsEnabled && GetRequestedInput().Boost)
                TryActivateBoost();

            UpdateBoost();
        }

        private void SetDrifting(bool active)
        {
            if (drifting == active)
                return;

            drifting = active;
            kart.SetDrifting(active);
            // 小幅入漂扰动，持续漂移由后轮抓地下降产生，不再每帧硬推车身。
            if(active&&body!=null&&!body.isKinematic)
                body.AddForce(-transform.right*Mathf.Sign(kart.SteeringInput)*Mathf.Min(1.2f,driftSideImpulse),ForceMode.VelocityChange);
            DriftStateChanged?.Invoke(active);
        }

        private void AddCharge(float amount)
        {
            float previous = Charge;
            Charge = Mathf.Clamp(Charge + amount, 0f, 100f);
            if (!Mathf.Approximately(previous, Charge))
                ChargeChanged?.Invoke(Charge);
        }

        private void TryActivateBoost()
        {
            if (IsBoosting || Charge < chargeCost)
                return;

            AddCharge(-chargeCost);
            boostRemaining = boostDuration;
            kart.SetBoostMultiplier(boostMultiplier);
            BoostStateChanged?.Invoke(true);
        }

        private void UpdateBoost()
        {
            if (boostRemaining <= 0f)
                return;

            boostRemaining -= Time.deltaTime;
            if (boostRemaining > 0f)
                return;

            boostRemaining = 0f;
            kart.SetBoostMultiplier(1f);
            BoostStateChanged?.Invoke(false);
        }

        private RaceVehicleInput GetRequestedInput()
        {
            // KartController 已在 Update 中决定键盘或外部控制来源；这里读取它当前帧的标准化输入。
            // 氮气采用按住可用的行为，启动后会忽略后续按键直至本段喷射结束。
            return kart.CurrentInput;
        }

        private void OnDisable()
        {
            SetDrifting(false);
            boostRemaining = 0f;
            if (kart != null)
                kart.SetBoostMultiplier(1f);
        }
    }
}
