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

### Tổ chức phần chơi và trạng thái

Phần game được xây dựng bằng Unity và C#, sử dụng Universal Render Pipeline để hiển thị môi trường 3D [@unity-manual]. Cảnh chơi chính là `NightOneRealistic`, chứa hẻm, nhà xe, nhà trọ và các vị trí tương tác. Các đêm dùng chung cảnh thay vì chuyển sang những bản đồ thử nghiệm tách rời. Cách tổ chức này giúp giữ quan hệ giữa vị trí trong kịch bản, vật thể hiển thị và đường đi của người chơi.

Mã điều khiển, trạng thái và giao diện được tách theo trách nhiệm như bảng 3.2. Khi người chơi thực hiện một hành động, bộ điều phối kiểm tra điều kiện, cập nhật dữ liệu tiến độ rồi đồng bộ lại thế giới và giao diện. Chẳng hạn, mua kệ chưa làm xuất hiện ngay một chiếc kệ hoàn chỉnh: trạng thái mua đồ cho phép bước lắp ráp, còn số bước đã hoàn thành quyết định tầng nào được hiển thị.

| Khối | Thành phần tiêu biểu | Trách nhiệm |
|---|---|---|
| Điều khiển và tương tác | `FirstPersonPlayer`, `WorldInteractable` | Di chuyển, góc nhìn, tư thế, chọn vật thể trong tầm với và chuyển quyền điều khiển cho giao diện |
| Điều phối truyện | `NightOneDirector`, các trạng thái của từng đêm | Điều kiện hành động, đồng hồ, lựa chọn, checkpoint và chuyển giai đoạn |
| Giao diện | `PhonePrototype`, `HudKit`, `PauseMenu` | Điện thoại, mục tiêu, lời nhắc tương tác, lựa chọn và quản lý phiên chơi |
| Nhân vật quan sát | `NightObserver` | Phát hiện, nghi ngờ, truy đuổi và tìm kiếm theo lịch của kịch bản |
| Lưu và dịch vụ trực tuyến | `NightPersistence`, `LocalJsonStore`, `OnlineServices` | Khôi phục tiến độ, cài đặt, hồ sơ, thống kê và bằng chứng |

: Phân chia trách nhiệm của các thành phần game

Tại thời điểm chốt bản nháp ngày 27/11/2026, phần điều khiển, Đêm 1 và dịch vụ trực tuyến đã có trên nhánh chính; phần Đêm 2 và Đêm 3 đang ở các nhánh tích hợp chờ duyệt. Nội dung dưới đây mô tả mã đã hiện thực, không khẳng định bản phát hành đã chơi liên tục đủ ba đêm. Kết quả kiểm thử và bản dựng cuối cùng được cập nhật riêng ở mục 5.

### Điều khiển nhân vật và tương tác với đồ vật

Nhân vật sử dụng `CharacterController` để di chuyển và xử lý va chạm. Unity Input System cung cấp các thao tác đi, nhìn, chạy, cúi và tương tác. Phím 1 mở máy Ngân, phím 2 mở máy của Sinh viên; Esc cất điện thoại hoặc mở menu tạm dừng tùy trạng thái. Bản cấu hình input được sao chép khi chạy, tránh sửa trực tiếp tài nguyên dùng chung của dự án.

Khi cúi, game thay đổi cả chiều cao vùng va chạm và vị trí camera. Trước khi đứng dậy, game kiểm tra khoảng trống phía trên để tránh xuyên trần hoặc vật cản. Tương tác được chọn bằng tia từ camera theo hướng nhìn, có giới hạn khoảng cách; tia phải gặp đối tượng tương tác trước khi gặp tường. Lớp của nhân vật và tay được loại khỏi phép kiểm để không tự chắn tia.

Mở điện thoại hoặc một giao diện truyện sẽ nhường quyền điều khiển, nhưng không nhất thiết dừng thế giới. Ngược lại, menu tạm dừng dừng thời gian mô phỏng và âm thanh. Khi ứng dụng mất focus, game tự tạm dừng; người chơi phải tiếp tục trước khi di chuyển lại. Việc tách hai cơ chế này tránh tình trạng vừa chọn tin nhắn vừa vô tình đi khỏi vị trí đang đứng.

### Điện thoại và giao diện trong game

