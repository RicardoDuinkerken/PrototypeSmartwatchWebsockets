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

        var thread = new System.Threading.Thread(HandleClient);
        thread.IsBackground = true;
        thread.Start();
    }

    public void SetDeviceId(string id)
    {
        deviceId = id;
        SessionManager.Register(id, this);
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

    private void HandleClient()
    {
        try
        {
            int byteCount;
            while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string json = Encoding.UTF8.GetString(buffer, 0, byteCount).Trim();
                Debug.Log($"[SocketConnection] Received: {json}");

                SocketProtocol.HandleMessage(json, stream, this);
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
