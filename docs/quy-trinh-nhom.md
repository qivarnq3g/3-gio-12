# Quy trình hoạt động nhóm

Nhóm "3 GIỜ 12", đồ án thực tế Công nghệ phần mềm, Trường Đại học Giao thông vận tải Thành phố Hồ Chí Minh (UTH).

Tài liệu gồm sáu phần theo rubric chấm quy trình nhóm. Quy ước kỹ thuật (đặt tên nhánh, commit, pull request) nằm ở [CONTRIBUTING.md](../CONTRIBUTING.md); tài liệu này không chép lại mà chỉ nói ai làm gì, theo dõi thế nào và xử lý khi có vướng mắc. Mọi điều ở đây có hiệu lực khi cả nhóm đã đồng ý trong một buổi họp và biên bản buổi đó ghi lại.

## 1. Phân công công việc

### 1.1. Nguyên tắc

- Mỗi người phụ trách chính một mảng, theo thế mạnh, để việc trong mảng có một người chịu trách nhiệm đến cùng.
- Mỗi mảng có một người hỗ trợ. Người hỗ trợ duyệt pull request của mảng đó và nhận việc thay khi người phụ trách vắng hoặc quá tải.
- Mọi việc là một issue trên GitHub, có người nhận, mốc hạn và tiêu chí hoàn thành. Việc không có issue thì không tính là việc của nhóm.
- Việc chung (báo cáo, slide, thuyết trình) chia theo phần, mỗi phần ghi tên người viết.

### 1.2. Bảng phân công

| Người | Mảng phụ trách chính | Hỗ trợ mảng |
|---|---|---|
| Quang (nhóm trưởng) | Kịch bản đêm 1-3, bối cảnh 3D, máy chủ và web, kiến trúc hệ thống, quy trình nhóm, kế hoạch khởi nghiệp | Gameplay, kiểm thử |
| Phúc | Gameplay Unity, ghép màn chơi, lược đồ lớp phần game | Kịch bản (đọc để dựng màn), điện thoại trong game |
| Tiên | Model nhân vật và đồ vật, đưa model vào Unity, biên bản họp | Bối cảnh 3D |
| Thắng | Điện thoại trong game, trang quản trị, website câu đố, use case, kiểm thử, tổ chức chơi thử | Máy chủ và web |

Chi tiết từng việc nằm trong 35 issue trên bảng việc, chia theo bốn mốc ở mục 2.2.

### 1.3. Cân bằng khối lượng

Số issue mỗi người nhận khác nhau vì độ lớn mỗi issue khác nhau. Để công bằng, nhóm cân bằng theo công sức, không theo số issue:

- Khi tạo issue, người tạo ghi ước lượng số buổi làm (một buổi khoảng 3 giờ) vào phần mô tả.
- Cuối mỗi mốc, nhóm trưởng cộng số buổi ước lượng của mỗi người và đưa ra buổi họp. Nếu một người gánh hơn người ít nhất từ 1,5 lần trở lên, nhóm chuyển bớt việc ở mốc sau.
- Ai xong sớm thì tự nhận issue chưa có người ở cột Cần làm, hoặc hỏi người đang quá tải để nhận bớt.

## 2. Quản lý và kiểm soát

### 2.1. Công cụ

| Công cụ | Dùng để |
|---|---|
| GitHub Issues và bảng việc (GitHub Projects) | Giao việc, theo dõi trạng thái: Cần làm, Đang làm, Đang duyệt, Xong |
| Pull request | Đưa thay đổi vào `main`, mỗi pull request có ít nhất một người khác duyệt |
| Mốc (milestone) trên GitHub | Gom issue theo hạn nộp |
| `docs/bien-ban/` | Biên bản họp tuần |
| Drive của nhóm | File gốc nặng (`.blend`, `.psd`, ảnh AI gốc) |
| Nhóm Zalo | Trao đổi nhanh; quyết định phải ghi lại vào issue hoặc biên bản (mục 4.3) |

### 2.2. Mốc và hạn nội bộ

