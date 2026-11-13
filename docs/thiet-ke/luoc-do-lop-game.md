# Lược đồ lớp phần game

Phụ trách: Phúc. Phục vụ #11 và phần lược đồ lớp trong thuyết minh thiết kế #13. Đối chiếu với `main` tại commit [`95b7317`](https://github.com/qivarnq3g/3-gio-12/tree/95b73172d47f7f7cb2bcee8f75c5eb6274cbfbb1), sau khi ghép #6, #14 và #10. Phần lưu tiến độ của #21 đã đối chiếu với `main` tại commit `591b994` (mục 4.1). Khi #17, #20 hoặc phần game của #25 được gộp, đối chiếu lại sơ đồ và sửa ngày đối chiếu ở mục 1 trước khi nộp #13. Bản vẽ gốc ở `lop-game/luoc-do-lop-game.drawio`, mỗi trang ứng với một hình bên dưới; sửa trong draw.io rồi xuất lại ảnh PNG cùng tên trong `lop-game/` (Xuất thành, PNG, tỉ lệ 200%). Đoạn này là ghi chú cho nhóm, không vào bản nộp; trong nội dung bên dưới không ghi số issue, vì script dựng Word báo lỗi khi gặp.

## 1. Phạm vi và phiên bản đối chiếu

Các sơ đồ đối chiếu với mã nguồn game ngày 31/10/2026, khi đã có điều khiển góc nhìn thứ nhất và bản chơi thô Đêm 1. Riêng phần lưu tiến độ và cài đặt ở mục 4.1 đối chiếu ngày 05/11/2026. Phần máy chủ và web ở task #12; ranh giới giữa các thành phần ở task #10.

Các sơ đồ dưới đây mô tả lớp đã có trong code. Chỉ liệt kê thành viên liên quan đến trách nhiệm và quan hệ chính, không chép toàn bộ hàm dựng UI, hàm Unity lifecycle hay cấu hình màn chơi. Các kiểu `MonoBehaviour`, `Camera`, `CharacterController`, `NavMeshAgent`, `InputActionAsset`, `UIDocument` là kiểu của Unity/package, không phải lớp tự viết.

| Ký hiệu | Ý nghĩa |
|---|---|
| Nét liền, đầu tam giác rỗng chỉ vào A | Lớp ở đầu kia kế thừa A |
| Nét liền, đầu mũi tên mở từ A tới B | A giữ tham chiếu tới B; không khẳng định sở hữu vòng đời |
| Nét đứt, đầu mũi tên mở từ A tới B | A sử dụng B qua lời gọi, tham số, kiểu dữ liệu hoặc sự kiện |
| Nét liền, hình thoi đặc ở phía A | A tạo và giữ B như thành phần nội bộ |
| `+`, `-`, `#` | Public, private, protected |
| `tên: Kiểu`, `Hàm(tham số: Kiểu): Kiểu trả về` | Cách ghi thuộc tính và phương thức theo UML |
| Chữ gạch chân | Thành viên tĩnh (static) |
| `0..1`, `0..*`, `1` | Số lượng ở đầu quan hệ; có thể thay đổi theo thời điểm chạy |

Trường/property trên sơ đồ giữ nguyên tên code. Sự kiện được ghi bằng kiểu `Action`, delegate kiểm tra điều kiện bằng `Func`; phần giải thích chỉ rõ chiều phát và chiều đăng ký. Setter private không làm property trở thành private. Riêng `CheckpointService.Save` và `Restore` lược bỏ kiểu tham số trên hình để tránh chồng chữ; chữ ký đầy đủ xem source được liên kết tại mục 4. Các quan hệ scene dùng association thay vì composition vì Unity quản lý vòng đời GameObject.

### 1.1. Sơ đồ tổng quát

![Lược đồ lớp: Sơ đồ tổng quát](lop-game/overview.png)

Hình tổng quát xếp mọi lớp tự viết của game vào sáu nhóm và chỉ ghi tên lớp. Quan hệ giữa hai lớp cùng nhóm được vẽ như ở các hình chi tiết. Quan hệ giữa hai nhóm được vẽ thành một mũi tên nét đứt nối hai khung, nghĩa là có lớp trong nhóm đầu dùng lớp trong nhóm sau. `NightOneDirector` là trung tâm: lớp này điều khiển nhân vật, tạo và giữ trạng thái Đêm 1, trao đổi dữ liệu với phần lưu tiến độ và phát sự kiện thống kê. Nhóm điện thoại là bản thử giao diện trong `SampleScene`, chưa nối với các nhóm khác. Kiểu của Unity và kiểu liệt kê được lược bỏ; thuộc tính, phương thức và nhãn quan hệ xem ở các mục sau.

