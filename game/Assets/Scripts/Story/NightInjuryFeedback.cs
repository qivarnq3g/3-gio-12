using System;
using UnityEngine;
namespace BaGio12.Story
{
    public sealed class NightInjuryFeedback : MonoBehaviour
    {
        public NightOneDirector director;
        AudioSource breath;
        AudioClip clip;
        Vector3 previousPosition;
        uint previousPose;
        float strainUntil;
        public bool IsBreathing => breath != null && breath.isPlaying;
        void Awake()
        {
            previousPosition=transform.position;previousPose=director.player.PoseRevision;breath=gameObject.AddComponent<AudioSource>();
            breath.loop=true;breath.volume=.18f;
            var samples=new float[22050];var random=new System.Random(12);
            float filtered=0;
            for(int i=0;i<samples.Length;i++)
            {filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,.13f);float envelope=Mathf.Sin(Mathf.PI*i/samples.Length);samples[i]=filtered*envelope;}
            clip=AudioClip.Create("Synthesized strained breath",samples.Length,1,22050,false);clip.SetData(samples,0);breath.clip=clip;
        }
        void LateUpdate()
        {
            Vector3 motion=transform.position-previousPosition;previousPosition=transform.position;
            // Step-offset climbing is reflected in position even when controller.velocity omits it.
            // Teleport/sitting transitions are not physical exertion.
            if(motion.sqrMagnitude>1 || previousPose!=director.player.PoseRevision)motion=Vector3.zero;
            previousPose=director.player.PoseRevision;
            ProcessMotion(motion,director.player.IsRunning);
        }
        public void ProcessMotion(Vector3 motion,bool running)
        {
            if(director.player.IsPaused)return;
            bool injured=director.State.prologue_beaten>=2 && director.State.stage!=NightStage.Ended;
            bool climbing=motion.y>.001f && new Vector2(motion.x,motion.z).sqrMagnitude>.00001f;
            bool straining=running || climbing || (director.State.stage==NightStage.Prologue && director.Hidden);
            if(injured && !director.Busy && straining)strainUntil=Time.time+.65f;
            bool playing=injured && !director.Busy && Time.time<strainUntil;
            if(playing && !breath.isPlaying)breath.Play();
            if(!playing && breath.isPlaying)breath.Stop();
        }
        void OnDestroy() {if(clip!=null)Destroy(clip);}
    }
}
