# UnityWebRTCPackageWebSocketSignalingSample
UnityRenderStreamingの心臓部である [WebRTC パッケージ](https://github.com/Unity-Technologies/com.unity.webrtc) を
WebSocketを使ったシグナリングで接続するサンプル

## WSServerTestシーン
Unity側でWebSocketサーバーを立て、ブラウザーはこのWebSocketサーバーに接続してシグナリングを行うサンプルシーン。
別途、シグナリングサーバーを立てる必要がない。
Web側のテストページはTestWebPageフォルダにある wsserverhandlertest.html(wsserverhandlertest.js)
(2020/3/26 Update)
WebSocketServerからHttpServerに変更、実行するとWebSocketシグナリングサーバーおよび簡易ウェブサーバーが立つ。
(DocumentRoot は StreamingAssets/webroot)

## WSClientTestシーン
実際の環境では別途シグナリングサーバーを立てて、Unity側もWebSocketクライアントでシグナリングを行うので、それに沿ったサンプルシーン。
サンプルのシグナリングサーバーはTestWebSocketSignalingServerフォルダにあるwebsocketsignalingserver.mjs(Node.js)。
Web側のテストページはTestWebPageフォルダにある wsclienthandlertest.html(wsclienthandlertest.js)

## 注意
とりあえず接続できるというのを確かめるための実装なため、再接続処理などの実装は行っていない。
