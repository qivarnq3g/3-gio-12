# Test case cho 14 yêu cầu chức năng

Tài liệu này là bản nháp để Thắng xem lại và điền kết quả (task #8). Yêu cầu chức năng (YC) đánh số theo mục 1.4 của `docs/thiet-ke/thuyet-minh.md`. Mỗi YC có ít nhất một test case. Nơi nào đã có bài kiểm tự động trong repo thì ghi tên bài; chỗ đó chỉ nói bài kiểm tồn tại, không nói đã đạt. Cột "Kết quả" để trống cho tới khi có người chạy và ghi ngày chạy, phiên bản, người chạy.

## 1. Cách chạy

| Nhóm | Lệnh | Máy cần |
|---|---|---|
| Máy chủ | `dotnet test` trong `server/` | Docker Desktop đang chạy (test dùng PostgreSQL trong container) |
| Trang quản trị | `npm test` trong `admin/` | Node.js |
| Game, bài kiểm trong Unity | `Unity.exe -batchmode -projectPath game -executeMethod <Lớp>.<Hàm> -logFile <tệp>` | Máy chạy được Unity; dùng `THREEGIO12_DATA_ROOT` trỏ thư mục tạm để không đè dữ liệu lưu thật |
| Game, kiểm tay | Chơi bản dựng Windows | Bản chơi thử mới nhất |

Cặp bài `SaveProgressRegression.Write` và `.Read` chạy hai lần, mỗi lần một tiến trình Unity, cùng một thư mục dữ liệu thử.

## 2. Test case của game

| Mã | YC | Cách kiểm | Các bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|---|---|
| TC-01 | 1 | Tay | Đi và chạy trong phòng, đi vào tường và cửa đóng; cúi để đi dưới vật thấp rồi thử đứng khi còn bên dưới; nhìn vào cửa, bấm E để mở, đi qua rồi đóng | Không xuyên tường hay cửa đóng; còn dưới vật thấp thì giữ cúi; thấy gợi ý E khi nhìn cửa; đứng giữa lối cửa thì không đóng được | |
| TC-02 | 1 | Tự động | `PlayerTestSmoke.Run`; `CameraMotionRegression.Run` | Bài kiểm thoát mã 0 | |
| TC-03 | 1 | Tay và tự động | Bật bộ gõ tiếng Việt (EVKey, kiểu Telex), mở game, giữ W rồi nhấn thêm A, S hoặc D. Tự động: `VietnameseKeysRegression.Run` | Nhân vật đi đúng hướng, không kẹt phím. Ghi chú: issue #60 báo lỗi này và đang mở; kết quả test phải ghi rõ đã thử với bản dựng nào | |
| TC-04 | 2 | Tay | Chơi Đêm 1 từ đầu tới hết: nhặt vật chứng, đi qua các khu vực, qua các checkpoint | Hoàn thành được Đêm 1; vật chứng đã nhặt xuất hiện trong sổ tay; khu vực mới mở đúng lúc theo kịch bản | |
| TC-05 | 2 | Tự động | `NightOneSmoke.Run`, `NightOneWalkSmoke.Run` | Thoát mã 0 (thành công ghi `NIGHT_ONE_RUNTIME_VALIDATION_PASSED`) | |
| TC-06 | 3 | Tay | Bấm 1 để mở điện thoại thứ nhất, 2 để chuyển sang điện thoại thứ hai; đọc tin nhắn, xem ảnh, nghe ghi âm; trả lời bằng một lựa chọn | Tin nhắn hiện đúng lô và đúng giờ; lựa chọn được ghi nhận; điện thoại đang mở thì nhân vật và camera đứng yên | |
| TC-07 | 3 | Tay | Chơi cùng một đoạn hai lần, mỗi lần chọn khác nhau ở một câu trả lời quan trọng | Diễn biến hoặc câu thoại sau đó khác nhau đúng theo kịch bản | |
| TC-08 | 3 | Tự động | `BatchBReplyRegression.Run` | Thoát mã 0 | |
| TC-09 | 4 | Tay | Làm theo 5 bước "Chơi thử trước khi duyệt" trong `docs/game/stealth-and-chase.md`: bị phát hiện, chạy tới chỗ nấp, đứng yên ngoài tầm nhìn 8 giây, bị bắt đủ ba lần | Thoát được khi đứng yên đủ 8 giây ngoài tầm nhìn; bị bắt lần 1 và 2 thì về chỗ nấp gần nhất kèm hình phạt; lần 3 reset về đầu hẻm; không bị bắt sau khi đã đặt máy | |
| TC-10 | 4 | Tự động | `StealthRegression.Run` | Thoát mã 0 | |
| TC-11 | 5 | Tay | Chơi mới, đi tới phòng 7 và nhắn tin, đợi hơn 2 giây, thoát; mở lại và chọn "Tiếp tục" | Vị trí, góc nhìn, tin nhắn, đồng hồ giữ nguyên như lúc thoát | |
| TC-12 | 5 | Tay | Chọn "Chơi lại từ checkpoint"; chọn "Chơi mới" và xác nhận; chọn "Xóa tiến độ" và xác nhận rồi mở lại | Chơi lại về snapshot của checkpoint; chơi mới bắt đầu lại Đêm 1; sau khi xóa không còn mục "Tiếp tục" nhưng cài đặt vẫn còn | |
| TC-13 | 5 | Tự động | `SaveProgressRegression.Write` rồi `SaveProgressRegression.Read` | Log có `SAVE_STORAGE_CASES_PASSED`, `SAVE_WRITE_PROCESS_PASSED`, `SAVE_READ_PROCESS_PASSED` | |
| TC-14 | 5 | Tay, trên dữ liệu thử | Làm hỏng tệp lưu chính, mở game; làm hỏng cả tệp dự phòng, mở lại | Hỏng tệp chính thì dùng bản dự phòng; hỏng cả hai thì báo lỗi và cho chọn chơi mới. Không thử trên dữ liệu lưu thật | |
| TC-15 | 6 | Tay | Chạy game lần đầu trên máy chưa có dữ liệu; chọn từ chối gửi thống kê; chơi một lúc; xem máy chủ | Game hỏi đồng ý ở lần chạy đầu; từ chối thì máy chủ không nhận sự kiện nào của máy đó | |
| TC-16 | 6 | Tay, cần máy chủ chạy bằng Docker | Đồng ý gửi, chơi qua một checkpoint, xem sự kiện trên trang quản trị; rút đồng ý trong Cài đặt | Sau khi rút, hàng đợi trên máy bị xóa và máy chủ xóa mọi sự kiện của mã cài đặt đó | |
| TC-17 | 6 | Tự động | `TelemetryDeleteTests` (máy chủ); `OnlineServicesRegression.Run` (game, có sau khi PR #130 gộp) | Thoát mã 0 | |
| TC-18 | 7 | Tay, cần máy chủ chạy bằng Docker | Đạt kết thúc "Về nhà", mở mục "Gửi bằng chứng", chọn tệp đúng; lặp lại với tệp sai | Tệp đúng thì nhận mã mở khóa và mở kết thúc thật; tệp sai báo số lượt còn lại; tệp không rời khỏi máy (chỉ gửi mã băm) | |
| TC-19 | 8 | Tay | Trong Cài đặt đổi âm lượng, độ nhạy chuột (0,02 đến 0,30), chất lượng đồ họa, bật rồi tắt cảnh báo nội dung; lưu; mở lại game | Âm thanh và độ nhạy đổi ngay; thiết lập còn nguyên sau khi mở lại; xóa tiến độ không xóa cài đặt | |
| TC-20 | 8 | Tự động | `AntialiasingSettingRegression.Run`; phần cài đặt trong `SaveProgressRegression` | Thoát mã 0 | |

: Test case của game (YC 1 đến 8)

## 3. Test case của máy chủ và trang quản trị

| Mã | YC | Cách kiểm | Các bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|---|---|
| TC-21 | 9 | Tự động | `AdminAuthTests`, `AdminAccountsTests` (máy chủ); `npm test` (`App.test.tsx`) | Mọi bài kiểm đạt | |
| TC-22 | 9 | Tay | Đăng nhập đúng; đăng nhập sai mật khẩu 5 lần liên tiếp rồi thử mật khẩu đúng; tạo tài khoản mới; thử khóa hoặc xóa chính mình | Sai 5 lần thì bị chặn 15 phút kể cả khi nhập đúng; thông báo lỗi không lộ email nào có tài khoản; không khóa hoặc xóa được chính mình | |
| TC-23 | 10 | Tự động | `AdminDashboardTests.Dashboard_returns_metrics_matching_ingested_events` | Số liệu dashboard khớp sự kiện đã gửi | |
| TC-24 | 10 | Tay | Gửi vài sự kiện từ game, mở dashboard, đổi khoảng thời gian | Hiện số người chơi, số phiên, thời gian chơi, số người bắt đầu và hoàn thành từng đêm, kết thúc, nơi bị bắt; lọc theo thời gian đúng | |
| TC-25 | 11 | Tự động | `AdminDashboardTests.Export_summary_returns_csv_with_utf8_bom`, `Export_events_returns_csv_with_utf8_bom`, `Export_unknown_dataset_answers_400` | Mọi bài kiểm đạt | |
| TC-26 | 11 | Tay | Xuất CSV số liệu tổng hợp và CSV sự kiện, mở bằng Excel | Chữ tiếng Việt hiển thị đúng; số dòng khớp dashboard | |
| TC-27 | 12 | Tự động | `AdminPuzzlesTests` | Mọi bài kiểm đạt | |
| TC-28 | 12 | Tay | Thêm một mã băm hợp lệ rồi gửi bằng chứng từ game; xóa mã băm rồi gửi lại; hạ giới hạn lượt gửi; xem lịch sử gửi | Mã vừa thêm được chấp nhận, xóa rồi thì bị từ chối; giới hạn mới áp dụng cho lần gửi kế tiếp; lịch sử hiện mới nhất trước | |
| TC-29 | 13 | Tự động | `TelemetryBatchTests`, `TelemetryRateLimitTests` | Mọi bài kiểm đạt (gồm: lô tối đa 100 sự kiện, sự kiện trùng chỉ lưu một lần, sự kiện sai bị loại riêng, thân yêu cầu trên 256 KiB trả 413) | |
| TC-30 | 13 | Tay | Tắt mạng, chơi một đoạn, bật mạng lại | Sự kiện nằm trong hàng đợi và được gửi sau khi có mạng; việc chơi không bị ảnh hưởng khi mất mạng | |
| TC-31 | 13 | Tay, xem cơ sở dữ liệu | Mở bảng `telemetry_events` sau khi gửi thử | Các cột đúng như `docs/thiet-ke/database-design.md`; không có tên, email hay địa chỉ IP của người chơi | |
| TC-32 | 14 | Tự động | `PuzzleSubmissionTests` | Mọi bài kiểm đạt (gồm: đúng thì nhận mã mở khóa và không tốn lượt, sai thì tốn một lượt, hết lượt thì trả 429 kể cả khi tệp đúng) | |
| TC-33 | 14 | Tay | Gửi bằng chứng sai 5 lần liên tiếp rồi gửi bằng chứng đúng | Lần thứ sáu bị chặn và báo thời gian chờ; mặc định 5 lần sai trong 60 phút; quản trị viên đổi được | |

: Test case của máy chủ và trang quản trị (YC 9 đến 14)

## 4. Độ phủ

Bảng 4.1 cho biết mỗi YC có bao nhiêu test case, và loại kiểm.

| YC | Số test case | Có bài tự động | Có kiểm tay |
|---|---|---|---|
| 1 | 3 | Có | Có |
| 2 | 2 | Có | Có |
| 3 | 3 | Có | Có |
| 4 | 2 | Có | Có |
| 5 | 4 | Có | Có |
| 6 | 3 | Có | Có |
| 7 | 1 | Chưa có bài riêng cho phần game; phần máy chủ nằm ở TC-32 | Có |
| 8 | 2 | Có | Có |
| 9 | 2 | Có | Có |
| 10 | 2 | Có | Có |
| 11 | 2 | Có | Có |
| 12 | 2 | Có | Có |
| 13 | 3 | Có | Có |
| 14 | 2 | Có | Có |

: Độ phủ test case theo yêu cầu chức năng

## 5. Điều chưa kiểm

- Chưa chạy bài kiểm nào khi soạn tài liệu này; tên bài kiểm lấy từ mã trong repo ngày 26/11/2026.
- `OnlineServicesRegression` chỉ có trên nhánh của PR #130, chưa có trên `main`.
- Chưa kiểm xem `BatchBReplyRegression`, `NightOneWalkSmoke` và `CameraMotionRegression` có đúng là bài phủ YC 3, 2 và 1 hay không bằng cách đọc nội dung từng bài; gán theo tên tệp. Thắng đối chiếu lại khi chạy.
- Cột "Kết quả" cần người chạy ghi ngày, phiên bản và máy.
