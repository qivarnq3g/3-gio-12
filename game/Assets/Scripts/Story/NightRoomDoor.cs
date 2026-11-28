using UnityEngine;
namespace BaGio12.Story
{
    [RequireComponent(typeof(NightInteraction))]
    public sealed class NightRoomDoor : MonoBehaviour
    {
        public Transform leaf;
        public Vector3 hinge;
        public float openAngle = -90;
        NightOneDirector director;
        Collider doorCollider;
        Quaternion closedRotation;
        Vector3 closedPosition;
        bool open, followupLocked;
        public void SetFollowupLocked(bool value) => followupLocked=value;
        void Awake()
        {
            director=FindFirstObjectByType<NightOneDirector>();doorCollider=GetComponent<Collider>();
            if(leaf!=null){closedRotation=leaf.rotation;closedPosition=leaf.position;}
        }
        void Update()
        {
            if(director==null)return;
            bool passable=director.State.stage>=NightStage.Room && !followupLocked;
            doorCollider.isTrigger=passable;
            if(leaf==null||passable==open)return;
            open=passable;
            leaf.SetPositionAndRotation(closedPosition,closedRotation);
            if(open)leaf.RotateAround(hinge,Vector3.up,openAngle);
        }
    }
}
