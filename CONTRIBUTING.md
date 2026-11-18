# Quy ước làm việc

## Công cụ

- Unity `6000.3.26f1` (Unity 6.3 LTS), đúng như `game/ProjectSettings/ProjectVersion.txt`. Cài đúng bản này qua Unity Hub; mở project bằng bản khác thì Unity nâng cấp project và đổi hàng loạt file trong `ProjectSettings/`, `Packages/`. Đổi phiên bản phải mở issue riêng và cả nhóm đổi cùng lúc.
- Blender 5.2 cho model 3D.

## Nhánh

- `main` luôn chạy được. Không commit thẳng vào `main`.
- Tạo nhánh từ issue, đặt tên `<loai>/<so-issue>-<mo-ta-ngan>`, ví dụ `feat/12-player-movement`, `fix/30-save-crash`, `docs/5-use-case`, `art/8-phong-tro`.

## Commit

Viết ngắn, ở thì hiện tại, bắt đầu bằng loại việc:

- `feat:` tính năng mới
- `fix:` sửa lỗi
- `art:` thêm hoặc sửa asset
- `docs:` tài liệu, biên bản
- `test:` kiểm thử
- `chore:` cấu hình, dọn dẹp

Ví dụ: `feat: thêm ẩn nấp trong tủ (#14)`.

## Pull request

1. Mở pull request vào `main`, ghi `Đóng #<số issue>` để issue tự đóng khi gộp.
2. Ít nhất một người khác duyệt. Người duyệt chạy thử, không chỉ đọc.
3. Người mở pull request tự gộp sau khi được duyệt, rồi xóa nhánh.

## Bảng việc

- Nhận việc: tự gán mình vào issue, kéo sang **Đang làm**.
- Mở pull request: kéo sang **Đang duyệt**.
- Gộp xong: issue tự sang **Xong**.
- Quá hạn hoặc bị vướng: bình luận vào issue và báo trong buổi họp tuần.

## Asset

- Ảnh, âm thanh, model đi qua Git LFS (đã cấu hình trong `.gitattributes`). Cài một lần: `git lfs install`.
- File gốc nặng (`.blend`, `.psd`, ảnh AI gốc) để trên Drive của nhóm, trong repo chỉ có file đã xuất cho Unity.
- Ghi nguồn và giấy phép của mọi asset lấy từ bên ngoài vào `game/Assets/Art/_Source/NGUON.md`.

## Model 3D

