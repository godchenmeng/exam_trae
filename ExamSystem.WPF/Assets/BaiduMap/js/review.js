// 学生答案地图查看器
const ReviewApp = {
  map: null,
  mapData: null,
  center: null,
  zoom: 12,
  overlays: [],
  // 水带渲染相关
  hoseLineLayer: null,
  hoseLineLayerData: {},
  overlayStats: {
    marker: 0,
    polyline: 0,
    polygon: 0,
    rectangle: 0,
    circle: 0
  },

  // 解析URL参数
  parseUrlParams() {
    const urlParams = new URLSearchParams(window.location.search);
    
    // 解析地图数据
    const mapDataParam = urlParams.get('mapData');
    if (mapDataParam) {
      try {
        const decodedData = decodeURIComponent(mapDataParam);
        this.mapData = JSON.parse(decodedData);
        console.log('解析到的地图数据:', this.mapData);
      } catch (e) {
        console.error('解析地图数据失败:', e);
        this.showError('地图数据格式错误');
        return false;
      }
    } else {
      // 不再强制通过 URL 传递数据，若未提供则等待 WebView2 消息
      console.log('未从URL提供地图数据，将等待来自WPF的消息');
    }
    
    // 解析地图中心
    const centerParam = urlParams.get('center');
    if (centerParam) {
      try {
        // 支持两种格式：JSON对象或逗号分隔的坐标
        if (centerParam.startsWith('{')) {
          this.center = JSON.parse(decodeURIComponent(centerParam));
        } else {
          const [lng, lat] = centerParam.split(',').map(Number);
          if (!isNaN(lng) && !isNaN(lat)) {
            this.center = { lng, lat };
          }
        }
        console.log('解析到的地图中心:', this.center);
      } catch (e) {
        console.error('解析地图中心失败:', e);
        // 使用默认中心
        this.center = { lng: 106.63, lat: 26.65 };
      }
    }
    
    // 解析缩放级别
    const zoomParam = urlParams.get('zoom');
    if (zoomParam) {
      const zoom = parseInt(zoomParam);
      if (!isNaN(zoom)) {
        this.zoom = zoom;
      }
    }
    
    return true;
  },

  // 初始化应用
  init() {
    console.log('初始化学生答案查看器...');
    
    // 解析URL参数
    if (!this.parseUrlParams()) {
      return;
    }
    
    // 初始化地图
    this.initMap();
    
    // 加载地图数据
    setTimeout(() => {
      this.loadMapData();
    }, 500);
  },

  // 初始化地图
  initMap() {
    console.log('初始化地图...');
    
    this.map = new BMapGL.Map('map');
    
    // 设置地图中心和缩放
    const center = this.center ? 
      new BMapGL.Point(this.center.lng, this.center.lat) : 
      new BMapGL.Point(106.63, 26.65);
    
    this.map.centerAndZoom(center, this.zoom);
    this.map.enableScrollWheelZoom(true);
    
    // 添加地图控件
    this.map.addControl(new BMapGL.ZoomControl());
    this.map.addControl(new BMapGL.ScaleControl());
    this.map.addControl(new BMapGL.MapTypeControl({
      anchor: BMapGL.BMAP_ANCHOR_TOP_RIGHT
    }));
    
    console.log('地图初始化完成');
  },

  // 加载地图数据
  loadMapData() {
    console.log('开始加载地图数据...');
    
    if (!this.mapData) {
      console.warn('没有可加载的地图数据，等待来自WPF的消息');
      return;
    }

    // 处理不同的数据格式
    let overlaysData = [];
    
    if (Array.isArray(this.mapData)) {
      // 直接是覆盖物数组
      overlaysData = this.mapData;
    } else if (this.mapData.overlays && Array.isArray(this.mapData.overlays)) {
      // 包含overlays字段的对象
      overlaysData = this.mapData.overlays;
    } else {
      console.warn('未识别的地图数据格式:', this.mapData);
      this.showError('地图数据格式不支持');
      return;
      }
      this.setCenterZoom(this.center, this.zoom)
    console.log('准备加载的覆盖物数据:', overlaysData);

    // 重置统计
    Object.keys(this.overlayStats).forEach(key => {
      this.overlayStats[key] = 0;
    });

    // 加载每个覆盖物
    overlaysData.forEach((item, index) => {
      try {
        this.loadOverlay(item, index);
      } catch (e) {
        console.error('加载覆盖物失败:', item, e);
      }
    });

    // 隐藏加载状态
    this.hideLoading();
    
    // 显示信息面板
    this.showInfoPanel();
    
    // 更新统计信息
    this.updateOverlayStats();
    
    console.log('地图数据加载完成，共加载', overlaysData.length, '个覆盖物');
  },

  // 通过 WebView2 接收消息并处理
  handleBridgeMessage(message) {
    try {
      const msg = (typeof message === 'string') ? JSON.parse(message) : message;
      if (!msg || !msg.type) {
        console.warn('[bridge] 无效消息:', message);
        return;
      }

      switch (msg.type) {
          case 'loadStudentData': {
          this.mapData = msg.data;
          if (msg.center) { this.center = msg.center; }
          if (typeof msg.zoom === 'number') { this.zoom = msg.zoom; }

          if (!this.map) {
            this.initMap();
          }
          this.loadMapData();
          console.log('[bridge] 已加载学生绘制数据');
          break;
        }
        case 'setCenterZoom': {
          if (msg.center) { this.center = msg.center; }
          if (typeof msg.zoom === 'number') { this.zoom = msg.zoom; }
          if (this.map && this.center) {
            this.setCenterZoom(this.center, this.zoom);
          }
          break;
        }
        case 'Error': {
          const text = msg.message || '前端收到错误消息';
          this.showError(text);
          break;
        }
        default:
          console.log('[bridge] 未处理的消息类型:', msg.type);
      }
    } catch (err) {
      console.error('[bridge] 处理消息失败:', err, message);
    }
  },

  // 设置中心点与缩放
  setCenterZoom(center, zoom) {
    try {
      const point = new BMapGL.Point(center.lng, center.lat);
      const z = (typeof zoom === 'number') ? zoom : this.zoom;
      this.map.centerAndZoom(point, z);
    } catch (e) {
      console.warn('设置中心/缩放失败:', e);
    }
  },

  // 加载单个覆盖物
  loadOverlay(data, index) {
    let overlay = null;
    const overlayId = `overlay_${index}`;

    console.log('加载覆盖物:', data);

    switch (data.type) {
      case 'marker':
        overlay = this.createMarker(data);
        break;
      case 'polyline':
        overlay = this.createPolyline(data);
        break;
      case 'polygon':
        overlay = this.createPolygon(data);
        break;
      case 'rectangle':
        overlay = this.createRectangle(data);
        break;
      case 'circle':
        overlay = this.createCircle(data);
        break;
      default:
        console.warn('未知的覆盖物类型:', data.type);
        return;
    }

    if (overlay) {
      this.map.addOverlay(overlay);
      
      // 添加到覆盖物列表
      this.overlays.push({
        id: overlayId,
        overlay: overlay,
        type: data.type,
        name: data.name || this.getDefaultName(data.type, index + 1),
        data: data
      });
      
      // 更新统计
      if (this.overlayStats.hasOwnProperty(data.type)) {
        this.overlayStats[data.type]++;
      }
      
      // 如果是线段，则按照编辑界面一样，渲染为“水带”效果
      if (data.type === 'polyline') {
        this.setupWaterHoseStyle(overlay);
      }

      // 添加标签（如果有名称）
      if (data.name) {
        this.addOverlayLabel(overlay, data.name, data.type);
      }
    }
  },

  // 创建标记
  createMarker(data) {
      if (!data.point || !data.point.lng || !data.point.lat) {
      console.error('标记缺少位置信息:', data);
      return null;
    }

      const point = new BMapGL.Point(data.point.lng, data.point.lat);
    const marker = new BMapGL.Marker(point);

    // 设置自定义图标
    if (data.icon && data.icon.url) {
      try {
        const icon = new BMapGL.Icon(
          data.icon.url,
          new BMapGL.Size(data.icon.width || 36, data.icon.height || 36),
          {
            anchor: new BMapGL.Size(
              (data.icon.width || 36) / 2,
              data.icon.height || 36
            )
          }
        );
        marker.setIcon(icon);
      } catch (e) {
        console.warn('设置标记图标失败:', e);
      }
    }

    return marker;
  },

  // 创建线条
  createPolyline(data) {
    if (!data.path || !Array.isArray(data.path) || data.path.length < 2) {
      console.error('线条缺少足够的点位信息:', data);
      return null;
    }

    const points = data.path.map(p => new BMapGL.Point(p.lng, p.lat));
    // 原始折线用于承载路径与标签位置，但不显示自身样式
    const polyline = new BMapGL.Polyline(points, {
      strokeColor: data.strokeColor || '#3388ff',
      strokeWeight: data.strokeWeight || 2,
      // 在 review 模式下，线段以“水带”呈现，隐藏原始 polyline
      strokeOpacity: 0
    });

    return polyline;
  },

  // 创建多边形
  createPolygon(data) {
      if (!data.path || !Array.isArray(data.path) || data.path.length < 3) {
      console.error('多边形缺少足够的点位信息:', data);
      return null;
    }

      const points = data.path.map(p => new BMapGL.Point(p.lng, p.lat));
    const polygon = new BMapGL.Polygon(points, {
      strokeColor: data.strokeColor || '#3388ff',
      fillColor: data.fillColor || '#3388ff',
      strokeWeight: data.strokeWeight || 2,
      strokeOpacity: data.strokeOpacity || 0.8,
      fillOpacity: data.fillOpacity || 0.2
    });

    return polygon;
  },

  // 创建矩形
  createRectangle(data) {
    // 适配两种数据格式：
    // 1) 新格式：data.path 为点数组 [{lng, lat}, ...]
    // 2) 旧格式：data.bounds.{sw, ne}
    let points = [];

    if (Array.isArray(data.path) && data.path.length >= 4) {
      // 使用 path 点集构建矩形（或一般的四边形）
      points = data.path.map(p => new BMapGL.Point(p.lng, p.lat));
      // 如果最后一个点与第一个点相同，则移除重复闭合点
      const first = data.path[0];
      const last = data.path[data.path.length - 1];
      if (first && last && first.lng === last.lng && first.lat === last.lat) {
        points.pop();
      }
    }else {
      console.error('矩形缺少路径或边界信息:', data);
      return null;
    }

    const style = data.style || {};
    const rectangle = new BMapGL.Polygon(points, {
      strokeColor: style.strokeColor || data.strokeColor || '#3388ff',
      fillColor: style.fillColor || data.fillColor || '#3388ff',
      strokeWeight: style.strokeWeight || data.strokeWeight || 2,
      strokeOpacity: (style.strokeOpacity != null ? style.strokeOpacity : (data.strokeOpacity != null ? data.strokeOpacity : 0.8)),
      fillOpacity: (style.fillOpacity != null ? style.fillOpacity : (data.fillOpacity != null ? data.fillOpacity : 0.2))
    });

    return rectangle;
  },

  // 创建圆形
  createCircle(data) {
    if (!data.center || !data.radius) {
      console.error('圆形缺少中心点或半径信息:', data);
      return null;
    }

    const center = new BMapGL.Point(data.center.lng, data.center.lat);
    const circle = new BMapGL.Circle(center, data.radius, {
      strokeColor: data.strokeColor || '#3388ff',
      fillColor: data.fillColor || '#3388ff',
      strokeWeight: data.strokeWeight || 2,
      strokeOpacity: data.strokeOpacity || 0.8,
      fillOpacity: data.fillOpacity || 0.2
    });

    return circle;
  },

  // —— 水带渲染（与 app.js 保持一致的视觉样式） ——
  // 设置水带样式
  setupWaterHoseStyle(polyline) {
    try {
      const path = polyline.getPath();
      if (!path || path.length < 2) return;

      // 使用 LineLayer 显示水带纹理
      this.addHoseLineLayer(polyline);
    } catch (err) {
      console.error('设置水带样式失败:', err);
    }
  },

  // 使用 LineLayer 创建水带显示
  addHoseLineLayer(polyline) {
    try {
      const path = polyline.getPath();
      const id = 'hose-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);

      // 若当前环境不支持 LineLayer，则回退为显示原始 polyline
      if (!BMapGL || typeof BMapGL.LineLayer !== 'function') {
        try { polyline.setStrokeOpacity(0.8); } catch (e) {}
        return;
      }

      // 隐藏原始 polyline（双保险）
      try { polyline.setStrokeOpacity(0); } catch (e) {}

      // 准备 LineLayer 数据
      const lineData = {
        type: 'FeatureCollection',
        features: [{
          type: 'Feature',
          properties: { name: id, type: 'hose' },
          geometry: {
            type: 'LineString',
            coordinates: path.map(point => [point.lng, point.lat])
          }
        }]
      };

      // 创建（或复用）LineLayer
      if (!this.hoseLineLayer) {
        this.hoseLineLayer = new BMapGL.LineLayer({
          enablePicked: true,
          autoSelect: false,
          pickWidth: 30,
          pickHeight: 30,
          opacity: 1,
          style: {
            sequence: false,
            marginLength: 16,
            borderColor: '#999',
            borderMask: true,
            borderWeight: 0,
            strokeWeight: 8,
            strokeLineJoin: 'round',
            strokeLineCap: 'square',
            strokeColor: '#ff6600',
            // 与编辑页一致的水带纹理
            strokeTextureUrl: 'assets/icons/line.png',
            strokeTextureWidth: 16,
            strokeTextureHeight: 64
          }
        });

        // 将 LineLayer 添加到地图
        this.map.addNormalLayer(this.hoseLineLayer);
      }

      // 追加数据并更新图层
      const existingData = this.hoseLineLayer.getData() || { type: 'FeatureCollection', features: [] };
      existingData.features.push(lineData.features[0]);
      this.hoseLineLayer.setData(existingData);

      // 可选：添加水带头标记
      const hoseHeadMarker = this.addHoseHeadMarker(polyline, id);

      // 记录信息，方便后续需要清理或联动
      this.hoseLineLayerData[id] = {
        polyline: polyline,
        hoseHeadMarker: hoseHeadMarker,
        featureIndex: existingData.features.length - 1
      };

      // 关联 id 到原始 polyline
      polyline._waterHoseLineLayer = { id };

    } catch (err) {
      console.error('创建 LineLayer 水带失败:', err);
    }
  },

  // 添加水带头标记（简化版本）
  addHoseHeadMarker(polyline, id) {
    try {
      const points = polyline.getPath();
      if (!points || points.length < 2) return null;

      const headImgSrc = 'assets/icons/line_top.png';

      // 计算起始段角度（参考 app.js 的计算方式）
      const startPoint = points[0];
      const nextPoint = points[1];
      const dx = nextPoint.lng - startPoint.lng;
      const dy = nextPoint.lat - startPoint.lat;
      let rotation = Math.atan2(-dy, dx) * 180 / Math.PI + 90;

      const icon = new BMapGL.Icon(headImgSrc, new BMapGL.Size(20, 20), {
        anchor: new BMapGL.Size(10, 10)
      });
      const headMarker = new BMapGL.Marker(points[0], { icon: icon });

      // 旋转水带头图标
      this.rotateIcon(headMarker, rotation);

      this.map.addOverlay(headMarker);
      return headMarker;
    } catch (err) {
      console.error('添加水带头标记失败:', err);
      return null;
    }
  },

  // 旋转图标（与 app.js 相同的实现方式）
  rotateIcon(marker, angle) {
    try {
      const icon = marker.getIcon();
      if (icon && icon.imageUrl) {
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        const img = new Image();

        img.onload = function() {
          const size = 24;
          canvas.width = size;
          canvas.height = size;

          ctx.translate(size/2, size/2);
          ctx.rotate(angle * Math.PI / 180);
          ctx.drawImage(img, -size/2, -size/2, size, size);

          const rotatedIcon = new BMapGL.Icon(canvas.toDataURL(), new BMapGL.Size(size, size), {
            anchor: new BMapGL.Size(size/2, size/2)
          });
          marker.setIcon(rotatedIcon);
        };

        img.src = icon.imageUrl;
      }
    } catch (err) {
      console.error('旋转图标失败:', err);
    }
  },

  // 添加覆盖物标签
  addOverlayLabel(overlay, name, type) {
    let labelPoint = null;

    // 根据覆盖物类型获取标签位置
    if (type === 'marker') {
      labelPoint = overlay.getPosition();
    } else if (type === 'circle') {
      labelPoint = overlay.getCenter();
    } else if (overlay.getPath) {
      const path = overlay.getPath();
      if (path && path.length > 0) {
        labelPoint = path[0];
      }
    }

    if (labelPoint) {
      const label = new BMapGL.Label(name, {
        position: labelPoint,
        offset: new BMapGL.Size(10, -10)
      });
      
      label.setStyle({
        color: '#333',
        fontSize: '12px',
        backgroundColor: 'rgba(255, 255, 255, 0.8)',
        border: '1px solid #ccc',
        borderRadius: '3px',
        padding: '2px 6px'
      });
      
      this.map.addOverlay(label);
    }
  },

  // 获取默认名称
  getDefaultName(type, index) {
    const typeNames = {
      marker: '标记',
      polyline: '线条',
      polygon: '多边形',
      rectangle: '矩形',
      circle: '圆形'
    };
    return `${typeNames[type] || '图形'}${index}`;
  },

  // 更新覆盖物统计
  updateOverlayStats() {
    const summaryContainer = document.getElementById('overlaySummary');
    const listContainer = document.getElementById('overlayList');
    
    if (!summaryContainer || !listContainer) return;

    // 清空现有内容
    summaryContainer.innerHTML = '';
    listContainer.innerHTML = '';

    // 生成统计信息
    const typeNames = {
      marker: '标记',
      polyline: '线条',
      polygon: '多边形',
      rectangle: '矩形',
      circle: '圆形'
    };

    const typeIcons = {
      marker: '📍',
      polyline: '📏',
      polygon: '🔷',
      rectangle: '⬜',
      circle: '⭕'
    };

    Object.keys(this.overlayStats).forEach(type => {
      const count = this.overlayStats[type];
      if (count > 0) {
        const countElement = document.createElement('div');
        countElement.className = 'overlay-count';
        countElement.innerHTML = `
          <div class="icon">${typeIcons[type] || '📐'}</div>
          <span>${typeNames[type] || type}: ${count}个</span>
        `;
        summaryContainer.appendChild(countElement);
      }
    });

    // 生成覆盖物列表
    this.overlays.forEach((item, index) => {
      const listItem = document.createElement('div');
      listItem.className = 'overlay-item';
      listItem.innerHTML = `
        <div class="overlay-type-icon type-${item.type}">${typeIcons[item.type] || '📐'}</div>
        <span>${item.name}</span>
      `;
      listContainer.appendChild(listItem);
    });

    // 如果没有覆盖物，显示提示
    if (this.overlays.length === 0) {
      listContainer.innerHTML = '<div style="text-align: center; color: #666; padding: 20px;">该学生未绘制任何图形</div>';
    }
  },

  // 显示错误信息
  showError(message) {
    const loadingOverlay = document.getElementById('loadingOverlay');
    const errorMessage = document.getElementById('errorMessage');
    const errorText = document.getElementById('errorText');
    
    if (loadingOverlay) loadingOverlay.classList.add('hidden');
    if (errorText) errorText.textContent = message;
    if (errorMessage) errorMessage.classList.remove('hidden');
    
    console.error('ReviewApp Error:', message);
  },

  // 隐藏加载状态
  hideLoading() {
    const loadingOverlay = document.getElementById('loadingOverlay');
    if (loadingOverlay) {
      loadingOverlay.classList.add('hidden');
    }
  },

  // 显示信息面板
  showInfoPanel() {
    const reviewInfo = document.getElementById('reviewInfo');
    if (reviewInfo) {
      reviewInfo.classList.remove('hidden');
    }
  }
};

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', () => {
  console.log('页面加载完成，开始初始化...');
  ReviewApp.init();

  // WebView2 消息监听：接收 WPF 通过 PostWebMessageAsJson 发送的数据
  try {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.addEventListener('message', (ev) => {
        const data = ev && ev.data ? ev.data : ev;
        ReviewApp.handleBridgeMessage(data);
      });
      console.log('[bridge] WebView2 消息监听已启用');
    } else {
      console.log('[bridge] 未检测到 WebView2 环境');
    }
  } catch (e) {
    console.error('[bridge] 消息监听初始化失败:', e);
  }
});