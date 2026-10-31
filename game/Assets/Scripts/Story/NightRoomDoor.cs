using UnityEngine;
namespace BaGio12.Story
{
    [RequireComponent(typeof(NightInteraction))]
    public sealed class NightRoomDoor : MonoBehaviour
    {
        NightOneDirector director;
        Collider doorCollider;
        void Awake() {director=FindFirstObjectByType<NightOneDirector>();doorCollider=GetComponent<Collider>();}
        void Update()
        {
            if(director!=null)doorCollider.isTrigger=director.State.stage>=NightStage.Room;
        }
    }
}
