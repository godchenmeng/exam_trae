/**
 * 地图绘制题学生作答页面核心脚本
 * 负责地图初始化、绘图工具、数据管理和与WPF的通信
 */

class MapDrawingAnswering {
    constructor() {
        this.map = null;
        this.currentTool = 'point';
        this.isDrawing = false;
        this.overlayCount = 0;
        this.questionConfig = null;
        this.studentOverlays = [];
        this.guideOverlays = [];
        
        // 工具映射
        this.toolNames = {
            'point': '点',
            'line': '线',
            'polygon': '多边形',
            'circle': '圆形',
            'edit': '编辑',
            'delete': '删除'
        };
        
        this.init();
    }
    
    /**
     * 初始化地图和控件
     */
    init() {
        this.initMap();
        this.initControls();
        this.bindEvents();
        this.bindMessageEvents();
        this.updateStatus('地图加载完成，准备作答');
        
        // 通知WPF地图已准备就绪
        this.sendMessage('mapReady', {
            status: 'ready',
            timestamp: new Date().toISOString()
        });
    }
    
    /**
     * 初始化百度地图
     */
    initMap() {
        try {
            // 创建百度地图实例
            const mapContainer = document.getElementById('map');
            // 设置地图容器大小
            mapContainer.style.width = '100%';
            mapContainer.style.height = '100%';
            
            // 初始化百度地图
            this.map = new BMap.Map('map');
            
            // 设置中心点和缩放级别
            const point = new BMap.Point(116.404, 39.915); // 北京坐标
            this.map.centerAndZoom(point, 10);
            
            // 添加缩放控件
            this.map.enableScrollWheelZoom(true); // 启用滚轮缩放
            this.map.addControl(new BMap.NavigationControl()); // 添加导航控件
            
            console.log('百度地图初始化成功');
        } catch (error) {
            console.error('百度地图初始化失败:', error);
            this.updateStatus('地图加载失败');
        }
    }
    
    /**
     * 初始化绘制控件和工具
     */
    initControls() {
        try {
            // 初始化绘制管理器
            this.initDrawingManager();
            
            // 初始化自定义工具栏事件
            this.initCustomToolbar();
            
            console.log('绘制控件初始化成功');
        } catch (error) {
            console.error('绘制控件初始化失败:', error);
        }
    }
    
    /**
     * 初始化百度地图绘制管理器
     */
    initDrawingManager() {
        // 百度地图绘制管理器配置
        const styleOptions = {
            strokeColor: '#007bff', // 线颜色
            fillColor: '#007bff',   // 填充颜色
            strokeWeight: 3,        // 线宽
            strokeOpacity: 1,       // 线透明度
            fillOpacity: 0.3        // 填充透明度
        };
        
        // 绘制管理器
        this.drawingManager = new BMapLib.DrawingManager(this.map, {
            isOpen: false,           // 是否开启绘制模式
            enableDrawingTool: false, // 是否显示默认绘制工具栏
            drawingToolOptions: {
                anchor: BMAP_ANCHOR_TOP_RIGHT, // 位置
                offset: new BMap.Size(5, 5),   // 偏移
            },
            polylineOptions: styleOptions,  // 线样式
            polygonOptions: styleOptions,   // 多边形样式
            circleOptions: styleOptions,    // 圆样式
            rectangleOptions: styleOptions, // 矩形样式
            markerOptions: {
                icon: new BMap.Icon('https://api.map.baidu.com/library/DrawingManager/1.4/src/images/marker.png', new BMap.Size(20, 30))
            }
        });
        
        // 监听绘制完成事件
        this.drawingManager.addEventListener('overlaycomplete', this.handleDrawingComplete.bind(this));
    }
    
    /**
     * 初始化自定义工具栏
     */
    initCustomToolbar() {
        // 工具栏按钮事件绑定将在bindEvents方法中处理
    }
    
