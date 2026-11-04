#!/usr/bin/env python3
"""
Công cụ nạp dữ liệu thống kê mẫu (Telemetry Seeder) cho 3 Giờ 12.
Dùng để các thành viên trong nhóm kiểm thử Dashboard và xuất CSV (#28).
Gửi sự kiện ẩn danh qua API tiêu chuẩn: POST /api/v1/telemetry/batches

Cách dùng:
  python tools/seed_telemetry.py         # Nạp 15 người chơi mẫu vào máy chủ
  python tools/seed_telemetry.py --clear # Xóa sạch dữ liệu về 0
"""

import json
import sys
import urllib.error
import urllib.request
import uuid
from datetime import datetime, timezone, timedelta

SERVER_URL = "http://localhost:8080"
BATCH_ENDPOINT = f"{SERVER_URL}/api/v1/telemetry/batches"
CLEAR_ENDPOINT = f"{SERVER_URL}/api/admin/dev/clear-telemetry"

def make_event(evt_type, session_id, seq, time_dt, **kwargs):
    time_str = time_dt.strftime("%Y-%m-%dT%H:%M:%SZ")
    e = {
        "eventId": str(uuid.uuid4()),
        "sessionId": session_id,
        "sequence": seq,
        "type": evt_type,
        "occurredAt": time_str,
    }
    for k, v in kwargs.items():
        if v is not None:
            e[k] = v
    return e

def generate_sample_players(num_players=15):
    now = datetime.now(timezone.utc)
    batches = []

    endings_pool = [
        "home", "home", "home",
        "bystander", "bystander",
        "scapegoat", "freezer",
        "red_thread_sandal", "deadline_passed"
    ]

    for p_idx in range(num_players):
        inst_id = str(uuid.uuid4())
        sess_id = str(uuid.uuid4())
        base_time = now - timedelta(days=p_idx % 7, hours=(p_idx * 3) % 24, minutes=15)
        seq = 1

        events = []
        # 1. Session start
        events.append(make_event("session_started", sess_id, seq, base_time))
        seq += 1

        # 2. Night 1 started
        t = base_time + timedelta(minutes=1)
        events.append(make_event("night_started", sess_id, seq, t, night=1, isReplay=False))
        seq += 1

        # Caught in Night 1?
        if p_idx % 2 == 0:
            t += timedelta(minutes=5)
            area = "n1.parking" if p_idx % 4 == 0 else "n1.alley"
            events.append(make_event("player_caught", sess_id, seq, t, night=1, areaId=area, checkpointId="n1.alley_start"))
            seq += 1

        # Choice in Night 1
        t += timedelta(minutes=3)
        choice = "1a" if p_idx % 3 == 0 else ("1b" if p_idx % 3 == 1 else "1c")
        events.append(make_event("choice_made", sess_id, seq, t, night=1, choiceId=choice))
        seq += 1

        # Complete Night 1?
        if p_idx < num_players - 2:
            dur = 450 + (p_idx * 35) % 300
            t += timedelta(minutes=7)
            events.append(make_event("night_completed", sess_id, seq, t, night=1, isReplay=False, durationSeconds=dur))
            seq += 1

            # Night 2
            t += timedelta(minutes=2)
            events.append(make_event("night_started", sess_id, seq, t, night=2, isReplay=False))
            seq += 1

            if p_idx % 3 == 0:
                t += timedelta(minutes=4)
                events.append(make_event("player_caught", sess_id, seq, t, night=2, areaId="n2.road", checkpointId="n2.gate"))
                seq += 1

            t += timedelta(minutes=4)
            c2 = "follow_girl" if p_idx % 2 == 0 else "hide_corner"
            events.append(make_event("choice_made", sess_id, seq, t, night=2, choiceId=c2))
            seq += 1

            # Complete Night 2?
            if p_idx < num_players - 5:
                dur2 = 600 + (p_idx * 40) % 250
                t += timedelta(minutes=8)
                events.append(make_event("night_completed", sess_id, seq, t, night=2, isReplay=False, durationSeconds=dur2))
                seq += 1

                # Night 3
                t += timedelta(minutes=3)
                events.append(make_event("night_started", sess_id, seq, t, night=3, isReplay=False))
                seq += 1

                if p_idx % 4 == 0:
                    t += timedelta(minutes=5)
                    events.append(make_event("player_caught", sess_id, seq, t, night=3, areaId="n3.gas_station", checkpointId="n3.road"))
                    seq += 1

                # Ending reached
                ending = endings_pool[p_idx % len(endings_pool)]
                t += timedelta(minutes=10)
                events.append(make_event("ending_reached", sess_id, seq, t, night=3, endingId=ending))
                seq += 1
            else:
                # Quit in night 2
                t += timedelta(minutes=6)
                events.append(make_event("session_ended", sess_id, seq, t, reason="quit_to_menu", durationSeconds=900, night=2, areaId="n2.parking"))
                seq += 1
        else:
            # Quit in night 1
            t += timedelta(minutes=4)
            events.append(make_event("session_ended", sess_id, seq, t, reason="quit_to_menu", durationSeconds=500, night=1, areaId="n1.alley"))
            seq += 1

        batches.append({
            "installationId": inst_id,
            "gameVersion": "0.3.0",
            "platform": "windows",
            "events": events
        })

    return batches

def clear_data():
    sys.stdout.reconfigure(encoding="utf-8")
    print(f"[*] Đang yêu cầu xóa sạch dữ liệu telemetry về 0...")
    req = urllib.request.Request(
        CLEAR_ENDPOINT,
        data=b"{}",
        headers={"Content-Type": "application/json; charset=utf-8"}
    )
    try:
        with urllib.request.urlopen(req) as resp:
            if resp.status == 200:
                print("[OK] Đã xóa sạch dữ liệu telemetry về 0!")
                print("[*] Mở http://localhost:5173/admin/ sẽ thấy trạng thái ban đầu (0 người chơi).")
                return True
    except urllib.error.URLError as e:
        print(f"[!] Lỗi kết nối máy chủ: {e}")
        return False

def send_batches(batches):
    sys.stdout.reconfigure(encoding="utf-8")
    print(f"[*] Đang gửi {len(batches)} lô sự kiện mẫu tới {BATCH_ENDPOINT}...")

    total_events = sum(len(b["events"]) for b in batches)
    success_count = 0

    for i, b in enumerate(batches, start=1):
        payload = json.dumps(b).encode("utf-8")
        req = urllib.request.Request(
            BATCH_ENDPOINT,
            data=payload,
            headers={"Content-Type": "application/json; charset=utf-8"}
        )
        try:
            with urllib.request.urlopen(req) as resp:
                if resp.status == 200:
                    success_count += 1
        except urllib.error.URLError as e:
            print(f"[!] Lỗi kết nối khi gửi lô {i}: {e}")
            print("    Hãy chắc chắn máy chủ API hoặc mock server đang chạy tại http://localhost:8080")
            return False

    print(f"[OK] Đã gửi thành công {success_count}/{len(batches)} lô ({total_events} sự kiện thống kê)!")
    print("[*] Mở http://localhost:5173/admin/ để xem số liệu hiển thị trên Dashboard.")
    return True

if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--clear":
        clear_data()
    else:
        batches = generate_sample_players(num_players=15)
        send_batches(batches)
