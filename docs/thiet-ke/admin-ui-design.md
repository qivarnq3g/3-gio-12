# Thiết kế trang quản trị

Tách từ `luoc-do-lop-may-chu.md`: cấu trúc thành phần và các trang của trang quản trị.

## 1. Cấu trúc thành phần

Trang quản trị là ứng dụng React, TypeScript, Vite. Ứng dụng không có lớp theo nghĩa hướng đối tượng; hình dưới là cây thành phần (component) và cách chúng gọi API.

![Cây thành phần của trang quản trị](admin-ui/component-tree.png)

## 2. Các trang

| Trang | Đường dẫn | API dùng | Yêu cầu |
|---|---|---|---|
| `LoginPage` | `/admin/login` | `POST /auth/login` | 9 |
| `DashboardPage` | `/admin` | `GET /dashboard`, `GET /exports/{dataset}` | 10, 11 |
| `AccountsPage` | `/admin/accounts` | `/accounts` và các API con | 9 |
| `ProfilePage` | `/admin/profile` | `POST /auth/password` | 9 |
| `PuzzlesPage` | `/admin/puzzles` | `GET /puzzles` | 12 |
| `PuzzleDetailPage` | `/admin/puzzles/:puzzleId` | `GET`, `PATCH /puzzles/{puzzleId}`, `/solutions`, `/submissions` | 12 |

: Các trang của trang quản trị

- Kiểu dữ liệu TypeScript sinh bằng `openapi-typescript` từ `docs/api/openapi.yaml`; `apiClient` dùng `openapi-fetch` với các kiểu đó, nên đổi hợp đồng mà quên sửa trang quản trị thì build báo lỗi.
- `apiClient` gặp 401 thì xóa tài khoản trong `AuthProvider`, và `RequireAuth` đưa về trang đăng nhập.
- Nút xuất CSV là liên kết tới `/api/admin/exports/{dataset}?from=...&to=...`; trình duyệt tự gửi cookie và tải tệp, không cần đọc tệp bằng JavaScript.
- `DateRangePicker` chọn ngày theo giờ Việt Nam và gửi mốc nửa đêm `+07:00`.
- Mã khu vực (`areaId`) hiện kèm tên dễ đọc, lấy từ danh sách mã khu vực của thiết kế màn chơi.

Website câu đố là trang tĩnh, không gọi API và không có dữ liệu riêng, nên không có lớp hay bảng riêng.
