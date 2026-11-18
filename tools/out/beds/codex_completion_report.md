# Báo cáo hoàn thành chỉnh sửa ba mô hình giường

Ngày kiểm chứng: 18/11/2026  
Vai trò thực hiện: Codex Coder  
Blender: 5.2.3 LTS

## Kết quả

Đã sửa `tools/generate_bed_props.py`, kết xuất lại đủ ba FBX và ba ảnh kiểm tra bằng lệnh:

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python tools/generate_bed_props.py -- --render
```

Lượt chạy chốt kết thúc với mã 0 và thông báo `All bed props built successfully!`.

| FBX | Kích thước Unity (m) | Bounds.min.y | Sai số tâm XZ | Transform Scale/Rotation | Tam giác | SHA-256 |
|---|---:|---:|---:|---|---:|---|
| `prop_bed_bunk_iron.fbx` | 0,9704 × 1,7150 × 2,0034 | 0,0000 m | 0,0000 / 0,0000 m | 69/69 identity | 1.796 | `1203499ecd9bb7715fc70c47f18acf4212dbdcae6f4d5add8d962ccd09d752bc` |
| `prop_bed_wood_slats.fbx` | 1,3000 × 0,6200 × 2,0000 | 0,0000 m | 0,0000 / 0,0000 m | 34/34 identity | 1.208 | `04d7f2f0a597f8fa9ff986ab5da7b46742c169e41e675198764e59196b993801` |
| `prop_bed_mattress_bamboo.fbx` | 1,2000 × 0,4445 × 1,9500 | 0,0000 m | 0,0000 / 0,0000 m | 356/356 identity | 6.492 | `e5bd54bf4e51a0213b27e1a6400e3bba20d22d039932e2ba7f41566f33644675` |

## Kiểm tra độc lập

Một trình kiểm tra tách biệt đã nhập lại từng FBX và xác nhận:

- ba tệp tồn tại, có dữ liệu và mã băm khớp `bed_props_report.json`;
- `Bounds.min.y = 0`, tâm XZ đối xứng tuyệt đối trong độ chính xác phép đo;
- toàn bộ mesh có Location và Rotation bằng 0, Scale bằng 1;
- cả ba mô hình dưới giới hạn 10.000 tam giác;
- giường gỗ có đúng 20 nan dọc và chiều cao tổng 0,62 m;
- chiếu trúc có đúng 336 hạt tách khe, không cặp AABB hạt nào chồng nhau, chỉ có một lưới viền liền và không còn đối tượng rãnh chồng dọc/ngang;
- viền chiếu tầng dưới là một lưới liền; 16 dải sọc không chồng diện tích;
- vùng đặt ba lô 0,90 × 0,80 m không bị gối hay phụ kiện xâm lấn;
- bạt xanh có độ võng đo được lớn hơn 0,07 m; móc áo và khăn rủ tồn tại trong FBX.

`python -Wall -m py_compile tools/generate_bed_props.py` hoàn tất không có cảnh báo. Lượt Blender chốt cũng không phát cảnh báo. Ba PNG cuối nằm trong `tools/out/beds/` và đã được kiểm tra trực quan ở độ phân giải gốc; không còn góc đen hay nhiễu bề mặt do z-fighting.

`game/Assets/Art/_Source/NGUON.md` đã được cập nhật theo đúng kích thước, số tam giác, cấu tạo và đường dẫn đầu ra thực tế.
