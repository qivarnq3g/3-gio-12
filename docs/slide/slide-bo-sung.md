# Slide thêm khi có số liệu thật

Hai slide dưới đây chưa đưa vào `slides.md` vì chưa có số liệu hay ảnh thật. Khi có, chép vào `slides.md` ở vị trí gợi ý, bỏ dòng ghi chú này, rồi chạy lại `python tools/build_slides.py`.

## Sau slide "Kiểm thử ở ba tầng": kết quả chạy test case

Điền số thật từ cột "Kết quả" của `docs/test-case.md` (người chạy: Thắng).

```
---

# Kết quả kiểm thử
~ KIỂM THỬ
::: stats
- <số đạt> | test case đạt | trên tổng 33
- <số lỗi> | lỗi tìm được | <số đã sửa> đã sửa
:::
> Chỉ ghi số đã chạy thật, kèm ngày và phiên bản bản dựng.
```

## Sau slide "Kiểm thử ở ba tầng": chơi thử kín

Cần ảnh dashboard có số liệu từ lượt chơi thử, lưu vào `docs/slide/anh/dashboard-so-lieu.png` (Thắng).

```
---

# Chơi thử kín: người chơi làm gì
~ KIỂM THỬ
- <số người chơi thử> người ngoài nhóm đã chơi
- <đêm nhiều người bỏ nhất> là chỗ bỏ nhiều nhất; nhóm đã sửa <điều đã sửa>
![Dashboard thống kê sau lượt chơi thử](docs/slide/anh/dashboard-so-lieu.png)
> Chỉ nói số có thật từ dashboard.
```
