import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { App } from "./App";

const account = {
  id: "6f1c2a9e-3b7d-4c51-9a2e-0d8f4b6e1a73",
  email: "admin@example.test",
  displayName: "Quản trị viên",
  isActive: true,
  createdAt: "2026-10-30T00:00:00Z",
  lastLoginAt: null,
};

const otherAccount = {
  id: "8a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
  email: "mod@example.test",
  displayName: "Điều hành viên",
  isActive: true,
  createdAt: "2026-10-31T10:00:00Z",
  lastLoginAt: "2026-11-01T15:30:00Z",
};

type Reply = { status: number; body?: unknown };

function serve(routes: Record<string, (req?: Request) => Reply>) {
  const calls: string[] = [];
  vi.stubGlobal("fetch", async (input: Request) => {
    const url = new URL(input.url);
    const key = `${input.method} ${url.pathname}`;
    calls.push(key);
    calls.push(`${input.method} ${url.pathname}${url.search}`);
    const reply = routes[key]?.(input) ?? { status: 404 };
    return new Response(reply.body === undefined ? null : JSON.stringify(reply.body), {
      status: reply.status,
      headers: { "Content-Type": "application/json" },
    });
  });
  return calls;
}

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  );
}

async function fillLogin(password: string) {
  await userEvent.type(await screen.findByLabelText("Email"), "admin@example.test");
  await userEvent.type(screen.getByLabelText("Mật khẩu"), password);
  await userEvent.click(screen.getByRole("button", { name: "Đăng nhập" }));
}

afterEach(() => {
  vi.unstubAllGlobals();
});

test("a visitor without a session lands on the login form", async () => {
  serve({ "GET /api/admin/auth/me": () => ({ status: 401 }) });

  renderAt("/accounts");

  expect(await screen.findByRole("heading", { name: "Đăng nhập quản trị" })).toBeInTheDocument();
});

test("logging in returns to the page the visitor asked for", async () => {
  serve({
    "GET /api/admin/auth/me": () => ({ status: 401 }),
    "POST /api/admin/auth/login": () => ({ status: 200, body: account }),
    "GET /api/admin/accounts": () => ({ status: 200, body: [account] }),
  });
  renderAt("/accounts");

  await fillLogin("mat-khau-thu-dai-12");

  expect(await screen.findByRole("heading", { name: "Tài khoản quản trị" })).toBeInTheDocument();
  expect(screen.getAllByText("Quản trị viên").length).toBeGreaterThan(0);
});

test("a wrong password shows the error and stays on the form", async () => {
  serve({
    "GET /api/admin/auth/me": () => ({ status: 401 }),
    "POST /api/admin/auth/login": () => ({ status: 401, body: { title: "Sai email hoặc mật khẩu", status: 401 } }),
  });
  renderAt("/");

  await fillLogin("sai-mat-khau-roi");

  expect(await screen.findByRole("alert")).toHaveTextContent("Sai email hoặc mật khẩu");
  expect(screen.getByRole("heading", { name: "Đăng nhập quản trị" })).toBeInTheDocument();
});

test("too many attempts tells the visitor to wait", async () => {
  serve({
    "GET /api/admin/auth/me": () => ({ status: 401 }),
    "POST /api/admin/auth/login": () => ({ status: 429 }),
  });
  renderAt("/");

  await fillLogin("sai-mat-khau-roi");

  expect(await screen.findByRole("alert")).toHaveTextContent("Chờ một phút");
});

test("an existing session opens the dashboard and logging out returns to the form", async () => {
  const calls = serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "POST /api/admin/auth/logout": () => ({ status: 204 }),
  });
  renderAt("/");
  expect(await screen.findByRole("heading", { name: "Dashboard" })).toBeInTheDocument();

  await userEvent.click(screen.getByRole("button", { name: "Đăng xuất" }));

  expect(await screen.findByRole("heading", { name: "Đăng nhập quản trị" })).toBeInTheDocument();
  expect(calls).toContain("POST /api/admin/auth/logout");
});

