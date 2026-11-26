# Triển khai máy chủ, trang quản trị và website câu đố lên Azure

Dành cho task #32. Tài liệu này chưa được chạy thật trên Azure: các lệnh `az` và bước trên máy ảo viết theo tài liệu và kết quả kiểm tra ngày 26/11/2026, chưa chạy trọn vẹn một lần. Mục 6 ghi cách kiểm.

## 1. Kiến trúc

Một máy ảo Linux chạy ba container bằng `docker-compose.prod.yml`:

| Container | Việc | Mở ra ngoài |
|---|---|---|
| `caddy` | HTTPS tự động; phục vụ trang quản trị ở `/admin/`, website câu đố ở `/`; chuyển `/api/*` sang `api` | 80, 443 |
| `api` | Máy chủ ASP.NET Core | Không |
| `postgres` | Cơ sở dữ liệu, dữ liệu trong volume `pgdata` | Không |

Game gọi `https://<DOMAIN>/api/v1/...`. Trang quản trị cùng nguồn gốc với API nên không cần CORS.

Biến `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` của `api` là bắt buộc. Mã giới hạn tần suất lấy IP từ `RemoteIpAddress`; thiếu biến này thì mọi người chơi mang IP của `caddy`, và một người vượt giới hạn sẽ chặn cả nhóm.

## 2. Khu vực và tài khoản

Gói "Azure for Students" của nhóm có chính sách chỉ cho tạo tài nguyên ở năm khu vực: `centralindia`, `japanwest`, `indonesiacentral`, `malaysiawest`, `koreacentral` (kiểm bằng `az policy assignment list` ngày 26/11/2026). Singapore (`southeastasia`) không dùng được. Tài liệu này dùng `malaysiawest` (Malaysia West): gần Việt Nam, có máy `Standard_B1ms` (1 vCPU, 2 GB RAM, kiểm bằng `az vm list-sizes`), giá 0,0238 USD mỗi giờ. `indonesiacentral` cùng giá và cùng có máy này, dùng làm phương án dự phòng.

Tín dụng sinh viên là 100 USD, dùng trong 12 tháng, một gói cho mỗi người. Nếu dùng gói đang chứa tài nguyên của môn Điện toán đám mây thì hai việc chia chung 100 USD đó. Tạo tài nguyên của nhóm trong một resource group riêng (`rg-3gio12`) để xóa gọn bằng một lệnh, không đụng tài nguyên môn kia.

## 3. Tạo máy ảo

Chạy trong PowerShell, đã `az login` và chọn đúng gói (`az account show`).

```powershell
$RG   = "rg-3gio12"
$LOC  = "malaysiawest"
$VM   = "vm-3gio12"
$DNS  = "<tên-duy-nhất-không-dấu>"      # DOMAIN sẽ là $DNS.$LOC.cloudapp.azure.com

az group create -n $RG -l $LOC

az vm create -g $RG -n $VM -l $LOC `
  --image Canonical:ubuntu-24_04-lts:server:latest `
  --size Standard_B1ms `
  --storage-sku StandardSSD_LRS --os-disk-size-gb 32 `
  --public-ip-sku Standard --public-ip-address-dns-name $DNS `
  --admin-username azureuser --generate-ssh-keys `
  --custom-data deploy/cloud-init.yaml

az vm open-port -g $RG -n $VM --port 80  --priority 1010
az vm open-port -g $RG -n $VM --port 443 --priority 1020
```

`az vm create` mở cổng 22 cho mọi địa chỉ. Giới hạn lại theo địa chỉ IP của người quản trị:

```powershell
$MYIP = (Invoke-RestMethod https://api.ipify.org)
az network nsg rule update -g $RG --nsg-name "$($VM)NSG" -n default-allow-ssh --source-address-prefixes $MYIP
```

`deploy/cloud-init.yaml` cài Docker, tạo 2 GB swap (máy B1ms chỉ có 2 GB RAM, dựng ảnh .NET cần thêm bộ nhớ) và Git.

## 4. Đưa mã lên máy ảo

Repo riêng tư nên máy ảo cần khóa chỉ đọc (deploy key):

