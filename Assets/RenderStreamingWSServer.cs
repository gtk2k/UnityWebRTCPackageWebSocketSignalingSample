using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSignalingMessageHandler;

public class RenderStreamingWSServer : MonoBehaviour
{
    [SerializeField, Tooltip("Array to set your own STUN/TURN servers")]
    private RTCIceServer[] iceServers = new RTCIceServer[]
    {
        new RTCIceServer()
        {
            urls = new string[] { "stun:stun.l.google.com:19302" }
        }
    };

    [SerializeField, Tooltip("Streaming size should match display aspect ratio")]
    private Vector2Int streamingSize = new Vector2Int(1920, 1080);

    private RTCConfiguration conf;
    private MediaStream videoStream;
    private MediaStream audioStream;
    private RTCPeerConnection pc;
    private WebSocketServerSignalingMessageHandler wssMessageHandler;

    private RTCOfferOptions offerOptions = new RTCOfferOptions
    {
        iceRestart = false,
        offerToReceiveAudio = false,
        offerToReceiveVideo = false
    };

    public void Awake()
    {
        WebRTC.Initialize(EncoderType.Hardware);
     
        wssMessageHandler = new WebSocketServerSignalingMessageHandler();
        wssMessageHandler.Init();
        wssMessageHandler.OnClientConnect += onClientConnect;
        wssMessageHandler.OnIceCandidate += onIceCandidate;
        wssMessageHandler.OnAnswer += onAnswer;
        wssMessageHandler.OnClientDisconnect += onClientDisconnect;
        wssMessageHandler.OnError += onError;
        wssMessageHandler.Start();
    }

    void Start()
    {
        videoStream = GetComponent<Camera>().CaptureStream(streamingSize.x, streamingSize.y, RenderTextureDepth.DEPTH_24);
        audioStream = Audio.CaptureStream();

        conf = default;
        conf.iceServers = iceServers;
        StartCoroutine(WebRTC.Update());
    }

    public void OnDestroy()
    {
        WebRTC.Finalize();
        Audio.Stop();
        wssMessageHandler.Dispose();
        wssMessageHandler = null;
    }

    private void onClientConnect(string clientId)
    {
        setupPeer(clientId);
    }

    private void onIceCandidate(string clientId, string candidate, string sdpMid, int sdpMLineIndex)
    {
        var cand = new RTCIceCandidate { candidate = candidate, sdpMid = sdpMid, sdpMLineIndex = sdpMLineIndex };
        pc.AddIceCandidate(ref cand);
    }

    private void onAnswer(string clientId, string sdp)
    {
        StartCoroutine(proccessAnswer(sdp));
    }

    private void onClientDisconnect(string clientId)
    {
        Debug.Log($"Close: clientId:{clientId}");
    }

    private void onError(string clientId, System.Exception e)
    {
        Debug.LogError(e);
    }

    void setupPeer(string clientId)
    {
        pc = new RTCPeerConnection(ref conf);
        pc.OnIceCandidate = candidate =>
        {
            Debug.Log($"onIceCandidate: candidate:{candidate.candidate}, sdpMid:{candidate.sdpMid}, sdpMLineIndex:{candidate.sdpMLineIndex}");
            wssMessageHandler.SendIceCandidate(clientId, candidate.candidate, candidate.sdpMid, candidate.sdpMLineIndex);
        };
        foreach (var track in videoStream.GetTracks())
            pc.AddTrack(track);
        foreach (var track in audioStream.GetTracks())
            pc.AddTrack(track);
        StartCoroutine(proccessOffer(clientId));
    }

    IEnumerator proccessOffer(string clientId)
    {
        var op = pc.CreateOffer(ref offerOptions);
        yield return op;

        if (op.isError)
            Debug.Log("create offer error");
        else
        {
            var ret = pc.SetLocalDescription(ref op.desc);
            yield return ret;

            if (ret.isError)
                Debug.LogError($"set local offer error:{ret.error}");
            else
            {
                Debug.Log($"send offer: {op.desc.sdp}");
                wssMessageHandler.SendOffer(clientId, op.desc.sdp);
            }
        }
    }

    IEnumerator proccessAnswer(string sdp)
    {
        //string pattern = @"(a=fmtp:\d+ .*level-asymmetry-allowed=.*)\r\n";
        //sdp = Regex.Replace(sdp, pattern, "$1;x-google-start-bitrate=16000;x-google-max-bitrate=160000\r\n");
        RTCSessionDescription answer = default;
        answer.type = RTCSdpType.Answer;
        answer.sdp = sdp;
        Debug.Log($"set remote answer: {sdp}");
        var ret = pc.SetRemoteDescription(ref answer);
        yield return ret;

        if (ret.isError)
            Debug.Log($"processAnser error:{ret.error}");
    }
}