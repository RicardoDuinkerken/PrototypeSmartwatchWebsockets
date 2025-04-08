using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LatencyDebugToggle : MonoBehaviour
{
    [Tooltip("Enable this to log latency values to the console.")]
    public bool enableLatencyLogging = false;

    private void OnValidate()
    {
        // Auto-update static flag whenever you toggle in the inspector
        DebugSettings.EnableLatencyLogging = enableLatencyLogging;
        Debug.Log($"[LatencyToggle] Latency logging {(enableLatencyLogging ? "ENABLED" : "DISABLED")} (via inspector)");
    }

    private void Start()
    {
        // Sync initial value at runtime
        DebugSettings.EnableLatencyLogging = enableLatencyLogging;
    }
}
