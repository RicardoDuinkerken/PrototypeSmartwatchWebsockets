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

                    // Defer Unity-safe stuff to main thread
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        Debug.Log($"[SocketProtocol] Handshake from device: {hs.deviceId}");

                        if (connection != null)
                        {
                            connection.SetDeviceId(hs.deviceId);
                        }

                        if (stream != null)
                        {
                            var responseObj = new { type = "handshake_success" };
                            string responseJson = JsonConvert.SerializeObject(responseObj) + "\n";
                            byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                            stream.Write(responseBytes, 0, responseBytes.Length);
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
}
