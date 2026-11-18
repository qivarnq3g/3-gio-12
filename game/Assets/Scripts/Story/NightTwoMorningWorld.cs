using System.Collections.Generic;
using BaGio12.Gameplay;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
        GameObject world;
        readonly List<Material> materials = new List<Material>();
        readonly List<GameObject> shelf = new List<GameObject>();
        Light deskLamp;
        Light worldSun;
        Color originalSky;
        Transform lienFacing;
        Material Material(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color; materials.Add(material); return material;
        }
        GameObject Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name;
            o.transform.SetParent(world.transform); o.transform.position = position; o.transform.localScale = size;
            o.GetComponent<Renderer>().sharedMaterial = material; return o;
        }
        void Node(string name, Vector3 position, string action, Material material, int index = 0)
        {
            var o = Box(name, position, new Vector3(.65f, .6f, .65f), material);
            var node = o.AddComponent<MorningInteraction>(); node.owner = this; node.action = action; node.index = index;
            var label = new GameObject("Label").AddComponent<TextMesh>(); label.transform.SetParent(o.transform.parent);
            label.transform.position = position + Vector3.up * .7f;
            label.text = name; label.fontSize = 48; label.characterSize = .045f; label.anchor = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<Renderer>().sharedMaterial = label.font.material;
        }
        void EnsureWorld()
        {
            if (world != null) return;
            originalSky = story.player.eyes.backgroundColor;
            world = new GameObject("Night 2 morning — temporary graybox");
            var floor = Material(new Color(.25f,.29f,.3f));
            var wall = Material(new Color(.6f,.63f,.57f));
            var action = Material(new Color(.14f,.56f,.46f));
            var prop = Material(new Color(.55f,.35f,.18f));
            Box("Ground", new Vector3(100,-.2f,45), new Vector3(40,.4f,110), floor);
            Box("West bound", new Vector3(80,1,45), new Vector3(.3f,2,110), wall);
            Box("East bound", new Vector3(120,1,45), new Vector3(.3f,2,110), wall);
            Box("South bound", new Vector3(100,1,-10), new Vector3(40,2,.3f), wall);
            Box("North bound", new Vector3(100,1,100), new Vector3(40,2,.3f), wall);
            Box("Room left", new Vector3(95,1.2f,0), new Vector3(.2f,2.4f,10), wall);
            Box("Room right", new Vector3(105,1.2f,0), new Vector3(.2f,2.4f,10), wall);
            Box("Room back", new Vector3(100,1.2f,-5), new Vector3(10,2.4f,.2f), wall);
            Box("Road", new Vector3(112,.012f,52), new Vector3(4,.02f,80), floor);
            Box("Stop line", new Vector3(112,.04f,47), new Vector3(4,.04f,.2f), wall);
            Box("Store facade", new Vector3(105,1.5f,95), new Vector3(6,3,.3f), wall);
            Node("Bánh mì", new Vector3(96,.7f,7), "breakfast", action);
            Node("Xe máy", Parking + Vector3.up * .6f, "bike", action);
            Node("Xe máy / cửa hàng", Store + Vector3.up * .6f, "store", action);
            Node("Lối vào phòng", new Vector3(100,.7f,7), "gate", action);
            Node("Nệm", new Vector3(102,.35f,-2), "bed", action);
            Node("Đèn kẹp bàn", new Vector3(102,.8f,1), "lamp", action);
            for (int i=0; i<4; i++)
            {
                shelf.Add(Box("Shelf tier " + i, new Vector3(98,.25f+i*.35f,-2), new Vector3(1.5f,.1f,.6f), prop));
            }
            Node("Ráp kệ", new Vector3(98,.5f,1.5f), "shelf", action, -1);
            var npc = Box("Cô Liên giữ chỗ", Lien + Vector3.up * .65f, new Vector3(.5f,1.3f,.5f), prop);
            // NPC body and facing marker must not occlude their own eye ray.
            Destroy(npc.GetComponent<Collider>());
            lienFacing = Box("Hướng nhìn cô Liên", Lien + new Vector3(0,1.5f,-.4f), new Vector3(.12f,.12f,.5f), action).transform;
            Destroy(lienFacing.GetComponent<Collider>());
            deskLamp = new GameObject("Desk light").AddComponent<Light>(); deskLamp.transform.SetParent(world.transform);
            deskLamp.type = LightType.Point; deskLamp.range = 4; deskLamp.intensity = 2; deskLamp.transform.position = Room + new Vector3(2,1.3f,1);
            worldSun = new GameObject("Morning daylight").AddComponent<Light>(); worldSun.transform.SetParent(world.transform);
            worldSun.type = LightType.Directional; worldSun.transform.rotation = Quaternion.Euler(50,-30,0);
            RefreshWorld();
        }
        void RefreshWorld()
        {
            if (world == null || State == null) return;
            for(int i=0;i<shelf.Count;i++) shelf[i].SetActive(i<State.assemblySteps);
            deskLamp.enabled = State.lampTested && State.stage != MorningStage.Evening;
            worldSun.intensity = State.stage == MorningStage.Evening ? .08f : 1.2f;
            story.player.eyes.backgroundColor = State.stage == MorningStage.Evening ? originalSky : new Color(.42f,.62f,.78f);
            lienFacing.localScale = new Vector3(.12f, .12f, State.gateElapsed >= 8 ? .5f : .1f);
        }
        public string PromptFor(string action, int index)
        {
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
                default: ok = Vector3.Distance(story.player.transform.position, Room) < 4 && Rest(); break;
            }
            if (!ok) story.Say(Objective);
            RefreshWorld();
        }
        void OnDestroy()
        {
            if (world != null) Destroy(world);
            foreach (var material in materials) Destroy(material);
        }
    }
}
