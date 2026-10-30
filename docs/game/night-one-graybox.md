# Đêm 1 bản thô (#14)

Nguồn yêu cầu: `docs/kich-ban/dem-1.md` trên main, gồm cập nhật truy đuổi duyệt 29/10. Nhánh triển khai kế thừa #6 (PR #45), không sửa scene bối cảnh #15 của Quang. Scene này dùng khối đơn giản và lời thoại nháp để duyệt logic, chưa phải bản đồ họa/âm thanh hoàn thiện.

## Chạy

Unity **6000.3.26f1**. Mở `Assets/Scenes/Night01/NightOneGraybox.unity`, Play, nhấp Game view. Nếu mất focus, Esc/Tiếp tục để nhận lại điều khiển.

WASD: đi; chuột: nhìn; Shift: chạy; C: cúi; E: tương tác; giữ E 2 giây để đặt máy trong mở đầu; 1/2: hai điện thoại; N: Sổ tay; Esc: đóng giao diện hoặc thả chuột. Khi điện thoại/Sổ tay/hội thoại mở, nhân vật đứng yên nhưng NPC tiếp tục chạy.

Các khối xanh/vàng là đối tượng tương tác; nhìn vào sẽ có gợi ý. Màu xám/tối là bối cảnh và vật chắn tầm nhìn. Không có phản chiếu mặt.

## Tuyến chơi

| Đoạn | Điều kiện vào/ra | Mốc giờ và trạng thái |
|---|---|---|
| Đầu hẻm → nhà xe | Tránh người quan sát, cúi và giữ E ở khối đặt máy cạnh xe; quay ra đầu hẻm | 20:56; chưa cho rút điện thoại; cảnh nhìn camera rồi hiện tên game |
| Nhà xe | Tương tác xe mình, xem số đồng hồ, nhặt máy ở cạnh xe bên kia | 21:00; lô A; điểm lưu nhà xe |
| Hẻm tùy chọn | Tiệm chú Hùng, camera, biển công an; có thể bỏ qua tiệm và camera | 21:10; xem camera mới mở câu trả lời biết camera |
| Cổng → tầng 3 → phòng 7 | Phụ cô Liên dựng chậu, đi cầu thang; tương tác cửa phòng 7 | 21:20; điểm lưu phòng 7; ba lô cạnh cửa |
| Phòng 7 | Xem vật chứng, điện thoại; lần gửi tin đầu trong phòng hoặc đóng máy lần đầu | Chỉ nhảy một lần tới 21:26; lô B, ba lô đổi chỗ |
| Số lạ | Khoảng 8 giây chơi sau lần nhảy giờ hoặc ngồi xuống nệm | 21:35; lô C; chặn/trả lời/bỏ qua |
| Nệm → khuya | Chọn chờ tới 22:30, rồi ngủ hoặc thức | Điểm lưu khuya; ngủ có D lúc 03:40; thức có báo thức 03:12 |
| Kết thúc | Ngủ, hoặc tắt báo thức và chờ sáng | Dừng 06:29, không chơi sang đêm 2 |

Bản thô rút ngắn thời gian chờ để duyệt; chưa được cân chỉnh thành lượt chơi 30–45 phút.

## Các nhánh phải kiểm

- Giữ máy: đi tới hết đêm bằng cả ngủ và thức.
- Mang máy đến biển công an ở đầu hẻm: xác nhận nộp → Người qua đường.
- Để máy lại ở khối đặt máy dưới chân xe: lần đầu cô Liên gọi xuống lấy; lấy rồi để lần hai → Người qua đường khi sang sáng.
- Không xem camera: câu đáp biết camera bị khóa; xem rồi mới mở.
- Gửi câu hỏi trước 21:26: lô B đáp; gửi sau đó không nhận phản hồi Mẹ tức thì.
- Tắt nguồn: tin nằm hàng đợi; bật lại mới hiện.
- Mẹ không có hiệu ứng đang nhập. Số lạ tới đã có trạng thái đã đọc.
- Mở máy/Sổ tay không dừng thời gian thế giới.
- Khôi phục điểm lưu tạo bản sao độc lập, không giữ lựa chọn phát sinh sau điểm lưu.

## Phân chia mã và điểm nối

