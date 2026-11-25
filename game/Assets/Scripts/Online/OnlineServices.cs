using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BaGio12.Persistence;
using BaGio12.Story;
using UnityEngine;
using UnityEngine.Networking;

namespace BaGio12.Online
{
    public sealed class OnlineServices : MonoBehaviour
    {
        const int TimeoutSeconds = 15;

        LocalJsonStore<PlayerProfile> profiles;
        LocalJsonStore<TelemetryQueueData> queues;
        TelemetryQueueData queue;
        string gameVersion, baseUrl;
        bool uploading, sendRequested, submitting;
        int maxEvents = TelemetryQueue.MaxEvents;
        double backoff;
        float nextAttempt;

        public PlayerProfile Profile { get; private set; }
        public int QueuedEvents => queue == null ? 0 : queue.events.Count;
        public bool Uploading => uploading;
        public bool Submitting => submitting;
        public string LastUploadError { get; private set; } = "";
        public static bool TelemetrySupported =>
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
        public bool AskConsent => TelemetrySupported && Profile != null && Profile.Consent == TelemetryConsent.Unanswered;
        public bool ConsentGranted => TelemetrySupported && Profile != null && Profile.Consent == TelemetryConsent.Granted;
        public bool CanSubmitEvidence => Profile != null && Profile.homeReached;

        public void Initialize(string dataRoot)
        {
            gameVersion = Application.version;
            baseUrl = ApiConfig.BaseUrl;
            profiles = new LocalJsonStore<PlayerProfile>(Path.Combine(dataRoot, "profile.json"), PlayerProfile.Valid);
            queues = new LocalJsonStore<TelemetryQueueData>(Path.Combine(dataRoot, "telemetry-queue.json"), TelemetryQueueData.Valid);
            if (!profiles.TryLoad(out var profile, out _)) { profile = PlayerProfile.Create(); profiles.Save(profile, out _); }
            Profile = profile;
            if (!queues.TryLoad(out queue, out _)) queue = new TelemetryQueueData();
            if (!ConsentGranted && queue.events.Count > 0) { queue.events.Clear(); SaveQueue(); }
            if (ConsentGranted) BeginSession();
            sendRequested = true;
        }

        public void SetConsent(bool granted)
        {
            if (Profile == null) return;
            var before = Profile.Consent;
            if (granted)
            {
                Profile.Consent = TelemetryConsent.Granted;
                Profile.pendingRevocations.Remove(Profile.installationId);
                SaveProfile();
                if (before != TelemetryConsent.Granted) BeginSession();
                sendRequested = true;
                return;
            }
            Profile.Consent = TelemetryConsent.Declined;
            if (before == TelemetryConsent.Granted)
            {
                queue.events.Clear(); SaveQueue();
                if (!Profile.pendingRevocations.Contains(Profile.installationId)) Profile.pendingRevocations.Add(Profile.installationId);
                Profile.ClearOpenSession();
                nextAttempt = 0; backoff = 0;
            }
            SaveProfile();
        }

        public void Record(NightTelemetry e)
        {
            if (Profile == null || e == null) return;
            if (e is EndingTelemetry ending && ending.endingId == PlayerProfile.HomeEnding && !Profile.homeReached)
            { Profile.homeReached = true; SaveProfile(); }
            if (!ConsentGranted) return;
            Enqueue(e);
            if (queue.events.Count >= TelemetryQueue.SendThreshold || e.type == "night_completed") sendRequested = true;
        }

        public void EndSession(string reason)
        {
            if (!ConsentGranted || Profile.openSessionId == "") return;
            var ended = new SessionEndedTelemetry(reason, SecondsSince(Profile.openSessionStartedAt, DateTime.UtcNow), Profile.openSessionNight);
            Enqueue(ended);
            Profile.ClearOpenSession();
            SaveProfile();
        }

        public void ResetProfile()
        {
            if (Profile == null) return;
            var consent = Profile.Consent;
            var revocations = Profile.pendingRevocations;
            queue.events.Clear(); SaveQueue();
            Profile = PlayerProfile.Create();
            Profile.Consent = consent;
            Profile.pendingRevocations = revocations;
            SaveProfile();
            if (ConsentGranted) BeginSession();
        }

        void BeginSession()
        {
            if (Profile.openSessionId != "" && Profile.openSessionId != NightTelemetry.CurrentSession)
            {
                var stale = new SessionEndedTelemetry("not_closed",
                    SecondsSince(Profile.openSessionStartedAt, ParseUtc(Profile.openSessionLastSeenAt) ?? DateTime.UtcNow), Profile.openSessionNight)
                { sessionId = Profile.openSessionId, sequence = Profile.openSessionNextSequence };
                if (ParseUtc(Profile.openSessionLastSeenAt) is DateTime lastSeen) stale.occurredAt = lastSeen.ToString("O", CultureInfo.InvariantCulture);
                TelemetryQueue.Add(queue, stale.ToJson(), ParseUtc(stale.occurredAt) ?? DateTime.UtcNow);
                Profile.ClearOpenSession();
            }
            if (Profile.openSessionId == NightTelemetry.CurrentSession) return;
            Profile.openSessionId = NightTelemetry.CurrentSession;
            Profile.openSessionStartedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            Enqueue(new SessionStartedTelemetry(Profile.openSessionNight));
        }

