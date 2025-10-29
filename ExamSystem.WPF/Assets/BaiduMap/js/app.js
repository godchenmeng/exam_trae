// 简化的图标数据（示例）
const IconStore = (() => {
  function svgData(color) {
    const svg = `<svg xmlns='http://www.w3.org/2000/svg' width='48' height='48'><circle cx='24' cy='24' r='20' fill='${color}'/></svg>`;
    return 'data:image/svg+xml;base64,' + btoa(svg);
  }
  const allIcons = Array.from({length: 60}).map((_, i) => ({
    id: i+1,
    name: '图标 ' + (i+1),
    data: svgData(['#007bff','#28a745','#ffc107','#dc3545','#6f42c1'][i%5]),
    category: ['building','fire','poi'][i%3]
  }));
  return {
    query: function(category, page, pageSize) {
      const list = category && category!=='all' ? allIcons.filter(x=>x.category===category) : allIcons;
      const total = list.length;
      const start = (page-1)*pageSize;
      const pageList = list.slice(start, start+pageSize);
      return { total, list: pageList };
    }
  };
})();

// 页面主逻辑（基于 BMapGL WebGL）
const App = {
  map: null,
  drawingManager: null,
  currentTool: null,
  overlays: [], // { id, overlay, type, name }
  selectedMarkerIcon: null,
  iconCategory: 'all',
  iconPage: 1,
  iconPageSize: 42,
  iconTotal: 0,
  iconModal: null,
  buildingMarkers: { dz: [], zz: [], zd: [] },
  // 确认定位按钮的目标缩放级别（数值越大越“放大”）。如需改为更远视野，可将其改小。
  confirmCenterZoom: 18,

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
    const c = this.cityCenters.guiyang;
    this.map.centerAndZoom(new BMapGL.Point(c.lng, c.lat), c.zoom);
    this.map.enableScrollWheelZoom(true);
    this.map.addControl(new BMapGL.ZoomControl());
    this.map.addControl(new BMapGL.ScaleControl());
    this.map.addControl(new BMapGL.MapTypeControl({anchor: BMapGL.BMAP_ANCHOR_TOP_RIGHT}));

    // 绘图管理器（BMapGLLib DrawingManager）
    this.initDrawingManager();

    // 表单验证
    this.initFormValidation();

    // 图标模态框
    this.iconModal = new bootstrap.Modal(document.getElementById('iconModal'));
    this.renderIconPage();

    // 绑定事件
    this.bindEvents();

    // 初始渲染覆盖物列表
    this.renderOverlayList();
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

      const id = 'ov-' + (this.overlays.length + 1);
      const name = this.defaultOverlayName(type, this.overlays.length + 1);
      const item = { id, overlay, type, name };
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
                alert('未能找到该地名，请使用 "lat,lng" 格式或更换关键词');
              }
            }, cityName);
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
      }
    });

    document.getElementById('chkDz').addEventListener('change', (ev) => this.toggleBuilding('dz', ev.target.checked));
    document.getElementById('chkZz').addEventListener('change', (ev) => this.toggleBuilding('zz', ev.target.checked));
    document.getElementById('chkZd').addEventListener('change', (ev) => this.toggleBuilding('zd', ev.target.checked));

    document.getElementById('btnClear').addEventListener('click', () => {
      this.overlays.forEach(ov => { try { this.map.removeOverlay(ov.overlay); } catch(e){} });
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
    if (checked) {
      const cityKey = document.getElementById('selectCity').value;
      const c = this.cityCenters[cityKey];
      if (!c) return;
      const base = [c.lat, c.lng];
      const offsets = [[0.01,0.01],[0.015,-0.008],[-0.012,0.014],[-0.02,-0.006]];
      const markers = offsets.map((off,i) => {
        const pt = new BMapGL.Point(base[1]+off[1], base[0]+off[0]);
        const m = new BMapGL.Marker(pt);
        m.setTitle(`${type.toUpperCase()} 标记 ${i+1}`);
        this.map.addOverlay(m);
        return m;
      });
      this.buildingMarkers[type] = markers;
      document.getElementById(type+'Count').textContent = markers.length;
    } else {
      this.clearBuildingMarkers(type);
      document.getElementById(type+'Count').textContent = 0;
    }
  },

  clearBuildingMarkers(type) {
    const list = this.buildingMarkers[type] || [];
    list.forEach(m => { try { this.map.removeOverlay(m); } catch(e){} });
    this.buildingMarkers[type] = [];
  },

  defaultOverlayName(type, idx) {
    switch(type) {
      case 'marker': return `标记 ${idx}`;
      case 'polyline': return `线 ${idx}`;
      case 'polygon': return `多边形 ${idx}`;
      case 'rectangle': return `矩形 ${idx}`;
      case 'circle': return `圆 ${idx}`;
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

  renderIconPage() {
    const res = IconStore.query(this.iconCategory, this.iconPage, this.iconPageSize);
    this.iconTotal = res.total;
    const iconListEl = document.getElementById('iconList');
    iconListEl.innerHTML = '';
    res.list.forEach(icon => {
      const item = document.createElement('div');
      item.className = 'icon-item';
      const img = document.createElement('img');
      img.className = 'icon-img';
      img.src = icon.data;
      img.title = icon.name;
      const span = document.createElement('span');
      span.className = 'icon-name';
      span.textContent = icon.name;
      item.appendChild(img);
      item.appendChild(span);
      item.addEventListener('click', () => {
        this.selectedMarkerIcon = icon.data;
        this.iconModal.hide();
        this.currentTool = 'marker';
        this.drawingManager.setDrawingMode('marker');
        this.drawingManager.open();
        this.setActiveTool('toolMarker');
      });
      iconListEl.appendChild(item);
    });

    const pagEl = document.getElementById('iconPagination');
    pagEl.innerHTML = '';
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
          break;
        }
      }
      return o;
    });
    return { center, zoom, overlays: items };
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
          this.overlays.push(item);
          // 显示导入图形的名称标注
          this.attachOverlayLabel(item);
        }
      });
      this.renderOverlayList();
    } catch(err) {
      alert('导入失败：' + err.message);
    }
  }
};

document.addEventListener('DOMContentLoaded', function () {
  App.init();
});
