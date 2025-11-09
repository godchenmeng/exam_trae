// 地图选点独立页面（不依赖原 app.js），用于在 WPF 对话框中选取坐标
// 与 WPF 通信：window.chrome.webview.postMessage({ type, payload })

const Picker = {
  map: null,
  selectedPoint: null, // { lat, lng }
  selectedMarker: null,

  cityCenters: {
    guiyang: { lat: 26.65, lng: 106.63, zoom: 12 },
    zunyi: { lat: 27.70, lng: 106.92, zoom: 12 },
    liupanshui: { lat: 26.60, lng: 104.83, zoom: 12 },
    anshun: { lat: 26.25, lng: 105.93, zoom: 12 },
    bijie: { lat: 27.30, lng: 105.28, zoom: 12 },
    tongren: { lat: 27.72, lng: 109.19, zoom: 12 },
    qiandongnan: { lat: 26.58, lng: 107.98, zoom: 10 },
    qiannan: { lat: 26.26, lng: 107.52, zoom: 11 },
    qianxinan: { lat: 25.09, lng: 104.90, zoom: 11 }
  },

  cityNameToKey: {
    '贵阳市': 'guiyang', '遵义市': 'zunyi', '六盘水市': 'liupanshui', '安顺市': 'anshun',
    '毕节市': 'bijie', '铜仁市': 'tongren', '黔东南': 'qiandongnan', '黔东南苗族侗族自治州': 'qiandongnan',
    '黔南': 'qiannan', '黔南布依族苗族自治州': 'qiannan', '黔西南': 'qianxinan', '黔西南布依族苗族自治州': 'qianxinan'
  },

  init() {
    // 初始化地图
    this.map = new BMapGL.Map('map');
    const c = this.cityCenters.guiyang;
    const center = new BMapGL.Point(c.lng, c.lat);
    this.map.centerAndZoom(center, c.zoom);
    this.map.enableScrollWheelZoom(true);
    this.map.addControl(new BMapGL.ZoomControl());
    this.map.addControl(new BMapGL.ScaleControl());

    // 绑定地图点击事件：选点
    this.map.addEventListener('click', (e) => {
      if (!e || !e.latlng) return;
      const lat = e.latlng.lat;
      const lng = e.latlng.lng;
      this.setSelectedPoint({ lat, lng });
    });

    // 绑定界面事件
    this.bindEvents();

    // 初始化WebView消息监听
    this.initWebViewMessageListener();
  },

  bindEvents() {
    const selectCity = document.getElementById('selectCity');
    selectCity.addEventListener('change', (ev) => {
      const key = ev.target.value;
      this.setCityByKey(key);
    });

    const tipInput = document.getElementById('tipInput');
    const btnConfirmCenter = document.getElementById('btnConfirmCenter');
    btnConfirmCenter.addEventListener('click', () => {
      const text = tipInput.value.trim();
      const m = text.match(/^\s*([\-\d\.]+)\s*,\s*([\-\d\.]+)\s*$/);
      if (m) {
        const lat = parseFloat(m[1]);
        const lng = parseFloat(m[2]);
        if (!isNaN(lat) && !isNaN(lng)) {
          this.map.centerAndZoom(new BMapGL.Point(lng, lat), this.map.getZoom());
          return;
        }
      }
      // 地名检索
      try {
        const geocoder = new BMapGL.Geocoder();
        const key = document.getElementById('selectCity').value;
        const cityNameMap = {
          guiyang: '贵阳市', zunyi: '遵义市', liupanshui: '六盘水市', anshun: '安顺市',
          bijie: '毕节市', tongren: '铜仁市', qiandongnan: '黔东南', qiannan: '黔南', qianxinan: '黔西南'
        };
        const cityName = cityNameMap[key] || '';
        geocoder.getPoint(text, (point) => {
          if (point) {
            this.map.centerAndZoom(point, 16);
          } else {
            alert('未能找到该地名，请使用 "lat,lng" 格式或更换关键词');
          }
        }, cityName);
      } catch (err) {
        alert('请输入格式为 "lat,lng" 的坐标，例如：26.65,106.63');
      }
    });

    const btnConfirmSelection = document.getElementById('btnConfirmSelection');
    btnConfirmSelection.addEventListener('click', () => {
      if (!this.selectedPoint) {
        alert('请先在地图上点击选择一个点');
        return;
      }
      const key = document.getElementById('selectCity').value;
      this.notifyWPF('locationSelected', {
        lat: this.selectedPoint.lat,
        lng: this.selectedPoint.lng,
        cityKey: key
      });
    });
  },

  setSelectedPoint(pt) {
    this.selectedPoint = pt;
    // 清理旧标记
    try { if (this.selectedMarker) this.map.removeOverlay(this.selectedMarker); } catch(e){}
    // 添加新标记
    const marker = new BMapGL.Marker(new BMapGL.Point(pt.lng, pt.lat));
    this.map.addOverlay(marker);
    this.selectedMarker = marker;
    // 更新状态文本
    const status = document.getElementById('selStatus');
    status.textContent = `已选择点: ${pt.lng.toFixed(6)}, ${pt.lat.toFixed(6)}`;
  },

  setCityByKey(key) {
    const c = this.cityCenters[key];
    if (!c) return;
    this.map.centerAndZoom(new BMapGL.Point(c.lng, c.lat), c.zoom);
  },

  setCityByName(name) {
    const key = this.cityNameToKey[name] || null;
    if (key) {
      const sel = document.getElementById('selectCity');
      sel.value = key;
      this.setCityByKey(key);
    }
  },

  centerMap(payload) {
    try {
      const { lat, lng, zoom } = payload || {};
      if (typeof lat === 'number' && typeof lng === 'number') {
        this.map.centerAndZoom(new BMapGL.Point(lng, lat), zoom || this.map.getZoom());
      }
    } catch(e){}
  },

  initWebViewMessageListener() {
    try {
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.addEventListener('message', (ev) => {
          const msg = ev.data || {};
          const type = msg.type;
          const payload = msg.payload;
          switch (type) {
            case 'setCity':
              if (payload) {
                if (payload.cityKey) this.setCityByKey(payload.cityKey);
                else if (payload.cityName) this.setCityByName(payload.cityName);
              }
              break;
            case 'centerMap':
              this.centerMap(payload);
              break;
            default:
              break;
          }
        });
      }
    } catch(e) {}
  },

  notifyWPF(type, payload) {
    try {
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ type, payload });
      } else {
        console.log('[MockNotify]', type, payload);
      }
    } catch(e) {
      console.warn('notifyWPF 失败:', e);
    }
  }
};

window.addEventListener('DOMContentLoaded', () => {
  try { Picker.init(); } catch(e) { console.error('初始化选点页面失败:', e); }
});