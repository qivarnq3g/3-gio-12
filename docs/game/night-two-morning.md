# Đêm 2: buổi sáng trong cảnh tả thực (#22)

## Trạng thái hiện tại — 26/11/2026

#83 đã cập nhật `main@70ab9c5` (merge gameplay `7dc4d42`). Model buổi sáng #121 và buổi tối #133 đã được gộp vào main; không còn dùng tình trạng thiếu ba model buổi sáng làm lý do giữ #83 ở nháp. Phần buổi sáng sẵn sàng cho người khác chạy thử/duyệt theo yêu cầu của Quang. Chưa đóng #22 hoặc xác nhận chơi xuyên ba đêm.

Điểm vào duy nhất: `Assets/Scenes/Night01/NightOneRealistic.unity`, Unity `6000.3.26f1`. Không dùng `NightOneGraybox`, thế giới riêng hoặc model thô trong bản chơi. Các tương tác được thêm lúc chạy nên không mất khi `NightOneRealisticSetup` dựng lại cảnh.

## Hành vi

- HUD gọi HudKit: font BeVietnamPro, nhãn, nút, cuộn, prompt, phụ đề. Điện thoại Ngân Đêm 1 không mở chồng lên UI buổi sáng.
- Phòng, nhà xe, cửa hàng, người bán lấy vị trí từ marker trong cảnh. Tuyến mua đồ theo hẻm → đường lớn → cửa hàng; tiến trình tuyến và km được lưu trong DTO.
- Runtime chỉ thêm volume tương tác không hiển thị lên model/marker sẵn có; không dựng nền, tường, đường, NPC hoặc đồ vật bằng primitive.
- Nhận `Night2_BreadCart`, `Night2_Shelf`, `Night2_ClampLamp` có renderer bật kể cả khi đối tượng/nhóm cha đang tắt. Không nhận marker rỗng hoặc renderer thô đã tắt.
- `Night2 props` tắt trong Đêm 1, bật khi bắt đầu Đêm 2. Xe bánh mì hiện từ buổi sáng; kệ hiện từng `Tier_1`–`Tier_4` đã ráp; đèn hiện sau tầng cuối, nguồn sáng/phát sáng bật khi thử. Chơi mới trả trạng thái về Đêm 1.
- Thiếu model thì khóa chuyển đêm và báo danh sách; gọi `Begin()` trực tiếp cũng từ chối trước khi sửa state/checkpoint hoặc tạo world. Không tự đổi tọa độ save thử nghiệm cảnh thô thành cảnh tả thực.

## Kết quả trên main mới

Unity 6000.3.26f1, Apple M2/macOS 15.7.4, `-batchmode -nographics`, dữ liệu cách ly. Ba tiến trình riêng kết thúc exit 0:

| Bài kiểm | Bằng chứng |
|---|---|
| `NightTwoMorningRegression.Write` | `NIGHT_TWO_REAL_MODELS_CONFIRMED_NO_FIXTURES`, `NIGHT_TWO_WRITE_PASSED` |
| `NightTwoMorningRegression.Read` | `NIGHT_TWO_REAL_MODELS_CONFIRMED_NO_FIXTURES`, `NIGHT_TWO_READ_PASSED` |
| `FollowupSceneRegression.Run` | `FOLLOWUP_SCENE_GUARD_PASSED`, không có `FOLLOWUP_ASSET_BLOCKER` |

Write/Read bật `THREEGIO12_REQUIRE_MORNING_MODELS=1`: từ chối fixture nếu thiếu model thật, không thêm model thay thế hoặc ép ẩn nhóm trong chế độ này. Kiểm hai nhánh ngủ/thức, báo thức, phanh/đèn đỏ, lưu giữa tuyến, cổng thấy/không thấy/tường che, ráp từng tầng, nạp ráp dở qua lần mở Unity mới, đèn, lời đáp số phòng, từ chối NaN, nghỉ tới 21:00 và chơi mới. Bài chuyển đêm kiểm điều kiện kết thúc Đêm 1, readiness, bắt đầu trong phòng thật và save hợp lệ.

```sh
THREEGIO12_REQUIRE_MORNING_MODELS=1 THREEGIO12_DATA_ROOT=/tmp/3gio12-morning-review \
  /path/to/Unity -batchmode -nographics -projectPath /path/to/repo/game \
  -executeMethod NightTwoMorningRegression.Write -logFile /tmp/morning-write.log
THREEGIO12_REQUIRE_MORNING_MODELS=1 THREEGIO12_DATA_ROOT=/tmp/3gio12-morning-review \
  /path/to/Unity -batchmode -nographics -projectPath /path/to/repo/game \
  -executeMethod NightTwoMorningRegression.Read -logFile /tmp/morning-read.log
```

Dùng cùng thư mục dữ liệu trống/cách ly cho Write rồi Read. Không thêm `-quit`. Mặc định vẫn có fixture chỉ trong Editor cho nhánh chưa có asset, không ghi vào scene/build. Chế độ nghiêm phía trên không dùng fixture.

## Giới hạn và bước duyệt

Batchmode không chờ ảnh Game View: ghi `NIGHT_TWO_PREVIEW_SKIPPED_NO_GAME_VIEW` rồi kết thúc kiểm logic. Các kết quả trên không thay cho kiểm hình ảnh, raycast bằng input hoặc chơi tay trên Windows. Log vẫn có lỗi SearchDatabase/indexing có sẵn; không có lỗi biên dịch.

Lượt Editor có GPU trước đó trên nhánh ghép thử #121 dừng exit 132, có native crash và lỗi Metal `no member named 'mtl_Position' in 'Mtl_ControlPointIn'`; chưa xác định nguyên nhân. Không tính lượt đó là đạt, không kết luận app Mac native sập từ lỗi Editor.

Người duyệt cần chạy cảnh chính: kết thúc Đêm 1 → bắt đầu Đêm 2, thử E/raycast, xe hai chiều, save/continue, cổng/cầu thang/phòng 7, ráp kệ, thử đèn, nghỉ tới 21:00 và HUD các tỉ lệ. Sau khi #83 gộp mới đổi đích #85 sang main và đưa #85 đi duyệt. #90 tiếp tục chờ #130 và #138; bản Mac #131 còn kiểm chơi tay/release. Không coi #83 đủ để hoàn thành #22.
