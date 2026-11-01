# Website câu đố ngoài game (ARG Site)

Website câu đố ngoài game cho đồ án "3 GIỜ 12" (thực hiện theo Issue #30, phục vụ Yêu cầu chức năng 7, 12 và 14).

## 1. Mục tiêu và Thiết kế

- **Tính hư cấu và nhập vai:** Trang web được dựng như một blog cá nhân chân thực của nhân vật **Nguyễn Thảo Ngân** (19 tuổi, sinh viên năm 2 khoa Nhật Bản học, người mất tích mà nhân vật chính nhặt được điện thoại). Website tuyệt đối **không** mang tên game, không logo, không nhắc tới game hay đồ án, kể cả ở trang `/drop`.
- **Trang ẩn `/drop`:** Không có bất kỳ liên kết trực tiếp nào từ thanh điều hướng hay nội dung trang chủ trỏ tới `/drop`. Người chơi phải tìm được manh mối từ nhật ký ngày 22/10 trong két ẩn ở Đêm 3 của game để biết đường dẫn này.
- **Lời giải không nằm trên website hay trong repo:** Website chỉ phát hành một ảnh vỏ chứa gói ZIP đã mã hóa. Ảnh bằng chứng gốc và mã băm SHA-256 của nó không có trong `arg-site/` và không commit; mã băm chỉ nằm trên máy chủ (mục 1 và 2.5 tài liệu kiến trúc).
- **Tệp tĩnh hoàn toàn:** Website là tập hợp các tệp tĩnh (HTML/CSS/JS thuần và hình ảnh), không thu thập dữ liệu người xem, không cần backend riêng, sẵn sàng để Caddy/Nginx phục vụ tại tên miền riêng (theo mục 2.5 tài liệu kiến trúc hệ thống).

## 2. Cấu trúc thư mục

```
arg-site/
├── index.html                           # Trang chủ blog: "Góc nhỏ của Bống"
├── about.html                           # Giới thiệu bản thân Ngân, mẹ Hòa, bạn thân Linh, mèo Na
├── drop/
│   ├── index.html                       # Trang bí mật /drop (bộ giải mã và link tải bản sao lưu)
│   └── IMG_20261022_234117.jpg          # Ảnh vỏ: JPEG mờ tối + gói ZIP mã hóa nối ở cuối
├── posts/                               # Các bài viết blog theo dòng thời gian
│   ├── 2026-09-28-hen-ngay-3-thang-12.html # Kỷ niệm ăn chè ngày 3/12 và kế hoạch bay Kyoto
│   ├── 2026-10-15-chuyen-meo-na.html    # Mèo Na gác cầu thang và mật hiệu gõ cửa
│   ├── 2026-10-23-nhung-dieu-ky-la.html # Bất thường: Na mất tích, dép lạ, gõ cửa 3:05, gợi ý trang /drop
│   └── 2026-11-02-dem-nguoc-30-ngay.html# Bài viết cuối: Đếm ngược một tháng trước khi mất tích
├── assets/
│   ├── css/
│   │   └── style.css                    # Giao diện blog phong cách mộc mạc, hoài niệm, responsive
│   ├── js/
│   │   └── drop.js                      # Đọc gói ZIP trong ảnh vỏ và giải mã ZipCrypto trên trình duyệt
│   └── img/                             # Ảnh minh họa của blog
└── README.md                            # Tài liệu kỹ thuật này
```

`drop.js` không chứa mã hay ảnh gốc. Nó tải ảnh vỏ, giải mã bằng mã người chơi nhập, rồi kiểm CRC32 của dữ liệu: sai mã thì CRC không khớp.

## 3. Luồng giải đố (Walkthrough)

1. **Khám phá trong game (Đêm 3):**
   - Người chơi liên lạc với mẹ thật (cô Hòa), xin được mật mã mở app Máy tính (két ẩn).
   - Trong két ẩn, mở nhật ký ngày 22/10: Ngân ghi lại việc chú Thành thay khóa, sự xuất hiện của đôi dép lạ thắt sợi chỉ đỏ trên kệ giày, và việc cô đã cất bản sao lưu khẩn cấp tại đường dẫn ẩn `/drop` trên blog cá nhân.
2. **Truy cập Website ngoài đời thực**, một trong hai cách:
   - Ở trang `/drop`, nhập mã két. Trình duyệt giải mã ảnh vỏ ngay trên máy, hiện ảnh dép và cho tải tệp gốc `red-thread-sandal.jpg`.
   - Tải ảnh vỏ `IMG_20261022_234117.jpg` về, mở bằng WinRAR hoặc 7-Zip (hoặc `tar -xf <tệp> --passphrase <mã>` trên Windows 10 trở lên), nhập mã két, giải nén ra `red-thread-sandal.jpg`.
3. **Mở kết thúc thật trong game:**
   - Sau khi hoàn thành kết thúc "Về nhà", game mở mục "Gửi bằng chứng cho công an".
   - Người chơi chọn tệp `red-thread-sandal.jpg` từ máy tính của mình.
   - Game tính mã băm SHA-256 và gửi lên `POST /api/v1/puzzles/red-thread-sandal/submissions`.
   - Máy chủ kiểm tra khớp mã băm và trả về `200 accepted` kèm `unlockId: ending_red_thread_sandal`, mở kết thúc thật.

## 4. Dựng tệp bằng chứng và cấu hình máy chủ

| Mục | Giá trị |
|---|---|
| **Puzzle ID** | `red-thread-sandal` |
| **Tiêu đề câu đố** | `Đôi dép chỉ đỏ` |
| **Unlock ID** | `ending_red_thread_sandal` |
| **Tên tệp bằng chứng gốc** | `red-thread-sandal.jpg` |
| **Mật khẩu gói ZIP** | Mã két ở đêm 3 (`docs/kich-ban/dem-3.md`, mục 4.3) |
| **Mã băm SHA-256** | Không ghi ở đây; xem bên dưới |

Dựng ảnh vỏ bằng script (cần Python và Pillow):

```bash
python tools/build_arg_site_assets.py
```

- Script giữ tệp gốc ở `tools/out/arg-evidence/red-thread-sandal.jpg` (đã gitignore) và in mã băm SHA-256 của nó ra màn hình.
- Chạy lại không đổi tệp gốc, nên mã băm trên máy chủ vẫn đúng. Máy mới chưa có tệp gốc, hoặc chạy với `--new-evidence`, sẽ tạo tệp gốc mới với mã băm mới: khi đó thêm mã băm mới trên trang quản trị (một câu đố nhận được nhiều mã băm).
- `--images` vẽ lại cả ảnh minh họa của blog.

Quản trị viên nhập mã băm in ra vào câu đố `red-thread-sandal` trên trang quản trị và bật câu đố. Không dán mã băm vào repo, issue hay chat nhóm công khai: biết mã băm là mở được kết thúc thật mà không cần giải đố.

Tệp gốc và mã băm cũ từng nằm trong lịch sử git (commit `01af83b`), nên không dùng mã băm của bản đó trên máy chủ.

## 5. Cách chạy thử nghiệm

Khởi chạy một máy chủ tĩnh nội bộ (trang `/drop` cần tải ảnh vỏ bằng `fetch`, nên không mở trực tiếp tệp HTML được):

```bash
python -m http.server 3000 -d arg-site
```

Mở trình duyệt:
- `http://localhost:3000`: trang chủ blog
- `http://localhost:3000/drop`: trang giải mã bằng chứng
