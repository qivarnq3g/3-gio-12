using System;
using System.IO;
using System.Linq;
using BaGio12.Gameplay;
using BaGio12.Persistence;
using UnityEngine;

namespace BaGio12.Story
{
    public sealed partial class NightTwoMorning
    {
        public static readonly Vector3 ThreePolice=new Vector3(86,.08f,20), ThreeBag=new Vector3(99,.08f,-3);
        public static readonly Vector3 CaseBackPosition=new Vector3(101.5f,1.35f,2.5f);
        GameObject threeWorld,threeDoor;
        RenderTexture threeCamera;
        Camera threePhotoCamera;
        Texture2D threePhotoPreview;
        string selectedPhoto="";
        void EnsureThreeWorld()
        {
            if(threeWorld!=null)return;
            threeWorld=new GameObject("Night 3 temporary props");threeWorld.transform.SetParent(world.transform);
            var mat=Material(new Color(.8f,.45f,.15f));
            var phone=Box("Mặt lưng máy Ngân — cá bống chỉ cam",CaseBackPosition,new Vector3(.28f,.5f,.04f),mat);
            phone.transform.SetParent(threeWorld.transform);phone.AddComponent<NightThreeCaseBack>();
            threeDoor=Box("Cửa phòng 7",Room+new Vector3(0,1.1f,3),new Vector3(2.2f,2.2f,.1f),mat);
            threeDoor.transform.SetParent(threeWorld.transform);
            foreach(var tuple in new[]{("Cây xăng",Store+new Vector3(-2,.6f,0),"gas"),("Công an phường",ThreePolice+Vector3.up*.6f,"police3"),("Ba lô — ngăn nhỏ",ThreeBag+Vector3.up*.6f,"bag3"),("Cáp / ổ sạc",Room+new Vector3(2,.6f,1),"charge3")})
            {var node=Node(tuple.Item1,tuple.Item2,tuple.Item3,mat);node.transform.SetParent(threeWorld.transform,true);}
        }
        void RefreshThreeWorld()
        {
            if(threeWorld==null)return;
            bool evening=Third.stage>=NightThreeStage.Evening;
            worldSun.intensity=evening?.12f:1.2f;deskLamp.enabled=true;
            story.player.eyes.backgroundColor=evening?originalSky:new Color(.42f,.62f,.78f);
            foreach(var n in morningNodes)n.SetActive(false);
            foreach(var n in eveningNodes)n.SetActive(false);
            bikeNode.transform.position=Vector3.zero;
            teaNode.SetActive(Third.teaArrived&&!Third.teaTaken);
            threeDoor.transform.rotation=Quaternion.Euler(0,Third.doorLocked?0:90,0);
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
                && hit.collider.GetComponent<NightThreeCaseBack>()!=null;
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
        }
    }
}
