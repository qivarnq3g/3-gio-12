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
| `Models/Props/prop_motorbike_cub.fbx` và vật liệu `Materials/M_Cub*.mat` | "Honda Super cub" của Aleksandr Sagidullin (https://sketchfab.com/3d-models/honda-super-cub-9ce554228fc746c89f6d02b995f43c41), tải bản GLB ngày 02/11/2026. Nhóm đã sửa: xóa chữ và logo hãng, đổi ảnh vân thành 11 vật liệu màu phẳng, đưa về dài 1,80 m, tách bánh trước, bánh sau, tay lái, yên thành object riêng, giảm từ 333.676 mặt còn 45.199 tam giác | CC Attribution (CC BY), ghi tên tác giả | 02/11/2026 |

# Cảnh tả thực

| Tệp | Nguồn | Giấy phép | Ngày |
|---|---|---|---|
| `TexturesPBR/` (asphalt_02, brick_wall_001, concrete_floor_worn_001, dark_wood, dirty_concrete, floor_tiles_06, painted_metal_shutter, painted_plaster_wall, peeling_painted_wall, rusty_corrugated_iron, rusty_painted_metal, square_pavers) | Ảnh vân quét của Poly Haven (polyhaven.com), bản 2K, đổi định dạng ảnh mặt nạ cho URP Lit | CC0 1.0 | 02/11/2026 |
| `TexturesPBR/PH_*`, lưới và vật liệu `PH_*` trong `Models/Environment/env_alley.fbx`, `MeshesPBR/`, `MaterialsPBR/` | Model quét của Poly Haven: cardboard_box_01, exterior_aircon_unit, lightbulb_01, modular_electric_cables, modular_metal_gutter, old_tyre, planter_pot_clay, plastic_bottle_gallon, plastic_crate_03, plastic_monobloc_chair_01, potted_plant_02, power_box_01, pull_chain_light_socket, rollershutter_door, security_camera_01, steel_frame_shelves_01, trashbag | CC0 1.0 | 03/11/2026 |
| `Decals/D_Leak*`, `D_Cracks*`, `D_Manhole*` và các vết ố | Làm từ ảnh của ambientCG (ambientcg.com): AsphaltDamage001, Leaking001 đến 006, Leaking008, ManholeCover008 | CC0 1.0 | 03/11/2026 |
| `Decals/D_Poster*`, `D_Plate*`, `D_RoomForRent`, `D_ShopSign`, `Models/Environment/env_alley.fbx` (phần nhà) | Nhóm tự làm | Của nhóm | 03/11/2026 |
| `Models/Environment/env_street_tree.fbx` (thân, cành), `Textures/T_StreetTreeBark*.png`, `T_StreetTreeBranch*.png`, `T_StreetTreeLeaves.png` | Model quét tree_small_02 của Poly Haven; nhóm rút gọn thân, cành và làm thẻ lá trong Blender 5.2 (5.400 tam giác) | CC0 1.0 | 06/11/2026 |
| `Models/Environment/env_boarding_house.fbx`, `Models/Environment/env_street_details.fbx` | Nhóm tự dựng bằng script trong Blender 5.2 theo khối thô nhà trọ và số đo cảnh (13.366 và 16.060 tam giác) | Của nhóm | 06/11/2026 |

# Phông chữ

| Tệp | Nguồn | Giấy phép | Ngày |
|---|---|---|---|
| `Fonts/NotoSans-Regular SDF.asset` | Font asset TextMeshPro tạo từ `Assets/Prototype/Resources/Fonts/NotoSans-Regular.ttf` (Noto Sans của Google), bảng ký tự cố định 229 ký tự: chữ tiếng Việt, chữ số, dấu câu thường gặp | SIL Open Font License 1.1, xem `Assets/Prototype/Resources/Fonts/LICENSE-NotoSans.txt` | 01/11/2026 |
| `Resources/Fonts/BeVietnamPro-Regular.ttf`, `BeVietnamPro-Medium.ttf` | Phông Be Vietnam Pro (github.com/google/fonts, thư mục `ofl/bevietnampro`), giữ nguyên tệp; menu tạm dừng tạo font asset TextMeshPro động từ hai tệp này lúc chạy | SIL Open Font License 1.1, xem `Resources/Fonts/BeVietnamPro-OFL.txt` | 03/11/2026 |
| `Assets/TextMesh Pro/` (ngoài `Assets/Art`) | Gói TMP Essential Resources đi kèm `com.unity.ugui` 2.0.0, gồm shader, cài đặt và phông Liberation Sans | Theo gói Unity; Liberation Sans theo SIL Open Font License, xem `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` | 01/11/2026 |

# Âm thanh

| Tệp | Nguồn | Giấy phép | Ngày |
|---|---|---|---|
| `Resources/Audio/Footsteps/footstep_concrete_000.ogg` đến `_004.ogg` | Gói Impact Sounds của Kenney (kenney.nl/assets/impact-sounds), giữ nguyên tệp | CC0 1.0, không cần ghi tên | 03/11/2026 |
