/**
 * 地图绘制题出题工具核心JavaScript
 */

class MapAuthoringTool {
    constructor() {
        this.map = null;
        this.baseLayer = null;
        this.currentTool = null;
        this.currentMode = 'guidance'; // 'guidance' | 'reference'
        this.guidanceOverlays = [];
        this.referenceOverlays = [];
        this.drawControl = null;
        this.config = null;
        this.buildingLayers = null;
        
        this.init();
    }

    init() {
        this.initMap();
        this.initControls();
        this.initEventListeners();
        this.bindMessageEvents();
        
        // 通知WPF地图已加载
        this.sendMessage('mapLoaded', {
            timestamp: Date.now()
        });
    }

    initMap() {
        // 初始化地图
        this.map = L.map('map', {
            center: [39.9042, 116.4074],
            zoom: 10,
            zoomControl: true,
            attributionControl: false
        });
        // 默认使用百度地图作为底图
        this.setBaseLayerToBaidu();
        // 更新状态栏底图信息
        this.setBaseLayerLabel('百度地图');

        // 添加坐标显示
        this.map.on('mousemove', (e) => {
            const coords = e.latlng;
            document.getElementById('coordinates').textContent = 
                `坐标: ${coords.lat.toFixed(4)}, ${coords.lng.toFixed(4)}`;
        });

        // 添加缩放级别显示
        this.map.on('zoomend', () => {
            document.getElementById('zoom-level').textContent = 
                `缩放: ${this.map.getZoom()}`;
        });

        // 初始化缩放级别显示
        document.getElementById('zoom-level').textContent = 
            `缩放: ${this.map.getZoom()}`;
    }

    setBaseLayerLabel(name) {
        const el = document.getElementById('base-layer-info');
        if (el) {
            el.textContent = `底图: ${name}`;
        }
    }

    initializeBaseMapWithFallback() {
        // 多个公共底图提供商，按顺序尝试
        const providers = [
            {
                name: 'OSM',
                url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
                options: { maxZoom: 19, attribution: '© OpenStreetMap contributors' }
            },
            {
                name: 'OSM HOT',
                url: 'https://{s}.tile.openstreetmap.fr/hot/{z}/{x}/{y}.png',
                options: { maxZoom: 19, attribution: '© OpenStreetMap contributors, HOT' }
            },
            {
                name: 'Stamen Toner',
                url: 'https://stamen-tiles.a.ssl.fastly.net/toner/{z}/{x}/{y}.png',
                options: { maxZoom: 20, attribution: 'Map tiles by Stamen Design, under CC BY 3.0.' }
            }
        ];

        let providerIndex = 0;
        const tryNextProvider = () => {
            if (providerIndex >= providers.length) {
                console.warn('所有在线底图加载失败，切换到离线网格背景。');
                this.enableOfflineGridBackground();
                this.showHint('网络瓦片加载失败，已切换离线背景。请检查网络或允许 tile.openstreetmap.org 域名访问。');
                return;
            }

            const p = providers[providerIndex++];
            console.log(`[BaseMap] 尝试底图提供商: ${p.name} -> ${p.url}`);
            const layer = L.tileLayer(p.url, p.options);
            let hadLoad = false;
            const onLoad = () => {
                if (!hadLoad) {
                    hadLoad = true;
                    console.log(`[BaseMap] 底图加载成功: ${p.name}`);
                    layer.off('tileerror', onError);
                    this.setBaseLayerLabel(p.name);
                    // 替换当前底图
                    if (this.baseLayer) {
                        try { this.map.removeLayer(this.baseLayer); } catch {}
                    }
                    this.baseLayer = layer;
                }
            };
            const onError = (ev) => {
                console.error(`[BaseMap] 瓦片加载错误: ${p.name}`, ev);
                // 将错误发送给 WPF 端，便于收集日志
                this.sendMessage({
                    type: 'Error',
                    source: 'FallbackTile',
                    message: `回退底图瓦片加载错误: ${p.name}`,
                    detail: { provider: p.name, coords: ev && ev.coords ? ev.coords : undefined }
                });
                // 如果仍未加载成功，尝试下一个提供商
                if (!hadLoad) {
                    layer.off('load', onLoad);
                    layer.off('tileerror', onError);
                    this.map.removeLayer(layer);
                    tryNextProvider();
                }
            };
            layer.on('load', onLoad);
            layer.on('tileerror', onError);
            layer.addTo(this.map);
        };

        tryNextProvider();
    }

    enableOfflineGridBackground() {
        // 使用自定义网格图层作为离线背景，避免空白
        const grid = L.gridLayer({ tileSize: 256 });
        grid.createTile = function(coords) {
            const tile = document.createElement('div');
            tile.style.width = '256px';
            tile.style.height = '256px';
            tile.style.background = 'repeating-linear-gradient(45deg, #fafafa, #fafafa 12px, #f0f0f0 12px, #f0f0f0 24px)';
            tile.style.border = '1px solid #eee';
            return tile;
        };
        grid.addTo(this.map);
        this.baseLayer = grid;
    }

