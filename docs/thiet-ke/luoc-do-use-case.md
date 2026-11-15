# Lược đồ use case

Tài liệu cho task #9, một trong ba phần bắt buộc của thuyết minh thiết kế nộp ngày 16/11/2026. Tác nhân và ánh xạ yêu cầu lấy từ mục 6 và 7 của `kien-truc-he-thong.md` (task #10). Bản vẽ gốc ở `use-case/luoc-do-use-case.drawio`, ba trang ứng với ba hình dưới đây; sửa trong draw.io rồi xuất lại ảnh PNG cùng tên. Số trong ngoặc là số yêu cầu chức năng trong bản đăng ký đề tài.

## 1. Tác nhân

| Tác nhân | Mô tả | Dùng phần nào | Yêu cầu |
|---|---|---|---|
| Người chơi | Người cài và chơi game, không có tài khoản | Game; máy chủ API (qua game) | 1 đến 8, 13, 14 |
| Quản trị viên | Thành viên nhóm vận hành máy chủ | Trang quản trị | 9 đến 12 |

: Các tác nhân trong lược đồ use case

Game là một phần của hệ thống nên không vẽ thành tác nhân. Gửi lô thống kê và xóa thống kê trên máy chủ do game tự làm, người chơi không chủ động yêu cầu, nên không nối thẳng với tác nhân mà vẽ thành use case mở rộng («extend») của use case người chơi trực tiếp làm.

Website câu đố là trang tĩnh, không nói chuyện với máy chủ và không thu dữ liệu (mục 2.5 của tài liệu kiến trúc), nên không có use case riêng. Người chơi tải tệp ở đó rồi gửi bằng chứng qua game.

## 2. Lược đồ tổng quát

![Lược đồ use case tổng quát](use-case/tong-quat.png)

Lược đồ chỉ giữ use case mà tác nhân trực tiếp muốn làm. Các use case con và use case mở rộng nằm ở hai lược đồ phân hệ.

## 3. Phân hệ game

![Lược đồ use case phân hệ game](use-case/phan-he-game.png)

- "Chơi một đêm" (1, 2) luôn gồm "Dùng điện thoại trong game" (3), nên nối bằng «include» [@uml]. Điều khiển nhân vật (yêu cầu 1) là thao tác bên trong mỗi đêm, không phải mục tiêu riêng của người chơi, nên không tách thành use case.
- "Ẩn nấp, chạy trốn" (4) chỉ xảy ra khi có người quan sát để ý tới người chơi, nên nối bằng «extend» vào "Chơi một đêm" [@uml].
- Âm lượng, độ nhạy chuột, đồ họa và cảnh báo nội dung là các bước bên trong "Chỉnh cài đặt" (8), không tách thành use case riêng.
- "Gửi lô thống kê" (13) mở rộng "Chơi một đêm" khi người chơi đã đồng ý gửi thống kê. "Xóa thống kê trên máy chủ" (6) mở rộng "Đồng ý hoặc rút đồng ý gửi thống kê" khi người chơi rút đồng ý.
- "Tiếp tục từ điểm lưu" (5): game tự lưu ở điểm lưu; việc người chơi chủ động làm là tiếp tục chơi hoặc trở về điểm lưu qua Sổ tay.

## 4. Phân hệ trang quản trị

![Lược đồ use case phân hệ trang quản trị](use-case/phan-he-may-chu.png)

- Bốn việc của quản trị viên đều cần phiên đăng nhập, nên cùng «include» "Đăng nhập" (9). Phần quản lý tài khoản thuộc yêu cầu 9 (đăng nhập, tài khoản quản trị).
- Máy chủ còn ba API công khai mà game gọi: gửi lô thống kê, xóa thống kê, gửi bằng chứng. Chúng là các bước bên trong use case của người chơi ở mục 3 và mục 5, nên không vẽ lại ở đây.

## 5. Đặc tả use case

### 5.1. Chơi một đêm

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi |
| Yêu cầu | 1, 2, 3, 4 |
| Tiền điều kiện | Đã mở khóa đêm cần chơi |
| Luồng chính | 1. Người chơi chọn đêm. 2. Game nạp màn đầu và giờ trong game. 3. Người chơi di chuyển, tương tác, đọc và trả lời tin trên điện thoại. 4. Các sự kiện xảy ra theo mốc giờ và lựa chọn. 5. Đêm kết thúc, game lưu tiến độ và mở đêm kế tiếp. |
| Luồng khác | 3a. Người quan sát phát hiện người chơi: chuyển sang ẩn nấp, chạy trốn. Bị bắt thì về điểm lưu gần nhất. 4a. Người chơi chọn một lựa chọn dẫn tới kết thúc sớm: game hiện kết thúc đó. |
| Kết quả | Biến truyện và lựa chọn được lưu; nếu người chơi đã đồng ý, sự kiện thống kê được ghi vào hàng đợi |

: Đặc tả use case “Chơi một đêm”

### 5.2. Tiếp tục từ điểm lưu

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi |
| Yêu cầu | 5 |
| Luồng chính | 1. Khi tới điểm lưu, game ghi tiến độ và biến truyện vào tệp JSON trên máy. 2. Lần mở sau, người chơi chọn Tiếp tục. 3. Game đọc tệp và đưa người chơi về điểm lưu cuối. |
| Luồng khác | 2a. Người chơi chọn "Trở về điểm lưu" trong Sổ tay: game nạp lại điểm lưu gần nhất. 3a. Tệp lưu hỏng hoặc không đọc được: game báo lỗi và cho bắt đầu lại. |

: Đặc tả use case “Tiếp tục từ điểm lưu”

