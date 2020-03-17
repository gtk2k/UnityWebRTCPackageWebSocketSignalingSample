using System;
using System.Threading;
using UnityEngine;
using WebSocketSharp;

namespace WebSocketSignalingMessageHandler
{
    class BaseSignalingMessageHandler
    {
        public SynchronizationContext context;

        public BaseSignalingMessageHandler()
        {
            context = SynchronizationContext.Current;
        }

        public virtual void SendSignalingMessage(WebSocket ws, SignalingMessage msg)
        {
            if (ws == null)
                Debug.LogWarning($"[WSSignalingHandler] ws is null");
            var sendMsg = JsonUtility.ToJson(msg);
            if (string.IsNullOrWhiteSpace(sendMsg))
                Debug.LogError(new Exception("sendMsg is empty"));
            ws.Send(sendMsg);
        }

        public virtual void SendOtherTextData(WebSocket ws, string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                throw new Exception("[WSSignalingHandler] text data is empty");
            ws.Send(data);
        }

        public virtual void SendOtherBinaryData(WebSocket ws, byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new Exception("[WSSignalingHandler] binary data is empty");
            ws.Send(data);
        }
    }
}
