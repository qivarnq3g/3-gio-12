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
| `TexturesPBR/rusty_metal_sheet` | Ảnh vân quét rusty_metal_sheet của Poly Haven (polyhaven.com), bản 2K, đổi định dạng ảnh mặt nạ cho URP Lit; dùng cho tôn và xà gồ nhà xe | CC0 1.0 | 08/11/2026 |
| `Models/Environment/env_garage.fbx`, `Textures/T_NoteGarage.png`, `Textures/T_FireRules.png` | Nhóm tự dựng nhà xe bằng script trong Blender 5.2 (46.654 tam giác, có đồ vật Poly Haven ở dòng trên). Chữ trên tờ giấy viết bằng phông Patrick Hand (Google Fonts, giấy phép SIL Open Font License 1.1), chữ bảng tiêu lệnh bằng Arial, cả hai chỉ in thành ảnh, không kèm tệp phông | Của nhóm | 08/11/2026 |

# Model dựng lại cho issue #96

Ngày dựng: 18/11/2026. Nhóm tự dựng lưới bằng script Python `tools/generate_issue96_props.py` trong Blender 5.2.3 LTS; không dùng model tải sẵn. Đơn vị mét, vật đứng tại gốc giữa đáy, trục Y hướng lên sau khi xuất FBX. Giấy phép lưới: của nhóm. Kích thước dưới đây là kích thước thiết kế; số đo bao toàn bộ phụ kiện và thống kê lưới sau khi nhập lại FBX nằm trong `tools/issue96_props_report.json`.

| Tệp trong `Models/Props/` | Nguồn kích thước | Ảnh hoặc mô tả tham chiếu | Chi tiết dựng lại |
|---|---|---|---|
| `prop_utility_pole_10m.fbx` | Đặc tả issue #96: dài 10,50 m, đường kính ngọn 0,190 m, gốc 0,330 m; đối chiếu bảng thông số nhà sản xuất ở dưới | `_Source/ref_utility_pole.jpg`, ảnh do người dùng cung cấp và duyệt | Thân bê tông ly tâm thuôn, ba xà U thép có giằng và sứ nhiều tầng, cần đèn chéo có chóa nghiêng, hai công tơ có ô kính và đai, cuộn cáp, giấy dán và mã sơn trắng 07 |
| `prop_utility_pole_8m.fbx` | Đặc tả issue #96: dài 8,50 m, đường kính ngọn 0,160 m, gốc 0,255 m; đối chiếu bảng thông số nhà sản xuất ở dưới | `_Source/ref_utility_pole.jpg` | Phiên bản hẻm, hai xà và cần đèn ngắn hơn; giữ hai công tơ và các chi tiết phụ |
| `prop_collapsible_gate_shop.fbx` | Đặc tả issue #96: bộ cửa vừa ô 3,20 × 2,60 m; bước thanh U 0,10 m; ba dải nan cao 0,18 m | `_Source/ref_collapsible_gate_nguyenmode.jpg`, cửa Nguyễn Mode tại 173 Nguyễn An Ninh do người dùng duyệt | Tiết diện U thực, nan chéo ngắn hai lớp, đinh tán, ray trên/dưới, bánh dẫn, hai đố giữa, tay nắm, bát khóa và khóa; không dựng biển hoặc nhãn hiệu cửa hàng |
| `prop_collapsible_gate_small.fbx` | Đặc tả issue #96: bộ cửa vừa ô 1,20 × 2,20 m; bước U 0,10 m, dải nan 0,18 m | `_Source/ref_collapsible_gate_nguyenmode.jpg` | Cùng kết cấu; khung và hai cánh trượt là ba lưới riêng |
| `prop_curb_ramp_concrete_a.fbx` | Đặc tả issue #96: rộng 1,20 m, sâu 0,50 m, cao 0,15 m | Mô tả trong issue #96; không có ảnh bê tông riêng | Mặt khía quả trám sâu 3 mm, hai góc sứt bất đối xứng, rãnh thông ngang dưới đáy rộng 55 mm, cao 26 mm |
| `prop_curb_ramp_concrete_b.fbx` | Đặc tả issue #96: rộng 2,00 m, sâu 0,55 m, cao 0,15 m | Mô tả trong issue #96 | Cùng kết cấu bê tông bản a, tăng chiều rộng và chiều sâu |
| `prop_curb_ramp_concrete_c.fbx` | Đặc tả issue #96: rộng 3,00 m, sâu 0,60 m, cao 0,15 m | Mô tả trong issue #96 | Cùng kết cấu, lưới mặt khía vẫn dưới 1.000 tam giác |
| `prop_curb_ramp_rubber.fbx` | Đặc tả issue #96: rộng 0,50 m, sâu 0,25 m, cao 0,15 m | Mô tả trong issue #96; không có ảnh cao su riêng | Khối cao su bo mép, hai lỗ neo, ba vạch vàng và gờ chống trượt |
| `prop_curb_ramp_steel.fbx` | Đặc tả issue #96: rộng 0,80 m, sâu 0,60 m, cao 0,15 m | `_Source/ref_curb_ramp_steel.jpg`, ảnh do người dùng cung cấp và duyệt | Khung thép hở, 17 thanh song song theo chiều dốc, gờ mối hàn, giằng dưới và chân đỡ phía cao |
| `prop_mattress_room7.fbx` | Mẫu A đã duyệt trong issue #96: thân nệm rộng 1,20 m, dài 1,90 m, dày 0,10 m | Mô tả của người dùng: nệm mút trải sàn bọc ga trơn và một gối; không có ảnh riêng | Ga xanh một màu, viền bo và đường may cùng màu, một gối trắng; vùng mặt phẳng trống 0,80 × 0,85 m. Bao gồm gối thì chiều cao toàn model là 0,205 m |

