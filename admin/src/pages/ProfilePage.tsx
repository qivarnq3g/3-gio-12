import { useState, type FormEvent } from "react";
import { api } from "../api/client";
import { getErrorMessage } from "../api/error";
import { useAuth } from "../auth/AuthProvider";

export function ProfilePage() {
  const { account } = useAuth();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
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
      <h1>Đổi mật khẩu</h1>
      {account && (
        <p className="status" style={{ marginBottom: "1.5rem" }}>
          Tài khoản: <strong>{account.displayName}</strong> ({account.email})
        </p>
      )}

      <form onSubmit={handleSubmit} className="form-card" style={{ maxWidth: 460 }}>
        {error && (
          <div role="alert" className="alert-error">
            {error}
          </div>
        )}
        {success && (
          <div role="status" className="alert-success">
            {success}
          </div>
        )}

        <label>
          Mật khẩu hiện tại
          <input
            type="password"
            autoComplete="current-password"
            required
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
          />
        </label>

        <label>
          Mật khẩu mới (tối thiểu 12 ký tự)
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={12}
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
          />
        </label>

        <label>
          Nhập lại mật khẩu mới
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={12}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
          />
        </label>

        <div style={{ marginTop: "1rem" }}>
          <button type="submit" disabled={busy}>
            {busy ? "Đang xử lý..." : "Lưu mật khẩu mới"}
          </button>
        </div>
      </form>
    </section>
  );
}
