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
        NightOneDirector Director => director != null ? director : director = FindFirstObjectByType<NightOneDirector>();
        public override string Prompt => action == NightAction.PlacePhone && Director.State.stage == NightStage.Prologue
            ? (Director.CanPlace ? "Giữ E 2 giây — Đặt máy (cần cúi)" : "Chưa thể đặt máy khi có người nghi/đuổi") : caption;
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
            Director.HoldProgress = heldTime / 2;
            if (heldTime >= 2) { heldTime = 0; Director.PlacePhone(); }
        }
        public override void Interact(FirstPersonPlayer player) { Director.Act(action); }
    }
}
