# Đêm 2: lát cắt buổi sáng (#22, đang làm)

## Phạm vi của nhánh

Nối từ kết thúc `night1_completed` trong `NightOneGraybox` tới lúc tỉnh dậy 21:00 ngày 25/11. Đối chiếu `docs/kich-ban/dem-2.md` mục 3.1–3.3, 4, 5.1–5.3, 6–9. Không đóng #22: buổi tối Đêm 2, Đêm 3 và chơi xuyên suốt ba đêm chưa được triển khai ở lát cắt này.

Giữ riêng khối thô ở x = 80–120, dựng khi chạy, không sửa scene đã lưu và không chép cảnh từ PR #80. Quang sẽ ghép các điểm tương tác vào bối cảnh sau. Người chơi dùng điều khiển và raycast E thật của `FirstPersonPlayer`. Điện thoại buổi sáng là HUD tạm, chưa phải tích hợp giao diện #17. Không có model/âm thanh/ảnh hay lời thoại cuối cùng mới.

![Khối thô buổi sáng Đêm 2, đang ráp hai tầng kệ](screenshots/night-two-morning.png)

## Chơi thử

1. Mở `Assets/Scenes/Night01/NightOneGraybox.unity` bằng Unity `6000.3.26f1`, Play, chơi Đêm 1. Sau “HẾT ĐÊM 1”, chọn “Sáng 25/11 — Bắt đầu Đêm 2”. Kết thúc “Người qua đường” không có nút này.
2. Tắt báo thức. Máy 1 cho xem trạng thái báo thức và trả lời số phòng nếu Đêm 1 đã đi ngủ. Máy 2 có bảng bước chân giữ chỗ, khác nhau theo lựa chọn ngủ/thức. Dữ liệu hiện thô, chưa có biểu đồ chạm từng quãng của #17. Câu trả lời của người chơi chỉ có “Đã gửi”; không nhận phản hồi “Mẹ” trong lúc đang thức.
3. Tới khối “Bánh mì”, E. Tới khối “Xe máy”, E để bật máy. W tăng tốc; S hoặc Space phanh. Tuyến thẳng giữ người chơi trên đường, tự chặn ở đèn đỏ ba giây, không có va chạm/đua/lái tự do. Đường thô dài 80 m đại diện cho 1,5 km mỗi lượt.
4. Đến cuối tuyến, E tắt máy/xuống xe. Tới khối cửa hàng, E mua kệ và đèn. E lần nữa để chở đồ về. Tới điểm dừng, E tắt máy/xuống xe.
5. Đi tới khối “Lối vào phòng”, E lách qua cô Liên. Cô cúi tám giây, nhìn cổng ba giây, nón nhìn 90 độ/8 m có raycast chặn tường. Bị thấy chỉ cằn nhằn và lưu `lienSawMorning`, vẫn vào phòng, không bắt/đuổi/thua. Flag này để phần buổi tối dùng tiếp.
6. Trong phòng, E bốn lần ở khối “Ráp kệ”. Prompt báo tầng kế tiếp, khối kệ xuất hiện từng tầng. E ở đèn kẹp để bật thử, rồi tới nệm E nằm nghỉ. Nếu tiếp tục đi lại, chớp mắt và tự nằm xuống sau 60 giây chơi (thời gian bản thô, cần chơi thử để chỉnh nhịp).
7. Màn hình đen hai giây, tỉnh dậy 21:00. Lô trưa và tối xuất hiện theo thời điểm gửi, không phải tin gửi trong lúc người chơi thức. Đây là điểm dừng của lát cắt, chưa phải “Hết Đêm 2”.

Mỗi lần bật/tắt máy ghi `odoLog` và Sổ tay. Mốc 08:10 là 12.411 nếu Đêm 1 đi ngủ, 12.400 nếu thức canh; khi về 09:20 là 12.414/12.403. Sau giấc nghỉ tăng thêm 11 km, không tự ghi hay giải thích số đó cho người chơi; lần bật máy buổi tối sẽ được triển khai sau.

## Lưu, nạp và tương thích

`NightOneState.nightTwoStarted` phân biệt Đêm 1 với buổi sáng Đêm 2; `nightTwo` chứa stage, bốn bước ráp kệ, nhánh số phòng, mức nghi, đồng hồ xe và lịch nhìn. Cờ riêng là cần thiết vì `JsonUtility` có thể biến object inline null thành object rỗng khi sao chép. Save cũ thiếu cờ này vẫn là Đêm 1; không suy đoán đêm từ việc object có tồn tại hay không.

Tiếp tục dùng slot `night-one.json`, envelope version 1, checksum/backup/menu của #21. Lưu giữa lúc đi xe giữ vị trí dọc tuyến, tốc độ, thời gian dừng đèn đỏ và khóa đi bộ; lúc khôi phục không cộng lại số km. Không lưu nửa coroutine màn hình đen; thoát giữa lúc nghỉ quay lại snapshot ổn định trước đó. Chơi mới trở lại Đêm 1 và bật lại observer của Đêm 1.

## Kiểm tra tự động

Chạy trong Editor có giao diện, hai tiến trình riêng, cùng thư mục dữ liệu thử. Không dùng dữ liệu chơi thật, không thêm `-quit`:

```sh
THREEGIO12_DATA_ROOT=/tmp/3gio12-night2-review /path/to/Unity \
  -projectPath /path/to/repo/game -executeMethod NightTwoMorningRegression.Write \
  -logFile /tmp/night2-write.log

THREEGIO12_DATA_ROOT=/tmp/3gio12-night2-review /path/to/Unity \
  -projectPath /path/to/repo/game -executeMethod NightTwoMorningRegression.Read \
  -logFile /tmp/night2-read.log
```

Thành công có `NIGHT_TWO_WRITE_PASSED` và `NIGHT_TWO_READ_PASSED`, exit 0. Bài Write đi cả hai nhánh Đêm 1, kiểm thao tác sai thứ tự/ngoài tầm, phanh/đèn đỏ, save giữa tuyến, ghi số km, cổng thấy/không thấy/có tường, bốn tầng kệ, từ chối save NaN, rồi ghi khi ráp được hai tầng. Bài Read dùng menu khởi động thật để nạp slot ấy trong tiến trình mới, ráp tiếp, thử đèn, nghỉ tới 21:00, nạp lại và chơi mới Đêm 1. Ảnh kiểm tra HUD ghi vào `morning-preview.png` trong thư mục thử.

Các bài hồi quy Đêm 1 và save #21 vẫn phải đạt. Chưa thay bài chơi thử bằng tay hoặc kiểm Windows/Linux. Trước khi duyệt phần này cần chơi tuyến bằng W/S/Space/E và kiểm menu Esc, bố cục HUD, nhịp buồn ngủ.

## Phần còn lại của #22

- Buổi tối Đêm 2: lục máy, quán cơm, trà sữa, câu hỏi/đặt mã, chọn ngủ/thức và sang Đêm 3.
- Đêm 3: cây xăng, mẹ thật, camera/bằng chứng, mã két, pin/cáp/thuốc, nhánh kết thúc và hết đêm.
- Ghép điện thoại #17, bối cảnh/model của nhóm; thay bảng chữ và thoại giữ chỗ bằng nội dung được duyệt.
- Kiểm chơi liền mạch cả ba đêm và lưu/nạp các nhánh mới trước khi đóng issue.
