using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaGio12.Story
{
    public enum NightStage { Prologue, Parking, Exploration, Room, LateNight, Ended }
    [Serializable]
    public sealed class NightOneState
    {
        public int schemaVersion = 1;
        public NightStage stage;
        public string clock = "20:56", ending = "", number_choice = "ignored";
        public int prologue_beaten, phone_left_count, odo_2100 = 12400;
        public bool phonePlaced, bikeParked, hasNganPhone, phoneEverPicked, phone_powered_off;
        public bool saw_shop_camera, found_deposit_slip, asked_lien_keep, helpedLien;
        public bool linh_knows_shop_camera, asked_mom_why_here, timeJumped, numberArrived, night1_slept;
        public bool bagMoved, bagInspected, bagDisplaced, boughtNoodles, alarm, batchD;
        public List<string> sent = new List<string>();
        public List<string> messages = new List<string>();
        public List<string> pendingMessages = new List<string>();
        public List<string> notebook = new List<string>();
        public void Note(string text)
        {
            if (!notebook.Exists(x => x.EndsWith(" — " + text))) notebook.Add(clock + " — " + text);
        }
        public NightOneState Copy() => JsonUtility.FromJson<NightOneState>(JsonUtility.ToJson(this));
    }
    public sealed class StoryClock
    {
        readonly NightOneState state;
        public StoryClock(NightOneState state) { this.state = state; }
        public void Set(string time) { state.clock = time; }
    }
    public sealed class CheckpointService
    {
        public string Id { get; private set; }
        NightOneState snapshot;
        Vector3 position;
        float yaw;
        public void Save(string id, NightOneState state, Vector3 point, float facing)
        { Id = id; snapshot = state.Copy(); position = point; yaw = facing; }
        public bool Restore(out NightOneState state, out Vector3 point, out float facing)
        { state = snapshot?.Copy(); point = position; facing = yaw; return state != null; }
    }
    [Serializable] public sealed class NightDialogue
    {
        public string[] batchA;
        public string batchB, cameraYes, cameraNo, why, warning, stranger, batchD, callReply;
    }
}
