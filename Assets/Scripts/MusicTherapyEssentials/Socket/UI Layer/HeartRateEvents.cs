using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class HeartRateEvents
{
    public static UnityEvent<HeartRateData> onHeartRateReceived = new UnityEvent<HeartRateData>();
}
