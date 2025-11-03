// 动态图标数据：从 /api/icons 加载

// 页面主逻辑（基于 BMapGL WebGL）
const App = {
  map: null,
  drawingManager: null,
  currentTool: null,
  overlays: [], // { id, overlay, type, name }
  customOverlays: {}, // 存储自定义覆盖物（纹理覆盖物和水带头标记）
  selectedMarkerIcon: null,
  selectedMarkerIconName: null,
  iconCategory: 'all',
  iconPage: 1,
  iconPageSize: 42,
  iconTotal: 0,
  iconModal: null,
  // 图标数据
  iconsLoaded: false,
  iconCategories: [], // [{ name, icons: [{name, url}] }]
  allIconsFlat: [],
  buildingMarkers: { dz: [], zz: [], zd: [] },
  // 存储建筑数据，按类型分类
  buildingData: { dz: [], zz: [], zd: [] },
  // 确认定位按钮的目标缩放级别（数值越大越"放大"）。如需改为更远视野，可将其改小。
  confirmCenterZoom: 18,
  // 只读模式相关
  isReviewMode: false,
  reviewMapData: null,

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


  init() {
    
    // 初始化地图（BMapGL）
    this.map = new BMapGL.Map('map');
    
    // 根据模式设置地图中心和缩放
    let center, zoom;
    if (this.isReviewMode && this.reviewCenter) {
      center = new BMapGL.Point(this.reviewCenter.lng, this.reviewCenter.lat);
      zoom = this.reviewZoom || 12;
    } else {
      const c = this.cityCenters.guiyang;
      center = new BMapGL.Point(c.lng, c.lat);
      zoom = c.zoom;
    }
    
    this.map.centerAndZoom(center, zoom);
    this.map.enableScrollWheelZoom(true);
    this.map.addControl(new BMapGL.ZoomControl());
    this.map.addControl(new BMapGL.ScaleControl());
    this.map.addControl(new BMapGL.MapTypeControl({anchor: BMapGL.BMAP_ANCHOR_TOP_RIGHT}));

    // 如果是只读模式，隐藏编辑工具
    if (this.isReviewMode) {
      this.hideEditingTools();
      this.loadReviewMapData();
    } else {
      // 绘图管理器（BMapGLLib DrawingManager）
      this.initDrawingManager();
    }

    // 表单验证
    this.initFormValidation();

    // 图标模态框
    this.iconModal = new bootstrap.Modal(document.getElementById('iconModal'));
    // 加载图标数据后渲染
    this.loadIcons().then(() => {
      this.populateIconCategories();
      this.renderIconPage();
    }).catch(err => {
      console.error('加载图标失败:', err);
      // 仍然初始化空内容以避免界面报错
      this.renderIconPage();
    });

    // 绑定事件
    this.bindEvents();

    // 初始化WebView2消息监听
    this.initWebViewMessageListener();

    // 初始渲染覆盖物列表
    this.renderOverlayList();
  },

  // 从内置HTTP服务器加载图标清单
  async loadIcons() {
    try {
      const resp = await fetch('/api/icons', { cache: 'no-store' });
      if (!resp.ok) throw new Error('图标接口返回错误: ' + resp.status);
      const data = await resp.json();
      const categories = Array.isArray(data.categories) ? data.categories : [];
      // 规范化结构
      this.iconCategories = categories.map(c => ({
        name: c.name,
        icons: Array.isArray(c.icons) ? c.icons.map(i => ({ name: i.name, url: i.url })) : []
      }));
      // 汇总全部图标
      this.allIconsFlat = this.iconCategories.flatMap(c => c.icons);
      this.iconsLoaded = true;
      console.log('[IconLoader] 已加载图标分类:', this.iconCategories.map(c => ({ name: c.name, count: c.icons.length })));
    } catch (e) {
      this.iconsLoaded = false;
      console.error('[IconLoader] 加载失败:', e);
      throw e;
    }
  },

  // 填充分类下拉框
  populateIconCategories() {
    try {
      const sel = document.getElementById('selectIconCategory');
      if (!sel) return;
      // 保留第一个“全部分类”选项，清空后续
      sel.querySelectorAll('option:not([value="all"])').forEach(opt => opt.remove());
      this.iconCategories.forEach(cat => {
        const opt = document.createElement('option');
        opt.value = cat.name;
        opt.textContent = cat.name;
        sel.appendChild(opt);
      });
    } catch (e) {
      console.warn('填充分类失败:', e);
    }
  },

  

  initDrawingManager() {
    const style = {
      strokeColor: '#3388ff',
      fillColor: '#3388ff',
      strokeWeight: 2,
      fillOpacity: 0.2,
      strokeOpacity: 0.8,
    };
    this.drawingManager = new BMapGLLib.DrawingManager(this.map, {
      isOpen: false,
      enableDrawingTool: false,
      polygonOptions: style,
      polylineOptions: style,
      circleOptions: style,
      rectangleOptions: style,
      markerOptions: {}
    });

    this.drawingManager.addEventListener('overlaycomplete', (e) => {
      const overlay = e.overlay;
      const type = e.drawingMode; // 'marker' | 'polyline' | 'polygon' | 'rectangle' | 'circle'

      if (type === 'marker' && this.selectedMarkerIcon) {
        try {
          const icon = new BMapGL.Icon(this.selectedMarkerIcon, new BMapGL.Size(36,36), { anchor: new BMapGL.Size(18,36) });
          overlay.setIcon(icon);
        } catch(err){}
      }

      // 为水带（polyline）添加特殊样式
      if (type === 'polyline') {
        try {
          this.setupWaterHoseStyle(overlay);
        } catch(err) {
          console.error('设置水带样式失败:', err);
        }
      }

      const id = 'ov-' + (this.overlays.length + 1);
      const name = this.defaultOverlayName(type, this.overlays.length + 1);
      // 记录样式/图标以便序列化
      const item = { id, overlay, type, name };
      if (type === 'marker' && this.selectedMarkerIcon) {
        // 同时保存到 style.iconUrl 与 icon.url，便于不同解析端兼容
        item.style = Object.assign({}, item.style, { iconUrl: this.selectedMarkerIcon });
        item.icon = { url: this.selectedMarkerIcon };
      }
      this.overlays.push(item);
      // 在地图上显示图形名称
      this.attachOverlayLabel(item);
      this.renderOverlayList();

      this.drawingManager.close();
      this.setActiveTool(null);
    });
  },

  initFormValidation() {
    const form = document.getElementById('infoForm');
    form.addEventListener('submit', (ev) => {
      if (!form.checkValidity()) {
        ev.preventDefault();
        ev.stopPropagation();
      }
      form.classList.add('was-validated');
    });
    const tipInput = document.getElementById('tipInput');
    tipInput.addEventListener('keydown', (ev) => {
      if (ev.key === 'Enter') {
        ev.preventDefault();
        const text = tipInput.value.trim();
        const m = text.match(/^\s*([\-\d\.]+)\s*,\s*([\-\d\.]+)\s*$/);
        if (m) {
          tipInput.classList.remove('is-invalid');
          const lat = parseFloat(m[1]);
          const lng = parseFloat(m[2]);
          if (!isNaN(lat) && !isNaN(lng)) {
            this.map.centerAndZoom(new BMapGL.Point(lng, lat), this.map.getZoom());
          }
        } else {
          try {
            const geocoder = new BMapGL.Geocoder();
            const cityKey = document.getElementById('selectCity').value;
            const cityNameMap = {
              guiyang: '贵阳市', zunyi: '遵义市', liupanshui: '六盘水市', anshun: '安顺市',
              bijie: '毕节市', tongren: '铜仁市', qiandongnan: '黔东南', qiannan: '黔南', qianxinan: '黔西南'
            };
            const cityName = cityNameMap[cityKey] || '';
            geocoder.getPoint(text, (point) => {
              if (point) {
                this.map.centerAndZoom(point, this.map.getZoom());
                tipInput.classList.remove('is-invalid');
              } else {
                tipInput.classList.add('is-invalid');
                alert('未能找到该地名，请更换关键词');
              }
            }, cityName);
            this.notifyWPF('requestBuildingData', { cityName: cityName });
          } catch (err) {
            tipInput.classList.add('is-invalid');
            alert('请输入格式为 "lat,lng" 的坐标，例如：26.65,106.63');
          }
        }
      }
    });

    // 创建并绑定“确认定位”按钮（若不存在则动态插入到中心输入框所在列之后）
    let btnConfirmCenter = document.getElementById('btnConfirmCenter');
    if (!btnConfirmCenter) {
      const tipCol = tipInput.parentElement; // .col-auto
      const col = document.createElement('div');
      col.className = 'col-auto';
      btnConfirmCenter = document.createElement('button');
      btnConfirmCenter.id = 'btnConfirmCenter';
      btnConfirmCenter.type = 'button';
      btnConfirmCenter.className = 'btn btn-sm btn-primary ms-2';
      btnConfirmCenter.textContent = '确认定位';
      col.appendChild(btnConfirmCenter);
      tipCol.insertAdjacentElement('afterend', col);
    }

    // 将“中心定位：”标签、输入框与“确认定位”按钮合并到同一行
    try {
      const labelEl = document.querySelector('label[for="tipInput"]');
      const labelCol = labelEl ? labelEl.parentElement : null;
      const tipCol = tipInput.parentElement;
      const btnColEl = btnConfirmCenter.parentElement;
      const container = tipCol ? tipCol.parentElement : null;
      if (labelEl && labelCol && tipCol && btnColEl && container && !container.querySelector('.center-row')) {
        const centerRow = document.createElement('div');
        centerRow.className = 'center-row';
        // 创建输入与校验信息的包裹
        const inputWrap = document.createElement('div');
        inputWrap.className = 'center-input-wrap';
        const tipFeedback = (tipInput.nextElementSibling && tipInput.nextElementSibling.classList.contains('invalid-feedback')) ? tipInput.nextElementSibling : null;
        // 插入位置：在原标签列(labelCol)之前插入
        container.insertBefore(centerRow, labelCol);
        // 迁移元素：label、输入包裹、按钮
        centerRow.appendChild(labelEl);
        inputWrap.appendChild(tipInput);
        if (tipFeedback) inputWrap.appendChild(tipFeedback);
        centerRow.appendChild(inputWrap);
        centerRow.appendChild(btnConfirmCenter);
        // 移除原列容器
        labelCol.remove();
        tipCol.remove();
        btnColEl.remove();
      }
    } catch(e) { /* 忽略合并行失败，不影响主要功能 */ }

    btnConfirmCenter.addEventListener('click', () => {
      const text = tipInput.value.trim();
      const m = text.match(/^\s*([\-\d\.]+)\s*,\s*([\-\d\.]+)\s*$/);
      if (m) {
        tipInput.classList.remove('is-invalid');
        const lat = parseFloat(m[1]);
        const lng = parseFloat(m[2]);
        if (!isNaN(lat) && !isNaN(lng)) {
          this.map.centerAndZoom(new BMapGL.Point(lng, lat), this.confirmCenterZoom || 18);
        }
      } else {
        try {
          const geocoder = new BMapGL.Geocoder();
          const cityKey = document.getElementById('selectCity').value;
          const cityNameMap = {
            guiyang: '贵阳市', zunyi: '遵义市', liupanshui: '六盘水市', anshun: '安顺市',
            bijie: '毕节市', tongren: '铜仁市', qiandongnan: '黔东南', qiannan: '黔南', qianxinan: '黔西南'
          };
          const cityName = cityNameMap[cityKey] || '';
          geocoder.getPoint(text, (point) => {
            if (point) {
              this.map.centerAndZoom(point, this.confirmCenterZoom || 18);
              tipInput.classList.remove('is-invalid');
            } else {
              tipInput.classList.add('is-invalid');
              alert('未能找到该地名，请使用 "lat,lng" 格式或更换关键词');
            }
          }, cityName);
        } catch (err) {
          tipInput.classList.add('is-invalid');
          alert('请输入格式为 "lat,lng" 的坐标，例如：26.65,106.63');
        }
      }
    });

    // 将“城市：”标签与城市选择器合并为同一行，并保持校验提示在下方
    try {
      const cityLabel = document.querySelector('label[for="selectCity"]');
      const cityLabelCol = cityLabel ? cityLabel.parentElement : null;
      const citySelect = document.getElementById('selectCity');
      const citySelectCol = citySelect ? citySelect.parentElement : null;
      const container = citySelectCol ? citySelectCol.parentElement : null;
      if (cityLabel && cityLabelCol && citySelect && citySelectCol && container && !container.querySelector('.city-row')) {
        const cityRow = document.createElement('div');
        cityRow.className = 'city-row';
        const inputWrap = document.createElement('div');
        inputWrap.className = 'city-input-wrap';
        const cityFeedback = (citySelect.nextElementSibling && citySelect.nextElementSibling.classList.contains('invalid-feedback')) ? citySelect.nextElementSibling : null;
        container.insertBefore(cityRow, cityLabelCol);
        cityRow.appendChild(cityLabel);
        inputWrap.appendChild(citySelect);
        if (cityFeedback) inputWrap.appendChild(cityFeedback);
        cityRow.appendChild(inputWrap);
        cityLabelCol.remove();
        citySelectCol.remove();
      }
    } catch(e) { /* 忽略失败，不影响主要功能 */ }
  },

  bindEvents() {
    const navIconPicker = document.getElementById('navIconPicker');
    if (navIconPicker) {
      navIconPicker.addEventListener('click', (ev) => {
        ev.preventDefault();
        this.iconModal.show();
      });
    }

    // 已移除矢量/路网切换相关交互

    document.getElementById('selectCity').addEventListener('change', (ev) => {
      const cityKey = ev.target.value;
      const c = this.cityCenters[cityKey];
      if (c) {
        this.map.centerAndZoom(new BMapGL.Point(c.lng, c.lat), c.zoom);
        this.clearBuildingMarkers('dz');
        this.clearBuildingMarkers('zz');
        this.clearBuildingMarkers('zd');
        document.getElementById('dzCount').textContent = 0;
        document.getElementById('zzCount').textContent = 0;
        document.getElementById('zdCount').textContent = 0;
        ['chkDz','chkZz','chkZd'].forEach(id => { const el = document.getElementById(id); if (el) el.checked = false; });
            
        // 请求该城市的建筑数据
        this.requestBuildingData(cityKey);
      }
    });

    document.getElementById('chkDz').addEventListener('change', (ev) => this.toggleBuilding('dz', ev.target.checked));
    document.getElementById('chkZz').addEventListener('change', (ev) => this.toggleBuilding('zz', ev.target.checked));
    document.getElementById('chkZd').addEventListener('change', (ev) => this.toggleBuilding('zd', ev.target.checked));

    document.getElementById('btnClear').addEventListener('click', () => {
      this.overlays.forEach(ov => { 
        try { this.map.removeOverlay(ov.overlay); } catch(e){}
        try { if (ov.nameLabel) this.map.removeOverlay(ov.nameLabel); } catch(e){}
        
        // 如果是水带，清理相关覆盖物
        if (ov.type === 'polyline') {
          // 清理旧的纹理覆盖物（向后兼容）
          if (ov.overlay._waterHoseOverlay) {
            this.clearCustomOverlays(ov.overlay._waterHoseOverlay.id);
          }
          // 清理新的LineLayer水带
          if (ov.overlay._waterHoseLineLayer) {
            this.clearHoseLineLayer(ov.overlay._waterHoseLineLayer.id);
          }
        }
      });
      this.overlays = [];
      this.renderOverlayList();
    });

    document.getElementById('btnCloseDraw').addEventListener('click', () => {
      try { this.drawingManager.close(); } catch(e){}
      this.setActiveTool(null);
      alert('已停止绘图');
    });

    document.getElementById('toolMarker').addEventListener('click', () => {
      this.currentTool = 'marker';
      this.drawingManager.setDrawingMode('marker');
      this.drawingManager.open();
      this.setActiveTool('toolMarker');
    });
    document.getElementById('toolPolyline').addEventListener('click', () => {
      this.currentTool = 'polyline';
      this.drawingManager.setDrawingMode('polyline');
      this.drawingManager.open();
      this.setActiveTool('toolPolyline');
    });
    document.getElementById('toolPolygon').addEventListener('click', () => {
      this.currentTool = 'polygon';
      this.drawingManager.setDrawingMode('polygon');
      this.drawingManager.open();
      this.setActiveTool('toolPolygon');
    });
    document.getElementById('toolRectangle').addEventListener('click', () => {
      this.currentTool = 'rectangle';
      this.drawingManager.setDrawingMode('rectangle');
      this.drawingManager.open();
      this.setActiveTool('toolRectangle');
    });
    document.getElementById('toolCircle').addEventListener('click', () => {
      this.currentTool = 'circle';
      this.drawingManager.setDrawingMode('circle');
      this.drawingManager.open();
      this.setActiveTool('toolCircle');
    });

    document.getElementById('selectIconCategory').addEventListener('change', (ev) => {
      this.iconCategory = ev.target.value;
      this.iconPage = 1;
      this.renderIconPage();
    });
  },

  setActiveTool(btnId) {
    const ids = ['toolMarker','toolPolyline','toolPolygon','toolRectangle','toolCircle'];
    ids.forEach(id => {
      const el = document.getElementById(id);
      if (!el) return;
      if (btnId === id) el.classList.add('active'); else el.classList.remove('active');
    });
  },

  toggleBuilding(type, checked) {
    console.log(`toggleBuilding 调用 - 类型:${type}, 选中:${checked}`);
    console.log(`当前 buildingData[${type}]:`, this.buildingData[type]);
    
    if (checked) {
      this.showBuildingsByType(type);
    } else {
      this.clearBuildingMarkers(type);
      document.getElementById(type+'Count').textContent = this.buildingData[type] ? this.buildingData[type].length : 0;
    }
  },

  // 显示指定类型的建筑标记
  showBuildingsByType(type) {
    console.log(`showBuildingsByType 调用 - 类型:${type}`);
    
    // 清除现有标记
    this.clearBuildingMarkers(type);
    console.log(`已清除 ${type} 类型的现有标记`);
    
    // 检查是否有该类型的建筑数据
    if (!this.buildingData[type] || this.buildingData[type].length === 0) {
      console.log(`没有${type}类型的建筑数据`);
      return;
    }

    console.log(`开始显示 ${type} 类型的 ${this.buildingData[type].length} 个建筑`);

    // 为每个建筑创建标记
    this.buildingData[type].forEach((building, index) => {
      console.log(`创建第${index + 1}个${type}类型建筑标记:`, building);
      
      const point = new BMapGL.Point(building.lng, building.lat);
      
      // 创建标记
      const marker = new BMapGL.Marker(point);
      
      // 设置自定义图标（如果图标文件存在）
      try {
        const icon = new BMapGL.Icon(building.iconUrl, new BMapGL.Size(32, 32), {
          anchor: new BMapGL.Size(16, 32)
        });
        marker.setIcon(icon);
      } catch (e) {
        // 如果图标加载失败，使用默认标记
        console.warn('图标加载失败，使用默认标记:', building.iconUrl);
      }

      // 添加信息窗口
      const infoWindow = new BMapGL.InfoWindow(`
        <div style="padding: 10px; min-width: 200px;">
          <h6 style="margin: 0 0 8px 0; color: #333;">${building.orgName || '未知机构'}</h6>
          <p style="margin: 0 0 4px 0; font-size: 12px; color: #666;">
            <strong>类型:</strong> ${building.typeText}
          </p>
          <p style="margin: 0 0 4px 0; font-size: 12px; color: #666;">
            <strong>地址:</strong> ${building.address}
          </p>
          <p style="margin: 0; font-size: 12px; color: #666;">
            <strong>坐标:</strong> ${building.lng.toFixed(6)}, ${building.lat.toFixed(6)}
          </p>
        </div>
      `, {
        width: 250,
        height: 120
      });

      // 点击标记显示信息窗口
      marker.addEventListener('click', () => {
        this.map.openInfoWindow(infoWindow, point);
      });

      // 添加到地图和存储数组
      this.map.addOverlay(marker);
      
      if (!this.buildingMarkers[type]) {
        this.buildingMarkers[type] = [];
      }
      this.buildingMarkers[type].push(marker);
    });

    console.log(`显示${type}类型建筑标记: ${this.buildingMarkers[type].length}个`);
  },

  clearBuildingMarkers(type) {
    const list = this.buildingMarkers[type] || [];
    list.forEach(m => { try { this.map.removeOverlay(m); } catch(e){} });
    this.buildingMarkers[type] = [];
  },

  defaultOverlayName(type, idx) {
    switch(type) {
      case 'marker': 
        if (this.selectedMarkerIconName) {
          return `${this.selectedMarkerIconName} ${idx}`;
        }
        return `标记 ${idx}`;
      case 'polyline': return `水带 ${idx}`;
      case 'polygon': return `多边形 ${idx}`;
      case 'rectangle': return `矩形 ${idx}`;
      case 'circle': return `圆形 ${idx}`;
      default: return `图形 ${idx}`;
    }
  },

  renderOverlayList() {
    const listEl = document.getElementById('overlayList');
    listEl.innerHTML = '';
    this.overlays.forEach((ov) => {
      const row = document.createElement('div');
      row.className = 'overlay-item';
      const input = document.createElement('input');
      input.type = 'text';
      input.className = 'form-control overlay-name-input';
      input.value = ov.name;
      input.addEventListener('input', (ev) => {
        ov.name = ev.target.value;
        // 同步更新地图上的名称标注
        if (ov.nameLabel && typeof ov.nameLabel.setContent === 'function') {
          try { ov.nameLabel.setContent(ov.name); } catch(err){}
        } else {
          try { if (ov.nameLabel) this.map.removeOverlay(ov.nameLabel); } catch(err){}
          ov.nameLabel = this.createOverlayLabel(ov);
          try { this.map.addOverlay(ov.nameLabel); } catch(err){}
        }
      });
      const btnDel = document.createElement('button');
      btnDel.className = 'btn btn-sm btn-outline-danger';
      btnDel.textContent = '删除';
      btnDel.addEventListener('click', () => {
        try { this.map.removeOverlay(ov.overlay); } catch(e){}
        try { if (ov.nameLabel) this.map.removeOverlay(ov.nameLabel); } catch(e){}
        
        // 如果是水带，清理相关纹理覆盖物和水带头
        if (ov.type === 'polyline' && ov.overlay._waterHoseOverlay) {
          this.clearCustomOverlays(ov.overlay._waterHoseOverlay.id);
        }
        
        this.overlays = this.overlays.filter(x => x.id !== ov.id);
        this.renderOverlayList();
      });
      row.appendChild(input);
      row.appendChild(btnDel);
      listEl.appendChild(row);
    });
  },

  // 计算各类图形的标注位置
  getOverlayLabelPosition(ov) {
    try {
      switch (ov.type) {
        case 'marker': {
          const p = ov.overlay.getPosition();
          return new BMapGL.Point(p.lng, p.lat);
        }
        case 'circle': {
          const p = ov.overlay.getCenter();
          return new BMapGL.Point(p.lng, p.lat);
        }
        case 'polyline': {
          const path = ov.overlay.getPath();
          const mid = path[Math.floor(path.length / 2)] || path[0];
          return new BMapGL.Point(mid.lng, mid.lat);
        }
        case 'polygon':
        case 'rectangle': {
          const path = ov.overlay.getPath();
          if (path && path.length) {
            let sumLng = 0, sumLat = 0;
            path.forEach(pt => { sumLng += pt.lng; sumLat += pt.lat; });
            const lng = sumLng / path.length;
            const lat = sumLat / path.length;
            return new BMapGL.Point(lng, lat);
          }
          return null;
        }
        default:
          return null;
      }
    } catch(err) { return null; }
  },

  // 创建名称标注
  createOverlayLabel(ov) {
    const pos = this.getOverlayLabelPosition(ov);
    if (!pos) return null;
    try {
      const label = new BMapGL.Label(ov.name || '', { position: pos, offset: new BMapGL.Size(0, -18) });
      label.setStyle({
        color: '#000', backgroundColor: 'rgba(255,255,255,0.85)', border: '1px solid #999',
        fontSize: '12px', padding: '2px 6px', borderRadius: '4px', whiteSpace: 'nowrap'
      });
      return label;
    } catch(err) { return null; }
  },

  // 附加名称标注到地图
  attachOverlayLabel(ov) {
    try {
      const label = this.createOverlayLabel(ov);
      if (label) {
        this.map.addOverlay(label);
        ov.nameLabel = label;
      }
    } catch(err){}
  },
  

  // 设置水带样式
  setupWaterHoseStyle(polyline) {
    try {
      const path = polyline.getPath();
      if (!path || path.length < 2) return;

      // 使用LineLayer替代纹理覆盖物
      this.addHoseLineLayer(polyline);
    } catch(err) {
      console.error('设置水带样式失败:', err);
    }
  },

  // 使用LineLayer创建水带显示
  addHoseLineLayer(polyline) {
    try {
      const path = polyline.getPath();
      const id = 'hose-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
      
      // 隐藏原始polyline
      polyline.setStrokeOpacity(0);
      
      // 准备LineLayer数据
      const lineData = {
        type: 'FeatureCollection',
        features: [{
          type: 'Feature',
          properties: {
            name: id,
            type: 'hose'
          },
          geometry: {
            type: 'LineString',
            coordinates: path.map(point => [point.lng, point.lat])
          }
        }]
      };

      // 创建LineLayer
      if (!this.hoseLineLayer) {
        this.hoseLineLayer = new BMapGL.LineLayer({
          enablePicked: true,
          autoSelect: true,
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
            // 使用水带纹理图片
            strokeTextureUrl: 'assets/icons/line.png',
            strokeTextureWidth: 16,
            strokeTextureHeight: 64,
          }
        });

        // 添加点击事件
        this.hoseLineLayer.addEventListener('click', (e) => {
          if (e.value.dataIndex !== -1 && e.value.dataItem) {
            console.log('点击了水带:', e.value.dataItem.properties.name);
          }
        });

        // 将LineLayer添加到地图
        this.map.addNormalLayer(this.hoseLineLayer);
      }

      // 获取现有数据并添加新的水带线
      const existingData = this.hoseLineLayer.getData() || { type: 'FeatureCollection', features: [] };
      existingData.features.push(lineData.features[0]);
      
      // 更新LineLayer数据
      this.hoseLineLayer.setData(existingData);

      // 添加水带头标记
      const hoseHeadMarker = this.addHoseHeadMarker(polyline, null, id);

      // 存储LineLayer相关信息
      if (!this.hoseLineLayerData) {
        this.hoseLineLayerData = {};
      }
      this.hoseLineLayerData[id] = {
        polyline: polyline,
        hoseHeadMarker: hoseHeadMarker,
        featureIndex: existingData.features.length - 1
      };

      // 将LineLayer信息关联到polyline对象
      polyline._waterHoseLineLayer = { id: id };

      console.log('LineLayer水带创建成功:', id);
    } catch(err) {
      console.error('创建LineLayer水带失败:', err);
    }
  },

  // 清理LineLayer水带
  clearHoseLineLayer(id) {
    try {
      if (!this.hoseLineLayerData || !this.hoseLineLayerData[id]) return;

      const hoseData = this.hoseLineLayerData[id];
      
      // 移除水带头标记
      if (hoseData.hoseHeadMarker) {
        this.map.removeOverlay(hoseData.hoseHeadMarker);
      }

      // 从LineLayer数据中移除对应的feature
      if (this.hoseLineLayer) {
        const existingData = this.hoseLineLayer.getData();
        if (existingData && existingData.features) {
          existingData.features = existingData.features.filter(feature => 
            feature.properties.name !== id
          );
          this.hoseLineLayer.setData(existingData);
        }
      }

      // 清理数据记录
      delete this.hoseLineLayerData[id];
      
      console.log('LineLayer水带清理成功:', id);
    } catch(err) {
      console.error('清理LineLayer水带失败:', err);
    }
  },

  // 添加水带纹理
  addHoseTexture(polyline) {
    try {
      const path = polyline.getPath();
      const id = 'hose-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
      
      // 确保纹理图片只加载一次
      if (!this.textureImg) {
        this.textureImg = new Image();
        this.textureImg.crossOrigin = 'anonymous';
        this.textureImg.src = 'assets/icons/line.png';
        
        this.textureImg.onload = () => {
          console.log('水带纹理图片加载成功');
          // 触发重绘所有水带纹理
          if (this.customOverlays) {
            Object.values(this.customOverlays).forEach(overlay => {
              if (overlay.textureOverlay && overlay.textureOverlay.draw) {
                overlay.textureOverlay.draw();
              }
            });
          }
        };
        
        this.textureImg.onerror = () => {
          console.error('水带纹理图片加载失败');
        };
      }

      // 创建纹理覆盖物
      const textureOverlay = this.createTextureOverlay(path, this.textureImg, id);
      this.map.addOverlay(textureOverlay);

      // 添加水带头标记
      const hoseHeadMarker = this.addHoseHeadMarker(polyline, textureOverlay, id);

      // 存储自定义覆盖物
      const overlayId = 'hose_' + Date.now();
      this.customOverlays[overlayId] = { textureOverlay, hoseHeadMarker };

      // 将覆盖物关联到polyline对象
      polyline._waterHoseOverlay = { id: overlayId };
    } catch(err) {
      console.error('添加水带纹理失败:', err);
    }
  },

  // 创建百度地图水带纹理覆盖物类
  createTextureOverlay(points, textureImg, id) {
    const _this = this;
    
    function TextureOverlay(points, texture, id) {
      this._points = points;
      this._texture = texture;
      this._id = id;
    }

    TextureOverlay.prototype = new BMapGL.Overlay();
    
    TextureOverlay.prototype.initialize = function(map) {
      this._map = map;
      const canvas = document.createElement('canvas');
      canvas.style.position = 'absolute';
      canvas.style.zIndex = '10';
      canvas.width = map.getContainer().clientWidth;
      canvas.height = map.getContainer().clientHeight;
      
      map.getPanes().labelPane.appendChild(canvas);
      this._canvas = canvas;
      return canvas;
    };

    TextureOverlay.prototype.draw = function() {
      const map = this._map;
      const canvas = this._canvas;
      const ctx = canvas.getContext('2d');
      
      // 清除画布
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      
      if (!this._texture || !this._texture.complete) {
        _this.drawTemporaryHose(ctx, this._points, map);
      } else {
        _this.drawHoseTexture(ctx, this._points, this._texture, map);
      }
    };

    return new TextureOverlay(points, textureImg, id);
  },

  // 绘制水带纹理
  drawHoseTexture(ctx, points, textureImg, map) {
    if (!points || points.length < 2) return;

    ctx.save();

    for (let i = 0; i < points.length - 1; i++) {
      const p1 = map.pointToPixel(points[i]);
      const p2 = map.pointToPixel(points[i + 1]);

      const dx = p2.x - p1.x;
      const dy = p2.y - p1.y;
      const length = Math.sqrt(dx * dx + dy * dy);
      const angle = Math.atan2(dy, dx);

      ctx.save();
      ctx.translate(p1.x, p1.y);
      ctx.rotate(angle);

      // 创建纹理图案
      const pattern = ctx.createPattern(textureImg, 'repeat');
      ctx.fillStyle = pattern;
      ctx.fillRect(0, 0, length, 15);

      ctx.restore();
    }

    ctx.restore();
  },

  // 绘制临时水带（图片加载前）
  drawTemporaryHose(ctx, points, map) {
    if (!points || points.length < 2) return;

    ctx.beginPath();
    const firstPixel = map.pointToPixel(points[0]);
    ctx.moveTo(firstPixel.x, firstPixel.y);

    for (let i = 1; i < points.length; i++) {
      const pixel = map.pointToPixel(points[i]);
      ctx.lineTo(pixel.x, pixel.y);
    }

    ctx.lineWidth = 15;
    ctx.strokeStyle = '#000';
    ctx.lineJoin = 'round';
    ctx.lineCap = 'round';
    ctx.stroke();
  },

  // 添加水带头标记
  addHoseHeadMarker(polyline, textureOverlay, id) {
    try {
      const points = polyline.getPath();
      if (!points || points.length < 2) return;

      const headImgSrc = 'assets/icons/line_top.png';
      
      // 计算水带头方向角度
      let rotation = 0;
      const startPoint = points[0];
      const nextPoint = points[1];
      const dx = nextPoint.lng - startPoint.lng;
      const dy = nextPoint.lat - startPoint.lat;
      
      // 使用与Vue相同的角度计算方式
      rotation = Math.atan2(-dy, dx) * 180 / Math.PI + 90;

      const icon = new BMapGL.Icon(headImgSrc, new BMapGL.Size(20, 20), {
        anchor: new BMapGL.Size(10, 10)
      });
      const headMarker = new BMapGL.Marker(points[0], { icon: icon  });
      
      // 旋转水带头图标
      this.rotateIcon(headMarker, rotation);
      
      this.map.addOverlay(headMarker);

      // 返回水带头标记，由调用方管理
      return headMarker;
    } catch(err) {
      console.error('添加水带头标记失败:', err);
    }
  },

  // 计算路径总距离
  calculatePathDistance(path) {
    let totalDistance = 0;
    for (let i = 1; i < path.length; i++) {
      const distance = this.map.getDistance(path[i-1], path[i]);
      totalDistance += distance;
    }
    return totalDistance;
  },

  // 获取路径上指定比例位置的坐标
  getPositionAlongPath(path, ratio) {
    if (ratio <= 0) return path[0];
    if (ratio >= 1) return path[path.length - 1];

    const totalDistance = this.calculatePathDistance(path);
    const targetDistance = totalDistance * ratio;
    
    let currentDistance = 0;
    for (let i = 1; i < path.length; i++) {
      const segmentDistance = this.map.getDistance(path[i-1], path[i]);
      if (currentDistance + segmentDistance >= targetDistance) {
        const segmentRatio = (targetDistance - currentDistance) / segmentDistance;
        const lng = path[i-1].lng + (path[i].lng - path[i-1].lng) * segmentRatio;
        const lat = path[i-1].lat + (path[i].lat - path[i-1].lat) * segmentRatio;
        return new BMapGL.Point(lng, lat);
      }
      currentDistance += segmentDistance;
    }
    return path[path.length - 1];
  },

  // 获取路径上指定位置的角度
  getAngleAtPosition(path, ratio) {
    try {
      let segmentIndex = 0;
      if (ratio >= 1) {
        segmentIndex = path.length - 2;
      } else {
        const totalDistance = this.calculatePathDistance(path);
        const targetDistance = totalDistance * ratio;
        let currentDistance = 0;
        
        for (let i = 1; i < path.length; i++) {
          const segmentDistance = this.map.getDistance(path[i-1], path[i]);
          if (currentDistance + segmentDistance >= targetDistance) {
            segmentIndex = i - 1;
            break;
          }
          currentDistance += segmentDistance;
        }
      }

      if (segmentIndex >= 0 && segmentIndex < path.length - 1) {
        const p1 = path[segmentIndex];
        const p2 = path[segmentIndex + 1];
        
        // 计算角度（弧度转角度）
        // 地图坐标系：经度(lng)是X轴，纬度(lat)是Y轴
        const deltaX = p2.lng - p1.lng;
        const deltaY = p2.lat - p1.lat;
        
        // 计算从p1指向p2的角度（以东方向为0度，逆时针为正）
        let angle = Math.atan2(deltaY, deltaX) * 180 / Math.PI;
        
        // 水带头部图标原始朝向是向右（东方向）
        // 需要将数学角度系统转换为地图角度系统（正上方为0度，顺时针为正）
        // 数学系统：东=0°，北=90°，西=180°，南=270°
        // 地图系统：北=0°，东=90°，南=180°，西=270°
        // 转换公式：地图角度 = 90° - 数学角度
        angle = 270 - angle;
        
        // 确保角度在0-360度范围内
        while (angle < 0) angle += 360;
        while (angle >= 360) angle -= 360;
        
        return angle;
      }
      return null;
    } catch(err) {
      return null;
    }
  },

  // 旋转图标
  rotateIcon(marker, angle) {
    try {
      // 百度地图的图标旋转需要通过CSS transform实现
      const icon = marker.getIcon();
      if (icon && icon.imageUrl) {
        // 创建带旋转的图标
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
    } catch(err) {
      console.error('旋转图标失败:', err);
    }
  },

  renderIconPage() {
    const iconListEl = document.getElementById('iconList');
    const pagEl = document.getElementById('iconPagination');
    iconListEl.innerHTML = '';
    pagEl.innerHTML = '';

    const list = (this.iconCategory && this.iconCategory !== 'all')
      ? (this.iconCategories.find(c => c.name === this.iconCategory)?.icons || [])
      : this.allIconsFlat;
    const total = list.length;
    this.iconTotal = total;

    const start = (this.iconPage - 1) * this.iconPageSize;
    const pageItems = list.slice(start, start + this.iconPageSize);

    pageItems.forEach(icon => {
      const item = document.createElement('div');
      item.className = 'icon-item';
      const img = document.createElement('img');
      img.className = 'icon-img';
      img.src = icon.url;
      img.title = icon.name;
      const span = document.createElement('span');
      span.className = 'icon-name';
      span.textContent = icon.name;
      item.appendChild(img);
      item.appendChild(span);
      item.addEventListener('click', () => {
        this.selectedMarkerIcon = icon.url;
        this.selectedMarkerIconName = icon.name;
        this.iconModal.hide();
        this.currentTool = 'marker';
        this.drawingManager.setDrawingMode('marker');
        this.drawingManager.open();
        this.setActiveTool('toolMarker');
      });
      iconListEl.appendChild(item);
    });

    const pages = Math.ceil(this.iconTotal / this.iconPageSize) || 1;
    const addPageItem = (page, text, active=false) => {
      const li = document.createElement('li');
      li.className = 'page-item' + (active? ' active':'');
      const a = document.createElement('a');
      a.className = 'page-link';
      a.href = '#';
      a.textContent = text;
      a.addEventListener('click', (ev) => {
        ev.preventDefault();
        this.iconPage = page;
        this.renderIconPage();
      });
      li.appendChild(a);
      pagEl.appendChild(li);
    };
    addPageItem(Math.max(1, this.iconPage-1), '«');
    for (let p = 1; p <= pages && p <= this.iconPage+4; p++) {
      if (p >= this.iconPage-2) addPageItem(p, String(p), p===this.iconPage);
    }
    addPageItem(Math.min(pages, this.iconPage+1), '»');
  },

  serializeOverlays() {
    const centerPt = this.map.getCenter();
    const center = { lng: centerPt.lng, lat: centerPt.lat };
    const zoom = this.map.getZoom();
    const items = this.overlays.map(ov => {
      const o = { id: ov.id, type: ov.type, name: ov.name };
      switch (ov.type) {
        case 'marker': {
          const p = ov.overlay.getPosition();
          o.point = { lng: p.lng, lat: p.lat };
          // 输出图标（两种字段，兼容review.js的 data.icon.url 与后端的 style.iconUrl）
          if (ov.icon && ov.icon.url) {
            o.icon = { url: ov.icon.url };
          }
          if (ov.style && ov.style.iconUrl) {
            o.style = Object.assign({}, ov.style);
          }
          break;
        }
        case 'circle': {
          const p = ov.overlay.getCenter();
          o.center = { lng: p.lng, lat: p.lat };
          o.radius = ov.overlay.getRadius();
          break;
        }
        default: {
          const path = ov.overlay.getPath();
          o.path = path.map(pt => ({ lng: pt.lng, lat: pt.lat }));
          // 其他图形暂不输出样式细节，如需可在此扩展 strokeColor 等
          break;
        }
      }
      return o;
    });
    return { center, zoom, overlays: items };
  },

  // 获取地图绘制答案数据，供WPF调用
  getMapDrawingAnswerData() {
    try {
      const data = this.serializeOverlays();
      console.log('获取地图绘制答案数据:', data);
      return JSON.stringify(data);
    } catch (error) {
      console.error('获取地图绘制答案数据失败:', error);
      return JSON.stringify({ center: null, zoom: null, overlays: [] });
    }
  },

  // 检查是否有绘制内容
  hasDrawingContent() {
    return this.overlays && this.overlays.length > 0;
  },

  // 获取绘制统计信息
  getDrawingStats() {
    const stats = {
      totalCount: this.overlays.length,
      markerCount: 0,
      polylineCount: 0,
      polygonCount: 0,
      rectangleCount: 0,
      circleCount: 0
    };
    
    this.overlays.forEach(ov => {
      switch (ov.type) {
        case 'marker': stats.markerCount++; break;
        case 'polyline': stats.polylineCount++; break;
        case 'polygon': stats.polygonCount++; break;
        case 'rectangle': stats.rectangleCount++; break;
        case 'circle': stats.circleCount++; break;
      }
    });
    
    return stats;
  },

  importOverlays(data) {
    try {
      if (!data || !Array.isArray(data.overlays)) throw new Error('格式错误');
      if (data.center && typeof data.center.lat === 'number' && typeof data.center.lng === 'number') {
        this.map.centerAndZoom(new BMapGL.Point(data.center.lng, data.center.lat), data.zoom || this.map.getZoom());
      }
      this.overlays.forEach(ov => { try { this.map.removeOverlay(ov.overlay); } catch(e){} });
      this.overlays = [];
      data.overlays.forEach((o, idx) => {
        let overlay = null;
        switch (o.type) {
          case 'marker': {
            const pt = new BMapGL.Point(o.point.lng, o.point.lat);
            overlay = new BMapGL.Marker(pt);
            // 应用图标（支持两种来源字段）
            const iconUrl = (o.icon && o.icon.url) ? o.icon.url : (o.style && o.style.iconUrl ? o.style.iconUrl : null);
            if (iconUrl) {
              try {
                const icon = new BMapGL.Icon(iconUrl, new BMapGL.Size(36,36), { anchor: new BMapGL.Size(18,36) });
                overlay.setIcon(icon);
              } catch(err) {
                console.warn('导入标记图标失败:', err);
              }
            }
            break;
          }
          case 'circle': {
            const pt = new BMapGL.Point(o.center.lng, o.center.lat);
            overlay = new BMapGL.Circle(pt, o.radius || 100);
            break;
          }
          case 'polyline': {
            const path = (o.path || []).map(p => new BMapGL.Point(p.lng, p.lat));
            overlay = new BMapGL.Polyline(path);
            break;
          }
          case 'polygon':
          case 'rectangle': {
            const path = (o.path || []).map(p => new BMapGL.Point(p.lng, p.lat));
            overlay = new BMapGL.Polygon(path);
            break;
          }
          default:
            overlay = null;
        }
        if (overlay) {
          this.map.addOverlay(overlay);
          const id = o.id || ('ov-' + (idx+1));
          const name = o.name || this.defaultOverlayName(o.type, idx+1);
          const item = { id, overlay, type: o.type, name };
          // 保存导入的图标信息到 overlays 项目，确保再次序列化时不丢失
          if (o.type === 'marker') {
            const iconUrl = (o.icon && o.icon.url) ? o.icon.url : (o.style && o.style.iconUrl ? o.style.iconUrl : null);
            if (iconUrl) {
              item.style = Object.assign({}, item.style, { iconUrl });
              item.icon = { url: iconUrl };
            }
          }
          this.overlays.push(item);
          // 显示导入图形的名称标注
          this.attachOverlayLabel(item);
        }
      });
      this.renderOverlayList();
    } catch(err) {
      alert('导入失败：' + err.message);
    }
  },

  // 加载建筑数据并按类型分类存储
  loadBuildingData(buildingData) {
    try {
      console.log('开始加载建筑数据:', buildingData);
      console.log('建筑数据类型:', typeof buildingData, '是否为数组:', Array.isArray(buildingData));
      
      if (!Array.isArray(buildingData)) {
        console.error('建筑数据格式错误，应为数组');
        return;
      }

      // 清除现有建筑标记和数据
      this.clearBuildingMarkers('dz');
      this.clearBuildingMarkers('zz');
      this.clearBuildingMarkers('zd');
      
      // 重置建筑数据存储
      this.buildingData = { dz: [], zz: [], zd: [] };
      console.log('已重置建筑数据存储');

      let dzCount = 0, zzCount = 0, zdCount = 0;

      buildingData.forEach((building, index) => {
        console.log(`处理第${index + 1}个建筑:`, building);
        
        // 兼容两种数据格式：WPF传递的格式和原有格式
        let lng, lat, orgType, orgName;
        
        if (building.longitude !== undefined && building.latitude !== undefined) {
          // WPF传递的格式
          lng = parseFloat(building.longitude);
          lat = parseFloat(building.latitude);
          orgType = building.type; // 1-消防队站；2-专职队；3-重点建筑
          orgName = building.name;
          console.log(`WPF格式 - 经度:${lng}, 纬度:${lat}, 类型:${orgType}, 名称:${orgName}`);
        } else if (building.gps && building.orgType) {
          // 原有格式
          const gpsArray = building.gps.split(',');
          if (gpsArray.length !== 2) {
            console.warn(`建筑${index + 1} GPS格式错误:`, building.gps);
            return;
          }
          lng = parseFloat(gpsArray[0]);
          lat = parseFloat(gpsArray[1]);
          orgType = building.orgType;
          orgName = building.orgName;
          console.log(`原有格式 - 经度:${lng}, 纬度:${lat}, 类型:${orgType}, 名称:${orgName}`);
        } else {
          console.warn(`建筑${index + 1} 数据格式不正确，跳过:`, building);
          return; // 数据格式不正确，跳过
        }

        if (isNaN(lng) || isNaN(lat)) {
          console.warn(`建筑${index + 1} 坐标无效，跳过 - 经度:${lng}, 纬度:${lat}`);
          return;
        }

        // 根据机构类型确定标记类型和图标
        let markerType = '';
        let iconUrl = '';
        let typeText = '';
        
        if (typeof orgType === 'number') {
          // WPF传递的数字类型
          switch (orgType) {
            case 1:
              markerType = 'dz';
              iconUrl = 'assets/icons/fire-station.png';
              typeText = '消防队站';
              dzCount++;
              break;
            case 2:
              markerType = 'zz';
              iconUrl = 'assets/icons/professional-team.png';
              typeText = '专职队';
              zzCount++;
              break;
            case 3:
              markerType = 'zd';
              iconUrl = 'assets/icons/key-building.png';
              typeText = '重点建筑';
              zdCount++;
              break;
            default:
              return; // 未知类型，跳过
          }
        } else {
          // 原有的字符串类型
          switch (orgType) {
            case '消防队站':
              markerType = 'dz';
              iconUrl = 'assets/icons/fire-station.svg';
              typeText = '消防队站';
              dzCount++;
              break;
            case '专职队':
              markerType = 'zz';
              iconUrl = 'assets/icons/professional-team.svg';
              typeText = '专职队';
              zzCount++;
              break;
            case '重点建筑':
              markerType = 'zd';
              iconUrl = 'assets/icons/key-building.svg';
              typeText = '重点建筑';
              zdCount++;
              break;
            default:
              return; // 未知类型，跳过
          }
        }

        // 将建筑数据存储到对应类型的数组中
        const buildingInfo = {
          lng: lng,
          lat: lat,
          orgName: orgName,
          orgType: orgType,
          typeText: typeText,
          iconUrl: iconUrl,
          address: building.address || '未知',
          originalData: building
        };
        
        this.buildingData[markerType].push(buildingInfo);
        console.log(`建筑${index + 1} 已存储到 ${markerType} 类型:`, buildingInfo);
      });

      console.log('建筑数据处理完成，最终存储结果:');
      console.log('消防队站(dz):', this.buildingData.dz.length, this.buildingData.dz);
      console.log('专职队(zz):', this.buildingData.zz.length, this.buildingData.zz);
      console.log('重点建筑(zd):', this.buildingData.zd.length, this.buildingData.zd);

      // 更新计数显示
      document.getElementById('dzCount').textContent = dzCount;
      document.getElementById('zzCount').textContent = zzCount;
      document.getElementById('zdCount').textContent = zdCount;
      
      console.log(`计数更新 - 消防队站:${dzCount}, 专职队:${zzCount}, 重点建筑:${zdCount}`);

      console.log(`建筑数据加载完成: 消防队站${dzCount}个, 专职队${zzCount}个, 重点建筑${zdCount}个`);
      
      // 检查当前复选框状态，显示已勾选的建筑类型
      ['dz', 'zz', 'zd'].forEach(type => {
        const checkbox = document.getElementById('chk' + type.charAt(0).toUpperCase() + type.slice(1));
        if (checkbox && checkbox.checked) {
          this.showBuildingsByType(type);
        }
      });
      
      // 通知WPF数据加载完成
      this.notifyWPF('buildingDataLoaded', {
        total: buildingData.length,
        dzCount,
        zzCount,
        zdCount
      });

    } catch (error) {
      console.error('加载建筑数据失败:', error);
      this.notifyWPF('buildingDataError', { error: error.message });
    }
  },

  // 请求建筑数据
  requestBuildingData(cityName) {
    console.log('请求建筑数据:', cityName);
    this.notifyWPF('requestBuildingData', { cityName: cityName });
  },

  // 向WPF发送消息
  notifyWPF(type, data) {
    console.log(`[notifyWPF] 准备发送消息到WPF:`, { type, data });
    
    if (window.chrome && window.chrome.webview) {
      try {
        const message = {
          type: type,
          data: data,
          timestamp: new Date().toISOString()
        };
        console.log(`[notifyWPF] 发送消息结构:`, message);
        
        window.chrome.webview.postMessage(message);
        console.log(`[notifyWPF] 消息发送成功`);
      } catch (error) {
        console.error('[notifyWPF] 发送消息到WPF失败:', error);
      }
    } else {
      console.warn('[notifyWPF] WebView2环境不可用，无法发送消息到WPF');
    }
  },

  // 初始化WebView2消息监听
  initWebViewMessageListener() {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.addEventListener('message', (event) => {
        try {
          const message = event.data;
          console.log('收到WPF消息:', message);

          switch (message.type) {
            case 'wpfReady':
              // WPF已准备好接收消息，再次发送页面就绪，避免首次消息丢失
              console.log('[WebView2] 收到 wpfReady 握手，回发 pageReady');
              this.notifyWPF('pageReady', { status: 'ready' });
              break;
            case 'loadBuildingData':
              this.loadBuildingData(message.data);
              break;
            case 'buildingDataResponse':
              // 处理从WPF返回的建筑数据
              console.log('收到建筑数据响应:', message);
              console.log('建筑数据数量:', message.buildings ? message.buildings.length : 0);
              this.loadBuildingData(message.buildings);
              break;
            case 'buildingDataError':
              console.error('建筑数据加载失败:', message.error);
              break;
            case 'centerMap':
              if (message.data.lng && message.data.lat) {
                const point = new BMapGL.Point(message.data.lng, message.data.lat);
                this.map.centerAndZoom(point, message.data.zoom || 15);
              }
              break;
            case 'clearBuildings':
              this.clearBuildingMarkers('dz');
              this.clearBuildingMarkers('zz');
              this.clearBuildingMarkers('zd');
              document.getElementById('dzCount').textContent = 0;
              document.getElementById('zzCount').textContent = 0;
              document.getElementById('zdCount').textContent = 0;
              break;
            case 'getMapData':
              // 返回当前地图数据
              const mapData = this.serializeOverlays();
              this.notifyWPF('mapDataResponse', mapData);
              break;
            case 'getMapDrawingData':
              // 返回地图绘制答案数据
              console.log('收到获取地图绘制数据请求');
              const drawingData = this.getMapDrawingAnswerData();
              console.log('返回地图绘制数据:', drawingData);
              this.notifyWPF('mapDrawingDataResponse', { data: drawingData });
              break;
            default:
              console.warn('未知消息类型:', message.type);
          }
        } catch (error) {
          console.error('处理WPF消息失败:', error);
          this.notifyWPF('messageError', { error: error.message });
        }
      });

      // 通知WPF页面已准备就绪
      this.notifyWPF('pageReady', { status: 'ready' });
    }
  },

  // 清理自定义覆盖物（纹理覆盖物和水带头标记）
  clearCustomOverlays(overlayId) {
    if (this.customOverlays && this.customOverlays[overlayId]) {
      const customOverlay = this.customOverlays[overlayId];
      
      // 清理纹理覆盖物
      if (customOverlay.textureOverlay) {
        try { this.map.removeOverlay(customOverlay.textureOverlay); } catch(e){}
      }
      
      // 清理水带头标记
      if (customOverlay.hoseHeadMarker) {
        try { this.map.removeOverlay(customOverlay.hoseHeadMarker); } catch(e){}
      }
      
      // 从记录中删除
      delete this.customOverlays[overlayId];
    }
  },

  // 隐藏编辑工具（只读模式）
  hideEditingTools() {
    // 隐藏绘图工具栏
    const toolbar = document.querySelector('.bottom-toolbar');
    if (toolbar) toolbar.style.display = 'none';
    
    // 隐藏控制面板
    const controlPanels = document.querySelector('.control-panels');
    if (controlPanels) controlPanels.style.display = 'none';
    
    // 隐藏地图提示
    const mapHint = document.querySelector('#mapHint');
    if (mapHint) mapHint.style.display = 'none';
    
    // 添加只读模式提示
    const mapContainer = document.querySelector('#map');
    if (mapContainer) {
      const reviewHint = document.createElement('div');
      reviewHint.className = 'review-hint';
      reviewHint.innerHTML = '<strong>查看模式：</strong>正在显示考生的地图绘制答案';
      reviewHint.style.cssText = `
        position: absolute;
        top: 10px;
        left: 10px;
        background: rgba(0, 123, 255, 0.9);
        color: white;
        padding: 8px 12px;
        border-radius: 4px;
        font-size: 14px;
        z-index: 1000;
        box-shadow: 0 2px 4px rgba(0,0,0,0.2);
      `;
      mapContainer.appendChild(reviewHint);
    }
  },

  // 加载只读模式的地图数据
  loadReviewMapData() {
    if (!this.reviewMapData || !Array.isArray(this.reviewMapData)) {
      console.warn('没有有效的地图数据可加载');
      return;
    }

    console.log('加载地图数据:', this.reviewMapData);

    // 清空现有覆盖物
    this.overlays = [];
    
    // 加载每个覆盖物
    this.reviewMapData.forEach((item, index) => {
      try {
        this.loadReviewOverlay(item, index);
      } catch (e) {
        console.error('加载覆盖物失败:', item, e);
      }
    });

    // 更新覆盖物列表显示
    this.renderOverlayList();
  },

  // 加载单个只读覆盖物
  loadReviewOverlay(data, index) {
    let overlay = null;
    const overlayId = `review_${index}`;

    switch (data.type) {
      case 'marker':
        overlay = new BMapGL.Marker(new BMapGL.Point(data.lng, data.lat));
        if (data.iconUrl) {
          const icon = new BMapGL.Icon(data.iconUrl, new BMapGL.Size(32, 32));
          overlay.setIcon(icon);
        }
        break;

      case 'polyline':
        if (data.points && data.points.length > 1) {
          const points = data.points.map(p => new BMapGL.Point(p.lng, p.lat));
          overlay = new BMapGL.Polyline(points, {
            strokeColor: data.strokeColor || '#FF0000',
            strokeWeight: data.strokeWeight || 3,
            strokeOpacity: data.strokeOpacity || 0.8
          });
        }
        break;

      case 'polygon':
        if (data.points && data.points.length > 2) {
          const points = data.points.map(p => new BMapGL.Point(p.lng, p.lat));
          overlay = new BMapGL.Polygon(points, {
            strokeColor: data.strokeColor || '#FF0000',
            strokeWeight: data.strokeWeight || 2,
            strokeOpacity: data.strokeOpacity || 0.8,
            fillColor: data.fillColor || '#FF0000',
            fillOpacity: data.fillOpacity || 0.3
          });
        }
        break;

      case 'rectangle':
        if (data.bounds) {
          const sw = new BMapGL.Point(data.bounds.sw.lng, data.bounds.sw.lat);
          const ne = new BMapGL.Point(data.bounds.ne.lng, data.bounds.ne.lat);
          overlay = new BMapGL.Polygon([
            sw,
            new BMapGL.Point(ne.lng, sw.lat),
            ne,
            new BMapGL.Point(sw.lng, ne.lat)
          ], {
            strokeColor: data.strokeColor || '#FF0000',
            strokeWeight: data.strokeWeight || 2,
            strokeOpacity: data.strokeOpacity || 0.8,
            fillColor: data.fillColor || '#FF0000',
            fillOpacity: data.fillOpacity || 0.3
          });
        }
        break;

      case 'circle':
        if (data.center && data.radius) {
          overlay = new BMapGL.Circle(
            new BMapGL.Point(data.center.lng, data.center.lat),
            data.radius,
            {
              strokeColor: data.strokeColor || '#FF0000',
              strokeWeight: data.strokeWeight || 2,
              strokeOpacity: data.strokeOpacity || 0.8,
              fillColor: data.fillColor || '#FF0000',
              fillOpacity: data.fillOpacity || 0.3
            }
          );
        }
        break;
    }

    if (overlay) {
      this.map.addOverlay(overlay);
      this.overlays.push({
        id: overlayId,
        overlay: overlay,
        type: data.type,
        name: data.name || `${data.type}_${index + 1}`
      });
    }
  }
};

document.addEventListener('DOMContentLoaded', function () {
  App.init();
});
