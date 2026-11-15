# Đêm 3: bản thô gameplay (#22)

## Nền và phạm vi

Nhánh này nối trên `feat/22-night-2-evening` (PR #85), sau buổi sáng PR #83. Chưa đóng #22: đây là luồng gameplay trên khối thô, còn ghép điện thoại #17, bối cảnh, ảnh/thoại/animation được duyệt và chơi thử cả ba đêm bằng tay. Không thay hoặc gộp hộ PR của Quang.

Đối chiếu `docs/kich-ban/dem-3.md`, đặc biệt mục 4, 6, 7, 9.1. Giữ luật không truy đuổi Đêm 3, không lộ mặt nhân vật chính, không có tin “Mẹ” gửi trong khi Sinh viên thức.

![Thư viện máy mình và ảnh chụp vật chứng khối thô](screenshots/night-three.png)

## Chơi thử

1. Mở `Assets/Scenes/Night01/NightOneGraybox.unity`, chơi Đêm 1 rồi Đêm 2. Ở “HẾT ĐÊM 2” chọn “Sáng 26/11 — Đêm 3”. Dữ liệu ngủ/thức, căn cước/vết trầy, số phòng, trà sữa, mức nghi, bộ nhớ Máy tính và thử mã sai được giữ lại.
2. Tắt báo thức 06:30. Xuống xe, E bật máy. Tuyến W/S/Space và E xuống xe giữ cách điều khiển Đêm 2. Tại điểm cuối, tương tác khối “Cây xăng”; không mở yên. Bật máy và quay về. Mỗi lượt 0,5 km; chuyến 3 giờ chỉ cộng 11 km nếu Đêm 2 chọn đi ngủ.
3. Máy mình (2) > Đồng hồ, đặt 13:00. Tới nệm E nằm nghỉ; màn hình đen rồi tỉnh lúc 21:00, báo thức hiện đã tắt lúc 13:00. Lô trưa/tối nằm sẵn. Buổi chiều cộng 11 km vào đồng hồ xe nhưng không tự ghi số ấy vào Sổ tay khi người chơi chưa bật xe.
4. Máy Ngân (1) > Dãy số: từ 21:05 có tin thoại; nhánh chặn số phải bỏ chặn mới nhận. Xem lịch sử có miếng vải, trang cá nhân Hòa Nguyễn, trang “Mẹ” trống và Nimbo. Các phút tối chạy một phút game mỗi bốn giây chơi, dừng ở 23:29.
5. Xem bài đăng tìm con ở trang Hòa Nguyễn. Nhân vật tự đi khóa cửa rồi trở về nệm, sau đó trả quyền điều khiển và lưu checkpoint. Đây là đường chuyển động cố định trong phòng khối thô, chưa phải animation hoàn chỉnh.
6. Chọn nhắn từ máy mình hoặc máy Ngân. Hai cách lưu `contactMode` khác nhau; bằng chứng luôn gửi từ máy mình.
7. Trong phòng có khối mặt lưng máy Ngân đứng gần nệm, màu cam. Camera máy mình có preview, nút xoay trái/phải/lên/xuống và Chụp ảnh. Ngắm đúng khối ở khoảng cách tối đa 3 m. Mọi ảnh đều ghi PNG thật; ảnh chụp vật khác vẫn xuất hiện trong thư viện nhưng không được cô Hòa xác nhận. Thư viện > chọn ảnh > Gửi cho cô Hòa. Đổi sang máy Ngân không gửi được thư viện máy mình.
8. Nghe cuộc gọi tới. Chỉ khi đã xem Bộ nhớ 3,9 GB ở Đêm 2 mới có câu hỏi xin mã. Mở Máy tính, nhập `472619`, bấm `=`. Các mục két ngoài mở theo thứ tự tùy ý; “Kẻ xâm nhập” chỉ có nếu đã thử sai mã Đêm 2. Album và tiếng gõ có nhịp tự chuyển góc nhìn, sau đó trả quyền điều khiển. Phần trong không mở ở Đêm 3.
9. Trong Tin nhắn/Liên lạc chọn giữ máy hoặc mang lên phường. Nhánh nộp phải đi bộ tới khối Công an phường, E giao máy mới kết thúc “Kẻ gánh tội”. Nhánh giữ máy có R1, R2, R3; R2/R3 ngăn tin thoại 23:47 về máy Ngân.
10. Thao tác trên máy Ngân buổi tối giảm pin, từ 30% xuống tối thiểu 9%, không giảm theo thời gian. Khi pin yếu, tới khối ba lô tìm cáp và vỉ thuốc, rồi tới khối Cáp/ổ sạc. Sổ tay ghi nhãn “Lê Thị Bé, zolpidem 5 mg”. Cắm sạc trước khi chờ khuya nếu đã xuống 9%.
11. Nệm > chờ tới 23:30 > đi ngủ hoặc thức canh. Nhánh thức canh dừng ở 23:47 cho người chơi nghe tin nếu có, rồi về nệm chờ sáng. Đi ngủ không tự đánh dấu đã nghe tin. Cả hai sang 06:30 ngày 27/11 và “HẾT ĐÊM 3”; không cộng chuyến 3 giờ đêm này. Không liên lạc với cô Hòa vẫn có thể kết thúc đêm, nhưng không nhận mã két.

## Những gì đang giữ chỗ

- Điện thoại dùng IMGUI cho kiểm luồng; chưa tích hợp UI #17. Giọng cô Hòa, tin thoại, vlog/âm thanh, ảnh album/nhật ký/miếng vải và đủ chín quy tắc chưa có asset hoàn chỉnh. Giao diện dùng mô tả hoặc bản chữ, không giả vờ đã có ảnh/âm thanh sản phẩm.
- Vật chứng camera là một khối nhận diện ở `CaseBackPosition`, chưa phải model ốp trong với cá bống thêu. Ảnh camera thật chụp chính khối thô này. “Kẻ xâm nhập” chỉ có mô tả tối/cụt mặt, không tạo ảnh lộ mặt nhân vật chính.
- Chuyển động khóa cửa và liếc hướng nhìn dùng tọa độ cố định của phòng thô. Chưa có động tác lật điện thoại trong tay, hoạt cảnh nệm hay biểu cảm. Khi ghép model cần thay đường chuyển động và marker, giữ thời điểm khóa/nhả điều khiển.
- Chưa gửi thống kê `n3.*` lên máy chủ; chưa thêm Đêm 4, true ending hay mã mở phần trong.

## Lưu/nạp và ảnh

Thêm `nightThreeStarted` và `NightThreeState` vào snapshot có sẵn. Cờ riêng tránh nhận nhầm object rỗng do JsonUtility tạo khi đọc save cũ. `NightThreeState.Valid` kiểm enum, số hữu hạn, cỡ pin, đường xe, tên ảnh UUID, tên mục két và quan hệ bằng chứng → cuộc gọi → mã → mở két. Dữ liệu vẫn đi qua checksum/backup của #21, một slot.

Ảnh nằm ở `THREEGIO12_DATA_ROOT/captures/<UUID>.png` (mặc định dưới thư mục save của game). Tối đa 32 ảnh mỗi phiên, mỗi ảnh 512×288. Camera điện thoại riêng không dùng camera stack tay của player; preview và ảnh dùng cùng pose, renderer và render target. Bộ kiểm đọc lại pixel giữa PNG để xác nhận vật màu cam nằm đúng tâm raycast. Snapshot giữ danh sách ID và kết quả raycast nhận đúng vật; không cho người chơi tự cung cấp đường dẫn tệp. Chỉ ảnh thuộc danh sách, nhận đúng vật và còn tệp mới gửi được. Không gửi ảnh lên mạng thật. Các PNG chưa tự dọn khi chơi mới/xóa slot; bản thô giữ tệp riêng để tránh xóa ảnh còn được snapshot dự phòng tham chiếu.

Không ghi snapshot giữa đoạn đen/chuyển camera/khóa cửa. Thoát giữa đoạn đó quay về snapshot ổn định trước, không khôi phục vào coroutine dở. Khi nạp giữa đường xe, khôi phục cả pose dọc tuyến và khóa đi bộ. Chơi mới tắt props Đêm 3 và bật lại observer Đêm 1.

## Kiểm tra

Chạy trong Unity `6000.3.26f1`, Editor có giao diện; hai tiến trình riêng cùng thư mục dữ liệu thử, không dùng dữ liệu chơi thật:

```sh
THREEGIO12_DATA_ROOT=/tmp/3gio12-night3-review /path/to/Unity \
  -projectPath /path/to/repo/game -executeMethod NightThreeRegression.Write \
  -logFile /tmp/night3-write.log
THREEGIO12_DATA_ROOT=/tmp/3gio12-night3-review /path/to/Unity \
  -projectPath /path/to/repo/game -executeMethod NightThreeRegression.Read \
  -logFile /tmp/night3-read.log
```

Không thêm `-quit`. Thành công: `NIGHT_THREE_WRITE_PASSED`, `NIGHT_THREE_READ_PASSED`, exit 0. Bộ kiểm seed checkpoint kết thúc Đêm 2 (luồng Đêm 2 kiểm riêng bằng `NightTwoEveningRegression`), sau đó chạy gameplay Đêm 3. Kiểm cả nhánh ngủ/thức, nạp giữa đường và giữa tiến trình, ảnh thật/sai vật/sai máy, chặn số/bỏ chặn, khóa cửa và liếc camera, điều kiện biết app Máy tính, két, pin/cáp, giữ/nộp máy, R2/R3, tin 23:47, không liên lạc, hoàn thành/nạp lại/chơi mới và không tăng số xe lúc rạng sáng.

Các kiểm này gọi API gameplay và chạy trong Play Mode, không thay việc đi bằng phím toàn tuyến hoặc kiểm bản Windows/Linux. Cần nhóm chơi lại trên cảnh/model cuối trước khi đóng #22.