    setBaseLayerWithUrl(url) {
        // 支持百度地图的快捷标识：当传入 'baidu' 时启用百度地图底图
        if (typeof url === 'string' && url.toLowerCase() === 'baidu') {
            this.setBaseLayerToBaidu();
            return;
        }
        // 移除旧底图
        if (this.baseLayer) {
            try { this.map.removeLayer(this.baseLayer); } catch {}
            this.baseLayer = null;
        }
        // 设置新底图（带错误回退）
        const layer = L.tileLayer(url, { maxZoom: 19 });
        let hadLoad = false;
        const onLoad = () => {
            if (!hadLoad) {
                hadLoad = true;
                console.log(`[BaseMap] 自定义底图加载成功: ${url}`);
                layer.off('tileerror', onError);
                this.setBaseLayerLabel(url.toLowerCase() === 'baidu' ? '百度地图' : '自定义瓦片');
            }
        };
        const onError = (ev) => {
            console.error('[BaseMap] 自定义底图瓦片加载错误:', ev);
            this.showHint('自定义底图瓦片加载错误，正在回退…');
            this.sendMessage({
                type: 'Error',
                source: 'CustomTile',
                message: '自定义底图瓦片加载错误',
                detail: {
                    url,
                    coords: ev && ev.coords ? ev.coords : undefined
                }
            });
            if (!hadLoad) {
                layer.off('load', onLoad);
                layer.off('tileerror', onError);
                this.map.removeLayer(layer);
                // 回退到默认多提供商尝试
                this.initializeBaseMapWithFallback();
            }
        };
        layer.on('load', onLoad);
        layer.on('tileerror', onError);
        layer.addTo(this.map);
        this.baseLayer = layer;
    }

    /**
     * 使用百度地图瓦片作为底图（适用于内网/国内环境）。
     * 说明：百度瓦片的 y 坐标与标准 TMS 不同，这里通过覆写 getTileUrl 做坐标转换。
     */
    setBaseLayerToBaidu() {
        // 移除旧底图
        if (this.baseLayer) {
            try { this.map.removeLayer(this.baseLayer); } catch {}
            this.baseLayer = null;
        }

        // 自定义百度瓦片图层类，处理 y 轴反转与子域名
        const BaiduTileLayer = L.TileLayer.extend({
            initialize: function(url, options = {}) {
                options.subdomains = options.subdomains || '0123456789';
                options.maxZoom = options.maxZoom || 19;
                L.TileLayer.prototype.initialize.call(this, url, options);
            },
            getTileUrl: function(coords) {
                // 百度瓦片坐标系：y 需做反转
                const x = coords.x;
                const y = Math.pow(2, coords.z) - 1 - coords.y;
                const data = {
                    s: this._getSubdomain(coords),
                    x: x,
                    y: y,
                    z: coords.z,
                    styles: this.options.styles || 'pl'
                };
                return L.Util.template(this._url, data);
            }
        });

        // 百度矢量底图 URL 模板（可根据需要切换 styles：'pl'=普通，'sl'=卫星标注等）
        const urlTemplate = 'http://online{s}.map.bdimg.com/tile/?qt=tile&x={x}&y={y}&z={z}&styles={styles}';
        const layer = new BaiduTileLayer(urlTemplate, { styles: 'pl' });

        let hadLoad = false;
        const onLoad = () => {
            if (!hadLoad) {
                hadLoad = true;
                console.log('[BaseMap] 百度底图加载成功');
                layer.off('tileerror', onError);
                this.setBaseLayerLabel('百度地图');
            }
        };
        const onError = (ev) => {
            console.error('[BaseMap] 百度底图瓦片加载错误:', ev);
            this.showHint('百度底图瓦片加载错误，正在回退…');
            // 将错误发送给 WPF 端，便于收集日志
            this.sendMessage({
                type: 'Error',
                source: 'BaiduTile',
                message: '百度底图瓦片加载错误',
                detail: {
                    coords: ev && ev.coords ? ev.coords : undefined
                }
            });
            if (!hadLoad) {
                layer.off('load', onLoad);
                layer.off('tileerror', onError);
                this.map.removeLayer(layer);
                // 回退到默认多提供商尝试
                this.initializeBaseMapWithFallback();
            }
        };
        layer.on('load', onLoad);
        layer.on('tileerror', onError);
        layer.addTo(this.map);
        this.baseLayer = layer;
    }

