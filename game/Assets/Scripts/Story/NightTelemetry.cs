using System;
using UnityEngine;
namespace BaGio12.Story
{
    // Local payloads only. A consent-aware adapter owns buffering and network delivery (#25).
    [Serializable] public abstract class NightTelemetry
    {
        static readonly string Session = Guid.NewGuid().ToString();
        static int nextSequence;
        public string eventId = Guid.NewGuid().ToString();
        public string sessionId = Session;
        public int sequence = nextSequence++;
        public string occurredAt = DateTime.UtcNow.ToString("O");
        public string type;
        public int night = 1;
        public string ToJson() => JsonUtility.ToJson(this);
    }
    [Serializable] public sealed class NightStartedTelemetry : NightTelemetry
    { public bool isReplay; public NightStartedTelemetry(bool replay) { type="night_started"; isReplay=replay; } }
    [Serializable] public sealed class NightCompletedTelemetry : NightTelemetry
    { public bool isReplay; public int durationSeconds; public NightCompletedTelemetry(bool replay,int seconds) {type="night_completed";isReplay=replay;durationSeconds=seconds;} }
    [Serializable] public sealed class CaughtTelemetry : NightTelemetry
    {
        public string areaId, checkpointId;
        public Vector3 position;
        public CaughtTelemetry(string area,string checkpoint,Vector3 point) {type="player_caught";areaId=area;checkpointId=checkpoint;position=point;}
    }
    [Serializable] public sealed class ChoiceTelemetry : NightTelemetry
    { public string choiceId; public ChoiceTelemetry(string id) {if(!System.Text.RegularExpressions.Regex.IsMatch(id??"",@"^[0-9]{1,2}[a-z]?$"))throw new ArgumentException("Invalid canonical choiceId",nameof(id));type="choice_made";choiceId=id;} }
    [Serializable] public sealed class EndingTelemetry : NightTelemetry
    { public string endingId; public EndingTelemetry(string id) {type="ending_reached";endingId=id;} }
}