Hạn nội bộ sớm hơn hạn nộp trên Moodle ít nhất một ngày để còn thời gian sửa.

| Mốc | Hạn nội bộ | Hạn nộp chính thức | Kết quả phải có |
|---|---|---|---|
| M1 - Nền tảng | 01/11/2026 | Không có bài nộp | Repo, môi trường, kịch bản đêm 1 cho 3D, hợp đồng API, quy trình nhóm |
| M2 - Thuyết minh thiết kế | 15/11/2026 | 16/11/2026 09:07 | Kiến trúc hệ thống, lược đồ use case, lược đồ lớp; đêm 1 bản thô chơi được |
| M3 - Bản chơi được đêm 1-3 | 29/11/2026 | Không có bài nộp | Đêm 1-3 chơi được, máy chủ và trang quản trị chạy, chơi thử kín lần 1 |
| M4 - Nộp cuối kỳ | 02/12/2026 | 03/12/2026 09:07 | Bản đóng gói, máy chủ đã triển khai, báo cáo, slide |

### 2.3. Họp tuần

- Mỗi tuần một buổi, gặp trực tiếp hoặc Google Meet, khoảng 45 phút. Lịch cố định chốt ở buổi họp đầu tiên.
- Thứ tự: mỗi người nói đã làm gì, sẽ làm gì, đang vướng gì (mỗi người tối đa 5 phút); xem bảng việc theo mốc; chốt các quyết định đang treo; giao việc tuần sau.
- Tiên ghi biên bản theo mẫu `docs/bien-ban/_mau.md`, mở pull request trong ngày họp. Tiên vắng thì người họp tiếp theo trong danh sách Phúc, Thắng, Quang ghi thay.

### 2.4. Kiểm soát tiến độ và chất lượng

- Trạng thái issue phải đúng với thực tế: nhận việc thì kéo sang Đang làm, mở pull request thì kéo sang Đang duyệt.
- Issue Đang làm quá 3 ngày không có commit hay bình luận thì nhóm trưởng hỏi trong issue.
- Người duyệt pull request chạy thử thay đổi, không chỉ đọc mã. Pull request được duyệt trong vòng 48 giờ kể từ khi gắn người duyệt.
- `main` luôn chạy được. Pull request làm hỏng bản build thì người mở sửa ngay hoặc hoàn tác.
- Hai ngày trước mỗi hạn nộp chính thức, cả nhóm cùng rà bài nộp theo tiêu chí trên Moodle.

## 3. Giải quyết vấn đề và ra quyết định

### 3.1. Ai quyết điều gì

| Loại quyết định | Người quyết | Cần ai đồng ý |
|---|---|---|
| Cách làm bên trong một issue | Người nhận issue | Không |
| Thay đổi ảnh hưởng mảng của người khác (giao diện giữa các phần, hợp đồng API, tên biến dùng chung) | Người đề xuất | Người phụ trách mảng bị ảnh hưởng, qua duyệt pull request |
| Cốt truyện, nhân vật, kết thúc | Quang (phụ trách kịch bản) | Cả nhóm xem; ai thấy vô lý hay khó dựng thì nêu trong issue |
| Phạm vi đồ án, công nghệ chính, phân công lại | Cả nhóm trong buổi họp | Đa số |
| Nội dung nộp lên Moodle | Cả nhóm | Mọi người đã đọc bản cuối |

### 3.2. Cách ra quyết định khi chưa thống nhất

1. Người nêu vấn đề mở issue (hoặc bình luận vào issue sẵn có), ghi rõ vấn đề, các phương án và phương án mình đề xuất.
2. Mọi người góp ý trong issue trong vòng 24 giờ. Im lặng sau 24 giờ được hiểu là không phản đối.
3. Còn bất đồng thì đưa ra buổi họp gần nhất; nếu gấp thì gọi họp ngắn qua Meet.
4. Họp vẫn chưa thống nhất thì biểu quyết, đa số thắng. Hòa phiếu thì nhóm trưởng quyết.
5. Quyết định được ghi vào issue và biên bản, kèm lý do.

