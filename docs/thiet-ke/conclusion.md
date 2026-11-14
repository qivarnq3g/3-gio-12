# Kết luận

Chương cuối của thuyết minh: tóm tắt kết quả thiết kế và những phần chưa làm.

## 1. Kết quả thiết kế

Thiết kế đã xác định mười bốn yêu cầu chức năng, hai tác nhân và mười một use case có đặc tả. Hệ thống gồm năm thành phần: game, máy chủ API, cơ sở dữ liệu, trang quản trị và website câu đố; luồng dữ liệu giữa chúng và cách triển khai bằng Docker Compose được mô tả ở `kien-truc-he-thong.md`.

Lược đồ lớp của game bám theo mã nguồn đã có, gồm sáu nhóm lớp: nhân vật và tương tác, điều phối Đêm 1, kẻ truy đuổi, trạng thái và lưu tiến độ, điện thoại, sự kiện thống kê. Lược đồ lớp của máy chủ gồm lớp thực thể, dịch vụ và controller, kèm các quy tắc nghiệp vụ chính. Cơ sở dữ liệu có bốn bảng nghiệp vụ và bảng tài khoản của ASP.NET Core Identity.

Ba nguyên tắc của kiến trúc được giữ ở mọi phần: game chơi được khi không có mạng (trừ bước gửi bằng chứng để mở kết thúc thật), người chơi không cần tài khoản, và game không chứa bí mật vì bằng chứng câu đố được kiểm trên máy chủ.

## 2. Phần chưa làm

| Phần | Đã có | Chưa làm |
|---|---|---|
| Nhân vật và tương tác | Điều khiển góc nhìn thứ nhất, tương tác với đồ vật | Hệ tương tác tổng quát nếu các đêm sau cần |
| Kẻ truy đuổi | Người quan sát của Đêm 1, tìm đường bằng NavMesh | Hệ truy đuổi dùng chung cho các đêm sau |
| Lưu tiến độ | Lưu và nạp tiến độ Đêm 1, cài đặt | Lưu cho Đêm 2, Đêm 3; chuyển đổi tệp lưu giữa các phiên bản |
| Điện thoại | Giao diện điện thoại tạm trong Đêm 1, bản thử giao diện | Ghép điện thoại hoàn chỉnh vào màn chơi |
| Gửi thống kê | Sự kiện thống kê phát ra trong game | Xin đồng ý, hàng đợi trên đĩa, gửi theo lô, gửi lại |
| Gửi bằng chứng | Chưa có | Màn hình chọn tệp, tính SHA-256, gửi lên máy chủ, lưu mã mở khóa; lối mở thẳng câu đố cho bản trình diễn |
| Máy chủ API | Khung dự án, kết nối PostgreSQL, đường kiểm tra `/health` | API nhận thống kê, API kiểm bằng chứng, đăng nhập và tài khoản quản trị, dashboard và xuất CSV đã viết, đang chờ duyệt để gộp |
| Trang quản trị | Chưa có trên nhánh chính | Đăng nhập, tài khoản, dashboard đã viết, đang chờ duyệt; trang quản lý câu đố chưa làm |
| Website câu đố | Chưa có trên nhánh chính | Bản đầu đã viết, đang sửa theo góp ý: chỉ trang `/drop` được chứa gói bằng chứng, nén bằng AES-256, chìa mới không đưa vào repo, gói đủ nội dung theo kịch bản |
| Triển khai | Tệp Docker Compose | Chọn nhà cung cấp máy ảo và kênh phát bản build |

: Những phần đã có và chưa làm

## 3. Hướng tiếp theo

Từ nay tới bản nộp cuối kỳ ngày 03/12/2026, nhóm hoàn thiện ba đêm đầu của game theo các lược đồ trong tài liệu, ghép điện thoại hoàn chỉnh, làm phần gửi thống kê và gửi bằng chứng của game, gộp phần máy chủ và trang quản trị đang chờ duyệt, làm trang quản lý câu đố, rồi triển khai máy chủ, trang quản trị, website câu đố lên một máy ảo. Khi mã nguồn đổi, lược đồ lớp được cập nhật theo.
