# Duyệt giao diện điện thoại #161 — 28/11/2026

Bản ghép PR #161 với #90 `8c4f070`, Unity 6000.3.26f1 / Apple M2 / RAM 8 GB / macOS 15.7.4. Thư mục dữ liệu cách ly, cảnh NightOneRealistic. Giữ giao diện mới của Quang khi giải xung đột với sửa chat #85; kiểm chiều cao chat theo đúng bề rộng chữ và dành chỗ cho phụ đề phía trên hàng phím.

Các ảnh là **Game View từ bài kiểm có seed trạng thái**, không phải lượt chơi tay xuyên ba đêm. N1 dùng PhonePrototype cho máy Ngân; N2/N3 là các trang HudKit của PR #161. Không mô tả chúng là cùng một giao diện PhonePrototype.

| Màn | Ảnh |
|---|---|
| Máy Ngân Đêm 1 — đối chiếu | [n1-phone](n1-phone.png) |
| Báo thức Đêm 2, chỉ một hành động tắt | [n2-alarm](n2-alarm.png) |
| Máy Ngân Đêm 2 | [n2-ngan](n2-ngan.png) |
| Máy của bạn Đêm 2 | [n2-own](n2-own.png) |
| Báo thức Đêm 3 | [n3-alarm](n3-alarm.png) |
| Dãy số: bỏ chặn và tin thoại thành các dòng riêng | [n3-stranger](n3-stranger.png) |
| Trang cá nhân sau đổi mục bằng D | [n3-profile](n3-profile.png) |

Bài `NightPhoneUiRegression.Run` dùng bàn phím Input System riêng, gửi Enter qua HUD để tắt báo thức N2/N3 và bỏ chặn dãy số, gửi D để đổi sang Trang cá nhân. Không gọi trực tiếp hàm hành động thay cho các lần nhấn đang kiểm. Các điều kiện vào đêm/nhận máy/tin thoại được tạo trước để kiểm riêng giao diện.

Lượt kiểm logic trên `e791c0d`: Morning.Write/Read, Evening.Run, NightThreeEnding.Run, NightThree.Write/Read và NightOneSmoke.Run đều PASS/exit 0. Morning/Evening bật strict model thật, không fixture; quầy FBX thật. Write Đêm 3 có GPU và PNG camera thật; các bài khác nographics. Không suy ra telemetry transport Windows, server thật hoặc bản phát hành đã được kiểm từ lượt Mac này. Log vẫn có lỗi lập chỉ mục SearchDatabase lúc khởi động như main.

Lượt UI cuối trên `34f61d8` đạt `NIGHT_PHONE_UI_KEYBOARD_CAPTURE_PASSED`, exit 0. Đã xem ảnh sau sửa: phụ đề và hàng phím không đè nhau; D đổi sang Trang cá nhân. Tổng cộng 7 lượt hồi quy logic và 1 lượt UI có đồ họa đạt. Những phụ đề còn từ cảnh trước trong ảnh là do bài kiểm seed/chuyển đêm nhanh, không được dùng làm bằng chứng chơi tự nhiên.

## Kiểm lỗi bài kiểm Windows sau khi #90 gộp

Quang báo `NightPhoneUiRegression.Run` thất bại trên Windows ở bước Enter tắt báo thức N2. Windows có `PhysicalKeyboardHook`: sau mỗi input update, `KeepCurrent()` chọn lại thiết bị `PhysicalKeyboard`. Bài kiểm cũ gửi phím vào một Keyboard giả khác, trong khi HUD đọc `Keyboard.current`, nên sự kiện không tới thiết bị HUD đang đọc.

Tái hiện trên M2 bằng `RunWithCompetingKeyboard`: một thiết bị khác được chọn lại trong `InputSystem.onAfterUpdate`, tương tự quyền sở hữu current của hook Windows. Trên `bb8b064` trước sửa, bài này FAIL với đúng `Enter did not dismiss N2 alarm`, exit 1. Đây là tái hiện xung đột thiết bị, không phải chạy Windows giả lập.

Bản sửa chỉ ở Editor: trên Windows mượn `PhysicalKeyboardHook.Device` nếu có; các nền tảng khác tạo bàn phím kiểm riêng. Gửi trạng thái phím trong `onBeforeUpdate` sau callback của hook, giữ đúng down/up qua khung hình. Không tháo hook hoặc xóa thiết bị Windows được mượn. Chế độ cạnh tranh chọn thiết bị fixture sở hữu current trên mọi nền tảng. Gameplay, phím và thời gian báo thức giữ nguyên.

Trên mã `79983cf`, M2/Unity 6000.3.26f1, lượt cạnh tranh đạt `PHONE_UI_REVIEW_INPUT_DEVICE ReviewCurrentKeyboardOwner owned=False`, `NIGHT_PHONE_UI_KEYBOARD_CAPTURE_PASSED`, exit 0. Thay đổi tiếp `4e6daec` chỉ sửa chọn fixture trong nhánh biên dịch Windows, không đổi đường chạy Mac. Cần Thắng/Quang chạy lại cả hai entrypoint trên Windows, có đồ họa và hai DATA_ROOT riêng; chưa xác nhận đường bàn phím vật lý/IME Windows bằng lượt M2 này.

Lượt `NightPhoneUiRegression.Run` thông thường trên `79983cf` cũng PASS/exit 0 (`PhoneUiReviewKeyboard owned=True`). Hai lượt dùng thư mục dữ liệu mới riêng biệt. Các kiểm trước sửa không bị sửa kết quả hoặc đổi thành PASS; log FAIL được giữ riêng để đối chiếu.