Vấn đề về yêu cầu đồ án hay cách chấm thì không biểu quyết: nhóm trưởng hỏi giảng viên và ghi câu trả lời vào issue.

### 3.3. Xử lý khi bị vướng

- Bị vướng quá nửa buổi làm thì bình luận vào issue: đang vướng gì, đã thử gì. Không chờ tới buổi họp.
- Người hỗ trợ mảng (mục 1.2) trả lời trong vòng 24 giờ.
- Vướng có nguy cơ trễ mốc thì báo nhóm trưởng ngay để chuyển việc hoặc cắt phạm vi.

## 4. Thu thập và xử lý thông tin

### 4.1. Nguồn thông tin

| Nguồn | Người theo dõi | Cách xử lý |
|---|---|---|
| Moodle học phần (hạn nộp, yêu cầu, thông báo) | Quang, kiểm mỗi tuần trước buổi họp | Hạn hay yêu cầu thay đổi thì sửa mốc trên GitHub và báo cả nhóm |
| Đề cương và rubric chấm | Quang | Tóm phần liên quan vào issue của bài nộp |
| Tài liệu kỹ thuật (Unity, ASP.NET Core, PostgreSQL, React) | Người làm mảng đó | Ghi đường dẫn tài liệu đã dùng vào pull request |
| Asset lấy từ bên ngoài (model, âm thanh, phông chữ) | Người đưa asset vào | Ghi nguồn và giấy phép vào `game/Assets/Art/_Source/NGUON.md` |
| Góp ý từ chơi thử và từ người ngoài đọc kịch bản | Thắng | Mỗi góp ý cần sửa thành một issue nhãn `loại: lỗi` hoặc `loại: tính năng` |

### 4.2. Kiểm chứng

Chi tiết dùng trong game hay báo cáo mà dựa vào thông tin bên ngoài (y khoa, pháp lý, thông số kỹ thuật) phải có nguồn. Chưa có nguồn thì ghi rõ "cần kiểm" trong issue, không đưa vào bản nộp như sự thật.

### 4.3. Lưu trữ

- Thông tin dùng lâu dài nằm trong repo (`docs/`) hoặc issue, không chỉ nằm trong tin nhắn Zalo.
- Trao đổi trên Zalo mà dẫn tới quyết định thì người khởi xướng chép tóm tắt vào issue liên quan trong ngày.
- Thông tin cá nhân (số CCCD, số điện thoại, MSSV) không đưa lên repo.

## 5. Phối hợp, tăng cường tham gia và cam kết

### 5.1. Phối hợp giữa các mảng

- Phần nào là đầu vào của phần khác thì làm trước và chốt bằng văn bản: kịch bản đêm 1 trước khi Phúc dựng màn, hợp đồng API trước khi làm phần gửi dữ liệu và trang quản trị, danh sách model trước khi Tiên dựng.
- Người làm phần đầu vào gắn người dùng phần đó làm người duyệt pull request, để người dùng xác nhận là đủ để làm tiếp.
- Hai mảng phụ thuộc nhau mà cùng làm một lúc thì làm việc với bản giả: ví dụ game gửi dữ liệu tới máy chủ giả dựng từ hợp đồng API trong khi máy chủ thật chưa xong.

### 5.2. Tăng cường tham gia

- Mỗi buổi họp, mỗi người đều báo cáo; ai chưa nói thì người chủ trì hỏi.
- Người duyệt pull request được xoay vòng để ai cũng đọc được phần của người khác, không chỉ nhóm trưởng duyệt.
- Mỗi người chơi thử đêm mới nhất ít nhất một lần mỗi mốc và góp ý bằng issue.
- Phần thuyết trình cuối kỳ chia đều, mỗi người trình bày phần mình làm.

### 5.3. Cam kết

Mỗi thành viên cam kết:

- Trả lời tin nhắn nhóm trong vòng 24 giờ, kể cả chỉ để báo "đã đọc, tối mai làm".
- Báo trước ít nhất một ngày nếu không kịp hạn một issue, kèm hạn mới đề xuất.
- Đi họp tuần; vắng thì báo trước và gửi phần báo cáo của mình vào nhóm trước giờ họp.
- Commit và mở pull request bằng tài khoản GitHub của chính mình, để lịch sử repo phản ánh đúng công sức.

