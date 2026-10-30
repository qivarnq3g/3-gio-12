import { render, screen } from "@testing-library/react";
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

type Reply = { status: number; body?: unknown };

function serve(routes: Record<string, () => Reply>) {
  const calls: string[] = [];
  vi.stubGlobal("fetch", async (input: Request) => {
    const key = `${input.method} ${new URL(input.url).pathname}`;
    calls.push(key);
    const reply = routes[key]?.() ?? { status: 404 };
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
  });
  renderAt("/accounts");

  await fillLogin("mat-khau-thu-dai-12");

  expect(await screen.findByRole("heading", { name: "Tài khoản quản trị" })).toBeInTheDocument();
  expect(screen.getByText("Quản trị viên")).toBeInTheDocument();
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
