using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;

public static class SessionManager
{
    private static Dictionary<string, SocketConnection> pendingDevices = new();
    private static string activeDeviceId = null;

    public static event System.Action DevicesUpdated;

    public static IReadOnlyDictionary<string, SocketConnection> GetPendingDevices() => pendingDevices;
    public static string GetActiveDeviceId() => activeDeviceId;

    public static void Register(string deviceId, SocketConnection connection)
    {
        if (activeDeviceId == null)
        {
            if (pendingDevices.TryGetValue(deviceId, out var existing))
            {
                Debug.LogWarning($"[SessionManager] Replacing pending connection for {deviceId}");
                existing.Close(); // 🔁 Close old connection
                pendingDevices[deviceId] = connection;
            }
            else
            {
                pendingDevices.Add(deviceId, connection);
                Debug.Log($"[SessionManager] Pending device registered: {deviceId}");
            }

            MainThreadDispatcher.Instance.Enqueue(() => DevicesUpdated?.Invoke());
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
        if (pendingDevices.Remove(deviceId))
        {
            Debug.Log($"[SessionManager] Removed pending device: {deviceId}");
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
        Debug.Log($"[SessionManager] Device activated: {deviceId}");

        RejectAllExcept(deviceId);
        MainThreadDispatcher.Instance.Enqueue(() => DevicesUpdated?.Invoke());
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
