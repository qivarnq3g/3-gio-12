# Đêm 3 trong cảnh tả thực (#22, đang làm)

Nhánh nối #83 → #85 → #90, đã cập nhật `main@7999a25` qua #85 (gồm #121/#133/#130/#138/#149/#153), trên Unity `6000.3.26f1`. Mở duy nhất `Assets/Scenes/Night01/NightOneRealistic.unity`. World/HUD Đêm 3 không dựng nền, đường, cửa, điện thoại hoặc NPC bằng primitive. Tương tác thêm lúc chạy nên không bị mất khi `NightOneRealisticSetup` dựng lại cảnh. Chưa đóng #22, chưa xác nhận bản phát hành chơi xuyên ba đêm.

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

- Model buổi sáng #121, buổi tối #133 (giải quyết #129), quầy #138 và online #130 đã vào main. Không còn dùng các PR này làm blocker asset/API. Vẫn cần chơi tay/build liền ba đêm; kiểm logic không thay cho việc đó.
- Điểm nộp máy chỉ gắn `Night3_PoliceDesk` thật, không dùng cả tòa nhà hoặc marker rỗng làm quầy. Nộp máy gọi móc kết thúc chung, phát `EndingTelemetry("scapegoat")` với `night=3`. Thao tác lại hoặc nạp lại không tự phát trùng sự kiện. Bài kiểm quầy không còn tạo cube thay thế, phải có mesh nguồn `Night3_PoliceDesk.fbx`.
- Móc `NightOneDirector.End("home")` phát sự kiện và mở quyền gửi bằng chứng qua OnlineServices, kể cả từ chối thống kê. `End("red_thread_sandal")` đọc `NightPersistence.Online.Profile.HasUnlock(PlayerProfile.SandalUnlock)` và từ chối khi chưa có cờ. Chỉ nhận bảy endingId theo hợp đồng máy chủ, chặn gọi lặp cùng kết thúc. Theo chốt phạm vi của Quang trên #90 ngày 28/11, PR này giữ hai kết quả hiện có: hết đêm hoặc Kẻ gánh tội; không bổ sung nhánh Về nhà/6.7. Móc chung SandalUnlock vẫn giữ để phần sau dùng. Không đổi “Hết Đêm 3” thành “Về nhà” để mở bằng chứng sớm.
- Tin thoại, ảnh album/nhật ký, animation lật máy/khóa cửa và các nội dung được duyệt còn cần hoàn thiện. Một số màn hiện bản chữ mô tả.
- Chưa chơi tay, kiểm HUD các tỉ lệ, kiểm tuyến đi bộ bằng input hoặc build/chơi xuyên ba đêm. Không coi test có fixture là xác nhận asset thật.

## Kiểm tra

`NightThreeRegression.Write/Read` mở cảnh tả thực, hai tiến trình riêng cùng thư mục dữ liệu cách ly. Fixture buổi sáng chỉ tồn tại trong Editor Play Mode khi asset chưa có (chế độ nghiêm từ chối fixture); quầy công an bắt buộc mesh FBX thật, không có nhánh tạo quầy giả. Không lưu fixture scene/build. Kết thúc Đêm 2 được seed để kiểm Đêm 3; chuỗi buổi sáng/buổi tối kiểm riêng.

```sh
THREEGIO12_DATA_ROOT=/tmp/3gio12-night3-review /path/to/Unity \
  -batchmode -projectPath /path/to/repo/game -executeMethod NightThreeRegression.Write \
  -logFile /tmp/night3-write.log
THREEGIO12_DATA_ROOT=/tmp/3gio12-night3-review /path/to/Unity \
  -batchmode -nographics -projectPath /path/to/repo/game -executeMethod NightThreeRegression.Read \
  -logFile /tmp/night3-read.log
```

Không dùng `-nographics` cho Write vì phải render PNG thật; không thêm `-quit`. Batchmode không chờ `ScreenCapture` của Game View, nhưng vẫn kiểm PNG từ camera: kích thước, ảnh không đồng màu, ảnh đổi theo góc ngắm, nhận sai/đúng vật, sai máy và file tồn tại. Read kiểm trạng thái/đường dẫn ảnh qua lần mở Unity mới, nhánh pin/mã/két/R2/R3/ngủ/thức/nộp máy và chơi mới. Marker thành công: `NIGHT_THREE_WRITE_PASSED`, `NIGHT_THREE_READ_PASSED`.

## Kết quả 25/11

Unity 6000.3.26f1 trên Apple M2/macOS 15.7.4, dữ liệu thử cách ly:

