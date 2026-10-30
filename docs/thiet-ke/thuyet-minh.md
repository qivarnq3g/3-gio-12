::: {custom-style="Cover Org"}
Trường Đại học Giao thông vận tải Thành phố Hồ Chí Minh
:::

::: {custom-style="Cover Org"}
Học phần Đồ án thực tế công nghệ phần mềm - lớp 012012213905
:::

::: {custom-style="Cover Title"}
Thuyết minh chi tiết thiết kế
:::

::: {custom-style="Cover Subtitle"}
Phần mềm 3 GIỜ 12: game kinh dị hành động 3D góc nhìn thứ nhất, kèm trang quản trị thống kê người chơi
:::

::: {custom-style="Cover Text"}
Nhóm 3 GIỜ 12
:::

| MSSV | Họ và tên | Vai trò chính |
|---|---|---|
| .................... | .................... (nhóm trưởng) | Quản lý dự án, máy chủ API, bối cảnh 3D, kịch bản |
| .................... | .................... | Lập trình game |
| .................... | .................... | Trang quản trị, giao diện điện thoại trong game, kiểm thử |
| .................... | .................... | Model 3D, biên bản họp |

::: {custom-style="Cover Text"}
Thành phố Hồ Chí Minh, tháng 11 năm 2026
:::

# Giới thiệu

## Mục đích tài liệu

Tài liệu mô tả thiết kế của phần mềm 3 GIỜ 12 ở mức đủ để lập trình và kiểm thử, gồm ba phần theo yêu cầu của học phần: kiến trúc hệ thống, lược đồ use case (ca sử dụng) và lược đồ lớp. Tài liệu bám theo bản đăng ký đề tài nộp ngày 23/10/2026; những điểm đã thay đổi từ đó được ghi ở chương 2.

## Giới thiệu phần mềm

3 GIỜ 12 là game kinh dị hành động 3D góc nhìn thứ nhất. Người chơi nhặt được chiếc điện thoại của một nữ sinh viên mất tích và bị cuốn vào việc tìm cô. Người chơi đi lại trong các không gian 3D (phòng trọ, nhà xe, chung cư cũ, căn nhà nơi cô bị giam), lục chiếc điện thoại để tìm manh mối, lẻn vào, ẩn nấp và chạy trốn khi bị phát hiện. Mỗi lựa chọn làm thay đổi diễn biến và dẫn tới nhiều kết thúc khác nhau. Kết thúc thật chỉ mở được khi người chơi giải một câu đố tiếp nối ra ngoài game, trên một website do nhóm dựng. Nội dung dành cho người chơi từ 18 tuổi.

Phần mềm gồm ba thành phần chính:

- Game (Unity): chạy offline trên máy tính Windows, không cần tài khoản.
- Máy chủ (ASP.NET Core, PostgreSQL): nhận thống kê ẩn danh từ game, kiểm tra bằng chứng của câu đố ngoài game.
- Trang quản trị (React): bảng điều khiển thống kê người chơi, quản lý câu đố ngoài game.

Ngoài ra có website câu đố là trang tĩnh, nơi người chơi tìm bằng chứng để mở kết thúc thật.

## Phạm vi bản nộp cuối kỳ

Bản nộp ngày 03/12/2026 gồm toàn bộ máy chủ, trang quản trị và website câu đố, cùng ba đêm đầu của game (đêm 1 đến đêm 3) chơi được từ đầu đến cuối. Các đêm sau được thiết kế ở mức kịch bản và làm tiếp sau học phần. Bản nộp dùng cho chơi thử kín theo lời mời, chưa phát hành công khai.

## Yêu cầu chức năng

Người chơi (game):

1. Điều khiển nhân vật 3D góc nhìn thứ nhất: đi, chạy, cúi, tương tác với đồ vật, mở cửa.
2. Chơi theo từng đêm (chương): khám phá màn chơi, thu thập vật chứng, mở khóa khu vực mới.
3. Dùng chiếc điện thoại trong game: đọc tin nhắn, xem ảnh, nghe ghi âm, trả lời các nhân vật; lựa chọn ảnh hưởng tới diễn biến và kết thúc.
4. Ẩn nấp, lẻn qua và chạy trốn khi bị phát hiện; bị bắt thì chơi lại từ điểm lưu gần nhất.
5. Tự lưu tiến độ trên máy; tiếp tục, chơi lại, xóa dữ liệu lưu.
6. Đồng ý hoặc từ chối gửi thống kê ẩn danh; dữ liệu được gom lại và gửi khi có mạng.
7. Gửi bằng chứng của câu đố ngoài game (tệp tìm được trên website đi kèm) để mở kết thúc thật.
8. Cài đặt: âm lượng, độ nhạy chuột, chất lượng đồ họa, cảnh báo nội dung.

Quản trị viên (trang quản trị):

9. Đăng nhập trang quản trị; quản lý tài khoản quản trị viên.
10. Xem bảng điều khiển thống kê: số người chơi, số người hoàn thành từng đêm, thời gian chơi, vị trí người chơi bị bắt hoặc bỏ game, tỉ lệ từng kết thúc, các lựa chọn phổ biến; lọc theo khoảng thời gian.
11. Xuất báo cáo thống kê ra tệp CSV (comma-separated values, giá trị phân tách bằng dấu phẩy).
12. Quản lý câu đố ngoài game: cấu hình bằng chứng hợp lệ, giới hạn số lần gửi, xem lịch sử gửi.

Hệ thống:

13. Máy chủ nhận sự kiện thống kê theo lô, chỉ khi người chơi đã đồng ý, không lưu thông tin định danh.
14. Máy chủ kiểm tra bằng chứng bằng mã băm SHA-256 (secure hash algorithm 256-bit) và giới hạn số lần thử.

## Quy ước trong tài liệu

- Sơ đồ được vẽ bằng Mermaid từ mã nguồn tài liệu trong kho mã của nhóm, nên sơ đồ luôn khớp với văn bản.
- Tên lớp, thuộc tính, bảng dữ liệu và đường dẫn API giữ nguyên như trong mã nguồn (tiếng Anh).
- Hợp đồng API (application programming interface, giao diện lập trình ứng dụng) giữa game và máy chủ được đặc tả bằng OpenAPI. Khi tài liệu này lệch với hợp đồng API, hợp đồng API là bản đúng.

# Thay đổi so với bản đăng ký

| Nội dung | Bản đăng ký (23/10/2026) | Thiết kế hiện tại | Ngày quyết định |
|---|---|---|---|
| Góc nhìn của game | 3D góc nhìn thứ ba | 3D góc nhìn thứ nhất | 27/10/2026 |

Lý do đổi góc nhìn:

- Cốt truyện cần giấu ngoại hình nhân vật chính tới cuối game. Góc nhìn thứ ba buộc người chơi luôn thấy toàn thân nhân vật (mặt, chiều cao, quần áo) và phải thêm nhiều luật camera gò bó để che.
- Góc nhìn thứ nhất chỉ cần dựng hai cánh tay cho nhân vật chính thay cho chuyển động toàn thân, giảm khối lượng dựng hình và hoạt ảnh trong thời gian của học phần.
- Các cơ chế chính giữ nguyên: khám phá, ẩn nấp, chạy trốn, dùng điện thoại trong game.

Yêu cầu chức năng 1 vì vậy đọc là "Điều khiển nhân vật 3D góc nhìn thứ nhất". Mười ba yêu cầu chức năng còn lại giữ nguyên như bản đăng ký.
