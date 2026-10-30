using UnityEngine;
namespace BaGio12.Gameplay
{
    public sealed class TestDoor : WorldInteractable
    {
        public bool IsOpen { get; private set; }
        public override string Prompt => IsOpen ? "Đóng cửa" : "Mở cửa";
        public override void Interact(FirstPersonPlayer player)
        {
            var destination = transform.position + (IsOpen ? Vector3.down : Vector3.up) * 2.8f;
            // Sliding test door: overlap check prevents closing onto the player.
            if (IsOpen && Physics.CheckBox(destination, new Vector3(0.85f, 1.2f, 0.18f), Quaternion.identity, 1 << 2))
            { player.Notify("Hãy tránh khỏi cửa trước khi đóng"); return; }
            transform.position = destination;
            IsOpen = !IsOpen;
        }
    }
}
