using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Generic/custom type for sensor
public interface ISensor<T>
{
    T GetLatestValue { get; set; }
    UnityEvent<T> OnValueReceived { get; set; }
}

// Default type for sensor (int-based sensors like heart rate)
public interface ISensor : ISensor<int> { }
