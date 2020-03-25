using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketSignalingMessageHandler
{
    public class SignalingMessage
    {
        public string type;
        public string sdp;
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
        public string clientId;
        public string logMsg;

        public SignalingMessage(string type, string sdp) : this(type, sdp, null) { }
        public SignalingMessage(string candidate, string sdpMid, int sdpMLineIndex) : this(candidate, sdpMid, sdpMLineIndex, null) { }

        public SignalingMessage(string type, string sdp, string clientId)
        {
            this.type = type;
            this.sdp = sdp;
            this.clientId = clientId;
        }

        public SignalingMessage(string candidate, string sdpMid, int sdpMLineIndex, string clientId)
        {
            this.type = "candidate";
            this.candidate = candidate;
            this.sdpMid = sdpMid;
            this.sdpMLineIndex = sdpMLineIndex;
            this.clientId = clientId;
        }
    }

}
