# Hợp đồng API giữa game và máy chủ

Nguồn duy nhất là [`openapi.yaml`](openapi.yaml) (OpenAPI 3.1). Tệp này giải thích các quy tắc mà OpenAPI không diễn tả được. Khi hai bên lệch nhau, sửa cả hai trong cùng một pull request.

## Các API

| Phương thức | Đường dẫn | Dùng khi | Yêu cầu chức năng |
|---|---|---|---|
| `POST` | `/api/v1/telemetry/batches` | Gửi lô sự kiện thống kê | 6, 13 |
| `DELETE` | `/api/v1/telemetry/installations/{installationId}` | Người chơi rút lại đồng ý | 6, 13 |
| `POST` | `/api/v1/puzzles/{puzzleId}/submissions` | Gửi bằng chứng của câu đố ngoài game | 7, 14 |
| `GET` | `/health` | Kiểm máy chủ còn sống | |

API của trang quản trị, mọi đường dẫn bắt đầu bằng `/api/admin`:

| Phương thức | Đường dẫn | Dùng khi | Yêu cầu chức năng |
|---|---|---|---|
| `POST` | `/auth/login`, `/auth/logout` | Đăng nhập, đăng xuất | 9 |
| `GET` | `/auth/me` | Biết đã đăng nhập chưa, là ai | 9 |
| `POST` | `/auth/password` | Đổi mật khẩu của chính mình | 9 |
| `GET`, `POST` | `/accounts` | Xem, tạo tài khoản quản trị viên | 9 |
| `PATCH`, `DELETE` | `/accounts/{accountId}` | Đổi tên, khóa, mở khóa, xóa tài khoản | 9 |
| `POST` | `/accounts/{accountId}/password` | Đặt lại mật khẩu cho người khác | 9 |
| `GET` | `/dashboard` | Số liệu dashboard theo khoảng thời gian | 10 |
| `GET` | `/exports/{dataset}` | Xuất CSV số liệu tổng hợp hoặc toàn bộ sự kiện | 11 |
| `GET` | `/puzzles`, `/puzzles/{puzzleId}` | Xem cấu hình câu đố | 12 |
| `PATCH` | `/puzzles/{puzzleId}` | Bật, tắt, đổi giới hạn lượt gửi | 12 |
| `POST`, `DELETE` | `/puzzles/{puzzleId}/solutions` | Thêm, bỏ mã băm hợp lệ | 12 |
| `GET` | `/puzzles/{puzzleId}/submissions` | Lịch sử gửi | 12 |

Lớp và bảng dữ liệu phía máy chủ ở `docs/thiet-ke/luoc-do-lop-may-chu.md`.

## Mã cài đặt và quyền riêng tư

- Lần chạy đầu, game sinh một UUID phiên bản 4 làm `installationId` và lưu trong tệp lưu trên máy. Khi người chơi xóa dữ liệu lưu, game sinh mã mới.
- Game không gửi tên người chơi nhập lúc bắt đầu, tên máy, địa chỉ email hay bất kỳ thông tin định danh nào.
- Máy chủ không lưu địa chỉ IP hay User-Agent. Địa chỉ IP chỉ dùng trong bộ nhớ để giới hạn tần suất.
- Game không có khóa API. Mọi thứ đóng trong bản build đều lấy ra được, nên một khóa bí mật trong game không bảo vệ được gì.

## Thống kê ẩn danh

### Đồng ý

- Lần chạy đầu, game hỏi người chơi có đồng ý gửi thống kê ẩn danh không. Chưa trả lời thì coi như từ chối.
- Từ chối: game không ghi sự kiện vào hàng đợi và không gọi API thống kê nào.
- Đổi từ đồng ý sang từ chối trong Cài đặt: game xóa hàng đợi trên máy, gọi `DELETE /api/v1/telemetry/installations/{installationId}` (gọi lại ở lần có mạng sau nếu lỗi), rồi dừng gửi.
- Máy chủ không lưu bản ghi đồng ý; lô gửi tới đã ngầm nghĩa là người chơi đồng ý.

### Hàng đợi và cách gửi

