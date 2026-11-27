# Thống kê ẩn danh và gửi bằng chứng từ game

Phần trong game của #25 (thống kê ẩn danh) và #26 (gửi bằng chứng câu đố). Hợp đồng với máy chủ ở `docs/api/README.md`; tệp này chỉ ghi cách game làm theo hợp đồng đó.

## Thành phần

| Tệp | Việc |
|---|---|
| `Scripts/Online/OnlineServices.cs` | Đọc hồ sơ và hàng đợi, ghi sự kiện, gửi lô, rút đồng ý, gửi bằng chứng |
| `Scripts/Online/PlayerProfile.cs` | `installationId`, lựa chọn đồng ý, cờ đã đạt kết thúc "Về nhà", các kết thúc đã mở, mã cài đặt chờ xóa trên máy chủ, phiên đang mở |
| `Scripts/Online/TelemetryQueue.cs` | Hàng đợi trên đĩa (tối đa 5.000 sự kiện, bỏ sự kiện quá 30 ngày), chia lô (100 sự kiện, 256 KiB), quy tắc xử lý mã trả về |
| `Scripts/Online/Evidence.cs` | Tính SHA-256 của tệp (từ chối tệp trên 20 MiB), dựng thân yêu cầu, đổi phản hồi thành câu báo cho người chơi |
| `Scripts/Online/FilePicker.cs` | Hộp chọn tệp của Windows (`comdlg32`); bản macOS và Linux chưa có |
| `Scripts/Online/ApiConfig.cs` | Địa chỉ máy chủ |

`NightPersistence` tạo `OnlineServices` khi vào cảnh và nối với `NightOneDirector.TelemetryProduced`. Không cần sửa cảnh.

Dữ liệu nằm cạnh bản lưu trong `NightPersistence.DataRoot`: `profile.json` và `telemetry-queue.json`. "Xóa tiến độ" sinh `installationId` mới, xóa hàng đợi và cờ kết thúc, giữ lựa chọn đồng ý.

## Địa chỉ máy chủ

Game đọc theo thứ tự: biến môi trường `THREEGIO12_API_BASE`, rồi `Assets/StreamingAssets/api.json`, rồi `http://localhost:8080` (Docker Compose trên máy). Khi triển khai máy chủ (#32), sửa `baseUrl` trong `api.json` thành địa chỉ HTTPS trước khi dựng bản phát hành. Cài đặt Player "Allow downloads over HTTP" đặt ở mức "Allowed in development builds", nên bản phát hành chỉ gọi được HTTPS.

## Thống kê ẩn danh (#25)

- Lần đầu mở game trên Windows, menu đầu chỉ có hai mục "Gửi thống kê ẩn danh" và "Không gửi thống kê". Chưa chọn thì coi như từ chối.
- Cài đặt có dòng "Gửi thống kê ẩn danh". Tắt dòng này thì game xóa hàng đợi, ghi mã cài đặt vào danh sách chờ rồi gọi `DELETE /api/v1/telemetry/installations/{id}`, thử lại theo nhịp lùi khi mất mạng, quá thời gian hay lỗi máy chủ, chờ `Retry-After` khi gặp `429`. Máy chủ trả `204` hoặc `404` thì coi như đã xóa; các lỗi `4xx` còn lại (như `400`, `413`) không thể thành công khi gửi lại, nên game bỏ lệnh khỏi danh sách và ghi lỗi vào log. Bật lại dòng này khi lệnh xóa còn chờ thì game giữ nguyên lệnh xóa và dùng một mã cài đặt mới, nên dữ liệu đã gửi trước lúc rút vẫn bị xóa.
- Sự kiện: `session_started` khi bật đồng ý hoặc mở game; `session_ended` với `quit_app` khi thoát; `not_closed` ở lần mở sau nếu lần trước tắt đột ngột (thời lượng tính tới sự kiện cuối cùng của phiên đó); các sự kiện đêm từ `NightOneDirector`.
- Game gửi khi mở game, khi hàng đợi đủ 50 sự kiện và khi có `night_completed`, rồi gửi tiếp tới khi hết hàng đợi.
- Máy chủ chỉ nhận `platform = windows`, nên bản macOS và Linux không hỏi đồng ý và không gửi gì.

## Gửi bằng chứng (#26)

- Mục "Gửi bằng chứng cho công an" hiện ở menu đầu khi hồ sơ có cờ đã đạt kết thúc "Về nhà". Cờ được bật khi có sự kiện `ending_reached` với `endingId = home`, kể cả khi người chơi từ chối thống kê. Đêm 3 (#90) cần phát sự kiện này ở kết thúc 6.6.
- Chọn mục thì mở hộp chọn tệp. Game tính SHA-256 trên luồng phụ và chỉ gửi mã băm.
- Chấp nhận thì `unlockId` (`ending_red_thread_sandal`) được lưu vào `profile.json`. Phần chơi kết thúc 6.7 thuộc các đêm sau, ngoài phạm vi bản nộp Đêm 1-3; khi làm, nó đọc `PlayerProfile.HasUnlock(PlayerProfile.SandalUnlock)`.
- Câu báo cho từ chối, hết lượt hoặc gửi quá nhanh (`429`, phân biệt theo `title` của problem+json, kèm giờ thử lại và thêm ngày khi giờ đó không thuộc hôm nay), câu đố tắt (`404`) và mất mạng hiện ở dòng trạng thái của menu.

## Kiểm thử

`OnlineServicesRegression.Run` (Unity batch mode, cảnh trống, không cần Docker):

- Phần logic: sức chứa hàng đợi, bỏ sự kiện cũ, giới hạn lô 100 sự kiện và 256 KiB, dạng thân lô, các mã 200/400/413/429/5xx, nhịp lùi 30 giây gấp đôi tới 30 phút, mã trả về của lệnh xóa, SHA-256, tệp trên 20 MiB, các câu báo bằng chứng.
- Phần chạy thật với máy chủ giả `HttpListener`: chưa đồng ý thì không ghi; đồng ý thì gửi `session_started`; `night_completed` đẩy lô; `not_closed` sau lần tắt đột ngột; `503` giữ hàng đợi; rút đồng ý thì xóa hàng đợi và gọi `DELETE`; bật lại rồi rút lần nữa thì gửi đủ hai lệnh xóa theo thứ tự, lệnh bị `400` chỉ gửi một lần; bằng chứng bị từ chối, được chấp nhận (lưu xuống đĩa), lỗi máy chủ.

Đã chạy lại `SaveProgressRegression` (Write, Read, Menu) và đạt. Đã chụp menu trong Editor có giao diện: màn hỏi đồng ý, mục gửi bằng chứng kèm lời dặn, trang Cài đặt với 11 dòng (khoảng cách dòng tự co lại khi quá 10 dòng để gợi ý không rơi khỏi màn hình). Chưa kiểm với máy chủ thật qua Docker.
