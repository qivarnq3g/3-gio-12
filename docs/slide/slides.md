# 3 GIỜ 12
- Game kinh dị hành động 3D góc nhìn thứ nhất, kèm máy chủ và trang quản trị
- Đồ án thực tế công nghệ phần mềm, lớp 012012213905
- Trường Đại học Giao thông vận tải TP.HCM
- Nhóm: Quang, Phúc, Tiên, Thắng
- Ảnh nền: ảnh tham chiếu bối cảnh, tạo bằng AI
> Mở đầu 30 giây: tên sản phẩm, ba thành phần, nhóm. Ảnh hẻm đêm là ảnh tham chiếu bối cảnh tạo bằng AI, không phải ảnh trong game; nếu được hỏi thì nói rõ, model 3D trong game do nhóm tự dựng bằng Blender. Ghi tên người trình bày từng phần vào ghi chú của slide tương ứng trước khi tập.

---

# Người chơi nhặt được một chiếc điện thoại
~ SẢN PHẨM
- Máy của Ngân, một nữ sinh viên mất tích
- Đọc tin nhắn, xem ảnh, nghe ghi âm để lần theo manh mối
- Ẩn nấp và chạy trốn, không chiến đấu
![Điện thoại của Ngân trong game](docs/slide/anh/dien-thoai-ngan.png)
> Ảnh chụp thật từ game: máy của Ngân với tin nhắn của mẹ và lựa chọn trả lời. Nói ngắn cốt truyện, không kể kết thúc. Nội dung dành cho người chơi từ 18 tuổi.

---

# Một vòng chơi
~ SẢN PHẨM
::: flow
- Đọc điện thoại | tin nhắn, ảnh, ghi âm
- Tìm manh mối | đi lại trong không gian 3D
- Ẩn nấp, chạy trốn | khi bị phát hiện
- Lựa chọn | đổi diễn biến
- Kết thúc | nhiều kết thúc khác nhau
:::
![Điện thoại của Ngân trong game](docs/slide/anh/dien-thoai-ngan.png){pair}
![Trụ sở công an phường, dựng 3D trong Unity](docs/slide/anh/cong-an-phuong.jpg){pair}
> Hai ảnh đều chụp từ game. Kết thúc thật chỉ mở khi người chơi giải câu đố ngoài game (slide sau).

---

# Ba thành phần và một website
~ SẢN PHẨM
::: cards
- Game | Unity, chạy offline trên Windows, không cần tài khoản | G
- Máy chủ | ASP.NET Core và PostgreSQL: nhận thống kê ẩn danh, kiểm bằng chứng | M
- Trang quản trị | React: dashboard thống kê, xuất CSV, quản lý câu đố | Q
- Website câu đố | Trang tĩnh: nơi người chơi tìm bằng chứng để mở kết thúc thật | W
:::
> Lý do có phần hệ thống quanh game: học phần cần use case và lớp có nội dung, game thuần túy khó đáp ứng. Bản nộp gồm toàn bộ hệ thống và Đêm 1 đến Đêm 3; Đêm 4 đến 7 làm tiếp sau học phần.

---

# Câu đố nằm ngoài game
~ SẢN PHẨM
- Website dựng như blog của chính Ngân
- Bằng chứng nằm trong tệp phải giải mã
- Người chơi gửi mã băm lên máy chủ để mở kết thúc thật
![Website câu đố](docs/slide/anh/website-cau-do.png)
> Tệp bằng chứng không rời máy người chơi, chỉ mã băm SHA-256 được gửi. Máy chủ giới hạn số lần gửi sai (mặc định 5 lần trong 60 phút).

---

# 14 yêu cầu chức năng
~ YÊU CẦU
::: stats
- 8 | Người chơi | điều khiển, chơi theo đêm, điện thoại, ẩn nấp, lưu tiến độ, đồng ý thống kê, gửi bằng chứng, cài đặt
- 4 | Quản trị viên | đăng nhập và tài khoản, dashboard, xuất CSV, quản lý câu đố
- 2 | Hệ thống | nhận thống kê theo lô khi người chơi đồng ý; kiểm bằng chứng bằng SHA-256
:::
> Đổi so với bản đăng ký: duy nhất góc nhìn, từ thứ ba sang thứ nhất (27/10), vì cốt truyện cần giấu ngoại hình nhân vật chính và giảm khối lượng dựng hình.

---

# Use case: người chơi và quản trị viên
~ THIẾT KẾ
![Lược đồ use case tổng quát](docs/thiet-ke/use-case/tong-quat.png){full}
> Hai tác nhân. Đăng nhập là use case dùng chung của bốn chức năng quản trị.

---

# Kiến trúc: năm thành phần
~ THIẾT KẾ
- Người chơi không cần tài khoản
- Thống kê chỉ gửi khi đồng ý; rút đồng ý thì dữ liệu bị xóa
- Game không chứa bí mật: bằng chứng kiểm trên máy chủ
![Sơ đồ thành phần](docs/thiet-ke/architecture/component.png)
> Ba nguyên tắc: chơi được khi không có mạng, không tài khoản, game không chứa bí mật. Bám nguyên tắc đồng ý và rút lại đồng ý của Luật Bảo vệ dữ liệu cá nhân.

---