- Mỗi sự kiện được gán `eventId` (UUID) ngay khi xảy ra và ghi vào hàng đợi trên đĩa, để tắt game giữa chừng không mất.
- Game gửi khi hàng đợi đủ 50 sự kiện, khi hết một đêm, và khi mở game. Mỗi lô tối đa 100 sự kiện và 256 KiB.
- Hàng đợi giữ tối đa 5.000 sự kiện; quá thì bỏ sự kiện cũ nhất. Sự kiện cũ hơn 30 ngày bị máy chủ loại, nên game cũng bỏ chúng trước khi gửi.
- Gửi lại cùng một lô là an toàn: máy chủ bỏ qua `eventId` đã nhận.

| Máy chủ trả | Game làm |
|---|---|
| `200` | Xóa cả lô khỏi hàng đợi, kể cả các sự kiện trong `rejected` (gửi lại cũng bị loại tiếp) |
| `400` | Phần vỏ của lô sai (sự kiện sai lẻ thì nằm trong `rejected`, không gây 400). Xóa cả lô và ghi log; đây là lỗi của game |
| `413` | Chia đôi lô rồi gửi lại |
| `429` | Chờ đúng số giây trong `Retry-After` |
| `5xx` hoặc mất mạng | Giữ lô, thử lại sau 30 giây, gấp đôi mỗi lần, tối đa 30 phút |

### Các loại sự kiện

| `type` | Khi nào | Dashboard dùng để |
|---|---|---|
| `session_started` | Mở game | Số người chơi |
| `session_ended` | Thoát về menu hoặc tắt game; `not_closed` gửi ở lần mở sau nếu lần trước tắt đột ngột | Thời gian chơi, vị trí bỏ game (`night`, `areaId`) |
| `night_started` | Bắt đầu một đêm | Số người bắt đầu từng đêm (chỉ lần đầu, `isReplay = false`) |
| `night_completed` | Hết một đêm | Số người hoàn thành từng đêm, thời gian từng đêm (chỉ lần đầu, `isReplay = false`) |
| `player_caught` | Bị bắt, quay về điểm lưu | Vị trí bị bắt |
| `choice_made` | Chọn một lựa chọn trong mục 5.1 của kịch bản | Lựa chọn phổ biến |
| `ending_reached` | Tới một kết thúc trong mục 6 của kịch bản | Tỉ lệ từng kết thúc |

Mã khu vực (`areaId`) và mã điểm lưu (`checkpointId`) do người dựng màn đặt, viết thường, ví dụ `n1.parking`. Danh sách mã sẽ ghi cùng thiết kế màn chơi để dashboard hiện tên dễ đọc.

## Bằng chứng của câu đố ngoài game

Câu đố duy nhất theo kịch bản bản 5 có `puzzleId` là `red-thread-sandal`: người chơi gửi ảnh chiếc dép giải mã được ở bước 8.5 để mở kết thúc 6.7.

### Phía game

- Mục "Gửi bằng chứng cho công an" chỉ hiện sau khi đạt kết thúc "Về nhà" (6.6). Máy chủ không kiểm được điều này.
- Người chơi chọn một tệp trên máy. Game đọc nguyên các byte của tệp, tính SHA-256, rồi chỉ gửi mã băm; tệp không rời khỏi máy.
- Game từ chối tệp lớn hơn 20 MiB trước khi tính.
- Mã băm phải đúng của tệp gốc. Chụp màn hình hay lưu lại ảnh sẽ ra mã khác, nên màn hình gửi cần nhắc người chơi chọn đúng tệp đã giải mã.

### Kết quả

| Máy chủ trả | Nghĩa | Game làm |
|---|---|---|
| `200`, `accepted` | Đúng | Lưu `unlockId` vào tệp lưu, mở kết thúc 6.7 |
| `200`, `rejected` | Sai, còn `attemptsRemaining` lượt | Báo sai và số lượt còn lại |
| `404` | Câu đố không có hoặc đã bị tắt | Báo tạm thời không gửi được |
| `429` | Hết lượt hoặc gửi quá nhanh | Báo thời điểm thử lại từ `Retry-After` |
| Mất mạng | | Báo cần mạng để gửi; không lưu gì |

### Giới hạn số lần thử

