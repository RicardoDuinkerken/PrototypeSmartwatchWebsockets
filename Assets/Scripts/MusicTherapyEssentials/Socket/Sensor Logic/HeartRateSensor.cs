using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HeartRateSensor : MonoBehaviour, ISensor
{
    public static HeartRateSensor Instance { get; private set; }

    public UnityEvent<int> OnValueReceived { get; set; } = new UnityEvent<int>();

    [SerializeField]
    private int heartRate;

    [SerializeField]
    private float normalized;

    private int minHR = 40;
    private int maxHR = 210;

    public int GetLatestValue
    {
        get => heartRate;
        set
        {
            heartRate = value;
            normalized = Mathf.InverseLerp(minHR, maxHR, heartRate);
            OnValueReceived.Invoke(heartRate);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnEnable()
    {
        HeartRateEvents.onHeartRateReceived.AddListener(OnHeartRateDataReceived);
    }

    private void OnDisable()
    {
        HeartRateEvents.onHeartRateReceived.RemoveListener(OnHeartRateDataReceived);
    }

    private void OnHeartRateDataReceived(HeartRateData data)
    {
        Debug.Log($"[HeartRateSensor] HR received: {data.heartRate}");
        GetLatestValue = data.heartRate;
    }
}
