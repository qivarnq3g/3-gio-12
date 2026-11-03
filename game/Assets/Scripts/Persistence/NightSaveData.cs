using System;
using BaGio12.Story;
using BaGio12.Stealth;
using UnityEngine;

namespace BaGio12.Persistence
{
    [Serializable] public sealed class NightSaveData
    {
        public int schemaVersion = 1;
        public string scene, savedAt;
        public NightOneState state, checkpointState;
        public string checkpointId, objective, modal;
        public Vector3 position, checkpointPosition;
        public float hidingSeconds;
        public float yaw, pitch, checkpointYaw, roomSeconds, unseenSeconds, immuneRemaining, prologueElapsed, playedSeconds;
        public bool crouched, leftStart, prologueDeparted, isReplay, completedInSession;
        public int activePhone, shoutCount;
        public bool[] alertLights;
        public bool parkingAlert;
        public ObserverSaveData[] observers;
        static bool VectorValid(Vector3 v) => GamePreferences.Finite(v.x) && GamePreferences.Finite(v.y) && GamePreferences.Finite(v.z)
            && Mathf.Abs(v.x) < 1000 && v.y > -10 && v.y < 1000 && Mathf.Abs(v.z) < 1000;
        static bool StateValid(NightOneState s) => s != null && s.schemaVersion == 2 && Enum.IsDefined(typeof(NightStage), s.stage)
            && s.clockDay >= 0 && s.clockDay <= 30 && TimeSpan.TryParseExact(s.clock, @"hh\:mm", System.Globalization.CultureInfo.InvariantCulture, out _)
            && s.prologue_beaten >= 0 && s.prologue_beaten < 3 && s.sent != null && s.messages != null && s.pendingMessages != null && s.notebook != null && s.afterJumpQuestions != null;
        public static bool Valid(NightSaveData d)
        {
            if (d.schemaVersion != 1 || d.scene != "NightOneGraybox" || !StateValid(d.state) || !StateValid(d.checkpointState)
                || string.IsNullOrEmpty(d.checkpointId) || !VectorValid(d.position) || !VectorValid(d.checkpointPosition)
                || !GamePreferences.Finite(d.yaw) || !GamePreferences.Finite(d.pitch) || !GamePreferences.Finite(d.checkpointYaw)
                || d.pitch < -80 || d.pitch > 80 || d.activePhone < 0 || d.activePhone > 2 || d.observers == null || d.alertLights == null) return false;
            foreach (var n in new[] { d.hidingSeconds, d.roomSeconds, d.unseenSeconds, d.immuneRemaining, d.prologueElapsed, d.playedSeconds })
                if (!GamePreferences.Finite(n) || n < 0) return false;
            foreach (var o in d.observers) if (o == null || !o.Valid()) return false;
            return true;
        }
    }
    [Serializable] public sealed class ObserverSaveData
    {
        public string id;
        public ObserverMode mode;
        public Vector3 position, lastSeen;
        public Quaternion rotation;
        public float attention, elapsed, modeTime, lampTime, nextRandom, watchUntil, focusRemaining, interval, watchDuration, startDelay;
        public bool lastVisible;
        public bool Valid()
        {
            if (string.IsNullOrEmpty(id) || !Enum.IsDefined(typeof(ObserverMode), mode)) return false;
            foreach (var n in new[] {position.x,position.y,position.z,lastSeen.x,lastSeen.y,lastSeen.z,rotation.x,rotation.y,rotation.z,rotation.w,
                attention,elapsed,modeTime,lampTime,nextRandom,watchUntil,focusRemaining,interval,watchDuration,startDelay})
                if (!GamePreferences.Finite(n)) return false;
            return elapsed >= 0 && modeTime >= 0 && lampTime >= 0 && attention >= 0 && attention <= 1 && interval > 0;
        }
    }
}