# Mô hình giường phòng trọ dựng theo ảnh thật

Ngày 18/11/2026, nhóm dựng lại ba mẫu theo bối cảnh nhà trọ cũ Sài Gòn cuối thập niên 1990 và đầu những năm 2000. Nhóm tự dựng lưới bằng `tools/generate_bed_props.py` trong Blender 5.2.3 LTS. FBX dùng đơn vị mét, trục Y hướng lên và gốc ở giữa hình chiếu bằng trên mặt sàn (`bounds.min.y = 0.0000 m`). Quyền đối với lưới dựng thuộc về nhóm.

| Tệp trong `Models/Props/` | Kích thước Unity (m) | Ảnh tham chiếu | Chi tiết dựng |
|---|---|---|---|
| `prop_bed_bunk_iron.fbx` | 1,0281 × 1,7000 × 2,0015 | `_Source/ref_bed_bunk_iron.jpg` | Khung sắt xám kem xỉn, gỉ ở chân cột, mối nối và thang. Bạt xanh có miếng vá và đường khâu, võng 152,5 mm. Chiếu cói sọc đỏ son và xanh rêu có viền đỏ sờn. Đệm trên kê áo gió và chăn dù, dưới gầm có chăn màn xếp, hông giường treo khăn ố vàng trên móc nhôm méo (8.959 tam giác). |
| `prop_bed_wood_slats.fbx` | 1,3010 × 0,6080 × 2,0015 | `_Source/ref_bed_wood_slats.jpg` | Khung gỗ tối có vệt ẩm, chân danh nghĩa 70 × 70 mm lộ chốt mộng và đinh sắt gỉ. Hai mươi bốn nan xẻ thô từ năm tông gỗ, kê trên ba xà ngang. Mép dát có miếng bìa các-tông rách (7.868 tam giác). |
| `prop_bed_mattress_bamboo.fbx` | 1,2000 × 0,4445 × 1,9500 | `_Source/ref_bed_mattress_bamboo.jpg` | Nệm sàn có ga hồng in hoa đỏ, 336 hạt trúc màu cánh gián và viền nâu sẫm. Gối hoa đỏ phối gối vải ngả vàng. Họa tiết ga dùng các mặt vật liệu cùng nằm trên bề mặt cũ và chừa vùng phẳng 0,90 × 0,80 m để đặt ba lô (8.732 tam giác). |

Ảnh do người dùng cung cấp là căn cứ chính để dựng hình. Tham chiếu bổ sung về khung sắt tròn màu kem và dát gỗ giá rẻ gồm [giường sắt GTS 40 của Tổng Kho Giường Sắt Miền Bắc](https://khogiuongsat.com/giuong-sat-tang-1m6-ong-tron-lech/) và [dát phản keo, bạch đàn DG02 của Nội thất Sinh Liên](https://noithatvanphonggiare.com/tam-dat-giuong-phan-go-nam-dat-gia-re-dg02/p6670.html).

Nguồn tra cứu cột điện: [trang thông số cột điện ly tâm tiền áp của Thủ Đức - Long An](https://www.thuduclongan.com/productservice/detail/thong-so-ky-thuat-cot-dien-ly-tam-tien-ap-1), liên kết [ảnh catalogue trang 15](https://www.thuduclongan.com/uploads/files/2017/07/04/TDCLA-Catalogue-P15-Pole-Specifications.jpg). Trang HTML đọc được nhưng ảnh bảng gốc chưa tải được trong phiên dựng này. Số liệu triển khai lấy đúng đặc tả người dùng và được đối chiếu với [bảng công bố của Bê tông Thủ Đức 1](https://betongthuduc1.vn/cot-dien-be-tong-cot-thep-ly-tam), có dòng 8.5-200 (8.500 mm, ngọn 160 mm, gốc 255 mm) và 10.5-350 (10.500 mm, ngọn 190 mm, gốc 330 mm). Không coi hai doanh nghiệp là cùng một nguồn.

Ba ảnh JPG được dùng để quan sát hình học, không nhúng vào FBX hoặc vật liệu trong game. Chưa có thông tin tác giả hoặc giấy phép ảnh gốc từ người cung cấp; việc duyệt ảnh tham chiếu không xác lập quyền phân phối ảnh. Phần giấy quảng cáo và mã cột là hình học tự dựng, không sao chép chữ, số điện thoại hoặc thương hiệu thật. Chỉ dùng các vật liệu `M_...` đã có trong `Materials/`.

Tùy chọn `--save-blend` lưu bản Blender trong `game/Assets/Art/_Source/`. Tùy chọn `--render` tạo năm ảnh kiểm tra từ FBX nhập lại trong `tools/out/beds/`. Thư mục này có ảnh tổng hợp `contact_sheet.jpg`, số đo và mã băm trong `bed_props_report.json`, cùng báo cáo nghiệm thu `codex_vintage_report.md`. Cả ba FBX đạt kiểm tra nhập riêng trên Unity 6000.6.7f1 với vật liệu `Toon/Toon` của dự án. Lượt `ArtPipelineCheck` toàn dự án báo lỗi ở hai mô hình môi trường `env_alley` và `env_main_road`.

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