## 2. Nhân vật, camera và tương tác

Nguồn: [FirstPersonPlayer.cs](../../game/Assets/Scripts/Player/FirstPersonPlayer.cs), [WorldInteractable.cs](../../game/Assets/Scripts/Player/WorldInteractable.cs), [PlayerGeometry.cs](../../game/Assets/Scripts/Player/PlayerGeometry.cs), [TestDoor.cs](../../game/Assets/Scripts/Player/TestDoor.cs), [TestPickup.cs](../../game/Assets/Scripts/Player/TestPickup.cs), [NightInteraction.cs](../../game/Assets/Scripts/Story/NightInteraction.cs).

### 2.1. Nhân vật và camera

![Lược đồ lớp: Nhân vật và camera](lop-game/nhan-vat-va-camera.png)

`FirstPersonPlayer` đọc Unity Input System, điều khiển `CharacterController`, xoay camera và raycast tìm `WorldInteractable`. Không có lớp camera riêng: `eyes` là camera con, yaw ở transform nhân vật và pitch ở camera. `PlayerGeometry` tập trung kích thước capsule và layer mask; các chiều cao là hằng số, các mask là property tĩnh.

### 2.2. Tương tác

![Lược đồ lớp: Tương tác](lop-game/tuong-tac.png)

`WorldInteractable.HandleInput` mặc định gọi `Interact` khi vừa nhấn. `NightInteraction` ghi đè để hỗ trợ giữ E đặt điện thoại; các hành động khác chuyển tới `NightOneDirector.Act(action)`. `TestDoor` và `TestPickup` phục vụ cảnh thử điều khiển, không phải hệ inventory/cửa tổng quát. Tham chiếu `target` được tìm lại mỗi frame và có thể null.

## 3. Điều phối Đêm 1 và kẻ truy đuổi

Nguồn: [NightOneDirector.cs](../../game/Assets/Scripts/Story/NightOneDirector.cs), [NightObserver.cs](../../game/Assets/Scripts/Stealth/NightObserver.cs), [NightOneHud.cs](../../game/Assets/Scripts/Story/NightOneHud.cs), [NightRoomDoor.cs](../../game/Assets/Scripts/Story/NightRoomDoor.cs).

Bốn lớp `NightOneDirector`, `NightObserver`, `NightOneHud`, `NightRoomDoor` đều kế thừa `MonoBehaviour` (lược bỏ các mũi tên Unity ở hai sơ đồ dưới cho dễ đọc).

### 3.1. Điều phối Đêm 1

![Lược đồ lớp: Điều phối Đêm 1](lop-game/dieu-phoi-dem-1.png)

`NightOneDirector` giữ trạng thái cốt truyện, điều kiện hành động, checkpoint và các observer được gán từ scene. Director đăng ký `FirstPersonPlayer.PhoneChanged` và `FellOutOfWorld`, đồng thời gán `CanOpenPhone`; nó gỡ đăng ký khi bị hủy.

`NightOneHud` vẽ UI qua IMGUI, đọc `story.State` và gọi hành động trên director. `NightRoomDoor` thay đổi `Collider.isTrigger` theo stage, không phải lớp con của `WorldInteractable`; component `NightInteraction` cùng GameObject mới xử lý E.

### 3.2. Kẻ truy đuổi

![Lược đồ lớp: Kẻ truy đuổi](lop-game/ke-truy-duoi.png)

`NightObserver` tính tầm nhìn bằng khoảng cách, góc nhìn và raycast, tăng/giảm `Attention`, rồi chuyển `ObserverMode`; `NavMeshAgent` đảm nhiệm đường đi khi observer có agent. Quan hệ `0..1` thể hiện tham chiếu có thể null; `Travel` bỏ qua di chuyển khi observer đứng yên hoặc agent null. Khi phát hiện hoặc bắt được người chơi, observer gọi director để xử lý tiếng hô và hậu quả. Director cũng gọi ngược observer để báo tiếng động hoặc reset. Đây là quan hệ hai chiều đang có, chưa tách thành hệ AI độc lập cho các đêm sau.

## 4. Trạng thái, đồng hồ và checkpoint

Nguồn: [NightOneState.cs](../../game/Assets/Scripts/Story/NightOneState.cs), [NightOneDirector.RestoreCheckpoint](../../game/Assets/Scripts/Story/NightOneDirector.cs).

