using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class SocketConnection
{
    private TcpClient client;
    private NetworkStream stream;
    private byte[] buffer = new byte[1024];

    private string deviceId;

    public SocketConnection(TcpClient tcpClient)
    {
        client = tcpClient;
        stream = client.GetStream();

        var thread = new System.Threading.Thread(HandleClient)
        {
            IsBackground = true
        };
        thread.Start();
    }

    public bool SetDeviceId(string id)
    {
        deviceId = id;
        return SessionManager.Register(id, this);
    }

    public void Close()
    {
        try
        {
            stream?.Close();
            client?.Close();
        }
        catch { }

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
