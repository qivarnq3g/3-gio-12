# 3 GIỜ 12

Game kinh dị 3D góc nhìn thứ ba (Unity), kèm máy chủ thống kê và trang quản trị. Đồ án thực tế Công nghệ phần mềm, UTH.

## Cấu trúc

| Thư mục | Nội dung | Phụ trách chính |
|---|---|---|
| `game/` | Project Unity 6.3 LTS | Phúc (gameplay), Quang (màn chơi, điện thoại), Tiên (3D nhân vật, đồ vật) |
| `server/` | Máy chủ ASP.NET Core + PostgreSQL | Quang |
| `admin/` | Trang quản trị React | Quang, Thắng |
| `arg-site/` | Website câu đố ngoài game | Thắng, Quang |
| `docs/` | Thiết kế, biên bản họp, kế hoạch | Cả nhóm |

File gốc nặng (`.blend`, `.psd`, ảnh AI gốc) để trên Drive, không đưa vào repo.

## Phân công

| Người | Việc |
|---|---|
| Quang (nhóm trưởng) | Kịch bản đêm 1-3, 3D bối cảnh, web chính, kiến trúc hệ thống, lược đồ lớp phần web, quy trình nhóm, kế hoạch khởi nghiệp |
| Phúc | Gameplay Unity, ghép màn chơi, lược đồ lớp phần game |
| Tiên | 3D nhân vật và đồ vật, đưa vào Unity, biên bản họp hằng tuần |
| Thắng | Điện thoại trong game, phụ web, kiểm thử, use case, tổ chức chơi thử |

## Cách làm việc

1. Mỗi việc là một issue, có người nhận và mốc hạn. Theo dõi trên bảng việc (GitHub Projects): Cần làm, Đang làm, Xong.
2. Không sửa thẳng `main`. Tạo nhánh từ issue, làm xong mở pull request, ít nhất một người khác duyệt rồi mới gộp.
3. Họp mỗi tuần một lần: xem bảng việc, ghi biên bản vào `docs/bien-ban/`, giao việc tuần sau.
4. Có vướng mắc thì ghi bình luận vào issue, không chỉ nói qua Zalo, để còn dấu vết.

## Mốc nộp

- 16/11/2026 09:07: thuyết minh thiết kế (kiến trúc, use case, lược đồ lớp).
- 03/12/2026 09:07: dự án hoàn chỉnh (đêm 1-3 chơi được, máy chủ, trang quản trị, báo cáo, thuyết trình).

## Hướng dẫn chạy máy chủ & cơ sở dữ liệu

Dự án sử dụng PostgreSQL 17 và ASP.NET Core API. Cơ sở dữ liệu và cấu trúc bảng được đồng bộ tự động giữa tất cả thành viên qua EF Core Migrations được lưu trong Git.

### 1. Khởi động nhanh bằng Docker Compose

Lần đầu mở dự án trên máy cá nhân, sao chép file cấu hình môi trường:

```bash
# Windows PowerShell
Copy-Item .env.example .env

# macOS / Linux
cp .env.example .env
```

Khởi động cơ sở dữ liệu PostgreSQL và máy chủ API:

```bash
docker compose up -d --build
```

- **API máy chủ:** `http://localhost:8080` (kiểm tra `http://localhost:8080/health` trả về `{"status":"ok"}`).
- **PostgreSQL:** Chạy ở cổng `5432` (`localhost:5432`, db: `threetwelve`, user/pass: `threetwelve` / `threetwelve-dev`).
- Dữ liệu được lưu bền vững vào Docker volume `pgdata`.
- Khi máy chủ khởi động, nó tự động chạy mọi Migration để đồng bộ cấu trúc bảng và tự động tạo tài khoản quản trị đầu tiên (`admin@example.test` / `mat-khau-thu-dai-12`).

### 2. Nạp dữ liệu mẫu để kiểm thử Dashboard (#28)

Vì hệ thống mới dựng và game chưa gửi sự kiện thực tế, cơ sở dữ liệu ban đầu hoàn toàn rỗng (0 người chơi, 0 phiên). Để nạp dữ liệu mẫu mô phỏng người chơi chơi qua các đêm để kiểm thử Dashboard:

```bash
python tools/seed_telemetry.py
```

Lệnh này sẽ gửi 15 lượt người chơi với 135 sự kiện thống kê mẫu (Đêm 1-3, vị trí bị bắt, nhánh lựa chọn, các kết thúc) qua API `POST /api/v1/telemetry/batches`.

Khi muốn xóa sạch dữ liệu quay về trạng thái rỗng ban đầu:
```bash
python tools/seed_telemetry.py --clear
```

### 3. Chạy trang quản trị Admin (#27, #28)

```bash
cd admin
npm install
npm run dev
```

Mở trình duyệt: `http://localhost:5173/admin/` (đăng nhập bằng `admin@example.test` / `mat-khau-thu-dai-12`).

## Tài liệu thiết kế

- [Kiến trúc hệ thống](docs/thiet-ke/kien-truc-he-thong.md)
- [Lược đồ lớp phần game](docs/thiet-ke/luoc-do-lop-game.md)

## Bản quyền

Kho này không phải mã nguồn mở. Nhóm giữ toàn quyền với mã nguồn, kịch bản, hình ảnh, âm thanh và bản build; không được sao chép hay dùng lại khi chưa được phép. Chi tiết ở [LICENSE](LICENSE). Tài sản của bên thứ ba theo giấy phép riêng, liệt kê ở [NGUON.md](game/Assets/Art/_Source/NGUON.md).
