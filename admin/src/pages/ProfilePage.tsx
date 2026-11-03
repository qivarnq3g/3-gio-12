import { useState, type FormEvent } from "react";
import { api } from "../api/client";
import { getErrorMessage } from "../api/error";
import { useAuth } from "../auth/AuthProvider";
import {
  AlertCircleIcon,
  CheckCircleIcon,
  EyeIcon,
  EyeOffIcon,
  KeyIcon,
  LockIcon,
  ShieldCheckIcon,
} from "../components/Icons";

export function ProfilePage() {
  const { account } = useAuth();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    if (newPassword.length < 12) {
      setError("Mật khẩu mới phải có tối thiểu 12 ký tự.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setError("Mật khẩu xác nhận không khớp.");
      return;
    }

    setBusy(true);
    try {
      const { response, error: apiError } = await api.POST("/api/admin/auth/password", {
        body: { currentPassword, newPassword },
      });

      if (response.ok) {
        setSuccess("Đã đổi mật khẩu thành công.");
        setCurrentPassword("");
        setNewPassword("");
        setConfirmPassword("");
      } else {
        setError(getErrorMessage(apiError, "Không thể đổi mật khẩu. Kiểm tra lại thông tin."));
      }
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="profile-page">
      <div className="page-header-row">
        <div>
          <h1>Đổi mật khẩu</h1>
          <p className="status" style={{ margin: "0.25rem 0 0" }}>
            Tài khoản: <strong>{account?.displayName}</strong> (<code>{account?.email}</code>)
          </p>
        </div>
      </div>

      <div className="profile-grid">
        <form onSubmit={handleSubmit} className="form-card profile-card">
          <div className="card-title-row">
            <h2>Thiết lập mật khẩu mới</h2>
            <span className="card-subtitle">Mật khẩu mới cần tối thiểu 12 ký tự</span>
          </div>

          {error && (
            <div role="alert" className="alert-error">
              <AlertCircleIcon />
              <span>{error}</span>
            </div>
          )}
          {success && (
            <div role="status" className="alert-success">
              <CheckCircleIcon />
              <span>{success}</span>
            </div>
          )}

          <div className="login-fields">
            <div className="login-field-group">
              <label htmlFor="current-pwd" className="login-label">
                Mật khẩu hiện tại
              </label>
              <div className="input-with-icon">
                <span className="input-icon" aria-hidden="true">
                  <LockIcon />
                </span>
                <input
                  id="current-pwd"
                  type={showCurrent ? "text" : "password"}
                  autoComplete="current-password"
                  required
                  placeholder="••••••••••••"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  className="login-input has-action"
                />
                <button
                  type="button"
                  tabIndex={-1}
                  className="password-toggle-btn"
                  onClick={() => setShowCurrent((p) => !p)}
                  title={showCurrent ? "Ẩn" : "Hiện"}
                  aria-label={showCurrent ? "Ẩn mật khẩu hiện tại" : "Hiện mật khẩu hiện tại"}
                >
                  {showCurrent ? <EyeOffIcon /> : <EyeIcon />}
                </button>
              </div>
            </div>

            <div className="login-field-group">
              <label htmlFor="new-pwd" className="login-label">
                Mật khẩu mới (tối thiểu 12 ký tự)
              </label>
              <div className="input-with-icon">
                <span className="input-icon" aria-hidden="true">
                  <KeyIcon />
                </span>
                <input
                  id="new-pwd"
                  type={showNew ? "text" : "password"}
                  autoComplete="new-password"
                  required
                  minLength={12}
                  placeholder="••••••••••••"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="login-input has-action"
                />
                <button
                  type="button"
                  tabIndex={-1}
                  className="password-toggle-btn"
                  onClick={() => setShowNew((p) => !p)}
                  title={showNew ? "Ẩn" : "Hiện"}
                  aria-label={showNew ? "Ẩn mật khẩu mới" : "Hiện mật khẩu mới"}
                >
                  {showNew ? <EyeOffIcon /> : <EyeIcon />}
                </button>
              </div>
            </div>

            <div className="login-field-group">
              <label htmlFor="confirm-pwd" className="login-label">
                Nhập lại mật khẩu mới
              </label>
              <div className="input-with-icon">
                <span className="input-icon" aria-hidden="true">
                  <KeyIcon />
                </span>
                <input
                  id="confirm-pwd"
                  type="password"
                  autoComplete="new-password"
                  required
                  minLength={12}
                  placeholder="••••••••••••"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  className="login-input"
                />
              </div>
            </div>
          </div>

          <div style={{ marginTop: "1.25rem" }}>
            <button type="submit" disabled={busy} className="save-pwd-btn">
              {busy ? (
                <>
                  <span className="btn-spinner" aria-hidden="true"></span>
                  <span>Đang xử lý...</span>
                </>
              ) : (
                <span>Lưu mật khẩu mới</span>
              )}
            </button>
          </div>
        </form>

        {/* Security recommendation box */}
        <aside className="security-guide-card">
          <div className="guide-title">
            <ShieldCheckIcon />
            <span>Quy tắc bảo mật tài khoản</span>
          </div>
          <ul className="guide-list">
            <li>Mật khẩu bắt buộc có <strong>tối thiểu 12 ký tự</strong>.</li>
            <li>Nên kết hợp chữ cái, số và ký tự đặc biệt để tăng độ an toàn.</li>
            <li>Đổi mật khẩu sẽ tự động đăng xuất các phiên đăng nhập khác của bạn trên các thiết bị.</li>
            <li>Phiên làm việc hiện tại trên trình duyệt này vẫn được duy trì an toàn.</li>
          </ul>
        </aside>
      </div>
    </section>
  );
}
