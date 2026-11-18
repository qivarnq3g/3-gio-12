## Kết luận nghiệm thu

Chưa nghiệm thu cuối cùng cả ba mô hình. Phần kỹ thuật Unity đạt hầu hết yêu cầu, nhưng độ tương đồng ảnh thật và một số bề mặt chồng cần sửa trước khi chốt tài sản.

### Phát hiện cần xử lý

1. Mức cao - độ trung thực của giường tầng chưa đạt.

   - Ảnh thật có bạt xanh võng ở mặt dưới tầng trên, chiếu cói sọc đỏ/xanh ở tầng dưới và mặt tầng trên ngả đỏ/trắng.
   - Bản dựng dùng nệm xanh trơn rất lớn ở tầng trên, khiến bạt xanh đặc trưng bị hiểu thành nệm. Chiếu dưới chỉ là mặt nâu phẳng với viền đỏ, thiếu hệ sọc nổi bật.
   - Khăn và móc có trong hình học nhưng quá nhỏ, cứng và không thấy được trong ảnh kiểm tra.
   - Tài liệu hiện mô tả chi tiết đầy đủ hơn kết quả trực quan thực tế tại [NGUON.md](D:/DACNPM/3-gio-12/game/Assets/Art/_Source/NGUON.md:63).

2. Mức trung bình - nệm chiếu trúc thiếu đặc trưng của ảnh.

   - Bố cục nệm và chiếu trúc nhận diện tốt.
   - Ga và gối chỉ dùng màu phẳng, chưa có hoa đỏ/hồng như ảnh.
   - Gối ôm xanh dài đặt trên mặt chiếu không giống ảnh; ảnh có gối xanh nhỏ nằm trên cụm gối hoa ở đầu giường.
   - Mô tả “ga hồng hoa văn” trong [NGUON.md](D:/DACNPM/3-gio-12/game/Assets/Art/_Source/NGUON.md:65) chưa đúng với tài sản hiện tại.

3. Mức trung bình - có bề mặt đồng phẳng chồng nhau.

   - Các thanh viền chiếu giao nhau tại góc với mặt trên đồng phẳng tại [generate_bed_props.py](D:/DACNPM/3-gio-12/tools/generate_bed_props.py:434).
   - Các rãnh dọc và ngang của chiếu trúc cũng chồng mặt tại mọi giao điểm tại [generate_bed_props.py](D:/DACNPM/3-gio-12/tools/generate_bed_props.py:445).
   - Viền đỏ chiếu tầng dưới có cấu trúc tương tự. Ảnh render đã xuất hiện các góc đen bất thường. Nên ghép thành một lưới hoặc cắt giao tuyến để loại nguy cơ z-fighting.

4. Mức trung bình - bộ kiểm tra Scale trong mã có lỗi logic.

   Tại [generate_bed_props.py](D:/DACNPM/3-gio-12/tools/generate_bed_props.py:568):

   ```python
   assert max(abs(x - 1) < 1e-6 for x in p.props[-3:])
   ```

   Điều kiện này chỉ cần một trục có Scale = 1 là đạt. Cần dùng:

   ```python
   assert all(abs(x - 1) < 1e-6 for x in p.props[-3:])
   ```

   Các FBX hiện tại vẫn có Scale đúng trên cả ba trục; lỗi nằm ở khả năng phát hiện hồi quy sau này.

5. Mức thấp - cảnh báo cú pháp và mã thừa.

   - `python -Wall -m py_compile` báo `DeprecationWarning: invalid escape sequence '\P'` tại đường dẫn Windows trong docstring đầu tệp. Nên dùng raw docstring hoặc dấu `/`.
   - `towel_pts` và `bolster_center` được khai báo nhưng không sử dụng tại [generate_bed_props.py](D:/DACNPM/3-gio-12/tools/generate_bed_props.py:311) và [generate_bed_props.py](D:/DACNPM/3-gio-12/tools/generate_bed_props.py:461).
   - Mã có xử lý trùng đỉnh, mặt suy biến và pháp tuyến, nhưng chưa có kiểm tra tự động cho mặt đồng phẳng, mặt trùng hoặc cạnh non-manifold.

6. Mức thấp - tài liệu sai đường dẫn đầu ra tùy chọn.

   [NGUON.md](D:/DACNPM/3-gio-12/game/Assets/Art/_Source/NGUON.md:71) nói `--save-blend` ghi vào `art-source/issue96/`, trong khi mã thực tế ghi vào `game/Assets/Art/_Source/`.

### Kết quả kiểm tra kỹ thuật

| Tiêu chí | Giường tầng | Giường gỗ | Nệm chiếu trúc |
|---|---:|---:|---:|
| Kích thước Unity, m | 0,949 × 1,715 × 2,0034 | 1,300 × 0,680 × 2,000 | 1,200 × 0,362 × 1,950 |
| `Bounds.min.y` | 0,000 | 0,000 | 0,000 |
| Đối xứng tâm XZ | Đạt | Đạt | Đạt |
| Root và child Identity | Đạt, 56 transform | Đạt, 31 transform | Đạt, 50 transform |
| Tam giác | 1.516 | 1.032 | 2.676 |
| UV đầy đủ trong [0, 1] | Đạt | Đạt | Đạt |
| Mặt suy biến | 0 | 0 | 0 |
| Cạnh non-manifold trên 2 mặt | 0 | 0 | 0 |
| Vật liệu có sẵn | Đạt | Đạt | Đạt |
| Shader | `Toon/Toon` | `Toon/Toon` | `Toon/Toon` |

Giường tầng có 36 cạnh biên thuộc bạt vải hở hai mặt, không phải lỗ thủng ngoài ý muốn của một khối kín. Không phát hiện pháp tuyến đảo rõ ràng sau khi nhập lại FBX; mã cũng đã tính lại pháp tuyến tại [generate_bed_props.py](D:/DACNPM/3-gio-12/tools/generate_bed_props.py:494).

Kiểm tra trực tiếp trong Unity xác nhận mọi material slot đều trỏ về đúng `.mat` trong `Assets/Art/Materials/`; không phát sinh vật liệu mới. Mã băm SHA-256 và kích thước của ba FBX khớp hoàn toàn với [bed_props_report.json](D:/DACNPM/3-gio-12/tools/out/beds/bed_props_report.json).

### Đánh giá từng mô hình

- `prop_bed_bunk_iron`: khung, tỷ lệ, thang và lan can nhận diện đúng kiểu giường KTX; cần sửa mạnh nệm tầng trên, chiếu sọc, bạt võng và khăn/móc.
- `prop_bed_wood_slats`: là mô hình gần ảnh nhất; cấu trúc bốn chân, vai giường và nan dọc đúng. Nên tăng số nan, tạo sai lệch nhẹ giữa các nan, giảm độ cao tấm đầu giường và thêm biến thiên màu gỗ cũ.
- `prop_bed_mattress_bamboo`: hình khối nệm, viền và lưới chiếu trúc tốt; cần sửa bố cục gối, bổ sung hoa văn bằng vật liệu có sẵn hoặc hình học tiết chế, đồng thời loại các mặt chồng.

Khuyến nghị nghiệm thu: sửa các mục 1-4 rồi xuất lại FBX và ảnh kiểm tra. Sau đó có thể nghiệm thu kỹ thuật và mỹ thuật trong một lượt cuối. Không có tệp dự án nào bị sửa trong lượt rà soát này; các tệp kiểm tra tạm đã được dọn sạch.