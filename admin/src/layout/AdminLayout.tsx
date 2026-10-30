import { NavLink, Outlet } from "react-router";
import { useAuth } from "../auth/AuthProvider";

const links = [
  { to: "/", label: "Dashboard", end: true },
  { to: "/puzzles", label: "Câu đố", end: false },
  { to: "/accounts", label: "Tài khoản", end: false },
  { to: "/profile", label: "Đổi mật khẩu", end: false },
];

export function AdminLayout() {
  const { account, logout } = useAuth();

  return (
    <div className="shell">
      <aside className="sidebar">
        <p className="brand">3 GIỜ 12</p>
        <nav>
          {links.map((link) => (
            <NavLink key={link.to} to={link.to} end={link.end}>
              {link.label}
            </NavLink>
          ))}
        </nav>
        <div className="who">
          <span>{account?.displayName}</span>
          <button type="button" onClick={() => void logout()}>
            Đăng xuất
          </button>
        </div>
      </aside>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}
