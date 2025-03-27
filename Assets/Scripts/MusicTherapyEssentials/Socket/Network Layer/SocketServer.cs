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
    public static bool IsRunning { get; private set; }

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
        IsRunning = false;

        listener?.Stop();
        serverThread?.Abort();
    }
}
