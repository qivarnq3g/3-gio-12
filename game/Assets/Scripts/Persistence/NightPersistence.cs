using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using BaGio12.Gameplay;
using BaGio12.Online;
using BaGio12.Story;
using UnityEngine;

namespace BaGio12.Persistence
{
    public sealed class NightPersistence : MonoBehaviour, IGameSession
    {
        public NightOneDirector story;
        public GamePreferences Preferences { get; private set; }
        public string Status
        {
            get => AtStart && AskTelemetryConsent ? "Trước khi chơi, bạn chọn có gửi thống kê ẩn danh cho nhóm làm game hay không." : status;
            private set => status = value;
        }
        string status = "";
        public OnlineServices Online { get; private set; }
        public bool AskTelemetryConsent => Online != null && Online.AskConsent;
        public bool TelemetryConsent
        {
            get => Online != null && Online.ConsentGranted;
            set { if (Online != null) Online.SetConsent(value); }
        }
        public void AnswerTelemetryConsent(bool granted)
        {
            if (Online == null) return;
            Online.SetConsent(granted);
            Status = granted ? "Đã bật gửi thống kê ẩn danh. Bạn đổi ý được trong Cài đặt." : "Game sẽ không gửi thống kê. Bạn đổi ý được trong Cài đặt.";
        }
        public bool CanSubmitEvidence => Online != null && Online.CanSubmitEvidence;
        public void SubmitEvidence() { if (Online != null) Online.SubmitEvidence(message => Status = message); }
        public bool Ready { get; private set; }
        public bool HasSave => saves != null && saves.Exists;
        public string SaveSummary
        {
            get
            {
                if (saves == null || !saves.TryLoad(out var save, out _)) return null;
                string text = $"Bạn quay lại lúc {save.state.clock} trong đêm, đúng chỗ của bản lưu gần nhất";
                if (DateTime.TryParse(save.savedAt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var at))
                    text += $", lưu ngày {at.ToLocalTime():dd/MM} lúc {at.ToLocalTime():HH:mm}";
                return text + ".";
            }
        }
        LocalJsonStore<NightSaveData> saves;
        LocalJsonStore<GamePreferences> preferences;
        NightSaveData initial, lastStable;
        bool startup, playing, quitting;
        float nextCheck, retryAt;
        string written, savedMilestone;
        Task<(bool ok, string message, string payload, string milestone)> pending;
        public bool AtStart => Ready && startup;
        public bool ShowWarning
        {
            get => Preferences == null || Preferences.showContentWarning;
            set { if (Preferences != null) Preferences.showContentWarning = value; }
        }
        public int Quality
        {
            get => Preferences == null || Preferences.quality < 0 ? QualitySettings.GetQualityLevel() : Preferences.quality;
            set
            {
                if (Preferences == null) return;
                Preferences.quality = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
                if (Preferences.quality != QualitySettings.GetQualityLevel()) QualitySettings.SetQualityLevel(Preferences.quality, true);
            }
        }
        public static string DataRoot => System.Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT") ?? Path.Combine(Application.persistentDataPath, "local-v1");
        IEnumerator Start()
        {
            // All observer Start methods must establish their home and NavMeshAgent first.
            yield return null;
            saves = new LocalJsonStore<NightSaveData>(Path.Combine(DataRoot, "night-one.json"), NightSaveData.Valid);
            preferences = new LocalJsonStore<GamePreferences>(Path.Combine(DataRoot, "preferences.json"), GamePreferences.Valid);
            if (!preferences.TryLoad(out var loaded, out var warning)) loaded = new GamePreferences();
            Preferences = loaded; Preferences.Apply(story.player); Status = warning;
            Online = gameObject.AddComponent<OnlineServices>(); Online.Initialize(DataRoot); story.TelemetryProduced += Online.Record;
            initial = story.CaptureSave(); Ready = true; story.player.Session = this;
            if (System.Environment.GetEnvironmentVariable("THREEGIO12_SKIP_START_MENU") == "1") { playing = true; story.StartNightTelemetry(); yield break; }
            startup = true; story.player.SetPaused(true); story.player.PauseMenuLocked = true;
        }
        void LateUpdate()
        {
            if (pending != null && pending.IsCompleted) Settle();
            if (!Ready || !playing || story.Busy || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .25f;
            var snapshot = story.CaptureSave();
            if (snapshot == null || !NightSaveData.Valid(snapshot)) return;
            lastStable = snapshot;
            string milestone = Milestone(snapshot);
            if (milestone != savedMilestone && Time.unscaledTime >= retryAt) Autosave(milestone);
        }
        static string Milestone(NightSaveData d) => JsonUtility.ToJson(d.state) + "|" + d.checkpointId + "|" + d.modal;
        void Autosave(string milestone)
        {
            if (pending != null || lastStable == null) return;
            var bytes = saves.Encode(lastStable, out var payload, out var message);
            if (bytes == null) { Status = message; return; }
            if (payload == written) { savedMilestone = milestone; return; }
            var store = saves;
            pending = Task.Run(() => { bool ok = store.Write(bytes, out var error); return (ok, error, payload, milestone); });
        }
        void Settle()
        {
            if (pending == null) return;
            var result = pending.Result; pending = null;
            if (result.ok) { written = result.payload; savedMilestone = result.milestone; Status = "Đã tự lưu tiến độ."; }
            else { Status = result.message; retryAt = Time.unscaledTime + 10; }
        }
        public bool Flush()
        {
            if (!Ready || !playing) return false;
            if (!story.Busy)
            {
                var snapshot = story.CaptureSave();
                if (snapshot != null && NightSaveData.Valid(snapshot)) lastStable = snapshot;
            }
            if (lastStable == null) return false;
            Settle();
            var bytes = saves.Encode(lastStable, out var payload, out var message);
            if (bytes == null) { Status = message; return false; }
            if (payload == written) return true;
            bool ok = saves.Write(bytes, out message);
            if (ok) { written = payload; savedMilestone = Milestone(lastStable); }
            Status = ok ? "Đã tự lưu tiến độ." : message; return ok;
        }
        public bool ContinueSaved()
        {
            if (!Ready) return false;
            if (!saves.TryLoad(out var save, out var message)) { Status = message; return false; }
            if (!story.ApplySave(save)) { Status = "Bản lưu không khớp màn chơi hiện tại. Bản lưu vẫn được giữ lại."; return false; }
            Settle(); written = null; savedMilestone = Milestone(save);
            lastStable = save; playing = true; Status = message; CloseMenu(); return true;
        }
        public bool StartNewGame()
        {
            if (!Ready || initial == null || !NightSaveData.Valid(initial)) { Status = "Chưa thể bắt đầu lại màn chơi."; return false; }
            Settle(); written = savedMilestone = null;
            if (!saves.Delete(out var message)) { Status = message; return false; }
            if (!story.ApplySave(initial)) { Status = "Chưa thể bắt đầu lại màn chơi."; return false; }
            lastStable = null; playing = true;
            if (!Flush()) { startup = true; story.player.SetPaused(true); story.player.PauseMenuLocked = true; return false; }
            CloseMenu(); story.StartNightTelemetry(); story.Say("Trong hẻm vẫn còn người ngồi trước cửa nhà, và bạn không được để bất kỳ ai nhìn thấy mình trên đường vào nhà xe.", 9); return true;
        }
        public bool DeleteProgress()
        {
            if (!Ready) return false;
            Settle(); written = savedMilestone = null;
            if (!saves.Delete(out var message)) { Status = message; return false; }
            story.ClearSessionCompletion();
            Online?.ResetProfile();
            // A paused current session must not silently re-create the deleted slot.
            playing = false; lastStable = null; startup = true;
            story.player.SetPaused(true); story.player.PauseMenuLocked = true;
            Status = "Đã xóa tiến độ. Cài đặt của bạn được giữ lại."; return true;
        }
        public bool SavePreferences()
        {
            if (!GamePreferences.Valid(Preferences)) { Status = "Cài đặt không hợp lệ."; return false; }
            Preferences.Apply(story.player);
            bool ok = preferences.Save(Preferences, out var message); Status = ok ? "Đã lưu cài đặt." : message; return ok;
        }
        public void ImportSettings(GameSettings settings)
        {
            if (Preferences == null) return;
            settings.volume = Preferences.volume; settings.sensitivity = Preferences.mouseSensitivity;
        }
        public bool SaveSettings(GameSettings settings)
        {
            if (Preferences == null) return false;
            Preferences.volume = settings.volume; Preferences.mouseSensitivity = settings.sensitivity;
            return SavePreferences();
        }
        public bool PrepareQuit()
        {
            if (playing && !Flush()) return false;
            Online?.EndSession("quit_app");
            quitting = true; return true;
        }
        public void OpenSettings()
        { story.player.SetPaused(true); story.player.GetComponent<PauseMenu>()?.ShowSettings(); }
        void CloseMenu()
        { startup = false; story.player.PauseMenuLocked = false; story.player.ResumeControl(); }
        void OnApplicationPause(bool paused) { if (paused) Flush(); }
        void OnApplicationFocus(bool focus) { if (!focus) Flush(); }
        void OnApplicationQuit() { if (!quitting) Flush(); Online?.EndSession("quit_app"); }
        void OnDestroy() { Settle(); if (story != null && Online != null) story.TelemetryProduced -= Online.Record; if (story != null && story.player != null && ReferenceEquals(story.player.Session, this)) { story.player.Session = null; story.player.PauseMenuLocked = false; } }
    }
}
