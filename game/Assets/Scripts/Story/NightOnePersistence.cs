using System;
using System.Linq;
using BaGio12.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BaGio12.Story
{
    public sealed partial class NightOneDirector
    {
        bool restoringSave;
        public NightSaveData CaptureSave()
        {
            if (Busy || !Checkpoints.Restore(out var checkpoint, out var point, out var facing)) return null;
            return new NightSaveData {
                scene = SceneManager.GetActiveScene().name, savedAt = DateTime.UtcNow.ToString("O"), state = State.Copy(),
                checkpointState = checkpoint, checkpointId = Checkpoints.Id, checkpointPosition = point, checkpointYaw = facing,
                position = player.transform.position, yaw = player.transform.eulerAngles.y, pitch = player.ViewPitch,
                crouched = player.IsCrouched, activePhone = player.ActivePhone, objective = Objective, modal = Modal,
                hidingSeconds = hidingSeconds, roomSeconds = roomSeconds, immuneRemaining = Mathf.Max(0, immuneUntil - Time.time),
                prologueElapsed = PrologueElapsed, prologueDeparted = PrologueDeparted, leftStart = leftStart, shoutCount = shoutCount,
                playedSeconds = Mathf.Max(0, Time.time - nightStartedAt), isReplay = isReplay, completedInSession = completedInSession,
                alertLights = alertLights.Select(x => x.enabled).ToArray(), parkingAlert = parkingAlertLight != null && parkingAlertLight.enabled,
                observers = observers.Select(x => x.CaptureObserver()).ToArray()
            };
        }
        public bool ApplySave(NightSaveData save)
        {
            // Validate the whole scene mapping before changing any runtime object.
            if (!NightSaveData.Valid(save) || save.observers.Length != observers.Length || save.alertLights.Length != alertLights.Length
                || !observers.Select(x => x.name).SequenceEqual(save.observers.Select(x => x.id))) return false;
            restoringSave = true;
            try
            {
                StopAllCoroutines(); Busy = false; HoldProgress = 0;
                State = save.state.Copy(); Clock = new StoryClock(State);
                Checkpoints.Save(save.checkpointId, save.checkpointState, save.checkpointPosition, save.checkpointYaw);
                hidingSeconds = save.hidingSeconds; hideAnchor = save.position;
                roomSeconds = save.roomSeconds; immuneUntil = Time.time + save.immuneRemaining;
                PrologueElapsed = save.prologueElapsed; PrologueDeparted = save.prologueDeparted; leftStart = save.leftStart; shoutCount = save.shoutCount;
                nightStartedAt = Time.time - save.playedSeconds; isReplay = save.isReplay; completedInSession = save.completedInSession;
                player.SetOverlay(false); player.SetPhone(0); player.Teleport(save.position, save.yaw); player.RestoreView(save.pitch, save.crouched);
                player.RunMultiplier = State.stage == NightStage.Prologue && State.prologue_beaten >= 2 ? 2f / 3 : 1;
                for (int i = 0; i < observers.Length; i++) observers[i].ApplyObserver(save.observers[i]);
                for (int i = 0; i < alertLights.Length; i++) alertLights[i].enabled = save.alertLights[i];
                if (parkingAlertLight != null) parkingAlertLight.enabled = save.parkingAlert;
                Objective = save.objective ?? ""; Subtitle = ""; subtitleUntil = 0;
                Modal = save.modal ?? ""; NotebookOpen = Modal == "notebook";
                BlackTitle = State.stage == NightStage.Ended ? State.ending == "bystander" ? "NGƯỜI QUA ĐƯỜNG" : "HẾT ĐÊM 1" : "";
                player.SetPhone(save.activePhone); player.SetOverlay(Modal != "" || State.stage == NightStage.Ended);
                bag.transform.position = State.hasBagPosition ? State.bagPosition : bagAtDoor.position;
                SyncWorld(); return true;
            }
            finally { restoringSave = false; }
        }
    }
}
