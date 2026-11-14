# Đêm 2: buổi tối và khuya (#22, đang làm)

Nối tiếp lát cắt buổi sáng (`night-two-morning.md`) từ lúc tỉnh dậy 21:00 ngày 25/11 tới chữ "Hết đêm 2". Đối chiếu `docs/kich-ban/dem-2.md` mục 2, 5.4, 5.5, 6, 7, 8. Vẫn là khối thô dựng lúc chạy trong `NightOneGraybox`, dùng chung `NightTwoMorningState`, chưa ghép bối cảnh thật, giao diện điện thoại #17 hay âm thanh.

## Chơi thử

1. Chơi qua buổi sáng Đêm 2 tới lúc nằm nghỉ. Tỉnh dậy 21:00; đồng hồ trong game chạy 1 phút mỗi 4 giây khi đang ở nhà hoặc ở quán cơm, dừng ở 22:59.
2. Máy Ngân (phím 1) có bảy mục: Tin nhắn, Ảnh, Bộ nhớ, Máy tính, Video, Linh, Khóa màn hình.
   - Tin nhắn: lô trưa (12:20) và lô tối (20:10) của "Mẹ" theo nhánh Đêm 1 và câu trả lời buổi sáng; tin dãy số lạ 21:15, 21:16, 21:40, 23:30 tự thành "Đã xem", không có nếu Đêm 1 đã chặn số.
   - Câu gửi "Mẹ" chỉ hiện khi đã thấy thứ liên quan: Q4 sau khi nhận trà sữa, Q5 sau khi xem Ảnh, Q6 sau khi xem Bộ nhớ, Q7 sau tin 21:16. Nhánh thức canh Đêm 1 có thêm câu trả lời số phòng. "Mẹ" không đáp trong lúc người chơi thức; câu đáp thuộc Đêm 3.
   - Máy tính: gõ số rồi bấm "=" thì đếm một lần thử két (`vaultWrongAttempts`), không có phản hồi trên màn hình.
   - Khóa màn hình: đặt mã máy Ngân (lựa chọn 15) làm "Mẹ" lên mức Nghi.
3. Xe máy đã nằm chỗ khác trong nhà xe. E để bật máy, chạy ra quán cơm (không có đèn đỏ), mua cơm, bật máy ở xe để về. Mỗi lượt cộng 0,5 km; Sổ tay ghi 21:30 và lúc tắt máy về.
4. Trà sữa (chỉ khi Đêm 1 đi ngủ và đã cho số phòng): tới lúc 21:10, cô Liên gọi nếu người chơi đang ở nhà, hoặc nói lúc người chơi về tới nếu đang ở quán. Lấy ly ở bàn cô Liên, chọn Uống hay Để đó.
5. Ăn xong, tới nệm: Đi ngủ hoặc Thức canh tới sáng.
   - Đi ngủ: màn đen rồi "Hết đêm 2".
   - Thức canh khi Đêm 1 đã đi ngủ: các mốc 23:30 tới 02:30, rồi báo thức 03:12 reo.
   - Thức canh hai đêm liền: người chơi đứng trong phòng 30 giây, màn hình chớp tắt; đứng yên 10 giây liền là ngủ gật, lỡ 03:12.

## Biến lưu cho Đêm 3

Trong `NightTwoMorningState`: `odoLog`, `roomReply`, `momSuspicion`, `teaArrived`, `teaTaken`, `drankMilkTea`, `vaultWrongAttempts`, `sawStorage`, `sawPhotos`, `sawVlog`, `sawLinhChat`, `setNganPasscode`, `lienSawMorning`, `night2Slept`, `vigilDozed`, `strangerDelivered`, `eveningSent` (các câu Q4 tới Q7 và câu số phòng, để lô 03:40 của Đêm 3 trả lời).

## Kiểm tra tự động

```sh
THREEGIO12_DATA_ROOT=<thư mục thử> Unity -batchmode -projectPath <repo>/game -executeMethod NightTwoEveningRegression.Run -logFile <log>
```

Thành công có dòng `NIGHT_TWO_EVENING_PASSED`. Bài chạy ba nhánh: Đêm 1 đi ngủ và cho số phòng (trà sữa, câu hỏi, két, đặt mã, lưu và nạp, đi ngủ); thức canh hai đêm (trả lời số phòng buổi tối, ngủ gật); Đêm 1 đi ngủ, từ chối số phòng, đã chặn số lạ (mức Nghi, báo thức 03:12, nạp lại giữa lúc báo thức reo).

## Chưa có

- Lời thoại Sinh viên ở cổng, quán cơm và các câu nghĩ hiện bằng phụ đề; chưa có giọng, ảnh, video thật.
- Tin dãy số lạ chưa có giờ máy chủ thật; giờ trong game chạy theo hằng số `SecondsPerMinute`.
- Câu T4 dòng đầu ("À cái báo thức đó hả cháu") chưa có vì Đêm 1 chưa có câu hỏi P10 về báo thức.
- Thống kê ẩn danh của Đêm 2 (`night_completed`, mã khu vực `n2.*`) chưa phát.

![Máy Ngân buổi tối, mục Tin nhắn](screenshots/night-two-evening.png)
