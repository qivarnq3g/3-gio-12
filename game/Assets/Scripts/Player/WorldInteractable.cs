using UnityEngine;
namespace BaGio12.Gameplay
{
    public abstract class WorldInteractable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public abstract void Interact(FirstPersonPlayer player);
    }
}
