# Đêm 2: buổi tối và khuya (#22, đang làm)

Nối tiếp lát cắt buổi sáng (`night-two-morning.md`) từ lúc tỉnh dậy 21:00 ngày 25/11 tới chữ "Hết đêm 2". Đối chiếu `docs/kich-ban/dem-2.md` mục 2, 5.4, 5.5, 6, 7, 8. Chạy trong cảnh tả thực `NightOneRealistic`, dùng chung `NightTwoMorningState` và vùng tương tác vô hình gắn lên model có sẵn như buổi sáng; giao diện vẽ bằng `HudKit`. Phần lời thoại có giọng và các nội dung ảnh/video còn cần hoàn thiện.

## Chơi thử

1. Chơi qua buổi sáng Đêm 2 tới lúc nằm nghỉ. Tỉnh dậy 21:00; đồng hồ trong game chạy 1 phút mỗi 4 giây khi đang ở nhà hoặc ở quán cơm, dừng ở 22:59.
2. Máy Ngân (phím 1) có bảy mục: Tin nhắn, Ảnh, Bộ nhớ, Máy tính, Video, Linh, Khóa màn hình.
   - Tin nhắn: lô trưa (12:20) và lô tối (20:10) của "Mẹ" theo nhánh Đêm 1 và câu trả lời buổi sáng; tin dãy số lạ 21:15, 21:16, 21:40, 23:30 tự thành "Đã xem", không có nếu Đêm 1 đã chặn số.
   - Câu gửi "Mẹ" chỉ hiện khi đã thấy thứ liên quan: Q4 sau khi nhận trà sữa, Q5 sau khi xem Ảnh, Q6 sau khi xem Bộ nhớ, Q7 sau tin 21:16. Nhánh thức canh Đêm 1 có thêm câu trả lời số phòng. "Mẹ" không đáp trong lúc người chơi thức; câu đáp thuộc Đêm 3.
   - Máy tính: gõ số rồi bấm "=" thì đếm một lần thử két (`vaultWrongAttempts`), không có phản hồi trên màn hình.
   - Khóa màn hình: đặt mã máy Ngân (lựa chọn 15) làm "Mẹ" lên mức Nghi.
3. Xe máy đã nằm chỗ khác trong nhà xe. E để bật máy, chạy ra quán cơm (không có đèn đỏ), mua cơm, bật máy ở xe để về. Mỗi lượt cộng 0,5 km; Sổ tay ghi 21:30 và lúc tắt máy về.
4. Trà sữa (chỉ khi Đêm 1 đi ngủ và đã cho số phòng): tới lúc 21:10, cô Liên gọi nếu người chơi đang ở nhà, hoặc nói lúc người chơi về tới nếu đang ở quán. Lấy ly ở bàn cô Liên, chọn Uống hay Để đó.
5. Ăn xong, tới nệm: Đi ngủ hoặc Thức canh tới sáng.
   - Đi ngủ: màn đen rồi "Hết đêm 2".
   - Thức canh khi Đêm 1 đã đi ngủ: các mốc 23:30 tới 02:30, rồi báo thức 03:12 reo.
   - Thức canh hai đêm liền: người chơi đứng trong phòng 30 giây, màn hình chớp tắt; đứng yên 10 giây liền là ngủ gật, lỡ 03:12.

## Biến lưu cho Đêm 3

Trong `NightTwoMorningState`: `odoLog`, `roomReply`, `momSuspicion`, `teaArrived`, `teaTaken`, `drankMilkTea`, `vaultWrongAttempts`, `sawStorage`, `sawPhotos`, `sawVlog`, `sawLinhChat`, `setNganPasscode`, `lienSawMorning`, `night2Slept`, `vigilDozed`, `strangerDelivered`, `eveningSent` (các câu Q4 tới Q7 và câu số phòng, để lô 03:40 của Đêm 3 trả lời).

## Model và điểm đánh dấu cần có trong cảnh

Buổi tối tìm các đối tượng sau theo tên. Thiếu thứ nào thì `MissingEveningAssets` báo trên mục tiêu, và bước liên quan không làm được; không có khối thay thế hiện cho người chơi.