```bash
ssh azureuser@$DNS.$LOC.cloudapp.azure.com
ssh-keygen -t ed25519 -f ~/.ssh/deploy -N ""
cat ~/.ssh/deploy.pub     # dán vào GitHub: Settings > Deploy keys > Add (không tick Write access)
export GIT_SSH_COMMAND="ssh -i ~/.ssh/deploy -o IdentitiesOnly=yes"

# Chỉ lấy các thư mục cần cho máy chủ; bỏ thư mục game (LFS, hàng nghìn tệp).
git clone --depth 1 --filter=blob:none --sparse git@github.com:qivarnq3g/3-gio-12.git app
cd app
git sparse-checkout set server admin arg-site deploy docs/api
```

## 5. Chạy

```bash
cd ~/app
cp .env.production.example .env.production
nano .env.production        # điền DOMAIN, POSTGRES_PASSWORD, ADMIN_EMAIL, ADMIN_PASSWORD
docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build
docker compose -f docker-compose.prod.yml ps
```

Lần dựng đầu mất vài phút. Caddy xin chứng chỉ HTTPS khi có yêu cầu đầu tiên vào `DOMAIN`; cổng 80 phải mở và DOMAIN phải trỏ đúng về máy ảo.

Sao lưu hằng đêm (cron của `azureuser`):

```bash
mkdir -p ~/backups
( crontab -l 2>/dev/null; echo '15 3 * * * cd ~/app && docker compose -f docker-compose.prod.yml exec -T postgres pg_dump -U threetwelve threetwelve | gzip > ~/backups/$(date +\%F).sql.gz' ) | crontab -
```

## 6. Cách kiểm

Làm theo thứ tự và ghi kết quả vào PR hoặc issue của #32.

1. `docker compose -f docker-compose.prod.yml ps`: ba dịch vụ ở trạng thái chạy, `postgres` khỏe.
2. `https://<DOMAIN>/` mở website câu đố, ổ khóa HTTPS hợp lệ.
3. `https://<DOMAIN>/admin/` mở trang đăng nhập; đăng nhập bằng `ADMIN_EMAIL` và `ADMIN_PASSWORD`.
4. `POST https://<DOMAIN>/api/v1/telemetry/batches` với một lô hợp lệ trả 200; sự kiện hiện trên dashboard.
5. Kiểm giới hạn theo IP: dùng hai máy khác mạng gửi liên tục; máy thứ nhất bị 429 không làm máy thứ hai bị chặn.
6. Chạy game bản dựng (task #140) với `THREEGIO12_API_BASE=https://<DOMAIN>`: đồng ý gửi thống kê, chơi qua một checkpoint, rút đồng ý, kiểm dữ liệu bị xóa.
7. Khởi động lại máy ảo (`az vm restart`): các dịch vụ tự lên lại (`restart: unless-stopped`) và dữ liệu còn nguyên.

## 7. Chi phí và tắt máy

- Máy ảo B1ms tính theo giờ khi đang chạy. Dừng và giải phóng (`az vm deallocate -g rg-3gio12 -n vm-3gio12`) thì ngừng tính tiền máy, nhưng ổ đĩa và địa chỉ IP tĩnh vẫn tính.
- Xóa hết sau khi chấm điểm: `az group delete -n rg-3gio12`. Lệnh này xóa cả dữ liệu; sao lưu `pg_dump` về máy trước.
- Giá và tín dụng: xem mục 4 của `docs/report/ke-hoach-khoi-nghiep.md`.

## 8. Điều chưa kiểm

- Chưa chạy các lệnh ở mục 3 đến 5 trên Azure.
- Chưa dựng ảnh Docker của `caddy` và `api` (máy soạn không chạy Docker). Đã kiểm: `docker compose config` hợp lệ, `npm run build` của trang quản trị chạy được và đường dẫn tài nguyên bắt đầu bằng `/admin/`.
- Chưa kiểm Caddyfile bằng `caddy validate`.
- Máy B1ms có trong danh sách của Malaysia West và Indonesia Central, nhưng chưa kiểm được gói sinh viên của nhóm có bị hạn chế riêng máy này không (lệnh `az vm list-skus` quá chậm khi thử). Nếu `az vm create` bị từ chối, thử `Standard_B2als_v2` (4 GB RAM, 0,0428 USD mỗi giờ) hoặc khu vực khác trong danh sách ở mục 2. VM cũ của gói này ở Hàn Quốc dùng `Standard_B2ls_v2`.
- Website câu đố có thể cần chỉnh nếu đang giả định đường dẫn gốc khác.
