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
        <div className="brand-header">
          <div className="brand-badge">3:12</div>
          <div className="brand-info">
            <span className="brand-title">3 GIỜ 12</span>
            <span className="brand-sub">Quản trị hệ thống</span>
          </div>
        </div>

        <nav>
          {links.map((link) => (
            <NavLink key={link.to} to={link.to} end={link.end} className={({ isActive }) => (isActive ? "active" : "")}>
              <span className="nav-indicator" aria-hidden="true"></span>
              <span className="nav-text">{link.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="who">
          <div className="user-profile-box">
            <div className="user-avatar" aria-hidden="true">
              {account?.displayName?.charAt(0).toUpperCase() || "A"}
            </div>
            <div className="user-meta">
              <span className="user-name">{account?.displayName}</span>
              <span className="user-email">{account?.email}</span>
            </div>
          </div>
          <button type="button" className="logout-btn" onClick={() => void logout()}>
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