Điện thoại là nơi người chơi đọc tin, xem nội dung đã lưu và lựa chọn cách trả lời. `PhonePrototype` dùng UI Toolkit, liên kết với bộ điều khiển qua sự kiện đổi điện thoại. Bộ điều phối truyện quyết định lúc nào được mở từng máy và nội dung nào được xem, thay vì để giao diện tự quyết định tiến độ.

Giao diện mục tiêu, phụ đề và lựa chọn của các đêm dùng `HudKit` để thống nhất phông chữ, kích thước, vùng cuộn và cách co giãn theo màn hình. Trạng thái tin đã xem, lựa chọn và tin chờ được lưu cùng tiến độ. Khi máy bị tắt, tin có thể được giữ trong hàng đợi để xuất hiện đúng khi người chơi bật máy lại.

Ở Đêm 3, camera của máy Sinh viên tạo ảnh PNG từ một camera riêng. Game kiểm tra tia ngắm có trúng phần vải ở mặt lưng máy Ngân hay không để phân biệt ảnh bằng chứng với ảnh thường. Mặt trước và thân máy vẫn che khuất tia; phần ốp trong và vùng tương tác vô hình không được coi là vật cản của phép kiểm này. Dữ liệu ảnh được lưu thành tệp riêng, còn bản lưu giữ mã ảnh và trạng thái liên quan. Đây là ảnh dùng trong diễn biến truyện; không đồng nhất với tệp bằng chứng của câu đố ngoài game.

### Ẩn nấp, phát hiện và truy đuổi

`NightObserver` điều khiển các trạng thái bình thường, nghi ngờ, truy đuổi, tìm kiếm và không hoạt động. Một nhân vật chỉ phản ứng khi đang hiện diện theo lịch của kịch bản. Khả năng nhìn thấy phụ thuộc khoảng cách, góc nhìn và tia kiểm tra vật cản; tường hoặc xe có thể che người chơi. Độ chú ý còn chịu tác động của chạy, cúi và vùng sáng tối.

Các nhân vật cần di chuyển sử dụng `NavMeshAgent`. Khi mất dấu, nhân vật tìm quanh vị trí nhìn thấy lần cuối thay vì biết chính xác vị trí hiện tại của người chơi. Tiếng chạy có thể thu hút người gần nhất; tiếng hô truyền sự chú ý cho những nhân vật đủ điều kiện ở gần. Người chơi phải đứng yên trong chỗ nấp, ngoài tầm nhìn liên tục trong khoảng thời gian quy định để cắt dấu; chỉ đi qua vùng tối không được tính như đã ẩn nấp thành công.

Hậu quả bị bắt tuân theo kịch bản Đêm 1. Hai lần đầu đưa người chơi về chỗ ẩn gần nhất và tăng bất lợi; lần thứ ba đặt lại màn mở đầu. Cơ chế này khác với tự lưu tiến độ: checkpoint phục vụ phục hồi theo luật của màn chơi, còn bản tự lưu phục vụ đóng và mở ứng dụng. Đêm 2 có tình huống né cô Liên ở cổng; Đêm 3 không thêm truy đuổi ngoài kịch bản.

### Lưu tiến độ và cài đặt

`NightPersistence` thu thập dữ liệu từ bộ điều phối vào `NightSaveData`: tiến độ truyện, snapshot checkpoint, vị trí và góc nhìn, tư thế, điện thoại đang mở, đồng hồ cùng trạng thái nhân vật quan sát. Trên nhánh tích hợp, dữ liệu Đêm 2 và Đêm 3 được bổ sung vào trạng thái chung để các lựa chọn có thể đi qua bước chuyển đêm.

Game chỉ chụp trạng thái tại thời điểm ổn định. Những đoạn đang chuyển cảnh, tua thời gian hoặc xử lý bị bắt không được lưu ở giữa chừng, vì một coroutine đang chạy không thể được khôi phục chỉ từ tệp dữ liệu. Khi đọc lại, game kiểm tra cấu trúc, giá trị và ánh xạ đối tượng trước khi sửa thế giới. Bản lưu có dữ liệu không hợp lệ phải bị từ chối thay vì áp dụng một phần.

