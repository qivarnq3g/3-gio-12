# Truy đuổi và ẩn nấp (#20)

## Phạm vi bản nộp

Bám [kịch bản Đêm 1, mục 4](../kich-ban/dem-1.md): truy đuổi ở màn 20:56, không chiến đấu và không chết. Đêm 2 chỉ né cô Liên ở cổng, Đêm 3 không có truy đuổi; không thêm tủ/gầm giường hay kẻ đuổi vào các cảnh chưa có trong kịch bản. Phần né cổng của Đêm 2 được nối cùng màn chơi ở #22.

Scene kiểm tra: `Assets/Scenes/Night01/NightOneGraybox.unity`. Các điểm nấp là hốc cửa, sau xe, gầm cầu thang và sau thùng rác, đặt bằng `NightOneDirector.hidingPoints` trong scene/setup. Bản 3D bối cảnh thay thế cần giữ collider che tầm nhìn và các điểm nấp này khi tích hợp.

## Luật hiện có và phần hoàn thiện

| Cơ chế | Hành vi |
|---|---|
| Tuần tra/lịch nhìn | Lịch riêng của từng observer; trẻ nhỏ di chuyển bằng NavMeshAgent, không xóa đường đi mỗi frame; hướng nhìn theo chuyển động khi đang đi |
| Thấy người chơi | Nón nhìn 90°, tầm 8 m; tường/xe che raycast. Chú ý tăng theo sáng/tối, cúi và chạy |
| Nghe | Tiếng chạy làm observer gần nhất đủ điều kiện trong 5 m quay về nguồn; tiếng thở khi nấp sau lần bắt thứ hai được mọi observer đang hiện diện trong 3 m nghe thấy, kể cả đang nghi/đuổi/tìm; tiếng hô lan trong 15 m |
| Observer vắng mặt | Cô Liên chưa tới giờ và actor inactive không phản ứng. Hết khung giờ 40–55 giây chỉ ẩn nếu không còn nghi/đuổi/tìm; cuộc đuổi đang diễn ra được tiếp tục, mất dấu thì tìm tới khoảng 20 giây rồi mới ẩn |
| Nghi → đuổi → tìm | Tiếp cận vị trí nhìn thấy lần cuối; đuổi khi phát hiện trong nhà xe/đặt máy hoặc thấy lại; mất tầm nhìn thì tìm rồi bỏ |
| Cắt dấu | Đứng yên ở điểm nấp và ngoài tầm nhìn tất cả observer liên tục 8 giây; di chuyển quá 5 cm khỏi vị trí bắt đầu đếm hoặc bị nhìn thấy thì đếm lại |
| Đèn pin | Phải rọi đúng liên tục 1 giây, trong 5 m, có tầm nhìn. Lệch tia hoặc bị che làm thời gian rọi về 0 |
| Bắt gần | Kẻ đuổi trong 1,2 m bắt được cả bên hông/sau lưng nếu raycast giữa hai người không bị che; tường/xe vẫn chặn bắt, độc lập với nón nhìn 90° |
| Đặt máy | Cúi, giữ E 2 giây, không có người đang nghi/đuổi/tìm. Đặt xong thì đường quay về không còn bị bắt |

`NightObserver` giữ máy trạng thái và NavMesh; `NightOneDirector` điều phối tiếng động, điểm nấp, hậu quả bị bắt và chuyển màn. Các observer đứng yên không bắt buộc có NavMeshAgent; mọi truy cập agent ở Update/Start được kiểm tra null.

## Bị bắt và checkpoint theo kịch bản

Issue yêu cầu chơi lại từ điểm gần nhất; tại prologue, kịch bản quy định chi tiết hơn thay vì chết và load lại toàn bộ:

- Lần 1: chuyển đến chỗ nấp gần nhất, giữ thương tích và tăng cảnh giác.
- Lần 2: đến chỗ nấp gần nhất, giữ số lần bị bắt, giảm tốc chạy còn 2/3 và có phản hồi thở nặng.
- Lần 3: reset toàn bộ màn về `n1.alley_start`, đồng hồ vẫn 20:56, xóa hậu quả hai lần trước.
- Mỗi lần bị bắt phát `player_caught` với area thực tế và checkpoint `n1.alley_start`. Tự quay ra đầu hẻm khi chưa đặt máy không phát sự kiện bị bắt.
- Hồi phục an toàn khi rơi khỏi bản đồ hoặc bấm checkpoint ở prologue không xóa thương tích; checkpoint sau prologue vẫn dùng snapshot riêng như #21.

Tiếng thở cập nhật vị trí âm thanh và hướng quay của người đang tìm mà không đặt lại trạng thái hoặc timeout; tiếng chạy vẫn chỉ gọi người gần nhất đang Bình thường.

Timer nấp được lưu cùng dữ liệu #21 và khôi phục với vị trí người chơi. Field `hidingSeconds` mới mặc định 0 khi đọc save v1 cũ, không đổi envelope/schema và không vô hiệu hóa các save cũ chỉ vì thêm timer này. Bộ đếm `unseenSeconds` cũ đã bỏ vì không còn quyết định hành vi; payload cũ chứa field này vẫn đọc được (field thừa được bỏ qua).

## Kiểm tra

Dùng Unity `6000.3.26f1`, thư mục dữ liệu thử riêng, không dùng save thật:

```sh
THREEGIO12_DATA_ROOT=/tmp/3gio12-stealth-review \
  /path/to/Unity -projectPath /path/to/repo/game \
  -executeMethod StealthRegression.Run -logFile /tmp/stealth-review.log
```

Script tự vào Play Mode và thoát; không thêm `-quit`. Log thành công: `STEALTH_REGRESSION_PASSED`. Kiểm tra trong scene thật: raycast che bởi tường, đèn pin ngắt quãng, bắt xuyên tường và bắt sau lưng, tiếng thở tới nhiều observer gồm người đang tìm, cuộc đuổi qua mốc 55 giây và timeout tìm, save cũ có field thừa, tiếng hô với observer chưa xuất hiện, nấp khi di chuyển/đứng yên, và tuần tra có dịch chuyển NavMesh theo thời gian mô phỏng.

Chạy thêm các bài cũ `NightOneSmoke.Run`, `NightOneReviewRegression.Run` với `THREEGIO12_SKIP_START_MENU=1` và dữ liệu thử riêng để xác minh ba lần bị bắt, thoát prologue, các nhánh Đêm 1 và checkpoint. `SaveProgressRegression.Write` rồi `.Read` ở hai tiến trình riêng kiểm tra tương thích phần lưu tiến độ.

### Chơi thử trước khi duyệt

1. Đi/cúi qua các nón nhìn, chạy gần hàng xóm để kiểm tra phản ứng nghe và thanh chú ý.
2. Để bị phát hiện trong nhà xe: chạy thoát đến điểm nấp, còn di chuyển thì chưa cắt dấu; đứng yên ngoài tầm nhìn 8 giây thì thoát.
3. Để đèn pin rọi ngắt quãng rồi liên tục: chỉ lần rọi liên tục đủ một giây mới bị tóm. Tường/xe phải che được tia.
4. Bị bắt đủ ba lần và kiểm tra vị trí hồi phục, hình phạt, đồng hồ. Chơi lại, đặt máy thành công rồi quay về để xác nhận không bị bắt sau khi đặt.
5. Tự lưu giữa truy đuổi/nấp, đóng rồi mở lại: tiến độ, observer và timer nấp tiếp tục theo save, không bị xóa hình phạt.
