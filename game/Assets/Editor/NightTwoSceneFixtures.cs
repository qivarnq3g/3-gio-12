using System.Linq;
using BaGio12.Story;
using UnityEngine;

// Logic fixtures exist only in Editor Play Mode; never save them into a scene/build.
public static class NightTwoSceneFixtures
{
    public static void AddMorning()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (System.Environment.GetEnvironmentVariable("THREEGIO12_REQUIRE_MORNING_MODELS") == "1")
        {
            foreach (string name in new[] { "Night2_BreadCart", "Night2_Shelf", "Night2_ClampLamp" })
            {
                var model = all.FirstOrDefault(t => t.name == name);
                if (model == null || !model.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled))
                    throw new System.Exception("Real morning model required; fixture refused: " + name);
            }
            Debug.Log("NIGHT_TWO_REAL_MODELS_CONFIRMED_NO_FIXTURES");
            return;
        }
        var root = all.FirstOrDefault(t => t.name == "Night2 props");
        if (root == null) root = new GameObject("Night2 props").transform;
        GameObject Model(string name, Vector3 position)
        {
            if (all.Any(t => t.name == name && t.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled))) return null;
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.SetParent(root); obj.transform.position = position;
            obj.transform.localScale = Vector3.one * .3f;
            Object.Destroy(obj.GetComponent<Collider>());
            return obj;
        }
        Model("Night2_BreadCart", NightTwoMorning.Breakfast + Vector3.up * .5f);
        var shelf = Model("Night2_Shelf", NightTwoMorning.Room + Vector3.right);
        if (shelf != null)
            for (int i = 1; i <= 4; i++)
            {
                var tier = new GameObject("Tier_" + i); tier.transform.SetParent(shelf.transform);
            }
        var lamp = Model("Night2_ClampLamp", NightTwoMorning.Room + new Vector3(1,1,0));
        if (lamp != null)
        {
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere); bulb.name = "Night2_ClampLamp_Bulb";
            bulb.transform.SetParent(lamp.transform,false); Object.Destroy(bulb.GetComponent<Collider>());
            bulb.AddComponent<Light>();
        }
        root.gameObject.SetActive(false);
    }
}