![Lược đồ lớp: Trạng thái, đồng hồ và checkpoint](lop-game/trang-thai-dong-ho-va-checkpoint.png)

`NightOneState` là DTO serializable chứa tiến độ Đêm 1; sơ đồ chỉ chọn một số field đại diện. `Copy()` dùng `JsonUtility` serialize/deserialize để tạo bản sao. `StoryClock` giữ đúng instance state hiện hành và từ chối mốc thời gian đi lùi; director tạo lại clock sau khi thay state.

`CheckpointService` giữ một snapshot trong RAM, vị trí và hướng quay. `Save` sao chép state đầu vào; `Restore` trả một bản sao khác, không đưa instance snapshot ra ngoài. `NightOneDirector` áp dụng lại state, pose và trạng thái thế giới. Riêng hồi phục an toàn trong prologue giữ hình phạt bị bắt, không gọi restore snapshot để xóa hình phạt.

`CheckpointService` chỉ giữ dữ liệu trong RAM và không đọc lại được sau khi thoát ứng dụng. Phần ghi xuống đĩa ở mục 4.1 chụp thêm snapshot này vào tệp lưu.

### 4.1. Lưu và nạp tệp

Nguồn: [NightPersistence.cs](../../game/Assets/Scripts/Persistence/NightPersistence.cs), [LocalJsonStore.cs](../../game/Assets/Scripts/Persistence/LocalJsonStore.cs), [NightSaveData.cs](../../game/Assets/Scripts/Persistence/NightSaveData.cs), [GamePreferences.cs](../../game/Assets/Scripts/Persistence/GamePreferences.cs), [NightOnePersistence.cs](../../game/Assets/Scripts/Story/NightOnePersistence.cs), [NightObserverPersistence.cs](../../game/Assets/Scripts/Stealth/NightObserverPersistence.cs).

![Lược đồ lớp: Lưu và nạp tệp](lop-game/luu-va-nap-tep.png)

`NightPersistence` là `MonoBehaviour` gắn vào director. Lớp này tự lưu khoảng 2 giây một lần theo thời gian thực và lưu thêm khi mất focus, tạm dừng hoặc thoát ứng dụng; không lưu giữa lúc chuyển cảnh. `LocalJsonStore<T>` bọc dữ liệu bằng số phiên bản và mã băm SHA-256, ghi qua tệp tạm rồi mới thay tệp chính, giữ bản dự phòng `.bak` và kiểm tra hợp lệ trước khi trả dữ liệu cho game. Tiến độ nằm ở `night-one.json`, cài đặt nằm ở `preferences.json`, chỉ có một vị trí lưu và không gửi lên máy chủ.

### 4.2. Dữ liệu tệp lưu

![Lược đồ lớp: Dữ liệu tệp lưu](lop-game/du-lieu-tep-luu.png)

`NightOneDirector` và `NightObserver` là lớp `partial`; các hàm chụp và khôi phục đặt trong `NightOnePersistence.cs` và `NightObserverPersistence.cs`. `FirstPersonPlayer` có thêm `ViewPitch` và `RestoreView` để khôi phục góc nhìn và tư thế cúi. Phần lưu hiện chỉ áp dụng cho Đêm 1 và chưa có cơ chế chuyển đổi tệp lưu giữa các phiên bản dữ liệu.

## 5. Điện thoại: bản thô Đêm 1 và prototype UI

### 5.1. Điện thoại trong NightOneGraybox

Trong scene Đêm 1, `FirstPersonPlayer.ActivePhone` có giá trị 0 (cất), 1 (máy Ngân), 2 (máy sinh viên). Director quyết định có được mở qua `CanOpenPhone`; HUD đọc `ActivePhone` và `NightOneState` để vẽ giao diện. Không có lớp `PhoneController` riêng trong phiên bản này.

`NightOneHud` có các hàm private `DrawPhone`, `DrawModal`; các thao tác gửi tin, tắt nguồn, chọn trả lời được chuyển tới các hàm public `NightOneDirector.Send`, `TogglePower`, `ChooseNumber`. Dữ liệu hội thoại thật lấy từ `NightDialogue` và các field/list của `NightOneState`.

### 5.2. Prototype trong SampleScene

Nguồn: [PhonePrototype.cs](../../game/Assets/Prototype/Scripts/PhonePrototype.cs), [PrototypeData.cs](../../game/Assets/Prototype/Scripts/PrototypeData.cs).

