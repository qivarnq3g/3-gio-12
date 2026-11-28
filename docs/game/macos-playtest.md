# Bản chơi thử macOS (#127)

Dựng Đêm 1 từ `NightOneRealisticSetup.ScenePath`, Unity `6000.3.26f1`, Mono và Universal (Intel x86_64 + Apple Silicon arm64). Hàm mới: `PlaytestBuild.NightOneRealMac`. Module `Mac Build Support (Mono)` phải được cài trong Unity Hub.

## Dựng và đóng gói

Trước khi dựng bản phát hành, cập nhật `main` đã gộp PR này và chạy `git lfs pull`. Bản dựng thử từ nhánh PR chỉ để kiểm, không được gắn vào release có commit khác.

```sh
/path/to/Unity -batchmode -nographics -projectPath game \
  -executeMethod PlaytestBuild.NightOneRealMac -logFile build-mac.log
./tools/package_macos_playtest.sh
```

Hàm đặt kiến trúc bằng `UnityEditor.OSXStandalone.UserBuildSettings.architecture`, giá trị `x64ARM64` của enum API hiện tại, rồi trả cấu hình cũ sau khi dựng. API trong assembly module Mac được tìm bằng reflection sau bước kiểm module để project vẫn compile trên máy chỉ cài Windows/Linux Build Support. Không đổi backend của dự án; nếu backend không phải Mono thì báo lỗi.

Đầu ra là `BuildMac/3gio12.app`. Script đóng gói **chạy trên Mac**, đọc tên executable từ `Info.plist`, kiểm cờ thực thi và hai kiến trúc bằng `lipo`, sau đó dùng `ditto --keepParent`. Script không ghi đè zip có sẵn. Có thể truyền đường dẫn app và zip làm hai đối số.

Nếu dựng trên Windows, chép app sang Mac và đặt cờ chạy trước khi đóng gói:

```sh
chmod +x BuildMac/3gio12.app/Contents/MacOS/*
./tools/package_macos_playtest.sh
```

## Kiểm trước khi phát hành

Giải nén zip vào thư mục khác bằng `ditto -x -k`, kiểm executable vẫn có cờ chạy và `lipo -archs` còn x86_64/arm64. Mở app trên Mac thật; chụp menu và cảnh Đêm 1, chơi hết đoạn đầu, thử chất lượng Thấp, ghi chip/macOS và FPS ước lượng. Binary Universal không thay cho việc thử thực tế trên cả hai loại chip.

Bản chưa notarize có thể bị macOS chặn lần mở đầu. Hướng dẫn người chơi vào System Settings → Privacy & Security → Open Anyway sau khi mở app từ release tin cậy.

Chỉ đính zip lên release sau khi mã được gộp và release dựng từ cùng commit; cập nhật hướng dẫn macOS và báo đường dẫn release/ảnh/máy kiểm trong #127. Không đánh dấu #127 xong khi mới build hoặc kiểm hai kiến trúc.

## Kết quả dựng thử 26/11/2026

- Nguồn: `ca2d934`, dựa trên `main` `c549f79`; Unity 6000.3.26f1. Hai lần dựng thành công; lần cuối trả `PLAYTEST_PREVIEW_RESULT StandaloneOSX Succeeded 0` và exit 0.
- Máy kiểm: Apple M2, RAM 8 GB, macOS 15.7.4. App dùng Metal. Đây chưa phải kiểm trên Intel.
- Zip `3gio12-macos-dem-1-2026-11-26.zip`: 350670498 byte; SHA-256 `7633c68120a82a7a74db6dff8540e6850f89f3caa5811a8214d554b8d6bd5f80`.
- Giải nén bằng ditto sang thư mục riêng: executable `Contents/MacOS/game` giữ quyền chạy và có `x86_64 arm64`.
- Đã mở app giải nén qua LaunchServices, thấy “Chơi mới Đêm 1”, vào hẻm tả thực, HUD tiếng Việt và tạo `night-one.json` trong thư mục dữ liệu kiểm thử riêng. Không sửa bản lưu cá nhân.
- Lần mở trước có menu “Tiếp tục” khi chưa chọn chơi mới; lần mở lại hiển thị đúng. Chưa kết luận nguyên nhân; cần kiểm lại khởi động/focus trước phát hành. Log có cảnh báo NavMesh và PanelSettings, không có managed exception trong lần kiểm này.
- **Chưa đạt đầy đủ tiêu chí #127:** chưa chơi hết đoạn đầu, chưa xác nhận chất lượng Thấp bằng giao diện, chưa đo FPS, chưa thử Intel và chưa upload release. Cấu hình kiểm thử đã nạp quality 0 nhưng không thay cho kiểm giao diện/chơi thực tế.

