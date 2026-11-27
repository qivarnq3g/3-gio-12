# Tổng quan đề tài

Ghi chú cho nhóm (script dựng bỏ đoạn này): chương 1 do Quang soạn từ bản đăng ký đề tài (nộp 23/10/2026) và thuyết minh thiết kế (nộp 15/11/2026). Các mục 1.4 và 1.5 sao từ thuyết minh; nếu thuyết minh đổi thì sửa cả hai nơi.

## 1. Bối cảnh và lý do chọn đề tài

Học phần Đồ án thực tế công nghệ phần mềm yêu cầu nhóm làm một phần mềm hoàn chỉnh, có phân tích và thiết kế bằng lược đồ use case và lược đồ lớp, có hiện thực, kiểm thử, triển khai vận hành và kế hoạch khởi nghiệp dựa trên sản phẩm. Một game thuần túy khó đáp ứng yêu cầu về use case, vì phần lớn tương tác là trong màn chơi. Vì vậy nhóm làm game cùng một phân hệ hệ thống đi kèm: máy chủ nhận thống kê ẩn danh, trang quản trị cho quản trị viên và website câu đố ngoài game.

Nhóm chọn thể loại kinh dị hành động với bối cảnh khu nhà trọ ở TP.HCM, tin nhắn và đời sống sinh viên năm 2026. Thể loại kinh dị trên thị trường game Việt đã có nhiều sản phẩm (xem chương 4), nên nhóm chọn bối cảnh gần gũi với người chơi và cách chơi kết hợp điện thoại trong game, ẩn nấp trong không gian 3D và câu đố ngoài game để tạo điểm khác biệt.

## 2. Mục tiêu

Mục tiêu của đề tài:

- Xây dựng game chơi được từ đầu đến cuối ba đêm đầu, chạy offline trên máy tính Windows, không cần tài khoản.
- Xây dựng máy chủ nhận thống kê chỉ khi người chơi đồng ý, và kiểm tra bằng chứng của câu đố ngoài game bằng mã băm.
- Xây dựng trang quản trị để quản trị viên xem thống kê, xuất báo cáo và quản lý câu đố.
- Triển khai máy chủ, trang quản trị và website câu đố lên một máy ảo để người ngoài nhóm chơi thử.
- Làm việc nhóm theo quy trình có kiểm soát: chia việc bằng issue, mọi thay đổi qua pull request có người duyệt, kiểm thử tự động chạy khi gộp mã.

## 3. Giới thiệu phần mềm

3 GIỜ 12 là game kinh dị hành động 3D góc nhìn thứ nhất. Nhân vật người chơi điều khiển là một sinh viên, nhặt được chiếc điện thoại của Ngân, một nữ sinh viên mất tích, và bị cuốn vào việc tìm cô. Trong game, người chơi dùng cả hai máy: máy của Ngân và máy của chính mình. Người chơi đi lại trong các không gian 3D (phòng trọ, nhà xe, chung cư cũ, căn nhà nơi cô bị giam), lục điện thoại của Ngân để tìm manh mối, lẻn vào, ẩn nấp và chạy trốn khi bị phát hiện. Mỗi lựa chọn làm thay đổi diễn biến và dẫn tới nhiều kết thúc khác nhau. Kết thúc thật chỉ mở được khi người chơi giải một câu đố tiếp nối ra ngoài game, trên một website do nhóm dựng, theo lối trò chơi thực tế thay thế (alternate reality game) [@kim-arg]. Nội dung dành cho người chơi từ 18 tuổi.

Phần mềm gồm ba thành phần chính:

- Game (Unity [@unity-manual]): chạy offline trên máy tính Windows, không cần tài khoản.
- Máy chủ (ASP.NET Core [@ms-aspnetcore], PostgreSQL): nhận thống kê ẩn danh từ game [@game-analytics], kiểm tra bằng chứng của câu đố ngoài game.
- Trang quản trị (React [@react]): bảng điều khiển thống kê người chơi, quản lý câu đố ngoài game.

Ngoài ra có website câu đố là trang tĩnh, nơi người chơi tìm bằng chứng để mở kết thúc thật.

## 4. Phạm vi

Bản nộp ngày 03/12/2026 gồm toàn bộ máy chủ, trang quản trị và website câu đố, cùng ba đêm đầu của game (đêm 1 đến đêm 3) chơi được từ đầu đến cuối. Các đêm sau được thiết kế ở mức kịch bản và làm tiếp sau học phần. Bản nộp dùng cho chơi thử kín theo lời mời, chưa phát hành công khai.