`LocalJsonStore` ghi JSON vào tệp tạm, hoàn tất ghi rồi thay tệp chính, đồng thời giữ một thế hệ dự phòng hợp lệ. Phiên bản định dạng và checksum SHA-256 hỗ trợ phát hiện tệp hỏng; checksum không được dùng như cơ chế chống gian lận. Nếu bản chính lỗi, hệ thống thử bản dự phòng và hiển thị tình trạng cho người chơi. Cài đặt âm lượng, độ nhạy, chất lượng đồ họa và cảnh báo nội dung được lưu riêng với tiến độ, nên chơi mới không đồng nghĩa mất các lựa chọn cài đặt.

### Hiện thực diễn biến ba đêm

Đêm 1 do `NightOneDirector` điều phối: màn lẻn vào nhà xe, đặt điện thoại, quay ra hẻm, đưa xe về và sinh hoạt trong phòng 7. Các hành động thay đổi biến truyện, tin nhắn, sổ tay và checkpoint. Tình huống giữ hay bỏ điện thoại, ngủ hay thức và phản hồi tin nhắn được giữ để sử dụng ở các đêm tiếp theo.

Buổi sáng Đêm 2 được tổ chức thành các bước báo thức, ăn sáng, đi mua đồ, trở về, qua cổng, trả lời số phòng, lắp kệ và thử đèn. Tuyến xe lưu tiến trình, tốc độ, điều kiện dừng đèn đỏ và số công-tơ-mét. Các tương tác bổ sung khi chạy được gắn vào model hoặc điểm đánh dấu đã có trong cảnh. Nhóm đồ Đêm 2 không hiển thị sớm ở Đêm 1; kệ hiện theo từng tầng đã ráp và đèn chỉ hiện ở bước phù hợp. Nếu thiếu model cần thiết, bước chuyển bị chặn trước khi sửa tiến độ; game không tạo khối thô thay thế để đi tiếp.

Buổi tối Đêm 2 bổ sung lô tin nhắn, đi mua cơm, nhận trà sữa, xem máy Ngân và quyết định ngủ hay thức. Đêm 3 tiếp tục với tuyến đổ xăng, liên lạc với cô Hòa, ảnh mặt lưng máy, mã Máy tính, pin yếu và cáp sạc. Các nhánh nộp máy hoặc giữ máy được phân biệt trong trạng thái. Nhánh nộp máy sử dụng quầy tiếp dân thật trong cảnh; tương tác thành công phát sự kiện kết thúc “Kẻ gánh tội”. Nếu giữ máy, kết thúc buổi chơi Đêm 3 vẫn chỉ là kết thúc đêm, không tự biến thành kết thúc “Về nhà”.

### Thống kê ẩn danh và bằng chứng ngoài game

Bộ điều phối phát sự kiện nghiệp vụ; `OnlineServices` phụ trách hồ sơ, lựa chọn đồng ý, hàng đợi và gửi tới máy chủ. Người chơi chưa đồng ý thì không gửi thống kê. Khi rút đồng ý, game xóa hàng đợi và yêu cầu máy chủ xóa dữ liệu của mã cài đặt; lỗi mạng được xử lý theo chính sách thử lại. Trong hiện thực hiện tại, thống kê chỉ được hỗ trợ trên Windows, phù hợp hợp đồng máy chủ.

Quyền gửi bằng chứng ngoài game được ghi vào hồ sơ khi nhận sự kiện kết thúc “Về nhà”, kể cả khi người chơi từ chối thống kê. Game chỉ tính và gửi mã băm SHA-256 của tệp được chọn, không tải nguyên tệp lên máy chủ. Phản hồi chấp nhận lưu cờ mở kết thúc vào hồ sơ; mã điều phối phải đọc cờ đó trước khi cho phép kết thúc tương ứng. Việc mở kết thúc và ghi nhận thống kê là hai trách nhiệm tách biệt.

Phạm vi bản nộp chỉ gồm ba đêm đầu, trong khi đường truyện tới kết thúc “Về nhà” còn thuộc các đêm sau. Vì vậy, các móc sự kiện và điều kiện hồ sơ đã có không đồng nghĩa người chơi đã đi được tới toàn bộ kết thúc theo cốt truyện. Lối trình diễn câu đố ngoài game và kết quả chạy trọn với máy chủ thật cần được cập nhật khi nhóm hoàn tất tích hợp, không được suy ra từ kiểm thử logic của một phương thức.

## 3. Mô hình 3D và đồ vật

