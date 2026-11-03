import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router";
import { useAuth } from "../auth/AuthProvider";

export function LoginPage() {
  const { account, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const from = (location.state as { from?: string } | null)?.from ?? "/";
  if (account) return <Navigate to={from} replace />;

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    const result = await login(email.trim(), password);
    setBusy(false);
    if (result.ok) navigate(from, { replace: true });
    else setError(result.message);
  }

  return (
    <main className="login">
      <div className="login-card-wrapper">
        <form onSubmit={submit} className="login-form">
          {/* Header & Brand */}
          <div className="login-header">
            <div className="login-logo-badge">
              <span className="logo-time">3:12</span>
            </div>
            <h1 className="login-title">Đăng nhập quản trị</h1>
            <p className="login-subtitle">
              Cổng quản trị nội dung & dữ liệu trò chơi <strong>3 GIỜ 12</strong>
            </p>
          </div>

          {/* Error Banner */}
          {error && (
            <div role="alert" className="alert-error login-error-box">
              <span className="error-icon" aria-hidden="true">⚠️</span>
              <span className="error-text">{error}</span>
            </div>
          )}

          {/* Form Fields */}
          <div className="login-fields">
            <div className="login-field-group">
              <label htmlFor="login-email" className="login-label">
                Email
              </label>
              <div className="input-with-icon">
                <span className="input-icon" aria-hidden="true">✉️</span>
                <input
                  id="login-email"
                  type="email"
                  autoComplete="username"
                  required
                  placeholder="admin@threetwelve.local"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="login-input"
                />
              </div>
            </div>

            <div className="login-field-group">
              <label htmlFor="login-password" className="login-label">
                Mật khẩu
              </label>
              <div className="input-with-icon">
                <span className="input-icon" aria-hidden="true">🔒</span>
                <input
                  id="login-password"
                  type={showPassword ? "text" : "password"}
                  autoComplete="current-password"
                  required
                  placeholder="••••••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="login-input has-action"
                />
                <button
                  type="button"
                  tabIndex={-1}
                  className="password-toggle-btn"
                  onClick={() => setShowPassword((prev) => !prev)}
                  title={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                >
                  {showPassword ? "Ẩn" : "Hiện"}
                </button>
              </div>
            </div>
          </div>

          {/* Submit Button */}
          <button type="submit" disabled={busy} className="login-submit-btn">
            {busy ? (
              <>
                <span className="btn-spinner" aria-hidden="true"></span>
                <span>Đang đăng nhập...</span>
              </>
            ) : (
              <span>Đăng nhập</span>
            )}
          </button>

          {/* Security footnote */}
          <div className="login-footer">
            <span className="security-icon" aria-hidden="true">🛡️</span>
            <span>Phiên bảo mật 8 giờ · Cookie SameSite Strict</span>
          </div>
        </form>
      </div>
    </main>
  );
}
