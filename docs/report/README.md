# Báo cáo cuối kỳ

Nguồn của báo cáo cuối kỳ nộp Moodle trước 03/12/2026 09:07 (task #35, dựng bằng task #137). Định dạng theo hướng dẫn trình bày của trường: `formatting-guide.pdf` trong thư mục này.

## Cấu trúc

| Phần của báo cáo | Nguồn | Người phụ trách |
|---|---|---|
| Bìa | Khối `Cover ...` đầu `bao-cao.md`, mẫu `docs/thiet-ke/mau-bia.docx`, tên và MSSV ở `docs/thiet-ke/thanh-vien.local.txt` | Quang |
| Mục lục, danh mục chữ viết tắt, bảng, hình | Tự sinh; chữ viết tắt ở `docs/thiet-ke/abbreviations.md` | Quang |
| Lời mở đầu | `bao-cao.md` | Quang |
| Chương 1. Tổng quan đề tài | `chuong-1-tong-quan.md` | Quang |
| Chương 2. Phân tích và thiết kế | Ghép từ sáu tệp của thuyết minh thiết kế trong `docs/thiet-ke/` (use case, kiến trúc, lớp game, lớp máy chủ, cơ sở dữ liệu, trang quản trị) | Quang, Phúc, Thắng |
| Chương 3. Hiện thực và kiểm thử | `chuong-3-hien-thuc-kiem-thu.md`, mỗi mục có người phụ trách trong chú thích HTML | Phúc (game), Tiên (3D), Thắng (kiểm thử), Quang (máy chủ, triển khai) |
| Chương 4. Kế hoạch khởi nghiệp | `ke-hoach-khoi-nghiep.md` | Quang |
| Kết luận | `ket-luan.md` | Quang |
| Tài liệu tham khảo | Tự sinh từ `docs/thiet-ke/references.json` bằng `ieee-sap-xep.csl` | Quang |
| Phụ lục A, B, C | `phu-luc.md` | Quang (A, C), Tiên (B) |

Chương 2 dùng lại nguyên văn các phần của thuyết minh thiết kế đã nộp, chỉ đổi cấp tiêu đề và số mục (ví dụ phần "Kiến trúc hệ thống" thành mục 2.2). Sửa nội dung thiết kế thì sửa ở `docs/thiet-ke/`, không sửa ở đây.

## Viết một phần

- Đầu mỗi tệp chương có dòng `# Tiêu đề`, rồi (nếu cần) một đoạn bắt đầu bằng "Ghi chú cho nhóm". Script bỏ cả hai khi dựng; tên chương và số chương do script thêm.
- Dùng `##` cho mục (script đánh số 3.1, 3.2), `###` cho nhóm tiểu mục, `####` cho tiểu mục. Không viết số trước tiêu đề. Cấp sâu hơn bị chuyển thành chữ đậm.
- Bảng: dòng `: Tên bảng` ngay sau bảng; script ghi "Bảng <chương>.<số>" phía trên bảng. Hình: khối Mermaid hoặc ảnh PNG trên một dòng riêng; script ghi "Hình <chương>.<số>" phía dưới hình.
- Nhắc tới bảng hoặc hình bằng số ("xem bảng 4.3"), đúng thứ tự xuất hiện trong chương; không viết "bảng sau đây". Nhắc tới mục khác trong cùng tệp bằng số cục bộ ("mục 5"); script đổi thành "mục 4.5".
- Trích dẫn nguồn bằng `[@khóa]`, khóa nằm trong `docs/thiet-ke/references.json`.
- Không ghi số issue hoặc pull request (`#12`) trong nội dung; script báo lỗi. Số issue chỉ đặt trong đoạn "Ghi chú cho nhóm".
- Muốn ghi chú cho người viết mà không đưa vào bản nộp: dùng chú thích HTML `<!-- ... -->`.
- Mục chưa viết ghi đúng dòng `Phần này đang được soạn.`; script đếm và báo số mục còn lại.

## Dựng bản Word

Cần Python 3.10 trở lên với `python-docx`, Pandoc 3, Mermaid CLI và Git LFS (ảnh sơ đồ trong `docs/thiet-ke/` là tệp LFS: chạy `git lfs pull` trước, nếu không hình sẽ trống).

```bash
python tools/build_final_report.py            # ghi docs/report/build/bao-cao-cuoi-ky.docx
python tools/build_final_report.py --word     # Windows có Word: cập nhật mục lục và xuất thêm PDF
```

Mở tệp Word lần đầu, chọn Yes khi Word hỏi cập nhật trường (nếu không dùng `--word`). Trước khi nộp, xuất PDF từ Word.

Script in các phần còn thiếu (tệp chưa có, mục "Phần này đang được soạn.", thiếu tên sinh viên ở bìa) và thoát mã 1 nếu còn nhắc số issue trong nội dung.

## Định dạng theo hướng dẫn của trường

Lề trên 3,5 cm, dưới 3 cm, trái 3,5 cm, phải 2 cm; Times New Roman cỡ 13; giãn dòng 1,5; tên chương in hoa, đậm; mục (1.1) đậm; nhóm tiểu mục (1.1.1) đậm nghiêng; tiểu mục (1.1.1.1) nghiêng; mục lục tới cấp ba. Số trang ở giữa chân trang: số La Mã thường từ Mục lục tới hết các danh mục, số thường từ Lời mở đầu; bìa không đánh số. Bảng đánh số theo chương, tên bảng phía trên; hình đánh số theo chương, tên hình phía dưới. Tài liệu tham khảo xếp theo khối ngôn ngữ (tiếng Việt trước), trong khối theo bảng chữ cái, đánh số liên tục.

Hai chỗ nhóm chọn, không do hướng dẫn quy định: văn bản trong ô của bảng và chú thích dùng giãn dòng đơn để bảng không quá cao; trang bìa lấy nguyên từ mẫu của trường (`mau-bia.docx`).

Chưa làm: trích dẫn qua Zotero (`--zotero` của script thuyết minh); báo cáo dùng trích dẫn tĩnh của Pandoc.