        void Enqueue(NightTelemetry e)
        {
            var at = ParseUtc(e.occurredAt) ?? DateTime.UtcNow;
            TelemetryQueue.Add(queue, e.ToJson(), at);
            SaveQueue();
            if (e.sessionId == Profile.openSessionId)
            {
                Profile.openSessionLastSeenAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                Profile.openSessionNextSequence = Math.Max(Profile.openSessionNextSequence, e.sequence + 1);
                if (e.night >= 1 && e.night <= 9) Profile.openSessionNight = e.night;
                SaveProfile();
            }
        }

        static DateTime? ParseUtc(string value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal, out var at) ? at : (DateTime?)null;

        static int SecondsSince(string startedAt, DateTime end) =>
            ParseUtc(startedAt) is DateTime start ? Math.Max(0, (int)(end - start).TotalSeconds) : 0;

        void SaveProfile() { if (profiles != null && Profile != null) profiles.Save(Profile, out _); }

        void SaveQueue() { if (queues != null && queue != null) queues.Save(queue, out _); }

        void Update()
        {
            if (Profile == null || uploading || Time.realtimeSinceStartup < nextAttempt) return;
            if (Profile.pendingRevocations.Count > 0) { StartCoroutine(Revoke(Profile.pendingRevocations[0])); return; }
            if (!ConsentGranted || !sendRequested) return;
            if (TelemetryQueue.Prune(queue, DateTime.UtcNow) > 0) SaveQueue();
            int count = TelemetryQueue.BatchSize(queue, Profile.installationId, gameVersion, maxEvents);
            if (count == 0) { sendRequested = false; return; }
            StartCoroutine(Upload(count));
        }

        IEnumerator Upload(int count)
        {
            uploading = true;
            string body = TelemetryQueue.Envelope(Profile.installationId, gameVersion, queue.events, 0, count);
            var first = queue.events[0];
            using (var request = Json("POST", baseUrl + "/api/v1/telemetry/batches", body))
            {
                yield return request.SendWebRequest();
                var decision = TelemetryPolicy.Decide(Status(request), request.GetResponseHeader("Retry-After"), count, maxEvents, backoff);
                if (decision.logError) Debug.LogError($"Telemetry batch dropped with HTTP {request.responseCode}: {request.downloadHandler?.text}");
                if (decision.remove > 0 && queue.events.Count >= decision.remove && ReferenceEquals(queue.events[0], first))
                { queue.events.RemoveRange(0, decision.remove); SaveQueue(); }
                maxEvents = decision.maxEvents;
                backoff = decision.backoffSeconds;
                nextAttempt = Time.realtimeSinceStartup + (float)decision.waitSeconds;
                LastUploadError = decision.remove > 0 && !decision.logError ? "" : $"HTTP {request.responseCode} {request.error}";
            }
            uploading = false;
        }

        IEnumerator Revoke(string installationId)
        {
            uploading = true;
            using (var request = UnityWebRequest.Delete(baseUrl + "/api/v1/telemetry/installations/" + installationId))
            {
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();
                long status = Status(request);
                if (status == 204 || status == 200 || status == 404)
                {
                    Profile.pendingRevocations.Remove(installationId); SaveProfile(); backoff = 0;
                }
                else
                {
                    var decision = TelemetryPolicy.Decide(status, request.GetResponseHeader("Retry-After"), 0, maxEvents, backoff);
                    backoff = decision.backoffSeconds;
                    nextAttempt = Time.realtimeSinceStartup + (float)Math.Max(1, decision.waitSeconds);
                }
            }
            uploading = false;
        }

        public void SubmitEvidence(Action<string> report)
        {
            if (submitting) return;
            if (!FilePicker.Supported) { report("Bản game cho máy này chưa chọn được tệp, nên bạn hãy dùng bản Windows để gửi bằng chứng."); return; }
            string path = FilePicker.PickImage("Chọn ảnh bằng chứng gửi cho công an");
            if (string.IsNullOrEmpty(path)) { report("Bạn chưa chọn tệp nào, nên chưa có gì được gửi."); return; }
            StartCoroutine(Submit(path, report));
        }

        public IEnumerator Submit(string path, Action<string> report)
        {
            submitting = true;
            report("Game đang đọc tệp bạn chọn.");
            var hashing = Task.Run(() => (ok: Evidence.TryHash(path, out var sha, out var message), sha, message));
            while (!hashing.IsCompleted) yield return null;
            var hashed = hashing.Result;
            if (!hashed.ok) { report(hashed.message); submitting = false; yield break; }
            report("Game đang gửi mã của tệp tới máy chủ.");
            string url = baseUrl + "/api/v1/puzzles/" + Evidence.PuzzleId + "/submissions";
            using (var request = Json("POST", url, Evidence.RequestBody(Profile.installationId, gameVersion, hashed.sha)))
            {
                yield return request.SendWebRequest();
                long status = Status(request);
                if (status == 400) Debug.LogError("Evidence submission rejected as malformed: " + request.downloadHandler?.text);
                var outcome = Evidence.Interpret(status, request.downloadHandler?.text, request.GetResponseHeader("Retry-After"), DateTime.Now);
                if (outcome.accepted) { Profile.Unlock(outcome.unlockId); SaveProfile(); }
                report(outcome.message);
            }
            submitting = false;
        }

        static UnityWebRequest Json(string method, string url, string body)
        {
            var request = new UnityWebRequest(url, method)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = TimeoutSeconds,
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        static long Status(UnityWebRequest request) =>
            request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError ? 0 : request.responseCode;
    }
}
