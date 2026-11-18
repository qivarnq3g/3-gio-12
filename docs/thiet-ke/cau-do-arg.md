# Website câu đố ngoài game

Website tĩnh của Ngân thực hiện issue #30. Bài ngày 28/10 dùng ảnh hoàng hôn bên hồ Bán Nguyệt. Ảnh chụp ngày 08/11 mang gói bằng chứng và được phục vụ ở `/drop/`, nơi người chơi tìm đến từ phổ âm của `test_mic_12-11.wav` trong két trên điện thoại Ngân.

## Luồng giải đố

1. Mở phần trong của két để lấy tệp ghi âm và `khoa.txt` chứa chìa giải mã 16 ký tự.
2. Xem phổ âm của tệp ghi âm để tìm đường dẫn `/drop`. Tải ảnh `hoang-hon-song-sai-gon.jpg` trên trang này.
3. Mở ảnh tải xuống bằng 7-Zip hoặc WinRAR và giải nén ZIP AES-256 bằng chìa từ két. Gói chứa ảnh dép chỉ đỏ trong tủ giày nhà Linh lúc 23:40 ngày 08/11/2026, ảnh tờ tìm bé Na và ghi chú so sánh của Ngân.
4. Sau khi đạt kết thúc “Về nhà”, gửi tệp `red-thread-sandal.jpg` qua tính năng gửi bằng chứng trong game. Game tính SHA-256 và gửi tới `POST /api/v1/puzzles/red-thread-sandal/submissions`. Máy chủ trả về `unlockId: ending_red_thread_sandal` khi chấp nhận.

## Tệp và nguồn ảnh

| Tệp | Nguồn và mục đích |
|---|---|
| `arg-site/assets/img/hoang-hon-song-sai-gon.png` | Ảnh hồ Bán Nguyệt của @jack_long304, tải từ bài viết trên [Phụ Nữ Online](https://tuoitre.vn/phunuonline/bo-tui-nhung-diem-san-hoang-hon-tuyet-dep-o-sai-gon-1101410746.htm) |
| `arg-site/assets/img/be-na.jpg` | Tranh minh họa nhân vật hư cấu tạo bằng ImageGen |
| `arg-site/assets/img/theu-ca-bong.jpg` | Ảnh minh họa miếng vải thêu cá bống, tự tạo bằng AI cho dự án. |
| `cover-2026-11-08.jpg` | Ảnh hoàng hôn riêng, tạo bằng ImageGen để đóng gói tại `/drop/` |
| `red-thread-sandal.jpg` | Ảnh bằng chứng hư cấu tạo bằng ImageGen, lưu riêng dưới dạng JPEG cố định |
| `missing-child-notice.jpg` | Tờ tìm trẻ dựng từ tranh minh họa bé Na và chữ theo kịch bản |
| `ghi-chu-so-sanh.txt` | Ghi chú của Ngân dùng để đối chiếu hai hình ảnh trong gói |

## Dựng lại gói bằng chứng

Đặt ảnh bìa 08/11, ba tệp bằng chứng và `khoa.txt` trong thư mục đầu vào cục bộ, mặc định là `tools/out/arg-evidence/`. Có thể chỉ định thư mục khác bằng `--inputs`. Lấy chìa đang dùng từ bản nội dung két được quản lý riêng và lưu chìa mới cùng nhóm phụ trách tích hợp game khi xoay chìa.

```bash
python -m pip install pyzipper==0.3.6
python tools/build_arg_site_assets.py
7z l -slt arg-site/drop/hoang-hon-song-sai-gon.jpg
```

Ba mục trong ZIP phải có phương thức AES-256. Script in SHA-256 của ảnh dép từ byte nguồn cố định. Giữ nguyên tệp nguồn khi dựng lại để kết quả đối chiếu trên máy chủ ổn định.

## Cấu hình máy chủ

Mã băm SHA-256 hiện tại của `red-thread-sandal.jpg` là `8753395d6eba4a2133ef231c152e99a77e93965eea896ddb4ab95da6aa03684b`.

Sau khi triển khai API câu đố và tạo mục `red-thread-sandal`, cấu hình lời giải trong cơ sở dữ liệu của môi trường tương ứng.

```sql
INSERT INTO puzzle_solutions (puzzle_id, sha256, note, created_at)
VALUES (
    'red-thread-sandal',
    '8753395d6eba4a2133ef231c152e99a77e93965eea896ddb4ab95da6aa03684b',
    'Ảnh chiếc dép chỉ đỏ trong tủ giày nhà Linh (23:40 08/11)',
    NOW()
);

UPDATE puzzles
SET is_enabled = true, updated_at = NOW()
WHERE id = 'red-thread-sandal';
```
