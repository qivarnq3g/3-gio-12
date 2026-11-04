export const AREA_NAMES: Record<string, string> = {
  // Night 1
  "n1.room7": "Phòng trọ số 7 (Đêm 1)",
  "n1.corridor_3f": "Hành lang tầng 3",
  "n1.stairs": "Cầu thang xóm trọ",
  "n1.parking": "Bãi giữ xe",
  "n1.alley": "Con hẻm xóm trọ",
  "n1.alley_start": "Đầu hẻm (gần chốt dân phòng)",
  "n1.road": "Đường lớn / mặt tiền",
  "n1.store": "Tiệm tạp hóa",
  "n1.gate": "Cổng chính xóm trọ",
  "n1.rice_shop": "Quán cơm tấm đêm",

  // Night 2
  "n2.room7": "Phòng trọ số 7 (Đêm 2)",
  "n2.corridor_3f": "Hành lang tầng 3",
  "n2.parking": "Bãi giữ xe",
  "n2.alley": "Con hẻm xóm trọ",
  "n2.road": "Đường lớn ngoài xóm",
  "n2.store": "Tiệm tạp hóa",
  "n2.gate": "Cổng chính xóm trọ",
  "n2.rice_shop": "Quán cơm tấm đêm",

  // Night 3
  "n3.room7": "Phòng trọ số 7 (Đêm 3)",
  "n3.corridor_3f": "Hành lang tầng 3",
  "n3.parking": "Bãi giữ xe",
  "n3.alley": "Con hẻm xóm trọ",
  "n3.road": "Đường lộ lớn",
  "n3.gas_station": "Cây xăng bỏ hoang",
  "n3.police": "Trụ sở công an phường",
};

export interface EndingMeta {
  title: string;
  tag: string;
  type: "true" | "neutral" | "bad";
  color: string;
}

export const ENDINGS_META: Record<string, EndingMeta> = {
  red_thread_sandal: {
    title: "6.7 Đôi dép chỉ đỏ",
    tag: "Bí mật ARG",
    type: "true",
    color: "#f43f5e",
  },
  home: {
    title: "6.6 Về nhà",
    tag: "Chính quy",
    type: "neutral",
    color: "#10b981",
  },
  bystander: {
    title: "6.1 Người qua đường",
    tag: "Bỏ cuộc",
    type: "bad",
    color: "#6b7280",
  },
  scapegoat: {
    title: "6.2 Kẻ gánh tội",
    tag: "Bị hại",
    type: "bad",
    color: "#ef4444",
  },
  freezer: {
    title: "6.3 Ngăn đá",
    tag: "Thảm kịch",
    type: "bad",
    color: "#06b6d4",
  },
  empty_room: {
    title: "6.4 Căn phòng trống",
    tag: "Biến mất",
    type: "bad",
    color: "#8b5cf6",
  },
  deadline_passed: {
    title: "6.5 Hết hạn",
    tag: "Quá giờ",
    type: "bad",
    color: "#f59e0b",
  },
};

export const CHOICE_DESCRIPTIONS: Record<string, string> = {
  "1a": "Chặn dãy số lạ (Đêm 1)",
  "1b": "Trả lời dãy số / gọi lại (Đêm 1)",
  "1c": "Kệ, không xử lý dãy số (Đêm 1)",
  "sleep": "Đi ngủ sớm",
  "watch": "Thức canh tới sáng",
  "0a": "Đi thẳng tới công an phường",
  "0b": "Vứt điện thoại tại bãi xe",
  "follow_girl": "Đi theo bóng cô gái trong hẻm",
  "hide_corner": "Nấp sau góc tường quan sát",
  "call_police": "Gọi báo công an phường",
  "confront": "Đối mặt với kẻ bám đuôi",
};

export function getAreaName(areaId: string): string {
  return AREA_NAMES[areaId] ?? areaId;
}

export function getEndingMeta(endingId: string): EndingMeta {
  return (
    ENDINGS_META[endingId] ?? {
      title: endingId,
      tag: "Kết thúc",
      type: "bad",
      color: "#9ca3af",
    }
  );
}

export function getChoiceDescription(choiceId: string): string {
  return CHOICE_DESCRIPTIONS[choiceId] ?? choiceId;
}

export function formatDuration(seconds: number | null | undefined): string {
  if (seconds === null || seconds === undefined || isNaN(seconds)) {
    return "—";
  }
  const s = Math.round(seconds);
  if (s < 60) return `${s}s`;
  const m = Math.floor(s / 60);
  const remainingSeconds = s % 60;
  if (m < 60) {
    return remainingSeconds > 0 ? `${m}p ${remainingSeconds}s` : `${m} phút`;
  }
  const h = Math.floor(m / 60);
  const remainingMinutes = m % 60;
  return remainingMinutes > 0 ? `${h}h ${remainingMinutes}p` : `${h} giờ`;
}
