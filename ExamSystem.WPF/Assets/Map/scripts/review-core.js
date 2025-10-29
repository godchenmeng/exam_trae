/**
 * 地图绘制题教师阅卷页面核心脚本
 * 负责地图初始化、图层管理、对比分析和与WPF应用程序的通信
 */

class MapReviewCore {
    constructor() {
        this.map = null;
        this.referenceOverlays = [];
        this.studentOverlays = [];
        this.guideOverlays = [];
        this.differenceOverlays = [];
        
        this.referenceData = [];
        this.studentData = [];
        this.guideData = [];
        
        this.stats = {
            referenceCount: 0,
            studentCount: 0,
            matchPercentage: 0,
            differenceCount: 0
        };
        
        this.isInitialized = false;
        this.currentMode = 'comparison'; // comparison, reference, student
        
        this.init();
    }
    
    /**
     * 初始化地图和控件
     */
    async init() {
        try {
            this.initializeMap();
            this.initializeLayers();
            this.initializeControls();
            this.bindEvents();
            this.hideLoading();
            this.updateStatus('地图就绪');
            this.sendMessage('mapReady', {});
            this.isInitialized = true;
        } catch (error) {
            console.error('地图初始化失败:', error);
            this.updateStatus('地图加载失败');
        }
    }
    
