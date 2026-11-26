# 3 GIỜ 12
- Game kinh dị hành động 3D góc nhìn thứ nhất, kèm máy chủ và trang quản trị thống kê
- Đồ án thực tế công nghệ phần mềm, lớp 012012213905
- Trường Đại học Giao thông vận tải TP.HCM
- Nhóm: Quang, Phúc, Tiên, Thắng
> Mở đầu trong 30 giây: tên sản phẩm, ba thành phần, nhóm. Ghi tên người trình bày từng phần vào ghi chú của slide tương ứng trước khi tập.

---

# Người chơi nhặt được một chiếc điện thoại
- Máy của Ngân, một nữ sinh viên mất tích
- Đọc tin nhắn, xem ảnh, nghe ghi âm để lần theo manh mối
- Ẩn nấp và chạy trốn khi bị phát hiện, không chiến đấu
- Mỗi lựa chọn đổi diễn biến và kết thúc
![Ảnh chụp màn chơi: người chơi cầm điện thoại của Ngân trong phòng 7](docs/slide/anh/man-choi-dien-thoai.png)
> CẦN BỔ SUNG: ảnh chụp thật từ bản chơi thử (Thắng hoặc Phúc chụp, lưu vào docs/slide/anh/). Nói ngắn cốt truyện, không kể kết thúc. Nhắc nội dung dành cho người chơi từ 18 tuổi.

---

# Ba thành phần, một sản phẩm
- Game (Unity): chạy offline trên Windows, không cần tài khoản
- Máy chủ (ASP.NET Core, PostgreSQL): nhận thống kê ẩn danh, kiểm tra bằng chứng câu đố
- Trang quản trị (React): dashboard thống kê, xuất CSV, quản lý câu đố
- Website câu đố: người chơi tìm bằng chứng để mở kết thúc thật
> Lý do có phần hệ thống quanh game: học phần cần use case và lớp có nội dung, game thuần túy khó đáp ứng. Bản nộp gồm toàn bộ hệ thống và Đêm 1 đến Đêm 3; Đêm 4 đến 7 làm tiếp sau học phần.

---

# Chơi theo đêm, lưu tiến độ, cài đặt
- Bản nộp: Đêm 1 đến Đêm 3
- Tự lưu tiến độ; tiếp tục, chơi lại từ điểm lưu, xóa dữ liệu
- Cài đặt âm lượng, độ nhạy chuột, đồ họa, cảnh báo nội dung
![Menu khởi động và quản lý tiến độ](docs/game/screenshots/save-menu.png)
> Ảnh là màn hình menu thật trong game. Nếu có ảnh đẹp hơn thì thay.

---

# 14 yêu cầu chức năng
- Người chơi (8): điều khiển, chơi theo đêm, dùng điện thoại, ẩn nấp, lưu tiến độ, đồng ý gửi thống kê, gửi bằng chứng, cài đặt
- Quản trị viên (4): đăng nhập và tài khoản, dashboard, xuất CSV, quản lý câu đố
- Hệ thống (2): nhận thống kê theo lô khi người chơi đồng ý, kiểm bằng chứng bằng mã băm SHA-256
> Đổi so với bản đăng ký: duy nhất góc nhìn, từ thứ ba sang thứ nhất (27/10), vì cốt truyện cần giấu ngoại hình nhân vật chính và giảm khối lượng dựng hình.

---

# Lược đồ use case
![Lược đồ use case tổng quát](docs/thiet-ke/use-case/tong-quat.png){full}
> Hai tác nhân: người chơi và quản trị viên. Đăng nhập là use case dùng chung của bốn chức năng quản trị.

---

# Kiến trúc hệ thống
- Người chơi không cần tài khoản
- Thống kê chỉ gửi khi người chơi đồng ý; rút đồng ý thì dữ liệu bị xóa
- Game không chứa bí mật: bằng chứng câu đố kiểm trên máy chủ, tệp không rời máy người chơi
![Sơ đồ thành phần](docs/thiet-ke/architecture/component.png)
> Ba nguyên tắc: chơi được khi không có mạng, không tài khoản, game không chứa bí mật. Bám nguyên tắc đồng ý và rút lại đồng ý của Luật Bảo vệ dữ liệu cá nhân.

---

# Thiết kế lớp phía game
![Lược đồ lớp phần game](docs/thiet-ke/lop-game/overview.png){full}
> Sáu nhóm lớp: nhân vật và tương tác, điều phối đêm, kẻ truy đuổi, trạng thái và lưu tiến độ, điện thoại, sự kiện thống kê. Người trình bày chọn một nhóm để nói kỹ.

---

