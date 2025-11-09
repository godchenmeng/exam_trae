// 建筑地图查看独立页面
// 接收 WPF 发送的 { type: 'showBuilding', payload: { name, city, lat, lng, zoom } }

const Viewer = {
  map: null,
  marker: null,

  init() {
    this.map = new BMapGL.Map('map');
    // 默认定位到贵州省中心（贵阳附近），等待 WPF 提供实际坐标
    const defaultCenter = new BMapGL.Point(106.63, 26.65);
    this.map.centerAndZoom(defaultCenter, 10);
    this.map.enableScrollWheelZoom(true);
    this.map.addControl(new BMapGL.ZoomControl());
    this.map.addControl(new BMapGL.ScaleControl());
    this.initWebViewMessageListener();
  },

  showBuilding(payload) {
    try {
      const name = payload?.name || '-';
      const city = payload?.city || '-';
      const lat = payload?.lat;
      const lng = payload?.lng;
      const zoom = payload?.zoom || 16;

      document.getElementById('lblTitle').textContent = `建筑：${name}`;
      document.getElementById('lblCity').textContent = `城市：${city}`;
      if (typeof lat === 'number' && typeof lng === 'number') {
        document.getElementById('lblCoord').textContent = `坐标：${lng.toFixed(6)}, ${lat.toFixed(6)}`;

        const point = new BMapGL.Point(lng, lat);
        this.map.centerAndZoom(point, zoom);

        if (this.marker) {
          try { this.map.removeOverlay(this.marker); } catch (e) {}
          this.marker = null;
        }
        this.marker = new BMapGL.Marker(point);
        this.map.addOverlay(this.marker);

        // 信息窗口
        const labelText = `${name}\n${city}`;
        const label = new BMapGL.Label(labelText, { position: point, offset: new BMapGL.Size(10, -20) });
        this.map.addOverlay(label);
      } else {
        document.getElementById('lblCoord').textContent = '坐标：-';
      }
    } catch (e) {
      console.error('showBuilding 失败:', e);
    }
  },

  initWebViewMessageListener() {
    try {
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.addEventListener('message', (ev) => {
          const msg = ev.data || {};
          const type = msg.type;
          const payload = msg.payload;
          switch (type) {
            case 'showBuilding':
              this.showBuilding(payload);
              break;
            default:
              break;
          }
        });
      }
    } catch (e) {
      console.warn('初始化消息监听失败:', e);
    }
  }
};

window.addEventListener('DOMContentLoaded', () => {
  try { Viewer.init(); } catch(e) { console.error('初始化建筑查看页面失败:', e); }
});