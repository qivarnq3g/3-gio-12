# Đêm 2: buổi sáng trong cảnh tả thực (#22, đang chuyển)

Nhánh đã cập nhật `main` tới `482d1db` và giữ `NightOneHud`/HudKit của #99. Điểm vào duy nhất là `Assets/Scenes/Night01/NightOneRealistic.unity`, Unity `6000.3.26f1`. Không dùng `NightOneGraybox`, không dựng lại thế giới riêng ở x=100 và không chỉnh scene bằng tay. Các tương tác bổ sung khi chạy nên không bị mất khi `NightOneRealisticSetup` dựng lại cảnh.

## Trạng thái thực tế ngày 23/11

**Chưa chơi xuyên Đêm 1 → Đêm 2 → Đêm 3 được. Không đánh dấu #22 hoàn thành.**

Kiểm kê scene bằng Unity xác nhận các marker vị trí đã có, nhưng ba model cho chuỗi buổi sáng còn thiếu đối tượng hiển thị để gắn gameplay:

| Hạng mục | Vị trí trong scene | Kết quả kiểm tra |
|---|---|---|
| Xe bánh mì | `BanhMi_Cart`, tâm (-2.3, 0.55, 2.2) | Renderer thô đã tắt; marker người bán `NPC_N2_BanhMiSeller` vẫn có |
| Kệ phòng 7 | `BH_F3_Room7_Rack`, tâm (-2.05, 11.05, -44.15) | Renderer thô đã tắt; không bật lại để giả model tả thực |
| Đèn kẹp bàn | Phòng 7 | Chưa có đối tượng tương tác độc lập; cần nhóm chốt vị trí và model |

Runtime nhận model tả thực qua tên `Night2_BreadCart`, `Night2_Shelf`, `Night2_ClampLamp`; có thể cập nhật ánh xạ trong `NightTwoMorningWorld` khi nhóm thống nhất tên khác. `ValidateSceneAssets()` chỉ chấp nhận model có renderer đang hiển thị. Không coi marker rỗng hay renderer đã tắt là asset hoàn chỉnh.

Khi thiếu asset, nút bắt đầu Đêm 2 ở cuối Đêm 1 bị khóa và ghi rõ danh sách đang chờ. Gọi `Begin()` trực tiếp cũng từ chối trước khi sửa state, checkpoint hoặc sinh world. Các save thử nghiệm cảnh thô không được chuyển thành tọa độ tả thực tự động; hãy chơi mới trong cảnh chính.

## Phần đã chuyển trong mã

- HUD buổi sáng gọi HudKit cho font BeVietnamPro, nhãn, nút, cuộn, prompt và phụ đề; không dùng font LegacyRuntime/GUI.skin riêng. Điện thoại Ngân của Đêm 1 không mở chồng lên giao diện buổi sáng.
- Room, garage, cửa hàng và người bán lấy vị trí từ marker của cảnh chính. Tuyến đi mua đồ bám hẻm → đường lớn → cửa hàng, giữ thước đo tiến trình trong DTO để không thay cách tính km.
- World buổi sáng chỉ thêm volume tương tác không hiển thị lên model/marker hiện có. Không tạo nền, tường, đường, NPC hay đồ vật bằng primitive.
- Lựa chọn ngủ/thức, số phòng, odometer và các bước ráp kệ vẫn trong DTO của #22. Bản chuyển này chưa xác nhận được chuỗi gameplay đó trong cảnh tả thực do thiếu các model trên.

## Kiểm tra

`FollowupSceneRegression.Inventory` mở cảnh thật và ghi `FOLLOWUP_MODEL`, `FOLLOWUP_RENDERER`, `FOLLOWUP_MISSING` để đối chiếu model/marker.

`FollowupSceneRegression.Run` chạy Play Mode thật: không cho bắt đầu Đêm 2 trước kết thúc Đêm 1; kiểm tra readiness theo asset hiện có; nếu thiếu model thì state không đổi, không bật cờ Đêm 2 và không tạo world dở dang. Kết quả ngày 23/11: `FOLLOWUP_SCENE_GUARD_PASSED`, đồng thời ghi `FOLLOWUP_ASSET_BLOCKER` cho ba model trên. Đây là kiểm tra điều kiện chuyển cảnh, **không phải** kết quả chơi xuyên ba đêm.

```sh
THREEGIO12_DATA_ROOT=/tmp/3gio12-realistic-review /path/to/Unity \
  -projectPath /path/to/repo/game -executeMethod FollowupSceneRegression.Run \
  -logFile /tmp/3gio12-realistic-review.log
```

Các kết quả `NightTwoMorningRegression.Write/Read` cũ thuộc cảnh thô, không chứng minh bản chuyển này đạt. Cần chuyển lại các fixture sau khi có model. Tiếp theo phải kiểm E/raycast thực tế, đi xe hai chiều, save/continue qua tiến trình mới, cổng/cầu thang/phòng 7, ráp kệ từng tầng, thử đèn, nghỉ tới 21:00, HUD ở nhiều tỉ lệ màn hình; sau đó tích hợp #85/#90 và build `PlaytestBuild.NightOneReal` để chơi xuyên ba đêm.
