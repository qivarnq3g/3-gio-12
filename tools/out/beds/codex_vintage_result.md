Đã chỉnh và kết xuất lại cả ba mẫu giường theo bối cảnh phòng trọ Sài Gòn cuối thập niên 1990 và đầu những năm 2000. [Xem ảnh tổng hợp 5 góc nhìn](/D:/DACNPM/3-gio-12/tools/out/beds/contact_sheet.jpg)

- **Giường tầng sắt** có sơn xám kem xỉn, gỉ ở mối hàn và bậc thang, chân cao su sờn, bạt xanh vá võng 15,25 cm và chiếu cói viền đỏ bạc màu.
- **Giường gỗ** có 24 nan xẻ thô phối năm tông gỗ, khe so le, ba xà ngang sẫm màu và miếng bìa các-tông rách chèn ở góc.
- **Nệm sàn** có ga hồng hoa đỏ, chiếu trúc màu cánh gián và gối ngả vàng. Họa tiết được chia trực tiếp trên bốn mặt ga cũ, giữ dáng nệm và tránh chồng mặt.

Cả ba FBX đạt kiểm tra nhập riêng trong Unity 6000.6.7f1. Đáy chạm sàn tại `0,0000 m`, tâm XZ ở gốc, transform chuẩn, UV trong `[0, 1]` và vật liệu lấy từ thư mục hiện có. Số tam giác lần lượt là **8.959**, **7.868** và **8.732**. Lượt quét toàn dự án vẫn báo lỗi ở `env_alley` và `env_main_road`. Diện mạo dưới ánh sáng cảnh chơi chưa được kiểm tra.

[Báo cáo nghiệm thu](/D:/DACNPM/3-gio-12/tools/out/beds/codex_vintage_report.md) · [Số đo và mã băm](/D:/DACNPM/3-gio-12/tools/out/beds/bed_props_report.json) · [Hồ sơ nguồn](/D:/DACNPM/3-gio-12/game/Assets/Art/_Source/NGUON.md)

Đã ghi cách gán họa tiết trên mặt phẳng FBX bằng tam giác ràng buộc và kiểm lại bằng tọa độ barycentric vào [ghi chú Blender](/D:/DACNPM/3-gio-12/docs/knowledge/blender-scripting-pitfalls.md) và bản mẫu dùng chung.