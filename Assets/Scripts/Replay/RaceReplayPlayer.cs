using System.IO;
using System.Collections.Generic;
using OriginalKartRacer.Race;
using OriginalKartRacer.Vehicle;
using UnityEngine;

namespace OriginalKartRacer.Replay
{
    /// <summary>按记录帧插值播放。回放不重新运行物理，因此车辆和仪表状态与记录结果一致。</summary>
    [RequireComponent(typeof(ReplayTelemetry))]
    public sealed class RaceReplayPlayer : MonoBehaviour
    {
        [SerializeField] private KartController kartController;
        [SerializeField] private string replayFileName;
        [SerializeField, Min(0.1f)] private float playbackSpeed = 1f;
        [SerializeField] private bool playOnStart;

        private ReplayTelemetry telemetry;
        private RaceReplayData data;
        private float replayTime;
        private int frameIndex;
        private sealed class VehicleState
        {
            public KartController kart;
            public Rigidbody body;
            public Vector3 position, velocity;
            public Quaternion rotation;
            public bool kinematic, collision;
            public Behaviour[] behaviours;
            public bool[] enabled;
        }
        private readonly List<VehicleState> saved = new List<VehicleState>();

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            telemetry = GetComponent<ReplayTelemetry>();
            if (kartController == null) kartController = GetComponent<KartController>();
        }
        private void Start()
        {
            if (playOnStart)
                LoadAndPlay(replayFileName);
        }

        private void Update()
        {
            if (!IsPlaying || data == null || data.frames.Count < 2)
                return;

            replayTime += Time.deltaTime * playbackSpeed;
            while (frameIndex < data.frames.Count - 2 && data.frames[frameIndex + 1].time < replayTime)
                frameIndex++;

            ReplayFrame a = data.frames[frameIndex];
            ReplayFrame b = data.frames[Mathf.Min(frameIndex + 1, data.frames.Count - 1)];
            float t = Mathf.InverseLerp(a.time, b.time, replayTime);
            transform.SetPositionAndRotation(Vector3.Lerp(a.position, b.position, t), Quaternion.Slerp(a.rotation, b.rotation, t));
            telemetry.SetInterpolatedFrame(Interpolate(a, b, t),b,t);
            if (a.rivals != null && b.rivals != null)
                foreach (var state in saved)
                    for (int i=0;i<Mathf.Min(a.rivals.Length,b.rivals.Length);i++)
                        if (a.rivals[i].name == state.kart.name && state.kart != kartController)
                            state.kart.transform.SetPositionAndRotation(Vector3.Lerp(a.rivals[i].position,b.rivals[i].position,t),Quaternion.Slerp(a.rivals[i].rotation,b.rivals[i].rotation,t));

            if (replayTime >= data.duration)
                StopPlayback();
        }

        public void LoadAndPlay(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            if (IsPlaying) StopPlayback();
            string path = Path.IsPathRooted(fileName)
                ? fileName
                : Path.Combine(Application.persistentDataPath, "Replays", fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Replay not found: {path}", this);
                return;
            }
            try { data = JsonUtility.FromJson<RaceReplayData>(File.ReadAllText(path)); }
            catch (System.Exception exception) { Debug.LogWarning("Cannot load replay: " + exception.Message); return; }
            if (data == null || data.frames == null || data.frames.Count < 2)
            {
                Debug.LogWarning("Replay has no playable frames.", this);
                return;
            }
            replayTime = data.frames[0].time;
            frameIndex = 0;
            IsPlaying = true;
            saved.Clear();
            foreach (var kart in FindObjectsOfType<KartController>())
            {
                var body=kart.GetComponent<Rigidbody>();
                var behaviours=new Behaviour[]{kart,kart.GetComponent<DriftNitroSystem>(),kart.GetComponent<RaceAIDriver>(),kart.GetComponent<RoadSurfaceDetector>()};
                var state=new VehicleState{kart=kart,body=body,position=body.position,rotation=body.rotation,velocity=body.velocity,kinematic=body.isKinematic,collision=body.detectCollisions,behaviours=behaviours,enabled=new bool[behaviours.Length]};
                for(int i=0;i<behaviours.Length;i++)if(behaviours[i]!=null){state.enabled[i]=behaviours[i].enabled;behaviours[i].enabled=false;}
                body.velocity=Vector3.zero;body.isKinematic=true;body.detectCollisions=false;saved.Add(state);
            }
        }

        public void StopPlayback()
        {
            IsPlaying = false;
            if(telemetry!=null)telemetry.Stop();
            foreach(var state in saved)
            {
                if(state.body==null)continue;
                state.body.position=state.position;state.body.rotation=state.rotation;state.body.isKinematic=state.kinematic;state.body.detectCollisions=state.collision;
                if(!state.kinematic)state.body.velocity=state.velocity;
                for(int i=0;i<state.behaviours.Length;i++)if(state.behaviours[i]!=null)state.behaviours[i].enabled=state.enabled[i];
            }
            saved.Clear();
        }

        private void OnDisable(){if(IsPlaying)StopPlayback();}

        private static ReplayFrame Interpolate(ReplayFrame a, ReplayFrame b, float t)
        {
            a.position = Vector3.Lerp(a.position, b.position, t);
            a.rotation = Quaternion.Slerp(a.rotation, b.rotation, t);
            a.velocity = Vector3.Lerp(a.velocity, b.velocity, t);
            a.speedKph = Mathf.Lerp(a.speedKph, b.speedKph, t);
            a.nitroCharge = Mathf.Lerp(a.nitroCharge, b.nitroCharge, t);
            a.engineRpm = Mathf.Lerp(a.engineRpm, b.engineRpm, t);
            a.lapTime = Mathf.Lerp(a.lapTime, b.lapTime, t);
            a.totalTime = Mathf.Lerp(a.totalTime, b.totalTime, t);
            return a;
        }
    }
}
