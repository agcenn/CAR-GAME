using OriginalKartRacer.Race;
using OriginalKartRacer.Vehicle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriginalKartRacer.UI
{
    /// <summary>将赛况数据映射到 TextMeshPro 与 Image.fillAmount。</summary>
    public sealed class RaceHUD : MonoBehaviour
    {
        [Header("数据源")]
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private RaceClock raceClock;
        [SerializeField] private KartController playerKart;
        [SerializeField] private DriftNitroSystem nitroSystem;

        [Header("左上")]
        [SerializeField] private TMP_Text compactLapText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text playerIdText;
        [SerializeField] private string playerId = "RACER_01";
        [SerializeField, Min(1)] private int playerRank = 1;
        [SerializeField, Min(1)] private int racerCount = 1;

        [Header("右上")]
        [SerializeField] private TMP_Text lapProgressText;
        [SerializeField] private TMP_Text currentLapTimeText;
        [SerializeField] private TMP_Text totalTimeText;
        [SerializeField] private TMP_Text bestTimeText;

        [Header("中央与右下")]
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private Image speedFill;
        [SerializeField] private Image nitroFill;
        [SerializeField] private Gradient speedGradient;
        [SerializeField, Min(1f)] private float gaugeMaximumKph = 150f;

        private void OnEnable()
        {
            raceManager.CountdownChanged += SetCountdown;
            raceManager.LapChanged += SetLap;
            raceManager.LapCompleted += OnLapCompleted;
            playerKart.SpeedChanged += SetSpeed;
            nitroSystem.ChargeChanged += SetNitro;
        }

        private void Start()
        {
            playerIdText.text = playerId;
            rankText.text = $"{playerRank}<size=55%> / {racerCount}</size>";
            SetLap(raceManager.CurrentLap, raceManager.TotalLaps);
            SetSpeed(0f);
            SetNitro(nitroSystem.Charge);
            bestTimeText.text = float.IsPositiveInfinity(raceManager.BestLap)
                ? "BEST  --:--.---"
                : $"BEST  {FormatTime(raceManager.BestLap)}";
        }

        private void Update()
        {
            currentLapTimeText.text = $"LAP   {FormatTime(raceClock.CurrentLapTime)}";
            totalTimeText.text = $"TIME  {FormatTime(raceClock.RaceTime)}";
        }

        private void OnDisable()
        {
            raceManager.CountdownChanged -= SetCountdown;
            raceManager.LapChanged -= SetLap;
            raceManager.LapCompleted -= OnLapCompleted;
            playerKart.SpeedChanged -= SetSpeed;
            nitroSystem.ChargeChanged -= SetNitro;
        }

        private void SetCountdown(string value)
        {
            countdownText.text = value;
            countdownText.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }

        private void SetLap(int current, int total)
        {
            compactLapText.text = $"LAP  {current}/{total}";
            lapProgressText.text = $"{current}/{total}  <size=55%>LAP</size>";
        }

        private void OnLapCompleted(float lapTime, float bestTime)
        {
            bestTimeText.text = $"BEST  {FormatTime(bestTime)}";
        }

        private void SetSpeed(float kph)
        {
            speedText.text = $"{Mathf.RoundToInt(kph):000}<size=38%> km/h</size>";
            float normalized = Mathf.Clamp01(kph / gaugeMaximumKph);
            speedFill.fillAmount = normalized;
            speedFill.color = speedGradient.Evaluate(normalized);
        }

        private void SetNitro(float charge)
        {
            nitroFill.fillAmount = charge / 100f;
        }

        /// <summary>毫秒显示采用截断而非四舍五入，避免显示到下一秒。</summary>
        public static string FormatTime(float seconds)
        {
            int totalMilliseconds = Mathf.Max(0, Mathf.FloorToInt(seconds * 1000f));
            int minutes = totalMilliseconds / 60000;
            int remaining = totalMilliseconds % 60000;
            int wholeSeconds = remaining / 1000;
            int milliseconds = remaining % 1000;
            return $"{minutes:00}:{wholeSeconds:00}.{milliseconds:000}";
        }
    }
}
