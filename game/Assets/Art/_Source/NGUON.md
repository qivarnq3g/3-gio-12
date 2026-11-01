# Nguồn ảnh trong Assets/Art

Tất cả tạo ngày 18/10/2026 bằng ChatGPT (tạo ảnh trên web, gói Free), cùng một cuộc chat "Create app icons". Điều khoản sở hữu và dùng thương mại của ảnh ChatGPT chưa đọc nguồn gốc; kiểm trước khi phát hành.

| Tệp | Nội dung | Kích thước gốc |
|---|---|---|
| `_Source/app-icons-sheet.png` | Ba biểu tượng Tíc, Nimbo, Mosa trên một tấm | 1254x1254 |
| `Icons/icon-tic.png`, `icon-nimbo.png`, `icon-mosa.png` | Tách từ tấm trên, nền trong suốt, thu về 512x512 | 512x512 |
| `Backgrounds/hem-3am.png` | Hẻm Sài Gòn lúc 3 giờ sáng, không người | 940x1672 |
| `Backgrounds/phong-tro-3am.png` | Phòng trọ lúc 3 giờ sáng, không người (chưa tải về) | 941x1672 |
| `Backgrounds/hanh-lang-cu-xa-3am.png` | Hành lang cư xá cũ lúc khuya, không người | 940x1672 |
| `Backgrounds/nha-xe-3am.png` | Nhà xe chung cư lúc khuya, không người | 941x1672 |

Câu lệnh chung: ảnh như chụp bằng điện thoại Android cũ ban đêm (nhiễu, mờ rìa, ánh vàng lẫn trắng), không người, không mặt, không chữ, số, biển hiệu hay logo đọc được; biểu tượng không được giống Zalo, Google, Facebook, Messenger, iCloud.

Từ 18/10 (sau): ảnh trong thư mục này chỉ là **ảnh tham chiếu** cho cảnh dựng 3D bằng Blender (`D:\Projects-gio-12rt-sourcelender`), không đưa thẳng vào game. Ảnh dựng xong nằm ở `Assets/Art/Scenes`.

# Model 3D

| Tệp | Nguồn | Giấy phép | Ngày |
|---|---|---|---|
| `Models/Props/prop_potted_snake_plant.fbx` | Nhóm tự dựng trong Blender 5.2 (chậu đất nung trồng lưỡi hổ, 582 tam giác) | Của nhóm | 30/10/2026 |
| `Models/Environment/env_garage.fbx` | Nhóm tự dựng trong Blender 5.2 theo khối thô nhà xe (3.617 tam giác) | Của nhóm | 30/10/2026 |
| `Models/Environment/env_room7.fbx` | Nhóm tự dựng trong Blender 5.2 theo khối thô phòng 7 tầng 3, cửa là object riêng (1.210 tam giác) | Của nhóm | 30/10/2026 |
| `Models/Environment/env_homeware_shop.fbx` | Nhóm tự dựng trong Blender 5.2 bằng script theo khối thô cửa hàng đồ gia dụng đêm 2 (6.114 tam giác) | Của nhóm | 01/11/2026 |
| `Models/Environment/env_rice_shop.fbx` | Nhóm tự dựng trong Blender 5.2 bằng script theo khối thô quán cơm đêm 2 (3.820 tam giác) | Của nhóm | 01/11/2026 |
| `Models/Environment/env_gas_station.fbx` | Nhóm tự dựng trong Blender 5.2 bằng script theo khối thô cây xăng đêm 3, cửa ki-ốt là object riêng (1.436 tam giác) | Của nhóm | 01/11/2026 |
| `Models/Environment/env_main_road.fbx` | Nhóm tự dựng trong Blender 5.2 bằng script theo khối thô đoạn đường lớn đêm 2-3: mặt đường, vỉa hè, 152 nhà phố nhiều kiểu, cột điện và dây, cây, xe máy, rác, đèn đường, đèn giao thông (156.896 tam giác) | Của nhóm | 01/11/2026 |

# Ảnh vân

| Tệp | Nguồn | Giấy phép | Ngày |
|---|---|---|---|
| `Textures/T_SignHomeware.png`, `T_SignRice.png`, `T_SignFuel.png`, `T_MenuRice.png` | Nhóm vẽ bằng script Python (Pillow) với phông Noto Sans trong `Assets/Prototype/Resources/Fonts/`; tên cửa hàng tự đặt, không có thương hiệu thật | Của nhóm; phông theo SIL Open Font License 1.1 | 01/11/2026 |
| `Textures/T_TileFacade.png`, `T_TileShutter.png`, `T_TileSidewalk.png`, `T_TileAsphalt.png` | Nhóm vẽ bằng script Python (NumPy, Pillow): vân lặp gần trắng, vết ố, gạch vỉa hè, nhựa đường vá | Của nhóm | 01/11/2026 |
| `Textures/T_SignStreet.png` | Nhóm vẽ bằng script Python (Pillow) với phông Noto Sans: tám biển tên loại cửa hàng chung chung, không có tên riêng hay thương hiệu | Của nhóm; phông theo SIL Open Font License 1.1 | 01/11/2026 |

# Phông chữ

| Tệp | Nguồn | Giấy phép | Ngày |
|---|---|---|---|
| `Fonts/NotoSans-Regular SDF.asset` | Font asset TextMeshPro tạo từ `Assets/Prototype/Resources/Fonts/NotoSans-Regular.ttf` (Noto Sans của Google), bảng ký tự cố định 229 ký tự: chữ tiếng Việt, chữ số, dấu câu thường gặp | SIL Open Font License 1.1, xem `Assets/Prototype/Resources/Fonts/LICENSE-NotoSans.txt` | 01/11/2026 |
| `Assets/TextMesh Pro/` (ngoài `Assets/Art`) | Gói TMP Essential Resources đi kèm `com.unity.ugui` 2.0.0, gồm shader, cài đặt và phông Liberation Sans | Theo gói Unity; Liberation Sans theo SIL Open Font License, xem `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` | 01/11/2026 |
