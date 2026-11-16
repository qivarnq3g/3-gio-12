using System;
using System.Collections;
using System.IO;
using BaGio12.Story;
using UnityEngine;

namespace BaGio12.Persistence
{
    public sealed class NightPersistence : MonoBehaviour
    {
        public NightOneDirector story;
        public GamePreferences Preferences { get; private set; }
        public string Status { get; private set; } = "";
        public bool Ready { get; private set; }
        public bool HasSave => saves != null && saves.Exists;
        LocalJsonStore<NightSaveData> saves;
        LocalJsonStore<GamePreferences> preferences;
        NightSaveData initial, lastStable;
        bool startup, playing, settings, confirmNew, confirmDelete, quitting;
        float nextSave;
        GUIStyle label, title;
        public static string DataRoot => System.Environment.GetEnvironmentVariable("THREEGIO12_DATA_ROOT") ?? Path.Combine(Application.persistentDataPath, "local-v1");
        IEnumerator Start()
        {
            // All observer Start methods must establish their home and NavMeshAgent first.
            yield return null;
            saves = new LocalJsonStore<NightSaveData>(Path.Combine(DataRoot, "night-one.json"), NightSaveData.Valid);
            preferences = new LocalJsonStore<GamePreferences>(Path.Combine(DataRoot, "preferences.json"), GamePreferences.Valid);
            if (!preferences.TryLoad(out var loaded, out var warning)) loaded = new GamePreferences();
            Preferences = loaded; Preferences.Apply(story.player); Status = warning;
            initial = story.CaptureSave(); Ready = true; story.player.HasPauseMenu = true;
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
            CloseMenu(); return true;
        }
        public bool DeleteProgress()
        {
            if (!Ready) return false;
            if (!saves.Delete(out var message)) { Status = message; return false; }
            // A paused current session must not silently re-create the deleted slot.
            playing = false; lastStable = null; startup = true; confirmDelete = false;
            story.player.SetPaused(true); story.player.PauseMenuLocked = true;
            Status = "Đã xóa tiến độ. Cài đặt của bạn được giữ lại."; return true;
        }
        public bool SavePreferences()
        {
            if (!GamePreferences.Valid(Preferences)) { Status = "Cài đặt không hợp lệ."; return false; }
            Preferences.Apply(story.player);
            bool ok = preferences.Save(Preferences, out var message); Status = ok ? "Đã lưu cài đặt." : message; return ok;
        }
        public void RequestNewGame()
        { confirmNew = true; story.player.SetPaused(true); story.player.PauseMenuLocked = true; }
        public void OpenSettings()
        { settings = true; story.player.SetPaused(true); story.player.PauseMenuLocked = true; }
        void CloseMenu()
        { startup = settings = confirmNew = confirmDelete = false; story.player.PauseMenuLocked = false; story.player.ResumeControl(); }
        void OnApplicationPause(bool paused) { if (paused) Flush(); }
        void OnApplicationFocus(bool focus) { if (!focus) Flush(); }
        void OnApplicationQuit() { if (!quitting) Flush(); }
        void OnDestroy() { if (story != null && story.player != null) { story.player.HasPauseMenu = false; story.player.PauseMenuLocked = false; } }
        void OnGUI()
        {
            if (!Ready || (!startup && !story.player.IsPaused)) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
                title = new GUIStyle(label) { fontSize = 30 };
            }
            var matrix = GUI.matrix;
            int previousButtonSize = GUI.skin.button.fontSize, previousToggleSize = GUI.skin.toggle.fontSize;
            GUI.skin.button.fontSize = 20; GUI.skin.toggle.fontSize = 20;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            var color = GUI.color; GUI.color = new Color(.025f, .035f, .05f, 1);
            GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture); GUI.color = color;
            GUI.Label(new Rect(240, 35, 830, 55), settings ? "CÀI ĐẶT" : "3 GIỜ 12", title);
            if (confirmNew || confirmDelete)
            {
                GUI.Label(new Rect(240, 130, 800, 140), confirmDelete ? "Xóa toàn bộ tiến độ Đêm 1 và bản dự phòng? Cài đặt được giữ lại." : "Chơi mới Đêm 1 sẽ thay thế tiến độ hiện tại. Bạn có chắc muốn tiếp tục?", label);
                if (GUI.Button(new Rect(240, 300, 360, 55), "Xác nhận")) { if (confirmDelete) DeleteProgress(); else StartNewGame(); }
                if (GUI.Button(new Rect(630, 300, 360, 55), "Hủy")) { confirmNew = confirmDelete = false; story.player.PauseMenuLocked = startup; }
            }
            else if (settings) DrawSettings();
            else
            {
                if (startup && Preferences.showContentWarning)
                    GUI.Label(new Rect(240, 100, 800, 90), "Cảnh báo nội dung: bạo lực, truy đuổi, âm thanh bất ngờ và tình huống gây căng thẳng.", label);
                int y = 205;
                if (!startup && playing) { if (GUI.Button(new Rect(240, y, 750, 45), "Tiếp tục đang chơi")) CloseMenu(); y += 55; }
                GUI.enabled = HasSave;
                if (GUI.Button(new Rect(240, y, 750, 45), "Tiếp tục bản đã lưu")) ContinueSaved(); y += 55;
                GUI.enabled = true;
                if (GUI.Button(new Rect(240, y, 750, 45), "Chơi mới Đêm 1")) { if (HasSave || playing) RequestNewGame(); else StartNewGame(); } y += 55;
                if (GUI.Button(new Rect(240, y, 750, 45), "Cài đặt")) OpenSettings(); y += 55;
                GUI.enabled = HasSave;
                if (GUI.Button(new Rect(240, y, 750, 45), "Xóa tiến độ")) { confirmDelete = true; story.player.PauseMenuLocked = true; } y += 55;
                GUI.enabled = true;
                if (GUI.Button(new Rect(240, y, 750, 45), "Lưu và thoát"))
                {
                    // Do not close the app after a failed save; keep the player's session available.
                    if (!playing || Flush()) { quitting = true; Application.Quit(); }
                }
            }
            GUI.enabled = true; GUI.Label(new Rect(240, 635, 800, 70), Status, label); GUI.matrix = matrix; GUI.skin.button.fontSize = previousButtonSize; GUI.skin.toggle.fontSize = previousToggleSize;
        }
        void DrawSettings()
        {
            GUI.Label(new Rect(240, 120, 360, 35), "Âm lượng: " + Mathf.RoundToInt(Preferences.volume * 100) + "%", label);
            Preferences.volume = GUI.HorizontalSlider(new Rect(630, 135, 360, 25), Preferences.volume, 0, 1);
            GUI.Label(new Rect(240, 180, 360, 35), "Độ nhạy chuột: " + Preferences.mouseSensitivity.ToString("F2"), label);
            Preferences.mouseSensitivity = GUI.HorizontalSlider(new Rect(630, 195, 360, 25), Preferences.mouseSensitivity, .02f, .3f);
            int quality = Preferences.quality < 0 ? QualitySettings.GetQualityLevel() : Preferences.quality;
            GUI.Label(new Rect(240, 240, 360, 35), "Chất lượng đồ họa", label);
            if (GUI.Button(new Rect(630, 240, 360, 40), QualitySettings.names[quality])) Preferences.quality = (quality + 1) % QualitySettings.names.Length;
            Preferences.showContentWarning = GUI.Toggle(new Rect(240, 320, 750, 40), Preferences.showContentWarning, "Hiện cảnh báo nội dung khi mở game");
            Preferences.Apply(story.player);
            if (GUI.Button(new Rect(240, 420, 750, 55), "Lưu cài đặt và quay lại") && SavePreferences()) { settings = false; story.player.PauseMenuLocked = startup; }
        }
    }
}
