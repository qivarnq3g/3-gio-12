import { useEffect, useState, useMemo } from "react";
import { api, type DashboardSummary } from "../api/client";
import { formatApiError } from "../api/error";
import { DateRangePicker } from "../components/DateRangePicker";
import {
  UsersIcon,
  PlayIcon,
  ClockIcon,
  TrendingUpIcon,
  DownloadIcon,
  AlertCircleIcon,
  MapPinIcon,
  TrophyIcon,
  GitBranchIcon,
  RefreshIcon,
} from "../components/Icons";
import {
  getAreaName,
  getEndingMeta,
  getChoiceDescription,
  formatDuration,
} from "../api/areaNames";

export function DashboardPage() {
  const [data, setData] = useState<DashboardSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dateRange, setDateRange] = useState<{ from?: string; to?: string }>({});
  const [locationTab, setLocationTab] = useState<"caught" | "quit">("caught");
  const [choiceNightFilter, setChoiceNightFilter] = useState<number | "all">("all");

  const loadDashboard = async (from?: string, to?: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await api.GET("/api/admin/dashboard", {
        params: {
          query: {
            from: from || undefined,
            to: to || undefined,
          },
        },
      });

      if (res.error) {
        setError(formatApiError(res.error));
        setData(null);
      } else if (res.data) {
        setData(res.data);
      }
    } catch (err) {
      setError(formatApiError(err));
      setData(null);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadDashboard(dateRange.from, dateRange.to);
  }, [dateRange]);

  const handleRangeChange = (from?: string, to?: string) => {
    setDateRange({ from, to });
  };

  const exportUrl = (dataset: "summary" | "events") => {
    const params = new URLSearchParams();
    if (dateRange.from) params.set("from", dateRange.from);
    if (dateRange.to) params.set("to", dateRange.to);
    const qs = params.toString();
    return `/api/admin/exports/${dataset}${qs ? `?${qs}` : ""}`;
  };

  // Calculations
  const totalCaught = useMemo(() => {
    if (!data?.caughtLocations) return 0;
    return data.caughtLocations.reduce((sum, item) => sum + item.count, 0);
  }, [data?.caughtLocations]);

  const totalQuits = useMemo(() => {
    if (!data?.quitLocations) return 0;
    return data.quitLocations.reduce((sum, item) => sum + item.count, 0);
  }, [data?.quitLocations]);

  const totalEndingPlayers = useMemo(() => {
    if (!data?.endings) return 0;
    return data.endings.reduce((sum, item) => sum + item.players, 0);
  }, [data?.endings]);

  const filteredChoices = useMemo(() => {
    if (!data?.choices) return [];
    if (choiceNightFilter === "all") return data.choices;
    return data.choices.filter((c) => c.night === choiceNightFilter);
  }, [data?.choices, choiceNightFilter]);

  return (
    <div className="dashboard-container">
      {/* Page Header */}
      <div className="dashboard-header">
        <div>
          <h1 className="page-title">Dashboard</h1>
          <p className="page-subtitle">
            Dữ liệu hành vi và tiến độ trải nghiệm ẩn danh của người chơi qua 3 đêm kinh hoàng.
          </p>
        </div>

        {/* CSV Export Bar */}
        <div className="export-actions">
          <a
            href={exportUrl("summary")}
            download
            className="btn btn-secondary export-btn"
            title="Tải số liệu tổng hợp định dạng CSV (UTF-8 BOM)"
          >
            <DownloadIcon />
            <span>Xuất tổng hợp CSV</span>
          </a>
          <a
            href={exportUrl("events")}
            download
            className="btn btn-secondary export-btn"
            title="Tải toàn bộ sự kiện thô định dạng CSV (UTF-8 BOM)"
          >
            <DownloadIcon />
            <span>Xuất sự kiện CSV</span>
          </a>
        </div>
      </div>

      {/* Date Range Picker */}
      <DateRangePicker
        from={dateRange.from}
        to={dateRange.to}
        onChange={handleRangeChange}
        isLoading={isLoading}
      />

      {/* Error state */}
      {error && (
        <div className="alert alert-error" role="alert">
          <AlertCircleIcon />
          <span>{error}</span>
          <button
            type="button"
            className="btn btn-sm btn-ghost"
            onClick={() => loadDashboard(dateRange.from, dateRange.to)}
            style={{ marginLeft: "auto" }}
          >
            <RefreshIcon /> Thử lại
          </button>
        </div>
      )}

      {/* Loading state skeleton */}
      {isLoading && !data && (
        <div className="loading-state" role="status">
          <div className="spinner" />
          <p>Đang tổng hợp dữ liệu thống kê...</p>
        </div>
      )}

      {/* Dashboard Data */}
      {data && (
        <>
          {/* Top Metric Cards */}
          <div className="metrics-grid">
            <div className="metric-card">
              <div className="metric-icon-box users">
                <UsersIcon />
              </div>
              <div className="metric-content">
                <div className="metric-label">Tổng người chơi</div>
                <div className="metric-value">{data.players.toLocaleString()}</div>
                <div className="metric-footnote">Mã cài đặt định danh ẩn danh</div>
              </div>
            </div>

            <div className="metric-card">
              <div className="metric-icon-box sessions">
                <PlayIcon />
              </div>
              <div className="metric-content">
                <div className="metric-label">Tổng phiên chơi</div>
                <div className="metric-value">{data.sessions.toLocaleString()}</div>
                <div className="metric-footnote">Lần khởi động trò chơi</div>
              </div>
            </div>

            <div className="metric-card">
              <div className="metric-icon-box playtime">
                <ClockIcon />
              </div>
              <div className="metric-content">
                <div className="metric-label">Tổng thời gian chơi</div>
                <div className="metric-value">{formatDuration(data.playtime.totalSeconds)}</div>
                <div className="metric-footnote">Thời lượng các phiên đã kết thúc</div>
              </div>
            </div>

            <div className="metric-card">
              <div className="metric-icon-box average">
                <TrendingUpIcon />
              </div>
              <div className="metric-content">
                <div className="metric-label">Thời gian TB / phiên</div>
                <div className="metric-value">{formatDuration(data.playtime.averageSessionSeconds)}</div>
                <div className="metric-footnote">
                  {data.playtime.averageSessionSeconds ? "Thời lượng trung bình" : "Chưa có dữ liệu phiên"}
                </div>
              </div>
            </div>
          </div>

          {/* Night Funnel Section */}
          <section className="dashboard-section" aria-labelledby="funnel-title">
            <div className="section-header">
              <h2 id="funnel-title" className="section-title">
                <TrendingUpIcon /> Tiến Độ Vượt Qua Các Đêm (Night Funnel)
              </h2>
              <span className="section-hint">Tỷ lệ người chơi hoàn thành từng đêm không tính chơi lại</span>
            </div>

            {data.nights.length === 0 ? (
              <div className="empty-card">Chưa ghi nhận người chơi nào bắt đầu các đêm trong khoảng thời gian này.</div>
            ) : (
              <div className="funnel-grid">
                {data.nights.map((night) => {
                  const rate =
                    night.playersStarted > 0
                      ? Math.round((night.playersCompleted / night.playersStarted) * 100)
                      : 0;
                  const dropoff = Math.max(0, night.playersStarted - night.playersCompleted);

                  return (
                    <div key={night.night} className="funnel-card">
                      <div className="funnel-card-header">
                        <span className={`night-badge night-${night.night}`}>Đêm {night.night}</span>
                        <span className="funnel-rate-badge">{rate}% Hoàn thành</span>
                      </div>

                      <div className="funnel-bar-track">
                        <div
                          className={`funnel-bar-fill fill-night-${night.night}`}
                          style={{ width: `${Math.min(100, Math.max(4, rate))}%` }}
                        />
                      </div>

                      <div className="funnel-stats-grid">
                        <div className="funnel-stat">
                          <span className="stat-label">Bắt đầu</span>
                          <span className="stat-val">{night.playersStarted.toLocaleString()}</span>
                        </div>
                        <div className="funnel-stat">
                          <span className="stat-label">Hoàn thành</span>
                          <span className="stat-val accent">{night.playersCompleted.toLocaleString()}</span>
                        </div>
                        <div className="funnel-stat">
                          <span className="stat-label">Rời bỏ</span>
                          <span className="stat-val text-muted">{dropoff.toLocaleString()}</span>
                        </div>
                        <div className="funnel-stat">
                          <span className="stat-label">Thời gian TB</span>
                          <span className="stat-val">{formatDuration(night.averageDurationSeconds)}</span>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </section>

          {/* Two-column layout: Location Hotspots & Ending Distribution */}
          <div className="dashboard-columns">
            {/* Location Hotspots */}
            <section className="dashboard-section" aria-labelledby="locations-title">
              <div className="section-header">
                <h2 id="locations-title" className="section-title">
                  <MapPinIcon /> Điểm Nóng Khu Vực
                </h2>
                <div className="tab-pill-group" role="tablist">
                  <button
                    type="button"
                    role="tab"
                    aria-selected={locationTab === "caught"}
                    className={`tab-pill ${locationTab === "caught" ? "active" : ""}`}
                    onClick={() => setLocationTab("caught")}
                  >
                    Bị tóm ({totalCaught})
                  </button>
                  <button
                    type="button"
                    role="tab"
                    aria-selected={locationTab === "quit"}
                    className={`tab-pill ${locationTab === "quit" ? "active" : ""}`}
                    onClick={() => setLocationTab("quit")}
                  >
                    Bỏ game ({totalQuits})
                  </button>
                </div>
              </div>

              {locationTab === "caught" ? (
                data.caughtLocations.length === 0 ? (
                  <div className="empty-card">Không có dữ liệu người chơi bị bắt.</div>
                ) : (
                  <div className="table-responsive">
                    <table className="data-table">
                      <thead>
                        <tr>
                          <th>Đêm</th>
                          <th>Khu vực</th>
                          <th style={{ textAlign: "right" }}>Số lần</th>
                          <th style={{ width: "30%" }}>Tỷ trọng</th>
                        </tr>
                      </thead>
                      <tbody>
                        {data.caughtLocations.map((item, idx) => {
                          const pct = totalCaught > 0 ? Math.round((item.count / totalCaught) * 100) : 0;
                          return (
                            <tr key={`${item.night}-${item.areaId}-${idx}`}>
                              <td>
                                <span className={`night-badge night-${item.night}`}>Đêm {item.night}</span>
                              </td>
                              <td>
                                <div className="area-info">
                                  <span className="area-title">{getAreaName(item.areaId)}</span>
                                  <code className="area-code">{item.areaId}</code>
                                </div>
                              </td>
                              <td style={{ textAlign: "right", fontWeight: 600 }}>{item.count}</td>
                              <td>
                                <div className="inline-bar-wrap">
                                  <div className="inline-bar-track">
                                    <div className="inline-bar-fill fill-danger" style={{ width: `${pct}%` }} />
                                  </div>
                                  <span className="inline-bar-pct">{pct}%</span>
                                </div>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                )
              ) : data.quitLocations.length === 0 ? (
                <div className="empty-card">Không có dữ liệu người chơi bỏ ngang phiên.</div>
              ) : (
                <div className="table-responsive">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Đêm</th>
                        <th>Khu vực</th>
                        <th style={{ textAlign: "right" }}>Số lần</th>
                        <th style={{ width: "30%" }}>Tỷ trọng</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.quitLocations.map((item, idx) => {
                        const pct = totalQuits > 0 ? Math.round((item.count / totalQuits) * 100) : 0;
                        return (
                          <tr key={`${item.night}-${item.areaId}-${idx}`}>
                            <td>
                              <span className={`night-badge night-${item.night}`}>Đêm {item.night}</span>
                            </td>
                            <td>
                              <div className="area-info">
                                <span className="area-title">{getAreaName(item.areaId)}</span>
                                <code className="area-code">{item.areaId}</code>
                              </div>
                            </td>
                            <td style={{ textAlign: "right", fontWeight: 600 }}>{item.count}</td>
                            <td>
                              <div className="inline-bar-wrap">
                                <div className="inline-bar-fill fill-warning" style={{ width: `${pct}%` }} />
                                <span className="inline-bar-pct">{pct}%</span>
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </section>

            {/* Ending Distribution */}
            <section className="dashboard-section" aria-labelledby="endings-title">
              <div className="section-header">
                <h2 id="endings-title" className="section-title">
                  <TrophyIcon /> Phân Phối Kết Thúc
                </h2>
                <span className="section-hint">Tổng cộng {totalEndingPlayers} người chạm tới kết thúc</span>
              </div>

              {data.endings.length === 0 ? (
                <div className="empty-card">Chưa có người chơi nào hoàn thành kết thúc.</div>
              ) : (
                <div className="endings-list">
                  {data.endings.map((ending) => {
                    const meta = getEndingMeta(ending.endingId);
                    const pct = totalEndingPlayers > 0 ? Math.round((ending.players / totalEndingPlayers) * 100) : 0;
                    const isSecret = ending.endingId === "red_thread_sandal";

                    return (
                      <div key={ending.endingId} className={`ending-row ${isSecret ? "secret-ending" : ""}`}>
                        <div className="ending-info">
                          <div className="ending-header">
                            <span className="ending-name">{meta.title}</span>
                            <span className={`ending-tag tag-${meta.type}`}>{meta.tag}</span>
                          </div>
                          <div className="ending-meta">
                            <code>{ending.endingId}</code>
                            <span className="ending-count">
                              <strong>{ending.players}</strong> người ({pct}%)
                            </span>
                          </div>
                        </div>

                        <div className="ending-bar-track">
                          <div
                            className="ending-bar-fill"
                            style={{
                              width: `${Math.max(4, pct)}%`,
                              backgroundColor: meta.color,
                            }}
                          />
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </section>
          </div>

          {/* Choices Breakdown */}
          <section className="dashboard-section" aria-labelledby="choices-title">
            <div className="section-header">
              <h2 id="choices-title" className="section-title">
                <GitBranchIcon /> Quyết Định Của Người Chơi (Branch Choices)
              </h2>
              <div className="tab-pill-group" role="tablist">
                <button
                  type="button"
                  className={`tab-pill ${choiceNightFilter === "all" ? "active" : ""}`}
                  onClick={() => setChoiceNightFilter("all")}
                >
                  Tất cả
                </button>
                <button
                  type="button"
                  className={`tab-pill ${choiceNightFilter === 1 ? "active" : ""}`}
                  onClick={() => setChoiceNightFilter(1)}
                >
                  Đêm 1
                </button>
                <button
                  type="button"
                  className={`tab-pill ${choiceNightFilter === 2 ? "active" : ""}`}
                  onClick={() => setChoiceNightFilter(2)}
                >
                  Đêm 2
                </button>
                <button
                  type="button"
                  className={`tab-pill ${choiceNightFilter === 3 ? "active" : ""}`}
                  onClick={() => setChoiceNightFilter(3)}
                >
                  Đêm 3
                </button>
              </div>
            </div>

            {filteredChoices.length === 0 ? (
              <div className="empty-card">Không có lựa chọn nào được ghi nhận.</div>
            ) : (
              <div className="table-responsive">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Đêm</th>
                      <th>Mã lựa chọn</th>
                      <th>Mô tả hành vi</th>
                      <th style={{ textAlign: "right" }}>Số người chọn</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredChoices.map((c, idx) => (
                      <tr key={`${c.night}-${c.choiceId}-${idx}`}>
                        <td>
                          <span className={`night-badge night-${c.night}`}>Đêm {c.night}</span>
                        </td>
                        <td>
                          <code className="choice-code">{c.choiceId}</code>
                        </td>
                        <td className="choice-desc">{getChoiceDescription(c.choiceId)}</td>
                        <td style={{ textAlign: "right", fontWeight: 600 }}>
                          <span className="badge badge-neutral">{c.players.toLocaleString()} người</span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}
