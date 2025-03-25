using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;

// Code for handling the connection handshake between the server and client
public class SocketProtocol
{
    [Serializable]
    private class BaseMessage
    {
        public string type;
    }

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
            BaseMessage baseMsg = JsonUtility.FromJson<BaseMessage>(json);
            if (string.IsNullOrEmpty(baseMsg.type)) return MessageType.Unknown;

            return baseMsg.type.ToLower() switch
            {
                "heartrate" => MessageType.HeartRate,
                "handshake" => MessageType.Handshake,
                _ => MessageType.Unknown,
            };
        }
        catch
        {
            return MessageType.Unknown;
        }
    }

    public static void HandleMessage(string json, NetworkStream stream = null, SocketConnection connection = null)
    {
        MessageType type = GetMessageType(json);

        switch (type)
        {
            case MessageType.HeartRate:
                HeartRateData heartRate = JsonUtility.FromJson<HeartRateData>(json);
                MainThreadDispatcher.Instance.Enqueue(() =>
                {
                    HeartRateEvents.onHeartRateReceived?.Invoke(heartRate);
                });
                break;

            case MessageType.Handshake:
                HandshakeData hs = JsonUtility.FromJson<HandshakeData>(json);
                Debug.Log($"[SocketProtocol] Handshake from device: {hs.deviceId}");

                if (connection != null)
                {
                    connection.SetDeviceId(hs.deviceId);
                }

                if (stream != null)
                {
                    string response = "handshake_success";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response + "\n");
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
                break;

            case MessageType.Unknown:
            default:
                Debug.LogWarning($"[SocketProtocol] Unknown message type: {json}");
                break;
        }
    }
}
