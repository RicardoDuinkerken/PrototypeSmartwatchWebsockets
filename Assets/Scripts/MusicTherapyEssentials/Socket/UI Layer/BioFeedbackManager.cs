using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BioFeedbackManager : MonoBehaviour
{
    public static BioFeedbackManager Instance { get; private set; }

    [Header("Heart Rate Display")]
    [SerializeField] private TMP_Text heartRateText;

    [Header("Device Selection UI")]
    [SerializeField] private GameObject deviceListPanel;
    [SerializeField] private Transform deviceListContainer;
    [SerializeField] private GameObject deviceButtonPrefab;
    [SerializeField] private TMP_Text connectedText;

    private void Awake()
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
        SessionManager.DevicesUpdated += RefreshDeviceList;
    }

    private void OnDisable()
    {
        HeartRateEvents.onHeartRateReceived.RemoveListener(OnHeartRateReceived);
        SessionManager.DevicesUpdated -= RefreshDeviceList;
    }

    private void Start()
    {
        RefreshDeviceList(); // Populate on startup
    }

    private void OnHeartRateReceived(HeartRateData data)
    {
        Debug.Log($"[BiofeedbackManager] HR: {data.heartRate}");
        heartRateText.text = $"Heart Rate: {data.heartRate}";
    }

    private void RefreshDeviceList()
    {
        foreach (Transform child in deviceListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var kvp in SessionManager.GetPendingDevices())
        {
            string deviceId = kvp.Key;
            bool isActive = SessionManager.IsDeviceActive(deviceId);
            bool isConnected = SessionManager.IsDeviceConnected(deviceId);

            GameObject buttonObj = Instantiate(deviceButtonPrefab, deviceListContainer);
            TMP_Text label = buttonObj.GetComponentInChildren<TMP_Text>();
            Button btn = buttonObj.GetComponentInChildren<Button>();

            // Label formatting
            if (!isConnected)
                label.text = $"<color=red>{deviceId} (connection lost)</color>";
            else if (isActive)
                label.text = $"<b>{deviceId}</b> (active)";
            else
                label.text = deviceId;

            btn.onClick.RemoveAllListeners(); // safety
            btn.onClick.AddListener(() =>
            {
                if (isActive)
                {
                    SessionManager.StopActiveDevice();
                    SessionManager.DeactivateCurrentDevice();
                }
                else
                {
                    SessionManager.SetActive(deviceId);
                    SessionManager.StartActiveDevice();
                }

                UpdateConnectedText();
                RefreshDeviceList();
            });
        }

        UpdateConnectedText();
    }

    private void UpdateConnectedText()
    {
        if (SessionManager.HasActiveDevice())
        {
            connectedText.text = $"Connected: {SessionManager.GetActiveDeviceId()}";
        }
        else if (!string.IsNullOrEmpty(SessionManager.GetLastActiveDeviceId()) &&
                 !SessionManager.IsDeviceConnected(SessionManager.GetLastActiveDeviceId()))
        {
            connectedText.text = $"Connection Lost: {SessionManager.GetLastActiveDeviceId()}";
        }
        else
        {
            connectedText.text = "Connected: None";
        }
    }
}
