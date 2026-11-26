# Slide thuyết trình

Nguồn slide là `slides.md`; `tools/build_slides.py` dựng thành `docs/slide/build/3-gio-12.pptx`. Sửa nội dung ở `slides.md`, không sửa tệp `.pptx` (nó bị bỏ qua khỏi Git và sẽ bị ghi đè khi dựng lại).

```bash
pip install python-pptx pillow
python tools/build_slides.py
```

Cách viết slide ở đầu `tools/build_slides.py`. Ý chính: ít chữ, mỗi slide một ý; lời dẫn viết trong dòng bắt đầu bằng `>` (thành ghi chú của người trình bày, không hiện trên màn hình).

## Việc còn thiếu trước khi trình bày

Script liệt kê ảnh chưa có (khung viền đỏ trên slide). Cần:

| Việc | Người | Hạn |
|---|---|---|
| Ảnh chụp thật từ bản chơi thử cho slide 2: `docs/slide/anh/man-choi-dien-thoai.png` | Thắng hoặc Phúc | 30/11 |
| Ảnh dashboard có số liệu từ lượt chơi thử cho slide "Chơi thử kín": `docs/slide/anh/dashboard-so-lieu.png` | Thắng | 01/12 |
| Số test case đạt, không đạt, số lỗi tìm được (slide "Kiểm thử", ghi chú có dòng THÊM) | Thắng | 01/12 |
| Tên miền thật và kết quả kiểm sau triển khai (slide "Triển khai") | Quang | 01/12 |
| Cập nhật số issue và pull request (slide "Quy trình làm việc nhóm") | Quang | 02/12 |
| Phân người trình bày từng slide và tập canh giờ | Cả nhóm | 02/12 |

Đếm lại issue và pull request:

```bash
gh issue list --state all --limit 300 --json state | python -c "import json,sys,collections; print(collections.Counter(i['state'] for i in json.load(sys.stdin)))"
gh pr list --state all --limit 300 --json state | python -c "import json,sys,collections; print(collections.Counter(i['state'] for i in json.load(sys.stdin)))"
```

Ảnh `.png` lớn trong `docs/` do Git LFS quản lý; chạy `git lfs pull` trước khi dựng, nếu không ảnh sơ đồ sẽ trống.
