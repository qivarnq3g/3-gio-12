using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BaGio12.Online;
using BaGio12.Persistence;
using BaGio12.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class OnlineServicesRegression
{
    sealed class Reply { public int status; public string body = "", retryAfter; }
    sealed class Received { public string method, path, body; }

    static HttpListener listener;
    static readonly ConcurrentQueue<Received> received = new ConcurrentQueue<Received>();
    static readonly ConcurrentDictionary<string, Reply> replies = new ConcurrentDictionary<string, Reply>();
    static string root, baseUrl;
    static OnlineServices online;
    static GameObject host;
    static readonly List<string> reports = new List<string>();
    static int step;
    static double deadline;

    static OnlineServicesRegression()
    { if (SessionState.GetBool("OnlineReview", false)) EditorApplication.playModeStateChanged += Changed; }

    static void Check(bool pass, string message) { if (!pass) throw new Exception("ONLINE REGRESSION: " + message); }

    public static void Run()
    {
        LogicCases();
        SessionState.SetBool("OnlineReview", true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        EditorApplication.EnterPlaymode();
    }

    static void LogicCases()
    {
        var data = new TelemetryQueueData();
        var now = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < TelemetryQueue.Capacity + 3; i++) TelemetryQueue.Add(data, "{\"i\":" + i + "}", now);
        Check(data.events.Count == TelemetryQueue.Capacity && data.events[0].json == "{\"i\":3}", "capacity did not drop the oldest events");
        TelemetryQueue.Add(data, "{\"old\":1}", now.AddDays(-31));
        data.events.Add(new QueuedEvent { json = "{\"bad\":1}", occurredAt = "not a date" });
        Check(TelemetryQueue.Prune(data, now) == 2 && data.events.Count == TelemetryQueue.Capacity - 1, "prune kept old or unreadable events");

        string id = Guid.NewGuid().ToString();
        Check(TelemetryQueue.BatchSize(data, id, "0.1.0", 500) == TelemetryQueue.MaxEvents, "batch exceeded 100 events");
        Check(TelemetryQueue.BatchSize(data, id, "0.1.0", 7) == 7, "batch ignored the reduced size");
        var big = new TelemetryQueueData();
        string payload = "{\"pad\":\"" + new string('x', 100 * 1024) + "\"}";
        for (int i = 0; i < 4; i++) TelemetryQueue.Add(big, payload, now);
        int count = TelemetryQueue.BatchSize(big, id, "0.1.0", 100);
        Check(count == 2 && Encoding.UTF8.GetByteCount(TelemetryQueue.Envelope(id, "0.1.0", big.events, 0, count)) <= TelemetryQueue.MaxBatchBytes, "batch exceeded 256 KiB");
        string envelope = TelemetryQueue.Envelope(id, "0.1.0", data.events, 0, 2);
        Check(envelope == "{\"installationId\":\"" + id + "\",\"gameVersion\":\"0.1.0\",\"platform\":\"windows\",\"events\":[{\"i\":4},{\"i\":5}]}", "envelope shape: " + envelope);

        var ok = TelemetryPolicy.Decide(200, null, 40, 20, 120);
        Check(ok.remove == 40 && ok.maxEvents == 100 && ok.backoffSeconds == 0 && ok.waitSeconds == 0, "200 did not clear the batch");
        var bad = TelemetryPolicy.Decide(400, null, 40, 100, 0);
        Check(bad.remove == 40 && bad.logError, "400 did not drop and log");
        var large = TelemetryPolicy.Decide(413, null, 40, 100, 0);
        Check(large.remove == 0 && large.maxEvents == 20, "413 did not halve the batch");
        var single = TelemetryPolicy.Decide(413, null, 1, 1, 0);
        Check(single.remove == 1 && single.logError, "413 on one event looped forever");
        var limited = TelemetryPolicy.Decide(429, "17", 40, 100, 0);
        Check(limited.remove == 0 && limited.waitSeconds == 17, "429 ignored Retry-After");
        var first = TelemetryPolicy.Decide(503, null, 40, 100, 0);
        var second = TelemetryPolicy.Decide(0, null, 40, 100, first.backoffSeconds);
        var capped = TelemetryPolicy.Decide(500, null, 40, 100, 1500);
        Check(first.waitSeconds == 30 && second.waitSeconds == 60 && capped.waitSeconds == 1800 && first.remove == 0, "backoff is not 30 s doubling to 30 min");

        var at = new DateTime(2026, 11, 25, 20, 0, 0);
        Check(Evidence.Interpret(200, "{\"result\":\"accepted\",\"unlockId\":\"ending_red_thread_sandal\",\"attemptsRemaining\":5,\"windowResetsAt\":\"2026-11-25T13:00:00Z\"}", null, at) is var yes && yes.accepted && yes.unlockId == PlayerProfile.SandalUnlock, "accepted response not unlocked");
        Check(Evidence.Interpret(200, "{\"result\":\"rejected\",\"attemptsRemaining\":3,\"windowResetsAt\":\"2026-11-25T13:00:00Z\"}", null, at) is var no && !no.accepted && no.message.Contains("3 lượt"), "rejected response did not show attempts left");
        Check(Evidence.Interpret(429, "", "600", at).message.Contains("20:10"), "429 did not show the retry time");
        Check(!Evidence.Interpret(404, "", null, at).accepted && !Evidence.Interpret(0, "", null, at).accepted, "error statuses unlocked");

        string dir = Path.Combine(Path.GetTempPath(), "3gio12-online-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string abc = Path.Combine(dir, "abc.jpg"); File.WriteAllText(abc, "abc");
            Check(Evidence.TryHash(abc, out var sha, out _) && sha == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", "SHA-256 mismatch");
            string huge = Path.Combine(dir, "huge.jpg");
            using (var file = new FileStream(huge, FileMode.Create)) file.SetLength(Evidence.MaxBytes + 1);
            Check(!Evidence.TryHash(huge, out _, out var message) && message.Contains("20 MiB"), "file above 20 MiB was hashed");
            Check(!Evidence.TryHash(Path.Combine(dir, "missing.jpg"), out _, out _), "missing file was hashed");
            Check(Evidence.RequestBody(id, "0.1.0", sha) == "{\"installationId\":\"" + id + "\",\"gameVersion\":\"0.1.0\",\"sha256\":\"" + sha + "\"}", "submission body shape");
        }
        finally { Directory.Delete(dir, true); }

        var profile = PlayerProfile.Create();
        Check(PlayerProfile.Valid(profile) && profile.Consent == TelemetryConsent.Unanswered, "new profile invalid or pre-consented");
        profile.pendingRevocations.Add("not-a-guid");
        Check(!PlayerProfile.Valid(profile), "invalid revocation id accepted");
        Debug.Log("ONLINE_LOGIC_CASES_PASSED");
    }

    static void Changed(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            Application.runInBackground = true;
            StartServer();
            root = Path.GetFullPath(Path.Combine("Library", "OnlineReview", Guid.NewGuid().ToString("N")));
            Environment.SetEnvironmentVariable("THREEGIO12_API_BASE", baseUrl);
            Spawn();
            step = 0; deadline = EditorApplication.timeSinceStartup + 60;
            EditorApplication.update += Tick;
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }

    static void Spawn()
    {
        host = new GameObject("Online services");
        online = host.AddComponent<OnlineServices>();
        online.Initialize(root);
    }

    static void StartServer()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0); probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
        baseUrl = "http://localhost:" + port;
        listener = new HttpListener();
        listener.Prefixes.Add(baseUrl + "/");
        listener.Start();
        listener.BeginGetContext(Serve, null);
    }

    static void Serve(IAsyncResult result)
    {
        HttpListenerContext context;
        try { context = listener.EndGetContext(result); }
        catch (Exception) { return; }
        try { listener.BeginGetContext(Serve, null); } catch (Exception) { }
        string body;
        using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8)) body = reader.ReadToEnd();
        string path = context.Request.Url.AbsolutePath;
        received.Enqueue(new Received { method = context.Request.HttpMethod, path = path, body = body });
        var key = replies.Keys.FirstOrDefault(k => path.StartsWith(k, StringComparison.Ordinal));
        var reply = key != null ? replies[key] : new Reply { status = 500 };
        context.Response.StatusCode = reply.status;
        if (reply.retryAfter != null) context.Response.AddHeader("Retry-After", reply.retryAfter);
        var bytes = Encoding.UTF8.GetBytes(reply.body);
        context.Response.ContentType = "application/json";
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        context.Response.Close();
    }

    static Received Take(string method, string path)
    {
        while (received.TryDequeue(out var r)) if (r.method == method && r.path.StartsWith(path, StringComparison.Ordinal)) return r;
        return null;
    }

    static Received waiting;

    static bool Await(string method, string path)
    {
        if (waiting == null) waiting = Take(method, path);
        return waiting != null;
    }

    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("ONLINE REGRESSION: timed out at step " + step);
            switch (step)
            {
                case 0:
                    Check(online.AskConsent && online.QueuedEvents == 0, "unanswered consent queued events");
                    online.Record(new ChoiceTelemetry("3"));
                    Check(online.QueuedEvents == 0, "event queued without consent");
                    replies["/api/v1/telemetry/batches"] = new Reply { status = 200, body = "{\"accepted\":1,\"duplicates\":0,\"rejected\":[]}" };
                    online.SetConsent(true);
                    Check(online.QueuedEvents == 1 && online.Profile.openSessionId == NightTelemetry.CurrentSession, "consent did not start a session");
                    step++; break;
                case 1:
                    if (!Await("POST", "/api/v1/telemetry/batches") || online.Uploading) return;
                    Check(waiting.body.Contains("\"platform\":\"windows\"") && waiting.body.Contains(online.Profile.installationId) && waiting.body.Contains("\"type\":\"session_started\""), "batch body: " + waiting.body);
                    Check(online.QueuedEvents == 0, "200 left events queued");
                    waiting = null;
                    online.Record(new EndingTelemetry(PlayerProfile.HomeEnding));
                    online.Record(new NightCompletedTelemetry(false, 600));
                    Check(online.CanSubmitEvidence, "home ending did not enable evidence");
                    step++; break;
                case 2:
                    if (!Await("POST", "/api/v1/telemetry/batches") || online.Uploading) return;
                    Check(waiting.body.Contains("ending_reached") && waiting.body.Contains("night_completed") && online.QueuedEvents == 0, "night_completed did not flush the queue");
                    waiting = null;
                    var staleId = Guid.NewGuid().ToString();
                    online.Profile.openSessionId = staleId; online.Profile.openSessionNextSequence = 42;
                    var baseTime = DateTime.UtcNow;
                    online.Profile.openSessionStartedAt = baseTime.AddMinutes(-10).ToString("O");
                    online.Profile.openSessionLastSeenAt = baseTime.AddMinutes(-4).ToString("O");
                    new LocalJsonStore<PlayerProfile>(Path.Combine(root, "profile.json"), PlayerProfile.Valid).Save(online.Profile, out _);
                    replies["/api/v1/telemetry/batches"] = new Reply { status = 503 };
                    UnityEngine.Object.DestroyImmediate(host);
                    Spawn();
                    Check(online.CanSubmitEvidence, "home ending flag not persisted");
                    step++; break;
                case 3:
                    if (!Await("POST", "/api/v1/telemetry/batches") || online.Uploading) return;
                    Check(waiting.body.Contains("\"reason\":\"not_closed\"") && waiting.body.Contains("\"sequence\":42") && waiting.body.Contains("\"durationSeconds\":360"), "not_closed event: " + waiting.body);
                    Check(online.QueuedEvents == 2, "503 dropped queued events");
                    waiting = null;
                    replies["/api/v1/telemetry/installations/"] = new Reply { status = 204 };
                    string revoked = online.Profile.installationId;
                    online.SetConsent(false);
                    Check(online.QueuedEvents == 0 && online.Profile.pendingRevocations.Contains(revoked), "withdrawal kept queue or skipped revocation");
                    online.Record(new ChoiceTelemetry("4"));
                    Check(online.QueuedEvents == 0, "event queued after withdrawal");
                    step++; break;
                case 4:
                    if (!Await("DELETE", "/api/v1/telemetry/installations/" + online.Profile.installationId) || online.Uploading) return;
                    Check(online.Profile.pendingRevocations.Count == 0, "revocation not cleared after 204");
                    waiting = null;
                    string file = Path.Combine(root, "evidence.jpg"); File.WriteAllText(file, "abc");
                    replies["/api/v1/puzzles/"] = new Reply { status = 200, body = "{\"result\":\"rejected\",\"attemptsRemaining\":4,\"windowResetsAt\":\"2026-11-25T13:00:00Z\"}" };
                    reports.Clear(); online.StartCoroutine(online.Submit(file, reports.Add));
                    step++; break;
                case 5:
                    if (online.Submitting || !Await("POST", "/api/v1/puzzles/red-thread-sandal/submissions")) return;
                    Check(waiting.body.Contains("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad") && !waiting.body.Contains("abc\""), "submission did not send only the hash: " + waiting.body);
                    Check(reports.Last().Contains("4 lượt") && !online.Profile.HasUnlock(PlayerProfile.SandalUnlock), "rejection reported wrong");
                    waiting = null;
                    replies["/api/v1/puzzles/"] = new Reply { status = 200, body = "{\"result\":\"accepted\",\"unlockId\":\"ending_red_thread_sandal\",\"attemptsRemaining\":4,\"windowResetsAt\":\"2026-11-25T13:00:00Z\"}" };
                    online.StartCoroutine(online.Submit(Path.Combine(root, "evidence.jpg"), reports.Add));
                    step++; break;
                case 6:
                    if (online.Submitting || !Await("POST", "/api/v1/puzzles/")) return;
                    Check(online.Profile.HasUnlock(PlayerProfile.SandalUnlock), "accepted evidence not saved");
                    var reloaded = new LocalJsonStore<PlayerProfile>(Path.Combine(root, "profile.json"), PlayerProfile.Valid);
                    Check(reloaded.TryLoad(out var saved, out _) && saved.HasUnlock(PlayerProfile.SandalUnlock) && saved.Consent == TelemetryConsent.Declined, "unlock or consent not on disk");
                    waiting = null;
                    replies.Clear();
                    online.StartCoroutine(online.Submit(Path.Combine(root, "evidence.jpg"), reports.Add));
                    step++; break;
                case 7:
                    if (online.Submitting || !Await("POST", "/api/v1/puzzles/")) return;
                    Check(reports.Last().Contains("thử lại sau"), "server error not reported: " + reports.Last());
                    Debug.Log("ONLINE_SERVICES_REGRESSION_PASSED");
                    Finish(0); return;
            }
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }

    static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool("OnlineReview", false);
        try { listener?.Stop(); } catch (Exception) { }
        Environment.SetEnvironmentVariable("THREEGIO12_API_BASE", null);
        try { if (root != null && Directory.Exists(root)) Directory.Delete(root, true); } catch (IOException) { }
        EditorApplication.Exit(code);
    }
}
