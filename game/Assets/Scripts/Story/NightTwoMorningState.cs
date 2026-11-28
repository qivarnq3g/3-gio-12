using System;
using System.Collections.Generic;
using BaGio12.Persistence;

namespace BaGio12.Story
{
    public enum MorningStage { Alarm, Breakfast, ToStore, Store, ReturnHome, Gate, Assembly, Lamp, Rest, Evening, ToRice, RiceShop, RiceReturn, Home, Vigil, Ended }

    [Serializable]
    public sealed class OdometerEntry
    {
        public string time;
        public float km;
        public bool engineOn;
    }

    [Serializable]
    public sealed class NightTwoMorningState
    {
        public MorningStage stage;
        public int assemblySteps;
        public bool riding, returning, redLightCleared, lienSawMorning, lampTested;
        public float routeMetres, speed, stoppedSeconds, gateElapsed, restSeconds;
        public float odometer;
        public string roomReply = "unanswered";
        public int momSuspicion;
        public List<OdometerEntry> odoLog = new List<OdometerEntry>();
        public bool hasRice, sawBikeMoved, teaArrived, teaCallPending, teaTaken, drankMilkTea, lienEveningSaid;
        public bool readEveningBatch, sawPhotos, sawStorage, openedCalculator, sawVlog, sawLinhChat, setNganPasscode;
        public bool night2Slept, vigilDozed;
        public int vaultWrongAttempts, strangerDelivered;
        public float vigilSeconds;
        public List<string> eveningSent = new List<string>();

        public static NightTwoMorningState FromNightOne(NightOneState first)
        {
            return new NightTwoMorningState { odometer = first.odo_2100 + (first.night1_slept ? 11 : 0) };
        }

        public static bool Valid(NightTwoMorningState s)
        {
            if (s == null || !Enum.IsDefined(typeof(MorningStage), s.stage) || s.assemblySteps < 0 || s.assemblySteps > 4
                || s.momSuspicion < 0 || s.momSuspicion > 1 || s.odoLog == null || s.eveningSent == null
                || s.vaultWrongAttempts < 0 || s.strangerDelivered < 0 || s.strangerDelivered > NightTwoEvening.StrangerCount
                || (s.roomReply != "unanswered" && s.roomReply != "given" && s.roomReply != "refused")) return false;
            foreach (float n in new[] { s.routeMetres, s.speed, s.stoppedSeconds, s.gateElapsed, s.restSeconds, s.odometer, s.vigilSeconds })
                if (!GamePreferences.Finite(n) || n < 0) return false;
            if (s.speed > FixedRouteRide.MaxSpeed || s.routeMetres > FixedRouteRide.Length || s.stoppedSeconds > FixedRouteRide.RedSeconds
                || s.gateElapsed >= 11 || s.vigilSeconds > NightTwoEvening.VigilLength
                || (s.riding && s.stage != MorningStage.ToStore && s.stage != MorningStage.ReturnHome && s.stage != MorningStage.ToRice && s.stage != MorningStage.RiceReturn)
                || (s.returning && s.stage != MorningStage.ReturnHome && s.stage < MorningStage.Gate)) return false;
            foreach (string id in s.eveningSent)
                if (Array.IndexOf(NightTwoEvening.QuestionIds, id) < 0) return false;
            foreach (var e in s.odoLog)
                if (e == null || !GamePreferences.Finite(e.km) || e.km < 0
                    || !TimeSpan.TryParseExact(e.time, @"hh\:mm", System.Globalization.CultureInfo.InvariantCulture, out _)) return false;
            return true;
        }
    }

    // A constrained route, not a vehicle-physics simulation. Distances on the
    // graybox are compressed; each leg represents 1.5 km in the story.
    public static class FixedRouteRide
    {
        public const float Length = 80, StopLine = 35, MaxSpeed = 8, RedSeconds = 3;
        public static bool AtStop(NightTwoMorningState s) => s.routeMetres >= Length && s.speed <= .05f;
        public static void Step(NightTwoMorningState s, float throttle, bool brake, float dt)
        {
            if (!s.riding || dt <= 0 || !GamePreferences.Finite(dt)) return;
            // Clamp long frames; pausing never advances this simulation.
            dt = Math.Min(dt, .1f);
            s.speed = UnityEngine.Mathf.MoveTowards(s.speed, brake ? 0 : UnityEngine.Mathf.Clamp01(throttle) * MaxSpeed, (brake ? 12 : 3) * dt);
            float next = Math.Min(Length, s.routeMetres + s.speed * dt);
            if (!s.returning && !s.redLightCleared && next >= StopLine)
            {
                next = StopLine; s.speed = 0;
                s.stoppedSeconds = Math.Min(RedSeconds, s.stoppedSeconds + dt);
                if (s.stoppedSeconds >= RedSeconds) s.redLightCleared = true;
            }
            s.routeMetres = next;
            if (next >= Length) s.speed = 0;
        }
    }
}
