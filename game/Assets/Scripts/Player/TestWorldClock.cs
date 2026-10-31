using UnityEngine;
namespace BaGio12.Gameplay
{
    // Visible movement continues while a phone is open.
    public sealed class TestWorldClock : MonoBehaviour
    {
        void Update() { transform.Rotate(0, 45 * Time.deltaTime, 0); }
    }
}