    /**
     * 初始化百度地图
     */
    initializeMap() {
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
            
            // 添加比例尺
            this.map.addControl(new BMap.ScaleControl({
                anchor: BMAP_ANCHOR_BOTTOM_LEFT
            }));
            
            console.log('百度地图初始化成功');
        } catch (error) {
            console.error('百度地图初始化失败:', error);
            throw error;
        }
    }

    /**
     * 更新当前使用的百度地图底图类型
     * @param {string} type - 底图类型：'normal'（普通）, 'satellite'（卫星）, 'hybrid'（混合）
     */
    setBaseLayerToBaidu(type = 'normal') {
        try {
            this.setMapType(type);
        } catch (error) {
            console.error('设置百度底图失败:', error);
        }
    }
    
    /**
     * 更新当前使用的百度地图底图类型
     * @param {string} type - 底图类型：'normal'（普通）, 'satellite'（卫星）, 'hybrid'（混合）
     */
    setMapType(type = 'normal') {
        try {
            switch(type.toLowerCase()) {
                case 'satellite':
                    this.map.setMapType(BMAP_SATELLITE_MAP);
                    break;
                case 'hybrid':
                    this.map.setMapType(BMAP_HYBRID_MAP);
                    break;
                default:
                    this.map.setMapType(BMAP_NORMAL_MAP);
            }
            console.log(`已切换到${type}底图`);
        } catch (error) {
            console.error('切换底图类型失败:', error);
        }
    }
    
    /**
     * 初始化图层（使用数组存储覆盖物）
     */
    initializeLayers() {
        // 初始化覆盖物数组
        this.referenceOverlays = [];
        this.studentOverlays = [];
        this.guideOverlays = [];
        this.differenceOverlays = [];
        
        console.log('图层初始化完成');
    }
    
    /**
     * 隐藏加载动画
     */
    hideLoading() {
        const loadingEl = document.getElementById('map-loading');
        if (loadingEl) {
            loadingEl.style.display = 'none';
        }
    }
    
    /**
     * 初始化控件
     */
    initializeControls() {
        // 图层控制面板切换
        const layerToggle = document.getElementById('toggleLayerControl');
        const layerPanel = document.querySelector('.layer-control-panel');
        
        layerToggle?.addEventListener('click', () => {
            layerPanel.classList.toggle('panel-collapsed');
            layerToggle.textContent = layerPanel.classList.contains('panel-collapsed') ? '+' : '−';
        });
        
        // 统计面板切换
        const statsToggle = document.getElementById('toggleStatsPanel');
        const statsPanel = document.querySelector('.stats-panel');
        
        statsToggle?.addEventListener('click', () => {
            statsPanel.classList.toggle('panel-collapsed');
            statsToggle.textContent = statsPanel.classList.contains('panel-collapsed') ? '+' : '−';
        });
    }
    
    /**
     * 绑定事件
     */
    bindEvents() {
        // 图层显示/隐藏控制
        document.getElementById('referenceLayer')?.addEventListener('change', (e) => {
            this.toggleOverlayVisibility(this.referenceOverlays, e.target.checked);
        });
        
        document.getElementById('studentLayer')?.addEventListener('change', (e) => {
            this.toggleOverlayVisibility(this.studentOverlays, e.target.checked);
        });
        
        document.getElementById('guideLayer')?.addEventListener('change', (e) => {
            this.toggleOverlayVisibility(this.guideOverlays, e.target.checked);
        });
        
        // 工具按钮事件
        document.getElementById('alignMaps')?.addEventListener('click', () => {
            this.alignMaps();
        });
        
        document.getElementById('calculateMatch')?.addEventListener('click', () => {
            this.calculateMatchPercentage();
        });
        
        document.getElementById('highlightDiff')?.addEventListener('click', () => {
            this.highlightDifferences();
        });
        
        // 地图事件
        this.map.addEventListener('mousemove', (e) => {
            this.updateCoordinates(e.point);
        });
        
        this.map.addEventListener('zoomend', () => {
            this.updateStatus(`缩放级别: ${this.map.getZoom()}`);
        });
        
        this.map.addEventListener('moveend', () => {
            const center = this.map.getCenter();
            this.updateStatus(`中心点: ${center.lng.toFixed(4)}, ${center.lat.toFixed(4)}`);
        });
    }
    
    /**
     * 切换覆盖物可见性
     * @param {Array} overlays - 覆盖物数组
     * @param {boolean} visible - 是否可见
     */
    toggleOverlayVisibility(overlays, visible) {
        overlays.forEach(overlay => {
            if (overlay) {
                if (visible) {
                    this.map.addOverlay(overlay);
                } else {
                    this.map.removeOverlay(overlay);
                }
            }
        });
    }
    
    /**
     * 加载参考答案数据
     */
    loadReferenceData(data) {
        try {
            this.referenceData = Array.isArray(data) ? data : [];
            this.renderReferenceLayer();
            this.updateStats();
            this.sendMessage('referenceDataLoaded', { count: this.referenceData.length });
        } catch (error) {
            console.error('加载参考答案数据失败:', error);
        }
    }
    
    /**
     * 加载学生答案数据
     */
    loadStudentData(data) {
        try {
            this.studentData = Array.isArray(data) ? data : [];
            this.renderStudentLayer();
            this.updateStats();
            this.sendMessage('studentDataLoaded', { count: this.studentData.length });
        } catch (error) {
            console.error('加载学生答案数据失败:', error);
        }
    }
    
    /**
     * 根据数据创建百度地图覆盖物
     * @param {Object} data - 覆盖物数据
     * @param {string} type - 覆盖物类型（'reference', 'student', 'difference'）
     * @returns {Object} 百度地图覆盖物
     */
    createOverlayFromData(data, type = 'reference') {
        try {
            if (!data || !data.geometry) {
                console.warn('无效的覆盖物数据:', data);
                return null;
            }
            
            let overlay = null;
            
            // 设置样式
            let style = {
                strokeColor: '#e74c3c',
                fillColor: '#e74c3c',
                strokeWeight: 3,
                strokeOpacity: 1,
                fillOpacity: 0.2,
                strokeStyle: 'solid'
            };
            
            // 根据类型设置不同样式
            switch(type) {
                case 'student':
                    style.strokeColor = '#3498db';
                    style.fillColor = '#3498db';
                    style.strokeWeight = 2;
                    break;
                case 'difference':
                    style.strokeColor = '#dc3545';
                    style.fillColor = '#dc3545';
                    style.strokeStyle = 'dashed';
                    break;
                default:
                    if (this.currentMode === 'comparison') {
                        style.strokeStyle = 'dashed';
                    }
            }
            
            // 根据几何类型创建覆盖物
            switch(data.geometry.type) {
                case 'Point':
                    const point = new BMap.Point(data.geometry.coordinates[0], data.geometry.coordinates[1]);
                    overlay = new BMap.Marker(point, {
                        icon: this.createCustomMarkerIcon(style.strokeColor)
                    });
                    break;
                case 'LineString':
                    const path = data.geometry.coordinates;
                    if (path && Array.isArray(path)) {
                        const points = path.map(p => new BMap.Point(p[0], p[1]));
                        overlay = new BMap.Polyline(points, {
                            strokeColor: style.strokeColor,
                            strokeWeight: style.strokeWeight,
                            strokeOpacity: style.strokeOpacity,
                            strokeStyle: style.strokeStyle
                        });
                    }
                    break;
                case 'Polygon':
                    const polygonPath = data.geometry.coordinates[0];
                    if (polygonPath && Array.isArray(polygonPath)) {
                        const polygonPoints = polygonPath.map(p => new BMap.Point(p[0], p[1]));
                        overlay = new BMap.Polygon(polygonPoints, {
                            strokeColor: style.strokeColor,
                            strokeWeight: style.strokeWeight,
                            fillColor: style.fillColor,
                            fillOpacity: style.fillOpacity,
                            strokeStyle: style.strokeStyle
                        });
                    }
                    break;
            }
            
            if (overlay) {
                // 保存ID
                overlay._overlayId = data.id || `overlay_${Date.now()}`;
                overlay._overlayType = type;
            }
            
            return overlay;
        } catch (error) {
            console.error('创建覆盖物失败:', error);
            return null;
        }
    }
    
    /**
     * 创建自定义标记图标
     * @param {string} color - 图标颜色
     * @returns {BMap.Icon} 百度地图图标对象
     */
    createCustomMarkerIcon(color) {
        // 创建一个简单的Canvas图标
        const canvas = document.createElement('canvas');
        canvas.width = 16;
        canvas.height = 16;
        const ctx = canvas.getContext('2d');
        
        // 绘制圆形标记
        ctx.fillStyle = color;
        ctx.beginPath();
        ctx.arc(8, 8, 6, 0, Math.PI * 2);
        ctx.fill();
        
        // 绘制白色边框
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 2;
        ctx.stroke();
        
        // 生成数据URL
        const iconUrl = canvas.toDataURL();
        
        return new BMap.Icon(iconUrl, new BMap.Size(16, 16), {
            anchor: new BMap.Size(8, 8)
        });
    }
    
    /**
     * 加载参考线数据
     */
    loadGuideData(data) {
        try {
            this.guideData = Array.isArray(data) ? data : [];
            this.renderGuideLayer();
        } catch (error) {
            console.error('加载参考线数据失败:', error);
        }
    }
    
    /**
     * 渲染参考答案图层
     */
    renderReferenceLayer() {
        // 清除现有参考覆盖物
        this.referenceOverlays.forEach(overlay => {
            if (overlay) {
                this.map.removeOverlay(overlay);
            }
        });
        this.referenceOverlays = [];
        
        // 创建新的覆盖物
        this.referenceData.forEach((item, index) => {
            // 适配不同的数据格式
            const adaptedItem = {
                id: item.id || `ref_${index}`,
                type: item.type || item.geometry?.type || 'point',
                geometry: item.geometry || item,
                properties: item.properties || {}
            };
            
            const overlay = this.createOverlayFromData(adaptedItem, 'reference');
            if (overlay) {
                // 添加信息窗口
                const infoWindow = new BMap.InfoWindow(`参考答案 ${index + 1}<br>类型: ${adaptedItem.type || '未知'}`);
                overlay.addEventListener('click', function() {
                    this.openInfoWindow(infoWindow);
                });
                
                this.referenceOverlays.push(overlay);
                this.map.addOverlay(overlay);
            }
        });
        
        this.stats.referenceCount = this.referenceData.length;
    }
    
    /**
     * 渲染学生答案图层
     */
    renderStudentLayer() {
        // 清除现有学生覆盖物
        this.studentOverlays.forEach(overlay => {
            if (overlay) {
                this.map.removeOverlay(overlay);
            }
        });
        this.studentOverlays = [];
        
        // 创建新的覆盖物
        this.studentData.forEach((item, index) => {
            // 适配不同的数据格式
            const adaptedItem = {
                id: item.id || `stu_${index}`,
                type: item.type || item.geometry?.type || 'point',
                geometry: item.geometry || item,
                properties: item.properties || {}
            };
            
            const overlay = this.createOverlayFromData(adaptedItem, 'student');
            if (overlay) {
                // 添加信息窗口
                const infoWindow = new BMap.InfoWindow(`学生答案 ${index + 1}<br>类型: ${adaptedItem.type || '未知'}`);
                overlay.addEventListener('click', function() {
                    this.openInfoWindow(infoWindow);
                });
                
                this.studentOverlays.push(overlay);
                this.map.addOverlay(overlay);
            }
        });
        
        this.stats.studentCount = this.studentData.length;
    }
    
    /**
     * 渲染参考线图层
     */
    renderGuideLayer() {
        // 清除现有参考线覆盖物
        this.guideOverlays.forEach(overlay => {
            if (overlay) {
                this.map.removeOverlay(overlay);
            }
        });
        this.guideOverlays = [];
        
        // 创建新的覆盖物
        this.guideData.forEach((item, index) => {
            // 适配不同的数据格式
            const adaptedItem = {
                id: item.id || `guide_${index}`,
                type: item.type || item.geometry?.type || 'line',
                geometry: item.geometry || item,
                properties: item.properties || {}
            };
            
            let overlay;
            try {
                const path = adaptedItem.geometry.coordinates || adaptedItem.geometry.path;
                if (path && Array.isArray(path)) {
                    const points = path.map(p => new BMap.Point(p.lng || p[0], p.lat || p[1]));
                    overlay = new BMap.Polyline(points);
                    
                    // 设置参考线样式
                    overlay.setStrokeColor('#f39c12');
                    overlay.setStrokeWeight(1);
                    overlay.setStrokeStyle('dashed');
                    overlay.setFillOpacity(0);
                }
            } catch (error) {
                console.error('创建参考线失败:', error);
            }
            
            if (overlay) {
                // 添加信息窗口
                const infoWindow = new BMap.InfoWindow(`参考线 ${index + 1}`);
                overlay.addEventListener('click', function() {
                    this.openInfoWindow(infoWindow);
                });
                
                this.guideOverlays.push(overlay);
                this.map.addOverlay(overlay);
            }
        });
    }                        radius: 6,
                        ...style
                    });
                }
            });
            
            return layer;
        } catch (error) {
            console.error('创建图层失败:', error);
            return null;
        }
    }
    
    /**
     * 对齐地图视图（适配参考答案的范围）
     */
    alignMaps() {
        try {
            if (this.referenceData.length > 0) {
                // 计算参考答案的边界
                const bounds = this.calculateBounds(this.referenceData);
                
                if (bounds) {
                    // 设置地图视图以显示所有要素
                    this.map.setViewport(bounds, { margins: [50, 50] });
                    this.updateStatus('已对齐到参考答案范围');
                } else {
                    this.updateStatus('无法计算参考答案范围');
                }
            } else if (this.studentData.length > 0) {
                // 如果没有参考答案，尝试对齐到学生答案
                const bounds = this.calculateBounds(this.studentData);
                
                if (bounds) {
                    this.map.setViewport(bounds, { margins: [50, 50] });
                    this.updateStatus('已对齐到学生答案范围');
                } else {
                    this.updateStatus('无法计算学生答案范围');
                }
            } else {
                this.updateStatus('没有可对齐的数据');
            }
        } catch (error) {
            console.error('对齐地图失败:', error);
            this.updateStatus('对齐失败');
        }
    }
    
    /**
     * 计算数据的地理边界
     */
    calculateBounds(data) {
        try {
            const points = [];
            
            data.forEach(item => {
                const geo = item.geometry || item;
                
                if (geo) {
                    switch (item.type || geo.type) {
                        case 'point':
                        case 'Point':
                            points.push(new BMap.Point(geo.lng || geo.coordinates[0], geo.lat || geo.coordinates[1]));
                            break;
                        case 'line':
                        case 'LineString':
                            const path = geo.coordinates || geo.path;
                            if (path && Array.isArray(path)) {
                                path.forEach(p => {
                                    points.push(new BMap.Point(p.lng || p[0], p.lat || p[1]));
                                });
                            }
                            break;
                        case 'polygon':
                        case 'Polygon':
                            const polygonPath = geo.coordinates || geo.path;
                            if (polygonPath && Array.isArray(polygonPath)) {
                                polygonPath.forEach(p => {
                                    points.push(new BMap.Point(p.lng || p[0], p.lat || p[1]));
                                });
                            }
                            break;
                        case 'circle':
                        case 'Circle':
                            if (geo.center) {
                                points.push(new BMap.Point(geo.center.lng, geo.center.lat));
                            }
                            break;
                    }
                }
            });
            
            if (points.length > 0) {
                return points;
            }
        } catch (error) {
            console.error('计算边界失败:', error);
        }
        
        return null;
    }
    
    /**
 * 计算匹配度
 */
    calculateMatchPercentage() {
        try {
            if (this.referenceData.length === 0) {
                this.stats.matchPercentage = 0;
                this.updateStats();
                return;
            }
            
            // 简化的匹配计算：比较数量和几何类型
            let matchCount = 0;
            const total = Math.max(this.referenceData.length, this.studentData.length);
            
            // 计算类型匹配的数量
            const referenceTypes = this.referenceData.map(item => item.type || item.geometry?.type);
            const studentTypes = this.studentData.map(item => item.type || item.geometry?.type);
            
            referenceTypes.forEach(type => {
                const index = studentTypes.indexOf(type);
                if (index !== -1) {
                    matchCount++;
                    studentTypes.splice(index, 1);
                }
            });
            
            this.stats.matchPercentage = Math.round((matchCount / total) * 100);
            this.stats.differenceCount = total - matchCount;
            
            this.updateStats();
            this.updateStatus(`匹配度计算完成: ${this.stats.matchPercentage}%`);
            
            this.sendMessage('matchCalculated', {
                percentage: this.stats.matchPercentage,
                matchCount: matchCount,
                totalCount: this.referenceData.length
            });
        } catch (error) {
            console.error('计算匹配度失败:', error);
        }
    }
    
    /**
     * 比较几何图形
     */
    compareGeometries(geom1, geom2, tolerance = 0.001) {
        if (!geom1 || !geom2) return false;
        
        try {
            // 对于点，检查距离
            if ((geom1.type === 'Point' || geom1.type === 'point') && 
                (geom2.type === 'Point' || geom2.type === 'point')) {
                const point1 = new BMap.Point(geom1.coordinates[0], geom1.coordinates[1]);
                const point2 = new BMap.Point(geom2.coordinates[0], geom2.coordinates[1]);
                return this.getDistance(point1, point2) < 1000; // 1公里以内视为匹配
            }
            
            // 对于线和面，检查中心点距离
            if (['LineString', 'line', 'Polygon', 'polygon', 'MultiPoint', 'multiPoint'].includes(geom1.type) && 
                ['LineString', 'line', 'Polygon', 'polygon', 'MultiPoint', 'multiPoint'].includes(geom2.type)) {
                // 简化处理，实际项目中可能需要更复杂的空间分析
                return true;
            }
        } catch (error) {
            console.error('几何比较失败:', error);
        }
        
        return false;
    }
    
    /**
     * 计算两点之间的距离（米）
     */
    getDistance(point1, point2) {
        return this.map.getDistance(point1, point2);
    }
    
    /**
     * 高亮差异
     */
    highlightDifferences() {
        try {
            // 清除差异覆盖物
            this.clearDifferenceOverlays();
            let differenceCount = 0;
            
            // 找出学生答案中与参考答案不匹配的部分
            this.studentData.forEach((studentItem, index) => {
                const hasMatch = this.referenceData.some(refItem => {
                    return this.compareGeometries(studentItem.geometry, refItem.geometry, 0.001);
                });
                
                if (!hasMatch) {
                    const adaptedItem = {
                        id: studentItem.id || `diff_${index}`,
                        type: studentItem.type || studentItem.geometry?.type || 'point',
                        geometry: studentItem.geometry || studentItem
                    };
                    
                    const overlay = this.createOverlayFromData(adaptedItem, 'difference');
                    if (overlay) {
                        // 添加信息窗口
                        const infoWindow = new BMap.InfoWindow(`差异项 ${differenceCount + 1}<br>学生答案与参考答案不匹配`);
                        overlay.addEventListener('click', function() {
                            this.openInfoWindow(infoWindow);
                        });
                        
                        this.differenceOverlays.push(overlay);
                        this.map.addOverlay(overlay);
                        differenceCount++;
                    }
                }
            });
            
            // 找出参考答案中学生未绘制的部分
            this.referenceData.forEach((refItem, index) => {
                const hasMatch = this.studentData.some(studentItem => {
                    return this.compareGeometries(refItem.geometry, studentItem.geometry, 0.001);
                });
                
                if (!hasMatch) {
                    const adaptedItem = {
                        id: refItem.id || `miss_${index}`,
                        type: refItem.type || refItem.geometry?.type || 'point',
                        geometry: refItem.geometry || refItem
                    };
                    
                    const overlay = this.createOverlayFromData(adaptedItem, 'missing');
                    if (overlay) {
                        // 添加信息窗口
                        const infoWindow = new BMap.InfoWindow(`缺失项 ${differenceCount + 1}<br>学生未绘制此参考答案`);
                        overlay.addEventListener('click', function() {
                            this.openInfoWindow(infoWindow);
                        });
                        
                        this.differenceOverlays.push(overlay);
                        this.map.addOverlay(overlay);
                        differenceCount++;
                    }
                }
            });
            
            this.stats.differenceCount = differenceCount;
            this.updateStats();
            this.updateStatus(`已高亮 ${differenceCount} 处差异`);
        } catch (error) {
            console.error('高亮差异失败:', error);
        }
    }
    
    /**
     * 清除差异覆盖物
     */
    clearDifferenceOverlays() {
        if (!this.differenceOverlays) {
            this.differenceOverlays = [];
            return;
        }
        
        this.differenceOverlays.forEach(overlay => {
            if (overlay) {
                this.map.removeOverlay(overlay);
            }
        });
        this.differenceOverlays = [];
    }
    
    /**
     * 更新网格线
     */
    updateGridLines() {
        if (this.map.hasLayer(this.gridLayer)) {
            this.gridLayer.clearLayers();
            this.createGridLines();
        }
    }
    
    /**
     * 更新统计信息
     */
    updateStats() {
        this.updateStatsDisplay();
        this.sendMessage('statsUpdated', this.stats);
    }
    
    /**
     * 更新统计信息显示
     */
    updateStatsDisplay() {
        const statsElement = document.getElementById('stats-container');
        if (statsElement) {
            statsElement.innerHTML = `
                <div class="stat-item">
                    <span class="stat-label">参考答案数量:</span>
                    <span class="stat-value">${this.stats.referenceCount}</span>
                </div>
                <div class="stat-item">
                    <span class="stat-label">学生答案数量:</span>
                    <span class="stat-value">${this.stats.studentCount}</span>
                </div>
                <div class="stat-item">
                    <span class="stat-label">匹配百分比:</span>
                    <span class="stat-value ${this.stats.matchPercentage >= 80 ? 'high' : this.stats.matchPercentage >= 60 ? 'medium' : 'low'}">
                        ${this.stats.matchPercentage}%
                    </span>
                </div>
                <div class="stat-item">
                    <span class="stat-label">差异数量:</span>
                    <span class="stat-value">${this.stats.differenceCount}</span>
                </div>
            `;
        }
    }
    
    /**
     * 更新坐标显示
     */
    updateCoordinates(point) {
        if (!point) return;
        
        // 获取经纬度坐标（屏幕坐标转换为地理坐标）
        const ll = this.map.pointToPixel(point);
        if (!ll) return;
        
        const coordElement = document.getElementById('coordinates');
        if (coordElement) {
            coordElement.textContent = `坐标: 屏幕坐标(${point.x.toFixed(0)}, ${point.y.toFixed(0)})`;
        }
    }
    
    /**
     * 更新状态消息
     */
    updateStatus(message) {
        const statusElement = document.getElementById('map-status');
        if (statusElement) {
            statusElement.textContent = message;
        }
        console.log(`[MapReview] ${message}`);
    }
    
    /**
     * 隐藏加载遮罩
     */
    hideLoading() {
        const loadingOverlay = document.getElementById('loadingOverlay');
        if (loadingOverlay) {
            loadingOverlay.style.display = 'none';
        }
    }
    
    /**
     * 显示加载遮罩
     */
    showLoading(message = '正在加载...') {
        const loadingOverlay = document.getElementById('loadingOverlay');
        const loadingText = document.querySelector('.loading-text');
        
        if (loadingOverlay) {
            loadingOverlay.style.display = 'flex';
        }
        
        if (loadingText) {
            loadingText.textContent = message;
        }
    }
    
    /**
     * 向WPF应用程序发送消息
     */
    sendMessage(type, data = {}) {
        console.log(`发送消息: ${type}`, data);
        try {
            // 在WebView2环境中，通过window.chrome.webview.postMessage发送消息
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({
                    type: type,
                    data: data,
                    timestamp: new Date().toISOString()
                });
            }
        } catch (error) {
            console.error('发送消息失败:', error);
        }
    }
    
    /**
     * 处理从WPF应用程序接收的消息
     */
    handleMessage(message) {
        console.log('接收到消息:', message);
        try {
            const { type, data } = message;
            
            switch (type) {
                case 'loadReferenceData':
                    this.loadReferenceData(data);
                    break;
                case 'loadStudentData':
                    this.loadStudentData(data);
                    break;
                case 'loadGuideData':
                    this.loadGuideData(data);
                    break;
                case 'switchMode':
                    this.switchMode(data.mode);
                    break;
                case 'setMapType':
                    this.setMapType(data.type);
                    break;
                case 'alignMaps':
                    this.alignMaps();
                    break;
                case 'calculateMatch':
                    this.calculateMatchPercentage();
                    break;
                case 'highlightDiff':
                    this.highlightDifferences();
                    break;
                default:
                    console.warn('未知消息类型:', type);
            }
        } catch (error) {
            console.error('处理消息失败:', error);
        }
    }
    
    /**
     * 处理来自WPF的消息
     */
    handleMessage(message) {
        try {
            const messageObj = typeof message === 'string' ? JSON.parse(message) : message;
            
            switch (messageObj.type) {
                case 'loadReferenceData':
                    this.loadReferenceData(messageObj.data);
                    break;
                    
                case 'loadStudentData':
                    this.loadStudentData(messageObj.data);
                    break;
                    
                case 'loadGuideData':
                    this.loadGuideData(messageObj.data);
                    break;
                    
                case 'setViewMode':
                    this.setViewMode(messageObj.data.mode);
                    break;
                    
                case 'toggleLayer':
                    this.toggleLayer(messageObj.data.layer, messageObj.data.visible);
                    break;
                    
                default:
                    console.warn('未知消息类型:', messageObj.type);
            }
        } catch (error) {
            console.error('处理消息失败:', error);
        }
    }
    
    /**
     * 设置视图模式
     */
    setViewMode(mode) {
        this.currentMode = mode;
        
        switch (mode) {
            case 'reference':
                this.map.removeLayer(this.studentLayer);
                this.map.addLayer(this.referenceLayer);
                break;
                
            case 'student':
                this.map.removeLayer(this.referenceLayer);
                this.map.addLayer(this.studentLayer);
                break;
                
            case 'comparison':
            default:
                this.map.addLayer(this.referenceLayer);
                this.map.addLayer(this.studentLayer);
                break;
        }
    }
    
    /**
     * 切换图层显示
     */
    toggleLayer(layerName, visible) {
        const layerMap = {
            'reference': this.referenceLayer,
            'student': this.studentLayer,
            'guide': this.guideLayer,
            'grid': this.gridLayer,
            'difference': this.differenceLayer
        };
        
        const layer = layerMap[layerName];
        if (layer) {
            if (visible) {
                this.map.addLayer(layer);
            } else {
                this.map.removeLayer(layer);
            }
        }
    }
}

