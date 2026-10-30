using UnityEngine;
namespace BaGio12.Story
{
    public sealed class NightPassingBike : MonoBehaviour
    {
        public NightOneDirector director;
        public Light headlight;
        public Vector3 from, to;
        public float departure = 15, duration = 8;
        Renderer[] visuals;
        public bool VisibleAt(float elapsed) => elapsed >= departure && elapsed < departure + duration;
        void Awake() {visuals=GetComponentsInChildren<Renderer>();}
        void Update()
        {
            bool visible=director.State.stage==NightStage.Prologue && !director.State.phonePlaced && director.PrologueDeparted && VisibleAt(director.PrologueElapsed);
            foreach(var visual in visuals)visual.enabled=visible;
            headlight.enabled=visible;
            if(!visible)return;
            float t=Mathf.Clamp01((director.PrologueElapsed-departure)/duration);
            transform.position=Vector3.Lerp(from,to,t);
            transform.rotation=Quaternion.LookRotation((to-from).normalized);
        }
    }
}
