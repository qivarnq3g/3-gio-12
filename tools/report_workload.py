"""Sinh bảng phân công thực tế cho phụ lục C của báo cáo từ dữ liệu GitHub (issue, pull request, lượt duyệt).

Chạy: python tools/report_workload.py > docs/report/phan-cong.md   (cần `gh` đã đăng nhập)
Số liệu là thống kê tự động tại thời điểm chạy; không thay cho đánh giá đóng góp của nhóm.
"""
import json
import subprocess
import sys
from collections import Counter
from datetime import datetime, timezone

REPO = "qivarnq3g/3-gio-12"
MEMBERS = [("Quang", "qivarnq3g"), ("Phúc", "GiaPhucs"), ("Tiên", "tienhxit-jpg"), ("Thắng", "Thang414")]


def gh_json(*args):
    out = subprocess.run(["gh", *args], capture_output=True, check=True).stdout.decode("utf-8")
    return json.loads(out)


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    issues = gh_json("issue", "list", "-R", REPO, "--state", "all", "--limit", "300", "--json", "number,state,assignees")
    prs = gh_json("pr", "list", "-R", REPO, "--state", "all", "--limit", "200", "--json", "number,state,author,reviews")
    assigned, closed = Counter(), Counter()
    for issue in issues:
        for person in issue["assignees"]:
            assigned[person["login"]] += 1
            if issue["state"] == "CLOSED":
                closed[person["login"]] += 1
    authored, merged = Counter(), Counter()
    reviewed = Counter()
    for pr in prs:
        login = pr["author"]["login"]
        authored[login] += 1
        if pr["state"] == "MERGED":
            merged[login] += 1
        for who in {r["author"]["login"] for r in pr["reviews"] if r["author"]["login"] != login}:
            reviewed[who] += 1
    today = datetime.now(timezone.utc).astimezone().strftime("%d/%m/%Y")
    print(f"Số liệu lấy tự động từ GitHub ngày {today}: số issue được giao, số pull request đã mở và đã gộp, số pull request của người khác đã duyệt. Quang mở nhiều pull request vì phụ trách nhiều mảng (máy chủ, trang quản trị, triển khai, báo cáo) và mở thay một số pull request cho thành viên khác khi nhánh của họ chưa có pull request. Số liệu chỉ đo hoạt động trên GitHub; dựng model, viết kịch bản, chơi thử và họp nhóm không để lại dấu vết ở đây.\n")
    print("Bảng C.1. Hoạt động trên GitHub của từng thành viên\n")
    print("| Thành viên | Tài khoản GitHub | Issue được giao | Issue đã đóng | Pull request đã mở | Pull request đã gộp | Pull request của người khác đã duyệt |")
    print("|---|---|---|---|---|---|---|")
    for name, login in MEMBERS:
        print(f"| {name} | {login} | {assigned[login]} | {closed[login]} | {authored[login]} | {merged[login]} | {reviewed[login]} |")


if __name__ == "__main__":
    main()