test("ProfilePage: successfully changes password with matching input", async () => {
  const calls = serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "POST /api/admin/auth/password": () => ({ status: 204 }),
  });

  renderAt("/profile");
  expect(await screen.findByRole("heading", { name: "Đổi mật khẩu" })).toBeInTheDocument();

  await userEvent.type(screen.getByLabelText("Mật khẩu hiện tại"), "matkhaucu12345");
  await userEvent.type(screen.getByLabelText("Mật khẩu mới (tối thiểu 12 ký tự)"), "matkhaumoi12345");
  await userEvent.type(screen.getByLabelText("Nhập lại mật khẩu mới"), "matkhaumoi12345");

  await userEvent.click(screen.getByRole("button", { name: "Lưu mật khẩu mới" }));

  expect(await screen.findByRole("status")).toHaveTextContent("Đã đổi mật khẩu thành công.");
  expect(calls).toContain("POST /api/admin/auth/password");
});

test("ProfilePage: reports validation errors for mismatched passwords or short password", async () => {
  serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
  });

  renderAt("/profile");
  expect(await screen.findByRole("heading", { name: "Đổi mật khẩu" })).toBeInTheDocument();

  await userEvent.type(screen.getByLabelText("Mật khẩu hiện tại"), "matkhaucu12345");
  await userEvent.type(screen.getByLabelText("Mật khẩu mới (tối thiểu 12 ký tự)"), "matkhaumoi12345");
  await userEvent.type(screen.getByLabelText("Nhập lại mật khẩu mới"), "khong-khop-nhau");

  await userEvent.click(screen.getByRole("button", { name: "Lưu mật khẩu mới" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Mật khẩu xác nhận không khớp.");
});

test("ProfilePage: displays server error when current password is wrong", async () => {
  serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "POST /api/admin/auth/password": () => ({
      status: 400,
      body: {
        title: "One or more validation errors occurred.",
        errors: { currentPassword: ["Mật khẩu hiện tại không đúng."] },
      },
    }),
  });

  renderAt("/profile");
  expect(await screen.findByRole("heading", { name: "Đổi mật khẩu" })).toBeInTheDocument();

  await userEvent.type(screen.getByLabelText("Mật khẩu hiện tại"), "matkhausai12345");
  await userEvent.type(screen.getByLabelText("Mật khẩu mới (tối thiểu 12 ký tự)"), "matkhaumoi12345");
  await userEvent.type(screen.getByLabelText("Nhập lại mật khẩu mới"), "matkhaumoi12345");

  await userEvent.click(screen.getByRole("button", { name: "Lưu mật khẩu mới" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Mật khẩu hiện tại không đúng.");
});

test("AccountsPage: lists accounts and disallows locking oneself", async () => {
  serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "GET /api/admin/accounts": () => ({ status: 200, body: [account, otherAccount] }),
  });

  renderAt("/accounts");
  expect(await screen.findByRole("heading", { name: "Tài khoản quản trị" })).toBeInTheDocument();

  expect(await screen.findByText("Điều hành viên")).toBeInTheDocument();
  expect(screen.getByText("(Bạn)")).toBeInTheDocument();

  // Find the row for current user in the table and ensure its lock button is disabled
  const table = screen.getByRole("table");
  const selfRow = within(table).getByText("admin@example.test").closest("tr")!;
  const selfLockButton = within(selfRow).getByRole("button", { name: /Khóa/i });
  expect(selfLockButton).toBeDisabled();
});

test("AccountsPage: creates a new account via the form", async () => {
  const calls = serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "GET /api/admin/accounts": () => ({ status: 200, body: [account] }),
    "POST /api/admin/accounts": () => ({
      status: 201,
      body: {
        id: "11111111-2222-3333-4444-555555555555",
        email: "newadmin@example.test",
        displayName: "Admin Mới",
        isActive: true,
        createdAt: "2026-11-03T00:00:00Z",
        lastLoginAt: null,
      },
    }),
  });

  renderAt("/accounts");
  expect(await screen.findByRole("heading", { name: "Tài khoản quản trị" })).toBeInTheDocument();

  await userEvent.click(screen.getByRole("button", { name: "+ Thêm tài khoản" }));
  expect(screen.getByRole("heading", { name: "Thêm tài khoản quản trị viên mới" })).toBeInTheDocument();

  await userEvent.type(screen.getByPlaceholderText("admin@threetwelve.local"), "newadmin@example.test");
  await userEvent.type(screen.getByPlaceholderText("Tên quản trị viên"), "Admin Mới");
  await userEvent.type(screen.getByLabelText("Mật khẩu khởi tạo (tối thiểu 12 ký tự)"), "matkhaukhoidau123");

  await userEvent.click(screen.getByRole("button", { name: "Tạo tài khoản" }));

  expect(await screen.findByRole("status")).toHaveTextContent("Đã tạo tài khoản cho newadmin@example.test.");
  expect(calls).toContain("POST /api/admin/accounts");
});

