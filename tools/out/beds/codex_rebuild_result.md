Đã dựng lại và xuất FBX cho mẫu 1 và mẫu 2 bằng Blender 5.2.3 LTS. Đã xác minh bằng SHA-256 rằng FBX và ảnh render của mẫu 3 được giữ nguyên.

| Mẫu | Chi tiết đã dựng | Tam giác |
|---|---|---:|
| Giường tầng sắt | Khung ống kem ngà có bản mã và bu-lông, thang móc, bạt xanh võng 95,6 mm, chiếu sọc, khăn rủ và chăn xếp dưới gầm | 5.988 |
| Giường gỗ dát nan | 24 nan dọc có thớ khắc, đầu vát và độ so le, kê trên ba xà ngang. Chân gỗ vuông 70 × 70 mm có chốt mộng và đầu đinh | 5.136 |

[Ảnh tổng hợp ba mẫu và hai góc nhìn từ dưới gầm](D:/DACNPM/3-gio-12/tools/out/beds/contact_sheet.jpg) · [Báo cáo nghiệm thu](D:/DACNPM/3-gio-12/tools/out/beds/bed_props_acceptance.md) · [Số đo và mã băm](D:/DACNPM/3-gio-12/tools/out/beds/bed_props_report.json) · [Hồ sơ nguồn](D:/DACNPM/3-gio-12/game/Assets/Art/_Source/NGUON.md)

Cả hai FBX đạt kiểm tra riêng trong Unity 6000.6.7f1 về gốc sàn `0,0000 m`, tâm XZ, transform, UV và vật liệu Toon. Lượt `ArtPipelineCheck` toàn dự án vẫn báo lỗi ở `env_alley` và `env_main_road`. Phần vân mịn và độ nhám trong ảnh được kết xuất bằng Blender. Diện mạo dưới ánh sáng cảnh chơi chưa được kiểm tra.

Đã ghi vào [ghi chú Blender](D:/DACNPM/3-gio-12/docs/knowledge/blender-scripting-pitfalls.md) và bản mẫu dùng chung cách tránh sàn dựng ảnh che khuất góc nhìn từ dưới gầm.