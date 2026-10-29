namespace BaGio12.Gameplay
{
    public sealed class TestPickup : WorldInteractable
    {
        public override string Prompt => "Nhặt vật chứng";
        public override void Interact(FirstPersonPlayer player) { player.Collect(); gameObject.SetActive(false); }
    }
}
