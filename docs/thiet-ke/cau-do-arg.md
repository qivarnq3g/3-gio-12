# Thiết kế Website câu đố ngoài game (ARG Site)

Tài liệu thiết kế và hướng dẫn kỹ thuật cho Website câu đố ngoài game của đồ án "3 GIỜ 12" (thực hiện theo Issue #30, phục vụ Yêu cầu chức năng 7, 12, 14, khớp mục 8 kịch bản bản 5 và tài liệu kiến trúc hệ thống mục 2.5, 3.2).

## 1. Thiết kế cốt truyện và tính nhập vai

- **Nhân vật Nguyễn Thảo Ngân:** 19 tuổi, sinh viên năm hai ngành Công nghệ thông tin, thành viên câu lạc bộ CTF của trường, sống cùng mẹ (cô Hòa) tại Căn 44, tầng 4, Cư xá C4. (Phòng 7 nhà trọ cô Liên ở hẻm 12 là phòng Linh thuê ngày 22/11 cho Sinh viên).
- **Linh:** Bạn thanh mai trúc mã từ lớp 6, hiện là sinh viên năm hai ngành Kế toán. Linh ít nói, không thích chụp ảnh mặt, và là người có những hành vi bất thường ở các đêm sau.
- **Bé Na:** Bé gái 6 tuổi ở tầng dưới cư xá C4, hay chạy lên căn 44 chơi và có mật hiệu gõ cửa "một cái, rồi hai cái liền" với chị Ngân. Việc bé Na mất tích là nút thắt lõi của truyện (mục 2.4, 2.5).
- **Tính bảo mật của blog:** Ngân giấu kín mọi nghi ngờ vào két bảo mật trên điện thoại vì sợ Linh đọc được (mục 2.7). Do đó, blog cá nhân tuyệt đối không đăng bài công khai về chuyện nghi ngờ hay việc bé Na mất tích để không làm lộ manh mối trước khi người chơi mở két.

## 2. Cấu trúc thư mục `arg-site/`

Thư mục `arg-site/` là website tĩnh thuần túy (không chứa mã giải mã client-side, không chứa tệp bằng chứng gốc chưa mã hóa, không chứa mật mã hay tài liệu giải pháp để tránh bị lộ khi triển khai công khai):

```
arg-site/
├── index.html                           # Trang chủ blog: "Góc nhỏ của Bống"
├── about.html                           # Giới thiệu bản thân Ngân, mẹ Hòa, bạn thân Linh, bé Na
├── drop/
│   └── index.html                       # Trang lưu trữ đính kèm /drop/ (tải ảnh hoàng hôn)
├── drop.html                            # Trang lưu trữ đính kèm /drop (alias)
├── posts/                               # Các bài viết blog theo dòng thời gian
│   ├── 2026-09-28-hen-ngay-3-thang-12.html # Kỷ niệm ăn chè ngày 3/12 từ hồi lớp 6 và kế hoạch bay
│   ├── 2026-10-15-chuyen-be-na.html     # Bé Na 6 tuổi tầng dưới và trò chơi gõ cửa mật hiệu
│   ├── 2026-10-28-hoang-hon-song-sai-gon.html # Chiều hoàng hôn sông Sài Gòn sau buổi sinh hoạt CTF
│   └── 2026-11-02-dem-nguoc-30-ngay.html# Đếm ngược một tháng trước khi bay
└── assets/
    ├── css/
    │   └── style.css                    # Giao diện blog phong cách mộc mạc, hoài niệm, responsive
    └── img/
        ├── avatar.jpg                   # Ảnh đại diện của Ngân
        ├── cu-xa-c4.jpg                 # Bối cảnh hành lang Cư xá C4
        ├── theu-ca-bong.jpg             # Miếng vải thêu con cá bống lót ốp điện thoại
        └── hoang-hon-song-sai-gon.jpg   # Ảnh hoàng hôn sông Sài Gòn (chứa tệp nén zip mã hóa ở đuôi)
```

## 3. Luồng giải đố (Walkthrough theo Mục 8 Kịch bản bản 5)

1. **Khám phá trong game (Mục 8.1):**
   - Người chơi dùng mật khẩu mở khóa màn hình để mở **phần trong của két bí mật** (ứng dụng Máy tính).
   - Trong phần trong có tệp ghi âm `test_mic_12-11.wav`. Khi mở xem phổ âm thanh (spectrogram) của tệp ghi âm này, người chơi thấy dòng chữ ẩn dẫn tới đường dẫn `/drop` trên blog của Ngân.
2. **Lấy chìa giải mã (Mục 8.4):**
   - Trong phần trong của két có tệp văn bản `khoa.txt` chứa chìa khóa giải mã 16 ký tự ngẫu nhiên.
3. **Tải và trích xuất bằng chứng (Mục 8.3):**
   - Người chơi truy cập trang `/drop` của website, tải tệp ảnh `hoang-hon-song-sai-gon.jpg`.
   - Sử dụng phần mềm quản lý lưu trữ (7-Zip, WinRAR, hoặc lệnh `unzip`): mở tệp ảnh này như một tệp nén (vì phần đuôi tệp đã được gắn gói nén ZIP theo kỹ thuật steganography).
   - Nhập mật khẩu từ `khoa.txt` để giải nén ra tệp ảnh bằng chứng gốc: `red-thread-sandal.jpg` (ảnh chụp chiếc dép chỉ đỏ trong tủ giày nhà Linh lúc 23:40 ngày 08/11/2026).
4. **Mở kết thúc thật (Mục 8.5):**
   - Sau khi hoàn thành kết thúc "Về nhà" (6.6), game mở tính năng "Gửi bằng chứng cho công an".
   - Người chơi chọn tệp `red-thread-sandal.jpg`. Game tự động đọc byte, tính SHA-256 và gửi lên `POST /api/v1/puzzles/red-thread-sandal/submissions`.
   - Máy chủ so khớp với mã băm hợp lệ đã cấu hình và trả về `200 accepted` kèm `unlockId: ending_red_thread_sandal`, mở kết thúc 6.7.

## 4. Thông số kỹ thuật & Cấu hình máy chủ

| Mục | Giá trị |
|---|---|
| **Puzzle ID** | `red-thread-sandal` |
| **Tiêu đề câu đố** | `Đôi dép chỉ đỏ` |
| **Unlock ID** | `ending_red_thread_sandal` |
| **Tên tệp bằng chứng gốc** | `red-thread-sandal.jpg` (ảnh chụp tủ giày nhà Linh lúc 23:40 ngày 08/11/2026) |
| **Chìa khóa giải mã (`khoa.txt`)** | `7bF#9qK$2vX@mP5!` (16 ký tự trong phần trong của két) |
| **Mã băm SHA-256** | `d83e03fc5af7b42e26aebf45fef98db53b7d7873b39c2f8c5d5b8d0a27d926a3` |

### Câu lệnh SQL cấu hình máy chủ

```sql
-- Thêm mã băm bằng chứng hợp lệ cho câu đố red-thread-sandal
INSERT INTO puzzle_solutions (puzzle_id, sha256, note, created_at)
VALUES (
    'red-thread-sandal',
    'd83e03fc5af7b42e26aebf45fef98db53b7d7873b39c2f8c5d5b8d0a27d926a3',
    'Ảnh chiếc dép chỉ đỏ trong tủ giày nhà Linh (23:40 08/11)',
    NOW()
);

-- Bật câu đố
UPDATE puzzles
SET is_enabled = true, updated_at = NOW()
WHERE id = 'red-thread-sandal';
```

## 5. Công cụ tạo tài nguyên

Tệp ảnh bằng chứng gốc và chìa khóa giải mã được quản lý trong thư mục `tools/out/` (đã khai báo trong `.gitignore` để không đưa lên repository). Khi cần tạo lại các tài nguyên hoặc đổi chìa khóa:

```bash
python tools/build_arg_site_assets.py
```