![Lược đồ lớp: Prototype trong SampleScene](lop-game/prototype-trong-samplescene.png)

`PhonePrototype` kế thừa `MonoBehaviour`; các DTO còn lại là lớp `[Serializable]` thông thường. `Boot()` là static private và chỉ tự khởi tạo trong `SampleScene`. Prototype dùng UI Toolkit/`UIDocument`, đọc `Resources/Prototype/chat-mau.json`, giữ lịch sử tin đã hiện trong RAM. Tin nhắn đã hiện được lưu trong `Dictionary<string, List<Message>> shown`; lựa chọn đã bấm nằm trong `HashSet<string> chose`.

Prototype này chưa nối với director hoặc HUD của Đêm 1; vì vậy không vẽ quan hệ giữa `PhonePrototype` và `FirstPersonPlayer`/`NightOneDirector`. Việc thay UI giữ chỗ bằng điện thoại hoàn chỉnh chưa làm; khi ghép cần chốt lại các lớp và luồng dữ liệu.

## 6. Sự kiện thống kê và ranh giới gửi mạng

Nguồn: [NightTelemetry.cs](../../game/Assets/Scripts/Story/NightTelemetry.cs), [NightOneDirector.cs](../../game/Assets/Scripts/Story/NightOneDirector.cs). Hợp đồng HTTP: [OpenAPI](../api/openapi.yaml), [hướng dẫn API](../api/README.md).

![Lược đồ lớp: Sự kiện thống kê và ranh giới gửi mạng](lop-game/su-kien-thong-ke-va-ranh-gioi-gui-mang.png)

Director phát `TelemetryProduced` với payload có kiểu; `NightTelemetry` cung cấp UUID, session ID, sequence, thời điểm UTC và JSON. `InternalEvent` là kênh debug nội bộ `(string type, string detail)` riêng, không phải sự kiện HTTP theo OpenAPI. Các lớp payload không kế thừa `MonoBehaviour`.

Trong code game đang đối chiếu chưa có subscriber sản phẩm gửi HTTP, consent, installation ID, batch, queue lưu đĩa hay retry. Sơ đồ không thêm lớp gửi mạng giả định; phần game gửi thống kê lên máy chủ chưa làm. Máy chủ đã có API nhận thống kê không có nghĩa game đã gửi được.

Khi làm adapter, cần chuyển đổi payload nội bộ sang DTO hợp đồng: `CaughtTelemetry.position` chỉ phục vụ cục bộ, không có trong event `player_caught` của OpenAPI; không gửi nguyên `ToJson()` của payload này lên endpoint có schema đóng. `installationId`, `gameVersion`, `platform` nằm ở envelope batch, không phải field của `NightTelemetry`. Việc gửi chỉ diễn ra khi người chơi đồng ý theo tài liệu kiến trúc/API.

## 7. Phạm vi đã mô tả và phần còn lại

| Nhóm lớp | Đã mô tả | Phạm vi chưa triển khai đầy đủ |
|---|---|---|
| Nhân vật | `FirstPersonPlayer`, camera, `PlayerGeometry` | Mở rộng gameplay theo các đêm sau |
| Tương tác | `WorldInteractable`, `TestDoor`, `TestPickup`, `NightInteraction`, `NightRoomDoor` | Inventory/hệ tương tác tổng quát nếu các đêm sau cần |
| Kẻ truy đuổi | `NightObserver`, `ObserverMode`, quan hệ director/NavMesh | Hệ truy đuổi hoàn chỉnh cho các đêm sau |
| Lưu game | `NightOneState`, `StoryClock`, `CheckpointService`, `NightPersistence`, `LocalJsonStore<T>`, `NightSaveData`, `GamePreferences` | Lưu cho đêm 2, đêm 3 và chuyển đổi phiên bản tệp lưu |
| Điện thoại | HUD Đêm 1 và các DTO/prototype UI Toolkit | Ghép điện thoại hoàn chỉnh |
| Gửi thống kê | Payload `NightTelemetry` và sự kiện phát từ director | Xin đồng ý, bộ đệm, gửi lại, phần gửi HTTP của game |

Các lớp trình bày phụ (`NightBagPresentation`, `NightInjuryFeedback`, `NightPassingBike`, `TestWorldClock`) và script trong `Assets/Editor/` được lược bỏ để tập trung vào sáu nhóm trên. Lược bỏ khỏi sơ đồ không có nghĩa xóa hoặc đổi trách nhiệm trong code.