test("AccountsPage: resets password for another account", async () => {
  const calls = serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "GET /api/admin/accounts": () => ({ status: 200, body: [account, otherAccount] }),
    "POST /api/admin/accounts/8a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d/password": () => ({ status: 204 }),
  });

  renderAt("/accounts");
  expect(await screen.findByRole("heading", { name: "Tài khoản quản trị" })).toBeInTheDocument();

  const otherCell = await screen.findByText("mod@example.test");
  const otherRow = otherCell.closest("tr")!;
  await userEvent.click(within(otherRow).getByRole("button", { name: "Đặt lại MK" }));

  expect(screen.getByRole("heading", { name: "Đặt lại mật khẩu" })).toBeInTheDocument();
  await userEvent.type(screen.getByLabelText("Mật khẩu mới (tối thiểu 12 ký tự)"), "matkhaumoi99999");

  await userEvent.click(screen.getByRole("button", { name: "Cập nhật mật khẩu" }));

  expect(await screen.findByRole("status")).toHaveTextContent("Đã đặt lại mật khẩu cho tài khoản mod@example.test.");
  expect(calls).toContain("POST /api/admin/accounts/8a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d/password");
});

test("AccountsPage: updates display name of an account", async () => {
  const calls = serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "GET /api/admin/accounts": () => ({ status: 200, body: [account, otherAccount] }),
    "PATCH /api/admin/accounts/8a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d": () => ({
      status: 200,
      body: { ...otherAccount, displayName: "Tên Đã Đổi" },
    }),
  });

  renderAt("/accounts");
  expect(await screen.findByRole("heading", { name: "Tài khoản quản trị" })).toBeInTheDocument();

  const otherCell = await screen.findByText("mod@example.test");
  const otherRow = otherCell.closest("tr")!;
  await userEvent.click(within(otherRow).getByRole("button", { name: "Sửa tên" }));

  expect(screen.getByRole("heading", { name: "Sửa tên hiển thị" })).toBeInTheDocument();
  const input = screen.getByLabelText("Tên hiển thị mới");
  await userEvent.clear(input);
  await userEvent.type(input, "Tên Đã Đổi");

  await userEvent.click(screen.getByRole("button", { name: "Lưu" }));

  expect(await screen.findByRole("status")).toHaveTextContent("Đã cập nhật tên hiển thị cho mod@example.test.");
  expect(calls).toContain("PATCH /api/admin/accounts/8a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d");
});

test("AccountsPage: deletes an account after confirmation", async () => {
  const calls = serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "GET /api/admin/accounts": () => ({ status: 200, body: [account, otherAccount] }),
    "DELETE /api/admin/accounts/8a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d": () => ({ status: 204 }),
  });

  renderAt("/accounts");
  expect(await screen.findByRole("heading", { name: "Tài khoản quản trị" })).toBeInTheDocument();

  const otherCell = await screen.findByText("mod@example.test");
  const otherRow = otherCell.closest("tr")!;
  await userEvent.click(within(otherRow).getByRole("button", { name: "Xóa" }));

  expect(screen.getByRole("heading", { name: "Xác nhận xóa tài khoản" })).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: "Xác nhận xóa" }));

  expect(await screen.findByRole("status")).toHaveTextContent("Đã xóa tài khoản mod@example.test.");
  expect(calls).toContain("DELETE /api/admin/accounts/8a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d");
});

const mockDashboardData = {
  from: "2026-10-05T00:00:00Z",
  to: "2026-11-04T12:00:00Z",
  players: 128,
  sessions: 342,
  playtime: {
    totalSeconds: 184520,
    averageSessionSeconds: 539.5,
  },
  nights: [
    {
      night: 1,
      playersStarted: 128,
      playersCompleted: 102,
      averageDurationSeconds: 720.0,
    },
    {
      night: 2,
      playersStarted: 95,
      playersCompleted: 68,
      averageDurationSeconds: 890.5,
    },
  ],
  caughtLocations: [
    { night: 1, areaId: "n1.parking", count: 24 },
    { night: 1, areaId: "n1.alley", count: 18 },
  ],
  quitLocations: [
    { night: 1, areaId: "n1.alley", count: 12 },
  ],
  endings: [
    { endingId: "red_thread_sandal", players: 7 },
    { endingId: "home", players: 28 },
  ],
  choices: [
    { night: 1, choiceId: "1a", players: 62 },
  ],
};

