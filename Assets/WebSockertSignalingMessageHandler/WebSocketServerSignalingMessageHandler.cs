using System;
using System.Collections.Generic;
using System.IO;
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
        public HttpServer httpsvr;
        public string path;

        private Dictionary<string, string> mimeTypes;

        public WebSocketServerSignalingMessageHandler() : base()
        {
            Clients = new Dictionary<string, WebSocket>();

            mimeTypes = new Dictionary<string, string>
            {
                {"txt", "text/plain" },
                {"html", "text/html" },
                {"htm", "text/html" },
                {"xhtml", "application/xhtml+xml" },
                {"xml", "text/xml" },
                {"json", "application/json" },

                {"css", "text/css" },
                {"js", "text/javascript" },
                {"php", "application/x-httpd-php" },

                {"gif", "image/gif" },
                {"jpg", "image/jpeg" },
                {"png", "image/png" },
                {"ico", "image/vnd.microsoft.icon" },

                {"mpg", "video/mpeg"  },
                {"mp4", "video/mp4" },
                {"webm", "video/webm" },
                {"ogg", "video/ogg" },
                {"mov", "video/quicktime" },


                {"mp3", "audio/mpeg" },
                {"m4a", "audio/aac" },
                {"octet", "application/octet-stream" }
            };
        }

        public virtual void Init(int port = 80, string path = "/")
        {
            httpsvr = new HttpServer(port);
            httpsvr.DocumentRootPath = Path.Combine(Application.streamingAssetsPath, "webroot");
            httpsvr.OnGet += Httpsvr_OnGet;
            httpsvr.Log.Level = LogLevel.Trace;

            this.path = path;
            httpsvr.AddWebSocketService<SignalerOriginalServerBehaviour>(path, behaviour =>
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

        private void Httpsvr_OnGet(object sender, HttpRequestEventArgs e)
        {
            Debug.Log(e.Request.Url);
            e.TryReadFile(e.Request.RawUrl, out byte[] contents);
            var ext = Path.GetExtension(e.Request.RawUrl).Trim();
            ext = ext.ToLower();
            if (ext.StartsWith("."))
                ext = ext.Substring(1);
            if (string.IsNullOrEmpty(ext) || !mimeTypes.ContainsKey(ext))
                e.Response.ContentType = mimeTypes["octet"];
            else
                e.Response.ContentType = mimeTypes[ext];
            e.Response.ContentLength64 = contents.LongLength;
            e.Response.Close(contents, true);
        }

        public virtual void Start()
        {
            //if (wss == null || wss.WebSocketServices.Count == 0)
            //    throw new Exception("[WSSignalingHandler] WebSocketServer not yet ready");
            //wss.Start();
            try
            {
                if (httpsvr == null || httpsvr.WebSocketServices.Count == 0)
                    throw new Exception("[WSSignalingHandler] WebSocketServer not yet ready");
                httpsvr.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        public virtual void Stop()
        {
            //if (wss != null)
            //    wss.Stop();
            //wss.RemoveWebSocketService(path);
            //wss = null;
            if (httpsvr != null)
                httpsvr.Stop();
            httpsvr.RemoveWebSocketService(path);
            httpsvr = null;
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
                        else if(msg.type != null && msg.type == "log")
                        {
                            Debug.Log($"<color='#aaccff'>[Log] {msg.logMsg}</color>");
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

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            OnBehaviourError(ID, e.Exception);
        }
    }

}
