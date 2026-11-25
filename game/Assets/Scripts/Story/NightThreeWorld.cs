using System;
using Object = UnityEngine.Object;
using System.IO;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Persistence;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
        public static Vector3 ThreePolice => ScenePoint("ENTRANCE_Police", Vector3.zero);
        public static Vector3 ThreeBag => Object.FindFirstObjectByType<NightOneDirector>()?.bag?.transform.position ?? Room;
        public static Vector3 ThreeGas => ScenePoint("PARK_N3_GasPump", Vector3.zero);
        public static Vector3 CaseBackPosition => Object.FindObjectsByType<NightThreeCaseBack>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault()?.SurfaceCentre ?? Room;
        GameObject threeWorld;
        NightRoomDoor threeDoor;
        RenderTexture threeCamera;
        Camera threePhotoCamera;
        Texture2D threePhotoPreview;
        string selectedPhoto="";
        readonly System.Collections.Generic.List<Mesh> threePhotoMeshes = new System.Collections.Generic.List<Mesh>();
        public string MissingThreeAssets { get; private set; } = "";
        void BindThree(Transform target, string action)
        {
            if (target == null) return;
            var node = new GameObject("Night3 interaction " + action);
            node.transform.SetParent(threeWorld.transform);
            var bounds = ModelBounds(target);
            if (!target.GetComponentsInChildren<Renderer>(true).Any()) bounds=new Bounds(target.position+Vector3.up*.9f,new Vector3(.6f,1.8f,.6f));
            node.transform.position = bounds.center;
            node.AddComponent<BoxCollider>().size = Vector3.Max(bounds.size + Vector3.one*.1f, Vector3.one*.2f);
            var interaction = node.AddComponent<MorningInteraction>(); interaction.owner=this; interaction.action=action;
        }
        void EnsureThreeWorld()
        {
            if(threeWorld!=null)return;
            threeWorld=new GameObject("Night 3 — realistic interactions");threeWorld.transform.SetParent(world.transform);
            threeDoor=Object.FindFirstObjectByType<NightRoomDoor>();
            BindThree(SceneObject("NPC_N3_GasAttendant"),"gas");
            BindThree(SceneObject("PARK_N3_GasPump"),"store");
            BindThree(story.bag == null ? null : story.bag.transform,"bag3");
            BindThree(SceneObject("Phone box and charger"),"charge3");
            var police=VisibleModel("Night3_PoliceDesk","env_police_station");
            BindThree(police,"police3");
            MissingThreeAssets=police==null?"Đêm 3 đang chờ điểm tiếp nhận của công an phường (Night3_PoliceDesk); không dùng khối thô.":"";
            var source=story.nganPhone == null ? null : story.nganPhone.transform.Find("Ngan phone model");
            if(source!=null && rack!=null)
            {
                var phone=Instantiate(source.gameObject,threeWorld.transform);phone.name="Night3 phone evidence";
                phone.transform.localScale=Vector3.one;phone.transform.rotation=Quaternion.identity;
                var shelfBounds=ModelBounds(rack);
                phone.transform.position=new Vector3(shelfBounds.center.x,shelfBounds.max.y+.02f,shelfBounds.center.z);
                foreach(var filter in phone.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer=filter.GetComponent<Renderer>(); if(renderer==null || filter.sharedMesh==null)continue;
                    for(int i=0;i<filter.sharedMesh.subMeshCount;i++)
                    {
                        if(i>=renderer.sharedMaterials.Length || renderer.sharedMaterials[i]==null)continue;
                        string material=renderer.sharedMaterials[i].name;
                        // The clear protective case does not hide the fabric underneath it.
                        if(material.Contains("NganPhoneCase"))continue;
                        var mesh=new Mesh {name="Phone photograph surface",indexFormat=filter.sharedMesh.indexFormat};
                        mesh.vertices=filter.sharedMesh.vertices;mesh.triangles=filter.sharedMesh.GetTriangles(i);mesh.RecalculateBounds();
                        threePhotoMeshes.Add(mesh);
                        var surface=new GameObject("Phone surface " + material);surface.transform.SetParent(filter.transform,false);
                        surface.AddComponent<MeshCollider>().sharedMesh=mesh;
                        if(material.Contains("NganPhoneFabric"))
                        {
                            var patch=surface.AddComponent<NightThreeCaseBack>();
                            patch.firstTriangle=0;patch.triangleCount=mesh.triangles.Length/3;patch.mesh=mesh;patch.submesh=0;
                            phone.transform.rotation=Quaternion.FromToRotation(patch.SurfaceNormal,Vector3.up)*phone.transform.rotation;
                        }
                    }
                }
                phone.SetActive(true);
                var phoneBounds=ModelBounds(phone.transform);
                phone.transform.position+=Vector3.up*(shelfBounds.max.y+.002f-phoneBounds.min.y);
            }
            else MissingThreeAssets += " Cần model mặt lưng máy Ngân và kệ để đặt máy.";
        }
        void RefreshThreeWorld()
        {
            if(threeWorld==null)return;
            if(threeDoor!=null)threeDoor.SetFollowupLocked(Third.doorLocked);
            if(milkTea!=null)milkTea.gameObject.SetActive(Third.teaArrived&&!Third.teaTaken);
            if(bikeHomeKnown && story.ownBike!=null && !Third.riding)
                story.ownBike.transform.SetPositionAndRotation(bikeHome,bikeHomeRotation);
        }
        string ThreePrompt(string action)
        {
            switch(action)
            {
                case "bike":return "Bật máy — tới cây xăng";
                case "store":return "Bật máy — về nhà trọ";
                case "gas":return "Đổ xăng qua nắp phía trước";
                case "police3":return "Nộp máy Ngân cho cán bộ trực";
                case "bag3":return "Giật ngăn nhỏ ba lô, tìm cáp";
                case "charge3":return "Cắm sạc máy Ngân";
                case "tea":return "Ly trà sữa cô Liên nhận hộ";
                case "bed":return "Ngồi xuống nệm";
                default:return "Đồ vật trong phòng";
            }
        }
        void InteractThree(string action)
        {
            if(!ThreeReady || Third.riding)return;bool ok=false;
            switch(action)
            {
                case "bike":case "store":ok=ThreeMount();break;
                case "gas":ok=RefuelThree();break;
                case "police3":ok=ThreeHandInPhone();break;
                case "bag3":ok=FindThreeCable();break;
                case "charge3":ok=ChargeThree();break;
                case "tea":if(Third.teaArrived&&!Third.teaTaken&&Nearby(LienDesk,4)){OpenThreePage("tea3");ok=true;}break;
                case "bed":
                    if(Nearby(Room,4))
                    {
                        if(Third.stage==NightThreeStage.MorningRoom)ok=ThreeNap();
                        else {OpenThreePage("bed3");ok=true;}
                    }break;
            }
            if(!ok)story.Say(ThreeObjective);
        }
        public bool DecideThreeTea(bool drink)
        {
            if(!ThreeReady || !Third.teaArrived || Third.teaTaken)return false;
            Third.teaTaken=true;Third.drankTea=drink;CloseThreePage();Checkpoint();return true;
        }
        string ThreePhotoPath(string id) => Path.Combine(NightPersistence.DataRoot,"captures",id+".png");
        void EnsureThreeCamera()
        {
            if(threeCamera!=null)return;
            threeCamera=new RenderTexture(512,288,24);threeCamera.Create();
            threePhotoCamera=new GameObject("Phone photograph camera").AddComponent<Camera>();
            threePhotoCamera.transform.SetParent(threeWorld.transform);threePhotoCamera.enabled=false;
        }
        void RenderThreeCamera()
        {
            EnsureThreeCamera();var source=story.player.eyes;
            // Do not render the player's URP camera stack: its overlay camera
            // has its own view matrix and would put first-person arms in photos.
            threePhotoCamera.CopyFrom(source);threePhotoCamera.enabled=false;
            threePhotoCamera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
            threePhotoCamera.ResetWorldToCameraMatrix();threePhotoCamera.ResetProjectionMatrix();
            threePhotoCamera.aspect=512f/288;threePhotoCamera.cullingMask=source.cullingMask&~PlayerGeometry.PlayerMask&~PlayerGeometry.ArmsMask;
            threePhotoCamera.targetTexture=threeCamera;threePhotoCamera.Render();
        }
        public bool CaptureThreePhoto()
        {
            if(!ThreeReady || story.player.ActivePhone!=2 || Third.riding || Third.photos.Count>=32)return false;
            EnsureThreeCamera();var camera=story.player.eyes;
            bool evidence=Physics.Raycast(camera.transform.position,camera.transform.forward,out var hit,3,PlayerGeometry.WorldMask,QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponent<NightThreeCaseBack>() is NightThreeCaseBack patch && patch.IsEvidence(hit);
            var active=RenderTexture.active;Texture2D image=null;
            try
            {
                RenderThreeCamera();RenderTexture.active=threeCamera;
                image=new Texture2D(512,288,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,512,288),0,0);image.Apply();
                string id=Guid.NewGuid().ToString("N");string path=ThreePhotoPath(id);Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path,image.EncodeToPNG());
                Third.photos.Add(new NightThreePhoto{id=id,caseBack=evidence});selectedPhoto=id;
                if(threePhotoPreview!=null)Destroy(threePhotoPreview);threePhotoPreview=image;image=null;
                story.Say("Đã lưu ảnh vào thư viện máy mình.");Checkpoint();return true;
            }
            catch(IOException){story.Say("Không lưu được ảnh. Kiểm tra dung lượng và quyền ghi.");return false;}
            catch(UnauthorizedAccessException){story.Say("Không có quyền ghi ảnh.");return false;}
            finally{RenderTexture.active=active;if(image!=null)Destroy(image);}
        }
        public bool SendThreePhoto(string id)
        {
            if(!ThreeReady || story.player.ActivePhone!=2 || Third.contactMode=="none" || Third.sentCasePhoto)return false;
            var photo=Third.photos.FirstOrDefault(p=>p.id==id);
            if(photo==null || !photo.caseBack || !File.Exists(ThreePhotoPath(photo.id))){story.Say("Cô Hòa vẫn cần bằng chứng về chiếc máy.");return false;}
            Third.sentCasePhoto=true;story.State.messages.Add("Bạn → cô Hòa · [Ảnh mặt lưng máy Ngân, cá bống chỉ cam]");
            story.Say("Cô Hòa gửi tin thoại rồi gọi tới. Bấm Nghe để nhận.");Checkpoint();return true;
        }
        void ReleaseThreeCamera()
        {
            if(threeCamera!=null){threeCamera.Release();Destroy(threeCamera);}
            if(threePhotoPreview!=null)Destroy(threePhotoPreview);
            foreach(var mesh in threePhotoMeshes)Destroy(mesh);
            threePhotoMeshes.Clear();
        }
    }
}
