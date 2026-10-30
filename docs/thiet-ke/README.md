# Tài liệu thiết kế

Thư mục này chứa nguồn của bản thuyết minh chi tiết thiết kế nộp Moodle trước 16/11/2026 09:07 (task #13). Mỗi phần là một tệp Markdown riêng; script `tools/build_design_doc.py` ghép chúng thành một tệp Word.

## Các tệp

| Tệp | Chương trong bản nộp | Task | Phụ trách |
|---|---|---|---|
| `thuyet-minh.md` | Bìa, 1. Giới thiệu, 2. Thay đổi so với bản đăng ký | #13 | Quang |
| `kien-truc-he-thong.md` | 3. Kiến trúc hệ thống | #10 | Quang |
| `luoc-do-use-case.md` | 4. Lược đồ use case | #9 | Thắng |
| `luoc-do-lop-game.md` | 5. Lược đồ lớp phần game | #11 | Phúc |
| `luoc-do-lop-may-chu.md` | 6. Lược đồ lớp phần máy chủ và web | #12 | Quang |

Tệp chưa có thì bản Word hiện chương đó với dòng "Phần này đang được soạn".

## Cách viết một phần

- Dòng đầu là tiêu đề cấp 1 (`# Lược đồ use case`). Đoạn giữa tiêu đề cấp 1 và tiêu đề cấp 2 đầu tiên là ghi chú cho nhóm (task, người duyệt, nguồn); script bỏ đoạn này khỏi bản nộp.
- Tiêu đề cấp 2, 3 đánh số trong phần (`## 1. Tác nhân`, `### 1.1. Người chơi`). Script tự thêm số chương phía trước, ví dụ `4.1.`, và đổi "mục 1.1" trong cùng tệp thành "mục 4.1.1".
- Sơ đồ viết bằng khối ` ```mermaid `. Script vẽ thành ảnh, đặt tên "Hình <chương>.<số>. <tên mục chứa sơ đồ>". Sơ đồ quá rộng được đặt trên trang ngang; sơ đồ rộng hơn khoảng ba lần chiều cao vẫn ra chữ nhỏ, nên vẽ theo chiều dọc (`flowchart TB`, `direction TB`) hoặc tách đôi.
- Nhắc tới phần khác thì ghi số task của phần đó (`task #11`); script đổi thành "chương 5". Nhắc tới task khác (#4, #18, #25...) thì script báo lỗi: viết lại câu cho người đọc ngoài nhóm hiểu.
- Liên kết tới tệp trong repo thành chữ thường trong bản Word, nên câu phải đọc được khi không bấm vào.
- Chính tả và trình bày theo quy ước chung: tiêu đề chỉ viết hoa chữ đầu và tên riêng, không in đậm trong câu, không dùng `--` hay dấu gạch dài.

## Dựng bản Word

Cần Python 3.10 trở lên với `python-docx`, Pandoc 3 và Mermaid CLI:

```bash
python -m pip install python-docx
npm install -g @mermaid-js/mermaid-cli
python tools/build_design_doc.py
```

Kết quả ở `docs/thiet-ke/build/thuyet-minh-thiet-ke.docx` (thư mục `build/` không đưa lên Git). Script in ra các phần còn thiếu và các chỗ còn nhắc task cần sửa; còn chỗ cần sửa thì mã thoát là 1.

Mermaid CLI cần một trình duyệt Chromium. Script tự dùng Edge hoặc Chrome đã cài trên máy; muốn chỉ định trình duyệt khác thì đặt biến môi trường `MERMAID_BROWSER` là đường dẫn tới tệp chạy của trình duyệt.

Mở tệp Word lần đầu, Word hỏi cập nhật trường: chọn Yes để có mục lục và số trang. Trước khi nộp, điền MSSV và họ tên ở bìa, rồi xuất PDF từ Word.
