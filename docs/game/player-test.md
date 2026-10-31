# Phòng thử điều khiển (#6)

Unity 6000.3.26f1. Mở `game/Assets/Scenes/PlayerTest.unity`, bấm Play và nhấp vào Game view. Esc mở tạm dừng (dừng thời gian và âm thanh); nút Tiếp tục nhận lại điều khiển. Khi đang xem điện thoại, Esc chỉ cất máy.

| Phím | Hành động |
|---|---|
| WASD / cần trái gamepad | Đi |
| Chuột / cần phải | Nhìn |
| Shift / nhấn cần trái | Chạy |
| C / nút East | Bật/tắt cúi |
| E / nút North | Tương tác |
| 1 | Rút/cất máy Ngân |
| 2 | Rút/cất máy Sinh viên |
| Esc | Cất máy; ngoài điện thoại thì tạm dừng/tiếp tục |

## Kiểm tra thủ công

1. Đi và chạy trong phòng, thử đi vào tường và cửa đóng: không xuyên qua.
2. Cúi để đi dưới khối thấp ở bên trái. Thử đứng khi còn bên dưới: phải giữ cúi. Ra ngoài mới đứng được.
3. Đến gần cửa xanh, nhìn vào cửa: có gợi ý E. Bấm E mở, đi qua rồi đóng. Đứng trong lối cửa thì không cho đóng.
4. Nhìn vật vàng trên bệ ở phòng sau, bấm E: vật biến mất, số vật đã nhặt tăng một lần. Không nhặt xuyên tường.
5. Bấm 1 rồi giữ WASD/di chuột: nhân vật và camera đứng yên; khối vàng quay ở phòng sau tiếp tục quay. Bấm 2 để chuyển máy, Esc để cất.
6. Chuyển cửa sổ khác rồi quay lại: phải bấm Tiếp tục hoặc Esc trước khi điều khiển lại.
7. Mở SampleScene: prototype điện thoại cũ vẫn hoạt động. PlayerTest không tự mở prototype đó.

## Phạm vi

- CharacterController xử lý va chạm; camera tầm mắt với near clip 0.03 m; kích thước chuẩn ở `PlayerGeometry` (cao 1.8 m, mắt 1.6 m, bán kính 0.3 m); cúi đổi cả capsule và tầm mắt, kiểm khoảng trống trước khi đứng.
- Nhân vật dùng layer `Player`, tay dùng `FirstPersonArms`. Mặt nạ `PlayerGeometry.WorldMask` loại đúng hai layer này khỏi ray tương tác, kiểm trần và tầm nhìn NPC; không dùng layer 2 cho nhân vật.
- InputActionAsset hiện có được clone khi chạy; các phím điện thoại chỉ thêm vào bản clone, không thay đổi input dùng chung.
- Hai cẳng tay là khối giữ chỗ, chờ model/animation của Tiên; chúng được vẽ bằng URP Overlay Camera với clear depth riêng, không bị tường cắt hình. Điện thoại là giao diện giữ chỗ để nối với #17; chưa có chat, lưu game hoặc kịch bản.
- Cửa trượt lên là đối tượng thử tương tác, chưa phải cửa hoàn thiện cho màn truyện.
- `FirstPersonPlayer.SetPhone(0/1/2)` là điểm nối cho UI điện thoại; không đổi Time.timeScale.

## Kiểm tra bằng Unity

- `PlayerTestSetup.Create`: tạo lại phòng thử (ghi đè scene, chỉ dùng khi chủ động muốn dựng lại).
- `PlayerTestSetup.Validate`: kiểm cấu hình, cửa, nhặt đồ, cúi dưới trần và trạng thái hai máy; chạy bằng batchmode trong phiên Unity riêng.
- `PlayerTestSmoke.Run`: kiểm input trong Play Mode; chỉ dùng phiên kiểm thử riêng vì tự thoát Unity khi xong.

Trước khi mở PR đóng #6, quay video thao tác theo danh sách trên và ghép model tay nếu nhóm yêu cầu bản cuối.
