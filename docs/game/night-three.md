# Đêm 3 trong cảnh tả thực (#22, đang làm)

Nhánh nối #83 → #85 → #90 trên Unity `6000.3.26f1`. Mở duy nhất `Assets/Scenes/Night01/NightOneRealistic.unity`. World/HUD Đêm 3 không dựng nền, đường, cửa, điện thoại hoặc NPC bằng primitive. Tương tác thêm lúc chạy nên không bị mất khi `NightOneRealisticSetup` dựng lại cảnh. Chưa đóng #22, chưa xác nhận bản phát hành chơi xuyên ba đêm.

## Đã chuyển

- HUD dùng HudKit cùng buổi sáng/buổi tối: font, nhãn, nút, keypad Máy tính, vùng cuộn, hình camera/thư viện. Không dùng GUIStyle/khung 1280×720 riêng. Hình ảnh gọi `HudKit.Image` để dùng cùng phép co giãn của HUD.
- Tuyến đổ xăng đi từ nhà xe, qua đầu hẻm tới `PARK_N3_GasPump`; tương tác theo `NPC_N3_GasAttendant`. Không dùng tọa độ của cửa hàng gia dụng hoặc vùng x=100 cũ. Giữ luật 0,5 km mỗi lượt và +11 km chỉ theo nhánh ngủ đã quy định.
- Khóa cửa điều khiển `NightRoomDoor` và cánh cửa thật. Điểm đứng khóa cửa tính từ cửa và checkpoint phòng; lưu/nạp giữ trạng thái khóa, chơi mới trả điều khiển cửa cho Đêm 1.
- Ba lô dùng vị trí ba lô hiện tại; điểm sạc dùng `Phone box and charger` có sẵn.
- Vật chứng camera dùng model `Ngan phone model` có sẵn ở Đêm 1, đặt trên mặt kệ Đêm 2. Không dùng khối màu cam. Các collider mặt máy tách theo submesh: phần ốp trong không cản lớp vải, mặt trước/thân máy vẫn chắn tia. Chỉ trúng phần vải mới là bằng chứng. Volume tương tác vô hình không che tia chụp.
- Ảnh PNG thật 512×288 nằm ở `captures/<UUID>.png`, tối đa 32 ảnh. Máy mình mới chụp/gửi được ảnh; ảnh thường vẫn vào thư viện nhưng không mở nhánh nhận mã. Camera ảnh riêng không dùng stack tay của camera người chơi. PNG chưa tự dọn khi chơi mới/xóa slot để không phá snapshot dự phòng.

## Luồng chơi

Sau “HẾT ĐÊM 2”, chọn bắt đầu Đêm 3. Tắt báo thức, đổ xăng, quay về đặt báo thức 13:00 trên máy mình rồi nằm nghỉ. Tỉnh lúc 21:00, xem tin thoại/lịch sử, trang cô Hòa và trang Mẹ. Xem bài đăng tìm con dẫn tới bước khóa cửa.

Chọn liên lạc từ máy mình hoặc máy Ngân; ảnh bằng chứng luôn gửi từ thư viện máy mình. Nghe cuộc gọi; chỉ khi đã xem bộ nhớ Máy tính ở Đêm 2 mới hỏi được mã. Mở Máy tính, nhập `472619` bằng keypad và bấm `=`. Pin giảm theo thao tác; xuống 9% phải tìm cáp và sạc trước khi chờ khuya. Nhánh R2/R3 chặn tin thoại 23:47 theo kịch bản. Ngủ không tự đánh dấu đã nghe tin thoại.

Đối chiếu nội dung với `docs/kich-ban/dem-3.md`. Không có truy đuổi Đêm 3 hoặc ảnh lộ mặt nhân vật chính.

## Còn thiếu và phụ thuộc

- #121 (model buổi sáng) và #129 (model buổi tối) chưa xong; bản phát hành hiện không thể đi liền từ Đêm 1 tới Đêm 3. Không bật lại khối thô để vượt điều kiện.
- Chưa có điểm tiếp nhận tả thực tại công an phường. Runtime nhận `Night3_PoliceDesk` hoặc `env_police_station` có renderer bật. Thiếu thì không tạo điểm nộp giả/không cho gọi API nộp máy để kết thúc. Marker `ENTRANCE_Police` không tự được coi là quầy tiếp nhận.
- #130 chưa gộp: chưa nối `EndingTelemetry("home")`/`("scapegoat")`, `PlayerProfile.SandalUnlock` và kết thúc 6.7. Làm sau khi #130 gộp theo yêu cầu của Quang. Kết thúc buổi chơi Đêm 3 hiện chưa chứng minh hoàn tất các kết thúc đó.
- Tin thoại, ảnh album/nhật ký, animation lật máy/khóa cửa và các nội dung được duyệt còn cần hoàn thiện. Một số màn hiện bản chữ mô tả.
- Chưa chơi tay, kiểm HUD các tỉ lệ, kiểm tuyến đi bộ bằng input hoặc build/chơi xuyên ba đêm. Không coi test có fixture là xác nhận asset thật.

## Kiểm tra

`NightThreeRegression.Write/Read` mở cảnh tả thực, hai tiến trình riêng cùng thư mục dữ liệu cách ly. Fixture model chỉ tồn tại trong Editor Play Mode khi asset chưa có; không lưu scene/build. Kết thúc Đêm 2 được seed để kiểm Đêm 3; chuỗi buổi sáng/buổi tối kiểm riêng.

```sh
THREEGIO12_DATA_ROOT=/tmp/3gio12-night3-review /path/to/Unity \
  -batchmode -projectPath /path/to/repo/game -executeMethod NightThreeRegression.Write \
  -logFile /tmp/night3-write.log
THREEGIO12_DATA_ROOT=/tmp/3gio12-night3-review /path/to/Unity \
  -batchmode -nographics -projectPath /path/to/repo/game -executeMethod NightThreeRegression.Read \
  -logFile /tmp/night3-read.log
```

Không dùng `-nographics` cho Write vì phải render PNG thật; không thêm `-quit`. Batchmode không chờ `ScreenCapture` của Game View, nhưng vẫn kiểm PNG từ camera: kích thước, ảnh không đồng màu, ảnh đổi theo góc ngắm, nhận sai/đúng vật, sai máy và file tồn tại. Read kiểm trạng thái/đường dẫn ảnh qua lần mở Unity mới, nhánh pin/mã/két/R2/R3/ngủ/thức/nộp máy và chơi mới. Marker thành công: `NIGHT_THREE_WRITE_PASSED`, `NIGHT_THREE_READ_PASSED`.