// 全局实例
let mapReviewCore;

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', () => {
    mapReviewCore = new MapReviewCore();
});

// 暴露给WPF的全局函数
window.handleWPFMessage = function(message) {
    if (mapReviewCore) {
        mapReviewCore.handleMessage(message);
    }
};

window.getMapStats = function() {
    return mapReviewCore ? mapReviewCore.stats : null;
};

window.exportMapData = function() {
    if (!mapReviewCore) return null;
    
    return {
        referenceData: mapReviewCore.referenceData,
        studentData: mapReviewCore.studentData,
        stats: mapReviewCore.stats,
        bounds: mapReviewCore.map.getBounds()
    };
};


/**
     * 使用百度地图瓦片作为底图（适用于内网/国内环境）
     */
setBaseLayerToBaidu() {
    // 移除旧底图
    if (this.baseLayer) {
        try { this.map.removeOverlay(this.baseLayer); } catch {}
        this.baseLayer = null;
    }

    // 百度地图默认已加载底图，无需额外添加
    console.log('[BaseMap] 百度底图已初始化');
    this.baseLayer = 'baidu-base';
}

/**
 * 指定 URL 设置底图，如失败则回退到提供商列表
 */
setBaseLayerWithUrl(url) {
    // 默认使用百度地图作为底图，无论传入什么URL
    this.setBaseLayerToBaidu();
    return;
    
    // 以下代码不会执行，保留以备将来需要
    if (!url) return;
    const layer = L.tileLayer(url, { maxZoom: 19 });
    let loaded = false;
    const onLoad = () => {
        if (!loaded) {
            loaded = true;
            layer.off('tileerror', onError);
            if (this.baseLayer) { try { this.map.removeLayer(this.baseLayer); } catch {} }
            this.baseLayer = layer;
            console.log('[BaseMap] 自定义底图加载成功:', url);
        }
    };
    const onError = () => {
        if (!loaded) {
            console.warn('[BaseMap] 自定义底图加载失败，回退到默认底图');
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
    this.baseLayer = layer;
}