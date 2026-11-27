using System;
using System.Collections.Generic;

namespace BaGio12.Online
{
    public enum TelemetryConsent { Unanswered = 0, Granted = 1, Declined = 2 }

    [Serializable] public sealed class PlayerProfile
    {
        public const string HomeEnding = "home";
        public const string SandalUnlock = "ending_red_thread_sandal";

        public int schemaVersion = 1;
        public string installationId = "";
        public int consent;
        public bool homeReached;
        public List<string> unlocks = new List<string>();
        public List<string> pendingRevocations = new List<string>();
        public string openSessionId = "";
        public string openSessionStartedAt = "";
        public string openSessionLastSeenAt = "";
        public int openSessionNextSequence;
        public int openSessionNight = 1;

        public TelemetryConsent Consent { get => (TelemetryConsent)consent; set => consent = (int)value; }

        public static PlayerProfile Create() => new PlayerProfile { installationId = Guid.NewGuid().ToString() };

        public static bool Valid(PlayerProfile p) =>
            p != null && p.schemaVersion == 1 && Guid.TryParse(p.installationId, out var id) && id != Guid.Empty
            && p.consent >= 0 && p.consent <= 2 && p.unlocks != null && p.pendingRevocations != null
            && p.pendingRevocations.TrueForAll(x => Guid.TryParse(x, out _))
            && (p.openSessionId == "" || Guid.TryParse(p.openSessionId, out _))
            && p.openSessionNextSequence >= 0 && p.openSessionNight >= 1 && p.openSessionNight <= 9;

        public bool HasUnlock(string id) => unlocks.Contains(id);

        public void Unlock(string id) { if (!string.IsNullOrEmpty(id) && !unlocks.Contains(id)) unlocks.Add(id); }

        public void ClearOpenSession()
        {
            openSessionId = openSessionStartedAt = openSessionLastSeenAt = "";
            openSessionNextSequence = 0;
            openSessionNight = 1;
        }
    }
}
