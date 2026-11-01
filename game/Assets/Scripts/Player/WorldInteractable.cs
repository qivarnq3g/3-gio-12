using UnityEngine;
namespace BaGio12.Gameplay
{
    public abstract class WorldInteractable : MonoBehaviour
    {
        public virtual void HandleInput(FirstPersonPlayer player, bool pressed, bool held) { if (pressed) Interact(player); }
        public abstract string Prompt { get; }
        public abstract void Interact(FirstPersonPlayer player);
    }
}
