using UnityEngine;
namespace BaGio12.Gameplay
{
    public static class PlayerGeometry
    {
        public const float StandingHeight = 1.8f, StandingEye = 1.6f, Radius = .3f;
        public const float CrouchedHeight = 1.1f, CrouchedEye = .9f, SkinWidth = .02f, StepOffset = .25f;
        public static int PlayerLayer => LayerMask.NameToLayer("Player");
        public static int ArmsLayer => LayerMask.NameToLayer("FirstPersonArms");
        public static int PlayerMask => 1 << PlayerLayer;
        public static int ArmsMask => 1 << ArmsLayer;
        public static int WorldMask => Physics.DefaultRaycastLayers & ~PlayerMask & ~ArmsMask;
    }
}
