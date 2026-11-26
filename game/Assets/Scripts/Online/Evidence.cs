using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BaGio12.Online
{
    [Serializable] public sealed class SubmissionRequest
    {
        public string installationId, gameVersion, sha256;
    }

    [Serializable] public sealed class SubmissionResponse
    {
        public string result, unlockId, windowResetsAt;
        public int attemptsRemaining;
    }

    [Serializable] public sealed class ProblemResponse
    {
        public string title;
    }

    public struct EvidenceOutcome
    {
        public string message;
        public string unlockId;
        public bool accepted;
    }

    public static class Evidence
    {
        public const string PuzzleId = "red-thread-sandal";
        public const long MaxBytes = 20L * 1024 * 1024;
        public const string OutOfAttemptsTitle = "Hết lượt gửi", TooFastTitle = "Gửi quá nhanh";

        public const string Instructions =
            "Bạn chọn đúng tệp ảnh đã giải nén. Ảnh chụp màn hình hay ảnh đã lưu lại sẽ không khớp với bằng chứng.";

        public static bool TryHash(string path, out string sha256, out string message)
        {
            sha256 = null; message = "";
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists) { message = "Game không tìm thấy tệp bạn chọn, nên bạn hãy chọn lại."; return false; }
                if (info.Length > MaxBytes) { message = "Tệp bạn chọn lớn hơn 20 MiB, nên đó không phải ảnh bằng chứng."; return false; }
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var sha = SHA256.Create())
                    sha256 = Hex(sha.ComputeHash(stream));
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            {
                message = "Game không đọc được tệp bạn chọn. Bạn hãy kiểm tra quyền đọc rồi thử lại.";
                return false;
            }
        }

        public static string Hex(byte[] bytes)
        {
            var text = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) text.Append(b.ToString("x2"));
            return text.ToString();
        }

        public static string RequestBody(string installationId, string gameVersion, string sha256) =>
            JsonUtility.ToJson(new SubmissionRequest { installationId = installationId, gameVersion = gameVersion, sha256 = sha256 });

        public static EvidenceOutcome Interpret(long status, string body, string retryAfter, DateTime nowLocal)
        {
            if (status == 200)
            {
                SubmissionResponse response = null;
                try { response = JsonUtility.FromJson<SubmissionResponse>(body ?? ""); }
                catch (ArgumentException) { }
                if (response != null && response.result == "accepted" && !string.IsNullOrEmpty(response.unlockId))
                    return new EvidenceOutcome { accepted = true, unlockId = response.unlockId,
                        message = "Công an đã nhận bằng chứng, và một kết thúc mới đã được mở." };
                if (response != null && response.result == "rejected")
                    return new EvidenceOutcome { message = response.attemptsRemaining > 0
                        ? $"Tệp này không phải bằng chứng cần tìm. Bạn còn {response.attemptsRemaining} lượt gửi."
                        : "Tệp này không phải bằng chứng cần tìm, và bạn đã dùng hết lượt gửi trong lúc này." };
                return new EvidenceOutcome { message = "Máy chủ trả lời không đúng dạng, nên bạn hãy thử lại sau." };
            }
            if (status == 404)
                return new EvidenceOutcome { message = "Máy chủ đang tạm ngừng nhận bằng chứng, nên bạn hãy thử lại sau." };
            if (status == 429)
            {
                var at = nowLocal.AddSeconds(TelemetryPolicy.RetryAfterSeconds(retryAfter));
                string when = at.ToString(at.Date == nowLocal.Date ? "HH:mm" : "HH:mm 'ngày' dd/MM", CultureInfo.InvariantCulture);
                string title = null;
                try { title = JsonUtility.FromJson<ProblemResponse>(body ?? "")?.title; }
                catch (ArgumentException) { }
                if (title == OutOfAttemptsTitle)
                    return new EvidenceOutcome { message = $"Bạn đã hết lượt gửi, nên hãy thử lại sau {when}." };
                if (title == TooFastTitle)
                    return new EvidenceOutcome { message = $"Game đã gửi quá nhiều lần trong một phút, nên bạn hãy thử lại sau {when}." };
                return new EvidenceOutcome { message = $"Bạn đã gửi quá nhiều lần, nên hãy thử lại sau {when}." };
            }
            if (status == 0)
                return new EvidenceOutcome { message = "Game cần kết nối mạng để gửi bằng chứng, nên bạn hãy kiểm tra mạng rồi thử lại." };
            return new EvidenceOutcome { message = "Máy chủ chưa xử lý được bằng chứng, nên bạn hãy thử lại sau." };
        }
    }
}
