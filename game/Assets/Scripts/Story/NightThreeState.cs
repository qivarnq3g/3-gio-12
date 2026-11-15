using System;
using System.Collections.Generic;
using BaGio12.Persistence;

namespace BaGio12.Story
{
    public enum NightThreeStage { Alarm, ToGas, GasStation, GasReturn, MorningRoom, Evening, LateNight, DawnWaiting, Completed, Scapegoat }
    [Serializable] public sealed class NightThreePhoto
    {
        public string id;
        public bool caseBack;
    }
    [Serializable] public sealed class NightThreeState
    {
        public NightThreeStage stage;
        public bool riding, returning, refuelled, alarm1300, doorLocked, openedHoaProfile, sawHistory;
        public bool v1Delivered, heardV1, v2Delivered, heardV2, teaArrived, teaTaken, drankTea;
        public bool sentCasePhoto, answeredCall, gotVaultCode, openedVault, keptPhone, policeIntent;
        public bool askedKeepSecret, askedTextMyNumber, toldReadReceipt, foundZolpidem, charged, night3Slept;
        public string contactMode = "none", ending = "";
        public float routeMetres, speed, odometer, clockSeconds;
        public int battery = 30, wrongAttempts;
        public List<NightThreePhoto> photos = new List<NightThreePhoto>();
        public List<string> viewedVault = new List<string>();
        public static bool Valid(NightThreeState s)
        {
            if(s == null || !Enum.IsDefined(typeof(NightThreeStage),s.stage) || s.photos == null || s.viewedVault == null
                || (s.contactMode!="none" && s.contactMode!="own_phone" && s.contactMode!="ngan_phone")
                || s.battery<9 || s.battery>100 || s.wrongAttempts<0 || s.photos.Count>32) return false;
            foreach(float n in new[]{s.routeMetres,s.speed,s.odometer,s.clockSeconds}) if(!GamePreferences.Finite(n) || n<0)return false;
            if(s.routeMetres>FixedRouteRide.Length || s.speed>FixedRouteRide.MaxSpeed || s.clockSeconds>=4
                || (s.riding && s.stage!=NightThreeStage.ToGas && s.stage!=NightThreeStage.GasReturn))return false;
            var ids=new HashSet<string>();
            foreach(var p in s.photos)
                if(p==null || !Guid.TryParseExact(p.id,"N",out _) || !ids.Add(p.id))return false;
            foreach(string id in s.viewedVault) if(Array.IndexOf(NightThreeContent.VaultIds,id)<0)return false;
            return (!s.answeredCall || s.sentCasePhoto) && (!s.gotVaultCode || s.answeredCall)
                && (!s.openedVault || s.gotVaultCode) && (!s.doorLocked || s.openedHoaProfile)
                && (!s.keptPhone || s.openedVault) && (!s.policeIntent || s.openedVault)
                && (s.stage!=NightThreeStage.Scapegoat || s.ending=="scapegoat");
        }
    }
}