<!-- Người viết: Tiên. Gồm: quy trình Blender sang Unity, phong cách hình ảnh, danh sách model và đồ vật đã dựng, ảnh Solid cạnh ảnh thật. Thắng bổ sung phần môi trường (hẻm, nhà trọ, phòng 7, quán cơm, công an phường). Nguồn: game/Assets/Art/_Source/NGUON.md, CONTRIBUTING.md. -->

Phần này đang được soạn.

### Môi trường ba đêm

<!-- Người viết: Thắng. Bản nháp 28/11; Tiên ghép với phần quy trình và danh sách model ở trên. -->

Toàn bộ không gian chơi nằm trong một cảnh tả thực, gồm một con hẻm ở Sài Gòn và đoạn đường lớn đầu hẻm. Ba đêm dùng lại cùng cảnh và chỉ mở thêm khu vực theo diễn biến, để người chơi nhận ra những chỗ quen khi trở lại. Bảng 3.2 liệt kê các khu vực và đêm sử dụng.

| Khu vực | Model chính | Đêm | Ghi chú |
|---|---|---|---|
| Hẻm | `env_alley`, `env_street_details` | 1, 2, 3 | Mặt tiền nhà ống, khám thờ Ông Địa, cột điện có công tơ, cửa xếp, kệ dép, quảng cáo sơn tường |
| Nhà trọ và nhà xe | `env_boarding_house`, `env_garage` | 1, 2, 3 | Sảnh tầng trệt, hành lang ba tầng; nhà xe có tôn, xà gồ, bảng tiêu lệnh chữa cháy |
| Phòng 7 | `env_room7`, nệm trải sàn, kệ nhựa, đèn kẹp | 1, 2, 3 | Phòng của Linh ở tầng 3, cửa là đối tượng riêng để mở, đóng |
| Đường lớn | `env_main_road`, `env_street_fronts`, `env_street_tree` | 1, 2, 3 | Vỉa hè gạch terrazzo, dốc dắt xe bê tông và cao su, cây đường phố |
| Tiệm gia dụng, quán cơm | `env_homeware_shop`, `env_rice_shop` | 2 | Hộp kệ nhựa ở tiệm gia dụng; bếp ga, nồi cơm và chồng đĩa sau quầy quán cơm |
| Cây xăng | `env_gas_station` | 3 | Một điểm trên đường đi của Đêm 3 |
| Công an phường | `env_police_station`, quầy tiếp nhận | 3 | Cổng sắt, tường vàng, bảng nền đỏ; tên phường hư cấu |

: Khu vực trong cảnh và đêm sử dụng

Nhà, phòng và đồ vật đặc trưng của Việt Nam (nhà trọ, nhà xe, phòng 7, cột điện, cửa xếp, dốc vỉa hè, xe bánh mì, kệ nhựa, quán cơm, công an phường) do nhóm tự dựng bằng script Python trong Blender, theo kích thước thật tính bằng mét. Với cột điện, nhóm lấy chiều dài và đường kính theo bảng thông số cột bê tông ly tâm của nhà sản xuất trong nước (cột 8,5 m và 10,5 m). Đồ vật phổ biến dùng model quét của Poly Haven (thùng các-tông, máy lạnh, ống máng, chai nhựa, ghế nhựa, chậu cây, ổ điện, giấy tờ trên quầy công an) theo giấy phép CC0, và một số model trên Sketchfab theo giấy phép CC BY (khám thờ, tượng, cột điện hẻm, ghế đỏ, các xe máy). Model tải về được đưa về kích thước thật, giảm số tam giác, xóa chữ, logo và biển số thật. Nguồn và giấy phép của từng tệp ghi ở `game/Assets/Art/_Source/NGUON.md`.

Bề mặt dùng vật liệu PBR của URP gồm ảnh màu, ảnh pháp tuyến và ảnh mặt nạ (kim loại, che khuất, độ bóng). Ảnh vân tường, nền, tôn chủ yếu lấy từ ảnh quét CC0 của Poly Haven và ambientCG; vết ố, vết nứt, áp phích và biển số nhà là decal dán lên. Biển hiệu cửa hàng do nhóm vẽ bằng script theo bố cục biển thật, với tên tiệm và số điện thoại hư cấu.

Nhóm đặt quy định không để khối thô (hình hộp ghép tạm) trong bản chơi được. Mỗi khối thô của giai đoạn dựng màn phải được thay bằng model tả thực trước khi gộp vào nhánh chính. Hai bài kiểm tự động trong Unity giữ chất lượng cảnh: `ArtPipelineCheck` kiểm thiết lập nhập texture và model, `PropContactRegression` đo đồ vật trong cảnh để phát hiện vật xuyên vào nhau hoặc lơ lửng.

