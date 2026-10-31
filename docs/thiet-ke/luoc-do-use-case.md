# Lược đồ use case

Tài liệu cho task #9, một trong ba phần bắt buộc của thuyết minh thiết kế nộp ngày 16/11/2026. Tác nhân và ánh xạ yêu cầu lấy từ mục 6 và 7 của `kien-truc-he-thong.md` (task #10). Bản vẽ gốc ở `use-case/luoc-do-use-case.drawio`, ba trang ứng với ba hình dưới đây; sửa trong draw.io rồi xuất lại ảnh PNG cùng tên. Số trong ngoặc là số yêu cầu chức năng trong bản đăng ký đề tài.

## 1. Tác nhân

| Tác nhân | Là ai | Dùng phần nào | Yêu cầu |
|---|---|---|---|
| Người chơi | Người cài và chơi game, không có tài khoản | Game; máy chủ API (qua game) | 1 đến 8, 13, 14 |
| Quản trị viên | Thành viên nhóm vận hành máy chủ | Trang quản trị | 9 đến 12 |

Game là một phần của hệ thống nên không vẽ thành tác nhân, kể cả ở lược đồ phân hệ máy chủ. Khi game gọi API (gửi thống kê, xóa thống kê, gửi bằng chứng), game làm thay người chơi, nên các use case đó nối với tác nhân Người chơi.

Website câu đố là trang tĩnh, không nói chuyện với máy chủ và không thu dữ liệu (mục 2.5 của tài liệu kiến trúc), nên không có use case riêng. Người chơi tải tệp ở đó rồi gửi bằng chứng qua game.

## 2. Lược đồ tổng quát

![Lược đồ use case tổng quát](use-case/tong-quat.png)

Lược đồ chỉ giữ use case mà tác nhân trực tiếp muốn làm. Các use case con của "Chơi một đêm" và phần máy chủ nằm ở hai lược đồ phân hệ.

## 3. Phân hệ game

![Lược đồ use case phân hệ game](use-case/phan-he-game.png)

- "Chơi một đêm" (2) luôn gồm "Điều khiển nhân vật" (1) và "Dùng điện thoại trong game" (3), nên nối bằng «include».
- "Ẩn nấp, chạy trốn" (4) chỉ xảy ra khi có người quan sát để ý tới người chơi, nên nối bằng «extend» vào "Chơi một đêm".
- Âm lượng, độ nhạy chuột, đồ họa và cảnh báo nội dung là các bước bên trong "Chỉnh cài đặt" (8), không tách thành use case riêng.
- "Lưu và tải tiến độ" (5) tách khỏi cài đặt: game tự lưu ở điểm lưu, người chơi trở về điểm lưu qua Sổ tay.

## 4. Phân hệ máy chủ và trang quản trị

![Lược đồ use case phân hệ máy chủ và trang quản trị](use-case/phan-he-may-chu.png)

- Bốn việc của quản trị viên đều cần phiên đăng nhập, nên cùng «include» "Đăng nhập" (9). Phần quản lý tài khoản thuộc yêu cầu 9 (đăng nhập, tài khoản quản trị).
- Ba use case của người chơi ở phía máy chủ ứng với ba API công khai mà game gọi thay người chơi: gửi lô thống kê (13), xóa thống kê theo mã cài đặt (6), gửi mã băm bằng chứng (14).

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

### 5.2. Lưu và tải tiến độ

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi |
| Yêu cầu | 5 |
| Luồng chính | 1. Khi tới điểm lưu, game ghi tiến độ và biến truyện vào tệp JSON trên máy. 2. Lần mở sau, người chơi chọn Tiếp tục. 3. Game đọc tệp và đưa người chơi về điểm lưu cuối. |
| Luồng khác | 2a. Người chơi chọn "Trở về điểm lưu" trong Sổ tay: game nạp lại điểm lưu gần nhất. 3a. Tệp lưu hỏng hoặc không đọc được: game báo lỗi và cho bắt đầu lại. |

### 5.3. Chỉnh cài đặt

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi |
| Yêu cầu | 8 |
| Luồng chính | 1. Người chơi mở Cài đặt. 2. Đổi âm lượng, độ nhạy chuột, chất lượng đồ họa hoặc bật, tắt cảnh báo nội dung. 3. Game áp dụng ngay và lưu vào tệp trên máy. |

