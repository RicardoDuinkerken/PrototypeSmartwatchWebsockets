using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BioFeedbackManager : MonoBehaviour
{
    public static BioFeedbackManager Instance { get; private set; }

    [SerializeField]
    private TMP_Text heartRateText;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        HeartRateEvents.onHeartRateReceived.AddListener(OnHeartRateReceived);
    }

    private void OnDisable()
    {
        HeartRateEvents.onHeartRateReceived.RemoveListener(OnHeartRateReceived);
    }

    private void OnHeartRateReceived(HeartRateData data)
    {
        Debug.Log($"[BioFeedbackManager] Displaying HR: {data.heartRate}");
        heartRateText.text = $"{data.heartRate}";
    }
}