## 4. Hiện thực máy chủ, trang quản trị và website câu đố

<!-- Người viết: Quang và Thắng. Đã viết phần máy chủ; Thắng bổ sung trang quản trị (React) và website câu đố nếu cần chi tiết hơn. -->

Máy chủ cung cấp 18 đường dẫn API, được đặc tả bằng OpenAPI [@openapi] trong hợp đồng giữa game và máy chủ. API chia thành ba nhóm: nhận thống kê ẩn danh và xóa dữ liệu khi người chơi rút đồng ý; kiểm tra bằng chứng câu đố; quản trị (đăng nhập, tài khoản, bảng điều khiển, xuất CSV, quản lý câu đố).

Nhóm áp dụng một số quy tắc thiết kế khi hiện thực:

- Thống kê gửi theo lô tối đa 100 sự kiện; máy chủ bỏ sự kiện trùng theo mã sự kiện và trả riêng các sự kiện sai thay vì loại cả lô.
- Bằng chứng câu đố chỉ gửi mã băm SHA-256, tệp không rời máy người chơi. Số lần gửi sai được giới hạn theo mã cài đặt (mặc định 5 lần trong 60 phút), đã hết lượt thì máy chủ từ chối kể cả khi mã băm đúng.
- Máy chủ không lưu địa chỉ IP hay thông tin định danh của người chơi; địa chỉ IP chỉ dùng trong bộ nhớ để giới hạn tần suất.
- Đăng nhập quản trị dùng cookie phiên có thuộc tính bảo mật; sai mật khẩu 5 lần liên tiếp thì tạm chặn 15 phút.

Phần máy chủ có 10 lớp kiểm thử tích hợp với 66 phương thức kiểm thử chạy trên PostgreSQL thật trong container; trang quản trị có kiểm thử riêng bằng Vitest. Hai bộ này chạy tự động trong GitHub Actions.

### Trang quản trị

<!-- Người viết: Thắng. Bản nháp 28/11. -->

Trang quản trị là ứng dụng một trang viết bằng React và TypeScript, dựng bằng Vite, được proxy ngược phục vụ ở đường dẫn `/admin/`. Trang có năm màn hình: đăng nhập, bảng điều khiển, quản lý câu đố (danh sách và chi tiết từng câu đố), quản lý tài khoản và hồ sơ của người đang đăng nhập. Mọi màn hình trừ đăng nhập nằm sau lớp kiểm tra phiên; khi máy chủ trả mã `401` vì phiên hết hạn hoặc tài khoản bị khóa, trang tự quay về màn hình đăng nhập.

Bảng điều khiển tổng hợp thống kê ẩn danh trong khoảng ngày người quản trị chọn: tổng người chơi, tổng phiên, tổng và trung bình thời gian chơi; số người bắt đầu, hoàn thành, rời bỏ từng đêm (không tính chơi lại); các khu vực người chơi hay bị bắt hoặc thoát game theo từng đêm; các lựa chọn và tỷ lệ các kết thúc. Bảng cho tải hai tệp CSV mã UTF-8 có BOM để mở thẳng bằng Excel: số liệu tổng hợp và toàn bộ sự kiện thô. Màn hình câu đố cho sửa cấu hình, thêm hoặc xóa mã băm đáp án và xem các lần gửi bằng chứng kèm kết quả. Màn hình tài khoản cho thêm tài khoản, đổi tên hiển thị, khóa hoặc mở lại, đặt lại mật khẩu và xóa tài khoản; màn hình hồ sơ cho người đang đăng nhập đổi mật khẩu của mình.

Kiểu dữ liệu TypeScript của trang được sinh tự động từ tệp OpenAPI của hợp đồng mỗi lần chạy, kiểm thử hay build. Vì vậy khi hợp đồng API đổi mà trang chưa sửa theo, bước build báo lỗi ngay thay vì để lỗi lọt ra lúc chạy. Bộ kiểm thử Vitest của trang có 25 trường hợp, kiểm luồng đăng nhập và tạm chặn khi sai nhiều lần, cùng các thao tác chính của từng màn hình (đổi mật khẩu, quản lý tài khoản, bảng điều khiển khi lỗi hoặc chưa có dữ liệu, thêm và xóa mã băm đáp án).