    /**
     * 绑定事件监听器
     */
    bindEvents() {
        try {
            // 绑定工具栏按钮事件
            if (document.querySelector('#tool-point')) {
                document.querySelector('#tool-point').addEventListener('click', () => this.selectTool('point'));
            }
            if (document.querySelector('#tool-line')) {
                document.querySelector('#tool-line').addEventListener('click', () => this.selectTool('line'));
            }
            if (document.querySelector('#tool-polygon')) {
                document.querySelector('#tool-polygon').addEventListener('click', () => this.selectTool('polygon'));
            }
            if (document.querySelector('#tool-circle')) {
                document.querySelector('#tool-circle').addEventListener('click', () => this.selectTool('circle'));
            }
            if (document.querySelector('#tool-edit')) {
                document.querySelector('#tool-edit').addEventListener('click', () => this.selectTool('edit'));
            }
            if (document.querySelector('#tool-delete')) {
                document.querySelector('#tool-delete').addEventListener('click', () => this.selectTool('delete'));
            }
            if (document.querySelector('#tool-clear')) {
                document.querySelector('#tool-clear').addEventListener('click', () => this.clearAllDrawings());
            }
            
            // 监听百度地图事件
            this.map.addEventListener('zoomend', () => {
                this.updateZoomInfo();
            });
            
            this.map.addEventListener('moveend', () => {
                this.updateCenterInfo();
            });
            
            console.log('事件绑定成功');
        } catch (error) {
            console.error('事件绑定失败:', error);
        }
    },
    
    /**
     * 更新当前缩放级别信息
     */
    updateZoomInfo() {
        const zoom = this.map.getZoom();
        console.log(`当前缩放级别: ${zoom}`);
    },
    
    /**
     * 更新当前中心点信息
     */
    updateCenterInfo() {
        const center = this.map.getCenter();
        console.log(`当前中心点: lng=${center.lng}, lat=${center.lat}`);
    }
    