    /**
     * 初始化绘制控件和工具
     */
    initControls() {
        try {
            // 初始化百度地图绘制管理器
            this.initDrawingManager();
            
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
            strokeColor: '#ff7800', // 线颜色
            fillColor: '#ff7800',   // 填充颜色
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
     * 处理绘制完成事件
     * @param {Object} event - 绘制完成事件对象
     */
    handleDrawingComplete(event) {
        const overlay = event.overlay;
        const overlayType = event.drawingMode;
        
        // 停止绘制
        this.drawingManager.close();
        
        // 保存覆盖物信息
        const overlayInfo = this.convertOverlayToData(overlay, overlayType);
        
        // 根据当前模式添加到对应的图层
        if (this.currentMode === 'guidance') {
            this.guidanceOverlays.push(overlayInfo);
        } else {
            this.referenceOverlays.push(overlayInfo);
        }
        
        // 更新计数
        this.overlayCount++;
        const countEl = document.getElementById('overlay-count');
        if (countEl) {
            countEl.textContent = `${this.overlayCount} 个图形`;
        }
        
        this.updateOverlayList();
        this.notifyOverlaysUpdated();
        
        console.log('绘制完成:', overlayInfo);
    }
    
    /**
     * 将百度地图覆盖物转换为数据格式
     * @param {Object} overlay - 百度地图覆盖物
     * @param {string} type - 覆盖物类型
     * @returns {Object} 转换后的数据对象
     */
    convertOverlayToData(overlay, type) {
        const id = 'overlay_' + Date.now();
        let geometry;
        
        // 根据覆盖物类型提取几何信息
        switch(type) {
            case BMAP_DRAWING_MARKER:
                const point = overlay.getPosition();
                geometry = { lng: point.lng, lat: point.lat };
                break;
            case BMAP_DRAWING_POLYLINE:
                const points = overlay.getPath();
                geometry = { 
                    path: points.map(p => [p.lat, p.lng]) 
                };
                break;
            case BMAP_DRAWING_POLYGON:
                const polygonPoints = overlay.getPath();
                geometry = { 
                    path: polygonPoints.map(p => [p.lat, p.lng]) 
                };
                break;
            case BMAP_DRAWING_CIRCLE:
                const center = overlay.getCenter();
                const radius = overlay.getRadius();
                geometry = { 
                    center: [center.lat, center.lng],
                    radius: radius
                };
                break;
        }
        
        return {
            id: id,
            type: this.getLayerTypeFromDrawingMode(type),
            coordinates: geometry,
            style: {
                color: '#ff7800',
                weight: 3,
                opacity: 1,
                fillColor: '#ff7800',
                fillOpacity: 0.3
            },
            timestamp: Date.now()
        };
    }
    
    /**
     * 从百度地图绘制模式转换为标准图层类型
     */
    getLayerTypeFromDrawingMode(mode) {
        switch(mode) {
            case BMAP_DRAWING_MARKER:
                return 'marker';
            case BMAP_DRAWING_POLYLINE:
                return 'polyline';
            case BMAP_DRAWING_POLYGON:
                return 'polygon';
            case BMAP_DRAWING_CIRCLE:
                return 'circle';
            default:
                return 'unknown';
        }
    }

    initEventListeners() {
        try {
            // 工具按钮事件
            const toolButtons = [
                {id: 'point-tool', tool: 'marker'},
                {id: 'line-tool', tool: 'polyline'},
                {id: 'polygon-tool', tool: 'polygon'},
                {id: 'circle-tool', tool: 'circle'},
                {id: 'edit-tool', tool: 'edit'},
                {id: 'delete-tool', tool: 'delete'},
                {id: 'clear-all', action: 'clearAllOverlays'}
            ];

            toolButtons.forEach(item => {
                const button = document.getElementById(item.id);
                if (button) {
                    if (item.action) {
                        button.addEventListener('click', () => this[item.action]());
                    } else {
                        button.addEventListener('click', () => this.setTool(item.tool));
                    }
                }
            });

            // 模式切换事件
            const modeButtons = [
                {id: 'guidance-mode', mode: 'guidance'},
                {id: 'reference-mode', mode: 'reference'}
            ];

            modeButtons.forEach(item => {
                const button = document.getElementById(item.id);
                if (button) {
                    button.addEventListener('click', () => this.setMode(item.mode));
                }
            });

            // 监听百度地图事件
            if (this.map.addEventListener) {
                this.map.addEventListener('zoomend', () => {
                    this.updateZoomInfo();
                });
                
                this.map.addEventListener('moveend', () => {
                    this.updateCenterInfo();
                });
            }
            
            console.log('事件绑定成功');
        } catch (error) {
            console.error('事件绑定失败:', error);
        }
    }
    
    /**
     * 更新当前缩放级别信息
     */
    updateZoomInfo() {
        const zoom = this.map.getZoom ? this.map.getZoom() : '未知';
        console.log(`当前缩放级别: ${zoom}`);
    }
    
    /**
     * 更新当前中心点信息
     */
    updateCenterInfo() {
        if (this.map.getCenter) {
            const center = this.map.getCenter();
            console.log(`当前中心点: lng=${center.lng}, lat=${center.lat}`);
        }
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
                        const message = JSON.parse(event.data);
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

    /**
     * 处理接收到的消息
     * @param {Object} message - 消息对象
     */
    handleMessage(message) {
        try {
            if (!message || !message.type) {
                console.warn('无效的消息格式:', message);
                return;
            }
            
            const { type, data } = message;
            
            switch (type) {
                case 'loadQuestion':
                    this.loadQuestion(data);
                    break;
                case 'setBaseLayer':
                    this.setMapType(data.type || 'normal');
                    break;
                case 'setZoom':
                    this.setZoom(data);
                    break;
                case 'setCenter':
                    this.setCenter(data);
                    break;
                case 'getDrawings':
                    this.getDrawings();
                    break;
                case 'clearAll':
                    this.clearAll();
                    break;
                case 'saveQuestion':
                    this.saveQuestion();
                    break;
                default:
                    console.warn('未知的消息类型:', type);
            }
        } catch (error) {
            console.error('处理消息失败:', error);
        }
    }

    /**
     * 设置地图缩放级别
     * @param {Object} data - 缩放数据
     */
    setZoom(data) {
        try {
            if (data && typeof data.zoom === 'number') {
                this.map.setZoom(data.zoom);
                console.log(`已设置缩放级别: ${data.zoom}`);
            }
        } catch (error) {
            console.error('设置缩放级别失败:', error);
        }
    }
    
    /**
     * 设置地图中心点
     * @param {Object} data - 中心点数据
     */
    setCenter(data) {
        try {
            if (data && typeof data.lng === 'number' && typeof data.lat === 'number') {
                const point = new BMap.Point(data.lng, data.lat);
                this.map.centerAndZoom(point, this.map.getZoom());
                console.log(`已设置中心点: lng=${data.lng}, lat=${data.lat}`);
            }
        } catch (error) {
            console.error('设置中心点失败:', error);
        }
    }
    
    /**
     * 加载题目数据
     * @param {Object} data - 题目数据
     */
    loadQuestion(data) {
        try {
            this.questionConfig = data;
            
            // 加载参考图层
            if (data.referenceLayer) {
                this.loadReferenceLayer(data.referenceLayer);
            }
            
            // 加载已有的绘制内容
            if (data.drawings && Array.isArray(data.drawings)) {
                data.drawings.forEach(drawing => {
                    const overlay = this.createOverlayFromData(drawing);
                    if (overlay) {
                        // 保存覆盖物信息
                        const overlayInfo = this.convertOverlayToData(overlay, drawing.type);
                        this.studentOverlays.push(overlayInfo);
                        this.map.addOverlay(overlay);
                    }
                });
                
                // 更新计数
                this.overlayCount = data.drawings.length;
                const countEl = document.getElementById('overlay-count');
                if (countEl) {
                    countEl.textContent = this.overlayCount;
                }
            }
            
            // 设置初始视图
            if (data.initialView) {
                if (data.initialView.center) {
                    this.setCenter(data.initialView.center);
                }
                if (data.initialView.zoom) {
                    this.setZoom({ zoom: data.initialView.zoom });
                }
            }
            
            // 设置底图类型
            if (data.baseMapType) {
                this.setMapType(data.baseMapType);
            }
            
            this.updateStatus('题目加载完成');
            
            // 通知WPF加载完成
            this.sendMessage('questionLoaded', {
                success: true,
                timestamp: new Date().toISOString()
            });
        } catch (error) {
            console.error('加载题目失败:', error);
            this.updateStatus('题目加载失败');
            
            this.sendMessage('questionLoaded', {
                success: false,
                error: error.message
            });
        }
    }
    
    /**
     * 保存题目数据
     */
    saveQuestion() {
        try {
            const questionData = {
                id: this.questionConfig ? this.questionConfig.id : 'new_' + Date.now(),
                title: this.questionConfig ? this.questionConfig.title : '未命名地图题',
                description: this.questionConfig ? this.questionConfig.description : '',
                baseMapType: this.questionConfig ? this.questionConfig.baseMapType : 'normal',
                initialView: {
                    center: this.map.getCenter(),
                    zoom: this.map.getZoom()
                },
                referenceLayer: this.questionConfig ? this.questionConfig.referenceLayer : [],
                drawings: this.studentOverlays.map(overlayInfo => {
                    const { overlay, ...data } = overlayInfo;
                    return data;
                }),
                settings: this.questionConfig ? this.questionConfig.settings : {},
                createdAt: new Date().toISOString()
            };
            
            // 发送保存消息
            this.sendMessage('questionSaved', {
                success: true,
                data: questionData,
                timestamp: new Date().toISOString()
            });
            
            this.updateStatus('题目保存成功');
        } catch (error) {
            console.error('保存题目失败:', error);
            this.updateStatus('题目保存失败');
            
            this.sendMessage('questionSaved', {
                success: false,
                error: error.message
            });
        }
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

    updateToolAvailability() {
        if (!this.config || !this.config.Constraints) return;

        const constraints = this.config.Constraints;
        
        document.getElementById('point-tool').disabled = !constraints.AllowPoints;
        document.getElementById('line-tool').disabled = !constraints.AllowLines;
        document.getElementById('polygon-tool').disabled = !constraints.AllowPolygons;
        document.getElementById('circle-tool').disabled = !constraints.AllowCircles;

        // 更新按钮样式
        ['point-tool', 'line-tool', 'polygon-tool', 'circle-tool'].forEach(id => {
            const btn = document.getElementById(id);
            if (btn.disabled) {
                btn.style.opacity = '0.5';
                btn.style.cursor = 'not-allowed';
            } else {
                btn.style.opacity = '1';
                btn.style.cursor = 'pointer';
            }
        });
    }

    setTool(tool) {
        try {
            // 清除之前的工具状态
            const toolButtons = document.querySelectorAll('.tool-btn');
            if (toolButtons.length > 0) {
                toolButtons.forEach(btn => {
                    btn.classList.remove('active');
                });
            }

            this.currentTool = tool;
            
            // 更新工具状态显示
            const toolNameEl = document.getElementById('current-tool');
            if (toolNameEl) {
                toolNameEl.textContent = `工具: ${this.getToolName(tool)}`;
            }

            // 激活对应的绘制工具
            const toolButtonMap = {
                'marker': 'point-tool',
                'polyline': 'line-tool',
                'polygon': 'polygon-tool',
                'circle': 'circle-tool',
                'edit': 'edit-tool',
                'delete': 'delete-tool'
            };

            const activeButtonId = toolButtonMap[tool];
            if (activeButtonId) {
                const activeButton = document.getElementById(activeButtonId);
                if (activeButton) {
                    activeButton.classList.add('active');
                }
            }

            // 关闭所有当前操作
            if (this.drawingManager && this.drawingManager.close) {
                this.drawingManager.close();
            }

            if (tool === 'edit') {
                // 启用编辑模式
                this.enableEditMode();
                this.showHint('编辑模式');
            } else if (tool === 'delete') {
                // 启用删除模式
                this.enableDeleteMode();
                this.showHint('删除模式');
            } else {
                // 启用绘图模式
                this.isDrawing = true;
                this.showHint(`准备绘制${this.getToolName(tool)}`);
                
                // 设置百度地图绘制模式
                let drawingMode = null;
                switch(tool) {
                    case 'marker':
                        drawingMode = typeof BMAP_DRAWING_MARKER !== 'undefined' ? BMAP_DRAWING_MARKER : null;
                        break;
                    case 'polyline':
                        drawingMode = typeof BMAP_DRAWING_POLYLINE !== 'undefined' ? BMAP_DRAWING_POLYLINE : null;
                        break;
                    case 'polygon':
                        drawingMode = typeof BMAP_DRAWING_POLYGON !== 'undefined' ? BMAP_DRAWING_POLYGON : null;
                        break;
                    case 'circle':
                        drawingMode = typeof BMAP_DRAWING_CIRCLE !== 'undefined' ? BMAP_DRAWING_CIRCLE : null;
                        break;
                }
                
                if (drawingMode && this.drawingManager && this.drawingManager.setDrawingMode && this.drawingManager.open) {
                    this.drawingManager.setDrawingMode(drawingMode);
                    this.drawingManager.open();
                }
            }
        } catch (error) {
            console.error('工具设置失败:', error);
            this.showHint('工具设置失败');
        }
    }
    
    /**
     * 启用编辑模式
     */
    enableEditMode() {
        try {
            // 启用所有覆盖物的编辑
            const currentOverlays = this.getCurrentOverlays();
            currentOverlays.forEach(overlayInfo => {
                if (overlayInfo.overlay && typeof overlayInfo.overlay.enableEditing === 'function') {
                    overlayInfo.overlay.enableEditing();
                }
            });
        } catch (error) {
            console.error('启用编辑模式失败:', error);
        }
    }
    
    /**
     * 启用删除模式
     */
    enableDeleteMode() {
        try {
            // 创建点击事件，点击覆盖物时删除
            if (this.map.addEventListener) {
                // 先移除之前的事件监听器
                if (this.handleDeleteModeClick) {
                    this.map.removeEventListener('click', this.handleDeleteModeClick);
                }
                // 添加新的事件监听器
                this.handleDeleteModeClick = this.handleDeleteModeClick.bind(this);
                this.map.addEventListener('click', this.handleDeleteModeClick);
            }
        } catch (error) {
            console.error('启用删除模式失败:', error);
        }
    }
    
    /**
     * 处理删除模式下的点击事件
     * @param {Object} e - 点击事件对象
     */
    handleDeleteModeClick(e) {
        try {
            // 查找点击位置的覆盖物
            if (this.map.getOverlays) {
                const overlays = this.map.getOverlays();
                for (let i = 0; i < overlays.length; i++) {
                    const overlay = overlays[i];
                    // 检查是否点击到覆盖物
                    if (overlay.getBounds && typeof overlay.getBounds === 'function' && 
                        overlay.getBounds().containsPoint && 
                        e.point) {
                        if (overlay.getBounds().containsPoint(e.point)) {
                            // 删除覆盖物
                            this.map.removeOverlay(overlay);
                            
                            // 更新数据
                            const currentOverlays = this.getCurrentOverlays();
                            const index = currentOverlays.findIndex(o => o.overlay === overlay);
                            if (index !== -1) {
                                currentOverlays.splice(index, 1);
                                this.updateOverlayList();
                                this.notifyOverlaysUpdated();
                                this.showHint('已删除图形');
                            }
                            break;
                        }
                    }
                }
            }
        } catch (error) {
            console.error('删除覆盖物失败:', error);
        }
    }

    getToolName(tool) {
        const names = {
            'marker': '点',
            'polyline': '线',
            'polygon': '多边形',
            'circle': '圆形',
            'edit': '编辑',
            'delete': '删除'
        };
        return names[tool] || '选择';
    }

    setMode(mode) {
        this.currentMode = mode;
        
        // 更新模式按钮状态
        document.querySelectorAll('.mode-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        
        if (mode === 'guidance') {
            document.getElementById('guidance-mode').classList.add('active');
            document.getElementById('current-mode-title').textContent = '指引图层';
        } else if (mode === 'reference') {
            document.getElementById('reference-mode').classList.add('active');
            document.getElementById('current-mode-title').textContent = '参考答案';
        }

        this.updateOverlayList();
    }

    onOverlayCreated(e) {
        const layer = e.layer;
        const overlayData = this.layerToOverlayData(layer);
        
        // 检查数量限制
        const currentOverlays = this.getCurrentOverlays();
        if (this.config && this.config.Constraints && 
            currentOverlays.length >= this.config.Constraints.MaxOverlays) {
            this.showHint(`最多只能绘制 ${this.config.Constraints.MaxOverlays} 个图形`);
            return;
        }

        // 添加到当前模式的图层集合
        currentOverlays.push(overlayData);
        
        // 添加到地图
        this.map.addLayer(layer);
        
        // 设置图层样式
        this.setLayerStyle(layer, this.currentMode);
        
        // 更新界面
        this.updateOverlayList();
        this.notifyOverlaysUpdated();
    }

    onOverlayEdited(e) {
        // 处理图层编辑
        this.notifyOverlaysUpdated();
    }

    onOverlayDeleted(e) {
        // 处理图层删除
        this.notifyOverlaysUpdated();
    }

    layerToOverlayData(layer) {
        const data = {
            id: L.Util.stamp(layer),
            type: this.getLayerType(layer),
            coordinates: this.getLayerCoordinates(layer),
            style: this.getLayerStyle(layer),
            timestamp: Date.now()
        };
        
        return data;
    }

    getLayerType(layer) {
        if (layer instanceof L.Marker) return 'marker';
        if (layer instanceof L.Polyline && !(layer instanceof L.Polygon)) return 'polyline';
        if (layer instanceof L.Polygon) return 'polygon';
        if (layer instanceof L.Circle) return 'circle';
        return 'unknown';
    }

    getLayerCoordinates(layer) {
        if (layer instanceof L.Marker) {
            return [layer.getLatLng().lat, layer.getLatLng().lng];
        }
        if (layer instanceof L.Circle) {
            const center = layer.getLatLng();
            return {
                center: [center.lat, center.lng],
                radius: layer.getRadius()
            };
        }
        if (layer instanceof L.Polyline) {
            return layer.getLatLngs().map(latlng => [latlng.lat, latlng.lng]);
        }
        return [];
    }

    getLayerStyle(layer) {
        return {
            color: layer.options.color || '#3388ff',
            weight: layer.options.weight || 3,
            opacity: layer.options.opacity || 1,
            fillColor: layer.options.fillColor || '#3388ff',
            fillOpacity: layer.options.fillOpacity || 0.2
        };
    }

    setLayerStyle(layer, mode) {
        const styles = {
            guidance: {
                color: '#ff7800',
                weight: 3,
                opacity: 0.8,
                fillColor: '#ff7800',
                fillOpacity: 0.3
            },
            reference: {
                color: '#00ff00',
                weight: 3,
                opacity: 0.8,
                fillColor: '#00ff00',
                fillOpacity: 0.3
            }
        };

        const style = styles[mode] || styles.guidance;
        layer.setStyle(style);
    }

    getCurrentOverlays() {
        return this.currentMode === 'guidance' ? this.guidanceOverlays : this.referenceOverlays;
    }

    updateOverlayList() {
        const overlays = this.getCurrentOverlays();
        const listElement = document.getElementById('overlay-list');
        const countElement = document.getElementById('overlay-count');
        
        countElement.textContent = `${overlays.length} 个图形`;
        
        listElement.innerHTML = '';
        
        overlays.forEach((overlay, index) => {
            const item = document.createElement('div');
            item.className = 'overlay-item';
            
            item.innerHTML = `
                <span class="overlay-type">${this.getOverlayTypeName(overlay.type)}</span>
                <div class="overlay-actions">
                    <button class="overlay-action" onclick="mapTool.editOverlay(${index})">编辑</button>
                    <button class="overlay-action delete" onclick="mapTool.deleteOverlay(${index})">删除</button>
                </div>
            `;
            
            listElement.appendChild(item);
        });
    }

    getOverlayTypeName(type) {
        const names = {
            'marker': '点',
            'polyline': '线',
            'polygon': '多边形',
            'circle': '圆形'
        };
        return names[type] || type;
    }

    editOverlay(index) {
        // 实现编辑功能
        console.log('Edit overlay:', index);
    }

    deleteOverlay(index) {
        const overlays = this.getCurrentOverlays();
        if (index >= 0 && index < overlays.length) {
            overlays.splice(index, 1);
            this.updateOverlayList();
            this.notifyOverlaysUpdated();
        }
    }

    clearAllOverlays() {
        try {
            if (confirm('确定要清空所有图形吗？')) {
                // 清除所有覆盖物
                this.studentOverlays = this.studentOverlays || [];
                this.studentOverlays.forEach(overlayInfo => {
                    if (overlayInfo.overlay) {
                        this.map.removeOverlay(overlayInfo.overlay);
                    }
                });
                
                // 清空数据
                this.getCurrentOverlays().length = 0;
                this.studentOverlays = [];
                
                // 清除地图上的图层
                this.map.eachLayer((layer) => {
                    if (layer instanceof L.Marker || 
                        layer instanceof L.Polyline || 
                        layer instanceof L.Circle || 
                        layer instanceof BMap.Marker || 
                        layer instanceof BMap.Polyline || 
                        layer instanceof BMap.Polygon || 
                        layer instanceof BMap.Circle) {
                        if (this.map.removeLayer) {
                            this.map.removeLayer(layer);
                        } else if (this.map.removeOverlay) {
                            this.map.removeOverlay(layer);
                        }
                    }
                });
                
                this.updateOverlayList();
                this.notifyOverlaysUpdated();
            }
        } catch (error) {
            console.error('清空内容失败:', error);
            this.showHint('清空失败');
        }
    }
    
    /**
     * 获取所有绘制内容
     */
    getDrawings() {
        try {
            const overlays = this.getCurrentOverlays();
            const drawings = overlays.map(overlayInfo => {
                // 移除overlay引用，只返回数据
                const { overlay, ...data } = overlayInfo;
                return data;
            });
            
            // 发送绘制内容
            this.sendMessage({
                type: 'drawingsData',
                success: true,
                data: drawings,
                timestamp: new Date().toISOString()
            });
        } catch (error) {
            console.error('获取绘制内容失败:', error);
            this.sendMessage({
                type: 'drawingsData',
                success: false,
                error: error.message
            });
        }
    }
    
    /**
     * 根据数据创建覆盖物
     * @param {Object} data - 覆盖物数据
     * @param {boolean} isReference - 是否为参考图层
     * @returns {Object} 地图覆盖物
     */
    createOverlayFromData(data, isReference = false) {
        try {
            if (!data || !data.coordinates || !data.type) {
                console.warn('无效的覆盖物数据:', data);
                return null;
            }
            
            let overlay = null;
            
            // 判断是百度地图还是Leaflet
            const isBaiduMap = window.BMap && this.map instanceof BMap.Map;
            
            if (isBaiduMap) {
                // 百度地图API创建覆盖物
                switch(data.type) {
                    case 'marker':
                        const point = new BMap.Point(data.coordinates[1], data.coordinates[0]);
                        overlay = new BMap.Marker(point);
                        break;
                    case 'polyline':
                        if (Array.isArray(data.coordinates)) {
                            const points = data.coordinates.map(p => new BMap.Point(p[1], p[0]));
                            overlay = new BMap.Polyline(points);
                        }
                        break;
                    case 'polygon':
                        if (Array.isArray(data.coordinates)) {
                            const points = data.coordinates.map(p => new BMap.Point(p[1], p[0]));
                            overlay = new BMap.Polygon(points);
                        }
                        break;
                    case 'circle':
                        if (data.coordinates.center) {
                            const center = new BMap.Point(data.coordinates.center[1], data.coordinates.center[0]);
                            overlay = new BMap.Circle(center, data.coordinates.radius);
                        }
                        break;
                }
                
                // 设置百度地图样式
                if (overlay && data.style) {
                    overlay.setStrokeColor(data.style.color || '#007bff');
                    overlay.setStrokeWeight(data.style.weight || 3);
                    overlay.setFillColor(data.style.fillColor || '#007bff');
                    overlay.setFillOpacity(data.style.fillOpacity || 0.3);
                    
                    // 参考图层使用虚线样式
                    if (isReference) {
                        overlay.setStrokeStyle('dashed');
                        overlay.setStrokeOpacity(0.6);
                        overlay.setFillOpacity(0.2);
                    }
                }
            } else {
                // Leaflet创建覆盖物（原有逻辑）
                // 这里可以保留原有的Leaflet图层创建逻辑
                return null;
            }
            
            return overlay;
        } catch (error) {
            console.error('创建覆盖物失败:', error);
            return null;
        }
    }
    
    /**
     * 加载参考图层
     * @param {Array} referenceData - 参考数据数组
     */
    loadReferenceLayer(referenceData) {
        try {
            // 清除现有参考图层
            this.referenceOverlays = this.referenceOverlays || [];
            this.referenceOverlays.forEach(overlay => {
                if (overlay) {
                    if (this.map.removeLayer) {
                        this.map.removeLayer(overlay);
                    } else if (this.map.removeOverlay) {
                        this.map.removeOverlay(overlay);
                    }
                }
            });
            this.referenceOverlays = [];
            
            if (!referenceData || !Array.isArray(referenceData)) {
                console.log('无参考图层数据');
                return;
            }
            
            // 添加新的参考图层
            referenceData.forEach(item => {
                const overlay = this.createOverlayFromData(item, true);
                if (overlay) {
                    this.referenceOverlays.push(overlay);
                    if (this.map.addLayer) {
                        this.map.addLayer(overlay);
                    } else if (this.map.addOverlay) {
                        this.map.addOverlay(overlay);
                    }
                }
            });
            
            console.log(`已加载${referenceData.length}个参考图层`);
        } catch (error) {
            console.error('加载参考图层失败:', error);
        }
    }

    notifyOverlaysUpdated() {
        // 检查是否为百度地图环境
        const isBaiduMap = window.BMap && this.map instanceof BMap.Map;
        
        // 转换数据格式以兼容百度地图
        const overlays = this.getCurrentOverlays().map(overlay => {
            // 如果是百度地图环境，转换坐标格式
            if (isBaiduMap && overlay.coordinates) {
                // 深拷贝对象，避免修改原始数据
                const converted = JSON.parse(JSON.stringify(overlay));
                
                // 根据类型转换坐标
                if (overlay.type === 'marker') {
                    // 点：[lat, lng] -> {lat, lng}
                    converted.geometry = { 
                        lat: converted.coordinates[0], 
                        lng: converted.coordinates[1] 
                    };
                } else if (overlay.type === 'polyline' || overlay.type === 'polygon') {
                    // 线和面：[[lat, lng], ...] -> {coordinates: [{lat, lng}, ...]}
                    converted.geometry = { 
                        coordinates: converted.coordinates.map(c => ({ 
                            lat: c[0], 
                            lng: c[1] 
                        })) 
                    };
                } else if (overlay.type === 'circle' && overlay.coordinates.center) {
                    // 圆：转换中心点坐标
                    converted.geometry = {
                        center: { 
                            lat: converted.coordinates.center[0], 
                            lng: converted.coordinates.center[1] 
                        },
                        radius: converted.coordinates.radius
                    };
                }
                
                return converted;
            }
            
            return overlay;
        });
        
        this.sendMessage({
            type: 'OverlaysUpdated',
            mode: this.currentMode,
            overlays: overlays
        });
    }

    previewQuestion(question, config, buildingLayers) {
        // 实现题目预览功能
        this.loadConfig(config, buildingLayers);
        
        // 显示题目信息
        this.showHint(`预览: ${question.title} (${question.score}分)`);
    }

    showHint(message) {
        let hint = document.querySelector('.drawing-hint');
        if (!hint) {
            hint = document.createElement('div');
            hint.className = 'drawing-hint';
            document.body.appendChild(hint);
        }
        
        hint.textContent = message;
        hint.classList.add('show');
        
        setTimeout(() => {
            hint.classList.remove('show');
        }, 3000);
    }
}

// 全局实例
let mapTool;

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', () => {
    mapTool = new MapAuthoringTool();
});

// 导出到全局作用域供HTML调用
window.mapTool = mapTool;