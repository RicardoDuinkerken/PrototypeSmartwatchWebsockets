using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public class FakeHeart : MonoBehaviour
{
    [Serializable]
    public class HeartRateRange
    {
        public SimulationMode mode;
        public int minHeartRate;
        public int maxHeartRate;
    }

    public enum SimulationMode
    {
        Resting,
        Exercise,
        StressTest
    }

    [Header("Simulation Settings")]
    public SimulationMode mode = SimulationMode.Resting;
    public float updateInterval = 1.0f;
    public bool simulationEnabled = true;
    public bool randomFluctuations = true;

    [Header("Heart Rate Ranges")]
    public List<HeartRateRange> heartRateRanges = new();

    private int currentHR = 70;
    private Coroutine simulationCoroutine;
    private bool previousSimulationState;

    private void Start()
    {
        TryToggleCoroutine();
    }

    private void Update()
    {
        if (simulationEnabled != previousSimulationState)
        {
            TryToggleCoroutine();
        }
    }

    private void TryToggleCoroutine()
    {
        previousSimulationState = simulationEnabled;

        if (simulationEnabled && simulationCoroutine == null)
        {
            simulationCoroutine = StartCoroutine(SimulateHeartRate());
        }
        else if (!simulationEnabled && simulationCoroutine != null)
        {
            StopCoroutine(simulationCoroutine);
            simulationCoroutine = null;
        }
    }

    private void OnValidate()
    {
        // Ensure default ranges exist
        foreach (SimulationMode simMode in Enum.GetValues(typeof(SimulationMode)))
        {
            if (!heartRateRanges.Any(r => r.mode == simMode))
            {
                heartRateRanges.Add(GetDefaultRange(simMode));
            }
        }

        heartRateRanges = heartRateRanges
            .GroupBy(r => r.mode)
            .Select(g => g.First())
            .ToList();
    }

    private HeartRateRange GetDefaultRange(SimulationMode mode)
    {
        switch (mode)
        {
            case SimulationMode.Resting:
                return new HeartRateRange { mode = mode, minHeartRate = 55, maxHeartRate = 70 };
            case SimulationMode.Exercise:
                return new HeartRateRange { mode = mode, minHeartRate = 120, maxHeartRate = 160 };
            case SimulationMode.StressTest:
                return new HeartRateRange { mode = mode, minHeartRate = 90, maxHeartRate = 180 };
            default:
                return new HeartRateRange { mode = mode, minHeartRate = 60, maxHeartRate = 100 };
        }
    }

    private HeartRateRange GetRangeForMode(SimulationMode currentMode)
    {
        return heartRateRanges.FirstOrDefault(r => r.mode == currentMode)
               ?? GetDefaultRange(currentMode);
    }

    private IEnumerator SimulateHeartRate()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            var range = GetRangeForMode(mode);
            int minHR = range.minHeartRate;
            int maxHR = range.maxHeartRate;

            if (randomFluctuations)
            {
                currentHR = UnityEngine.Random.Range(minHR, maxHR + 1);
            }
            else
            {
                currentHR += UnityEngine.Random.value > 0.5f ? 1 : -1;
                currentHR = Mathf.Clamp(currentHR, minHR, maxHR);
            }

            var data = new HeartRateData
            {
                heartRate = currentHR,
                timestamp = DateTime.Now.ToString("HH:mm:ss")
            };

            Debug.Log($"[FakeHeart] Mode: {mode} | Simulated HR: {data.heartRate}");

            HeartRateEvents.onHeartRateReceived?.Invoke(data);
        }
    }
}