Chốt ngày 30/10/2026 (#3). Mẫu đã đi trọn quy trình: `game/Assets/Art/Models/Props/prop_potted_snake_plant.fbx`.

### Lấy model từ đâu

| Thứ tự | Nguồn | Giấy phép | Dùng cho |
|---|---|---|---|
| 1 | Tự dựng trong Blender | Của nhóm | Mọi model: đồ vật, bối cảnh, nhân vật (tự gắn xương trong Blender) |
| 2 | Kenney, Poly Pizza (chỉ lọc CC0), Poly Haven | CC0, không cần ghi tên | Đồ vật phổ thông, chỉnh màu lại cho hợp kiểu hình |
| 3 | Sketchfab | Xem từng model; chỉ lấy CC0 hoặc CC-BY và ghi tên tác giả | Thứ khó dựng mà nguồn 2 không có |

- Không dùng công cụ AI tạo model 3D. Nhóm tự dựng (chốt 31/10/2026).
- Không lấy model có thương hiệu thật (logo xe, biển hiệu).
- Game dùng hình tả thực (đổi ngày 02/11/2026, thay kiểu cách điệu ngày 27/10). Model chính giữ khối thật, ưu tiên mẫu quét từ vật thật. Cảnh `Assets/Scenes/RealisticPreview.unity` do `RealisticSceneBuilder` dựng: script đọc vật liệu `M_...` của model rồi đổi sang vật liệu URP Lit trong `Assets/Art/MaterialsPBR`, dùng ảnh vân trong `Assets/Art/TexturesPBR`. Bảng `Surfaces` trong script quy định mỗi `M_...` dùng ảnh vân nào; vật liệu mới phải thêm vào bảng này. Riêng `env_alley.fbx` và `env_garage.fbx` xuất từ bản tả thực nên vật liệu URP Lit nằm trong `Assets/Art/Models/Environment/Materials`; `ArtPipelineCheck` chấp nhận thư mục này.

### Dựng trong Blender

- Đơn vị mét, `Unit Scale` = 1. Đo theo vật thật: cửa 2,1 m, bậc thang cao 0,17 m, chậu kiểng khoảng 0,3 m, nhân vật nam 1,68 m.
- Đồ vật và nhân vật: gốc tọa độ (origin) đặt ở giữa đáy vật, đúng chỗ vật chạm sàn. Vật đứng ở gốc thế giới, mặt trước quay về phía -Y của Blender.
- Bối cảnh: dựng đúng tọa độ của khối thô `night1_3_blockout` (mở file khối thô, dựng đè lên khối cần thay), gốc tọa độ ở gốc thế giới. Kéo FBX vào cảnh ở vị trí (0, 0, 0) là khớp các điểm đánh dấu `CHECKPOINT_`, `PLACE_`, `DOOR_` của khối thô.
- Phần chuyển động được (cửa, ngăn kéo, nắp) là một object riêng trong cùng file, gốc tọa độ đặt ở bản lề, dựng ở tư thế đóng.
- Áp (apply) xoay và tỉ lệ trước khi xuất: `Ctrl+A` > `All Transforms`.
- Mọi lưới phải có UV, kể cả khi không dùng ảnh vân. Lưới không có UV thì Unity không tính được tiếp tuyến (tangent) và shader `Toon/Toon` tô đen các mặt quay theo trục X. Cách nhanh: chế độ Edit, chọn hết, `U` > `Cube Projection` hoặc `Smart UV Project`.
- Đồ vật nhỏ dưới 1.000 tam giác, đồ vật lớn và nhân vật phụ dưới 10.000. Ngoại lệ: vật chủ chốt người chơi nhìn cận và chỉ có vài cái trong cảnh (xe máy) được tới khoảng 50.000; xuống dưới 10.000 thì mẫu Super Cub vỡ dáng (thử ngày 02/11/2026). Phần lớn model không cần ảnh vân (texture): mỗi vùng màu là một vật liệu riêng. Riêng chữ và hình làm theo mục "Vật có chữ hoặc hình" bên dưới.
- Đặt tên vật liệu `M_<TenTiengAnh>` (ví dụ `M_PotTerracotta`). Hai model cùng màu thì dùng chung tên vật liệu.
- File `.blend` lưu trên Drive của nhóm, cùng tên với file FBX.

### Vật có chữ hoặc hình

Chọn cách làm theo việc chữ có đổi trong lúc chơi hay không, và người chơi có cần đọc kỹ hay không.

| Trường hợp | Ví dụ | Cách làm |
|---|---|---|
| Chữ, hình cố định, chỉ cần nhìn thấy | Nhãn vỉ thuốc, giấy ghi mật khẩu wifi, tờ giấy dán cột | Ảnh vân dán lên một mặt riêng của model |
| Chữ, số đổi trong lúc chơi | Số km trên đồng hồ xe (đổi theo nhánh), đồng hồ treo tường | Model để mặt trống, chữ do Unity vẽ bằng TextMeshPro |
| Vật chứng người chơi phải đọc kỹ | Vỉ thuốc, phiếu gửi tiền, sổ ghi chép | Làm như một trong hai dòng trên, thêm ảnh 2D phóng to hiện trên giao diện khi tương tác |

Ảnh vân dán lên model:

- Trong Blender, ưu tiên tách vùng có chữ thành mặt riêng ngay trên lưới của vật, gán vật liệu riêng; cách này không có hai mặt chồng nhau nên không bao giờ nhấp nháy. Chỉ khi vật cong hoặc nhiều mặt mới đặt một tấm phẳng mỏng nổi lên khỏi bề mặt 3 mm. Cách 1 mm dễ nhấp nháy khi nhìn từ xa vì độ chính xác chiều sâu giảm theo khoảng cách, mà đường lớn dài khoảng 440 m; nếu 3 mm vẫn nhấp nháy ở xa thì tăng lên 5 mm. Trải UV của mặt chữ phủ khít ảnh, trong khoảng 0 đến 1, và gán vật liệu riêng `M_<TenTiengAnh>`.
- Ảnh PNG, cạnh là lũy thừa của 2: 512 cho phần lớn đồ vật, 1024 khi chữ nhỏ mà người chơi phải đọc. Đặt tên `T_<TenTiengAnh>.png` trong `game/Assets/Art/Textures/`.
- Dùng ảnh chụp hoặc ảnh quét được, ghi nguồn trong `NGUON.md`. Chữ tiếng Việt dùng phông có đủ dấu. Không có tên hay logo của thương hiệu thật.
- Không cần chỉnh thiết lập nhập ảnh: `ModelImportRules.cs` tự đặt cho mọi ảnh trong `Assets/Art/Textures/` là sRGB bật, nén chất lượng cao (`High Quality`), mipmap bật, lọc `Trilinear`, `Aniso Level` 8, `Wrap Mode: Clamp`, cỡ tối đa 1024. Mipmap giữ bật để chữ không lấp lánh khi nhìn từ xa; chữ nhòe khi nhìn nghiêng do mipmap thì `Aniso Level` 8 bù lại.
- Ảnh lặp cho mặt lớn (tường, cửa cuốn, vỉa hè, nhựa đường) đặt tên `T_Tile<TenTiengAnh>.png`. Ảnh này vẽ gần trắng, chỉ có vân, vết ố và chỗ đậm nhạt; màu chính vẫn lấy từ `BaseColor` của vật liệu, nên một ảnh dùng được cho nhiều màu sơn. UV được phép ra ngoài 0 đến 1 để ảnh lặp lại, và `ModelImportRules.cs` đặt `Wrap Mode: Repeat` cho riêng các ảnh có tên bắt đầu bằng `T_Tile`.
- Vật liệu nguồn trong `Assets/Art/Materials` vẫn dùng shader `Toon/Toon` để `ArtPipelineCheck` kiểm và `RealisticSceneBuilder` đọc màu, ảnh: gán ảnh vào `BaseMap`, bật `Use BaseMap as 1st_ShadeMap` và `Use 1st_ShadeMap as 2nd_ShadeMap` để chữ không tối đi ở vùng bóng và vùng bóng sâu.

Chữ, số đổi trong lúc chơi:

- Model chỉ có mặt trống. Thêm một object `TextMeshPro` (bản 3D, chuột phải `3D Object > Text - TextMeshPro`, không phải `TextMeshProUGUI` của giao diện) làm con của model, đặt sát mặt trống. Script của màn chơi gán nội dung, ví dụ số km lấy từ trạng thái của đêm.
- Phông chữ mặc định của TextMeshPro trong project là `Assets/Art/Fonts/NotoSans-Regular SDF.asset`, tạo từ Noto Sans có sẵn trong repo, có đủ chữ tiếng Việt, chữ số và dấu câu thường gặp. Bảng ký tự cố định (static), nên ký tự ngoài bảng sẽ hiện thành ô vuông; cần ký tự lạ thì báo người giữ quy trình để thêm vào bảng.

Ảnh 2D phóng to: chữ trên model nhìn từ góc nhìn thứ nhất thường nhỏ và nghiêng. Vật chứng cần đọc thì xuất thêm một ảnh 2D có cùng nội dung, để giao diện hiện lên khi người chơi tương tác (#24). Ảnh vân và ảnh 2D, kể cả ảnh tạo bằng AI, đều ghi nguồn vào `NGUON.md`.

### Xuất FBX

`File > Export > FBX`, chọn:

- `Limit to: Selected Objects`, `Object Types: Mesh` (thêm `Armature` nếu có xương).
- `Apply Scalings: FBX Units Scale`, bật `Apply Unit`, bật `Apply Transform`.
- `Forward: -Z Forward`, `Up: Y Up`.
- `Geometry > Smoothing: Face`, bật `Apply Modifiers`.
- `Armature`: tắt `Add Leaf Bones`. Đồ vật đứng yên thì tắt `Bake Animation`.
- `Path Mode: Strip Path`, không nhúng ảnh.

Tên file `snake_case` tiếng Anh, bắt đầu bằng loại: `prop_` đồ vật, `env_` bối cảnh, `char_` nhân vật. Ví dụ `prop_potted_snake_plant.fbx`, `char_landlady_lien.fbx`.

### Đưa vào Unity

| Loại | Thư mục |
|---|---|
| Đồ vật | `game/Assets/Art/Models/Props/` |
| Bối cảnh | `game/Assets/Art/Models/Environment/` |
| Nhân vật | `game/Assets/Art/Models/Characters/` |
| Vật liệu | `game/Assets/Art/Materials/` |
| Ảnh vân | `game/Assets/Art/Textures/` |

1. Tạo vật liệu trước: trong `Assets/Art/Materials`, chuột phải > `Create > Material`, đặt đúng tên `M_...` như trong Blender, đổi shader thành `Toon/Toon` (gói Unity Toon Shader). Chỉnh `BaseColor`, `1st_ShadeColor` (tối hơn khoảng một phần ba), `2nd_ShadeColor` (tối hơn một nửa), `Outline_Width` khoảng 1, `Outline_Color` gần đen cùng tông. Nhanh nhất là nhân bản một vật liệu có sẵn rồi đổi màu.
2. Kéo file FBX vào đúng thư mục. `Assets/Editor/ModelImportRules.cs` tự đặt tỉ lệ 1, không nhập camera và đèn, tắt animation cho đồ vật và bối cảnh, và tự gắn vật liệu trùng tên trong project.
3. Chạy menu `3 Gio 12 > Check Art Models`. Lệnh báo lỗi khi gốc model có xoay hoặc tỉ lệ khác 1, khi đáy đồ vật hay nhân vật không nằm ở y = 0, khi lưới không có UV, khi vật liệu nằm ngoài `Assets/Art/Materials` hoặc không dùng shader `Toon/Toon`. Với vật có ảnh vân, lệnh còn báo lỗi khi ảnh `T_...` nằm ngoài `Assets/Art/Textures`, ảnh trong thư mục đó sai tên, cạnh không phải lũy thừa của 2 hoặc lớn hơn 1024, UV của mặt có ảnh ra ngoài khoảng 0 đến 1 (trừ ảnh lặp `T_Tile...`), hay vật liệu có ảnh mà chưa bật hai ô ShadeMap ở trên. Sửa trong Blender rồi xuất lại, không chữa bằng Transform trong Unity.
4. Nếu Unity đã tự tạo thư mục `Materials` cạnh file FBX (do kéo FBX vào trước khi có vật liệu), xóa thư mục đó rồi chuột phải file FBX > `Reimport`.

Chạy kiểm không cần mở Editor, ví dụ trước khi mở pull request:

```bash
Unity.exe -batchmode -projectPath game -executeMethod ArtPipelineCheck.Run -logFile -
```

Lệnh thoát với mã 0 khi đạt và ghi ảnh xem trước vào `game/Logs/ArtPipelineCheck.png`. Nếu máy không có mạng, Unity có thể crash lúc tắt và trả mã 139 dù đã kiểm xong; khi đó đọc dòng `[ArtCheck] PASS` hoặc `FAIL` trong log.

## Họp

Mỗi tuần một buổi. Người ghi biên bản chép từ `docs/bien-ban/_mau.md` thành `docs/bien-ban/yyyy-mm-dd.md` và mở pull request trong ngày.
