# Nghiệm thu giường tầng sắt và giường gỗ dát nan

Ngày kiểm tra 18/11/2026. Hai FBX dựng lại đạt các tiêu chí kỹ thuật trong Unity 6000.6.7f1. Ảnh PNG được kết xuất từ FBX đã nhập lại vào Blender 5.2.3 LTS. Mẫu 3 dùng lại FBX và PNG gốc, đã đối chiếu mã băm trước và sau lượt dựng.

[Ảnh tổng hợp ba mẫu và hai góc nhìn từ dưới gầm](contact_sheet.jpg) · [Số đo và mã băm FBX](bed_props_report.json)

## Đối chiếu với ảnh tham chiếu

- **Mẫu 1.** Khung ống kem ngà có bốn chụp chân cao su, bản mã, đầu bu-lông, gờ hàn và vết xước sơn. Thang hông có hai gióng móc qua dầm trên, bốn bậc tròn và các khớp nối sẫm màu. Lan can có góc uốn và thanh chống đứng. Bạt xanh có bốn dây buộc, viền nổi và nếp võng 95,6 mm tính từ cao độ góc buộc. Tầng dưới có chiếu sọc đỏ, xanh lá và xanh dương trên nền cói nâu be, bọc viền đỏ và kê trên chín nan thép hộp. Tầng trên có đệm mỏng viền đỏ và đồ gấp. Khăn rủ qua móc nhôm, còn ba lớp chăn xếp nằm sát sàn trong góc gầm.
- **Mẫu 2.** Bốn chân gỗ vuông danh nghĩa 70 × 70 mm có cạnh vát, chốt mộng và đầu đinh. Vai và thành đầu giường dùng gỗ nâu sẫm. Thành đầu giường cao hơn mặt dát danh nghĩa 229 mm. Hai đố trong đỡ ba xà ngang. Hai mươi bốn nan dọc có đầu vát, rãnh thớ chạy theo chiều xẻ và ba tông gỗ. Độ lệch ngang tuyệt đối là 3,0–4,6 mm, biến thiên chiều dài tuyệt đối là 10,0–14,9 mm. Mười hai nan nghiêng ngang 0,56–0,98°.

Cách bố trí dựa trên ảnh người dùng cung cấp. Chiều sâu các cấu kiện khuất và độ so le của từng nan được dựng theo đặc tả. Các chi tiết bề mặt nhỏ như sợi vải, độ nhám sơn và vân mịn trong ảnh xem trước dùng thiết lập ánh sáng và vật liệu của Blender. Bản Unity dùng vật liệu Toon hiện có, với thớ khắc, khe nan, mảng màu và nếp vải trên lưới xuất. Lượt này kiểm tra nhập Unity và xem ảnh Blender, chưa đánh giá hai mẫu trong ánh sáng của cảnh chơi.

## Kết quả đo trực tiếp trong Unity

| Tiêu chí | Mẫu 1 | Mẫu 2 |
|---|---|---|
| Kích thước X × Y × Z (m) | 1,0255 × 1,7000 × 2,0020 | 1,3010 × 0,6080 × 2,0012 |
| `bounds.min.y` (m) | 0,0000 | 0,0000 |
| Tâm bao XZ trong dung sai 0,000001 m | Đạt | Đạt |
| Vị trí và quay bằng 0, tỷ lệ bằng 1 ở nút gốc và các nút con | Đạt | Đạt |
| Số tam giác sau khi Unity nhập | 5.988 | 5.136 |
| Số lưới kết xuất | 121 | 56 |
| UV Smart UV Project nằm trong [0, 1] | Đạt | Đạt |
| Vật liệu tham chiếu tới `Assets/Art/Materials/` | 12 vật liệu `Toon/Toon` | 4 vật liệu `Toon/Toon` |

Một lượt đọc FBX độc lập với generator kiểm tra lại số tam giác, diện tích mặt và diện tích UV của từng tam giác. Ba lưới bạt, khăn và chiếu đều kín theo số mặt kề trên mỗi cạnh. Độ chênh lớn nhất trên toàn lớp lưới bạt, tính cả độ dày, là 97,2 mm. Các kích thước trong bảng bao gồm phụ kiện. Riêng mẫu 1 có khung danh nghĩa rộng 0,94 m và chiều rộng bao 1,0255 m tính đến chân thang.

`ArtPipelineCheck.Run` trả mã 1 khi quét toàn dự án. Log ghi lỗi ở `env_alley.fbx` và `env_main_road.fbx`, gồm lưới thiếu UV, vật liệu và tỷ lệ nút con. Lượt kiểm tra riêng hai FBX giường trả mã 0.

## Lệnh kết xuất

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python-exit-code 1 --python tools/generate_bed_props.py -- --render --only prop_bed_bunk_iron prop_bed_wood_slats
```

Khi chạy với `--only`, generator cập nhật các mục tương ứng trong báo cáo JSON và giữ mục mẫu 3.
