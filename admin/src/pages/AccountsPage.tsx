import { useCallback, useEffect, useState, type FormEvent } from "react";
import { api, type AdminAccount } from "../api/client";
import { getErrorMessage } from "../api/error";
import { useAuth } from "../auth/AuthProvider";
import {
  AlertCircleIcon,
  CheckCircleIcon,
  EditIcon,
  KeyIcon,
  LockIcon,
  TrashIcon,
  UserPlusIcon,
} from "../components/Icons";

function formatDate(iso: string | null): string {
  if (!iso) return "Chưa đăng nhập";
  try {
    const d = new Date(iso);
    return d.toLocaleString("vi-VN", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

export function AccountsPage() {
  const { account: currentAdmin } = useAuth();
  const [accounts, setAccounts] = useState<AdminAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Form thêm tài khoản
  const [showAddForm, setShowAddForm] = useState(false);
  const [newEmail, setNewEmail] = useState("");
  const [newDisplayName, setNewDisplayName] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [addBusy, setAddBusy] = useState(false);

  // Modal / trạng thái Sửa tên hiển thị
  const [editingAccount, setEditingAccount] = useState<AdminAccount | null>(null);
  const [editDisplayName, setEditDisplayName] = useState("");
  const [editBusy, setEditBusy] = useState(false);

  // Modal / trạng thái Đặt lại mật khẩu
  const [resetAccount, setResetAccount] = useState<AdminAccount | null>(null);
  const [resetNewPassword, setResetNewPassword] = useState("");
  const [resetBusy, setResetBusy] = useState(false);

  // Xóa tài khoản
  const [deletingAccount, setDeletingAccount] = useState<AdminAccount | null>(null);
  const [deleteBusy, setDeleteBusy] = useState(false);

  const fetchAccounts = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const { data, response, error: apiError } = await api.GET("/api/admin/accounts");
      if (response.ok && data) {
        setAccounts(data);
      } else {
        setError(getErrorMessage(apiError, "Không tải được danh sách tài khoản."));
      }
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchAccounts();
  }, [fetchAccounts]);

  async function handleCreateAccount(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    if (newPassword.length < 12) {
      setError("Mật khẩu khởi tạo phải có tối thiểu 12 ký tự.");
      return;
    }

    setAddBusy(true);
    try {
      const { response, error: apiError } = await api.POST("/api/admin/accounts", {
        body: {
          email: newEmail.trim(),
          displayName: newDisplayName.trim(),
          password: newPassword,
        },
      });

      if (response.ok) {
        setSuccess(`Đã tạo tài khoản cho ${newEmail.trim()}.`);
        setNewEmail("");
        setNewDisplayName("");
        setNewPassword("");
        setShowAddForm(false);
        await fetchAccounts();
      } else {
        setError(getErrorMessage(apiError, "Không thể tạo tài khoản. Kiểm tra lại thông tin."));
      }
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setAddBusy(false);
    }
  }

  async function handleToggleActive(target: AdminAccount) {
    if (target.id === currentAdmin?.id) {
      setError("Không tự khóa tài khoản của mình.");
      return;
    }

    setError(null);
    setSuccess(null);
    const newStatus = !target.isActive;
    const actionLabel = newStatus ? "mở khóa" : "khóa";

    try {
      const { response, error: apiError } = await api.PATCH("/api/admin/accounts/{accountId}", {
        params: { path: { accountId: target.id } },
        body: { isActive: newStatus },
      });

      if (response.ok) {
        setSuccess(`Đã ${actionLabel} tài khoản ${target.email}.`);
        await fetchAccounts();
      } else {
        setError(getErrorMessage(apiError, `Không thể ${actionLabel} tài khoản.`));
      }
    } catch {
      setError("Không kết nối được tới máy chủ.");
    }
  }

  async function handleUpdateDisplayName(event: FormEvent) {
    event.preventDefault();
    if (!editingAccount) return;

    setError(null);
    setSuccess(null);
    setEditBusy(true);

    try {
      const { response, error: apiError } = await api.PATCH("/api/admin/accounts/{accountId}", {
        params: { path: { accountId: editingAccount.id } },
        body: { displayName: editDisplayName.trim() },
      });

      if (response.ok) {
        setSuccess(`Đã cập nhật tên hiển thị cho ${editingAccount.email}.`);
        setEditingAccount(null);
        await fetchAccounts();
      } else {
        setError(getErrorMessage(apiError, "Không thể cập nhật tên hiển thị."));
      }
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setEditBusy(false);
    }
  }

  async function handleResetPassword(event: FormEvent) {
    event.preventDefault();
    if (!resetAccount) return;

    setError(null);
    setSuccess(null);

    if (resetNewPassword.length < 12) {
      setError("Mật khẩu mới phải có tối thiểu 12 ký tự.");
      return;
    }

    setResetBusy(true);
    try {
      const { response, error: apiError } = await api.POST("/api/admin/accounts/{accountId}/password", {
        params: { path: { accountId: resetAccount.id } },
        body: { newPassword: resetNewPassword },
      });

      if (response.ok) {
        setSuccess(`Đã đặt lại mật khẩu cho tài khoản ${resetAccount.email}.`);
        setResetAccount(null);
        setResetNewPassword("");
      } else {
        setError(getErrorMessage(apiError, "Không thể đặt lại mật khẩu."));
      }
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setResetBusy(false);
    }
  }

  async function handleDeleteAccount() {
    if (!deletingAccount) return;

    if (deletingAccount.id === currentAdmin?.id) {
      setError("Không tự xóa tài khoản của mình.");
      setDeletingAccount(null);
      return;
    }

    setError(null);
    setSuccess(null);
    setDeleteBusy(true);

    try {
      const { response, error: apiError } = await api.DELETE("/api/admin/accounts/{accountId}", {
        params: { path: { accountId: deletingAccount.id } },
      });

      if (response.ok) {
        setSuccess(`Đã xóa tài khoản ${deletingAccount.email}.`);
        setDeletingAccount(null);
        await fetchAccounts();
      } else {
        setError(getErrorMessage(apiError, "Không thể xóa tài khoản."));
      }
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setDeleteBusy(false);
    }
  }

  const activeCount = accounts.filter((a) => a.isActive).length;
  const inactiveCount = accounts.length - activeCount;

  return (
    <section className="accounts-page">
      {/* Top Header & Metrics */}
      <div className="page-header-row">
        <div>
          <h1>Tài khoản quản trị</h1>
          <div className="stats-pills">
            <span className="stats-pill">
              Tổng số: <strong>{accounts.length}</strong>
            </span>
            <span className="stats-pill pill-active">
              Hoạt động: <strong>{activeCount}</strong>
            </span>
            {inactiveCount > 0 && (
              <span className="stats-pill pill-inactive">
                Đã khóa: <strong>{inactiveCount}</strong>
              </span>
            )}
          </div>
        </div>

        <button
          type="button"
          className="add-account-btn"
          onClick={() => {
            setShowAddForm((prev) => !prev);
            setError(null);
          }}
        >
          <UserPlusIcon />
          <span>{showAddForm ? "Đóng form" : "+ Thêm tài khoản"}</span>
        </button>
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

      {/* Form tạo tài khoản mới */}
      {showAddForm && (
        <form onSubmit={handleCreateAccount} className="form-card account-create-card">
          <div className="card-title-row">
            <h2>Thêm tài khoản quản trị viên mới</h2>
            <span className="card-subtitle">Điền thông tin tài khoản và mật khẩu khởi tạo (tối thiểu 12 ký tự)</span>
          </div>
          <div className="form-grid">
            <label>
              Email
              <input
                type="email"
                required
                value={newEmail}
                onChange={(e) => setNewEmail(e.target.value)}
                placeholder="admin@threetwelve.local"
              />
            </label>
            <label>
              Tên hiển thị
              <input
                type="text"
                required
                value={newDisplayName}
                onChange={(e) => setNewDisplayName(e.target.value)}
                placeholder="Tên quản trị viên"
              />
            </label>
            <label>
              Mật khẩu khởi tạo (tối thiểu 12 ký tự)
              <input
                type="password"
                required
                minLength={12}
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="••••••••••••"
              />
            </label>
          </div>
          <div style={{ display: "flex", gap: "0.5rem", marginTop: "1.25rem" }}>
            <button type="submit" disabled={addBusy}>
              {addBusy ? "Đang tạo..." : "Tạo tài khoản"}
            </button>
            <button type="button" className="secondary" onClick={() => setShowAddForm(false)}>
              Hủy
            </button>
          </div>
        </form>
      )}

      {/* Bảng danh sách tài khoản */}
      {loading ? (
        <div className="loading-state">
          <span className="btn-spinner" aria-hidden="true"></span>
          <p className="status">Đang tải danh sách tài khoản...</p>
        </div>
      ) : accounts.length === 0 ? (
        <p className="status">Chưa có tài khoản nào.</p>
      ) : (
        <div className="table-wrapper">
          <table className="accounts-table">
            <thead>
              <tr>
                <th>Tên hiển thị</th>
                <th>Email</th>
                <th>Trạng thái</th>
                <th>Đăng nhập gần nhất</th>
                <th>Ngày tạo</th>
                <th style={{ textAlign: "right" }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {accounts.map((acc) => {
                const isMe = acc.id === currentAdmin?.id;
                return (
                  <tr key={acc.id} className={isMe ? "row-self" : ""}>
                    <td>
                      <div className="user-cell">
                        <div className="avatar-sm" aria-hidden="true">
                          {acc.displayName.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <strong>{acc.displayName}</strong>
                          {isMe && <span className="tag-self"> (Bạn)</span>}
                        </div>
                      </div>
                    </td>
                    <td className="font-mono text-muted">{acc.email}</td>
                    <td>
                      <span className={`badge ${acc.isActive ? "badge-active" : "badge-inactive"}`}>
                        <span className="badge-dot" aria-hidden="true"></span>
                        {acc.isActive ? "Hoạt động" : "Đã khóa"}
                      </span>
                    </td>
                    <td className="text-muted">{formatDate(acc.lastLoginAt ?? null)}</td>
                    <td className="text-muted">{formatDate(acc.createdAt)}</td>
                    <td>
                      <div className="actions-cell" style={{ justifyContent: "flex-end" }}>
                        <button
                          type="button"
                          className="sm secondary action-icon-btn"
                          onClick={() => {
                            setEditingAccount(acc);
                            setEditDisplayName(acc.displayName);
                            setError(null);
                          }}
                          title="Sửa tên hiển thị"
                        >
                          <EditIcon />
                          <span>Sửa tên</span>
                        </button>

                        <button
                          type="button"
                          className={`sm secondary action-icon-btn ${acc.isActive ? "btn-lock" : "btn-unlock"}`}
                          disabled={isMe}
                          title={isMe ? "Không tự khóa tài khoản của chính mình" : acc.isActive ? "Khóa tài khoản" : "Mở khóa tài khoản"}
                          onClick={() => void handleToggleActive(acc)}
                        >
                          <LockIcon />
                          <span>{acc.isActive ? "Khóa" : "Mở khóa"}</span>
                        </button>

                        {!isMe && (
                          <button
                            type="button"
                            className="sm secondary action-icon-btn"
                            onClick={() => {
                              setResetAccount(acc);
                              setResetNewPassword("");
                              setError(null);
                            }}
                            title="Đặt lại mật khẩu cho tài khoản này"
                          >
                            <KeyIcon />
                            <span>Đặt lại MK</span>
                          </button>
                        )}

                        {!isMe && (
                          <button
                            type="button"
                            className="sm danger action-icon-btn"
                            onClick={() => {
                              setDeletingAccount(acc);
                              setError(null);
                            }}
                            title="Xóa tài khoản khỏi hệ thống"
                          >
                            <TrashIcon />
                            <span>Xóa</span>
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Modal / Dialog: Sửa tên hiển thị */}
      {editingAccount && (
        <div className="modal-backdrop">
          <div className="modal-content">
            <h3>Sửa tên hiển thị</h3>
            <p className="status" style={{ margin: "0.25rem 0 1rem" }}>
              Tài khoản: <code>{editingAccount.email}</code>
            </p>
            <form onSubmit={handleUpdateDisplayName}>
              <label>
                Tên hiển thị mới
                <input
                  type="text"
                  required
                  value={editDisplayName}
                  onChange={(e) => setEditDisplayName(e.target.value)}
                  autoFocus
                />
              </label>
              <div style={{ display: "flex", gap: "0.5rem", marginTop: "1rem", justifyContent: "flex-end" }}>
                <button type="button" className="secondary" onClick={() => setEditingAccount(null)}>
                  Hủy
                </button>
                <button type="submit" disabled={editBusy}>
                  {editBusy ? "Đang lưu..." : "Lưu"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal / Dialog: Đặt lại mật khẩu cho tài khoản khác */}
      {resetAccount && (
        <div className="modal-backdrop">
          <div className="modal-content">
            <h3>Đặt lại mật khẩu</h3>
            <p className="status" style={{ margin: "0.25rem 0 1rem" }}>
              Đặt lại mật khẩu cho: <strong>{resetAccount.displayName}</strong> ({resetAccount.email})
            </p>
            <form onSubmit={handleResetPassword}>
              <label>
                Mật khẩu mới (tối thiểu 12 ký tự)
                <input
                  type="password"
                  required
                  minLength={12}
                  value={resetNewPassword}
                  onChange={(e) => setResetNewPassword(e.target.value)}
                  placeholder="••••••••••••"
                  autoFocus
                />
              </label>
              <div style={{ display: "flex", gap: "0.5rem", marginTop: "1rem", justifyContent: "flex-end" }}>
                <button type="button" className="secondary" onClick={() => setResetAccount(null)}>
                  Hủy
                </button>
                <button type="submit" disabled={resetBusy}>
                  {resetBusy ? "Đang lưu..." : "Cập nhật mật khẩu"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal / Dialog xác nhận xóa tài khoản */}
      {deletingAccount && (
        <div className="modal-backdrop">
          <div className="modal-content">
            <h3 style={{ color: "var(--error)" }}>Xác nhận xóa tài khoản</h3>
            <p style={{ margin: "0.5rem 0 1rem", lineHeight: 1.5 }}>
              Bạn có chắc chắn muốn xóa tài khoản <strong>{deletingAccount.displayName}</strong> (
              <code>{deletingAccount.email}</code>) không? Hành động này không thể hoàn tác.
            </p>
            <div style={{ display: "flex", gap: "0.5rem", justifyContent: "flex-end" }}>
              <button type="button" className="secondary" onClick={() => setDeletingAccount(null)}>
                Hủy
              </button>
              <button type="button" className="danger" disabled={deleteBusy} onClick={() => void handleDeleteAccount()}>
                {deleteBusy ? "Đang xóa..." : "Xác nhận xóa"}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
