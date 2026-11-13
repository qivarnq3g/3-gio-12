# Máy chủ API

ASP.NET Core trên .NET 10, EF Core, PostgreSQL. Kiến trúc ở `docs/thiet-ke/kien-truc-he-thong.md`, hợp đồng API ở `docs/api/`.

## Cần cài

- .NET SDK 10
- Docker Desktop (đang chạy)

## Chạy bằng Docker Compose

Từ thư mục gốc của repo, lần đầu chép tệp biến môi trường mẫu:

```bash
cp .env.example .env
```

Rồi chạy:

```bash
docker compose up -d --build
```

Máy chủ ở `http://localhost:8080`. Kiểm: `http://localhost:8080/health` trả `{"status":"ok"}`. Khi khởi động, máy chủ tự áp mọi migration chưa chạy.

Dừng và xóa dữ liệu:

```bash
docker compose down -v
```

## Chạy từ mã nguồn

Chạy riêng PostgreSQL bằng Compose, rồi chạy máy chủ bằng `dotnet run`; cấu hình `Development` trỏ tới PostgreSQL ở `localhost:5432` với tài khoản trong `.env.example`.

```bash
docker compose up -d postgres
dotnet run --project server/src/ThreeTwelve.Api
```

## Kiểm thử

```bash
cd server
dotnet test
```

Test chạy với PostgreSQL thật trong container (Testcontainers), nên Docker phải đang chạy. Trên GitHub, workflow `server` chạy test và build ảnh Docker cho mọi pull request sửa `server/`.

## Thêm migration

```bash
cd server
dotnet tool restore
dotnet ef migrations add <TenMigration> -p src/ThreeTwelve.Api -o Data/Migrations
```

## Các API đã có

| API | Task |
|---|---|
| `GET /health` | #18 |
| `POST /api/v1/telemetry/batches`, `DELETE /api/v1/telemetry/installations/{installationId}` | #25 |
| `POST /api/v1/puzzles/{puzzleId}/submissions` | #26 |
| `/api/admin/auth/...` (đăng nhập, đăng xuất, tài khoản đang đăng nhập, đổi mật khẩu), `/api/admin/accounts/...` | #27 |

Giới hạn tần suất theo địa chỉ IP: API thống kê 60 yêu cầu mỗi phút (biến `RateLimits__Telemetry__PermitLimit`), API gửi bằng chứng 20 yêu cầu mỗi phút (biến `RateLimits__PuzzleSubmit__PermitLimit`). Địa chỉ IP chỉ nằm trong bộ nhớ, không ghi vào cơ sở dữ liệu.

Tài khoản quản trị đầu tiên được tạo lúc khởi động nếu chưa có tài khoản nào, từ `Admin__Email` và `Admin__Password` (Docker Compose lấy từ `ADMIN_EMAIL`, `ADMIN_PASSWORD` trong `.env`). Mật khẩu tối thiểu 12 ký tự. Đăng nhập dùng cookie phiên `ThreeTwelve.Admin` (HttpOnly, Secure, SameSite=Strict, hết hạn sau 8 giờ không hoạt động). Sai mật khẩu 5 lần liên tiếp thì tài khoản bị chặn đăng nhập 15 phút; mỗi địa chỉ IP thử tối đa 10 lần mỗi phút (`RateLimits__AdminLogin__PermitLimit`).

Câu đố `red-thread-sandal` được migration tạo sẵn ở trạng thái tắt, chưa có mã băm hợp lệ, nên API trả `404` cho tới khi quản trị viên thêm mã băm và bật câu đố. Trang quản lý câu đố chưa có (task #29); trong lúc chờ, bật bằng SQL trên máy cá nhân:

```sql
insert into puzzle_solutions (puzzle_id, sha256, created_at) values ('red-thread-sandal', '<sha256 của tệp bằng chứng>', now());
update puzzles set is_enabled = true where id = 'red-thread-sandal';
```

Lượt sai được đếm theo cửa sổ trượt cho từng cặp câu đố và mã cài đặt; hai lần gửi cùng lúc của một mã cài đặt được xếp hàng bằng `pg_advisory_xact_lock`, nên không lần nào vượt quá số lượt.

## Cấu trúc

| Thư mục | Nội dung |
|---|---|
| `src/ThreeTwelve.Api/Controllers/` | Controller: nhận yêu cầu, kiểm phần vỏ, trả mã HTTP |
| `src/ThreeTwelve.Api/Contracts/` | Dữ liệu vào ra theo `docs/api/openapi.yaml` |
| `src/ThreeTwelve.Api/Services/` | Quy tắc nghiệp vụ: kiểm từng sự kiện, bỏ trùng, xóa theo mã cài đặt, kiểm bằng chứng và đếm lượt sai |
| `src/ThreeTwelve.Api/Domain/` | Lớp thực thể |
| `src/ThreeTwelve.Api/Data/` | `AppDbContext` và migration; tên bảng, cột viết snake_case |
| `src/ThreeTwelve.Api/Auth/` | Cấu hình cookie đăng nhập, tạo tài khoản quản trị đầu tiên |
| `src/ThreeTwelve.Api/RateLimiting/` | Giới hạn tần suất theo địa chỉ IP |
| `src/ThreeTwelve.Api/Health/` | Định dạng phản hồi của `/health` theo hợp đồng API |
| `tests/ThreeTwelve.Api.Tests/` | Test tích hợp chạy qua HTTP với PostgreSQL thật |
