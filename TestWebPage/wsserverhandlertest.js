const ws = new WebSocket(`ws://${location.hostname}`);
ws.onopen = evt => {
    console.log('ws open');
};
ws.onmessage = async evt => {
    const msg = JSON.parse(evt.data);
    console.log(msg.type);
    if (msg.sdp) {
        await pc.setRemoteDescription(msg);
        if (msg.type === 'offer') {
            const answer = await pc.createAnswer();
            await pc.setLocalDescription(answer);
            const sendData = JSON.stringify(answer);
            ws.send(sendData);
        }
    } else if (msg.type === 'candidate') {
        console.log(`add ice candidate: ${evt.data}`);
        await pc.addIceCandidate(msg);
    }
};
ws.onclose = evt => {
    console.log(`ws close:${evt.code}`);
};
ws.onerror = _ => {
    console.log('ws error');
};

const pc = new RTCPeerConnection({ iceServers: [{ urls: 'stun:stun.l.google.com:19302' }] });
pc.onicecandidate = evt => {
    if (evt.candidate) {
        const sendData = JSON.stringify({ type: 'candidate', candidate: evt.candidate.candidate, sdpMid: evt.candidate.sdpMid, sdpMLineIndex: evt.candidate.sdpMLineIndex });
        ws.send(sendData);
    }
};
pc.ontrack = evt => {
    if (evt.track.kind === 'video')
        vid.srcObject = evt.streams[0];
};
pc.ondatachannel = evt => {
    console.log(`data channel open:${evt.channel.name}`);
};
