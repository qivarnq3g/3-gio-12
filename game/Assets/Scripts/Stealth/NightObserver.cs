using BaGio12.Story;
using BaGio12.Gameplay;
using UnityEngine;
using UnityEngine.AI;

namespace BaGio12.Stealth
{
    public enum ObserverMode { Normal, Suspicious, Chasing, Searching, Inactive }
    public sealed partial class NightObserver : MonoBehaviour
    {
        public NightOneDirector director;
        public string displayName;
        public bool child, flashlight, stationary, startsDormant, irregularLook, once, patrol;
        public float interval = 20, watchDuration = 5, startDelay;
        public float range = 8, fieldOfView = 90;
        public float Attention { get; private set; }
        public ObserverMode Mode { get; private set; }
        public bool ActiveThreat => Mode == ObserverMode.Suspicious || Mode == ObserverMode.Chasing || Mode == ObserverMode.Searching;
        NavMeshAgent agent;
        Renderer[] visuals;
        Light torch;
        Vector3 home, lastSeen;
        Quaternion homeRotation;
        float elapsed, modeTime, lampTime, nextRandom;
        bool lastVisible;
        float originalInterval, originalDuration, originalDelay, watchUntil, focusUntil;
        public bool IsOnSchedule(float sinceDeparture,bool departed,int captures) => !once || captures>0 || (departed && sinceDeparture>=startDelay && sinceDeparture<startDelay+watchDuration);
        // The appearance window starts an encounter; it must not interrupt an active pursuit.
        public bool IsPresent => director != null && (IsOnSchedule(director.PrologueElapsed, director.PrologueDeparted, director.State.prologue_beaten)
            || (ActiveThreat && once && director.PrologueDeparted && director.PrologueElapsed >= startDelay));
        void RefreshPresentation()
        {
            bool present = IsPresent && Mode != ObserverMode.Inactive;
            foreach (var visual in visuals) visual.enabled = present;
            if (torch != null) torch.enabled = present && Mode == ObserverMode.Chasing && director.State.stage == NightStage.Prologue && !director.State.phonePlaced;
        }
        void Start()
        {
            visuals=GetComponentsInChildren<Renderer>();
            agent = GetComponent<NavMeshAgent>(); home = transform.position; homeRotation = transform.rotation;
            originalInterval=interval; originalDuration=watchDuration; originalDelay=startDelay;
            nextRandom=startDelay+UnityEngine.Random.Range(8f,15f);
            if(startsDormant)Mode=ObserverMode.Inactive;
            if(flashlight)
            {
                var lamp=new GameObject("Flashlight beam");lamp.transform.SetParent(transform,false);lamp.transform.localPosition=Vector3.up*1.4f;
                torch=lamp.AddComponent<Light>();torch.type=LightType.Spot;torch.range=5;torch.spotAngle=24;torch.intensity=3;torch.enabled=false;
            }
            if (agent != null) { agent.speed = 3.8f; agent.angularSpeed = 360; agent.stoppingDistance = .65f; }
        }
        void Update()
        {
            if (director == null) { Stop(); return; }
            float scheduleTime=once && director.State.prologue_beaten==0 ? director.PrologueElapsed : elapsed;
            bool scheduledVisible = IsPresent;
            RefreshPresentation();
            if (director == null || director.player.IsPaused || director.State.stage != NightStage.Prologue || director.State.phonePlaced || Mode == ObserverMode.Inactive || director.Busy)
            { Stop(); return; }
            if (!scheduledVisible)
            {
                if (ActiveThreat) ReturnHome();
                Attention = 0; lampTime = 0; Stop(); RefreshPresentation(); return;
            }
            elapsed += Time.deltaTime;
            modeTime += Time.deltaTime;
            if (Mode == ObserverMode.Normal)
            {
                if (!patrol && agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
                scheduleTime=once && director.State.prologue_beaten==0 ? director.PrologueElapsed : elapsed;
                float cycle = Mathf.Repeat(scheduleTime - startDelay, interval);
                bool watching = elapsed >= startDelay && cycle < watchDuration;
                if(once && director.State.prologue_beaten==0)watching=director.PrologueDeparted && scheduleTime>=startDelay && scheduleTime<startDelay+watchDuration;
                if(irregularLook && elapsed>=nextRandom) {watchUntil=elapsed+3;nextRandom=elapsed+UnityEngine.Random.Range(8f,15f);}
                if(irregularLook)watching=elapsed<watchUntil;
                if(patrol)Travel(home+new Vector3(watching?-1.3f:0,0,0));
                if (patrol && agent != null && agent.velocity.sqrMagnitude > .01f)
                    transform.rotation = Quaternion.LookRotation(new Vector3(agent.velocity.x, 0, agent.velocity.z));
                else transform.rotation = Quaternion.Slerp(transform.rotation, homeRotation * Quaternion.Euler(0,watching ? 0 : 180,0),Time.deltaTime*5);
                if(Time.time<focusUntil)transform.rotation=Quaternion.LookRotation(new Vector3(lastSeen.x-transform.position.x,0,lastSeen.z-transform.position.z));
            }
            bool visible = CanSeePlayer();
            // The flashlight requires an uninterrupted hit, not accumulated glimpses.
            bool lit = visible && flashlight && Mode == ObserverMode.Chasing
                && Vector3.Distance(transform.position, director.player.transform.position) < 5
                && Vector3.Angle(transform.forward, director.player.transform.position - transform.position) < 12;
            lampTime = lit ? lampTime + Time.deltaTime : 0;
            if (lampTime >= 1) director.Caught();
            if (visible)
            {
                lastSeen = director.player.transform.position;
                float factor = (director.Dark || director.player.IsCrouched ? .5f : 1) * (director.player.IsRunning ? 2 : 1);
                Attention = Mathf.Clamp01(Attention + Time.deltaTime*factor/1.5f);
                if (Attention >= 1 && (Mode == ObserverMode.Normal || (Mode == ObserverMode.Suspicious && !lastVisible))) Detect(false);
                if (Mode == ObserverMode.Searching) Change(ObserverMode.Chasing);
            }
            else { Attention = Mathf.Max(0, Attention-Time.deltaTime/3); lampTime = 0; }
            if (Mode == ObserverMode.Suspicious)
            {
                if (visible && modeTime > 1.5f) Change(ObserverMode.Chasing);
                Travel(lastSeen);
                if (modeTime > 15) ReturnHome();
            }
            if (Mode == ObserverMode.Chasing)
            {
                if (visible) Travel(lastSeen); else Change(ObserverMode.Searching);
                if (Vector3.Distance(transform.position, director.player.transform.position) < 1.2f && HasUnobstructedPathToPlayer()) director.Caught();
            }
            if (Mode == ObserverMode.Searching)
            {
                Travel(lastSeen);
                if (modeTime > 20) ReturnHome();
                if (agent != null && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 1) transform.Rotate(0,30*Time.deltaTime,0);
            }
            lastVisible = visible;
            RefreshPresentation();
        }
        void Stop() { if (agent != null && agent.isOnNavMesh) agent.isStopped = true; }
        void Travel(Vector3 point)
        {
            if (stationary || agent == null || !agent.isOnNavMesh) return;
            if (NavMesh.SamplePosition(point,out var hit,2,NavMesh.AllAreas)) { agent.isStopped=false; agent.SetDestination(hit.position); }
        }
        bool HasUnobstructedPathToPlayer()
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 delta = director.player.eyes.transform.position - origin;
            return !Physics.Raycast(origin, delta.normalized, delta.magnitude, PlayerGeometry.WorldMask, QueryTriggerInteraction.Ignore);
        }
        public bool CanSeePlayer()
        {
            if (director == null || Mode == ObserverMode.Inactive || director.State.phonePlaced || director.State.stage != NightStage.Prologue) return false;
            if (!IsPresent || (!once && elapsed < startDelay)) return false;
            var origin = transform.position + Vector3.up*1.5f;
            var destination = director.player.eyes.transform.position;
            Vector3 delta = destination-origin;
            if (delta.magnitude > range || Vector3.Angle(transform.forward,delta) > fieldOfView/2) return false;
            return !Physics.Raycast(origin,delta.normalized,delta.magnitude, PlayerGeometry.WorldMask,QueryTriggerInteraction.Ignore);
        }
        public void Detect(bool placement)
        {
            if (director == null || Mode == ObserverMode.Inactive || director.State.stage != NightStage.Prologue || director.State.phonePlaced) return;
            if(stationary) {director.Shout(this);Attention=0;return;}
            if (child)
            { director.Shout(this); Change(ObserverMode.Inactive); return; }
            lastSeen = director.player.transform.position;
            Change(placement || director.InParking || Mode == ObserverMode.Suspicious ? ObserverMode.Chasing : ObserverMode.Suspicious);
        }
        void Change(ObserverMode mode)
        {
            if (Mode == mode) return;
            Mode = mode; modeTime = 0;
            if (mode == ObserverMode.Chasing) director.Shout(this);
        }
        public bool CanHear => director != null && Mode == ObserverMode.Normal && director.State.stage == NightStage.Prologue
            && !director.State.phonePlaced && IsOnSchedule(director.PrologueElapsed, director.PrologueDeparted, director.State.prologue_beaten);
        public void Hear(Vector3 point, bool shout)
        {
            if (!CanHear) return;
            lastSeen = point;
            if (shout) { if (child) Change(ObserverMode.Inactive); else Change(ObserverMode.Suspicious); }
            else {focusUntil=Time.time+2;transform.rotation = Quaternion.LookRotation(new Vector3(point.x-transform.position.x,0,point.z-transform.position.z));}
        }
        public bool CanHearBreathing => director != null && Mode != ObserverMode.Inactive && director.State.stage == NightStage.Prologue
            && !director.State.phonePlaced && IsPresent;
        public void HearBreathing(Vector3 point)
        {
            if (!CanHearBreathing) return;
            // Searching/chasing keeps its state and timeout, but investigates the latest sound position.
            lastSeen = point; focusUntil = Time.time + 2;
            var direction = new Vector3(point.x - transform.position.x, 0, point.z - transform.position.z);
            if (direction.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(direction);
        }
        public void Activate() {Mode=ObserverMode.Normal;elapsed=0;startDelay=0;}
        public void LoseTrail() { if (ActiveThreat) ReturnHome(); }
        void ReturnHome()
        {
            Mode = ObserverMode.Normal; modeTime = 0; Attention = 0;
            if (agent != null && agent.isOnNavMesh) { agent.Warp(home); agent.ResetPath(); }
            transform.rotation = homeRotation;
        }
        public void ResetObserver(bool inactive)
        {
            ReturnHome(); elapsed = 0; lampTime = 0;
            if (inactive) Mode = ObserverMode.Inactive;
            // Later captures keep shorter but readable windows to slip past.
            interval=originalInterval; watchDuration=originalDuration; startDelay=originalDelay;
            if (director != null && director.State.prologue_beaten > 0) { interval=11; watchDuration=6; startDelay=0; }
            if(startsDormant)Mode=ObserverMode.Inactive;
        }
        void OnGUI()
        {
            if (Attention <= .01f || director == null || director.Busy || director.player.ActivePhone != 0) return;
            var p=director.player.eyes.WorldToScreenPoint(transform.position+Vector3.up*2.2f);
            if(p.z<=0)return;
            var rect=new Rect(p.x-45,Screen.height-p.y,90,7);
            GUI.color=Color.black; GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=Mode == ObserverMode.Chasing ? Color.red : Color.yellow;
            GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width*Attention,rect.height),Texture2D.whiteTexture); GUI.color=Color.white;
        }
    }
}
