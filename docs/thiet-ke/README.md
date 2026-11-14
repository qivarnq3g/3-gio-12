# Tài liệu thiết kế

Thư mục này chứa nguồn của bản thuyết minh chi tiết thiết kế nộp Moodle trước 16/11/2026 09:07 (task #13). Mỗi phần là một tệp Markdown riêng; script `tools/build_design_doc.py` ghép chúng thành một tệp Word.

## Các tệp

| Tệp | Chương trong bản nộp | Task | Phụ trách |
|---|---|---|---|
| `thuyet-minh.md` | Bìa, 1. Giới thiệu (gồm thay đổi so với bản đăng ký) | #13 | Quang |
| `luoc-do-use-case.md` | 2. Lược đồ use case | #9 | Thắng |
| `kien-truc-he-thong.md` | 3. Kiến trúc hệ thống | #10 | Quang |
| `luoc-do-lop-game.md` | 4. Lược đồ lớp phần game | #11 | Phúc |
| `luoc-do-lop-may-chu.md` | 5. Lược đồ lớp phần máy chủ | #12 | Quang |
| `database-design.md` | 6. Thiết kế cơ sở dữ liệu | #12 | Quang |
| `admin-ui-design.md` | 7. Thiết kế trang quản trị | #27 | Quang |
| `conclusion.md` | 8. Kết luận | #13 | Quang |

Tệp chưa có thì bản Word hiện chương đó với dòng "Phần này đang được soạn".

## Cách viết một phần

- Dòng đầu là tiêu đề cấp 1 (`# Lược đồ use case`). Đoạn giữa tiêu đề cấp 1 và tiêu đề cấp 2 đầu tiên là ghi chú cho nhóm (task, người duyệt, nguồn); script bỏ đoạn này khỏi bản nộp.
- Tiêu đề cấp 2, 3 đánh số trong phần (`## 1. Tác nhân`, `### 1.1. Người chơi`). Script tự thêm số chương phía trước, ví dụ `4.1.`, và đổi "mục 1.1" trong cùng tệp thành "mục 4.1.1".
- Sơ đồ viết bằng khối ` ```mermaid `, hoặc là ảnh PNG xuất từ draw.io đặt trên một dòng riêng (`![...](lop-game/ten-hinh.png)`); bản vẽ `.drawio` để cạnh ảnh. Script vẽ khối Mermaid thành ảnh, đặt tên "Hình <chương>.<số>. <tên mục chứa sơ đồ>". Mọi trang đều dọc; hình được co cho vừa khung 15 x 21 cm, nên sơ đồ rộng phải vẽ theo chiều dọc. Với Mermaid: sơ đồ có nhiều khung con dùng `%%{init: {"layout": "elk"}}%%` để các khung xếp chồng; sơ đồ tuần tự dùng `wrap`, `actorMargin`, `width` nhỏ trong khối `init` và ngắt dòng đường dẫn dài bằng `<br/>` (để tự ngắt thì đường dẫn bị chèn gạch nối).
- Hình draw.io xuất ở tỉ lệ 200% với chữ 12 px chỉ in ra 9 pt khi script không phải co hình: bản vẽ (cả viền) tối đa khoảng 567 x 794 px; tới 640 px chiều ngang chữ vẫn còn khoảng 8 pt. Lớn hơn thì dàn gọn, ngắt chữ ký hàm dài xuống dòng hoặc tách thành hai hình ở hai mục con, rồi xem trang A4 ở 100% trước khi đẩy.
- Nhắc tới phần khác thì ghi số task của phần đó (`task #11`); script đổi thành "chương 5". Nhắc tới task khác (#4, #18, #25...), hay ghi số issue trần như `#20` ngoài khối mã, thì script báo lỗi: viết lại câu cho người đọc ngoài nhóm hiểu. Số issue chỉ để trong đoạn ghi chú cho nhóm ở đầu tệp.
- Liên kết tới tệp trong repo thành chữ thường trong bản Word, nên câu phải đọc được khi không bấm vào.
- Chính tả và trình bày theo quy ước chung: tiêu đề chỉ viết hoa chữ đầu và tên riêng, không in đậm trong câu, không dùng `--` hay dấu gạch dài.

## Dựng bản Word

Cần Python 3.10 trở lên với `python-docx`, Pandoc 3 và Mermaid CLI:

```bash
python -m pip install python-docx
npm install -g @mermaid-js/mermaid-cli
python tools/build_design_doc.py
```

Trên Windows có Microsoft Word, thêm `--word` để Word cập nhật mục lục, danh mục hình, danh mục bảng rồi xuất thêm PDF cạnh tệp Word; bản dựng không có `--word` để Word tự hỏi cập nhật trường khi mở.

- Chú thích bảng: viết một dòng `: Tên bảng` ngay sau bảng; script tự đánh số "Bảng <chương>.<số>." và đặt chú thích phía trên bảng. Bảng thiếu chú thích không vào danh mục bảng.
- Độ rộng cột: dòng phân cách có số gạch bằng nhau (`|---|---|`) thì Word tự co giãn cột. Bảng có chuỗi mã dài (tên lớp, đường dẫn API) thì ghi tỉ lệ bằng số gạch, ví dụ `|----------------------------------------|------------------------|------------------------------------|` cho 40, 24, 36 phần trăm; script đặt độ rộng cố định theo tỉ lệ đó trên khổ 16 cm. Trong bảng, script chèn chỗ ngắt dòng vô hình sau `.`, `/` và `,` trong đoạn mã, và không để một hàng bị tách qua hai trang.
- Trích dẫn: nguồn nằm trong `references.json` (CSL JSON, nhập được vào Zotero); trong văn bản viết `[@khóa]`, ví dụ `[@uml]`. Pandoc định dạng theo `ieee.csl` (lấy từ kho kiểu trích dẫn của Zotero, giấy phép CC BY-SA 3.0) và tạo mục "Tài liệu tham khảo" ở cuối.
- Danh mục chữ viết tắt lấy từ `abbreviations.md`; thêm chữ viết tắt mới vào đó theo thứ tự chữ cái.

Hai trang bìa lấy từ mẫu của trường `docs/thiet-ke/mau-bia.docx` (bìa chính có khung viền và logo, bìa phụ). Tên học phần, mã lớp, tiêu đề, tên đề tài và tháng năm lấy từ bốn khối `Cover Course`, `Cover Title`, `Cover Subtitle`, `Cover Date` ở đầu `thuyet-minh.md`. MSSV và họ tên không nằm trong Git, vì repo công khai. Tạo tệp `docs/thiet-ke/thanh-vien.local.txt` (đã có trong `.gitignore`), mỗi dòng `MSSV - Họ và tên`, theo thứ tự muốn in trên bìa. Thiếu tệp thì bìa giữ dấu chấm và script báo thiếu.

Kết quả ở `docs/thiet-ke/build/thuyet-minh-thiet-ke.docx` (thư mục `build/` không đưa lên Git). Script in ra các phần còn thiếu và các chỗ còn nhắc task cần sửa; còn chỗ cần sửa thì mã thoát là 1.

Mermaid CLI cần một trình duyệt Chromium. Script tự dùng Edge hoặc Chrome đã cài trên máy; muốn chỉ định trình duyệt khác thì đặt biến môi trường `MERMAID_BROWSER` là đường dẫn tới tệp chạy của trình duyệt.

Mở tệp Word lần đầu, Word hỏi cập nhật trường: chọn Yes để có mục lục và số trang. Trước khi nộp, xuất PDF từ Word.