Câu đố ngoài game cần nội dung của các đêm sau: chìa giải mã và địa chỉ trang tải nằm trong phần trong của két điện thoại, mở từ đêm 3 đến đêm 6, còn mục gửi bằng chứng chỉ mở sau kết thúc "Về nhà". Vì vậy, gửi bằng chứng và kết thúc thật nằm ngoài phạm vi bản nộp: người chơi chưa đi tới câu đố theo mạch truyện, và game không có lối mở thẳng mục gửi bằng chứng. Phần game, máy chủ và website câu đố của luồng này đã hiện thực và được kiểm bằng bài kiểm tự động; buổi trình diễn dùng website câu đố và API kiểm bằng chứng của máy chủ.

## 5. Yêu cầu chức năng

Yêu cầu chức năng mô tả những việc hệ thống phải làm cho từng nhóm người dùng [@sommerville].

Người chơi (game):

1. Điều khiển nhân vật 3D góc nhìn thứ nhất: đi, chạy, cúi, tương tác với đồ vật, mở cửa.
2. Chơi theo từng đêm (chương): khám phá màn chơi, thu thập vật chứng, mở khóa khu vực mới.
3. Dùng chiếc điện thoại trong game: đọc tin nhắn, xem ảnh, nghe ghi âm, trả lời các nhân vật; lựa chọn ảnh hưởng tới diễn biến và kết thúc.
4. Ẩn nấp, lẻn qua và chạy trốn khi bị phát hiện; bị bắt thì chơi lại từ điểm lưu gần nhất.
5. Tự lưu tiến độ trên máy; tiếp tục, chơi lại, xóa dữ liệu lưu.
6. Đồng ý hoặc từ chối gửi thống kê ẩn danh [@law-pdp]; dữ liệu được gom lại và gửi khi có mạng.
7. Gửi bằng chứng của câu đố ngoài game (tệp tìm được trên website đi kèm) để mở kết thúc thật.
8. Cài đặt: âm lượng, độ nhạy chuột, chất lượng đồ họa, cảnh báo nội dung.

Quản trị viên (trang quản trị):

9. Đăng nhập trang quản trị; quản lý tài khoản quản trị viên.
10. Xem bảng điều khiển thống kê: số người chơi, số người hoàn thành từng đêm, thời gian chơi, vị trí người chơi bị bắt hoặc bỏ game, tỉ lệ từng kết thúc, các lựa chọn phổ biến; lọc theo khoảng thời gian.
11. Xuất báo cáo thống kê ra tệp CSV (comma-separated values, giá trị phân tách bằng dấu phẩy) [@rfc4180].
12. Quản lý câu đố ngoài game: cấu hình bằng chứng hợp lệ, giới hạn số lần gửi, xem lịch sử gửi.

Hệ thống:

13. Máy chủ nhận sự kiện thống kê theo lô, chỉ khi người chơi đã đồng ý, không lưu thông tin định danh.
14. Máy chủ kiểm tra bằng chứng bằng mã băm SHA-256 (secure hash algorithm 256-bit) và giới hạn số lần thử [@fips180].

## 6. Thay đổi so với bản đăng ký

Bảng 1.1 ghi điểm duy nhất thay đổi so với bản đăng ký nộp ngày 23/10/2026.

| Nội dung | Bản đăng ký (23/10/2026) | Thiết kế hiện tại | Ngày quyết định |
|---|---|---|---|
| Góc nhìn của game | 3D góc nhìn thứ ba | 3D góc nhìn thứ nhất | 27/10/2026 |

: Thay đổi so với bản đăng ký đề tài

Lý do đổi góc nhìn:

- Cốt truyện cần giấu ngoại hình nhân vật chính tới cuối game. Góc nhìn thứ ba buộc người chơi luôn thấy toàn thân nhân vật (mặt, chiều cao, quần áo) và phải thêm nhiều luật camera gò bó để che.
- Góc nhìn thứ nhất chỉ cần dựng hai cánh tay cho nhân vật chính thay cho chuyển động toàn thân, giảm khối lượng dựng hình và hoạt ảnh trong thời gian của học phần.
- Các cơ chế chính giữ nguyên: khám phá, ẩn nấp, chạy trốn, dùng điện thoại trong game.

Yêu cầu chức năng 1 vì vậy đọc là "Điều khiển nhân vật 3D góc nhìn thứ nhất". Mười ba yêu cầu chức năng còn lại giữ nguyên như bản đăng ký.

## 7. Cấu trúc báo cáo

Chương 2 phân tích và thiết kế hệ thống. Chương 3 trình bày hiện thực, kiểm thử và triển khai. Chương 4 là kế hoạch khởi nghiệp. Phần kết luận tóm tắt kết quả.
