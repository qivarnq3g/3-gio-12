import { useCallback, useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import { Link, useParams } from "react-router";
import {
  api,
  type PuzzleConfig,
  type PuzzleSolution,
  type PuzzleSubmissionPage,
  type SubmissionResult,
} from "../api/client";
import { getErrorMessage } from "../api/error";
import { AlertCircleIcon, CheckCircleIcon, TrashIcon } from "../components/Icons";

const PAGE_SIZE = 50;
const SHA256 = /^[0-9a-f]{64}$/;
const RESULT_LABEL: Record<SubmissionResult, string> = {
  accepted: "Đúng",
  rejected: "Sai",
  blocked: "Bị chặn",
};

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString("vi-VN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

function shortHash(sha: string): string {
  return `${sha.slice(0, 12)}…${sha.slice(-6)}`;
}

async function sha256OfFile(file: File): Promise<string> {
  const digest = await crypto.subtle.digest("SHA-256", await file.arrayBuffer());
  return Array.from(new Uint8Array(digest), (b) => b.toString(16).padStart(2, "0")).join("");
}

function Alerts({ error, success }: { error: string | null; success: string | null }) {
  return (
    <>
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
    </>
  );
}

function EnabledBadge({ enabled }: { enabled: boolean }) {
  return (
    <span className={`badge ${enabled ? "badge-active" : "badge-inactive"}`}>
      <span className="badge-dot" />
      {enabled ? "Đang bật" : "Đang tắt"}
    </span>
  );
}

export function PuzzlesPage() {
  const [puzzles, setPuzzles] = useState<PuzzleConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        const { data, response, error: apiError } = await api.GET("/api/admin/puzzles");
        if (response.ok && data) setPuzzles(data);
        else setError(getErrorMessage(apiError, "Không tải được danh sách câu đố."));
      } catch {
        setError("Không kết nối được tới máy chủ.");
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  return (
    <section>
      <div className="page-header-row">
        <div>
          <h1>Câu đố ngoài game</h1>
          <p className="status">Câu đố tạo sẵn theo kịch bản; ở đây chỉ cấu hình bằng chứng, lượt gửi và xem lịch sử.</p>
        </div>
      </div>
      <Alerts error={error} success={null} />
      {loading ? (
        <p className="status">Đang tải danh sách câu đố...</p>
      ) : puzzles.length === 0 && !error ? (
        <p className="status">Chưa có câu đố nào.</p>
      ) : (
        <div className="table-wrapper">
          <table className="accounts-table">
            <thead>
              <tr>
                <th>Câu đố</th>
                <th>Trạng thái</th>
                <th>Mã băm hợp lệ</th>
                <th>Giới hạn lượt sai</th>
                <th>Cập nhật</th>
              </tr>
            </thead>
            <tbody>
              {puzzles.map((p) => (
                <tr key={p.puzzleId}>
                  <td>
                    <Link to={`/puzzles/${p.puzzleId}`}>{p.title}</Link>
                    <div className="text-muted font-mono">{p.puzzleId}</div>
                  </td>
                  <td>
                    <EnabledBadge enabled={p.isEnabled} />
                  </td>
                  <td>{p.solutions.length}</td>
                  <td>
                    {p.maxFailedAttempts} lượt / {p.attemptWindowMinutes} phút
                  </td>
                  <td>{formatDate(p.updatedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

export function PuzzleDetailPage() {
  const { puzzleId = "" } = useParams();
  const [puzzle, setPuzzle] = useState<PuzzleConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const [maxFailed, setMaxFailed] = useState("");
  const [windowMinutes, setWindowMinutes] = useState("");
  const [savingLimits, setSavingLimits] = useState(false);
  const [toggling, setToggling] = useState(false);

  const [newSha, setNewSha] = useState("");
  const [newNote, setNewNote] = useState("");
  const [adding, setAdding] = useState(false);
  const [removingId, setRemovingId] = useState<number | null>(null);

  const [history, setHistory] = useState<PuzzleSubmissionPage | null>(null);
  const [resultFilter, setResultFilter] = useState<"" | SubmissionResult>("");
  const [installationFilter, setInstallationFilter] = useState("");
  const [page, setPage] = useState(1);

  const applyPuzzle = useCallback((p: PuzzleConfig) => {
    setPuzzle(p);
    setMaxFailed(String(p.maxFailedAttempts));
    setWindowMinutes(String(p.attemptWindowMinutes));
  }, []);

  const loadPuzzle = useCallback(async () => {
    try {
      const { data, response, error: apiError } = await api.GET("/api/admin/puzzles/{puzzleId}", {
        params: { path: { puzzleId } },
      });
      if (response.ok && data) applyPuzzle(data);
      else setError(getErrorMessage(apiError, "Không tải được câu đố."));
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setLoading(false);
    }
  }, [puzzleId, applyPuzzle]);

  const loadHistory = useCallback(async () => {
    const installationId = installationFilter.trim();
    try {
      const { data, response, error: apiError } = await api.GET("/api/admin/puzzles/{puzzleId}/submissions", {
        params: {
          path: { puzzleId },
          query: {
            page,
            pageSize: PAGE_SIZE,
            ...(resultFilter ? { result: resultFilter } : {}),
            ...(installationId ? { installationId } : {}),
          },
        },
      });
      if (response.ok && data) setHistory(data);
      else setError(getErrorMessage(apiError, "Không tải được lịch sử gửi."));
    } catch {
      setError("Không kết nối được tới máy chủ.");
    }
  }, [puzzleId, page, resultFilter, installationFilter]);

  useEffect(() => {
    void loadPuzzle();
  }, [loadPuzzle]);

  useEffect(() => {
    void loadHistory();
    // Typing in the installation box should not refetch on every key; the filter form submits instead.
  }, [puzzleId, page, resultFilter]);

  async function patch(body: { isEnabled?: boolean; maxFailedAttempts?: number; attemptWindowMinutes?: number }, done: string) {
    setError(null);
    setSuccess(null);
    const { data, response, error: apiError } = await api.PATCH("/api/admin/puzzles/{puzzleId}", {
      params: { path: { puzzleId } },
      body,
    });
    if (response.ok && data) {
      applyPuzzle(data);
      setSuccess(done);
    } else {
      setError(getErrorMessage(apiError, "Không lưu được cấu hình."));
    }
  }

  async function handleToggle() {
    if (!puzzle) return;
    setToggling(true);
    try {
      await patch({ isEnabled: !puzzle.isEnabled }, puzzle.isEnabled ? "Đã tắt câu đố." : "Đã bật câu đố.");
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setToggling(false);
    }
  }

  async function handleSaveLimits(event: FormEvent) {
    event.preventDefault();
    const max = Number(maxFailed);
    const minutes = Number(windowMinutes);
    if (!Number.isInteger(max) || max < 1 || max > 100) {
      setError("Số lượt sai phải là số nguyên từ 1 tới 100.");
      return;
    }
    if (!Number.isInteger(minutes) || minutes < 1 || minutes > 10080) {
      setError("Khoảng thời gian phải là số nguyên từ 1 tới 10080 phút.");
      return;
    }
    setSavingLimits(true);
    try {
      await patch({ maxFailedAttempts: max, attemptWindowMinutes: minutes }, "Đã lưu giới hạn lượt gửi. Game áp dụng từ lần gửi kế tiếp.");
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setSavingLimits(false);
    }
  }

  async function handlePickFile(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    setNewSha(await sha256OfFile(file));
    if (!newNote) setNewNote(file.name);
  }

  async function handleAddSolution(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSuccess(null);
    const sha256 = newSha.trim().toLowerCase();
    if (!SHA256.test(sha256)) {
      setError("Mã băm SHA-256 phải gồm 64 ký tự hex.");
      return;
    }
    setAdding(true);
    try {
      const { response, error: apiError } = await api.POST("/api/admin/puzzles/{puzzleId}/solutions", {
        params: { path: { puzzleId } },
        body: { sha256, note: newNote.trim() },
      });
      if (response.ok) {
        setNewSha("");
        setNewNote("");
        setSuccess("Đã thêm mã băm hợp lệ.");
        await loadPuzzle();
      } else {
        setError(getErrorMessage(apiError, "Không thêm được mã băm."));
      }
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setAdding(false);
    }
  }

  async function handleRemove(solution: PuzzleSolution) {
    setError(null);
    setSuccess(null);
    setRemovingId(solution.id);
    try {
      const { response, error: apiError } = await api.DELETE("/api/admin/puzzles/{puzzleId}/solutions/{solutionId}", {
        params: { path: { puzzleId, solutionId: solution.id } },
      });
      if (response.ok) {
        setSuccess("Đã bỏ mã băm.");
        await loadPuzzle();
      } else {
        setError(getErrorMessage(apiError, "Không bỏ được mã băm."));
      }
    } catch {
      setError("Không kết nối được tới máy chủ.");
    } finally {
      setRemovingId(null);
    }
  }

  function handleFilter(event: FormEvent) {
    event.preventDefault();
    if (page === 1) void loadHistory();
    else setPage(1);
  }

  if (loading) return <p className="status">Đang tải câu đố...</p>;
  if (!puzzle)
    return (
      <section>
        <p>
          <Link to="/puzzles">← Danh sách câu đố</Link>
        </p>
        <Alerts error={error ?? "Không có câu đố này."} success={null} />
      </section>
    );

  const pages = history ? Math.max(1, Math.ceil(history.total / history.pageSize)) : 1;

  return (
    <section>
      <p>
        <Link to="/puzzles">← Danh sách câu đố</Link>
      </p>
      <div className="page-header-row">
        <div>
          <h1>{puzzle.title}</h1>
          <p className="status font-mono">
            {puzzle.puzzleId} · mở khóa {puzzle.unlockId}
          </p>
        </div>
        <div style={{ display: "flex", gap: "0.75rem", alignItems: "center" }}>
          <EnabledBadge enabled={puzzle.isEnabled} />
          <button type="button" className={puzzle.isEnabled ? "secondary" : ""} disabled={toggling} onClick={() => void handleToggle()}>
            {puzzle.isEnabled ? "Tắt câu đố" : "Bật câu đố"}
          </button>
        </div>
      </div>

      <Alerts error={error} success={success} />

      <form onSubmit={handleSaveLimits} className="form-card">
        <div className="card-title-row">
          <h2>Giới hạn lượt gửi</h2>
          <span className="card-subtitle">
            Mỗi máy được gửi sai tối đa số lượt này trong khoảng thời gian; hết lượt thì game nhận "bị chặn". Lần gửi đúng không trừ lượt.
          </span>
        </div>
        <div className="form-grid">
          <label>
            Số lượt sai tối đa
            <input type="number" min={1} max={100} required value={maxFailed} onChange={(e) => setMaxFailed(e.target.value)} />
          </label>
          <label>
            Trong khoảng (phút)
            <input type="number" min={1} max={10080} required value={windowMinutes} onChange={(e) => setWindowMinutes(e.target.value)} />
          </label>
        </div>
        <div style={{ marginTop: "1.25rem" }}>
          <button type="submit" disabled={savingLimits}>
            {savingLimits ? "Đang lưu..." : "Lưu giới hạn"}
          </button>
        </div>
      </form>

      <div className="form-card">
        <div className="card-title-row">
          <h2>Bằng chứng hợp lệ</h2>
          <span className="card-subtitle">Game gửi SHA-256 của tệp người chơi tìm được; khớp một mã dưới đây là đúng.</span>
        </div>
        {puzzle.solutions.length === 0 ? (
          <p className="status">Chưa có mã băm nào. Câu đố chỉ bật được khi có ít nhất một mã.</p>
        ) : (
          <div className="table-wrapper">
            <table className="accounts-table">
              <thead>
                <tr>
                  <th>SHA-256</th>
                  <th>Ghi chú</th>
                  <th>Thêm lúc</th>
                  <th style={{ textAlign: "right" }}>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {puzzle.solutions.map((s) => (
                  <tr key={s.id}>
                    <td className="font-mono" title={s.sha256}>
                      {shortHash(s.sha256)}
                    </td>
                    <td>{s.note || <span className="text-muted">—</span>}</td>
                    <td>{formatDate(s.createdAt)}</td>
                    <td className="actions-cell">
                      <button
                        type="button"
                        className="action-icon-btn"
                        aria-label={`Bỏ mã ${shortHash(s.sha256)}`}
                        title="Bỏ mã băm này"
                        disabled={removingId === s.id}
                        onClick={() => void handleRemove(s)}
                      >
                        <TrashIcon />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <form onSubmit={handleAddSolution} style={{ marginTop: "1.25rem" }}>
          <div className="form-grid">
            <label>
              Tệp bằng chứng gốc (tự tính mã băm)
              <input type="file" onChange={(e) => void handlePickFile(e)} />
            </label>
            <label>
              Mã SHA-256
              <input className="font-mono" required value={newSha} onChange={(e) => setNewSha(e.target.value)} placeholder="64 ký tự hex" />
            </label>
            <label>
              Ghi chú
              <input maxLength={200} value={newNote} onChange={(e) => setNewNote(e.target.value)} placeholder="Ví dụ: ảnh gốc 1920px" />
            </label>
          </div>
          <div style={{ marginTop: "1rem" }}>
            <button type="submit" disabled={adding}>
              {adding ? "Đang thêm..." : "Thêm mã băm"}
            </button>
          </div>
        </form>
      </div>

      <div className="form-card">
        <div className="card-title-row">
          <h2>Lịch sử gửi</h2>
          <span className="card-subtitle">30 ngày gần nhất, mới nhất trước.</span>
        </div>
        <form onSubmit={handleFilter} className="form-grid">
          <label>
            Kết quả
            <select
              value={resultFilter}
              onChange={(e) => {
                setResultFilter(e.target.value as "" | SubmissionResult);
                setPage(1);
              }}
            >
              <option value="">Tất cả</option>
              <option value="accepted">Đúng</option>
              <option value="rejected">Sai</option>
              <option value="blocked">Bị chặn</option>
            </select>
          </label>
          <label>
            Mã cài đặt
            <input className="font-mono" value={installationFilter} onChange={(e) => setInstallationFilter(e.target.value)} placeholder="UUID của máy" />
          </label>
          <div style={{ alignSelf: "end" }}>
            <button type="submit" className="secondary">
              Lọc
            </button>
          </div>
        </form>
        {!history ? (
          <p className="status">Đang tải lịch sử...</p>
        ) : history.items.length === 0 ? (
          <p className="status">Chưa có lượt gửi nào.</p>
        ) : (
          <>
            <div className="table-wrapper">
              <table className="accounts-table">
                <thead>
                  <tr>
                    <th>Thời điểm</th>
                    <th>Kết quả</th>
                    <th>Mã cài đặt</th>
                    <th>SHA-256 đã gửi</th>
                    <th>Phiên bản</th>
                  </tr>
                </thead>
                <tbody>
                  {history.items.map((s) => (
                    <tr key={s.id}>
                      <td>{formatDate(s.submittedAt)}</td>
                      <td>
                        <span className={`badge ${s.result === "accepted" ? "badge-active" : "badge-inactive"}`}>{RESULT_LABEL[s.result]}</span>
                      </td>
                      <td className="font-mono" title={s.installationId}>
                        {s.installationId.slice(0, 8)}
                      </td>
                      <td className="font-mono" title={s.sha256}>
                        {shortHash(s.sha256)}
                      </td>
                      <td>{s.gameVersion}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div style={{ display: "flex", gap: "0.75rem", alignItems: "center", marginTop: "1rem" }}>
              <button type="button" className="secondary" disabled={page <= 1} onClick={() => setPage(page - 1)}>
                Trang trước
              </button>
              <span className="status">
                Trang {history.page}/{pages} · {history.total} lượt
              </span>
              <button type="button" className="secondary" disabled={page >= pages} onClick={() => setPage(page + 1)}>
                Trang sau
              </button>
            </div>
          </>
        )}
      </div>
    </section>
  );
}
