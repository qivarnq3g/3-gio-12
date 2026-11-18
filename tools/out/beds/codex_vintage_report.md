# Nghiệm thu ba mẫu giường phòng trọ Sài Gòn

Ngày 18/11/2026, cả ba mẫu đã được kết xuất lại bằng Blender 5.2.3 LTS và kiểm tra nhập trong Unity 6000.6.7f1. Bản dựng thể hiện đồ dùng lâu năm trong phòng trọ Sài Gòn cuối thập niên 1990 và đầu những năm 2000 qua mảng gỉ, gỗ chắp vá và vải bạc màu.

[Ảnh tổng hợp năm góc nhìn](contact_sheet.jpg) · [Số đo và mã băm FBX](bed_props_report.json) · [Hồ sơ nguồn](../../../game/Assets/Art/_Source/NGUON.md)

## Chi tiết mỹ thuật

### Giường tầng sắt

- Khung xám kem xỉn dùng `M_WallPlinthGray` và `M_ShutterGray`. Các đầu bu-lông, bản mã, mối hàn, chân cột, lan can đầu giường và bốn bậc thang có mảng `M_TinRust` hoặc `M_SteelDark`. Cả bốn chân cột có chụp cao su rách mép và để lộ sắt gỉ phía trên.
- Bạt xanh dưới tầng trên võng 152,5 mm tính từ cao độ dây buộc. Lưới bạt có viền bạc màu, miếng vá và chỉ khâu. Mặt tầng trên có đệm mỏng ngả vàng, chăn dù gấp và áo gió xanh có tay áo, khóa kéo.
- Chiếu tầng dưới có sáu dải sọc đỏ son và xanh rêu trên nền cói ngả nâu vàng. Viền đỏ có các đoạn sờn ngả vàng và đầu sợi bung. Dưới gầm là chồng chăn xám xanh và màn ngả vàng. Khăn bông ố vàng rủ qua móc nhôm bị méo.

[Góc toàn cảnh](prop_bed_bunk_iron.png) · [Cận cảnh bạt, khung và thang](prop_bed_bunk_iron_tarp_detail.png)

### Giường gỗ dát nan

- Khung gỗ tối có mảng ẩm trên chân, vai và ván đầu giường. Bốn chân tiết diện danh nghĩa 70 × 70 mm có chốt mộng lộ mặt và đầu đinh sắt gỉ. Ván đầu giường cao hơn mặt dát danh nghĩa 229 mm.
- Hai mươi bốn nan dọc phối năm tông gỗ từ xám tro đến nâu sẫm. Đầu nan vát thô và có rãnh thớ. Chiều dài lệch 10,1–19,3 mm so với nan danh nghĩa. Khoảng trống giữa các bao chiếu ngang của hai nan kề đo được từ 2,9 đến 20,8 mm.
- Ba xà ngang gỗ sẫm nhìn thấy từ mặt dưới và qua khe nan. Miếng bìa các-tông chèn ở góc cuối giường có mép rách, gờ gấp và lớp ruột lộ ra.

[Góc thấy mặt dát và bìa chèn](prop_bed_wood_slats.png) · [Góc kiểm ba xà ngang](prop_bed_wood_slats_joists_detail.png)

### Nệm sàn phủ chiếu trúc

- Ga hồng phối đường may đỏ và 32 hoa đỏ trên bốn mặt bên. Chiếu trúc màu cánh gián gồm 336 hạt cách nhau 4 mm, với viền nâu sẫm. Cụm gối phối hoa đỏ và vải ngả vàng.
- FBX giữ kích thước bao và vùng đặt ba lô 0,90 × 0,80 m. So với FBX trước khi sửa, 355 lưới ngoài thân nệm khớp toàn bộ tọa độ đỉnh và chỉ số mặt. Thân nệm giữ đủ 192 đỉnh gốc. 1.120 đỉnh mới nằm trên các mặt ga cũ với sai lệch lớn nhất khoảng 0,03 micromét và chênh tổng diện tích mặt khoảng 0,000000013 m². Thân nệm vẫn kín, với hai mặt kề trên mỗi cạnh.
- Họa tiết hoa được tạo bằng cách chia bốn mặt bên thành các tam giác cùng mặt phẳng và gán vật liệu theo từng vùng. Cách này giữ dáng nệm và tránh hiện tượng nhấp nháy do chồng mặt (z-fighting).

[Góc thấy ga hoa và chiếu trúc](prop_bed_mattress_bamboo.png)

## Kết quả đo sau khi nhập Unity

| Tiêu chí | Giường tầng sắt | Giường gỗ dát nan | Nệm sàn phủ chiếu trúc |
|---|---:|---:|---:|
| Kích thước X × Y × Z (m) | 1,0281 × 1,7000 × 2,0015 | 1,3010 × 0,6080 × 2,0015 | 1,2000 × 0,4445 × 1,9500 |
| `bounds.min.y` (m) | 0,0000 | 0,0000 | 0,0000 |
| Tâm bao XZ (m) | (0,0000, 0,0000) | (0,0000, 0,0000) | (0,0000, 0,0000) |
| Số tam giác, giới hạn dưới 10.000 | 8.959 | 7.868 | 8.732 |
| Số lưới kết xuất | 158 | 62 | 356 |
| Khoảng UV đo được | 0,0003415–0,9996585 | 0,0002957–0,9997044 | 0,0008603–0,9991396 |
| Vị trí, góc quay, tỷ lệ của gốc và mọi nút con | Identity | Identity | Identity |
| Vật liệu `Toon/Toon` trong `Assets/Art/Materials/` | 14 | 9 | 6 |

Blender kiểm định FBX trục Y hướng lên, đơn vị mét, dữ liệu UV hợp lệ và các tam giác có diện tích dương. Ba lưới bạt, chiếu cói và khăn đều kín. Một lượt kiểm tra riêng trên Unity xác nhận thông số trên, gồm diện tích UV khác 0 trên từng tam giác và vật liệu trỏ đúng tệp trong dự án.

`ArtPipelineCheck.Run` quét toàn dự án và trả mã 1 do lỗi ở `env_alley.fbx` và `env_main_road.fbx`. Không có lỗi ở ba mẫu giường trong lượt quét.

## Kết xuất và phạm vi kiểm tra

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python-exit-code 1 --python tools/generate_bed_props.py -- --render
```

Năm ảnh được kết xuất từ FBX nhập lại, sau đó ghép thành ảnh tổng hợp. Vết vá, gỉ, ẩm, hoa đỏ và sự khác màu giữa các nan được lưu bằng vật liệu hoặc lưới trong FBX. Vân mịn và độ nhám trong ảnh xem trước dùng thiết lập kết xuất Blender. Diện mạo dưới ánh sáng của cảnh chơi chưa được kiểm tra.