### Website câu đố

<!-- Người viết: Thắng. Bản nháp 28/11. -->

Website câu đố là trang HTML tĩnh, đóng vai blog cá nhân “Góc nhỏ của Bống” của nhân vật Ngân, gồm trang chủ, trang giới thiệu, bốn bài viết từ 28/9 đến 2/11/2026 và trang lưu trữ tệp `/drop/`. Nội dung bài viết khớp kịch bản: lời hẹn ngày 3/12 với Linh, chuyện bé Na, buổi hoàng hôn bên sông Sài Gòn và những ngày đếm ngược. Ảnh minh họa và ảnh bằng chứng đều do nhóm tạo, không dùng ảnh thật của người khác.

Câu đố nối game với website theo bốn bước. Người chơi mở phần trong của két trên điện thoại Ngân trong game để lấy tệp ghi âm và chìa giải mã. Tệp ghi âm nghe như một lần thử micro, nhưng phổ âm của nó chứa chữ `/drop` ở dải 7 đến 14 kHz, chỉ thấy khi mở bằng phần mềm xem phổ như Audacity. Trang `/drop/` có một ảnh hoàng hôn, thực chất là ảnh ghép với tệp ZIP mã hóa AES-256; giải nén bằng chìa trong két sẽ được ảnh bằng chứng dép chỉ đỏ và tờ tìm trẻ. Cuối cùng, người chơi gửi ảnh bằng chứng qua game; game chỉ gửi mã băm SHA-256, máy chủ so với mã băm đã cấu hình và trả về mã mở kết thúc khi khớp.

Tệp ghi âm được đặt trong `StreamingAssets` để Unity giữ nguyên từng byte, vì khi Unity nén âm thanh, chữ ở tần số cao có thể mất. Gói bằng chứng được dựng lại bằng script từ các tệp nguồn cố định, nên mã băm của ảnh bằng chứng không đổi giữa các lần dựng và khớp với cấu hình trên máy chủ.

## 5. Kiểm thử

<!-- Người viết: Thắng. Gồm: bộ test case 33 mục ở docs/test-case.md (bản nháp ở pull request của Quang), kết quả chạy từng test case (ngày, phiên bản, người chạy), tổng hợp lỗi tìm được và đã sửa, kết quả chơi thử kín. Chỉ ghi kết quả đã chạy thật. -->

<!-- Bản nháp 28/11 của Thắng: đã có phương pháp và tổng hợp lỗi. Kết quả từng test case và kết quả chơi thử kín điền sau khi có bản Windows #140. -->

### Phương pháp kiểm thử

Nhóm kiểm thử ở ba mức. Mức thứ nhất là bài kiểm tự động trong Unity: mỗi tính năng chính của game có một bài kiểm chạy bằng Unity ở chế độ không giao diện (`-batchmode`), tự mở cảnh, mô phỏng thao tác của người chơi rồi thoát với mã 0 khi đạt. Các bài kiểm lưu tiến độ chạy thành cặp hai tiến trình, tiến trình đầu ghi và tiến trình sau đọc lại, để chắc dữ liệu còn nguyên sau khi tắt game. Mỗi bài kiểm dùng thư mục dữ liệu riêng để không đè bản lưu thật. Mức thứ hai là kiểm thử tích hợp của máy chủ chạy trên PostgreSQL thật trong container, cùng kiểm thử của trang quản trị bằng Vitest; hai bộ này chạy tự động trong GitHub Actions ở mỗi pull request. Mức thứ ba là kiểm tay trên bản dựng Windows và buổi chơi thử kín với người ngoài nhóm.

Bộ test case gồm 33 mục cho 14 yêu cầu chức năng, ghi ở `docs/test-case.md`. Mỗi yêu cầu có ít nhất một test case; test case nào đã có bài kiểm tự động thì ghi tên bài, phần còn lại kiểm tay theo các bước ghi sẵn. Chỉ ghi “Đạt” cho test case đã chạy thật, kèm ngày, phiên bản và người chạy.

### Kết quả chạy test case

Phần này đang được soạn: điền sau khi có bản Windows Đêm 1-3 của task #140.

### Lỗi tìm được và đã sửa

