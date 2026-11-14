# Lược đồ lớp phần máy chủ

Tài liệu cho task #12, một trong ba phần bắt buộc của thuyết minh thiết kế nộp ngày 16/11/2026. Tài liệu đi tiếp từ `kien-truc-he-thong.md` (task #10): ở đó là các thành phần và luồng dữ liệu, ở đây là lớp, dịch vụ và bảng dữ liệu bên trong máy chủ API, cùng cấu trúc trang quản trị.

Hợp đồng API (`docs/api/openapi.yaml`, `docs/api/README.md`) là nguồn đúng. Tên trường, mã lỗi và giới hạn ở đây phải khớp hợp đồng; lệch thì sửa tài liệu này.

Lược đồ lớp vẽ bằng draw.io: bản vẽ gốc ở `lop-may-chu/luoc-do-lop-may-chu.drawio`, mỗi trang ứng với một hình bên dưới; sửa trong draw.io rồi xuất lại ảnh PNG cùng tên trong `lop-may-chu/` (Xuất thành, PNG, tỉ lệ 200%). Lược đồ cơ sở dữ liệu ở `database-design.md`, cấu trúc trang quản trị ở `admin-ui-design.md`.

## 1. Tổ chức mã nguồn

Máy chủ là một dự án ASP.NET Core (`server/src/ThreeTwelve.Api`), chia theo lớp như mục 2.2 của tài liệu kiến trúc:

