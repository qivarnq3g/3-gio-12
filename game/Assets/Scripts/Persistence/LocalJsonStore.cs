using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BaGio12.Persistence
{
    // One slot per file. A verified previous generation survives an interrupted write.
    public sealed class LocalJsonStore<T> where T : class
    {
        [Serializable] sealed class Envelope { public int version; public string payload, checksum; }
        readonly string path;
        readonly Func<T, bool> valid;
        public LocalJsonStore(string path, Func<T, bool> valid) { this.path = path; this.valid = valid; }
        static string Hash(string text)
        { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text))); }
        public bool Exists => File.Exists(path) || File.Exists(path + ".bak");
        public bool TryLoad(out T value, out string message)
        {
            value = null; message = "";
            if (Read(path, out value)) return true;
            if (Read(path + ".bak", out value)) { message = "Đã khôi phục bản lưu dự phòng."; return true; }
            if (Exists) message = "Không đọc được bản lưu hoặc phiên bản chưa được hỗ trợ. Bạn có thể chơi mới hoặc xóa bản lưu.";
            return false;
        }
        bool Read(string file, out T value)
        {
            value = null;
            try
            {
                if (!File.Exists(file) || new FileInfo(file).Length > 4 * 1024 * 1024) return false;
                var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(file));
                if (envelope == null || envelope.version != 1 || string.IsNullOrEmpty(envelope.payload) || envelope.checksum != Hash(envelope.payload)) return false;
                var data = JsonUtility.FromJson<T>(envelope.payload);
                if (data == null || !valid(data)) return false;
                value = data; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { return false; }
        }
        public bool Save(T value, out string message)
        {
            message = "";
            if (value == null || !valid(value)) { message = "Dữ liệu chưa ở điểm có thể lưu."; return false; }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string payload = JsonUtility.ToJson(value);
                var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope { version = 1, payload = payload, checksum = Hash(payload) }));
                using (var file = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                // Do not replace a good backup with a corrupt primary after recovery.
                if (Read(path, out _)) File.Copy(path, path + ".bak", true);
                if (File.Exists(path)) File.Delete(path);
                File.Move(path + ".tmp", path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { message = "Không ghi được dữ liệu trên máy. Kiểm tra dung lượng và quyền ghi rồi thử lại."; return false; }
        }
        public bool Delete(out string message)
        {
            message = "";
            try { foreach (var suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(path + suffix)) File.Delete(path + suffix); return true; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { message = "Không xóa được bản lưu. Kiểm tra quyền ghi rồi thử lại."; return false; }
        }
    }
}
