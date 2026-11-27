# Hiện thực và kiểm thử

Ghi chú cho nhóm (script dựng bỏ đoạn này): mỗi mục có người phụ trách ghi trong chú thích HTML ngay dưới tiêu đề (script không đưa chú thích vào bản Word). Mục nào còn dòng "Phần này đang được soạn." là chưa có người viết; script báo số mục như vậy khi dựng. Viết hết mục của mình trước 30/11 để Quang ghép và Tiên rà.

## 1. Môi trường và công cụ

<!-- Người viết: Quang. Đã viết; cập nhật nếu công cụ đổi. -->

Bảng 3.1 liệt kê công cụ nhóm dùng cho từng thành phần.

| Thành phần | Công cụ |
|---|---|
| Game | Unity 6.3 LTS (6000.3.26f1) với URP, ngôn ngữ C#; dựng model 3D bằng Blender |
| Máy chủ | ASP.NET Core trên .NET 10, Entity Framework Core, PostgreSQL 17 |
| Trang quản trị | React và TypeScript, dựng bằng Vite |
| Website câu đố | Trang HTML tĩnh |
| Đóng gói và triển khai | Docker Compose, Caddy làm proxy ngược và cấp chứng chỉ HTTPS |
| Quản lý mã và việc | Git và GitHub: issue, pull request, GitHub Actions |

: Công cụ theo từng thành phần

Nhóm làm việc theo quy trình sau. Mỗi việc là một issue, có người phụ trách và mốc thời gian (M1 đến M4). Mọi thay đổi mã vào nhánh chính phải qua pull request và có người khác trong nhóm duyệt. Với phần máy chủ và trang quản trị, GitHub Actions chạy kiểm thử tự động ở mỗi pull request và mỗi lần gộp vào nhánh chính. Quy tắc làm việc chi tiết nằm ở phụ lục.

## 2. Hiện thực game

<!-- Người viết: Phúc. Gồm: điều khiển nhân vật; điện thoại trong game; ẩn nấp và truy đuổi; lưu tiến độ và cài đặt; Đêm 1, 2, 3; gửi thống kê và bằng chứng. Gợi ý nguồn: docs/game/*.md, docs/kich-ban/dem-1.md, dem-2.md, dem-3.md. -->

Phần này đang được soạn.

## 3. Mô hình 3D và đồ vật

<!-- Người viết: Tiên. Gồm: quy trình Blender sang Unity, phong cách hình ảnh, danh sách model và đồ vật đã dựng, ảnh Solid cạnh ảnh thật. Thắng bổ sung phần môi trường (hẻm, nhà trọ, phòng 7, quán cơm, công an phường). Nguồn: game/Assets/Art/_Source/NGUON.md, CONTRIBUTING.md. -->

Phần này đang được soạn.

## 4. Hiện thực máy chủ, trang quản trị và website câu đố

<!-- Người viết: Quang và Thắng. Đã viết phần máy chủ; Thắng bổ sung trang quản trị (React) và website câu đố nếu cần chi tiết hơn. -->

Máy chủ cung cấp 18 đường dẫn API, được đặc tả bằng OpenAPI [@openapi] trong hợp đồng giữa game và máy chủ. API chia thành ba nhóm: nhận thống kê ẩn danh và xóa dữ liệu khi người chơi rút đồng ý; kiểm tra bằng chứng câu đố; quản trị (đăng nhập, tài khoản, bảng điều khiển, xuất CSV, quản lý câu đố).

Nhóm áp dụng một số quy tắc thiết kế khi hiện thực:

- Thống kê gửi theo lô tối đa 100 sự kiện; máy chủ bỏ sự kiện trùng theo mã sự kiện và trả riêng các sự kiện sai thay vì loại cả lô.
- Bằng chứng câu đố chỉ gửi mã băm SHA-256, tệp không rời máy người chơi. Số lần gửi sai được giới hạn theo mã cài đặt (mặc định 5 lần trong 60 phút), đã hết lượt thì máy chủ từ chối kể cả khi mã băm đúng.
- Máy chủ không lưu địa chỉ IP hay thông tin định danh của người chơi; địa chỉ IP chỉ dùng trong bộ nhớ để giới hạn tần suất.
- Đăng nhập quản trị dùng cookie phiên có thuộc tính bảo mật; sai mật khẩu 5 lần liên tiếp thì tạm chặn 15 phút.

Phần máy chủ có 10 lớp kiểm thử tích hợp với 66 phương thức kiểm thử chạy trên PostgreSQL thật trong container; trang quản trị có kiểm thử riêng bằng Vitest. Hai bộ này chạy tự động trong GitHub Actions.

## 5. Kiểm thử

<!-- Người viết: Thắng. Gồm: bộ test case 33 mục ở docs/test-case.md (bản nháp ở pull request của Quang), kết quả chạy từng test case (ngày, phiên bản, người chạy), tổng hợp lỗi tìm được và đã sửa, kết quả chơi thử kín. Chỉ ghi kết quả đã chạy thật. -->

Phần này đang được soạn.

## 6. Triển khai

<!-- Người viết: Quang. Cập nhật sau khi máy ảo Azure được tạo và chạy: tên miền thật, kết quả 7 bước kiểm ở docs/deploy-azure.md. -->

Máy chủ, trang quản trị và website câu đố chạy trên một máy ảo Linux bằng Docker Compose với ba container: proxy ngược Caddy, máy chủ API và PostgreSQL. Chỉ Caddy mở cổng 80 và 443 ra ngoài; Caddy tự xin chứng chỉ HTTPS, phục vụ trang quản trị ở đường dẫn `/admin/` và website câu đố ở đường dẫn gốc, rồi chuyển các yêu cầu `/api/` sang máy chủ API. Nhóm chọn Azure làm nhà cung cấp máy ảo.

Khi chuẩn bị triển khai, nhóm phát hiện một lỗi chỉ xuất hiện sau proxy: mã giới hạn tần suất lấy địa chỉ IP trực tiếp từ kết nối, nên sau Caddy mọi người chơi mang cùng một địa chỉ và một người vượt giới hạn có thể chặn cả nhóm. Nhóm xử lý bằng cách bật đọc địa chỉ IP gốc từ tiêu đề chuyển tiếp của proxy trong cấu hình Compose; cách này an toàn vì máy chủ API không mở cổng ra ngoài.

Phần này đang được soạn: kết quả triển khai thật và kết quả kiểm sau triển khai.
