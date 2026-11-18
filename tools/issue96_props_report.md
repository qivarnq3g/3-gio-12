# Bàn giao model cho Anti: issue #96

Đã dựng bằng Blender 5.2.3 LTS và kiểm tra bằng cách xuất rồi nhập lại từng FBX.
Kích thước là bao toàn bộ model theo trục Unity (X rộng, Y cao, Z sâu), đơn vị mét.
Số đỉnh là số đỉnh lưới Blender sau nhập lại; Unity có thể tách thêm đỉnh tại biên UV hoặc pháp tuyến.

| Tệp FBX | Kích thước (X, Y, Z), m | Số đỉnh | Số tam giác | Vật liệu |
|---|---|---:|---:|---|
| `prop_utility_pole_10m.fbx` | 1.7 × 10.5 × 2.457055 | 5072 | 9444 | `M_BulbWarmGlow`, `M_ConcretePole`, `M_GlassFrosted`, `M_MetalHandle`, `M_MeterBoxGray`, `M_PaperWhite`, `M_SteelDark`, `M_WireBlack` |
| `prop_utility_pole_8m.fbx` | 1.2 × 8.5 × 1.979055 | 4186 | 7728 | `M_BulbWarmGlow`, `M_ConcretePole`, `M_GlassFrosted`, `M_MetalHandle`, `M_MeterBoxGray`, `M_PaperWhite`, `M_SteelDark`, `M_WireBlack` |
| `prop_collapsible_gate_shop.fbx` | 3.2 × 2.6 × 0.156497 | 5768 | 9460 | `M_MetalHandle`, `M_ShutterBlue`, `M_SteelDark` |
| `prop_collapsible_gate_small.fbx` | 1.2 × 2.2 × 0.156497 | 2208 | 3644 | `M_MetalHandle`, `M_ShutterBlue`, `M_SteelDark` |
| `prop_curb_ramp_concrete_a.fbx` | 1.2 × 0.15 × 0.5 | 193 | 366 | `M_ConcreteFloor` |
| `prop_curb_ramp_concrete_b.fbx` | 2 × 0.15 × 0.55 | 317 | 606 | `M_ConcreteFloor` |
| `prop_curb_ramp_concrete_c.fbx` | 3 × 0.15 × 0.6 | 379 | 734 | `M_ConcreteFloor` |
| `prop_curb_ramp_rubber.fbx` | 0.5 × 0.15 × 0.25 | 116 | 154 | `M_CurbYellow`, `M_Rubber` |
| `prop_curb_ramp_steel.fbx` | 0.8 × 0.15 × 0.6 | 388 | 560 | `M_SteelDark` |
| `prop_mattress_room7.fbx` | 1.2 × 0.205 × 1.9 | 1530 | 3052 | `M_ClothBlue`, `M_PaperWhite` |

Tất cả 10 model: đáy Y = 0, giữa X/Z, các nút không có dịch chuyển hoặc xoay, tỉ lệ 1; có UV trong 0–1, không có tam giác hình học hoặc UV suy biến. Đã đọc trực tiếp các nút và tọa độ trong FBX để kiểm tra Y hướng lên và đơn vị mét. Các ngưỡng tam giác đều đạt.

Thân nệm đúng 1,20 × 1,90 × 0,10 m; chiều cao 0,205 m trong bảng tính cả gối. Mặt nệm trống phẳng 0,80 × 0,85 m. Cột 10m/8m có thân dài 10,50/8,50 m; chiều rộng và chiều sâu trong bảng bao gồm xà, cần đèn và hộp điện. Hai cửa tách khung cố định và hai cánh trượt; chưa có hoạt ảnh xếp cửa.

Nguồn kích thước, phạm vi sử dụng ảnh và tình trạng tra cứu catalogue đã ghi trong [NGUON.md](../game/Assets/Art/_Source/NGUON.md). Chỉ dùng tên vật liệu đã tồn tại, không sửa vật liệu Unity.

Script: [generate_issue96_props.py](generate_issue96_props.py). Dữ liệu kiểm tra và SHA-256 của từng FBX: [issue96_props_report.json](issue96_props_report.json). Ảnh render từ FBX nhập lại nằm trong `tools/out/issue96/`; nguồn Blender tại `art-source/issue96/` khi dùng tùy chọn dưới đây, cần chuyển lên Drive theo quy định dự án.

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python-exit-code 1 --python tools/generate_issue96_props.py -- --render --save-blend
```

Chưa chạy Unity ArtPipelineCheck. Anti nhập 10 asset, kiểm tra ánh xạ vật liệu, chạy `ArtPipelineCheck.Run` và bổ sung các tệp `.meta` do Unity tạo. Không sửa `env_alley.fbx`, `env_garage.fbx`, `RealisticSceneBuilder.cs` hoặc cảnh `.unity`.
