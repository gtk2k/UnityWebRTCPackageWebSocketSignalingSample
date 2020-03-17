import WebSocket from 'ws';

const wss = new WebSocket.Server({ port: 8989 });
const clients = {};
let sender = null;
let id = 0;

wss.on('connection', ws => {
    if (ws.protocol === 'sender')
        sender = ws;
    else {
        ws.id = ++id;
        clients[ws.id] = ws;
        sender.send(JSON.stringify({ type: 'clientconnect', clientId: ws.id }))
    }

    ws.on('message', data => {
        const msg = JSON.parse(data);
        console.log(data);
        if (msg.clientId)
            clients[msg.clientId].send(data);
        else
            sender.send(data);
    });

    ws.on('close', ws => {
        if (ws.id)
            sender.send(JSON.stringify({ type: 'clientclose', clientId: ws.id }));
        delete clients[ws.id];
    });
});
