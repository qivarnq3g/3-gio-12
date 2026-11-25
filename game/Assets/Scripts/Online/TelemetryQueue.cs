using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BaGio12.Online
{
    [Serializable] public sealed class QueuedEvent
    {
        public string json;
        public string occurredAt;
    }

    [Serializable] public sealed class TelemetryQueueData
    {
        public int schemaVersion = 1;
        public List<QueuedEvent> events = new List<QueuedEvent>();

        public static bool Valid(TelemetryQueueData d) =>
            d != null && d.schemaVersion == 1 && d.events != null && d.events.TrueForAll(e => e != null && !string.IsNullOrEmpty(e.json));
    }

    public static class TelemetryQueue
    {
        public const int Capacity = 5000;
        public const int MaxEvents = 100;
        public const int MaxBatchBytes = 256 * 1024;
        public const int SendThreshold = 50;
        public static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);

        public static void Add(TelemetryQueueData data, string json, DateTime occurredAtUtc)
        {
            data.events.Add(new QueuedEvent { json = json, occurredAt = occurredAtUtc.ToString("O", CultureInfo.InvariantCulture) });
            if (data.events.Count > Capacity) data.events.RemoveRange(0, data.events.Count - Capacity);
        }

        public static int Prune(TelemetryQueueData data, DateTime nowUtc)
        {
            DateTime oldest = nowUtc - MaxAge;
            return data.events.RemoveAll(e => !TryParse(e.occurredAt, out var at) || at < oldest);
        }

        static bool TryParse(string value, out DateTime at) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal, out at);

        public static string Envelope(string installationId, string gameVersion, IList<QueuedEvent> events, int start, int count)
        {
            var body = new StringBuilder(Head(installationId, gameVersion));
            for (int i = 0; i < count; i++)
            {
                if (i > 0) body.Append(',');
                body.Append(events[start + i].json);
            }
            return body.Append("]}").ToString();
        }

        static string Head(string installationId, string gameVersion) =>
            "{\"installationId\":\"" + installationId + "\",\"gameVersion\":\"" + gameVersion + "\",\"platform\":\"windows\",\"events\":[";

        public static int BatchSize(TelemetryQueueData data, string installationId, string gameVersion, int maxEvents)
        {
            int limit = Math.Min(Math.Min(maxEvents, MaxEvents), data.events.Count);
            if (limit <= 0) return 0;
            int bytes = Encoding.UTF8.GetByteCount(Head(installationId, gameVersion)) + 2;
            int count = 0;
            while (count < limit)
            {
                int next = Encoding.UTF8.GetByteCount(data.events[count].json) + (count > 0 ? 1 : 0);
                if (count > 0 && bytes + next > MaxBatchBytes) break;
                bytes += next;
                count++;
            }
            return count;
        }
    }

    public struct UploadDecision
    {
        public int remove;
        public int maxEvents;
        public double waitSeconds;
        public double backoffSeconds;
        public bool logError;
    }

    public static class TelemetryPolicy
    {
        public const double FirstBackoff = 30, MaxBackoff = 30 * 60, DefaultRetryAfter = 60;

        public static UploadDecision Decide(long status, string retryAfter, int batchCount, int maxEvents, double backoffSeconds)
        {
            var d = new UploadDecision { maxEvents = maxEvents, backoffSeconds = backoffSeconds };
            if (status >= 200 && status < 300)
            {
                d.remove = batchCount; d.maxEvents = TelemetryQueue.MaxEvents; d.backoffSeconds = 0;
            }
            else if (status == 400)
            {
                d.remove = batchCount; d.logError = true; d.backoffSeconds = 0;
            }
            else if (status == 413)
            {
                if (batchCount <= 1) { d.remove = batchCount; d.logError = true; }
                else d.maxEvents = Math.Max(1, batchCount / 2);
            }
            else if (status == 429)
            {
                d.waitSeconds = RetryAfterSeconds(retryAfter);
            }
            else
            {
                d.backoffSeconds = backoffSeconds <= 0 ? FirstBackoff : Math.Min(MaxBackoff, backoffSeconds * 2);
                d.waitSeconds = d.backoffSeconds;
            }
            return d;
        }

        public static double RetryAfterSeconds(string header)
        {
            if (!string.IsNullOrWhiteSpace(header) && double.TryParse(header.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
                return seconds;
            return DefaultRetryAfter;
        }
    }
}