# Lớp phía game: sáu nhóm
~ THIẾT KẾ
![Lược đồ lớp phần game](docs/thiet-ke/lop-game/overview.png){full}
> Nhân vật và tương tác, điều phối đêm, kẻ truy đuổi, trạng thái và lưu tiến độ, điện thoại, sự kiện thống kê. Người trình bày chọn một nhóm để nói kỹ.

---

# Lớp phía máy chủ
~ THIẾT KẾ
![Lược đồ lớp phần máy chủ](docs/thiet-ke/lop-may-chu/overview.png){full}
> Thực thể, dịch vụ, controller. 18 đường dẫn API đặc tả bằng OpenAPI; bốn bảng nghiệp vụ cộng bảng tài khoản của ASP.NET Core Identity.

---

# Một máy ảo, ba container
~ TRIỂN KHAI
- Caddy: cấp HTTPS, phục vụ trang quản trị và website
- Máy chủ API và PostgreSQL không mở cổng ra ngoài
- Docker Compose, chạy trên máy ảo Azure
![Sơ đồ triển khai](docs/thiet-ke/architecture/deployment.png)
> CẬP NHẬT trước khi trình bày: tên miền thật và kết quả kiểm sau triển khai (7 bước ở docs/deploy-azure.md).

---

# Một lỗi chỉ lộ khi triển khai
~ TRIỂN KHAI
::: flow Trước khi sửa
- Người chơi A, B, C | mỗi người một IP
- Caddy | chuyển tiếp, API thấy IP của Caddy
- API | giới hạn tần suất theo IP
- ! Chặn cả nhóm | một người vượt là mọi người bị chặn
:::
::: flow Sau khi sửa
- Người chơi A, B, C | mỗi người một IP
- Caddy | gắn IP thật vào tiêu đề
- API | đọc IP thật từ tiêu đề
- + Mỗi người một giới hạn | chỉ người vượt mới bị chặn
:::
> Phát hiện khi chuẩn bị triển khai, không phải khi chạy trên máy nhà. Sửa bằng một biến cấu hình trong Docker Compose, không đổi mã; an toàn vì máy chủ API không mở cổng ra ngoài. Bài học: kiểm sớm trên môi trường gần thật.

---

# Làm việc có kiểm soát
~ QUY TRÌNH
::: stats
- 73 | issue | 45 đã đóng, mỗi việc có người nhận
- 53 | pull request đã gộp | mỗi pull request có người duyệt
- 266 | commit trên nhánh chính | 
- 4 | mốc M1 đến M4 | hạn nội bộ sớm hơn hạn nộp
:::
> Số liệu ngày 26/11; CẬP NHẬT ngay trước khi trình bày (lệnh đếm ở docs/slide/README.md). Quy định nhóm: không dùng model thô, người duyệt phải chạy thử thay đổi.

---

# Kiểm thử ở ba tầng
~ KIỂM THỬ
- Game: bộ kiểm thử trong Unity cho lưu tiến độ, ẩn nấp, phím khi bật bộ gõ tiếng Việt, Đêm 1 đến 3
::: stats
- 66 | phương thức kiểm thử máy chủ | 10 lớp, chạy trên PostgreSQL thật
- 33 | test case | phủ đủ 14 yêu cầu chức năng
- CI | tự chạy mỗi pull request | GitHub Actions cho máy chủ và trang quản trị
:::
> THÊM sau khi Thắng chạy xong: số test case đạt và không đạt, số lỗi tìm được và đã sửa. Chỉ ghi số đã chạy thật.

---

# Kế hoạch khởi nghiệp
~ KINH DOANH
- Đề xuất bán trọn gói trên Steam, phát hành theo chương
::: stats
- 9,9 triệu | chi phí đã biết trong 12 tháng | máy chủ Azure và phí Steam
- 315 | bản để hòa vốn | giá giả định 45.000 đồng, Steam giữ 30%
- 18+ | nhãn tuổi dự kiến | bán cho người chơi ở Việt Nam cần có doanh nghiệp
:::
> Nói rõ đây là giả định, không phải dự báo. Chỉ đi tiếp nếu chơi thử đạt tiêu chí và có ý kiến luật sư. Chương 4 của báo cáo có bảng chi phí, doanh thu theo kịch bản và danh sách điều chưa biết.

---

# Khó khăn và bài học
~ BÀI HỌC
::: cards
- Máy mạnh dồn một nơi | Dựng và kiểm Unity, Docker chạy ở một máy, nên tiến độ phụ thuộc vào máy đó | 1
- Model thô | Đặt quy định không dùng model thô trong cảnh người chơi nhìn thấy | 2
- Lỗi sau proxy | Chỉ lộ khi chuẩn bị triển khai; cần kiểm sớm trên môi trường gần thật | 3
- Khối lượng | Việc giữa các mảng không đều; nhóm chuyển việc ở từng mốc | 4
:::
> Trình bày trung thực, kèm điều nhóm đã làm để xử lý. Thẻ 4 có thể bỏ nếu nhóm không muốn nêu; xem lại trước khi tập.

---

# Cảm ơn
@bg docs/slide/anh/hem-3am.png
- Xin mời thầy cô và các bạn đặt câu hỏi
- Nhóm 3 GIỜ 12: Quang, Phúc, Tiên, Thắng
> Chuẩn bị trả lời: vì sao đổi góc nhìn; vì sao game cần máy chủ; dữ liệu người chơi được bảo vệ thế nào; vì sao chưa phát hành công khai; kế hoạch Đêm 4 đến 7.