Giữ PR nháp. Bản zip này chỉ dùng nội bộ để kiểm, không phải release đã được duyệt.

## Kiểm tiếp sau sửa menu

Bản nguồn `dd481ed` đã dựng lại thành công (exit 0, `PLAYTEST_PREVIEW_RESULT StandaloneOSX Succeeded 0`). Zip nội bộ mới: `3gio12-macos-dem-1-2026-11-26-menu-fix.zip`, SHA-256 `bbd303f7878e61d84f7e130a4eb5d7575ff853044ee1febee28d24ba5a5bc935`.

Sửa menu mở trước khi coroutine phiên lưu sẵn sàng: làm mới danh sách khi thay phiên, trạng thái bắt đầu hoặc có/không có bản lưu. `PauseMenuSessionRegression.Run` kiểm trên cảnh tả thực, mô phỏng menu mở sớm, gắn phiên, xuất hiện bản lưu và chuyển sang đang chơi; marker `PAUSE_MENU_SESSION_REGRESSION_PASSED`, exit 0. Đây là kiểm logic, không thay cho ảnh native.

App giải nén mới trên M2 đã hiện đúng chơi mới, vào hẻm, đổi **Cao → Thấp qua giao diện**, đi tới cửa nhà xe và chạy nhánh bị phát hiện/quay về chỗ ẩn. Các vùng đã quan sát không có màu hồng tím/thiếu vật liệu. Không có managed exception hoặc sập trong lượt này; cảnh báo NavMesh/PanelSettings vẫn tồn tại.

- [Menu native](qa/macos-2026-11-26/menu.png)
- [Chất lượng Thấp trong UI](qa/macos-2026-11-26/quality-low.png)
- [Đi trong hẻm ở mức Thấp](qa/macos-2026-11-26/walk-low.png)
- [Đến cửa nhà xe](qa/macos-2026-11-26/garage-approach.png)

Đo bằng `-playtestStats`: chỉ lấy cửa sổ 10 giây khi app có focus và không tạm dừng, tự bỏ mẫu khi đổi độ phân giải/chất lượng. Chế độ này chỉ ghi log; không tắt NPC, di chuyển người chơi hay đổi chất lượng. Kết quả 1280×720, Metal, PC Low: 17 cửa sổ, trung vị 87.6 FPS, khoảng 67.2–182.6 FPS. [Số liệu từng cửa sổ](qa/macos-2026-11-26/fps.csv). Mẫu gồm đi lại, đứng tại chỗ và chuỗi bị phát hiện, không phải benchmark tuyến cố định và không chứng minh hiệu năng toàn game.

**Vẫn chưa đạt:** hoàn tất đặt máy rồi quay ra đầu hẻm; chơi liền Đêm 1→2→3; kiểm download từ release đúng commit. Chưa tạo video demo đạt yêu cầu. Chưa phát hành hoặc đánh dấu #127 xong. Ảnh và CSV là từ app native mới, không phải Editor.


## Đồng bộ menu với main — 28/11/2026

