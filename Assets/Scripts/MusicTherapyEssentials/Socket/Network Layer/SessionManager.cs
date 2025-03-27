using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;

public static class SessionManager
{
    private static Dictionary<string, SocketConnection> pendingDevices = new();
    private static string activeDeviceId = null;
    private static string lastActiveDeviceId = null;
    private static HashSet<string> previouslyStartedDevices = new();

    public static event System.Action DevicesUpdated;

    public static IReadOnlyDictionary<string, SocketConnection> GetPendingDevices() => pendingDevices;
    public static string GetActiveDeviceId() => activeDeviceId;
    public static string GetLastActiveDeviceId() => lastActiveDeviceId;

    public static void Register(string deviceId, SocketConnection connection)
    {
        if (activeDeviceId == null)
        {
            if (pendingDevices.ContainsKey(deviceId))
            {
                var existing = pendingDevices[deviceId];

                if (existing != null)
                {
                    Debug.LogWarning($"[SessionManager] Replacing active pending connection for {deviceId}");
                    existing.Close();
                }
                else
                {
                    Debug.Log($"[SessionManager] Replacing disconnected reference for {deviceId}");
                }

                pendingDevices[deviceId] = connection;
            }
            else
            {
                pendingDevices.Add(deviceId, connection);
                Debug.Log($"[SessionManager] Pending device registered: {deviceId}");
            }

            MainThreadDispatcher.Instance.Enqueue(() => DevicesUpdated?.Invoke());

            // ✅ Auto-reconnect and resume
            if (deviceId == lastActiveDeviceId && previouslyStartedDevices.Contains(deviceId))
            {
                Debug.Log($"[SessionManager] Re-activating and restarting device: {deviceId} after reconnect");

                SetActive(deviceId);
                StartActiveDevice();
            }
        }
        else if (deviceId == activeDeviceId)
        {
            // Already the active device — do nothing
            return;
        }
        else
        {
            Debug.LogWarning($"[SessionManager] Rejecting device {deviceId} — {activeDeviceId} is active");
            connection.Close();
        }
    }

    public static void Unregister(string deviceId)
    {
        if (pendingDevices.TryGetValue(deviceId, out var connection))
        {
            pendingDevices[deviceId] = null; // Mark as disconnected
            Debug.Log($"[SessionManager] Device marked as disconnected: {deviceId}");
        }

        if (deviceId == activeDeviceId)
        {
            Debug.Log($"[SessionManager] Active device disconnected: {deviceId}");
            activeDeviceId = null;
        }

        MainThreadDispatcher.Instance.Enqueue(() => DevicesUpdated?.Invoke());
    }

    public static void SetActive(string deviceId)
    {
        if (!pendingDevices.ContainsKey(deviceId))
        {
            Debug.LogWarning($"[SessionManager] Cannot activate {deviceId} — not pending.");
            return;
        }

        activeDeviceId = deviceId;
        lastActiveDeviceId = deviceId;

        Debug.Log($"[SessionManager] Device activated: {deviceId}");

        RejectAllExcept(deviceId);
        MainThreadDispatcher.Instance.Enqueue(() => DevicesUpdated?.Invoke());
    }

    public static void StartActiveDevice()
    {
        if (activeDeviceId != null)
        {
            SendToActive("{\"type\": \"start\"}");
            previouslyStartedDevices.Add(activeDeviceId);
            Debug.Log($"[SessionManager] Sent start to active device: {activeDeviceId}");
        }
    }

    public static void StopActiveDevice()
    {
        if (activeDeviceId != null)
        {
            SendToActive("{\"type\": \"stop\"}");
            previouslyStartedDevices.Remove(activeDeviceId);
            Debug.Log($"[SessionManager] Sent stop to active device: {activeDeviceId}");
        }
    }

    public static bool IsDeviceConnected(string deviceId)
    {
        return pendingDevices.TryGetValue(deviceId, out var connection) && connection != null;
    }

    public static void DeactivateCurrentDevice()
    {
        if (activeDeviceId != null)
        {
            Debug.Log($"[SessionManager] Deactivating device: {activeDeviceId}");
            activeDeviceId = null;
            MainThreadDispatcher.Instance.Enqueue(() => DevicesUpdated?.Invoke());
        }
    }

    private static void RejectAllExcept(string keepDeviceId)
    {
        List<string> toRemove = new();

        foreach (var kvp in pendingDevices)
        {
            string deviceId = kvp.Key;
            var conn = kvp.Value;

            if (deviceId != keepDeviceId)
            {
                Debug.Log($"[SessionManager] Rejecting device: {deviceId}");
                conn.Close();
                toRemove.Add(deviceId);
            }
        }

        foreach (var id in toRemove)
        {
            pendingDevices.Remove(id);
        }
    }

    public static bool IsDeviceActive(string deviceId)
    {
        return deviceId == activeDeviceId;
    }

    public static bool HasActiveDevice()
    {
        return activeDeviceId != null;
    }

    public static void SendToActive(string message)
    {
        if (activeDeviceId != null && pendingDevices.TryGetValue(activeDeviceId, out var connection))
        {
            connection.SendRaw(message);
            Debug.Log($"[SessionManager] Sent to {activeDeviceId}: {message}");
        }
    }

}
