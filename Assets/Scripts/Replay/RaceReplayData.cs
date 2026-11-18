using System;
using System.Collections.Generic;
using UnityEngine;
using OriginalKartRacer.Vehicle;

namespace OriginalKartRacer.Replay
{
    [Serializable]
    public struct ReplayFrame
    {
        public float time;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public float speedKph;
        public float nitroCharge;
        public float lapTime;
        public float totalTime;
        public int lap;
        public float throttle;
        public float steering;
        public bool drifting;
        public bool boosting;
        public int positionInRace;
        public float engineRpm;
        public int gear;
        public WheelReplayFrame[] wheels;
        public RivalReplayFrame[] rivals;
    }

    [Serializable]
    public struct WheelReplayFrame
    {
        public Vector3 localPosition;
        public float steerAngle;
        public float rollAngle;
    }

    [Serializable]
    public struct RivalReplayFrame
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    public sealed class RaceReplayData
    {
        public string trackId;
        public string recordedUtc;
        public float duration;
        public List<ReplayFrame> frames = new List<ReplayFrame>();
    }

}
