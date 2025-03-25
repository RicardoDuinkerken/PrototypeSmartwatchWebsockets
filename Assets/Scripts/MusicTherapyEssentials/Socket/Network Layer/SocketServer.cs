using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

// Require component type of MainThreadDispatcher
public class SocketServer : MonoBehaviour
{
    public int port = 7474;

    private TcpListener listener;
    private Thread serverThread;
    private bool isRunning;

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
        serverThread = new Thread(ListenForConnections);
        serverThread.IsBackground = true;
        serverThread.Start();
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
                new SocketConnection(client); // spawn handler
            }
            catch (Exception e)
            {
                // Happens during shutdown, safe to ignore
                if (isRunning)
                {
                    Debug.LogError($"[SocketServer] Socket error: {e.Message}");
                }
            }
        }
    }

    public void StopServer()
    {
        isRunning = false;
        listener?.Stop();
        serverThread?.Abort();
    }
}
