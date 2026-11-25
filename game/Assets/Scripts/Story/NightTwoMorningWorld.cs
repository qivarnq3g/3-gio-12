using System.Linq;
using BaGio12.Gameplay;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
        GameObject world;
        Color originalSky;
        Transform rack, clampLamp, breadCart, riceShop, milkTea, eveningBikeSpot;
        Vector3 bikeHome; Quaternion bikeHomeRotation; bool bikeHomeKnown;
        Light deskLamp;
        Transform propsRoot;
        Renderer bulb;
        MaterialPropertyBlock bulbProperties;
        public string MissingSceneAssets { get; private set; } = "";

        static Transform SceneObject(string name) => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == name);
        static Vector3 ScenePoint(string name, Vector3 fallback) => SceneObject(name) is Transform t ? t.position : fallback;
        static Transform VisibleModel(params string[] names)
        {
            foreach (var name in names)
            {
                var target = SceneObject(name);
                if (target != null && target.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled)) return target;
            }
            return null;
        }
        float assetsCheckedAt = -100;
        public bool ValidateSceneAssets()
        {
            if (Time.unscaledTime - assetsCheckedAt < 1) return MissingSceneAssets == "";
            assetsCheckedAt = Time.unscaledTime;
            propsRoot = SceneObject("Night2 props");
            breadCart = VisibleModel("Night2_BreadCart", "BanhMi_Cart");
            rack = VisibleModel("Night2_Shelf", "BH_F3_Room7_Rack");
            clampLamp = VisibleModel("Night2_ClampLamp");
            var missing = new System.Collections.Generic.List<string>();
            if (breadCart == null) missing.Add("xe bánh mì đầu hẻm");
            if (rack == null) missing.Add("kệ nhựa phòng 7");
            if (clampLamp == null) missing.Add("đèn kẹp bàn phòng 7");
            MissingSceneAssets = missing.Count == 0 ? "" : "Đêm 2 đang chờ model: " + string.Join(", ",missing) + ". Xem #22.";
            return missing.Count == 0;
        }
        public string MissingEveningAssets { get; private set; } = "";
        float eveningCheckedAt = -100;
        public bool ValidateEveningAssets()
        {
            if (Time.unscaledTime - eveningCheckedAt < 1) return MissingEveningAssets == "";
            eveningCheckedAt = Time.unscaledTime;
            riceShop = VisibleModel("Night2_RiceShop");
            milkTea = SceneObject("Night2_MilkTea");
            eveningBikeSpot = SceneObject("PARK_N2_EveningBike");
            var missing = new System.Collections.Generic.List<string>();
            if (riceShop == null) missing.Add("quán cơm bình dân trên đường lớn");
            if (milkTea == null || !milkTea.GetComponentsInChildren<Renderer>(true).Any()) missing.Add("ly trà sữa có giấy dán");
            if (eveningBikeSpot == null) missing.Add("chỗ đậu xe buổi tối trong nhà xe");
            MissingEveningAssets = missing.Count == 0 ? "" : "Buổi tối Đêm 2 đang chờ: " + string.Join(", ",missing) + ". Xem #22.";
            return missing.Count == 0;
        }
        static Bounds ModelBounds(Transform target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            var bounds = new Bounds(target.position, Vector3.zero);
            if (renderers.Length == 0) return bounds;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        // An invisible interaction volume on an existing model, never replacement geometry.
        void Bind(string name, Transform target, string action, int index = 0)
        {
            if (target == null) return;
            var bounds = ModelBounds(target);
            var node = new GameObject(name);
            node.transform.SetParent(world.transform);
            node.transform.position = bounds.center;
            var collider = node.AddComponent<BoxCollider>();
            collider.size = Vector3.Max(bounds.size + Vector3.one * .08f, Vector3.one * .18f);
            var interaction = node.AddComponent<MorningInteraction>();
            interaction.owner = this; interaction.action = action; interaction.index = index;
        }
        void EnsureWorld()
        {
            if (world != null) return;
            originalSky = story.player.eyes.backgroundColor;
            world = new GameObject("Night 2 — realistic interactions");
            ValidateSceneAssets();
            Bind("Ăn bánh mì", breadCart, "breakfast");
            Bind("Xe máy của Sinh viên", story.ownBike == null ? null : story.ownBike.transform, "bike");
            Bind("Mua kệ và đèn", SceneObject("BUY_N2_ShelfAndLamp"), "store");
            Bind("Lối lên phòng 7", SceneObject("Lien_Gate_PostL"), "gate");
            Bind("Nằm nghỉ", story.bed, "bed");
            Bind("Ráp kệ", rack, "shelf", -1);
            Bind("Đèn kẹp bàn", clampLamp, "lamp");
            if (clampLamp != null)
            {
                deskLamp = clampLamp.GetComponentInChildren<Light>(true);
                bulb = clampLamp.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == "Night2_ClampLamp_Bulb");
            }
            ValidateEveningAssets();
            Bind("Mua cơm", riceShop, "rice");
            Bind("Cô Liên", SceneObject("PersonLien"), "lien");
            Bind("Ly trà sữa", milkTea, "tea");
            if (story.ownBike != null) { bikeHome = story.ownBike.transform.position; bikeHomeRotation = story.ownBike.transform.rotation; bikeHomeKnown = true; }
            RefreshWorld();
        }
        // The setup root starts inactive: discover assets without making them visible in Night 1.
        void ResetScenePresentation()
        {
            if (propsRoot == null) propsRoot = SceneObject("Night2 props");
            if (propsRoot != null) propsRoot.gameObject.SetActive(false);
            else
                foreach (string name in new[] { "Night2_BreadCart", "Night2_Shelf", "Night2_ClampLamp" })
                { var target = SceneObject(name); if (target != null) target.gameObject.SetActive(false); }
            if (deskLamp != null) deskLamp.enabled = false;
        }
        void RefreshWorld()
        {
            if (world == null || State == null) return;
            if (propsRoot != null) propsRoot.gameObject.SetActive(true);
            if (breadCart != null) breadCart.gameObject.SetActive(true);
            if (rack != null)
            {
                rack.gameObject.SetActive(State.assemblySteps > 0);
                for (int i = 1; i <= 4; i++)
                { var tier = rack.Find("Tier_" + i); if (tier != null) tier.gameObject.SetActive(i <= State.assemblySteps); }
            }
            if (clampLamp != null) clampLamp.gameObject.SetActive(State.assemblySteps == 4);
            bool lit = State.lampTested && State.stage < MorningStage.Evening;
            if (deskLamp != null) deskLamp.enabled = lit;
            if (bulb != null)
            {
                if (bulbProperties == null) bulbProperties = new MaterialPropertyBlock();
                bulb.GetPropertyBlock(bulbProperties);
                bulbProperties.SetColor("_EmissionColor", lit ? new Color(1f,.82f,.55f) * 2.5f : Color.black);
                bulb.SetPropertyBlock(bulbProperties);
            }
            bool evening = State.stage >= MorningStage.Evening;
            if (milkTea != null) milkTea.gameObject.SetActive(evening && State.teaArrived && !State.teaTaken && !State.teaCallPending);
            if (bikeHomeKnown && story.ownBike != null && !State.riding)
            {
                bool moved = evening && eveningBikeSpot != null;
                story.ownBike.transform.SetPositionAndRotation(moved ? eveningBikeSpot.position : bikeHome, moved ? eveningBikeSpot.rotation : bikeHomeRotation);
            }
        }
        public string PromptFor(string action, int index)
        {
            if (story.InNightThree) return ThreePrompt(action);
            if (State.stage >= MorningStage.Evening)
                switch(action)
                {
                    case "bike": return "Bật máy xe, đi mua cơm";
                    case "store": return "Bật máy, về nhà trọ";
                    case "rice": return "Mua cơm";
                    case "lien": return "Hỏi cô Liên có ai gửi gì không";
                    case "tea": return "Lấy ly trà sữa";
                    case "bed": return "Đi ngủ hay thức canh";
                }
            switch(action)
            {
                case "breakfast": return "Ăn bánh mì";
                case "bike": return "Bật máy xe — đi mua kệ và đèn";
                case "store": return State.stage == MorningStage.Store ? "Mua kệ và đèn kẹp bàn" : "Bật máy — chở đồ về";
                case "gate": return "Lách qua cô Liên vào phòng";
                case "shelf": return "Ráp tầng kệ " + (index<0 ? Mathf.Min(4,State.assemblySteps+1) : index+1);
                case "lamp": return "Kẹp đèn và bật thử";
                default: return "Nằm nghỉ một chút";
            }
        }
        public void Interact(string action, int index)
        {
            if (story.InNightThree) { InteractThree(action); return; }
            if (Transitioning || story.player.IsPaused || State.riding) return;
            bool ok;
            switch(action)
            {
                case "breakfast": ok = EatBreakfast(); break;
                case "bike": ok = Mount(); break;
                case "store": ok = State.stage == MorningStage.Store ? BuyFurniture() : Mount(); break;
                case "gate": ok = PassGate(); break;
                case "shelf": ok = Assemble(index<0 ? State.assemblySteps : index); break;
                case "lamp": ok = TestLamp(); break;
                case "rice": ok = BuyRice(); break;
                case "lien": ok = AskLien(); break;
                case "tea": ok = TakeTea(); break;
                default: ok = State.stage >= MorningStage.Evening ? OpenBed() : Vector3.Distance(story.player.transform.position, Room) < 4 && Rest(); break;
            }
            if (!ok && !(action == "bed" && State.stage >= MorningStage.Evening && !State.hasRice)) story.Say(Objective);
            RefreshWorld();
        }
        void OnDestroy()
        {
            ReleaseThreeCamera();
            if (world != null) Destroy(world);
        }
    }
}
