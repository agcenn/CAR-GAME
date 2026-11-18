using System;
using System.IO;
using OriginalKartRacer.Race;
using OriginalKartRacer.Vehicle;
using UnityEngine;

namespace OriginalKartRacer.Replay
{
    /// <summary>
    /// 按固定采样率记录车辆姿态、输入、氮气和比赛仪表数据。
    /// 赛后调用 SaveReplay 写入 persistentDataPath，不会覆盖项目资源。
    /// </summary>
    [RequireComponent(typeof(KartController))]
    public sealed class RaceReplayRecorder : MonoBehaviour
    {
        [SerializeField] private RaceClock raceClock;
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private DriftNitroSystem nitro;
        [SerializeField, Min(10)] private int samplesPerSecond = 30;
        [SerializeField] private string trackId = "neon_harbor_coast_02";
        [SerializeField] private bool autoRecord = true;

        private KartController kart;
        private Rigidbody body;
        private RaceReplayData data;
        private float nextSampleTime;
        private RacePositionBoard board;
        private KartController[] rivals;
        private KartVisualMotion visual;

        public bool IsRecording { get; private set; }
        public RaceReplayData CurrentReplay => data;
        public string LastSavedPath { get; private set; }

        private void Awake()
        {
            kart = GetComponent<KartController>();
            body = GetComponent<Rigidbody>();
            board = FindObjectOfType<RacePositionBoard>();
            rivals = FindObjectsOfType<KartController>();
            visual = GetComponent<KartVisualMotion>();
        }

        private void Update()
        {
            if (autoRecord && !IsRecording && raceClock != null && raceClock.IsRunning)
                StartRecording();
            if (IsRecording && raceManager != null && raceManager.IsFinished)
                StopAndSave();
        }

        private void FixedUpdate()
        {
            if (!IsRecording || Time.time < nextSampleTime)
                return;
            nextSampleTime += 1f / samplesPerSecond;
            RecordFrame();
        }

        public void StartRecording()
        {
            data = new RaceReplayData
            {
                trackId = trackId,
                recordedUtc = DateTime.UtcNow.ToString("O")
            };
            IsRecording = true;
            nextSampleTime = Time.time;
        }

        public void StopAndSave()
        {
            if (!IsRecording)
                return;
            RecordFrame();
            IsRecording = false;
            if (data.frames.Count > 0)
                data.duration = data.frames[data.frames.Count - 1].time;
            SaveReplay();
        }

        public void SaveReplay()
        {
            if (data == null || data.frames.Count == 0)
                return;
            string folder = Path.Combine(Application.persistentDataPath, "Replays");
            Directory.CreateDirectory(folder);
            string filename = $"{trackId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            LastSavedPath = Path.Combine(folder, filename);
            File.WriteAllText(LastSavedPath, JsonUtility.ToJson(data, true));
            Debug.Log($"Replay saved: {LastSavedPath}", this);
        }

        private void RecordFrame()
        {
            RaceVehicleInput input = kart.CurrentInput;
            var rivalFrames = new RivalReplayFrame[rivals.Length];
            for (int i=0;i<rivals.Length;i++)
                rivalFrames[i]=new RivalReplayFrame{name=rivals[i].name,position=rivals[i].transform.position,rotation=rivals[i].transform.rotation};
            data.frames.Add(new ReplayFrame
            {
                time = raceClock == null ? Time.time : raceClock.RaceTime,
                position = transform.position,
                rotation = transform.rotation,
                velocity = body == null ? Vector3.zero : body.velocity,
                speedKph = kart.SpeedKph,
                nitroCharge = nitro == null ? 0f : nitro.Charge,
                lapTime = raceClock == null ? 0f : raceClock.CurrentLapTime,
                totalTime = raceClock == null ? 0f : raceClock.RaceTime,
                lap = raceManager == null ? 1 : raceManager.CurrentLap,
                throttle = input.Throttle,
                steering = input.Steering,
                drifting = nitro != null && nitro.IsDrifting,
                boosting = nitro != null && nitro.IsBoosting,
                positionInRace = board == null ? 1 : board.GetPosition(kart),
                engineRpm = kart.EngineRpm,
                gear = kart.Gear,
                wheels = visual==null?null:visual.CaptureWheels(),
                rivals = rivalFrames
            });
        }
    }
}
