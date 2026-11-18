using BaGio12.Persistence;
using UnityEngine;
namespace BaGio12.Stealth
{
    public sealed partial class NightObserver
    {
        public ObserverSaveData CaptureObserver() => new ObserverSaveData {
            id = name, mode = Mode, position = transform.position, rotation = transform.rotation, lastSeen = lastSeen,
            attention = Attention, elapsed = elapsed, modeTime = modeTime, lampTime = lampTime, nextRandom = nextRandom,
            watchUntil = watchUntil, focusRemaining = Mathf.Max(0, focusUntil - Time.time), interval = interval,
            watchDuration = watchDuration, startDelay = startDelay, lastVisible = lastVisible
        };
        public void ApplyObserver(ObserverSaveData data)
        {
            if (agent != null && agent.isOnNavMesh) { agent.Warp(data.position); agent.ResetPath(); agent.isStopped = true; }
            else transform.position = data.position;
            transform.rotation = data.rotation; Mode = data.mode; Attention = data.attention; lastSeen = data.lastSeen;
            elapsed = data.elapsed; modeTime = data.modeTime; lampTime = data.lampTime; nextRandom = data.nextRandom;
            watchUntil = data.watchUntil; focusUntil = Time.time + data.focusRemaining; interval = data.interval;
            watchDuration = data.watchDuration; startDelay = data.startDelay; lastVisible = data.lastVisible;
        }
    }
}
