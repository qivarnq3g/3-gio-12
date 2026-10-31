# Đêm 1 bản thô (#14)

Nguồn yêu cầu: `docs/kich-ban/dem-1.md` trên main, gồm cập nhật truy đuổi duyệt 29/10. Nhánh triển khai kế thừa #6 (PR #45), không sửa scene bối cảnh #15 của Quang. Scene này dùng khối đơn giản và lời thoại nháp để duyệt logic, chưa phải bản đồ họa/âm thanh hoàn thiện.

## Chạy

Unity **6000.3.26f1**. Mở `Assets/Scenes/Night01/NightOneGraybox.unity`, Play, nhấp Game view. Nếu mất focus, Esc/Tiếp tục để nhận lại điều khiển.

WASD: đi; chuột: nhìn; Shift: chạy; C: cúi; E: tương tác; giữ E 2 giây để đặt máy trong mở đầu; 1/2: hai điện thoại; N: Sổ tay; Esc: đóng giao diện; ngoài giao diện là tạm dừng/tiếp tục, dừng cả thế giới và âm thanh. Khi điện thoại/Sổ tay/hội thoại mở, nhân vật đứng yên nhưng NPC tiếp tục chạy.

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
| `StoryClock` | Mốc giờ chỉ tiến, có ngày riêng khi qua nửa đêm |
| `CheckpointService` | Bản chụp tiến trình trong bộ nhớ và vị trí phục hồi |
| `NightOneDirector` | Điều kiện chuyển đoạn, lựa chọn, nhảy giờ, hậu quả bị tóm; phát sự kiện |
| `NightInteraction` | Đối tượng tương tác theo loại, giữ E khi đặt máy |
| `NightOneHud` | HUD, hội thoại và hai điện thoại tối thiểu để duyệt; UI #17 có thể thay mà giữ director/state |
| `NightObserver` | Nón nhìn, vật chắn, chú ý, nghi/đuổi/tìm; di chuyển trên NavMesh đã bake |
| `NightOneSetup` | Công cụ Editor dựng lại scene/prefab/NavMesh; không cần khi chơi bản build |

- #17: thay HUD điện thoại bằng UI thật; dùng `Send`, `ChooseNumber`, `TogglePower`, `StopAlarm` và dữ liệu tin nhắn. Không sửa tiến trình bằng nội dung hiển thị trên UI.
- #20: mở rộng/cân chỉnh thành phần người quan sát dùng chung, giữ trạng thái chấn thương `prologue_beaten`.
- #21: lưu `NightOneState` ra `persistentDataPath`, quản lý phiên bản và cài đặt. Hiện thoát Play sẽ mất tiến độ; không tuyên bố đã làm #21.
- #25: subscribe `TelemetryProduced` để nhận payload có kiểu, đúng hợp đồng API và chỉ ghi/gửi khi đồng ý. `InternalEvent` dành riêng cho diễn biến trong game, không gửi API. Bản thô chỉ phát sự kiện cục bộ, không gửi dữ liệu mạng.
- #15/#16: thay geometry/nhân vật bằng asset thật, giữ collider, vị trí tương tác và điểm lưu. Scene Quang không bị ghi đè.

## Giới hạn cần ghi trong PR

- Người, xe, tay và vật chứng dùng khối giữ chỗ; chưa có animation nhặt/đặt/dắt xe hoặc model chấn thương.
- Lời thoại nháp đọc từ `Resources/Story/night-one-dialogue.json`; UI điện thoại tạm, chưa thay #17.
- Cảnh bị tóm dùng nền đen và chữ, chờ âm thanh được cấp phép; báo thức và tiếng thở là âm tổng hợp.
- Vị trí công an là điểm tương tác đại diện ở đầu hẻm; chưa dựng đoạn đường 200 m.
- Cần người khác chơi thử độ khó và kiểm tra bằng thao tác tay; kiểm thử tự động dùng gọi tương tác/di chuyển vị trí, không thay thế chơi thử.
- Không đánh dấu đóng #14/#20 chỉ vì kiểm tra mã đạt. Video và danh sách phần còn giữ chỗ phải đi cùng PR.

## Kiểm thử bằng Unity

`NightOneSmoke.Run` mở Play Mode, kiểm đường NavMesh, ba mức bị tóm, chuyển mở đầu, nhặt máy/lô A, khóa lựa chọn camera, nhảy giờ duy nhất/lô B, ngủ/thức, báo thức, kết thúc sớm, hàng đợi khi tắt nguồn và checkpoint. Chạy ở phiên Unity riêng vì công cụ tự thoát khi hoàn tất.

