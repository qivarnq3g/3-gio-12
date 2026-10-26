# Quy ước làm việc

## Nhánh

- `main` luôn chạy được. Không commit thẳng vào `main`.
- Tạo nhánh từ issue, đặt tên `<loai>/<so-issue>-<mo-ta-ngan>`, ví dụ `feat/12-player-movement`, `fix/30-save-crash`, `docs/5-use-case`, `art/8-phong-tro`.

## Commit

Viết ngắn, ở thì hiện tại, bắt đầu bằng loại việc:

- `feat:` tính năng mới
- `fix:` sửa lỗi
- `art:` thêm hoặc sửa asset
- `docs:` tài liệu, biên bản
- `test:` kiểm thử
- `chore:` cấu hình, dọn dẹp

Ví dụ: `feat: thêm ẩn nấp trong tủ (#14)`.

## Pull request

1. Mở pull request vào `main`, ghi `Đóng #<số issue>` để issue tự đóng khi gộp.
2. Ít nhất một người khác duyệt. Người duyệt chạy thử, không chỉ đọc.
3. Người mở pull request tự gộp sau khi được duyệt, rồi xóa nhánh.

## Bảng việc

- Nhận việc: tự gán mình vào issue, kéo sang **Đang làm**.
- Mở pull request: kéo sang **Đang duyệt**.
- Gộp xong: issue tự sang **Xong**.
- Quá hạn hoặc bị vướng: bình luận vào issue và báo trong buổi họp tuần.

## Asset

- Ảnh, âm thanh, model đi qua Git LFS (đã cấu hình trong `.gitattributes`). Cài một lần: `git lfs install`.
- File gốc nặng (`.blend`, `.psd`, ảnh AI gốc) để trên Drive của nhóm, trong repo chỉ có file đã xuất cho Unity.
- Ghi nguồn và giấy phép của mọi asset lấy từ bên ngoài vào `game/Assets/Art/_Source/NGUON.md`.

## Họp

Mỗi tuần một buổi. Người ghi biên bản chép từ `docs/bien-ban/_mau.md` thành `docs/bien-ban/yyyy-mm-dd.md` và mở pull request trong ngày.