Gộp `main@7999a25` (đã có #130, #149, #153) vào nhánh #131. Giữ màn hỏi đồng ý thống kê, mục gửi bằng chứng, trang giới thiệu và chặn mở cài đặt khi chưa trả lời; đồng thời cập nhật danh sách khi phiên lưu gắn muộn hoặc trạng thái đồng ý/quyền gửi bằng chứng thay đổi. Cảnh báo nội dung vẫn xuất hiện nếu phiên tới sau khi menu mở. Ảnh nền làm mờ bỏ canvas menu/cảnh báo khỏi khung chụp, tránh chữ cảnh báo lưu lại phía sau menu.

Máy kiểm: Apple M2 / 8 GB / macOS 15.7.4, Unity 6000.3.26f1. Các thư mục `THREEGIO12_DATA_ROOT` riêng, không dùng bản lưu người chơi.

| Bài kiểm | Nguồn | Kết quả |
|---|---|---|
| `SaveProgressRegression.Write`, `Read` — hai tiến trình, cùng dữ liệu | `5ed3937` | `SAVE_WRITE_PROCESS_PASSED`, `SAVE_READ_PROCESS_PASSED`, exit 0 |
| `OnlineServicesRegression.Run` | `5ed3937` | `ONLINE_LOGIC_CASES_PASSED`, `ONLINE_PLATFORM_GUARD_PASSED`, `ONLINE_SERVICES_REGRESSION_PASSED`, exit 0 |
| `PauseMenuSessionRegression.Run` | `a6ca144` | `PAUSE_MENU_SESSION_REGRESSION_PASSED`, exit 0 |
| `SaveProgressRegression.Menu` — Editor có đồ họa | `a6ca144` | `SAVE_MENU_CAPTURE_PASSED`, exit 0; đã xem ảnh warning/menu/settings |

Bài lưu/nạp hiện dùng NightOneGraybox để kiểm trạng thái/lưu trữ; bài menu gắn phiên muộn dùng NightOneRealistic. Không suy ra đã chơi xuyên ba đêm từ các bài này. Log Editor có lỗi lập chỉ mục `UnityEditor.Search.SearchDatabase` lúc khởi động như main.

Telemetry chỉ hỗ trợ Windows: Mac phải không hỏi/không xếp hàng/không gửi, kể cả gọi `SetConsent(true)` bằng mã. Bản sửa bổ sung chặn này ở API; không bật telemetry trên Mac để làm bài kiểm đạt. Vẫn kiểm HTTP loopback gửi **hash** bằng chứng, phản hồi từ chối/chấp nhận/lỗi máy chủ và lưu unlock khi từ chối thống kê. Nhánh kiểm transport/batches/revocation Windows giữ nguyên nhưng cần chạy trên Windows, chưa được xác nhận bởi lượt Mac này.

Đây là cập nhật mã để duyệt #131; zip native và số FPS ngày 26/11 vẫn từ `dd481ed`, không phải bản dựng của các sửa 28/11. Chưa đóng #127 hoặc phát hành Mac.

Ảnh Game View mới (không phải app native): [cảnh báo](qa/macos-menu-2026-11-28/warning.png), [menu sau xác nhận](qa/macos-menu-2026-11-28/menu.png), [cài đặt](qa/macos-menu-2026-11-28/settings.png). Bài chụp dùng bàn phím Input System riêng, đợi đủ thời gian tối thiểu rồi xác nhận cảnh báo; kiểm ảnh tồn tại trước khi báo đạt.

## Đồng bộ sau khi #85/#90/#152 gộp vào main

Gộp `main@8c5348a` vào #131 ở `b36fadf`. Mã lưu/nạp trên nhánh mới dùng NightOneRealistic (các kết quả Graybox ngày trước chỉ áp dụng nguồn cũ). Trên M2/macOS 15.7.4/Unity 6000.3.26f1, dữ liệu cách ly:

| Bài | Kết quả trên b36fadf |
|---|---|
| PauseMenuSessionRegression.Run | PASS / exit 0 |
| SaveProgressRegression.Write / Read — hai tiến trình | PASS / exit 0 |
| OnlineServicesRegression.Run | PASS logic/chặn telemetry Mac/gửi hash bằng chứng qua HTTP loopback / exit 0 |
| SaveProgressRegression.Menu — có đồ họa | PASS / exit 0 |

Nhánh transport/revocation Windows vẫn cần Thắng kiểm; không dùng lượt Mac thay cho nhánh này. Như trước, log có SearchDatabase lúc khởi động.

Sửa tiếp `27a10e9` cho công cụ chụp menu: trên Windows mượn thiết bị PhysicalKeyboardHook đang sở hữu Keyboard.current, xếp phím Space theo onBeforeUpdate sau callback của hook; không tắt hook hoặc xóa thiết bị mượn. Lý do tương tự lỗi công cụ kiểm điện thoại trong #162; đây không phải thay đổi gameplay. Cần người duyệt chạy lại Menu trên Windows với DATA_ROOT mới.

#85/#90 đã gộp. Quang đã chuyển phần chơi xuyên N1→N2→N3 và quay video sang Thắng/#140 trên Windows; #22 còn mở. Việc đó không tự xác nhận tiêu chí app Mac đã chơi hết mở đầu, bản Intel, hay tải zip từ release. #131 vẫn cần duyệt/gộp; binary Mac nội bộ đã dựng trước đó không được coi là binary của mã đồng bộ mới hoặc đính vào release khác commit.

Lượt Menu cuối trên `27a10e9` đạt `SAVE_MENU_REVIEW_INPUT_DEVICE SaveMenuReviewKeyboard owned=True`, `SAVE_MENU_CAPTURE_PASSED`, exit 0. Đã xem ảnh menu và cài đặt trên cảnh tả thực: không dính chữ cảnh báo ở nền. Ba ảnh warning/menu/settings trong thư mục QA đã thay bằng lượt này. Chưa chạy bản sửa nhập phím này trên Windows.