`NightOneSmoke.Record` ghi hình tự động vào thư mục tạm; chỉ là minh họa kiểm logic, có chuyển vị trí để rút ngắn thời gian. Video thao tác tay cần bổ sung trước khi đóng issue.

NavMesh đã bake nằm trong scene; tệp NavMesh `.asset` là nhị phân và được đặt `-text` trong `.gitattributes` để Git không đổi byte xuống dòng.

Menu `3 Gio 12/Night 1/Create Graybox` ghi đè scene khối thô này; không chạy sau khi đã chỉnh scene thủ công nếu chưa lưu bản chỉnh.


## Bản sửa theo review qivarnq3g (#49)

- `StoryClock.Set` từ chối mốc lùi trong cùng ngày; chuyển qua nửa đêm phải chỉ rõ ngày 1. Snapshot checkpoint được phép khôi phục mốc cũ bằng một state riêng.
- `lienCalled` nằm trong state: nhánh 0b chỉ gọi một lần, kể cả tin số lạ tới sau 21:40.
- Đoạn 20:56 không có Sổ tay. Phục hồi an toàn khi rơi giữ nguyên số lần bị tóm, tốc độ giảm và đèn cảnh giác. Chỉ lần bị tóm thứ ba (hoặc bỏ kế hoạch đúng nhánh kịch bản) khởi tạo lại màn.
- Ba lô đã dời được giữ đúng vị trí và mở hé ngăn chính; chưa dời thì chuyển lên nệm khi UI che. Nhánh không cầm máy dùng cắt đen 0.25 giây lúc ngồi xuống, không dịch vật ngay trước mắt.
- Lô D trả lời từng câu gửi sau 21:26. Cô Liên chìa găng xám ở mức bị tóm 2; tiếng thở khi chạy/lên cầu thang giữ tới hết đêm.
- Báo thức chỉ phát ở người chơi khi đang cầm máy và máy bật; không phát trong phòng nếu máy nằm ở nhà xe.
- Đèn nhà xe bật từ mức cảnh giác 1. Hai xe máy có đèn pha đi qua sau khi rời đầu hẻm; độ sáng xét nón sáng và vật chắn. Lịch cô Liên 40–55 giây tính từ lúc rời đầu hẻm, không từ mở scene.
- Layer `Player` / `FirstPersonArms`, URP camera stacking và kiểm trần đủ chiều cao dùng chung được áp dụng cho cả phòng thử #6 và scene đêm 1.

### Sự kiện và hợp đồng API

`NightTelemetry` mang `eventId`, `sessionId`, `sequence`, `occurredAt`, `type`, `night`. Các lớp con có đúng trường riêng: `isReplay`, `durationSeconds`, `choiceId`, `endingId`, hoặc `areaId` / `checkpointId` / `position`. JSON dùng đúng camelCase. Thời gian chơi dùng thời gian game thực trừ lúc pause, không dùng giờ kịch bản. Cờ replay trong phiên được giữ khi chơi lại điểm lưu sau hoàn thành; lưu replay xuyên lần mở ứng dụng thuộc #21.

Chỉ lựa chọn có mã canonical trong tài liệu mới phát thống kê: nộp máy `0`, để máy `0b`, số lạ `1a`/`1b`/`1c`. Camera, câu hỏi chat và ngủ/thức phát `InternalEvent`; không tự đặt mã số chưa được chốt trong bản 5. Adapter không cần dịch các chuỗi `camera:yes` hoặc `areaId|checkpointId` nữa.

`NightOneReviewRegression.Run` kiểm các lỗi reviewer nêu; xuất JSONL để kiểm bằng schema `TelemetryEvent` trong OpenAPI #37. `NightOneSmoke.Run` kiểm lại toàn tuyến; `PlayerTestSmoke.Run` kiểm input #6. Kiểm thử bằng mã chưa thay cho lượt chơi tay độc lập để cân chỉnh độ khó/âm lượng.


Kiểm dữ liệu xuất từ Unity: cài gói phát triển `PyYAML` và `jsonschema`, rồi chạy `python3 tools/validate_night_events.py /private/tmp/3gio12-review-events.jsonl`. Công cụ dùng OpenAPI trong `docs/api/` hoặc bản ở nhánh API-contract nếu chưa merge, không gọi mạng. Có thể truyền `--openapi <đường dẫn>` để chỉ định bản hợp đồng cần đối chiếu.

`NightOneWalkSmoke.Run` kiểm bằng input thật: đi lên cầu thang và nhận tiếng thở do đau sườn, Esc dừng/tiếp tục, điện thoại không dừng thế giới; đồng thời chụp camera tay khi đứng sát tường. Model vẫn là khối giữ chỗ, cần người duyệt cân chỉnh âm lượng và độ khó trong một lượt chơi tay.