# Thiết kế lớp phía máy chủ
![Lược đồ lớp phần máy chủ](docs/thiet-ke/lop-may-chu/overview.png){full}
> Thực thể, dịch vụ, controller. 18 đường dẫn API đặc tả bằng OpenAPI; bốn bảng nghiệp vụ cộng bảng tài khoản của ASP.NET Core Identity.

---

# Triển khai bằng Docker Compose
- Ba container: Caddy, máy chủ API, PostgreSQL
- Chỉ Caddy mở cổng 80 và 443, tự cấp HTTPS
- Lỗi chỉ lộ ra khi triển khai: sau proxy, mọi người chơi mang cùng một địa chỉ IP nên giới hạn tần suất chặn cả nhóm
- Sửa bằng một biến cấu hình, không đổi mã
![Sơ đồ triển khai](docs/thiet-ke/architecture/deployment.png)
> Máy ảo Linux trên Azure. Nhấn mạnh bài học: kiểm trên môi trường gần thật sớm. CẬP NHẬT trước khi trình bày: tên miền thật và kết quả kiểm sau triển khai.

---

# Quy trình làm việc nhóm
- Mọi việc là một issue: 73 issue, 45 đã đóng (số ngày 26/11)
- Mọi thay đổi qua pull request có người duyệt: 53 pull request đã gộp
- Bốn mốc M1 đến M4, hạn nội bộ sớm hơn hạn nộp
- GitHub Actions chạy kiểm thử máy chủ và trang quản trị ở mỗi pull request
> CẬP NHẬT các số này ngay trước khi trình bày (chạy lệnh đếm trong docs/slide/README.md). Quy định nhóm: không dùng model thô, người duyệt phải chạy thử thay đổi.

---

# Kiểm thử
- Máy chủ: 10 lớp kiểm thử, 66 phương thức, chạy trên PostgreSQL thật
- Game: bộ kiểm thử trong Unity cho lưu tiến độ, ẩn nấp, phím khi bật bộ gõ tiếng Việt, Đêm 1 đến 3
- 33 test case phủ đủ 14 yêu cầu chức năng
> THÊM sau khi Thắng chạy xong: số test case đạt và không đạt, số lỗi tìm được và đã sửa. Chỉ ghi số đã chạy thật.

---

# Chơi thử kín và số liệu thật
- Thống kê ẩn danh, chỉ gửi khi người chơi đồng ý
- Dashboard: số người chơi, hoàn thành từng đêm, nơi bị bắt, kết thúc đạt được
![Ảnh dashboard thống kê có số liệu từ lượt chơi thử](docs/slide/anh/dashboard-so-lieu.png)
> CẦN BỔ SUNG: ảnh dashboard sau lượt chơi thử kín (Thắng). Nói số người chơi thử, đêm nhiều người bỏ nhất, điều nhóm sửa theo đó. Chỉ nói số có thật.

---

# Kế hoạch khởi nghiệp
- Đề xuất bán trọn gói trên Steam, phát hành theo chương
- Chi phí đã biết trong 12 tháng: khoảng 9,9 triệu đồng (máy chủ Azure, phí Steam)
- Hòa vốn khoảng 315 bản ở giá giả định 45.000 đồng
- Rào cản lớn nhất: bán cho người chơi ở Việt Nam cần có doanh nghiệp
- Chỉ đi tiếp nếu chơi thử đạt tiêu chí và có ý kiến luật sư
> Nói rõ đây là giả định, không phải dự báo. Chương 4 của báo cáo có bảng chi phí, doanh thu theo kịch bản và danh sách điều chưa biết.

---

# Khó khăn và bài học
- Dựng và kiểm Unity, Docker tập trung ở một máy mạnh, nên tiến độ phụ thuộc vào máy đó
- Đặt quy định không dùng model thô trong cảnh người chơi thấy
- Lỗi giới hạn tần suất sau proxy chỉ lộ khi chuẩn bị triển khai
- Khối lượng giữa các mảng không đều, nhóm chuyển việc ở từng mốc
> Trình bày trung thực, kèm điều nhóm đã làm để xử lý. Câu cuối có thể bỏ nếu nhóm không muốn nêu; xem lại trước khi tập.

---

# Kết luận
- Đã có: máy chủ, trang quản trị, website câu đố và game Đêm 1 đến 3
- Làm tiếp: Đêm 4 đến 7 và chơi thử rộng hơn
- Cảm ơn thầy cô và các bạn đã theo dõi
- Câu hỏi?
> Chuẩn bị trả lời: vì sao đổi góc nhìn; vì sao game cần máy chủ; dữ liệu người chơi được bảo vệ thế nào; vì sao chưa phát hành công khai; kế hoạch Đêm 4 đến 7.
