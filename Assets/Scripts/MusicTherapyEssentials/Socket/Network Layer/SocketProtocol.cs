using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Code for handling the connection handshake between the server and client
public class SocketProtocol
{
    [Serializable]
    public class HandshakeData
    {
        public string type;
        public string deviceId;
    }

    public enum MessageType
    {
        Unknown,
        HeartRate,
        Handshake
    }

    private const string HandshakeMessage = "UNITY_HEART_RATE_SERVER_MMPRO_RG1fCEaI307MPsMD";
    private const string HandshakeResponse = "DEVICE_HEART_RATE_CLIENT_MMPRO_CPmfpz9OzsVQKsdt";

    public static bool IsValidHandshake(string input)
    {
        return input.Trim() == HandshakeMessage;
    }

    public static byte[] GetHandshakeResponseBytes()
    {
        return Encoding.UTF8.GetBytes(HandshakeResponse);
    }

    public static MessageType GetMessageType(string json)
    {
        try
        {
            JObject obj = JObject.Parse(json);
            string type = (string)obj["type"];

            return type?.ToLower() switch
            {
                "heartrate" => MessageType.HeartRate,
                "handshake" => MessageType.Handshake,
                _ => MessageType.Unknown
            };
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SocketProtocol] Failed to parse message type: {e.Message}");
            return MessageType.Unknown;
        }
    }

 public static void HandleMessage(string json, NetworkStream stream = null, SocketConnection connection = null)
    {
        MessageType type = GetMessageType(json);

        switch (type)
        {
            case MessageType.HeartRate:
                try
                {
                    HeartRateData heartRate = JsonConvert.DeserializeObject<HeartRateData>(json);

                    TryLogLatency(type, heartRate.deviceId, heartRate.timestamp );

                    // 🧠 Only allow data from the active device
                    if (!SessionManager.IsDeviceActive(heartRate.deviceId))
                    {
                        Debug.Log($"[SocketProtocol] Ignoring HR from {heartRate.deviceId} (not active)");
                        return;
                    }

                    var dispatcher = MainThreadDispatcher.Instance;
                    if (dispatcher != null)
                    {
                        dispatcher.Enqueue(() =>
                        {
                            HeartRateEvents.onHeartRateReceived?.Invoke(heartRate);
                        });
                    }
                    else
                    {
                        Debug.LogWarning("[SocketProtocol] Skipped dispatching HR event — dispatcher is null (probably exiting).");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SocketProtocol] Failed to parse HeartRateData: {e.Message}");
                }
                break;

            case MessageType.Handshake:
                try
                {
                    HandshakeData hs = JsonConvert.DeserializeObject<HandshakeData>(json);

                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        Debug.Log($"[SocketProtocol] Handshake from device: {hs.deviceId}");

                        bool accepted = false;
                        if (connection != null)
                        {
                            accepted = connection.SetDeviceId(hs.deviceId);
                        }

                        if (accepted && stream != null)
                        {
                            try
                            {
                                var responseObj = new { type = "handshake_success" };
                                string responseJson = JsonConvert.SerializeObject(responseObj) + "\n";
                                byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                                stream.Write(responseBytes, 0, responseBytes.Length);
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"[SocketProtocol] Stream already closed while responding to handshake: {e.Message}");
                            }
                        }
                    });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SocketProtocol] Failed to parse HandshakeData: {e.Message}");
                }
                break;

            case MessageType.Unknown:
            default:
                Debug.LogWarning($"[SocketProtocol] Unknown message type: {json}");
                break;
        }
    }

    private static bool TryLogLatency(MessageType type, string deviceId, string timestamp)
    {
        if (!DebugSettings.EnableLatencyLogging || string.IsNullOrEmpty(timestamp))
        {
            return false;
        }

        try
        {
            var receivedTime = DateTime.UtcNow;
            var sentTime = DateTime.ParseExact(timestamp, "yyyy-MM-ddTHH:mm:ss.fffZ", null, System.Globalization.DateTimeStyles.AdjustToUniversal);
            var latency = (receivedTime - sentTime).TotalMilliseconds;

            Debug.Log($"[Latency] {type} latency from {deviceId}: {latency} ms");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Latency] Failed to parse timestamp: {e.Message}");
            return false;
        }
    }
}
