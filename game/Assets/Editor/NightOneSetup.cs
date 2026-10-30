using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Story;
using BaGio12.Stealth;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class NightOneSetup
{
    const string Root="Assets/Scenes/Night01";
    static Material concrete, ground, accent, yellow, dark, skin;
    static readonly List<NightObserver> watchers=new List<NightObserver>();
    static Material Material(string name,Color color)
    {
        string path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;return m;
    }
    static GameObject Cube(string name,Vector3 pos,Vector3 size,Material mat)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;
        o.transform.position=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;return o;
    }
    static Transform Marker(string name,Vector3 pos)
    {var o=new GameObject(name);o.transform.position=pos;return o.transform;}
    static void Sign(string text,Vector3 pos,float yaw=0,float size=.1f)
    {
        var o=new GameObject("Sign "+text);o.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));
        var label=o.AddComponent<TextMesh>();label.text=text;label.fontSize=64;label.characterSize=size;
        label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;
        label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");o.GetComponent<Renderer>().sharedMaterial=label.font.material;
        label.color=new Color(.9f,.92f,.82f);
    }
    static GameObject Interact(string name,Vector3 pos,Vector3 size,NightAction action,string caption,Material mat=null)
    {
        var o=Cube(name,pos,size,mat==null?accent:mat);var interaction=o.AddComponent<NightInteraction>();
        interaction.action=action;interaction.caption=caption;return o;
    }
    static Light Lamp(string name,Vector3 pos,Color color,float intensity=2)
    {var l=new GameObject(name).AddComponent<Light>();l.type=LightType.Point;l.range=7;l.intensity=intensity;l.color=color;l.transform.position=pos;return l;}
    static NightObserver Watch(string name,Vector3 pos,float yaw,float interval,float duration,float delay,NightOneDirector story,bool child=false)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Capsule);o.name=name;o.layer=2;
        o.transform.position=pos+Vector3.up*.9f;
        o.GetComponent<Renderer>().sharedMaterial=skin;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        var agent=o.AddComponent<NavMeshAgent>();agent.radius=.25f;agent.height=1.8f;agent.baseOffset=0;
        o.transform.position=pos;o.transform.rotation=Quaternion.Euler(0,yaw,0);
        // Visual body is offset from the agent's feet.
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Renderer>());UnityEngine.Object.DestroyImmediate(o.GetComponent<MeshFilter>());
        var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="Body placeholder";body.layer=2;
        UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());body.transform.SetParent(o.transform,false);body.transform.localPosition=Vector3.up*.9f;body.GetComponent<Renderer>().sharedMaterial=skin;
        var head=Cube("Facing marker",Vector3.zero,new Vector3(.12f,.12f,.3f),yellow);UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());head.layer=2;head.transform.SetParent(o.transform,false);head.transform.localPosition=new Vector3(0,1.5f,.4f);
        var w=o.AddComponent<NightObserver>();w.director=story;w.displayName=name;w.interval=interval;w.watchDuration=duration;w.startDelay=delay;w.child=child;
        watchers.Add(w);return w;
    }
    [MenuItem("3 Gio 12/Night 1/Create Graybox (overwrites scene)")]
    public static void Create()
    {
        foreach(string folder in new[]{Root+"/Materials","Assets/Prefabs/Player"})Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();watchers.Clear();
        EditorSceneManager.OpenScene("Assets/Scenes/PlayerTest.unity");
        var source=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
        PrefabUtility.SaveAsPrefabAsset(source.gameObject,"Assets/Prefabs/Player/FirstPersonPlayer.prefab");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        concrete=Material("Concrete",new Color(.28f,.32f,.35f));ground=Material("Ground",new Color(.12f,.16f,.18f));
        accent=Material("Interact",new Color(.2f,.55f,.5f));yellow=Material("Evidence",new Color(.85f,.58f,.18f));dark=Material("Dark",new Color(.055f,.065f,.08f));skin=Material("Observers",new Color(.55f,.4f,.35f));
        var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/FirstPersonPlayer.prefab"));
        player.name="Player";player.transform.position=new Vector3(0,.08f,1.6f);
        var controller=player.GetComponent<FirstPersonPlayer>();controller.showTestHud=false;
        controller.eyes.clearFlags=CameraClearFlags.SolidColor; controller.eyes.backgroundColor=new Color(.025f,.04f,.065f);
        var story=new GameObject("NightOneDirector").AddComponent<NightOneDirector>();story.player=controller;
        story.gameObject.AddComponent<NightOneHud>().story=story;
        story.alleyStart=Marker("Checkpoint alley start",new Vector3(0,.08f,1.6f));
        story.parkingSpawn=Marker("Checkpoint parking",new Vector3(0,.08f,27.5f));
        // Continuous alley; recesses sit behind parked objects and door jambs.
        Cube("Alley floor",new Vector3(0,-.2f,18),new Vector3(7,.4f,40),ground);
        Cube("Left frontage",new Vector3(-3.5f,2,18),new Vector3(.3f,4,40),concrete);
        Cube("Right frontage",new Vector3(3.5f,2,9),new Vector3(.3f,4,18),concrete);
        Cube("Dead end",new Vector3(0,2,37),new Vector3(7,4,.3f),concrete);
        Cube("Street boundary",new Vector3(0,2,-2),new Vector3(7,4,.3f),concrete);
        for(int i=0;i<5;i++)
        {
            float z=6+i*5;
            Cube("Door recess left "+i,new Vector3(-2.5f,1.1f,z),new Vector3(1.5f,2.2f,.25f),dark);
            Cube("Occluder left "+i,new Vector3(-2.15f,.8f,z+1.6f),new Vector3(1.1f,1.6f,.8f),concrete);
        }
        Cube("Right pavement",new Vector3(2.5f,.12f,8),new Vector3(2,.24f,18),concrete);
        Interact("Start street",new Vector3(0,1,0),new Vector3(2,2,.2f),NightAction.ReturnToStart,"Ra đầu hẻm",dark);
        Interact("Police sign",new Vector3(-2,1.3f,.4f),new Vector3(1.2f,1,.15f),NightAction.Police,"Xem đường tới công an phường");
        Sign("CONG AN PHUONG",new Vector3(-2,2.2f,.3f));
        Interact("Hung shop",new Vector3(-2.65f,1,5),new Vector3(1,.8f,2),NightAction.Shop,"Nói chuyện với chú Hùng");
        Sign("TIEM CHU HUNG",new Vector3(-2.7f,2.5f,5),270,.04f);
        Interact("Security camera",new Vector3(-2.1f,2.5f,5),new Vector3(.3f,.3f,.5f),NightAction.Camera,"Xem kỹ camera",yellow);
        Lamp("Camera red LED",new Vector3(-2.1f,2.5f,4.7f),Color.red,.1f);
        var hung=Watch("Chú Hùng",new Vector3(-2.7f,.03f,7),90,20,5,5,story);hung.flashlight=true;
        Watch("Người phụ nữ",new Vector3(2.6f,.26f,12),270,13,3,3,story).irregularLook=true;
        Watch("Đứa nhỏ 1",new Vector3(2.6f,.26f,16),270,12,4,6,story,true).patrol=true;
        Watch("Đứa nhỏ 2",new Vector3(2.6f,.26f,17.5f),270,12,4,8,story,true).patrol=true;
        Watch("Cô Liên",new Vector3(2.7f,.03f,21),270,55,15,40,story).once=true;
        var lights=new List<Light>();
        for(int i=0;i<4;i++)
        {var l=Lamp("Alert porch "+i,new Vector3(i%2==0?-2:2,2.5f,7+i*7),new Color(1,.68f,.36f),3);l.enabled=false;lights.Add(l);}
        for(int i=0;i<4;i++)
        {var neighbor=Watch("Hàng xóm "+i,new Vector3(-3,.03f,7+i*7),90,1,1,0,story);neighbor.stationary=true;neighbor.startsDormant=true;}
        story.alertLights=lights.ToArray();story.observers=watchers.ToArray();
        var hiding=new List<Transform>();
        foreach(var point in new[]{new Vector3(-2.9f,.06f,9),new Vector3(-2.9f,.06f,19),new Vector3(-2.9f,.06f,29),new Vector3(2.8f,.06f,26)})
        {hiding.Add(Marker("Hiding spot",point));Sign("NAP",point+Vector3.up*.5f,270,.035f);}
        story.hidingPoints=hiding.ToArray();
        // Parking: separate own bike and the target bike.
        Interact("Own bike",new Vector3(-1.7f,.7f,29),new Vector3(.65f,1.1f,1.8f),NightAction.ParkBike,"Dựng xe / xem đồng hồ",yellow);
        Cube("Wave placeholder",new Vector3(1.8f,.6f,32),new Vector3(.65f,1.1f,1.8f),concrete);
        Interact("Place phone beside bike",new Vector3(.85f,.35f,32),new Vector3(.45f,.5f,.6f),NightAction.PlacePhone,"Đặt / lấy chiếc máy",dark);
        story.nganPhone=Interact("Ngan phone",new Vector3(.85f,.18f,32),new Vector3(.15f,.12f,.3f),NightAction.Phone,"Nhặt chiếc máy",accent);
        Interact("Parking notice",new Vector3(-2.8f,1.4f,31),new Vector3(.15f,.65f,.8f),NightAction.Notice,"Đọc tờ giấy",yellow);
        Cube("Parking roof",new Vector3(0,3.8f,32),new Vector3(7,.15f,9),dark);
        Sign("NHA XE",new Vector3(0,3.1f,27));
        // Gate and stairs: three flights, real walkable steps, no teleport interaction.
        Cube("Boarding floor",new Vector3(9,-.15f,20),new Vector3(11,.3f,16),ground);
        Interact("Lien gate plant",new Vector3(3.8f,.45f,19),new Vector3(.6f,.9f,.6f),NightAction.Gate,"Phụ cô Liên dựng chậu");
        Interact("Ask Lien keep phone",new Vector3(3.8f,1.2f,20.5f),new Vector3(.4f,.5f,.5f),NightAction.LienPhone,"Nhờ cô Liên giữ máy");
        Sign("NHA TRO / PHONG 7 TANG 3",new Vector3(4,2.8f,18),90,.045f);
        for(int flight=0;flight<3;flight++)
        {
            float x=6+flight*2.2f;
            for(int step=0;step<18;step++)
            {
                float height=flight*2.8f+(step+1)*(2.8f/18);
                float z=flight%2==0 ? 14+step*.36f : 20.12f-step*.36f;
                Cube("Stair "+flight+"-"+step,new Vector3(x,height/2,z),new Vector3(1.6f,height,.36f),concrete);
            }
            float landingZ=flight%2==0?21f:13f;
            Cube("Landing "+flight,new Vector3(x+1.1f,(flight+1)*2.8f-.15f,landingZ),new Vector3(3.8f,.3f,2.2f),concrete);
            Cube("Stair outer rail "+flight,new Vector3(x-.9f,flight*2.8f+2.2f,17),new Vector3(.15f,1,7),dark);
        }
        Cube("Third floor corridor",new Vector3(13.3f,8.25f,21),new Vector3(5,.3f,3),ground);
        Cube("Room floor",new Vector3(17,8.25f,21),new Vector3(6,.3f,7),ground);
        Cube("Room far wall",new Vector3(20,9.8f,21),new Vector3(.2f,2.8f,7),concrete);
        Cube("Room north wall",new Vector3(17,9.8f,24.5f),new Vector3(6,2.8f,.2f),concrete);
        Cube("Room south wall",new Vector3(17,9.8f,17.5f),new Vector3(6,2.8f,.2f),concrete);
        Cube("Room doorway left",new Vector3(14,9.8f,18.5f),new Vector3(.2f,2.8f,2),concrete);
        Cube("Room doorway right",new Vector3(14,9.8f,23.5f),new Vector3(.2f,2.8f,2),concrete);
        Interact("Room7 entry",new Vector3(13.7f,9.4f,21),new Vector3(.18f,1.8f,1.7f),NightAction.Room,"Vào phòng 7");
        // Door collider opens only after interaction, and remains usable to leave.
        var entry=GameObject.Find("Room7 entry");entry.AddComponent<NightRoomDoor>();
        story.roomSpawn=Marker("Checkpoint room7",new Vector3(15,8.45f,21));
        story.bagAtDoor=Marker("Bag beside door",new Vector3(14.7f,8.65f,22.1f));
        story.bed=Interact("Mattress",new Vector3(18.1f,8.6f,22.6f),new Vector3(1.5f,.4f,2),NightAction.Bed,"Ngồi xuống nệm / nghỉ",dark).transform;
        story.bag=Interact("Backpack",story.bagAtDoor.position,new Vector3(.4f,.5f,.35f),NightAction.Bag,"Xem kỹ ba lô",dark);
        Interact("Move bag",new Vector3(14.8f,8.7f,22.7f),new Vector3(.2f,.2f,.2f),NightAction.MoveBag,"Dời ba lô",accent);
        Interact("Deposit slip",new Vector3(17.8f,9.05f,18.4f),new Vector3(.4f,.08f,.3f),NightAction.Deposit,"Xem giấy nhận cọc",yellow);
        Interact("Phone box and charger",new Vector3(18.5f,9.05f,18.4f),new Vector3(.4f,.2f,.3f),NightAction.Charge,"Cắm sạc máy");
        Cube("Table",new Vector3(18,8.7f,18.4f),new Vector3(2,.5f,.7f),concrete);
        Interact("Wifi",new Vector3(14.2f,9.8f,23),new Vector3(.12f,.5f,.6f),NightAction.Wifi,"Xem bảng Wi-Fi",yellow);
        Interact("Foggy mirror",new Vector3(12,9.9f,22.4f),new Vector3(.7f,1,.1f),NightAction.Mirror,"Xem gương mờ",dark);
        Lamp("Room lamp",new Vector3(17,10.8f,21),new Color(1,.8f,.55f),3);
        Lamp("Corridor lamp",new Vector3(12,10.8f,21),new Color(.7f,.85f,1),2);
        Sign("PHONG 7",new Vector3(13.5f,10.8f,21),90,.055f);
        // Upper-floor safety colliders keep the graybox traversable.
        Cube("Corridor back rail",new Vector3(11,9,22.6f),new Vector3(.2f,1.2f,2),dark);
        var sun=new GameObject("Moon").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.7f;sun.color=new Color(.55f,.65f,.85f);sun.transform.rotation=Quaternion.Euler(45,30,0);
        RenderSettings.ambientLight=new Color(.25f,.3f,.38f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.06f,.09f,.12f);RenderSettings.fogDensity=.012f;
        var nav=new GameObject("Night1 navigation").AddComponent<NavMeshSurface>();nav.useGeometry=NavMeshCollectGeometry.PhysicsColliders;nav.layerMask=~(1<<2);nav.BuildNavMesh();
        if(nav.navMeshData!=null) { AssetDatabase.DeleteAsset(Root+"/NightOneNavMesh.asset"); AssetDatabase.CreateAsset(nav.navMeshData,Root+"/NightOneNavMesh.asset"); }
        EditorSceneManager.SaveScene(scene,Root+"/NightOneGraybox.unity");AssetDatabase.SaveAssets();
        Debug.Log("NIGHT_ONE_SCENE_CREATED");
    }
    public static void InspectPaths()
    {
        EditorSceneManager.OpenScene(Root+"/NightOneGraybox.unity");
        foreach(var target in new[]{new Vector3(6,0,14),new Vector3(6,2.8f,20.8f),new Vector3(8.2f,5.6f,13.3f),new Vector3(10.4f,8.4f,20.7f)})
        {
            var path=new NavMeshPath();bool sampled=NavMesh.SamplePosition(target,out var hit,1,NavMesh.AllAreas);
            NavMesh.CalculatePath(new Vector3(0,0,27.5f),sampled?hit.position:target,NavMesh.AllAreas,path);
            Debug.Log($"NAV_DIAG target={target} sample={sampled} at={hit.position} status={path.status} corners={string.Join(";",path.corners.Select(x=>x.ToString()))}");
        }
    }
    public static void OpenForReview(){EditorSceneManager.OpenScene(Root+"/NightOneGraybox.unity");}
}
