using System;
using System.Collections.Generic;
using System.Threading;
using WebSocketSignalingMessageHandler;
using WebSocketSharp;
using UnityEngine;

namespace WebSocketSignalingMessageHandler
{
    class WebSocketClientSignalingMessageHandler : BaseSignalingMessageHandler
    {
        public delegate void dlgOnClientConnect(string clientId);
        public delegate void dlgOnClientDisconnect(string clientId);
        public delegate void dlgOnIceCandidate(string clientId, string candidate, string sdpMid, int sdpMLineIndex);
        public delegate void dlgOnOffer(string clientId, string sdp);
        public delegate void dlgOnAnswer(string clientId, string sdp);
        public delegate void dlgOnDescription(string clientId, string type, string sdp);
        public delegate void dlgOnError(string clientId, Exception ex);

        public event dlgOnClientConnect OnClientConnect;
        public event dlgOnClientDisconnect OnClientDisconnect;
        public event dlgOnIceCandidate OnIceCandidate;
        public event dlgOnOffer OnOffer;
        public event dlgOnAnswer OnAnswer;
        public event dlgOnDescription OnDescription;
        public event dlgOnError OnError;

        public WebSocket ws;

        public WebSocketClientSignalingMessageHandler() : base()
        {
        }

        public virtual void Init(string url, string[] protocols = null)
        {
            if (protocols == null)
                ws = new WebSocket(url);
            else
                ws = new WebSocket(url, protocols);

            ws.OnMessage += onMessage;
        }

        public virtual void onMessage(object sender, MessageEventArgs e)
        {
            context.Post(_ =>
            {
                if (e.IsText)
                {
                    if (e.Data.StartsWith("{") && e.Data.EndsWith("}"))
                    {
                        var msg = JsonUtility.FromJson<SignalingMessage>(e.Data);
                        if (msg.type == "clientconnect")
                        {
                            OnClientConnect?.Invoke(msg.clientId);
                        }
                        else if (msg.type != null && (msg.type == "offer" || msg.type == "answer"))
                        {
                            OnDescription?.Invoke(msg.clientId, msg.type, msg.sdp);
                            if (msg.type == "offer")
                                OnOffer?.Invoke(msg.clientId, msg.sdp);
                            else if (msg.type == "answer")
                                OnAnswer?.Invoke(msg.clientId, msg.sdp);
                            return;
                        }
                        else if (msg.type != null && msg.type == "candidate")
                        {
                            OnIceCandidate?.Invoke(msg.clientId, msg.candidate, msg.sdpMid, msg.sdpMLineIndex);
                            return;
                        }
                    }
                }
            }, null);
        }

        public virtual void Connect()
        {
            if (ws != null)
                ws.Connect();
        }

        public virtual void Close()
        {
            if (ws != null)
                ws.Close();
            ws = null;
        }

        public virtual void Dispose()
        {
            if (ws != null)
                ws.Close();
        }

        public virtual void SendIceCandidate(string candidate, string sdpMid, int sdpMLineIndex, string clientId)
        {
            SendSignalingMessage(type: "candidate", candidate: candidate, sdpMid: sdpMid, sdpMLineIndex: sdpMLineIndex, clientId: clientId);
        }

        public virtual void SendOffer(string sdp, string clientId)
        {
            SendSignalingMessage(type: "offer", sdp: sdp, clientId: clientId);
        }

        public virtual void SendAnswer(string sdp, string clientId)
        {
            SendSignalingMessage(type: "answer", sdp: sdp, clientId: clientId);
        }

        public virtual void SendDescription(string type, string sdp, string clientId)
        {
            SendSignalingMessage(type: type, sdp: sdp, clientId: clientId);
        }

        public virtual void SendSignalingMessage(string type = "", string sdp = "", string candidate = "", string sdpMid = "", int sdpMLineIndex = int.MaxValue, string clientId = "")
        {
            SignalingMessage msg = null;
            if (type == "offer" || type == "answer")
            {
                if (string.IsNullOrWhiteSpace(sdp))
                    throw new Exception("[WSSignalingHandler] sdp is empty");
                else
                    msg = new SignalingMessage(type, sdp, clientId);
            }
            else if (type == "candidate")
            {
                if (string.IsNullOrWhiteSpace(sdpMid))
                    throw new Exception("[WSSignalingHandler] sdpMid is empty");
                if (sdpMLineIndex == int.MaxValue)
                    throw new Exception("[WSSignalingHandler] sdpMLineIndex is empty");
                msg = new SignalingMessage(candidate, sdpMid, sdpMLineIndex, clientId);
            }
            else
                throw new Exception("[WSSignalingHandler] unknown type");
            base.SendSignalingMessage(ws, msg);
        }
    }
}
