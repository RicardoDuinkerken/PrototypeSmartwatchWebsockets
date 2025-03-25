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
                Debug.LogError("MainThreadDispatcher not initialized. Make sure it's present in the scene before starting server.");
            }
            return _instance;
        }
    }

    private readonly Queue<Action> executionQueue = new Queue<Action>();

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