### 5.4. Đồng ý hoặc rút đồng ý gửi thống kê

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi |
| Yêu cầu | 6 |
| Luồng chính | 1. Lần chạy đầu, game hỏi người chơi có đồng ý gửi thống kê ẩn danh không. 2. Người chơi đồng ý. 3. Game tạo mã cài đặt (installationId), từ đó ghi sự kiện vào hàng đợi trên đĩa. |
| Luồng khác | 2a. Người chơi từ chối: game không ghi sự kiện nào. 4. Sau này người chơi rút đồng ý trong Cài đặt: game xóa hàng đợi trên máy và gọi API xóa mọi sự kiện của mã cài đặt trên máy chủ. |

### 5.5. Gửi bằng chứng câu đố

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi |
| Yêu cầu | 7, 14 |
| Tiền điều kiện | Câu đố đang bật; người chơi đã giải mã tệp lấy từ website câu đố |
| Luồng chính | 1. Người chơi chọn tệp bằng chứng trong game. 2. Game tính mã băm SHA-256 trên máy; tệp không rời máy. 3. Game gửi mã băm lên máy chủ. 4. Máy chủ so với các mã băm hợp lệ. 5. Đúng: game nhận mã mở khóa và mở kết thúc thật. |
| Luồng khác | 4a. Sai: máy chủ trả số lượt còn lại. 4b. Sai quá 5 lần trong 60 phút: máy chủ tạm khóa và báo thời gian chờ. 3a. Mất mạng: game báo không gửi được, không trừ lượt. |

### 5.6. Đăng nhập

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên |
| Yêu cầu | 9 |
| Luồng chính | 1. Quản trị viên nhập email và mật khẩu ở trang quản trị. 2. Máy chủ kiểm tài khoản và cấp cookie phiên. 3. Trang quản trị mở dashboard. |
| Luồng khác | 2a. Sai email, sai mật khẩu hoặc tài khoản bị khóa: cùng một thông báo lỗi, không lộ email nào có tài khoản. 2b. Sai mật khẩu 5 lần liên tiếp: tài khoản bị tạm chặn đăng nhập 15 phút. |

### 5.7. Xem dashboard thống kê

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên |
| Yêu cầu | 10 |
| Tiền điều kiện | Đã đăng nhập |
| Luồng chính | 1. Quản trị viên chọn khoảng thời gian. 2. Máy chủ tổng hợp sự kiện theo giờ máy chủ nhận được. 3. Trang hiện số người chơi, số phiên, thời gian chơi, số người bắt đầu và hoàn thành từng đêm, lựa chọn, kết thúc, nơi người chơi bị bắt và nơi bỏ game. |

### 5.8. Xuất CSV

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên |
| Yêu cầu | 11 |
| Tiền điều kiện | Đã đăng nhập |
| Luồng chính | 1. Quản trị viên chọn loại dữ liệu (số liệu tổng hợp hoặc sự kiện) và khoảng thời gian. 2. Máy chủ tạo tệp CSV. 3. Trình duyệt tải tệp về. |

### 5.9. Quản lý câu đố

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên |
| Yêu cầu | 12 |
| Tiền điều kiện | Đã đăng nhập |
| Luồng chính | 1. Quản trị viên mở danh sách câu đố. 2. Bật, tắt câu đố hoặc đổi giới hạn lượt gửi. 3. Thêm hoặc bỏ mã băm hợp lệ. 4. Xem lịch sử gửi bằng chứng. |

### 5.10. Quản lý tài khoản quản trị

| Mục | Nội dung |
|---|---|
| Tác nhân | Quản trị viên |
| Yêu cầu | 9 |
| Tiền điều kiện | Đã đăng nhập |
| Luồng chính | 1. Quản trị viên xem danh sách tài khoản. 2. Tạo tài khoản mới, đổi tên hiển thị, khóa hoặc mở khóa, xóa tài khoản. 3. Đổi mật khẩu của mình hoặc đặt lại mật khẩu cho người khác. |

### 5.11. Gửi lô thống kê

| Mục | Nội dung |
|---|---|
| Tác nhân | Người chơi (game tự gửi sau khi người chơi đồng ý) |
| Yêu cầu | 13 |
| Tiền điều kiện | Người chơi đã đồng ý gửi thống kê; có mạng |
| Luồng chính | 1. Game lấy tối đa 100 sự kiện từ hàng đợi. 2. Gửi lên máy chủ. 3. Máy chủ lưu, bỏ qua sự kiện trùng mã. 4. Game xóa lô đã gửi khỏi hàng đợi. |
| Luồng khác | 2a. Mất mạng hoặc máy chủ lỗi: lô nằm lại hàng đợi, game thử lại sau thời gian chờ tăng dần; việc chơi không bị ảnh hưởng. |