| Bài kiểm | Kết quả |
|---|---|
| Morning Write / Read, cảnh tả thực, hai tiến trình | `NIGHT_TWO_WRITE_PASSED`, `NIGHT_TWO_READ_PASSED` |
| Evening Run, cảnh tả thực | `NIGHT_TWO_EVENING_PASSED` |
| Three Write có đồ họa / Read không đồ họa, hai tiến trình | `NIGHT_THREE_WRITE_PASSED`, `NIGHT_THREE_READ_PASSED` |
| SaveProgress Write / Read, cảnh tả thực | `SAVE_WRITE_PROCESS_PASSED`, `SAVE_READ_PROCESS_PASSED` |
| NightOneSmoke với `THREEGIO12_NIGHT_SCENE` trỏ cảnh tả thực | `NIGHT_ONE_RUNTIME_VALIDATION_PASSED` |

Các tiến trình trên kết thúc mã 0. SaveProgress fixture đã cập nhật điều kiện xe tới nhà xe/giúp cô Liên của Đêm 1; bộ này không thay việc kiểm prologue. Lượt Menu không đồ họa không được tính là kiểm ảnh giao diện. Log còn lỗi SearchDatabase/indexing có sẵn ở nhiều bài; không dùng các kết quả trên để xác nhận asset đã được duyệt hoặc chơi xuyên ba đêm trên bản phát hành.

## Kiểm nối ending sau #130/#138 — 27/11/2026

`NightThreeEndingRegression.Run` mở cảnh chính, dùng model thật và dữ liệu cách ly, `-batchmode -nographics`. Bài kiểm seed các lựa chọn để kiểm riêng nhánh kết thúc, không mô phỏng một lượt chơi đầy đủ:

- Đi hết Đêm 3 không phát `home`/không mở quyền gửi bằng chứng.
- Nộp máy từ xa bị từ chối. Đứng phía tiếp dân, ngắm quầy và raycast theo `reach`/mask của người chơi phải trúng `MorningInteraction`; kích hoạt qua `HandleInput`.
- Nộp tại quầy phát đúng một `scapegoat`, night 3; save/continue không phát lại, không mở quyền gửi bằng chứng.
- Móc chung từ chối endingId không hợp lệ, chặn dép đỏ chưa mở, nhận `home` và ghi `homeReached` xuống profile dù đã từ chối thống kê; gọi lại không phát trùng.
- Khi profile có `PlayerProfile.SandalUnlock`, móc chung cho phát `red_thread_sandal`. Cờ trong bài kiểm được tạo trực tiếp để kiểm điều kiện; không dùng kết quả này thay cho gửi bằng chứng tới máy chủ thật.

Marker: `NIGHT_THREE_REAL_DESK_INTERACTION_PASSED`, `NIGHT_THREE_ENDING_REGRESSION_PASSED`.

Kết quả trên mã `d44bee7` (main `4f7c4dd`), Unity 6000.3.26f1/M2/macOS 15.7.4, năm tiến trình `-batchmode -nographics`, tất cả exit 0:

| Bài kiểm | Marker |
|---|---|
| NightThreeEndingRegression.Run | NIGHT_THREE_REAL_DESK_INTERACTION_PASSED; NIGHT_THREE_ENDING_REGRESSION_PASSED |
| NightOneSmoke.Run, trỏ cảnh tả thực | NIGHT_ONE_RUNTIME_VALIDATION_PASSED |
| NightTwoEveningRegression.Run | NIGHT_TWO_EVENING_PASSED |
| SaveProgressRegression.Write | SAVE_WRITE_PROCESS_PASSED |
| SaveProgressRegression.Read, tiến trình mới | SAVE_READ_PROCESS_PASSED |

Log còn lỗi SearchDatabase/indexing có sẵn; không có lỗi biên dịch. Năm kết quả này không kiểm ảnh camera mới, HUD đa tỉ lệ, input chơi tay hoặc server thật/Docker. Kết quả camera PNG ngày 25/11 ở trên là kết quả lịch sử, không được ghi thành đã chạy lại trên main 27/11. Giữ #90 nháp cho phần còn lại; không đóng #22.

## Camera và lưu/nạp Đêm 3 — kiểm tiếp 27/11/2026

Chạy thêm trên mã `16c4ef7`, Unity 6000.3.26f1/M2/macOS 15.7.4, dữ liệu cách ly và `THREEGIO12_REQUIRE_MORNING_MODELS=1`:

- `NightThreeRegression.Write`: batchmode **có GPU**, `NIGHT_THREE_REAL_POLICE_DESK_NO_FIXTURE`, `NIGHT_THREE_WRITE_PASSED`, exit 0.
- `NightThreeRegression.Read`: tiến trình mới không đồ họa, cùng dữ liệu; `NIGHT_THREE_REAL_POLICE_DESK_NO_FIXTURE`, `NIGHT_THREE_READ_PASSED`, exit 0.