### 5.4. Ghi nhận đóng góp

Đóng góp của mỗi người được ghi nhận từ dữ liệu có sẵn: issue đã đóng, pull request đã gộp, pull request đã duyệt, biên bản họp. Cuối mỗi mốc, nhóm trưởng tổng hợp vào biên bản buổi họp cuối mốc; trước khi nộp cuối kỳ, bảng đóng góp được đưa vào báo cáo và cả nhóm xác nhận.

## 6. Đàm phán và giải quyết xung đột

### 6.1. Các loại xung đột thường gặp và cách xử lý

| Tình huống | Cách xử lý |
|---|---|
| Hai người sửa cùng một file, Git báo xung đột khi gộp | Người gộp sau giải quyết, hỏi người kia nếu không rõ đoạn nào giữ. File Unity khó gộp (scene, prefab) thì báo trong issue trước khi sửa để chỉ một người sửa một lúc |
| Bất đồng về cách làm hay hướng thiết kế | Theo mục 3.2: nêu phương án trong issue, góp ý 24 giờ, họp, biểu quyết |
| Hai việc tranh thời gian của một người | Nhóm trưởng sắp thứ tự theo mốc gần nhất và việc đang chặn người khác |
| Một người trễ hạn nhiều lần | Mục 6.3 |
| Căng thẳng cá nhân | Mục 6.2 |

### 6.2. Nguyên tắc khi bất đồng

- Tranh luận về việc, không về người. Góp ý trên pull request nói rõ chỗ nào chưa được và vì sao, đề xuất cách sửa nếu có.
- Bất đồng nói thẳng trong issue hoặc trong họp, không nói sau lưng.
- Trước khi phản đối một phương án, nêu được phương án thay thế.
- Bất đồng giữa hai người mà không tự giải quyết được thì một người thứ ba trong nhóm (không phải người trong cuộc) nghe cả hai bên và đề xuất cách giải quyết. Nhóm trưởng là người trong cuộc thì thành viên còn lại chủ trì.
- Không giải quyết được trong nhóm thì nhóm trưởng báo giảng viên.

### 6.3. Khi một thành viên không theo kịp

1. Lần đầu trễ hạn mà không báo trước: người hỗ trợ mảng hỏi trong issue để hiểu lý do và cùng chốt hạn mới.
2. Lần thứ hai: đưa ra buổi họp, nhóm cùng xem việc có quá sức không, có cần chia nhỏ hay chuyển bớt việc không.
3. Tiếp tục không tham gia sau khi đã điều chỉnh: ghi vào biên bản, chuyển việc cho người khác và phản ánh đúng mức đóng góp trong bảng ở mục 5.4. Nhóm trưởng báo giảng viên nếu ảnh hưởng tới bài nộp.

Mục tiêu của các bước trên là giữ tiến độ và ghi nhận đúng công sức, không phải phạt.

### 6.4. Đàm phán phạm vi khi thiếu thời gian

Nếu đến giữa một mốc mà nhóm thấy không kịp, nhóm thương lượng cắt phạm vi thay vì làm ẩu:

1. Mỗi người liệt kê việc còn lại của mình và ước lượng thời gian.
2. Cả nhóm xếp việc theo thứ tự: yêu cầu bắt buộc của bài nộp, rồi yêu cầu chức năng đã đăng ký, rồi phần làm game hay hơn.
3. Việc cuối danh sách được chuyển sang mốc sau hoặc bỏ, ghi rõ trong issue và biên bản.
4. Việc bỏ mà nằm trong 14 yêu cầu chức năng đã đăng ký thì ghi vào phần hạn chế của báo cáo.

## Thông qua

Tài liệu được cả nhóm đọc và đồng ý tại buổi họp ngày ......../......../2026, ghi trong biên bản cùng ngày. Thay đổi về sau đi qua pull request như mọi tài liệu khác và được báo trong buổi họp kế tiếp.