| Thành phần | Trách nhiệm |
|---|---|
| `FirstPersonPlayer` | Bộ điều khiển dùng chung; `SetOverlay`, `SetPhone`, `CanOpenPhone`, `PhoneChanged`; HUD phòng thử có thể tắt |
| `NightOneState` | Biến truyện, tin nhắn, Sổ tay; tuần tự hóa JSON được, không tự ghi ra đĩa |
| `StoryClock` | Mốc giờ truyện |
| `CheckpointService` | Bản chụp tiến trình trong bộ nhớ và vị trí phục hồi |
| `NightOneDirector` | Điều kiện chuyển đoạn, lựa chọn, nhảy giờ, hậu quả bị tóm; phát sự kiện |
| `NightInteraction` | Đối tượng tương tác theo loại, giữ E khi đặt máy |
| `NightOneHud` | HUD, hội thoại và hai điện thoại tối thiểu để duyệt; UI #17 có thể thay mà giữ director/state |
| `NightObserver` | Nón nhìn, vật chắn, chú ý, nghi/đuổi/tìm; di chuyển trên NavMesh đã bake |
| `NightOneSetup` | Công cụ Editor dựng lại scene/prefab/NavMesh; không cần khi chơi bản build |

- #17: thay HUD điện thoại bằng UI thật; dùng `Send`, `ChooseNumber`, `TogglePower`, `StopAlarm` và dữ liệu tin nhắn. Không sửa tiến trình bằng nội dung hiển thị trên UI.
- #20: mở rộng/cân chỉnh thành phần người quan sát dùng chung, giữ trạng thái chấn thương `prologue_beaten`.
- #21: lưu `NightOneState` ra `persistentDataPath`, quản lý phiên bản và cài đặt. Hiện thoát Play sẽ mất tiến độ; không tuyên bố đã làm #21.
- #25: subscribe `StoryEvent`; chuẩn hóa dữ liệu theo hợp đồng API và chỉ ghi/gửi khi đồng ý. Bản thô chỉ phát sự kiện cục bộ, không gửi dữ liệu mạng.
- #15/#16: thay geometry/nhân vật bằng asset thật, giữ collider, vị trí tương tác và điểm lưu. Scene Quang không bị ghi đè.

## Giới hạn cần ghi trong PR

- Người, xe, tay và vật chứng dùng khối giữ chỗ; chưa có animation nhặt/đặt/dắt xe hoặc model chấn thương.
- Lời thoại nháp đọc từ `Resources/Story/night-one-dialogue.json`; UI điện thoại tạm, chưa thay #17.
- Cảnh bị tóm dùng nền đen và chữ, chờ âm thanh được cấp phép; báo thức là âm tổng hợp.
- Vị trí công an là điểm tương tác đại diện ở đầu hẻm; chưa dựng đoạn đường 200 m.
- Cần người khác chơi thử độ khó và kiểm tra bằng thao tác tay; kiểm thử tự động dùng gọi tương tác/di chuyển vị trí, không thay thế chơi thử.
- Không đánh dấu đóng #14/#20 chỉ vì kiểm tra mã đạt. Video và danh sách phần còn giữ chỗ phải đi cùng PR.

## Kiểm thử bằng Unity

`NightOneSmoke.Run` mở Play Mode, kiểm đường NavMesh, ba mức bị tóm, chuyển mở đầu, nhặt máy/lô A, khóa lựa chọn camera, nhảy giờ duy nhất/lô B, ngủ/thức, báo thức, kết thúc sớm, hàng đợi khi tắt nguồn và checkpoint. Chạy ở phiên Unity riêng vì công cụ tự thoát khi hoàn tất.

`NightOneSmoke.Record` ghi hình tự động vào thư mục tạm; chỉ là minh họa kiểm logic, có chuyển vị trí để rút ngắn thời gian. Video thao tác tay cần bổ sung trước khi đóng issue.

NavMesh đã bake nằm trong scene; tệp NavMesh `.asset` là nhị phân và được đặt `-text` trong `.gitattributes` để Git không đổi byte xuống dòng.

Menu `3 Gio 12/Night 1/Create Graybox` ghi đè scene khối thô này; không chạy sau khi đã chỉnh scene thủ công nếu chưa lưu bản chỉnh.