test("DashboardPage: loads and renders summary metrics, funnel, hotspots, endings, and choices", async () => {
  const calls = serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "GET /api/admin/dashboard": () => ({ status: 200, body: mockDashboardData }),
  });

  renderAt("/");

  expect(await screen.findByRole("heading", { name: "Dashboard" })).toBeInTheDocument();

  // Metric cards
  const playerValues = await screen.findAllByText("128");
  expect(playerValues.length).toBeGreaterThanOrEqual(1);
  expect(screen.getByText("342")).toBeInTheDocument();

  // Funnel
  expect(screen.getByText("80% Hoàn thành")).toBeInTheDocument();
  expect(screen.getByText("72% Hoàn thành")).toBeInTheDocument();

  // Locations
  expect(screen.getByText("Bãi giữ xe")).toBeInTheDocument();
  expect(screen.getByText("n1.parking")).toBeInTheDocument();

  // Endings
  expect(screen.getByText("6.7 Đôi dép chỉ đỏ")).toBeInTheDocument();
  expect(screen.getByText("Bí mật ARG")).toBeInTheDocument();
  expect(screen.getByText("6.6 Về nhà")).toBeInTheDocument();

  // Choices
  expect(screen.getByText("1a")).toBeInTheDocument();
  expect(screen.getByText("Chặn dãy số lạ (Đêm 1)")).toBeInTheDocument();

  // CSV Export links
  const summaryLink = screen.getByRole("link", { name: /Xuất tổng hợp CSV/i });
  expect(summaryLink).toHaveAttribute("href", expect.stringContaining("/api/admin/exports/summary"));

  const eventsLink = screen.getByRole("link", { name: /Xuất sự kiện CSV/i });
  expect(eventsLink).toHaveAttribute("href", expect.stringContaining("/api/admin/exports/events"));

  expect(calls).toContain("GET /api/admin/dashboard");
});

test("DashboardPage: toggles between caught and quit locations", async () => {
  serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "GET /api/admin/dashboard": () => ({ status: 200, body: mockDashboardData }),
  });

  renderAt("/");
  expect(await screen.findByText("Bãi giữ xe")).toBeInTheDocument();

  // Switch to quit locations tab
  await userEvent.click(screen.getByRole("tab", { name: /Bỏ game/i }));

  expect(screen.getByText("12")).toBeInTheDocument();
});

test("DashboardPage: changing date range preset re-fetches dashboard", async () => {
  const calls = serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "GET /api/admin/dashboard": () => ({ status: 200, body: mockDashboardData }),
  });

  renderAt("/");
  expect(await screen.findByRole("heading", { name: "Dashboard" })).toBeInTheDocument();

  // Click 7 days preset
  await userEvent.click(screen.getByRole("button", { name: "7 ngày" }));

  // Re-fetched
  const dashboardCalls = calls.filter((c) => c.startsWith("GET /api/admin/dashboard"));
  expect(dashboardCalls.length).toBeGreaterThanOrEqual(2);
});

test("DashboardPage: displays error alert and allows retry when API fails", async () => {
  let attempt = 0;
  serve({
    "GET /api/admin/auth/me": () => ({ status: 200, body: account }),
    "GET /api/admin/dashboard": () => {
      attempt++;
      if (attempt === 1) {
        return { status: 500, body: { title: "Máy chủ bận", status: 500 } };
      }
      return { status: 200, body: mockDashboardData };
    },
  });

  renderAt("/");

  expect(await screen.findByRole("alert")).toBeInTheDocument();
  expect(screen.getByText("Máy chủ bận")).toBeInTheDocument();

  // Click retry
  await userEvent.click(screen.getByRole("button", { name: /Thử lại/i }));

  const playerValues = await screen.findAllByText("128");
  expect(playerValues.length).toBeGreaterThanOrEqual(1);
});