    /**
     * 绑定工具栏事件
     */
    bindToolbarEvents() {
        // 绘制工具按钮
        document.querySelectorAll('.tool-btn[data-tool]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const tool = e.currentTarget.dataset.tool;
                this.selectTool(tool);
            });
        });
        
        // 清空按钮
        document.getElementById('tool-clear').addEventListener('click', () => {
            this.clearAllDrawings();
        });
    }
    
    /**
     * 绑定绘制事件
     */
    bindDrawEvents() {
        // 绘制完成事件
        this.map.on(L.Draw.Event.CREATED, (e) => {
            const layer = e.layer;
            this.drawnItems.addLayer(layer);
            this.overlayCount++;
            this.updateOverlayCount();
            this.updateStatus(`已绘制 ${this.toolNames[this.currentTool]}`);
            
            // 发送绘制数据到WPF
            this.sendDrawingData();
        });
        
        // 编辑完成事件
        this.map.on(L.Draw.Event.EDITED, (e) => {
            this.updateStatus('图形编辑完成');
            this.sendDrawingData();
        });
        
        // 删除完成事件
        this.map.on(L.Draw.Event.DELETED, (e) => {
            const deletedLayers = e.layers;
            this.overlayCount -= deletedLayers.getLayers().length;
            this.updateOverlayCount();
            this.updateStatus('图形删除完成');
            this.sendDrawingData();
        });
        
        // 绘制开始事件
        this.map.on(L.Draw.Event.DRAWSTART, () => {
            this.isDrawing = true;
            this.updateStatus(`正在绘制${this.toolNames[this.currentTool]}...`);
        });
        
        // 绘制结束事件
        this.map.on(L.Draw.Event.DRAWSTOP, () => {
            this.isDrawing = false;
        });
    }
    
    /**
     * 绑定鼠标事件
     */
    bindMouseEvents() {
        this.map.on('mousemove', (e) => {
            const lat = e.latlng.lat.toFixed(6);
            const lng = e.latlng.lng.toFixed(6);
            document.getElementById('coordinates').textContent = `坐标: ${lat}, ${lng}`;
        });
        
        this.map.on('mouseout', () => {
            document.getElementById('coordinates').textContent = '坐标: --';
        });
    }
    
    /**
     * 绑定消息事件
     */
    bindMessageEvents() {
        try {
            // 从外部（WPF）接收消息
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.addEventListener('message', (event) => {
                    try {
                        const raw = event.data;
                        const message = typeof raw === 'string' ? JSON.parse(raw) : raw;
                        this.handleMessage(message);
                    } catch (error) {
                        console.error('解析消息失败:', error, event.data);
                    }
                });
            } else {
                console.warn('WebView2通信桥接未找到');
            }
        } catch (error) {
            console.error('绑定消息事件失败:', error);
        }
    }
    
    /**
     * 处理接收到的消息
     * @param {Object} message - 消息对象
     */
    handleMessage(message) {
        try {
            // 支持两种协议：
            // 1) 旧版：{ type: 'loadConfig', data: {...} }
            // 2) BridgeMessage：{ messageType: 'LoadQuestion', payload: {...} }
            if (message && typeof message === 'object') {
                if (message.messageType) {
                    switch (message.messageType) {
                        case 'LoadQuestion':
                            this.loadQuestionConfig(message.payload?.config || {});
                            break;
                        case 'ClearAnswer':
                            this.clearAllDrawings();
                            break;
                        case 'RequestSubmit':
                            this.sendDrawingData();
                            break;
                        default:
                            break;
                    }
                } else if (message.type) {
                    switch (message.type) {
                        case 'loadConfig':
                            this.loadQuestionConfig(message.data);
                            break;
                        case 'loadAnswer':
                            this.loadSavedAnswer(message.data);
                            break;
                        case 'selectTool':
                            this.selectTool(message.tool);
                            break;
                        case 'clearAll':
                            this.clearAllDrawings();
                            break;
                        case 'exportData':
                            this.sendDrawingData();
                            break;
                        case 'setZoom':
                            this.setZoom(message.data);
                            break;
                        case 'setCenter':
                            this.setCenter(message.data);
                            break;
                        case 'setBaseLayer':
                            this.setMapType(message.data?.type || 'normal');
                            break;
                        case 'loadQuestion':
                            this.loadQuestion(message.data);
                            break;
                        case 'getDrawings':
                            this.getDrawings();
                            break;
                        case 'requestCurrentData':
                            // 处理自动保存请求，返回当前绘制数据
                            this.sendCurrentData();
                            break;
                    }
                }
            }
        } catch (error) {
            console.error('处理消息失败:', error);
        }
    }
    
    /**
     * 选择绘制工具
     */
    selectTool(tool) {
        try {
            this.currentTool = tool;
            
            // 更新工具栏样式
            document.querySelectorAll('.tool-btn').forEach(btn => {
                btn.classList.remove('active');
            });
            const activeBtn = document.querySelector(`#tool-${tool}`) || document.querySelector(`[data-tool="${tool}"]`);
            if (activeBtn) {
                activeBtn.classList.add('active');
            }
            
            // 关闭所有当前操作
            if (this.drawingManager) {
                this.drawingManager.close();
            }
            
            if (tool === 'edit') {
                // 启用编辑模式
                this.enableEditMode();
                this.updateCurrentTool();
                this.updateStatus('编辑模式已启用');
            } else if (tool === 'delete') {
                // 启用删除模式
                this.enableDeleteMode();
                this.updateCurrentTool();
                this.updateStatus('删除模式已启用');
            } else {
                // 启用绘图模式
                this.isDrawing = true;
                this.updateCurrentTool();
                this.updateStatus(`已选择${this.toolNames[tool]}工具`);
                
                // 设置绘制模式
                let drawingMode = null;
                switch(tool) {
                    case 'point':
                        drawingMode = BMAP_DRAWING_MARKER;
                        break;
                    case 'line':
                        drawingMode = BMAP_DRAWING_POLYLINE;
                        break;
                    case 'polygon':
                        drawingMode = BMAP_DRAWING_POLYGON;
                        break;
                    case 'circle':
                        drawingMode = BMAP_DRAWING_CIRCLE;
                        break;
                }
                
                if (drawingMode && this.drawingManager) {
                    this.drawingManager.setDrawingMode(drawingMode);
                    this.drawingManager.open();
                }
                
                // 通知WPF工具变更
                this.sendMessage('toolChanged', {
                    tool: tool,
                    toolName: this.toolNames[tool]
                });
            }
        } catch (error) {
            console.error('选择工具失败:', error);
            this.updateStatus('工具选择失败');
        }
    },
    
    /**
     * 启用编辑模式
     */
    enableEditMode() {
        // 这里需要实现编辑模式的逻辑
        // 百度地图编辑需要使用覆盖物的enableEditing方法
        if (this.studentOverlays) {
            this.studentOverlays.forEach(overlayInfo => {
                const overlay = overlayInfo.overlay;
                if (overlay && overlay.enableEditing) {
                    overlay.enableEditing();
                }
            });
        }
    },
    
    /**
     * 启用删除模式
     */
    enableDeleteMode() {
        // 这里需要实现删除模式的逻辑
        // 创建点击事件，点击覆盖物时删除
        this.map.addEventListener('click', this.handleDeleteModeClick.bind(this));
    },
    
    /**
     * 处理删除模式下的点击事件
     */
    handleDeleteModeClick(e) {
        // 查找点击位置的覆盖物
        const overlays = this.map.getOverlays();
        for (let i = 0; i < overlays.length; i++) {
            const overlay = overlays[i];
            // 检查是否点击到覆盖物
            if (overlay.getBounds && overlay.getBounds().containsPoint(e.point)) {
                // 删除覆盖物
                this.map.removeOverlay(overlay);
                
                // 更新数据和计数
                if (this.studentOverlays) {
                    this.studentOverlays = this.studentOverlays.filter(o => o.overlay !== overlay);
                }
                this.overlayCount--;
                
                if (document.getElementById('overlay-count')) {
                    document.getElementById('overlay-count').textContent = this.overlayCount;
                }
                
                this.updateStatus('已删除图形');
                break;
            }
        }
    }
    
    /**
     * 停用所有绘制模式
     */
    disableAllDrawModes() {
        Object.values(this.drawControl._toolbars.draw._modes).forEach(mode => {
            if (mode.handler.enabled()) {
                mode.handler.disable();
            }
        });
        
        Object.values(this.drawControl._toolbars.edit._modes).forEach(mode => {
            if (mode.handler.enabled()) {
                mode.handler.disable();
            }
        });
    }
    
    /**
     * 清空所有绘制
     */
    clearAllDrawings() {
        if (this.overlayCount === 0) {
            this.updateStatus('没有可清空的图形');
            return;
        }
        
        if (confirm('确定要清空所有绘制的图形吗？')) {
            try {
                // 清除所有覆盖物
                if (this.studentOverlays) {
                    this.studentOverlays.forEach(overlayInfo => {
                        if (overlayInfo.overlay) {
                            this.map.removeOverlay(overlayInfo.overlay);
                        }
                    });
                    this.studentOverlays = [];
                } else {
                    this.drawnItems.clearLayers();
                }
                
                this.overlayCount = 0;
                this.updateOverlayCount();
                this.updateStatus('已清空所有图形');
                this.sendDrawingData();
            } catch (error) {
                console.error('清空内容失败:', error);
                this.updateStatus('清空失败');
            }
        }
    }
    
    /**
     * 更新覆盖物数量显示
     */
    updateOverlayCount() {
        document.getElementById('overlay-count').textContent = this.overlayCount;
        
        // 通知WPF数量变更
        this.sendMessage('overlayCountChanged', {
            count: this.overlayCount
        });
    }
    
    /**
     * 更新当前工具显示
     */
    updateCurrentTool() {
        document.getElementById('current-tool').textContent = this.toolNames[this.currentTool];
    }
    
    /**
     * 更新状态显示
     * @param {string} status - 状态文本
     */
    updateStatus(status) {
        try {
            const statusEl = document.getElementById('status-text');
            if (statusEl) {
                statusEl.textContent = status;
            }
            console.log(`状态更新: ${status}`);
        } catch (error) {
            console.error('更新状态失败:', error);
        }
    }
    
    /**
     * 发送绘制数据到WPF
     */
    sendDrawingData() {
        const data = this.exportDrawingData();
        this.sendMessage('drawingDataChanged', {
            data: data,
            count: this.overlayCount,
            timestamp: new Date().toISOString()
        });
    }
    
    /**
     * 发送当前绘制数据（用于自动保存）
     */
    sendCurrentData() {
        try {
            const data = this.exportCurrentDrawingData();
            this.sendMessage('currentDataResponse', {
                data: data,
                count: this.overlayCount,
                timestamp: new Date().toISOString()
            });
            console.log('已发送当前绘制数据用于自动保存');
        } catch (error) {
            console.error('发送当前绘制数据失败:', error);
        }
    }
    
    /**
     * 导出当前绘制数据（用于自动保存）
     */
    exportCurrentDrawingData() {
        const features = [];
        
        // 如果使用百度地图覆盖物
        if (this.studentOverlays && this.studentOverlays.length > 0) {
            this.studentOverlays.forEach(overlayInfo => {
                const feature = this.convertOverlayInfoToGeoJSON(overlayInfo);
                if (feature) {
                    features.push(feature);
                }
            });
        } else if (this.drawnItems) {
            // 如果使用Leaflet图层
            this.drawnItems.eachLayer((layer) => {
                let feature = {
                    id: L.Util.stamp(layer),
                    type: this.getLayerType(layer),
                    properties: {
                        style: this.getLayerStyle(layer)
                    }
                };
                
                if (layer instanceof L.Marker) {
                    feature.geometry = {
                        type: 'Point',
                        coordinates: [layer.getLatLng().lng, layer.getLatLng().lat]
                    };
                } else if (layer instanceof L.Polyline && !(layer instanceof L.Polygon)) {
                    feature.geometry = {
                        type: 'LineString',
                        coordinates: layer.getLatLngs().map(latlng => [latlng.lng, latlng.lat])
                    };
                } else if (layer instanceof L.Polygon) {
                    const coords = layer.getLatLngs()[0].map(latlng => [latlng.lng, latlng.lat]);
                    coords.push(coords[0]); // 闭合多边形
                    feature.geometry = {
                        type: 'Polygon',
                        coordinates: [coords]
                    };
                } else if (layer instanceof L.Circle) {
                    feature.geometry = {
                        type: 'Point',
                        coordinates: [layer.getLatLng().lng, layer.getLatLng().lat]
                    };
                    feature.properties.radius = layer.getRadius();
                }
                
                features.push(feature);
            });
        }
        
        return {
            type: 'FeatureCollection',
            features: features
        };
    }
    
    /**
     * 将百度地图覆盖物信息转换为GeoJSON格式
     * @param {Object} overlayInfo - 覆盖物信息
     * @returns {Object} GeoJSON特征对象
     */
    convertOverlayInfoToGeoJSON(overlayInfo) {
        try {
            const feature = {
                id: overlayInfo.id,
                type: 'Feature',
                properties: {
                    style: overlayInfo.style || {},
                    meta: overlayInfo.meta || {}
                }
            };
            
            // 根据覆盖物类型转换几何信息
            switch(overlayInfo.type) {
                case BMAP_DRAWING_MARKER:
                    if (overlayInfo.geometry && overlayInfo.geometry.lng !== undefined && overlayInfo.geometry.lat !== undefined) {
                        feature.geometry = {
                            type: 'Point',
                            coordinates: [overlayInfo.geometry.lng, overlayInfo.geometry.lat]
                        };
                    }
                    break;
                case BMAP_DRAWING_POLYLINE:
                    if (overlayInfo.geometry && overlayInfo.geometry.path && Array.isArray(overlayInfo.geometry.path)) {
                        feature.geometry = {
                            type: 'LineString',
                            coordinates: overlayInfo.geometry.path.map(p => [p.lng, p.lat])
                        };
                    }
                    break;
                case BMAP_DRAWING_POLYGON:
                    if (overlayInfo.geometry && overlayInfo.geometry.path && Array.isArray(overlayInfo.geometry.path)) {
                        const coords = overlayInfo.geometry.path.map(p => [p.lng, p.lat]);
                        coords.push(coords[0]); // 闭合多边形
                        feature.geometry = {
                            type: 'Polygon',
                            coordinates: [coords]
                        };
                    }
                    break;
                case BMAP_DRAWING_CIRCLE:
                    if (overlayInfo.geometry && overlayInfo.geometry.center && overlayInfo.geometry.radius !== undefined) {
                        feature.geometry = {
                            type: 'Point',
                            coordinates: [overlayInfo.geometry.center.lng, overlayInfo.geometry.center.lat]
                        };
                        feature.properties.radius = overlayInfo.geometry.radius;
                    }
                    break;
            }
            
            return feature;
        } catch (error) {
            console.error('转换覆盖物信息为GeoJSON失败:', error);
            return null;
        }
    }
    
    /**
     * 获取覆盖物标签
     * @param {string} type - 覆盖物类型
     * @returns {string} 标签文本
     */
    getOverlayLabel(type) {
        switch(type) {
            case BMAP_DRAWING_MARKER:
                return '点标记';
            case BMAP_DRAWING_POLYLINE:
                return '折线';
            case BMAP_DRAWING_POLYGON:
                return '多边形';
            case BMAP_DRAWING_CIRCLE:
                return '圆形';
            default:
                return '图形';
        }
    }
        const onLoad = () => {
            if (!loaded) {
                loaded = true;
                console.log('[BaseMap] 百度底图加载成功');
                layer.off('tileerror', onError);
                if (this.baseLayer) { try { this.map.removeLayer(this.baseLayer); } catch {} }
                this.baseLayer = layer;
            }
        };
        const onError = () => {
            if (!loaded) {
                layer.off('load', onLoad);
                layer.off('tileerror', onError);
                this.map.removeLayer(layer);
                // 回退到默认提供商列表
                this.initializeBaseMapWithFallback();
            }
        };
        layer.on('load', onLoad);
        layer.on('tileerror', onError);
        layer.addTo(this.map);
    }
    
    /**
     * 发送消息到外部
     * @param {string} type - 消息类型
     * @param {Object} data - 消息数据
     */
    sendMessage(type, data) {
        try {
            if (window.chrome && window.chrome.webview) {
                const message = {
                    type: type,
                    data: data,
                    timestamp: new Date().toISOString()
                };
                window.chrome.webview.postMessage(JSON.stringify(message));
                console.log('已发送消息:', type);
            } else {
                console.warn('WebView2通信桥接未找到，无法发送消息');
            }
        } catch (error) {
            console.error('发送消息失败:', error);
        }
    }
}

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', () => {
    window.mapDrawingAnswering = new MapDrawingAnswering();
});