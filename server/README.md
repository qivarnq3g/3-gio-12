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

## Cấu trúc

| Thư mục | Nội dung |
|---|---|
| `src/ThreeTwelve.Api/Controllers/` | Controller của API; chưa có, thêm ở task #25, #26, #27 |
| `src/ThreeTwelve.Api/Data/` | `AppDbContext` và migration |
| `src/ThreeTwelve.Api/Health/` | Định dạng phản hồi của `/health` theo hợp đồng API |
| `tests/ThreeTwelve.Api.Tests/` | Test tích hợp chạy qua HTTP với PostgreSQL thật |
