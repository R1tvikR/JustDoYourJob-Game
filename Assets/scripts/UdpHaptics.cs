using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// Sends haptic commands to the ESP32 vest over UDP.
/// Packet (6 bytes): [left 0-255][right 0-255][lead_ms u16 LE][duration_ms u16 LE]
/// Hold mode (duration 0): updates are rate-limited and re-sent as keep-alives.
/// One-shot (duration > 0 or lead > 0): sent immediately, once.
public class UdpHaptics : MonoBehaviour
{
    public static UdpHaptics Instance { get; private set; }



    [Header("ESP32")]
    public string esp32Ip = "192.168.0.100";
    public int port = 4210;

    [Header("Timing")]
    [Tooltip("Max rate for hold-mode updates")]
    public float minSendInterval = 0.033f;
    [Tooltip("Must stay well under the ESP32 watchdog (500 ms)")]
    public float keepAliveInterval = 0.2f;
    [Tooltip("Predictive lead used by one-shots when no lead is passed")]
    public int defaultLeadMs = 0;

    UdpClient client;
    IPEndPoint endpoint;

    byte wantL, wantR;       // desired hold state
    byte sentL, sentR;       // last hold state sent
    float lastSendTime = -10f;
         float holdBlockedUntil; 

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[UdpHaptics] Duplicate instance, destroying this one");
            Destroy(this);
            return;
        }
        Instance = this;

        if (!IPAddress.TryParse(esp32Ip, out var ip))
        {
            Debug.LogError($"[UdpHaptics] Invalid IP '{esp32Ip}'");
            enabled = false;
            return;
        }

        try
        {
            endpoint = new IPEndPoint(ip, port);
            client = new UdpClient();
            Debug.Log($"[UdpHaptics] Ready -> {esp32Ip}:{port}");
    
        }
        catch (Exception e)
        {
            Debug.LogError($"[UdpHaptics] Socket error: {e.Message}");
            enabled = false;
        }
    }

    void OnDisable()
    {
        if (Instance == this)
        {
            Transmit(0, 0, 0, 0);
            client?.Close();
            client = null;
            Instance = null;
            Debug.Log("[UdpHaptics] Closed");
        }
    }

    void Update()
    {
        if (client == null) return;
        float now = Time.unscaledTime;
        if (now < holdBlockedUntil) return;
        bool changed = wantL != sentL || wantR != sentR;
        bool active = wantL != 0 || wantR != 0;

        if (changed && now - lastSendTime >= minSendInterval)
            Flush(now);
        else if (!changed && active && now - lastSendTime >= keepAliveInterval)
            Flush(now); // keep-alive
    }

    void Flush(float now)
    {
        Transmit(wantL, wantR, 0, 0);
        sentL = wantL; sentR = wantR;
        lastSendTime = now;
    }

    // ---------- Public static API ----------

    /// Continuous hold. Values 0..1. Call every frame or on change; call Stop() or send 0,0 to end.
    public static void Send(float left, float right)
    {
        if (Instance == null) return;
        Instance.wantL = To8(left);
        Instance.wantR = To8(right);
    }

    /// One-shot pulse. leadMs < 0 uses defaultLeadMs.
    public static void Pulse(float left, float right, int durationMs, int leadMs = -1)
    {
        if (Instance == null || Instance.client == null) return;
        int lead = leadMs < 0 ? Instance.defaultLeadMs : leadMs;
        Instance.holdBlockedUntil = Time.unscaledTime + (lead + durationMs) / 1000f;
        Instance.Transmit(To8(left), To8(right), (ushort)Mathf.Clamp(lead, 0, 65535),
                          (ushort)Mathf.Clamp(durationMs, 1, 65535));
    }

    public static void Stop()
    {
        if (Instance == null) return;
        Instance.wantL = 0; Instance.wantR = 0;
        Instance.Transmit(0, 0, 0, 0);
        Instance.sentL = 0; Instance.sentR = 0;
          Instance.holdBlockedUntil = 0f;
    }

    // ---------- Internals ----------

    static byte To8(float v) => (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);

    void Transmit(byte l, byte r, ushort leadMs, ushort durMs)
    {
        if (client == null) return;
        var buf = new byte[6]
        {
            l, r,
            (byte)(leadMs & 0xFF), (byte)(leadMs >> 8),
            (byte)(durMs & 0xFF),  (byte)(durMs >> 8)
        };
    try
{
    client.Send(buf, buf.Length, endpoint);
    Debug.Log($"[UdpHaptics] TX L={l} R={r} lead={leadMs} dur={durMs}");
    CloudLog.Packet(l, r, leadMs, durMs);   // <- add this line here
}
catch (Exception e)
{
    Debug.LogWarning($"[UdpHaptics] Send failed: {e.Message}");
}
    }
}