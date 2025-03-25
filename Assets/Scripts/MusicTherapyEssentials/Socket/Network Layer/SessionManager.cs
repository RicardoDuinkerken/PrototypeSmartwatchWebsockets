using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;

public static class SessionManager
{
    private static Dictionary<string, SocketConnection> activeSessions = new Dictionary<string, SocketConnection>();

    public static void Register(string deviceId, SocketConnection connection)
    {
        if (activeSessions.ContainsKey(deviceId))
        {
            Debug.LogWarning($"[SessionManager] Replacing existing connection for {deviceId}");
            activeSessions[deviceId].Close(); // Close the old connection
        }

        activeSessions[deviceId] = connection;
        Debug.Log($"[SessionManager] Registered: {deviceId}");
    }

    public static void Unregister(string deviceId)
    {
        if (activeSessions.Remove(deviceId))
        {
            Debug.Log($"[SessionManager] Unregistered: {deviceId}");
        }
    }

    public static bool IsConnected(string deviceId)
    {
        return activeSessions.ContainsKey(deviceId);
    }
}
