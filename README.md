# 3 GIỜ 12

Game kinh dị 3D góc nhìn thứ ba (Unity), kèm máy chủ thống kê và trang quản trị. Đồ án thực tế Công nghệ phần mềm, UTH.

## Cấu trúc

| Thư mục | Nội dung | Phụ trách chính |
|---|---|---|
| `game/` | Project Unity 6.3 LTS | Phúc (gameplay), Quang (màn chơi, điện thoại), Tiên (3D nhân vật, đồ vật) |
| `server/` | Máy chủ ASP.NET Core + PostgreSQL | Quang |
| `admin/` | Trang quản trị React | Quang, Thắng |
| `arg-site/` | Website câu đố ngoài game | Thắng, Quang |
| `docs/` | Thiết kế, biên bản họp, kế hoạch | Cả nhóm |

File gốc nặng (`.blend`, `.psd`, ảnh AI gốc) để trên Drive, không đưa vào repo.

## Phân công

| Người | Việc |
|---|---|
| Quang (nhóm trưởng) | Kịch bản đêm 1-3, 3D bối cảnh, web chính, kiến trúc hệ thống, lược đồ lớp phần web, quy trình nhóm, kế hoạch khởi nghiệp |
| Phúc | Gameplay Unity, ghép màn chơi, lược đồ lớp phần game |
| Tiên | 3D nhân vật và đồ vật, đưa vào Unity, biên bản họp hằng tuần |
| Thắng | Điện thoại trong game, phụ web, kiểm thử, use case, tổ chức chơi thử |

## Cách làm việc

1. Mỗi việc là một issue, có người nhận và mốc hạn. Theo dõi trên bảng việc (GitHub Projects): Cần làm, Đang làm, Xong.
2. Không sửa thẳng `main`. Tạo nhánh từ issue, làm xong mở pull request, ít nhất một người khác duyệt rồi mới gộp.
3. Họp mỗi tuần một lần: xem bảng việc, ghi biên bản vào `docs/bien-ban/`, giao việc tuần sau.
4. Có vướng mắc thì ghi bình luận vào issue, không chỉ nói qua Zalo, để còn dấu vết.

## Mốc nộp

- 16/11/2026 09:07: thuyết minh thiết kế (kiến trúc, use case, lược đồ lớp).
- 03/12/2026 09:07: dự án hoàn chỉnh (đêm 1-3 chơi được, máy chủ, trang quản trị, báo cáo, thuyết trình).
