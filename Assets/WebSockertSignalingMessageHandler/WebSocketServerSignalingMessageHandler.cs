using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace WebSocketSignalingMessageHandler
{
    class WebSocketServerSignalingMessageHandler : BaseSignalingMessageHandler
    {
        public delegate void dlgOnClientConnect(string clientId);
        public delegate void dlgOnClientDisconnect(string clientId);
        public delegate void dlgOnIceCandidate(string clientId, string candidate, string sdpMid, int sdpMLineIndex);
        public delegate void dlgOnOffer(string clientId, string sdp);
        public delegate void dlgOnAnswer(string clientId, string sdp);
        public delegate void dlgOnDescription(string clientId, string type, string sdp);
        public delegate void dlgOnOtherTextData(string clientId, string data);
        public delegate void dlgOnOtherBinaryData(string clientId, byte[] data);
        public delegate void dlgOnError(string clientId, Exception ex);

        public event dlgOnClientConnect OnClientConnect;
        public event dlgOnClientDisconnect OnClientDisconnect;
        public event dlgOnIceCandidate OnIceCandidate;
        public event dlgOnOffer OnOffer;
        public event dlgOnAnswer OnAnswer;
        public event dlgOnDescription OnDescription;
        public event dlgOnOtherTextData OnOtherTextData;
        public event dlgOnOtherBinaryData OnOtherBinaryData;
        public event dlgOnError OnError;

        public Dictionary<string, WebSocket> Clients;
        public WebSocketServer wss;
        public string path;

        public WebSocketServerSignalingMessageHandler() : base()
        {
            Clients = new Dictionary<string, WebSocket>();
        }

        public virtual void Init(int port = 80, string path = "/")
        {
            wss = new WebSocketServer(port);
            this.path = path;
            wss.AddWebSocketService<SignalerOriginalServerBehaviour>(path, behaviour =>
            {
                behaviour.OnClientConnect += (clientId, ws) =>
                {
                    context.Post(_ =>
                    {
                        if (Clients.ContainsKey(clientId))
                        {
                            Clients[clientId].Close();
                            Clients.Remove(clientId);
                        }
                        Clients.Add(clientId, ws);
                        OnClientConnect?.Invoke(clientId);
                    }, null);
                };

                behaviour.OnIceCandidate += (clientId, candidate, sdpMid, sdpMLineIndex) =>
                {
                    context.Post(_ =>
                    {
                        OnIceCandidate?.Invoke(clientId, candidate, sdpMid, sdpMLineIndex);
                    }, null);
                };

                behaviour.OnDescription += (clientId, type, sdp) =>
                {
                    context.Post(_ =>
                    {
                        OnDescription?.Invoke(clientId, type, sdp);
                    }, null);
                };

                behaviour.OnOffer += (clientId, sdp) =>
                {
                    context.Post(_ =>
                    {
                        OnOffer?.Invoke(clientId, sdp);
                    }, null);
                };

                behaviour.OnAnswer += (clientId, sdp) =>
                {
                    context.Post(_ =>
                    {
                        OnAnswer?.Invoke(clientId, sdp);
                    }, null);
                };

                behaviour.OnOtherTextData += (clientId, data) =>
                {
                    context.Post(_ =>
                    {
                        OnOtherTextData?.Invoke(clientId, data);
                    }, null);
                };

                behaviour.OnOtherBinaryData += (clientId, data) =>
                {
                    context.Post(_ =>
                    {
                        OnOtherBinaryData?.Invoke(clientId, data);
                    }, null);
                };

                behaviour.OnClientDisconnect += (clientId) =>
                {
                    context.Post(_ =>
                    {
                        OnClientDisconnect?.Invoke(clientId);
                    }, null);
                };

                behaviour.OnBehaviourError += (clientId, ex) =>
                {
                    context.Post(_ =>
                    {
                        OnError?.Invoke(clientId, ex);
                    }, null);
                };
            });
        }

        public virtual void Start()
        {
            if (wss == null || wss.WebSocketServices.Count == 0)
                throw new Exception("[WSSignalingHandler] WebSocketServer not yet ready");
            wss.Start();
        }

        public virtual void Stop()
        {
            if (wss != null)
                wss.Stop();
            wss.RemoveWebSocketService(path);
            wss = null;
        }

        public virtual void Dispose()
        {
            if (Clients != null)
                Clients.Clear();
            Stop();
        }

        public void SendOtherTextData(string clientId, string data)
        {
            if (!Clients.ContainsKey(clientId))
                throw new Exception($"[WSSignalingHandler] clientId:'{clientId}' is nothing");
            base.SendOtherTextData(Clients[clientId], data);
        }

        public void SendOtherBinaryData(string clientId, byte[] data)
        {
            if (!Clients.ContainsKey(clientId))
                throw new Exception($"[WSSignalingHandler] clientId:'{clientId}' is nothing");
            base.SendOtherBinaryData(Clients[clientId], data);
        }

        public void BroadcastOtherTextData(string data)
        {
            foreach (var client in Clients.Values)
                client.Send(data);
        }

        public void BroadcastOtherBinaryData(byte[] data)
        {
            foreach (var client in Clients.Values)
                client.Send(data);
        }

        public virtual void SendIceCandidate(string clientId, string candidate, string sdpMid, int sdpMLineIndex)
        {
            SendSignalingMessage(clientId: clientId, type: "candidate", candidate: candidate, sdpMid: sdpMid, sdpMLineIndex: sdpMLineIndex);
        }

        public virtual void SendOffer(string clientId, string sdp)
        {
            SendSignalingMessage(clientId: clientId, type: "offer", sdp: sdp);
        }

        public virtual void SendAnswer(string clientId, string sdp)
        {
            SendSignalingMessage(clientId: clientId, type: "answer", sdp: sdp);
        }

        public virtual void SendDescription(string clientId, string type, string sdp)
        {
            SendSignalingMessage(clientId: clientId, type: type, sdp: sdp);
        }

        public virtual void SendSignalingMessage(string clientId, string type = "", string sdp = "", string candidate = "", string sdpMid = "", int sdpMLineIndex = int.MaxValue)
        {
            if (!Clients.ContainsKey(clientId))
                throw new Exception($"[WSSignalingHandler] clientId:'{clientId}' is nothing");
            SignalingMessage msg = null;
            if (type == "offer" || type == "answer")
            {
                if (string.IsNullOrWhiteSpace(sdp))
                    throw new Exception("[WSSignalingHandler] sdp is empty");
                else
                    msg = new SignalingMessage(type, sdp);
            }
            else if (type == "candidate")
            {
                if (string.IsNullOrWhiteSpace(sdpMid))
                    throw new Exception("[WSSignalingHandler] sdpMid is empty");
                if (sdpMLineIndex == int.MaxValue)
                    throw new Exception("[WSSignalingHandler] sdpMLineIndex is empty");
                msg = new SignalingMessage(candidate, sdpMid, sdpMLineIndex);
            }
            else
                throw new Exception("[WSSignalingHandler] unknown type");
            base.SendSignalingMessage(Clients[clientId], msg);
        }
    }

    public class SignalerOriginalServerBehaviour : WebSocketBehavior
    {
        public delegate void dlgOnClientConnect(string clientId, WebSocket webSockeet);
        public delegate void dlgOnClientDisconnect(string clientId);
        public delegate void dlgOnIceCandidate(string clientId, string candidate, string sdpMid, int sdpMLineIndex);
        public delegate void dlgOnDescription(string clientId, string type, string sdp);
        public delegate void dlgOnOffer(string clientId, string sdp);
        public delegate void dlgOnAnswer(string clientId, string sdp);
        public delegate void dlgOnOtherTextData(string clientId, string data);
        public delegate void dlgOnOtherBinaryData(string clientId, byte[] data);
        public delegate void dlgOnError(string clientId, Exception ex);

        public event dlgOnClientConnect OnClientConnect;
        public event dlgOnClientDisconnect OnClientDisconnect;
        public event dlgOnIceCandidate OnIceCandidate;
        public event dlgOnOffer OnOffer;
        public event dlgOnAnswer OnAnswer;
        public event dlgOnDescription OnDescription;
        public event dlgOnOtherTextData OnOtherTextData;
        public event dlgOnOtherBinaryData OnOtherBinaryData;
        public event dlgOnError OnBehaviourError;

        protected override void OnOpen()
        {
            OnClientConnect.Invoke(ID, Context.WebSocket);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                if (e.IsText)
                {
                    if (e.Data.StartsWith("{") && e.Data.EndsWith("}"))
                    {
                        var msg = JsonUtility.FromJson<SignalingMessage>(e.Data);
                        if (msg.type != null && (msg.type == "offer" || msg.type == "answer"))
                        {
                            OnDescription.Invoke(ID, msg.type, msg.sdp);
                            if (msg.type == "offer")
                                OnOffer(ID, msg.sdp);
                            else if (msg.type == "answer")
                                OnAnswer(ID, msg.sdp);
                            return;
                        }
                        else if (msg.type != null && msg.type == "candidate")
                        {
                            OnIceCandidate.Invoke(ID, msg.candidate, msg.sdpMid, msg.sdpMLineIndex);
                            return;
                        }
                    }
                    OnOtherTextData.Invoke(ID, e.Data);
                }
                else
                    OnOtherBinaryData.Invoke(ID, e.RawData);
            }
            catch (Exception ex)
            {
                OnBehaviourError.Invoke(ID, ex);
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            OnClientDisconnect(ID);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            OnBehaviourError(ID, e.Exception);
        }
    }

}
