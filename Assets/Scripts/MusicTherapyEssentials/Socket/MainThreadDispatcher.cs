using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogWarning("[MainThreadDispatcher] Instance is null");
            }
            return _instance;
        }
    }

    private readonly Queue<Action> executionQueue = new();

    public static void InitializeOnMainThread(MainThreadDispatcher dispatcher)
    {
        if (_instance == null)
        {
            _instance = dispatcher;
            DontDestroyOnLoad(dispatcher.gameObject);
        }
    }

    public void Enqueue(Action action)
    {
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                executionQueue.Dequeue()?.Invoke();
            }
        }
    }
}