| Tên trong cảnh | Là gì | Dùng ở |
|---|---|---|
| `Night2_RiceShop` | Quán cơm bình dân trên đường lớn, cùng đoạn với cửa hàng đồ gia dụng | Bước mua cơm |
| `PARK_N2_RiceShop` | Điểm dừng xe trước quán cơm (thiếu thì dùng điểm của cửa hàng đồ gia dụng) | Điểm cuối tuyến xe buổi tối |
| `Night2_MilkTea` | Ly trà sữa có giấy dán đọc được, đặt trên bàn cô Liên | Bước nhận trà sữa |
| `PARK_N2_EveningBike` | Chỗ xe đậu sát vách, đầu quay ra, khác chỗ buổi sáng | Xe đổi chỗ lúc 21:00 |
| `PersonLien` | Cô Liên (đã có trong cảnh) | Hỏi cô Liên, vị trí bàn trà sữa |

## Kiểm tra tự động

```sh
THREEGIO12_REQUIRE_EVENING_MODELS=1 THREEGIO12_DATA_ROOT=<thư mục thử> Unity -batchmode -nographics -projectPath <repo>/game -executeMethod NightTwoEveningRegression.Run -logFile <log>
```

Bài mở cảnh `NightOneRealistic`; thiếu `THREEGIO12_DATA_ROOT` thì tự tạo thư mục thử trong `Library`. Với `THREEGIO12_REQUIRE_EVENING_MODELS=1`, bài bắt buộc có mesh FBX thật của năm model buổi sáng/quán cơm/trà sữa và hai marker đỗ xe; thiếu thì thoát lỗi trước khi chạy, không tạo vật thay thế. Renderer đang bật vẫn được nhận khi đối tượng/nhóm cha đang tắt. Không bật cờ này thì giữ fixture chỉ trong Editor cho các nhánh cũ thiếu model; fixture không được đưa vào scene/build. Thành công có dòng `NIGHT_TWO_EVENING_PASSED`. Bài chạy ba nhánh: Đêm 1 đi ngủ và cho số phòng (trà sữa, câu hỏi, két, đặt mã, lưu và nạp, đi ngủ); thức canh hai đêm (trả lời số phòng buổi tối, ngủ gật); Đêm 1 đi ngủ, từ chối số phòng, đã chặn số lạ (mức Nghi, báo thức 03:12, nạp lại giữa lúc báo thức reo).

## Chưa có

- Lời thoại Sinh viên ở cổng, quán cơm và các câu nghĩ hiện bằng phụ đề; chưa có giọng, ảnh, video thật.
- Tin dãy số lạ chưa có giờ máy chủ thật; giờ trong game chạy theo hằng số `SecondsPerMinute`.
- Câu T4 dòng đầu ("À cái báo thức đó hả cháu") chưa có vì Đêm 1 chưa có câu hỏi P10 về báo thức.
- Thống kê ẩn danh của Đêm 2 (`night_completed`, mã khu vực `n2.*`) chưa phát.

![Máy Ngân buổi tối, mục Tin nhắn](screenshots/night-two-evening.png)

## Kết quả với model thật — 27/11/2026

Nhánh #85 đã gộp phần cập nhật #83 (`47e0b56`), có model #121/#133 từ main `70ab9c5`. Mã kiểm `6700683`, Unity 6000.3.26f1 trên M2/macOS 15.7.4, dữ liệu cách ly, `-batchmode -nographics` và `THREEGIO12_REQUIRE_EVENING_MODELS=1`:

```text
NIGHT_TWO_EVENING_REAL_MODELS_CONFIRMED_NO_FIXTURES
NIGHT_TWO_EVENING_PASSED
exit 0
```

Bài kiểm chạy cả ba nhánh nêu trên bằng model thật. Sửa cách nhận model trong bài kiểm: trước đây điều kiện `activeInHierarchy` có thể tạo fixture trùng khi `Night2 props` đang tắt trong Đêm 1. Nay không xem nhóm cha tắt là thiếu model; chế độ nghiêm còn kiểm đường dẫn mesh nguồn FBX và không chạy nhánh tạo fixture. Lỗi khởi tạo bài kiểm được ghi và thoát lỗi, không để treo phiên Unity.

Đây là kiểm logic/hành động API và save/continue với model thật; có đặt vị trí nhân vật và seed lựa chọn Đêm 1. Không thay cho chơi tay từ đầu, raycast ở mọi đồ vật, ảnh/HUD đa tỉ lệ hoặc bản Windows phát hành. Log còn lỗi SearchDatabase/indexing có sẵn, không lỗi biên dịch.

Theo lịch #147, giữ #85 nháp và đích hiện tại cho tới khi #83 được gộp; sau đó mới đổi đích sang main và đưa đi duyệt. Không đóng #22 từ kết quả này.
