using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public static class CloudLog
{
    const string DbUrl = "https://jdyj-e5463-default-rtdb.asia-southeast1.firebasedatabase.app"; // no trailing slash
    public static bool enabled = true;

    static readonly string session = System.Guid.NewGuid().ToString("N").Substring(0, 6);
    static float lastPacketTime = -1f;
    const float PacketMinInterval = 0.2f; // throttle hold-mode spam (max 5/s)

    // Append-only event log: /events
    public static void Event(string type, string detail = "")
    {
        string json = "{\"t\":{\".sv\":\"timestamp\"},\"session\":\"" + session +
                      "\",\"type\":\"" + Esc(type) + "\",\"detail\":\"" + Esc(detail) + "\"}";
        Send("events", "POST", json);
    }

    // Haptic packet log: /packets
public static void Packet(int l, int r, int leadMs, int durMs)
{
    float now = Time.unscaledTime;
    bool isStop = (l == 0 && r == 0);
    bool isHold = (durMs == 0);
    if (!isStop && isHold && now - lastPacketTime < PacketMinInterval) return;
    lastPacketTime = now;

    string json = "{\"t\":{\".sv\":\"timestamp\"},\"session\":\"" + session +
                  "\",\"l\":" + l + ",\"r\":" + r + ",\"lead\":" + leadMs + ",\"dur\":" + durMs + "}";
    Send("packets", "POST", json);
}
    // Live game state (merged, not replaced): /state
    public static void State(string status, string entity = "", int papersDisposed = -1)
    {
        string json = "{\"session\":\"" + session + "\",\"status\":\"" + Esc(status) +
                      "\",\"entity\":\"" + Esc(entity) + "\"";
        if (papersDisposed >= 0) json += ",\"papers\":" + papersDisposed;
        json += ",\"t\":{\".sv\":\"timestamp\"}}";
        Send("state", "PATCH", json);
    }

    static void Send(string path, string method, string json)
    {
        if (!enabled) return;
        string url = DbUrl + "/" + path + ".json";
        var req = new UnityWebRequest(url, method)
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = 5
        };
        req.SetRequestHeader("Content-Type", "application/json");
        Debug.Log($"[CloudLog] {method} /{path} {json}");

        var op = req.SendWebRequest();
        op.completed += _ =>
        {
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"[CloudLog] FAIL /{path}: {req.error} ({req.responseCode})");
            req.Dispose();
        };
    }

    static string Esc(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}