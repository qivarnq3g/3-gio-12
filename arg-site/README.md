# Website câu đố ngoài game (ARG Site)

Website câu đố ngoài game cho đồ án "3 GIỜ 12" (thực hiện theo Issue #30, phục vụ Yêu cầu chức năng 7, 12 và 14).

## 1. Mục tiêu và Thiết kế

- **Tính hư cấu và nhập vai:** Trang web được dựng như một blog cá nhân chân thực của nhân vật **Nguyễn Thảo Ngân** (19 tuổi, sinh viên năm 2 khoa Nhật Bản học, người mất tích mà nhân vật chính nhặt được điện thoại). Website tuyệt đối **không** mang tên game, không logo hay thông tin đồ án để đảm bảo tính nhập vai (ARG - Alternate Reality Game).
- **Trang ẩn `/drop`:** Không có bất kỳ liên kết trực tiếp nào từ thanh điều hướng hay nội dung trang chủ trỏ tới `/drop`. Người chơi phải tìm được manh mối từ nhật ký ngày 22/10 trong két ẩn ở Đêm 3 của game để biết đường dẫn này.
- **Tệp tĩnh hoàn toàn:** Website là tập hợp các tệp tĩnh (HTML/CSS/JS thuần và hình ảnh), không thu thập dữ liệu người xem, không cần backend riêng, sẵn sàng để Caddy/Nginx phục vụ tại tên miền riêng (theo mục 2.5 tài liệu kiến trúc hệ thống).

## 2. Cấu trúc thư mục

```
arg-site/
├── index.html                           # Trang chủ blog: "Góc nhỏ của Bống"
├── about.html                           # Giới thiệu bản thân Ngân, mẹ Hòa, bạn thân Linh, mèo Na
├── drop/
│   └── index.html                       # Trang bí mật /drop/ (Web Decryptor & tải bằng chứng)
├── drop.html                            # Trang bí mật /drop (hỗ trợ truy cập trực tiếp không slash)
├── posts/                               # Các bài viết blog theo dòng thời gian
│   ├── 2026-09-28-hen-ngay-3-thang-12.html # Kỷ niệm ăn chè ngày 3/12 và kế hoạch bay Kyoto
│   ├── 2026-10-15-chuyen-meo-na.html    # Mèo Na gác cầu thang và mật hiệu gõ cửa
│   ├── 2026-10-23-nhung-dieu-ky-la.html # Bất thường: Na mất tích, dép lạ, gõ cửa 3:05, gợi ý trang /drop
│   └── 2026-11-02-dem-nguoc-30-ngay.html# Bài viết cuối: Đếm ngược một tháng trước khi mất tích
├── assets/
│   ├── css/
│   │   └── style.css                    # Giao diện blog phong cách mộc mạc, hoài niệm, responsive
│   ├── js/
│   │   ├── drop.js                      # Bộ giải mã trực tiếp trên trình duyệt (client-side decryptor)
│   │   └── evidence_data.js             # Dữ liệu mã hóa nhúng để giải mã trực tiếp không cần server
│   └── img/
│       ├── avatar.jpg                   # Ảnh đại diện của Ngân
│       ├── theu-ca-bong.jpg             # Miếng vải thêu con cá bống lót ốp điện thoại
│       ├── meo-na.jpg                   # Ảnh mèo Na
│       ├── hem-12.jpg                   # Bối cảnh con hẻm 12
│       └── evidence/
│           ├── red-thread-sandal.jpg    # TỆP BẰNG CHỨNG GỐC (Đôi dép chỉ đỏ)
│           └── evidence-package.zip     # Gói nén ZIP khóa mật khẩu ngoại tuyến (Password: 472619)
└── README.md                            # Tài liệu kỹ thuật này
```

## 3. Luồng giải đố (Walkthrough)

1. **Khám phá trong game (Đêm 3):**
   - Người chơi liên lạc với mẹ thật (cô Hòa), xin được mật mã mở app Máy tính (két ẩn): `472619`.
   - Trong két ẩn, mở nhật ký ngày 22/10: Ngân ghi lại việc chú Thành thay khóa, sự xuất hiện của đôi dép lạ thắt sợi chỉ đỏ trên kệ giày, và việc cô đã chụp ảnh lưu trữ khẩn cấp tại đường dẫn ẩn `/drop` trên blog cá nhân.
2. **Truy cập Website ngoài đời thực:**
   - Người chơi mở trình duyệt, truy cập trang `/drop` của website.
   - Nhập mật mã mở két: `472619`.
   - Trang web giải mã thành công và hiển thị ảnh bằng chứng `red-thread-sandal.jpg` kèm mã băm SHA-256.
   - Người chơi tải tệp ảnh gốc về máy (hoặc tải gói `evidence-package.zip` và giải nén với mật khẩu `472619`).
3. **Mở kết thúc thật trong game:**
   - Sau khi hoàn thành kết thúc "Về nhà", game mở mục "Gửi bằng chứng cho công an".
   - Người chơi chọn tệp `red-thread-sandal.jpg` từ máy tính của mình.
   - Game tính mã băm SHA-256 và gửi lên `POST /api/v1/puzzles/red-thread-sandal/submissions`.
   - Máy chủ kiểm tra khớp mã băm và trả về `200 accepted` kèm `unlockId: ending_red_thread_sandal`, mở kết thúc thật.

## 4. Thông tin bằng chứng & Cấu hình máy chủ

| Mục | Giá trị |
|---|---|
| **Puzzle ID** | `red-thread-sandal` |
| **Tiêu đề câu đố** | `Đôi dép chỉ đỏ` |
| **Unlock ID** | `ending_red_thread_sandal` |
| **Tên tệp bằng chứng gốc** | `red-thread-sandal.jpg` |
| **Mật mã giải nén / giải mã** | `472619` |
| **Mã băm SHA-256** | `470aa17cf42bf8d7baec3a7a8574b2b5802c3df79c4cb2a3d9174fad4d1d0f94` |

### Câu lệnh SQL cấu hình cơ sở dữ liệu

Quản trị viên cấu hình giải pháp bằng chứng vào PostgreSQL trên máy chủ:

```sql
-- Thêm mã băm bằng chứng hợp lệ cho câu đố red-thread-sandal
INSERT INTO puzzle_solutions (puzzle_id, sha256, note, created_at)
VALUES (
    'red-thread-sandal',
    '470aa17cf42bf8d7baec3a7a8574b2b5802c3df79c4cb2a3d9174fad4d1d0f94',
    'Ảnh đôi dép chỉ đỏ từ arg-site/drop',
    NOW()
);

-- Kích hoạt câu đố
UPDATE puzzles
SET is_enabled = true, updated_at = NOW()
WHERE id = 'red-thread-sandal';
```

## 5. Cách chạy thử nghiệm

Mở trực tiếp tệp `arg-site/index.html` trong bất kỳ trình duyệt web nào, hoặc khởi chạy một máy chủ tĩnh nội bộ:

```bash
# Sử dụng Python
python -m http.server 3000 -d arg-site

# Mở trình duyệt:
# http://localhost:3000          (Trang chủ blog)
# http://localhost:3000/drop     (Trang giải mã bằng chứng)
```
