using BaGio12.Gameplay;
using UnityEngine;
namespace BaGio12.Story
{
    public enum NightAction { PlacePhone, ReturnToStart, ParkBike, Phone, Shop, Camera, Police, Gate, Room, Deposit, Bag, MoveBag, Wifi, Mirror, Notice, Bed, Charge, LienPhone }
    public sealed class NightInteraction : WorldInteractable
    {
        public NightAction action;
        public string caption;
        NightOneDirector director;
        float heldTime, lastHeld;
        public const float PlaceSeconds = 4;
        NightOneDirector Director => director != null ? director : director = FindFirstObjectByType<NightOneDirector>();
        public override string Prompt
        {
            get
            {
                if (action == NightAction.ParkBike && Director.State.stage == NightStage.Parking) return Director.State.bikeParked ? "Xem lại số đồng hồ xe" : "Cúi xuống xem đồng hồ xe";
                if (Director.State.stage != NightStage.Prologue) return caption;
                if (action == NightAction.PlacePhone) return Director.State.phonePlaced ? "" : Director.CanPlace ? "Đặt máy (cúi, giữ 4 giây)" : "Chưa đặt máy được khi có người nghi hay đuổi";
                if (action == NightAction.ReturnToStart && Director.State.phonePlaced) return caption;
                return "";
            }
        }
        public override void HandleInput(FirstPersonPlayer player, bool pressed, bool held)
        {
            if (action != NightAction.PlacePhone || Director.State.stage != NightStage.Prologue) { base.HandleInput(player, pressed, held); return; }
            if (!held) { heldTime = 0; return; }
            if (Time.time - lastHeld > .15f) heldTime = 0;
            lastHeld = Time.time;
            if (!player.IsCrouched) { Director.Say("Cúi cạnh chiếc xe để đặt máy."); heldTime = 0; return; }
            if (Director.ExposedWhilePlacing()) { heldTime = 0; return; }
            if (!Director.CanPlace) { heldTime = 0; return; }
            heldTime += Time.deltaTime;
            Director.HoldProgress = heldTime / PlaceSeconds;
            if (heldTime >= PlaceSeconds) { heldTime = 0; Director.PlacePhone(); }
        }
        public override void Interact(FirstPersonPlayer player) { if (!string.IsNullOrEmpty(Prompt)) Director.Act(action); }
    }
}
