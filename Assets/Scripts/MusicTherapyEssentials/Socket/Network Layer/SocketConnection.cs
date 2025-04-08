using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles individual TCP socket connections between the smartwatch and Unity server.
/// Tracks connection lifetime and supports logging when debugging is enabled.
/// </summary>
[Serializable]
public class SocketConnection
{
    private TcpClient client;
    private NetworkStream stream;
    private byte[] buffer = new byte[1024];
    private string deviceId;
    private SocketServer server;
    private DateTime connectionStartTime;

    private bool isClosed = false;

    private string connectionLabel => $"[{deviceId ?? "unknown"} @ {connectionStartTime:HH:mm:ss}]";

    public SocketConnection(TcpClient tcpClient, SocketServer server)
    {
        this.client = tcpClient;
        this.server = server;
        this.stream = client.GetStream();
        this.connectionStartTime = DateTime.UtcNow;

        var thread = new System.Threading.Thread(HandleClient)
        {
            IsBackground = true
        };
        thread.Start();
    }

    public bool SetDeviceId(string id)
    {
        deviceId = id;

        if (DebugSettings.EnableConnectionLogging)
        {
            Debug.Log($"[Connection] Opened [{deviceId} @ {connectionStartTime:HH:mm:ss}]");
        }

        return SessionManager.Register(id, this);
    }

    public void Close()
    {
        if (isClosed)
        {
            return; // already closed — no-op
        }
        
        isClosed = true;

        try
        {
            stream?.Close();
            client?.Close();
            Debug.Log("[SocketConnection] Closed socket.");

            if (DebugSettings.EnableConnectionLogging)
            {
                var duration = DateTime.UtcNow - connectionStartTime;
                Debug.Log($"[Connection] Closed {connectionLabel} after {duration.TotalSeconds:F1} seconds");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SocketConnection] Error closing socket: {e.Message}");
        }

        if (!string.IsNullOrEmpty(deviceId))
        {
            SessionManager.Unregister(deviceId);
        }
    }

    public void SendRaw(string message)
    {
        try
        {
            if (client.Connected)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message.Trim() + "\n");
                stream.Write(bytes, 0, bytes.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SocketConnection] Failed to send: {e.Message}");
        }
    }

    private void HandleClient()
    {
        try
        {
            int byteCount;
            while (SocketServer.IsRunning && (byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string incoming = Encoding.UTF8.GetString(buffer, 0, byteCount);
                string[] messages = incoming.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string message in messages)
                {
                    SocketProtocol.HandleMessage(message.Trim(), stream, this);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SocketConnection] Connection error: {e.Message}");
        }
        finally
        {
            Close();
        }
    }
}
