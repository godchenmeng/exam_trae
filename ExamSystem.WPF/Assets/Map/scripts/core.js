(function(){
  const logEl = document.getElementById('log');
  const mapEl = document.getElementById('map');
  const btnSubmit = document.getElementById('btnSubmit');
  const btnClear = document.getElementById('btnClear');

  const state = {
    questionId: null,
    config: null,
    guidanceOverlays: [],
    referenceOverlays: [], // 学生端不会传
    studentOverlays: [],
    drawStartTime: null
  };

  function log(msg){
    const time = new Date().toLocaleTimeString();
    logEl.innerText += `[${time}] ${msg}\n`;
    logEl.scrollTop = logEl.scrollHeight;
  }

  function sendMessage(messageType, payload){
    const msg = { messageType, payload };
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage(msg);
    } else {
      log(`[warn] WebView2 bridge not available, message not sent: ${JSON.stringify(msg)}`);
    }
  }

  function handleLoadQuestion(payload){
    state.questionId = payload.questionId;
    state.config = payload.config || {};
    state.guidanceOverlays = payload.guidanceOverlays || [];
    state.referenceOverlays = payload.referenceOverlays || [];
    state.studentOverlays = [];
    state.drawStartTime = Date.now();

    // 模拟在地图上渲染：这里只是显示数量与类型
    mapEl.innerText = `题目ID: ${state.questionId}\n` +
      `允许工具: ${(state.config.allowedTools||[]).join(', ')}\n` +
      `指引图层: ${state.guidanceOverlays.length} 个\n` +
      `（学生端不显示参考答案图层）`;

    log(`[LoadQuestion] guidance=${state.guidanceOverlays.length}, tools=${(state.config.allowedTools||[]).join(',')}`);
  }

  function handleClearAnswer(payload){
    state.studentOverlays = [];
    state.drawStartTime = Date.now();
    log(`[ClearAnswer] reset overlays and start time.`);
  }

  function handleRequestSubmit(payload){
    // WPF 请求提交，JS 主动发送当前答案
    const durationSec = state.drawStartTime ? Math.round((Date.now() - state.drawStartTime)/1000) : 0;
    sendMessage('SubmitAnswer', { 
      questionId: state.questionId, 
      overlays: state.studentOverlays, 
      drawDurationSeconds: durationSec 
    });
    log(`[RequestSubmit->SubmitAnswer] overlays=${state.studentOverlays.length}, duration=${durationSec}s`);
  }

  function handleError(payload){
    log(`[Error] ${payload.code} - ${payload.detail || ''}`);
  }

  function handleUnknown(message){
    log(`[Unknown] ${JSON.stringify(message)}`);
  }

  function onBridgeMessage(message){
    try {
      const { messageType, payload } = message;
      switch(messageType){
        case 'LoadQuestion':
          handleLoadQuestion(payload);
          break;
        case 'ClearAnswer':
          handleClearAnswer(payload);
          break;
        case 'RequestSubmit':
          handleRequestSubmit(payload);
          break;
        case 'Error':
          handleError(payload);
          break;
        default:
          handleUnknown(message);
      }
    } catch (e) {
      log(`[exception] ${e.message}`);
    }
  }

  // 作答（模拟）：按照允许工具添加一个示例覆盖物
  btnSubmit.addEventListener('click', function(){
    // 模拟添加一个学生绘制的覆盖物到状态中
    const newOverlay = {
      id: 's' + (state.studentOverlays.length + 1), 
      type: 'Polyline', 
      editable: false, 
      visible: true,
      geometry: { path: [ {lng:116.4,lat:39.9}, {lng:116.41,lat:39.91} ] },
      style: { strokeColor: '#ff0000', strokeWeight: 4 }, 
      meta: { label: '示例折线' }
    };
    state.studentOverlays.push(newOverlay);

    const durationSec = state.drawStartTime ? Math.round((Date.now() - state.drawStartTime)/1000) : 0;
    sendMessage('SubmitAnswer', { 
      questionId: state.questionId, 
      overlays: state.studentOverlays, 
      drawDurationSeconds: durationSec 
    });
    log(`[SubmitAnswer] overlays=${state.studentOverlays.length}, duration=${durationSec}s`);
  });

  btnClear.addEventListener('click', function(){
    state.studentOverlays = [];
    state.drawStartTime = Date.now();
    log(`[ClearAnswer] reset start time.`);
  });

  // WebView2 消息监听
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', (ev) => {
      const msg = ev.data || ev;
      onBridgeMessage(msg);
    });
    log('[bridge] WebView2 ready.');
  } else {
    log('[bridge] WebView2 not available.');
  }
})();