Write tạo hai PNG thật ở hai góc ngắm khác nhau, kiểm 512×288, ảnh không đồng màu và nội dung khác nhau, phân biệt ảnh thường/mặt vải, từ chối gửi sai ảnh hoặc gửi từ máy Ngân. Read kiểm ảnh và trạng thái liên lạc qua lần mở Unity mới, pin/mã/két, R2/R3, ngủ/thức, nộp máy và chơi mới. Bài kiểm seed lựa chọn, vị trí và góc camera để kiểm riêng từng cơ chế; không thay cho đi/chụp bằng input người chơi. Cả hai lượt dùng model thật, không tạo quầy giả.

Như vậy lượt này đã kiểm camera trên nhánh tích hợp hiện tại; ghi chú “chưa chạy lại camera” ở kết quả năm bài kiểm phía trên là trạng thái trước lượt bổ sung này. Vẫn chưa xác nhận chơi tay liên tục N1→N2→N3 hoặc chất lượng ảnh/giao diện trong bản phát hành. Các thay đổi làm tròn float ở vật liệu do Editor sinh đã được phục hồi, không đưa vào PR.

## Kiểm buổi tối không fixture — bổ sung 27/11

Đã ghép sửa bài kiểm từ #85 vào nhánh Đêm 3, mã kiểm `d9479e6`. Chạy `NightTwoEveningRegression.Run` với `THREEGIO12_REQUIRE_EVENING_MODELS=1`, dữ liệu cách ly, Unity 6000.3.26f1/M2/macOS 15.7.4/nographics: `NIGHT_TWO_EVENING_REAL_MODELS_CONFIRMED_NO_FIXTURES`, `NIGHT_TWO_EVENING_PASSED`, exit 0. Đây là lượt kiểm riêng buổi tối với model FBX thật trên nhánh tích hợp Đêm 3; không dùng kết quả để nhận đã chơi tay toàn tuyến. Chi tiết ba nhánh và sửa điều kiện nhóm cha đang tắt ở `night-two-evening.md`.


## Đồng bộ và kiểm lại 28/11/2026

Gộp #85 `96c8581` (đã chứa main `7999a25`) vào #90; giữ các điều kiện đầu vào của cảnh tả thực và kiểm `night_started` mới của #149. Mang theo sửa tin nhắn dài/chữ UI tạm, bỏ fallback kệ/xe bánh mì thô và ảnh morning cũ. Mã kiểm mới: `45752cd`. Bổ sung chặn gọi đồng ý telemetry bằng mã trên Mac như #131, không thay đổi chính sách Windows-only.

Quang đã chốt trên #90 ngày 28/11: chỉ giữ hết đêm/Kẻ gánh tội, không thêm nhánh Về nhà hay 6.7 trong PR này. Móc SandalUnlock giữ nguyên.

[Ảnh thư viện Đêm 3 mới](screenshots/night-three.png) là Game View từ `NightThreeRegression.Write`, dùng model thật. Bài kiểm đặt vị trí/góc ngắm; ảnh và kết quả tự động không thay cho lượt chơi bằng tay/video xuyên N1→N2→N3.


Tất cả 8 lượt sau chạy trên `45752cd`, Unity 6000.3.26f1/M2/macOS 15.7.4, dữ liệu cách ly, đều **exit 0**:

| Bài kiểm | Marker |
|---|---|
| Morning.Write / Read, hai tiến trình | `NIGHT_TWO_WRITE_PASSED`, `NIGHT_TWO_READ_PASSED` |
| Evening.Run | `NIGHT_TWO_EVENING_PASSED` |
| NightThreeEnding.Run | `NIGHT_THREE_REAL_DESK_INTERACTION_PASSED`, `NIGHT_THREE_ENDING_REGRESSION_PASSED` |
| NightThree.Write / Read, hai tiến trình | `NIGHT_THREE_WRITE_PASSED`, `NIGHT_THREE_READ_PASSED` |
| SaveProgress.Write / Read, hai tiến trình | `SAVE_WRITE_PROCESS_PASSED`, `SAVE_READ_PROCESS_PASSED` |

Morning/Evening đều bật cờ bắt buộc model thật và ghi marker `NO_FIXTURES`; bài Đêm 3 bắt buộc quầy FBX thật. NightThree.Write chạy Editor có GPU, đã xem ảnh Game View cùng hai PNG camera; các bài còn lại nographics. SaveProgress trên nhánh này mở NightOneRealistic và seed điều kiện đã tới nhà/giúp cô Liên để kiểm riêng lưu trữ. Log vẫn có SearchDatabase/indexing khởi động như main. Các float vật liệu Editor tự làm tròn không đưa vào PR.

#83 đã gộp, #85 sẵn sàng chờ Thắng duyệt. #90 giữ đích #85 cho tới khi #85 gộp, sau đó mới đổi sang main. Chưa có video/chơi tay toàn tuyến; chưa đóng #22 hoặc khẳng định đủ điều kiện phát hành.
