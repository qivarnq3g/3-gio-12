# Trang quản trị

React, TypeScript, Vite. Thiết kế ở `docs/thiet-ke/luoc-do-lop-may-chu.md` mục 7, API ở `docs/api/`.

## Cần cài

- Node.js 24
- Máy chủ API đang chạy ở `http://localhost:8080` (xem `server/README.md`)

## Chạy khi phát triển

```bash
cd admin
npm install
npm run dev
```

Mở `http://localhost:5173/admin/`. Máy chủ phát triển của Vite chuyển mọi yêu cầu `/api` sang `http://localhost:8080`, nên cookie đăng nhập dùng thẳng như khi chạy thật.

Tài khoản thử là `ADMIN_EMAIL`, `ADMIN_PASSWORD` trong `.env` ở thư mục gốc; máy chủ tạo tài khoản này lúc khởi động nếu chưa có tài khoản nào.

## Kiểu dữ liệu của API

Kiểu TypeScript sinh từ `docs/api/openapi.yaml` vào `src/api/schema.d.ts` (không đưa vào Git). `npm run dev`, `npm test` và `npm run build` đều tự sinh lại trước khi chạy, nên đổi hợp đồng mà quên sửa trang thì build báo lỗi. Gọi API bằng `api` trong `src/api/client.ts`:

```ts
const { data, error } = await api.GET("/api/admin/accounts");
```

Máy chủ trả `401` (phiên hết hạn hoặc tài khoản bị khóa) thì `client.ts` báo cho `AuthProvider`, và trang tự về màn hình đăng nhập.

## Kiểm thử và build

```bash
npm test
npm run build
```

Build ra `dist/`, proxy ngược phục vụ ở `/admin`.

## Cấu trúc

| Thư mục | Nội dung |
|---|---|
| `src/api/` | `client.ts`: gọi API bằng `openapi-fetch`, xử lý `401` |
| `src/auth/` | `AuthProvider` giữ tài khoản đang đăng nhập; `RequireAuth` đưa người chưa đăng nhập về `/admin/login` |
| `src/layout/` | `AdminLayout`: thanh bên, nút đăng xuất |
| `src/pages/` | Các trang. `LoginPage` đã xong; dashboard (#28), câu đố (#29), tài khoản và đổi mật khẩu (#27) đang là trang chờ |