- Mỗi `installationId` có một số lượt gửi sai trong một cửa sổ thời gian. Mặc định 5 lượt trong 60 phút; quản trị viên đổi được trên trang quản trị (yêu cầu 12).
- Hết lượt thì mọi lần gửi trong cửa sổ đều nhận `429`, kể cả bằng chứng đúng.
- Gửi đúng không trừ lượt. Gửi lại bằng chứng đúng vẫn nhận `accepted`.
- Thêm một giới hạn theo địa chỉ IP (20 lần mỗi phút) để chặn việc đổi `installationId` liên tục.
- Máy chủ lưu mọi lần gửi (mã cài đặt, mã băm đã gửi, kết quả, thời gian) để trang quản trị xem lịch sử. Lần gửi bị chặn vì hết lượt được lưu với kết quả `blocked`; lần bị giới hạn theo địa chỉ IP thì không lưu.
- Cửa sổ thời gian là cửa sổ trượt: số lượt sai là số lần `rejected` trong `attemptWindowMinutes` phút gần nhất. `windowResetsAt` là lúc lượt sai mới nhất trong cửa sổ hết hạn; chưa có lượt sai nào thì bằng giờ hiện tại. Khi bị chặn, `Retry-After` tính tới lúc lượt sai cũ nhất trong cửa sổ hết hạn, vì lúc đó có lại một lượt.

### Giới hạn đã chấp nhận

Game chạy offline nên người chơi sửa tệp lưu vẫn tự mở được kết thúc 6.7. Hợp đồng này không chống gian lận phía máy; nó chỉ bảo đảm lời giải nằm ở máy chủ, không nằm trong bản build để bị đọc ra.

## Trang quản trị

- Trang quản trị và API cùng tên miền, nên đăng nhập dùng cookie phiên (`HttpOnly`, `Secure`, `SameSite=Strict`), không dùng token. Cookie hết hạn sau 8 giờ không hoạt động.
- Không có cookie hợp lệ thì API trả `401`, không chuyển hướng sang trang đăng nhập; trang quản trị tự đưa người dùng về màn hình đăng nhập.
- Mọi quản trị viên có cùng quyền. Đồ án không chia vai trò.
- Tài khoản đầu tiên tạo lúc máy chủ khởi động nếu chưa có tài khoản nào, lấy email và mật khẩu từ biến môi trường `Admin__Email`, `Admin__Password`. Không ghi mật khẩu thật vào repo.
- Luôn còn ít nhất một tài khoản hoạt động: không khóa, không xóa được tài khoản hoạt động cuối cùng, và không tự khóa hay tự xóa chính mình.
- Khoảng thời gian `from`, `to` là nửa mở: tính `from`, không tính `to`. Trang quản trị gửi mốc nửa đêm theo giờ Việt Nam, ví dụ `2026-11-01T00:00:00+07:00`; máy chủ đổi sang UTC trước khi lọc.
- Dashboard và CSV lọc theo `occurredAt` (giờ trên máy người chơi). Máy chủ đã loại sự kiện lệch quá xa giờ máy chủ, nên sai số chấp nhận được.

## Quy ước chung

- JSON dùng camelCase; giá trị liệt kê dùng snake_case.
- Thời gian theo RFC 3339, giờ UTC, ví dụ `2026-11-24T14:02:11Z`.
- Dashboard và CSV lọc theo giờ máy chủ nhận, không theo `occurredAt`. Game chạy offline nên một lô có thể gửi muộn tới 30 ngày; sự kiện khi đó được tính vào ngày gửi lên, đổi lại số liệu không phụ thuộc đồng hồ máy người chơi.
- Lỗi trả theo RFC 9457 (problem details), dạng mặc định của ASP.NET Core.
- Đổi không tương thích (bỏ trường, đổi nghĩa) thì tăng lên `/api/v2`. Thêm trường tùy chọn hoặc thêm loại sự kiện thì giữ `v1`; máy chủ cũ loại sự kiện lạ bằng lý do `unknown_type`.

## Làm song song khi máy chủ chưa xong

Phía game có thể chạy máy chủ giả sinh từ tệp OpenAPI, trả đúng các ví dụ trong tệp:

```bash
npx @stoplight/prism-cli@5 mock docs/api/openapi.yaml --port 8080
```

Kiểm tệp OpenAPI sau khi sửa:

```bash
npx @redocly/cli@1 lint docs/api/openapi.yaml
```
