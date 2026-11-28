# Slide thuyết trình

Nguồn slide là `slides.md`; `tools/build_slides.py` dựng thành `docs/slide/build/3-gio-12.pptx`. Sửa nội dung ở `slides.md`, không sửa tệp `.pptx` (nó bị bỏ qua khỏi Git và sẽ bị ghi đè khi dựng lại).

```bash
pip install python-pptx pillow lxml
python tools/build_slides.py
```

Cú pháp (thẻ, ô số liệu, sơ đồ luồng, ảnh) ở đầu `tools/build_slides.py`. Mỗi slide có chuyển cảnh mờ; các phần tử xuất hiện lần lượt khi vào slide. Lời dẫn cho người trình bày viết ở dòng bắt đầu bằng `>` (thành ghi chú, không hiện trên màn hình).

## Ảnh trong slide

| Ảnh | Nguồn | Ghi chú |
|---|---|---|
| `anh/dien-thoai-ngan.png` | Ảnh chụp từ game (`docs/game/may-ngan-night01.png`) | Thật |
| `anh/cong-an-phuong.jpg` | Ảnh chụp trong Unity của trụ sở công an phường (nhánh #138) | Thật |
| `anh/website-cau-do.png` | Chụp `arg-site/index.html` bằng Edge không giao diện | Thật |
| `anh/hem-3am.png` | `game/Assets/Art/Backgrounds/hem-3am.png`: ảnh tham chiếu bối cảnh tạo bằng AI | Dùng làm nền bìa và slide cuối; slide bìa ghi rõ; không phải ảnh trong game |
| `docs/thiet-ke/**.png` | Sơ đồ UML của nhóm | Cần `git lfs pull` |

Chưa dùng ảnh Đêm 2 và Đêm 3 từ nhánh của Phúc vì đó là giao diện tạm cho bản thô. Khi giao diện điện thoại hoàn chỉnh, chụp lại và thay vào slide 2 hoặc 3.

## Việc còn thiếu trước khi trình bày

| Việc | Người | Hạn |
|---|---|---|
| Thêm slide kết quả test và chơi thử (mẫu ở `slide-bo-sung.md`) khi có số liệu thật | Thắng, Quang | 01/12 |
| Thay ảnh chụp game bằng ảnh đẹp hơn nếu có (bản chơi thử mới) | Thắng hoặc Phúc | 30/11 |
| Cập nhật tên miền thật và kết quả kiểm sau triển khai (slide "Một máy ảo, ba container") | Quang | 01/12 |
| Cập nhật số issue, pull request, commit (slide "Làm việc có kiểm soát") | Quang | 02/12 |
| Phân người trình bày từng slide và tập canh giờ | Cả nhóm | 02/12 |

Đếm lại issue, pull request, commit:

```bash
gh issue list --state all --limit 300 --json state | python -c "import json,sys,collections; print(collections.Counter(i['state'] for i in json.load(sys.stdin)))"
gh pr list --state all --limit 300 --json state | python -c "import json,sys,collections; print(collections.Counter(i['state'] for i in json.load(sys.stdin)))"
git rev-list --count origin/main
```

Ảnh `.png` và `.jpg` trong repo do Git LFS quản lý; chạy `git lfs pull` trước khi dựng, nếu không ảnh sẽ trống.