Lỗi được ghi thành issue theo mẫu báo lỗi (các bước tái hiện, kết quả mong đợi, kết quả thực tế, phiên bản), kèm tọa độ trên bảng F3 của bản chơi thử khi lỗi gắn với một vị trí trong cảnh. Tới ngày 28/11/2026, nhóm ghi nhận 24 lỗi, đã sửa 19 và còn mở 5. Bảng 3.3 chia số lỗi theo nhóm.

| Nhóm lỗi | Số lỗi | Đã sửa | Ví dụ |
|---|---|---|---|
| Hình ảnh và model 3D | 12 | 11 | Xé hình (z-fighting) ở tường và biển số nhà; vật xuyên vào nhau hoặc lơ lửng; decal chiếu đè lên song sắt; bánh xe Cub bị phẳng |
| Lối chơi và kịch bản | 5 | 4 | Rơi khỏi bản đồ ở khe giữa hai tiệm; vật cản vô hình ở nhà xe; tin nhắn của “Mẹ” tự gửi khi người chơi chưa chọn; chỉ dẫn ghi sai loại xe |
| Âm thanh | 1 | 1 | Tiếng ù kéo dài khi nhấn Esc |
| Hiệu năng | 1 | 0 | FPS thấp trong cảnh tả thực |
| Bản dựng trên nền tảng khác | 1 | 0 | Model hiện thành khối hồng trên Linux |
| Bài kiểm tự động | 2 | 2 | Bài đi bộ Đêm 1 phụ thuộc tốc độ khung hình; Esc không thoát tạm dừng trong bài kiểm |
| Thống kê ẩn danh | 1 | 0 | Sự kiện bắt đầu Đêm 1 không tới máy chủ |
| Công cụ báo lỗi | 1 | 1 | Phím F3 không hiện tọa độ trong bản dựng |

: Lỗi tìm được theo nhóm, tới ngày 28/11/2026

Mười một lỗi (phần lớn là lỗi hình ảnh) được ghi trong lượt rà cảnh ngày 21/11 và đã sửa xong trong ngày 23 và 24/11, trước bản chơi thử ngày 24/11. Năm lỗi còn mở gồm:

- Bộ gõ tiếng Việt làm kẹt phím di chuyển: đã có bản sửa và bài kiểm tự động, chờ xác nhận trên bản dựng.
- Khung sắt, dây điện và xe máy ngoài đường lớn: đã sửa trong cảnh, chờ kiểm bằng mắt trên bản dựng.
- FPS thấp trong cảnh tả thực: chờ số đo trên bản Windows #140.
- Model hiện khối hồng trên Linux: bản Linux nằm ngoài cam kết của thuyết minh thiết kế (chỉ Windows là bắt buộc).
- Sự kiện bắt đầu Đêm 1 không tới máy chủ: đã có bản sửa, đã kiểm với máy chủ chạy trong Docker, chờ gộp.

### Kết quả chơi thử kín

Phần này đang được soạn: điền sau buổi chơi thử kín (task #31).

## 6. Triển khai

<!-- Người viết: Quang. Cập nhật sau khi máy ảo Azure được tạo và chạy: tên miền thật, kết quả 7 bước kiểm ở docs/deploy-azure.md. -->

Máy chủ, trang quản trị và website câu đố chạy trên một máy ảo Linux bằng Docker Compose với ba container: proxy ngược Caddy, máy chủ API và PostgreSQL. Chỉ Caddy mở cổng 80 và 443 ra ngoài; Caddy tự xin chứng chỉ HTTPS, phục vụ trang quản trị ở đường dẫn `/admin/` và website câu đố ở đường dẫn gốc, rồi chuyển các yêu cầu `/api/` sang máy chủ API. Nhóm chọn Azure làm nhà cung cấp máy ảo.

Khi chuẩn bị triển khai, nhóm phát hiện một lỗi chỉ xuất hiện sau proxy: mã giới hạn tần suất lấy địa chỉ IP trực tiếp từ kết nối, nên sau Caddy mọi người chơi mang cùng một địa chỉ và một người vượt giới hạn có thể chặn cả nhóm. Nhóm xử lý bằng cách bật đọc địa chỉ IP gốc từ tiêu đề chuyển tiếp của proxy trong cấu hình Compose; cách này an toàn vì máy chủ API không mở cổng ra ngoài.

Phần này đang được soạn: kết quả triển khai thật và kết quả kiểm sau triển khai.
