using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

// Require component type of MainThreadDispatcher
public class SocketServer : MonoBehaviour
{
    public static bool IsRunning { get; private set; }
    public int port = 7474;

    private TcpListener listener;
    private Thread serverThread;

    private bool isRunning;

    private readonly List<SocketConnection> activeConnections = new List<SocketConnection>();

    void Start()
    {
        MainThreadDispatcher.InitializeOnMainThread(FindObjectOfType<MainThreadDispatcher>());
        StartServer();
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    public void StartServer()
    {
        isRunning = true;
        IsRunning = true;

        serverThread = new Thread(ListenForConnections)
        {
            IsBackground = true
        };
        serverThread.Start();
        StartDiscoveryBroadcast();
    }

    private void ListenForConnections()
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Debug.Log($"[SocketServer] Listening on port {port}");

        while (isRunning)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                Debug.Log("[SocketServer] New client connected");

                var connection = new SocketConnection(client, this);
                RegisterConnection(connection);
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogError($"[SocketServer] Socket error: {e.Message}");
                }
            }
        }
    }

    public void RegisterConnection(SocketConnection connection)
    {
        lock (activeConnections)
        {
            activeConnections.Add(connection);
        }
    }

    public void StopServer()
    {
        isRunning = false;
        IsRunning = false;

        lock (activeConnections)
        {
            foreach (var connection in activeConnections)
            {
                connection.Close();
            }
            activeConnections.Clear();
        }

        listener?.Stop();
        serverThread?.Abort();
        Debug.Log("[SocketServer] Server stopped and all connections closed.");
    }

    private void StartDiscoveryBroadcast()
    {
        new Thread(() =>
        {
            using UdpClient udp = new UdpClient();
            udp.EnableBroadcast = true;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, 8888);

            while (isRunning)
            {
                string msg = $"{{\"type\":\"discovery\",\"ip\":\"{GetLocalIPAddress()}\",\"port\":{port}}}";
                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                udp.Send(bytes, bytes.Length, endPoint);
                Thread.Sleep(2000); // every 2 seconds
            }
        })
        {
            IsBackground = true
        }.Start();
    }

    private string GetLocalIPAddress()
    {
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "127.0.0.1";
    }
}