| Thư mục | Nội dung | Phụ thuộc vào |
|---|---|---|
| `Controllers/` | Nhận yêu cầu HTTP, kiểm dữ liệu vào, gọi dịch vụ, đổi kết quả thành mã HTTP | `Services/`, `Contracts/` |
| `Contracts/` | Lớp dữ liệu vào ra (DTO, data transfer object) đúng theo schema trong `openapi.yaml` | Không |
| `Services/` | Quy tắc nghiệp vụ | `Domain/`, `Data/`, `Contracts/` |
| `Domain/` | Lớp thực thể và kiểu liệt kê | Không |
| `Data/` | `AppDbContext`, cấu hình bảng, migration | `Domain/` |
| `Auth/` | Cấu hình cookie, tạo tài khoản quản trị đầu tiên | `Domain/`, `Data/` |
| `Health/` | Định dạng phản hồi `/health` (đã có từ task #18) | Không |

: Các thư mục mã nguồn máy chủ

Controller không truy cập `AppDbContext` trực tiếp; mọi truy vấn đi qua dịch vụ. Lớp thực thể không bao giờ được trả thẳng ra API; controller luôn đổi sang DTO, để đổi bảng không làm đổi hợp đồng.

Giờ hiện tại lấy từ `TimeProvider` của .NET, không gọi `DateTime.UtcNow`, để test đặt được giờ giả khi kiểm cửa sổ lượt gửi và khoảng thời gian dashboard [@ms-timeprovider].

### 1.1. Sơ đồ tổng quát các lớp máy chủ

![Lược đồ lớp: Sơ đồ tổng quát](lop-may-chu/overview.png)

Hình tổng quát xếp các lớp theo thư mục ở bảng trên và chỉ ghi tên lớp. Mỗi controller gọi một giao diện dịch vụ, riêng `AdminDashboardController` dùng cả `IDashboardService` và `ICsvExportService`. `AdminAuthController` không có dịch vụ riêng mà dùng thẳng `SignInManager` và `UserManager` của ASP.NET Core Identity, nên mũi tên nét đứt đi từ khung Controllers tới khung Auth; `IAdminAccountService` cũng dùng `UserManager`. Các lớp cài đặt dịch vụ (tên bỏ chữ `I`) đọc ghi dữ liệu qua `AppDbContext`, lớp này ánh xạ các lớp thực thể thành bảng. DTO trong `Contracts/`, kiểu liệt kê và các lớp phụ (`TelemetryEventValidator`, `SubmissionOutcome`, `DateRange`, `SubmissionQuery`) được lược bỏ; chi tiết ở mục 2 và 3.

## 2. Lớp thực thể

Lớp thực thể chia hai nhóm không nối với nhau: sự kiện thống kê và câu đố cùng quản trị viên.

### 2.1. Sự kiện thống kê

![Lược đồ lớp: Sự kiện thống kê](lop-may-chu/su-kien-thong-ke.png)

### 2.2. Câu đố và quản trị viên

![Lược đồ lớp: Câu đố và quản trị viên](lop-may-chu/cau-do-va-quan-tri-vien.png)

### 2.3. Ý nghĩa các lớp

| Lớp | Ý nghĩa | Ghi chú |
|---|---|---|
| `TelemetryEvent` | Một sự kiện thống kê game gửi lên | Một bảng cho cả bảy loại sự kiện; trường riêng của từng loại để trống khi không dùng (xem `database-design.md`). `ReceivedAt` do máy chủ ghi, `OccurredAt` là giờ trên máy người chơi |
| `Puzzle` | Một câu đố ngoài game | Tạo sẵn bằng migration, khóa là `puzzleId` của hợp đồng (`red-thread-sandal`). Quản trị viên chỉ cấu hình |
| `PuzzleSolution` | Một mã băm SHA-256 được coi là đúng | Một câu đố có thể có nhiều mã băm, ví dụ khi phát hành lại tệp bằng chứng |
| `PuzzleSubmission` | Một lần game gửi bằng chứng | Lưu cả lần đúng, sai và bị chặn để trang quản trị xem lịch sử |
| `AdminUser` | Tài khoản quản trị viên | Kế thừa `IdentityUser<Guid>` của ASP.NET Core Identity; thêm tên hiển thị, trạng thái hoạt động và thời điểm đăng nhập cuối |

: Ý nghĩa các lớp thực thể

Không có lớp người chơi. Máy chủ chỉ biết `InstallationId`, một UUID ngẫu nhiên, nên `TelemetryEvent` và `PuzzleSubmission` chỉ giữ mã này như một giá trị, không trỏ tới bảng nào.

Các trường liệt kê (`Type`, `EndingId`, `EndReason`, `Result`) lưu dạng chuỗi snake_case giống hợp đồng, không lưu số, để đọc thẳng được trong cơ sở dữ liệu và trong tệp CSV. Các khối enumeration trong sơ đồ là tập giá trị hợp lệ; với sự kiện thống kê, tập này nằm trong `TelemetryEventValidator`.

## 3. Lớp dịch vụ và controller

### 3.1. Phần công khai (game gọi)

#### 3.1.1. Nhận thống kê

![Lược đồ lớp: Nhận thống kê](lop-may-chu/nhan-thong-ke.png)

#### 3.1.2. Gửi bằng chứng câu đố

![Lược đồ lớp: Gửi bằng chứng câu đố](lop-may-chu/gui-bang-chung-cau-do.png)

| Controller, hành động | API | Dịch vụ | Mã HTTP |
|---|---|---|---|
| `TelemetryController.PostBatch` | `POST /api/v1/telemetry/batches` | `IngestAsync` | 200; 400 vỏ lô sai; 413 quá 256 KiB (giới hạn kích thước thân yêu cầu); 429 giới hạn IP |
| `TelemetryController.DeleteInstallation` | `DELETE /api/v1/telemetry/installations/{installationId}` | `DeleteInstallationAsync` | 204, kể cả khi không có gì để xóa |
| `PuzzleSubmissionsController.Post` | `POST /api/v1/puzzles/{puzzleId}/submissions` | `SubmitAsync` | `Accepted`, `Rejected`: 200; `Blocked`: 429 kèm `Retry-After`; `PuzzleNotFound`: 404 |

: Hành động, API và dịch vụ của phần công khai

Giới hạn tần suất theo địa chỉ IP dùng bộ giới hạn tần suất có sẵn của ASP.NET Core (`AddRateLimiter`) [@ms-ratelimit], khai báo bằng thuộc tính `[EnableRateLimiting]` trên controller; giữ trong bộ nhớ, không ghi IP vào cơ sở dữ liệu. Ba chính sách: `telemetry`, `puzzle-submit` (20 lần mỗi phút), `admin-login` (10 lần mỗi phút). `telemetry` cho 60 yêu cầu mỗi phút cho mỗi IP (chốt ở task #25, đổi bằng cấu hình `RateLimits:Telemetry:PermitLimit`).

### 3.2. Phần quản trị (trang quản trị gọi)

#### 3.2.1. Đăng nhập

![Lược đồ lớp: Đăng nhập](lop-may-chu/dang-nhap.png)

#### 3.2.2. Tài khoản quản trị

![Lược đồ lớp: Tài khoản quản trị](lop-may-chu/tai-khoan-quan-tri.png)

#### 3.2.3. Dashboard và CSV

![Lược đồ lớp: Dashboard và CSV](lop-may-chu/dashboard-va-csv.png)

#### 3.2.4. Quản lý câu đố

![Lược đồ lớp: Quản lý câu đố](lop-may-chu/quan-ly-cau-do.png)

Mỗi giao diện (interface) có một lớp cài đặt cùng tên bỏ chữ `I` (`AdminAccountService`, `DashboardService`, `CsvExportService`, `PuzzleAdminService`), cùng dùng `AppDbContext`; sơ đồ bỏ các lớp đó cho gọn.

| Controller, hành động | API | Dịch vụ |
|---|---|---|
| `AdminAuthController.Login` | `POST /api/admin/auth/login` | `SignInManager.PasswordSignInAsync`, rồi ghi `LastLoginAt` |
| `AdminAuthController.Logout` | `POST /api/admin/auth/logout` | `SignInManager.SignOutAsync` |
| `AdminAuthController.Me` | `GET /api/admin/auth/me` | `UserManager.GetUserAsync` |
| `AdminAuthController.ChangePassword` | `POST /api/admin/auth/password` | `UserManager.ChangePasswordAsync` |
| `AdminAccountsController.List`, `Create` | `GET`, `POST /api/admin/accounts` | `ListAsync`, `CreateAsync` |
| `AdminAccountsController.Update`, `Delete` | `PATCH`, `DELETE /api/admin/accounts/{accountId}` | `UpdateAsync`, `DeleteAsync` |
| `AdminAccountsController.ResetPassword` | `POST /api/admin/accounts/{accountId}/password` | `ResetPasswordAsync` |
| `AdminDashboardController.Get` | `GET /api/admin/dashboard` | `GetSummaryAsync` |
| `AdminDashboardController.Export` | `GET /api/admin/exports/{dataset}` | `WriteSummaryAsync` hoặc `WriteEventsAsync` |
| `AdminPuzzlesController.List`, `Get` | `GET /api/admin/puzzles`, `/{puzzleId}` | `ListAsync`, `GetAsync` |
| `AdminPuzzlesController.Update` | `PATCH /api/admin/puzzles/{puzzleId}` | `UpdateAsync` |
| `AdminPuzzlesController.AddSolution`, `DeleteSolution` | `POST`, `DELETE .../solutions` | `AddSolutionAsync`, `RemoveSolutionAsync` |
| `AdminPuzzlesController.ListSubmissions` | `GET .../submissions` | `ListSubmissionsAsync` |

: Hành động, API và dịch vụ của phần quản trị

Cả bốn controller quản trị mang thuộc tính `[Authorize]`. `AdminAuthController.Login` mang `[AllowAnonymous]`.

Dịch vụ báo lỗi nghiệp vụ bằng ba ngoại lệ riêng: `NotFoundException`, `ConflictException`, `ValidationException`. Một bộ xử lý ngoại lệ chung (`IExceptionHandler`) đổi chúng thành 404, 409, 400 dạng RFC 9457 [@ms-errors; @rfc9457]. Controller không tự bắt ngoại lệ.

## 4. Quy tắc trong dịch vụ

### 4.1. Nhận lô thống kê

1. `TelemetryEventValidator.Check` kiểm từng sự kiện: loại có biết không, đủ trường bắt buộc của loại đó không, `occurredAt` có cũ hơn 30 ngày hay sau giờ máy chủ hơn 24 giờ không. Sai thì cho vào `rejected` với lý do `invalid`, `unknown_type`, `too_old` hoặc `in_future`.
2. Các sự kiện hợp lệ được chèn trong một giao dịch bằng `INSERT ... ON CONFLICT (event_id) DO NOTHING`. Số dòng chèn được là `accepted`; phần còn lại là `duplicates`. Cách này đúng cả khi game gửi cùng một lô hai lần song song, điều mà kiểu "đọc trước xem đã có chưa rồi mới chèn" không bảo đảm.
3. `eventId` trùng ngay trong một lô cũng tính là `duplicates`.

`DeleteInstallationAsync` xóa mọi sự kiện có `installation_id` đó bằng một câu `DELETE`. Lượt gửi bằng chứng của mã cài đặt đó không bị xóa: đó không phải thống kê, và cần cho giới hạn lượt.

### 4.2. Kiểm bằng chứng

`PuzzleSubmissionService.SubmitAsync` chạy trong một giao dịch:

1. Đọc câu đố. Không có hoặc `IsEnabled = false` thì trả `PuzzleNotFound`, không ghi gì.
2. Lấy khóa `pg_advisory_xact_lock` theo cặp `(puzzleId, installationId)`, để hai lần gửi cùng lúc của một mã cài đặt không cùng đọc thấy còn lượt.
3. Đếm số lần `rejected` của cặp đó trong `AttemptWindowMinutes` phút gần nhất. Đủ `MaxFailedAttempts` thì ghi một dòng `blocked` và trả `Blocked`, kể cả khi mã băm đúng.
4. So mã băm gửi lên với các `PuzzleSolution` của câu đố. Khớp thì ghi `accepted`, trả `UnlockId`; không khớp thì ghi `rejected`.
5. Tính `AttemptsRemaining`, `WindowResetsAt` và `RetryAfter` theo cửa sổ trượt của hợp đồng API.

So mã băm là so chuỗi hex chữ thường đã kiểm định dạng; hợp đồng đã buộc game gửi chữ thường.

### 4.3. Dashboard và CSV

`DateRange.Resolve` đổi `from`, `to` của yêu cầu thành khoảng nửa mở theo UTC: thiếu `to` thì lấy giờ hiện tại, thiếu `from` thì lấy 30 ngày trước `to`; `from` không trước `to` hoặc khoảng dài hơn 366 ngày thì báo lỗi 400.

`DashboardService.GetSummaryAsync` chạy các truy vấn tổng hợp trên `telemetry_events` lọc theo `received_at` (giờ máy chủ nhận), không theo giờ máy người chơi:

| Số liệu | Cách tính |
|---|---|
| `players` | Đếm `installation_id` khác nhau |
| `sessions` | Đếm `session_started` |
| `playtime` | Tổng và trung bình `duration_seconds` của `session_ended` |
| `nights` | Theo `night`: đếm `installation_id` khác nhau của `night_started` và của `night_completed`, cùng có `is_replay = false`; trung bình `duration_seconds` của `night_completed` có `is_replay = false` |
| `caughtLocations` | Nhóm `player_caught` theo `night`, `area_id` |
| `quitLocations` | Nhóm `session_ended` có `night` theo `night`, `area_id` |
| `endings` | Theo `ending_id`: đếm `installation_id` khác nhau của `ending_reached` |
| `choices` | Theo `night`, `choice_id`: đếm `installation_id` khác nhau của `choice_made` |

: Cách tính các số liệu trên dashboard

`CsvExportService.WriteSummaryAsync` gọi `GetSummaryAsync` rồi trải mỗi số thành một dòng `metric,night,key,value`. `WriteEventsAsync` đọc sự kiện theo từng phần và ghi thẳng ra luồng phản hồi, không nạp hết vào bộ nhớ.

### 4.4. Tài khoản quản trị

- Mật khẩu băm bằng `PasswordHasher` mặc định của Identity (PBKDF2) [@ms-identity-config]. Tối thiểu 12 ký tự, không bắt buộc chữ hoa, số hay ký tự đặc biệt.
- Sai mật khẩu 5 lần liên tiếp thì Identity tạm chặn đăng nhập 15 phút (`LockoutEnd`) [@ms-identity-config]. Việc này khác với khóa tài khoản (`IsActive = false`), do quản trị viên làm và không tự hết.
- Đăng nhập kiểm thêm `IsActive`; tài khoản đã khóa nhận cùng lỗi 401 như sai mật khẩu.
- Khóa tài khoản, đặt lại hay đổi mật khẩu đều đổi `SecurityStamp`. Cookie kiểm lại dấu này mỗi phút, nên phiên cũ hết hiệu lực trong vòng một phút.
- `UpdateAsync` và `DeleteAsync` nhận `actorId` để từ chối (409) khi người làm tự khóa hoặc tự xóa mình, và khi thao tác làm số tài khoản hoạt động về 0.
- `AdminSeeder` chạy lúc máy chủ khởi động: chưa có tài khoản nào thì tạo một tài khoản từ `Admin__Email` và `Admin__Password`; đã có thì không làm gì.

### 4.5. Cấu hình câu đố

- `UpdateAsync` từ chối (409) việc bật câu đố khi chưa có mã băm hợp lệ nào. Câu đố tạo bằng migration mặc định tắt, nên quản trị viên phải nhập mã băm trước rồi mới bật.
- `RemoveSolutionAsync` từ chối (409) việc bỏ mã băm cuối cùng của câu đố đang bật.
- `AddSolutionAsync` từ chối (409) mã băm đã có trong câu đố.
- Mọi thay đổi ghi `UpdatedAt` và `UpdatedById` vào câu đố.

## 5. Ánh xạ lớp với hợp đồng API

| Schema trong `openapi.yaml` | Lớp `Contracts/` | Thực thể hoặc nguồn |
|---|---|---|
| `TelemetryBatch` | `TelemetryBatchRequest` | Vỏ lô; `gameVersion` chép vào từng `TelemetryEvent` |
| `TelemetryEvent` (bảy loại) | `JsonElement`, kiểm bằng `TelemetryEventValidator` | `TelemetryEvent`; `reason` thành `EndReason`, `position` thành ba cột `Position*` |
| `TelemetryBatchResult`, `RejectedEvent` | `TelemetryBatchResponse` | Kết quả của `IngestAsync` |
| `PuzzleSubmission` | `PuzzleSubmissionRequest` | `PuzzleSubmission` |
| `PuzzleSubmissionResult` | `PuzzleSubmissionResponse` | `SubmissionOutcome` |
| `AdminAccount` | `AdminAccountResponse` | `AdminUser` |
| `DashboardSummary` và các schema con | `DashboardSummary` | Truy vấn tổng hợp trên `TelemetryEvent` |
| `PuzzleConfig`, `PuzzleSolution` | `PuzzleConfigResponse`, `PuzzleSolutionResponse` | `Puzzle`, `PuzzleSolution` |
| `PuzzleSubmissionRecord`, `PuzzleSubmissionPage` | `PuzzleSubmissionRecordResponse`, `PuzzleSubmissionPage` | `PuzzleSubmission` |

: Ánh xạ schema OpenAPI với lớp và thực thể

Mỗi sự kiện trong lô được đọc dưới dạng JSON thô (`JsonElement`), rồi kiểm theo `type` trong `TelemetryEventValidator` (đã làm ở task #25). Làm vậy vì hợp đồng yêu cầu một sự kiện sai chỉ bị loại riêng, không làm hỏng cả lô; nếu để bộ đọc JSON kiểm theo từng kiểu con, một sự kiện sai sẽ làm cả lô nhận 400.

## 6. Quyết định và lý do

| Quyết định | Lý do | Phương án đã bỏ |
|---|---|---|
| Một bảng `telemetry_events` cho bảy loại sự kiện, trường riêng để trống | Truy vấn dashboard chỉ là `GROUP BY` trên một bảng; thêm loại sự kiện chỉ thêm cột tùy chọn | Mỗi loại một bảng (truy vấn phải nối nhiều bảng); một cột `jsonb` (khó kiểm kiểu, truy vấn dài hơn) |
| Bỏ trùng bằng `ON CONFLICT DO NOTHING` | Đúng cả khi gửi song song; một câu lệnh | Đọc trước rồi chèn |
| Câu đố tạo bằng migration, không có API tạo hay xóa | Kịch bản chỉ có một câu đố; yêu cầu 12 chỉ đòi cấu hình | Trang quản trị tạo câu đố mới |
| Nhiều mã băm hợp lệ cho một câu đố | Phát hành lại tệp bằng chứng mà không làm người đã giải bị sai | Một cột mã băm trong `puzzles` |
| Lưu cả lần gửi bị chặn (`blocked`) | Trang quản trị thấy được ai đang dò lời giải | Chỉ lưu lần đúng và sai |
| Khóa tư vấn (advisory lock) theo cặp câu đố và mã cài đặt | Hai lần gửi cùng lúc không vượt được giới hạn lượt; không khóa cả bảng | Mức cô lập `SERIALIZABLE` (phải thử lại khi xung đột) |
| `IdentityUserContext`, không có vai trò | Mọi quản trị viên cùng quyền; bớt bốn bảng không dùng | `IdentityDbContext` đầy đủ với vai trò |
| Lỗi nghiệp vụ là ngoại lệ, đổi sang mã HTTP ở một chỗ | Controller ngắn; mã lỗi nhất quán theo RFC 9457 | Mỗi controller tự kiểm và tự trả lỗi |
| Tên bảng và cột snake_case | Quy ước của PostgreSQL [@pg-lexical]; viết SQL tay không cần ngoặc kép | Tên PascalCase mặc định của EF Core |

: Các quyết định thiết kế máy chủ và lý do
