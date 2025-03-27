using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HeartRateData
{
    public string type;
    public string deviceId;     //ID of the device sending this data
    public int heartRate;       //Heart rate in BPM
    public string timestamp;    //ISO 8601 format timestamp (e.g., "2025-03-27T19:25:00Z")
}
