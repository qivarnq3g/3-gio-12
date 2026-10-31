using UnityEngine;
namespace BaGio12.Story
{
    public sealed class NightBagPresentation : MonoBehaviour
    {
        public NightOneDirector director;
        public Transform flap;
        void LateUpdate() {flap.localRotation=Quaternion.Euler(director.State.bagOpen?-28:0,0,0);}
    }
}