### 5.3. Chỉnh cài đặt

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi |
| Yêu cầu | 8 |
| Luồng chính | 1. Người chơi mở Cài đặt. 2. Đổi âm lượng, độ nhạy chuột, chất lượng đồ họa hoặc bật, tắt cảnh báo nội dung. 3. Game áp dụng ngay và lưu vào tệp trên máy. |

: Đặc tả use case “Chỉnh cài đặt”

### 5.4. Đồng ý hoặc rút đồng ý gửi thống kê

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi |
| Yêu cầu | 6 |
| Luồng chính | 1. Lần chạy đầu, game hỏi người chơi có đồng ý gửi thống kê ẩn danh không. 2. Người chơi đồng ý. 3. Game tạo mã cài đặt (installationId), từ đó ghi sự kiện vào hàng đợi trên đĩa. |
| Luồng khác | 2a. Người chơi từ chối: game không ghi sự kiện nào. *a. Bất kỳ lúc nào sau đó, người chơi rút đồng ý trong Cài đặt: game xóa hàng đợi trên máy và gọi API xóa mọi sự kiện của mã cài đặt trên máy chủ. |

: Đặc tả use case “Đồng ý hoặc rút đồng ý gửi thống kê”

### 5.5. Gửi bằng chứng câu đố

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi |
| Yêu cầu | 7, 14 |
| Tiền điều kiện | Người chơi đã đạt kết thúc "Về nhà", nhờ đó mục gửi bằng chứng trong game được mở; câu đố đang bật; người chơi đã giải mã tệp lấy từ website câu đố |
| Luồng chính | 1. Người chơi chọn tệp bằng chứng trong game. 2. Game tính mã băm SHA-256 trên máy; tệp không rời máy. 3. Game gửi mã băm lên máy chủ. 4. Máy chủ so với các mã băm hợp lệ. 5. Đúng: game nhận mã mở khóa và mở kết thúc thật. |
| Luồng khác | 3a. Mất mạng: game báo không gửi được, không trừ lượt. 4a. Sai: máy chủ trả số lượt còn lại. 4b. Đã sai đủ số lượt trong cửa sổ thời gian (mặc định 5 lần trong 60 phút, quản trị viên đổi được): máy chủ chặn lần gửi này, kể cả khi mã băm đúng, và báo thời gian chờ. |

: Đặc tả use case “Gửi bằng chứng câu đố”

### 5.6. Đăng nhập

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên |
| Yêu cầu | 9 |
| Luồng chính | 1. Quản trị viên nhập email và mật khẩu ở trang quản trị. 2. Máy chủ kiểm tài khoản và cấp cookie phiên. 3. Trang quản trị mở dashboard. |
| Luồng khác | 2a. Sai email, sai mật khẩu hoặc tài khoản bị khóa: cùng một thông báo lỗi, không lộ email nào có tài khoản. 2b. Sai mật khẩu 5 lần liên tiếp: tài khoản bị tạm chặn đăng nhập 15 phút. |

: Đặc tả use case “Đăng nhập”

### 5.7. Xem dashboard thống kê

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên |
| Yêu cầu | 10 |
| Tiền điều kiện | Đã đăng nhập |
| Luồng chính | 1. Quản trị viên chọn khoảng thời gian. 2. Máy chủ tổng hợp sự kiện theo giờ máy chủ nhận được. 3. Trang hiện số người chơi, số phiên, thời gian chơi, số người bắt đầu và hoàn thành từng đêm, lựa chọn, kết thúc, nơi người chơi bị bắt và nơi bỏ game. |

: Đặc tả use case “Xem dashboard thống kê”

### 5.8. Xuất CSV

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên |
| Yêu cầu | 11 |
| Tiền điều kiện | Đã đăng nhập |
| Luồng chính | 1. Quản trị viên chọn loại dữ liệu (số liệu tổng hợp hoặc sự kiện) và khoảng thời gian. 2. Máy chủ tạo tệp CSV. 3. Trình duyệt tải tệp về. |

: Đặc tả use case “Xuất CSV”

### 5.9. Quản lý câu đố

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên |
| Yêu cầu | 12 |
| Tiền điều kiện | Đã đăng nhập |
| Luồng chính | 1. Quản trị viên mở danh sách câu đố. 2. Bật, tắt câu đố hoặc đổi giới hạn lượt gửi. 3. Thêm hoặc bỏ mã băm hợp lệ. 4. Xem lịch sử gửi bằng chứng. |

: Đặc tả use case “Quản lý câu đố”

### 5.10. Quản lý tài khoản quản trị

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên |
| Yêu cầu | 9 |
| Tiền điều kiện | Đã đăng nhập |
| Luồng chính | 1. Quản trị viên xem danh sách tài khoản. 2. Tạo tài khoản mới, đổi tên hiển thị, khóa hoặc mở khóa, xóa tài khoản. 3. Đổi mật khẩu của mình hoặc đặt lại mật khẩu cho người khác. |

: Đặc tả use case “Quản lý tài khoản quản trị”

### 5.11. Gửi lô thống kê

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi (game tự gửi sau khi người chơi đồng ý) |
| Yêu cầu | 13 |
| Tiền điều kiện | Người chơi đã đồng ý gửi thống kê; có mạng |
| Luồng chính | 1. Game lấy tối đa 100 sự kiện từ hàng đợi. 2. Gửi lên máy chủ. 3. Máy chủ lưu, bỏ qua sự kiện trùng mã. 4. Game xóa lô đã gửi khỏi hàng đợi. |
| Luồng khác | 2a. Mất mạng hoặc máy chủ lỗi: lô nằm lại hàng đợi, game thử lại sau thời gian chờ tăng dần; việc chơi không bị ảnh hưởng. |

: Đặc tả use case “Gửi lô thống kê”
