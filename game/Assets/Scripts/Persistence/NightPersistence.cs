using System;
using System.Collections;
using System.IO;
using BaGio12.Gameplay;
using BaGio12.Story;
using UnityEngine;

namespace BaGio12.Persistence
{
    public sealed class NightPersistence : MonoBehaviour, IGameSession
    {
        public NightOneDirector story;
        public GamePreferences Preferences { get; private set; }
        public string Status { get; private set; } = "";
        public bool Ready { get; private set; }
        public bool HasSave => saves != null && saves.Exists;
        LocalJsonStore<NightSaveData> saves;
        LocalJsonStore<GamePreferences> preferences;
        NightSaveData initial, lastStable;
        bool startup, playing, quitting;
        float nextSave;
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
            initial = story.CaptureSave(); Ready = true; story.player.Session = this;
            if (System.Environment.GetEnvironmentVariable("THREEGIO12_SKIP_START_MENU") == "1") { playing = true; yield break; }
            startup = true; story.player.SetPaused(true); story.player.PauseMenuLocked = true;
        }
        void LateUpdate()
        {
            if (!Ready || !playing) return;
            if (!story.Busy)
            {
                var snapshot = story.CaptureSave();
                if (snapshot != null && NightSaveData.Valid(snapshot)) lastStable = snapshot;
            }
            if (Time.unscaledTime >= nextSave) { Flush(); nextSave = Time.unscaledTime + 2; }
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
            bool ok = saves.Save(lastStable, out var message); Status = ok ? "Đã tự lưu tiến độ." : message; return ok;
        }
        public bool ContinueSaved()
        {
            if (!Ready) return false;
            if (!saves.TryLoad(out var save, out var message)) { Status = message; return false; }
            if (!story.ApplySave(save)) { Status = "Bản lưu không khớp màn chơi hiện tại. Bản lưu vẫn được giữ lại."; return false; }
            lastStable = save; playing = true; Status = message; CloseMenu(); return true;
        }
        public bool StartNewGame()
        {
            if (!Ready || initial == null || !NightSaveData.Valid(initial)) { Status = "Chưa thể bắt đầu lại màn chơi."; return false; }
            if (!saves.Delete(out var message)) { Status = message; return false; }
            if (!story.ApplySave(initial)) { Status = "Chưa thể bắt đầu lại màn chơi."; return false; }
            lastStable = null; playing = true;
            if (!Flush()) { startup = true; story.player.SetPaused(true); story.player.PauseMenuLocked = true; return false; }
            CloseMenu(); story.Say("Đừng để ai thấy.", 6); return true;
        }
        public bool DeleteProgress()
        {
            if (!Ready) return false;
            if (!saves.Delete(out var message)) { Status = message; return false; }
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
            quitting = true; return true;
        }
        public void OpenSettings()
        { story.player.SetPaused(true); story.player.GetComponent<PauseMenu>()?.ShowSettings(); }
        void CloseMenu()
        { startup = false; story.player.PauseMenuLocked = false; story.player.ResumeControl(); }
        void OnApplicationPause(bool paused) { if (paused) Flush(); }
        void OnApplicationFocus(bool focus) { if (!focus) Flush(); }
        void OnApplicationQuit() { if (!quitting) Flush(); }
        void OnDestroy() { if (story != null && story.player != null && ReferenceEquals(story.player.Session, this)) { story.player.Session = null; story.player.PauseMenuLocked = false; } }
    }
}
