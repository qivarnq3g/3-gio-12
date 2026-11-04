import { useState } from "react";
import { CalendarIcon, RefreshIcon } from "./Icons";

interface DateRangePickerProps {
  from?: string;
  to?: string;
  onChange: (from?: string, to?: string) => void;
  isLoading?: boolean;
}

// Converts a Date to YYYY-MM-DD in Vietnam (+07:00)
function toVietnamDateString(date: Date): string {
  const vnTime = new Date(date.getTime() + 7 * 60 * 60 * 1000);
  return vnTime.toISOString().slice(0, 10);
}

// Converts YYYY-MM-DD to ISO 8601 with +07:00
function toMidnightVnIso(dateStr: string): string {
  return `${dateStr}T00:00:00+07:00`;
}

export function DateRangePicker({ from, to, onChange, isLoading }: DateRangePickerProps) {
  const [fromDate, setFromDate] = useState<string>(() => {
    if (from) return from.slice(0, 10);
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return toVietnamDateString(d);
  });

  const [toDate, setToDate] = useState<string>(() => {
    if (to) return to.slice(0, 10);
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return toVietnamDateString(d);
  });

  const [activePreset, setActivePreset] = useState<number | "all">(30);
  const [validationError, setValidationError] = useState<string | null>(null);

  const applyPreset = (days: number) => {
    setActivePreset(days);
    setValidationError(null);
    const now = new Date();
    const end = new Date(now);
    end.setDate(end.getDate() + 1);

    const start = new Date(now);
    start.setDate(start.getDate() - days);

    const startStr = toVietnamDateString(start);
    const endStr = toVietnamDateString(end);

    setFromDate(startStr);
    setToDate(endStr);
    onChange(toMidnightVnIso(startStr), toMidnightVnIso(endStr));
  };

  const handleCustomApply = (e: React.FormEvent) => {
    e.preventDefault();
    setValidationError(null);

    if (fromDate && toDate && fromDate >= toDate) {
      setValidationError("Ngày bắt đầu phải trước ngày kết thúc.");
      return;
    }

    const startMs = new Date(fromDate).getTime();
    const endMs = new Date(toDate).getTime();
    const diffDays = (endMs - startMs) / (1000 * 60 * 60 * 24);
    if (diffDays > 366) {
      setValidationError("Khoảng thời gian không được vượt quá 366 ngày.");
      return;
    }

    setActivePreset("all");
    onChange(fromDate ? toMidnightVnIso(fromDate) : undefined, toDate ? toMidnightVnIso(toDate) : undefined);
  };

  const handleReset = () => {
    applyPreset(30);
  };

  return (
    <div className="date-picker-card">
      <div className="date-picker-header">
        <div className="date-picker-title">
          <CalendarIcon />
          <span>Khoảng thời gian thống kê</span>
        </div>
        <div className="preset-group" role="group" aria-label="Khoảng thời gian nhanh">
          <button
            type="button"
            className={`preset-btn ${activePreset === 7 ? "active" : ""}`}
            onClick={() => applyPreset(7)}
            disabled={isLoading}
          >
            7 ngày
          </button>
          <button
            type="button"
            className={`preset-btn ${activePreset === 30 ? "active" : ""}`}
            onClick={() => applyPreset(30)}
            disabled={isLoading}
          >
            30 ngày
          </button>
          <button
            type="button"
            className={`preset-btn ${activePreset === 90 ? "active" : ""}`}
            onClick={() => applyPreset(90)}
            disabled={isLoading}
          >
            90 ngày
          </button>
        </div>
      </div>

      <form className="date-picker-form" onSubmit={handleCustomApply}>
        <div className="date-input-group">
          <label htmlFor="date-from">Từ ngày</label>
          <input
            id="date-from"
            type="date"
            className="date-input"
            value={fromDate}
            onChange={(e) => {
              setFromDate(e.target.value);
              setActivePreset("all");
            }}
            disabled={isLoading}
          />
        </div>

        <div className="date-input-group">
          <label htmlFor="date-to">Đến ngày</label>
          <input
            id="date-to"
            type="date"
            className="date-input"
            value={toDate}
            onChange={(e) => {
              setToDate(e.target.value);
              setActivePreset("all");
            }}
            disabled={isLoading}
          />
        </div>

        <div className="date-picker-actions">
          <button type="submit" className="btn btn-primary btn-sm" disabled={isLoading}>
            {isLoading ? "Đang tải..." : "Áp dụng"}
          </button>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={handleReset}
            disabled={isLoading}
            title="Đặt lại 30 ngày mặc định"
          >
            <RefreshIcon />
          </button>
        </div>
      </form>

      {validationError && (
        <div className="date-picker-error" role="alert">
          {validationError}
        </div>
      )}
    </div>
  );
}
