using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows toggling debug logging features (latency, connection stability)
/// via the Unity Inspector during testing sessions.
/// </summary>
public class LatencyDebugToggle : MonoBehaviour
{
    [Header("Enable this to log latency values to the console.")]
    public bool enableLatencyLogging = false;

    [Header("Enable to log when connections are opened/closed/reconnected")]
    public bool enableConnectionLogging = false;

    private void OnValidate()
    {
        ApplyDebugSettings();
    }

    private void Start()
    {
        ApplyDebugSettings();
    }

    private void ApplyDebugSettings()
    {
        DebugSettings.EnableLatencyLogging = enableLatencyLogging;
        DebugSettings.EnableConnectionLogging = enableConnectionLogging;

        Debug.Log($"[DebugToggle] Latency logging: {(enableLatencyLogging ? "ENABLED" : "DISABLED")}, " +
                  $"Connection logging: {(enableConnectionLogging ? "ENABLED" : "DISABLED")}");
    }
}
