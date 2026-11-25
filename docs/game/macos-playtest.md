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
