<template>
  <div id="edit-baidu-map">
    <div id="edit-baidu-container"></div>
    <div class="map-controls">
      <el-button
        class="control-btn"
        @click="toggleSatellite"
        :type="isSatellite ? 'primary' : ''">
        {{ isSatellite ? '矢量地图' : '卫星地图' }}
      </el-button>
      <el-button
        class="control-btn"
        v-if="showRoadNetwork"
        @click="toggleRoadNetwork"
        :type="showRoads ? 'primary' : ''">
        路网显示
      </el-button>
    </div>
    <div class="info">
      <div class="input-item">
        <select v-model="selectedCity" @change="handleCityChange">
            <option value="guiyang" selected>贵阳市</option>
            <option value="zunyi">遵义市</option>
            <option value="liupanshui">六盘水市</option>
            <option value="anshun">安顺市</option>
            <option value="bijie">毕节市</option>
            <option value="tongren">铜仁市</option>
            <option value="qiandongnan">黔东南苗族侗族自治州</option>
            <option value="qiannan">黔南布依族苗族自治州</option>
            <option value="qianxinan">黔西南布依族苗族自治州</option>
        </select>
      </div>
      <div class="input-item">
        <div class="input-item-prepend">
          <span class="input-item-text" style="width:6rem;">中心定位：</span>
        </div>
        <input id='edit-baidu-tipinput' type="text">
      </div>
      <div class="input-item">
        <input type="checkbox" name="dz" value="1" @change="handleBuildingCheckboxChange('dz', $event.target.checked)">消防队站(<span>1</span>)&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
        <input type="checkbox" name="zz" value="2" @change="handleBuildingCheckboxChange('zz', $event.target.checked)">专职队(<span>1</span>)&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
        <input type="checkbox" name="zd" value="3" @change="handleBuildingCheckboxChange('zd', $event.target.checked)">重点建筑(<span>1</span>)
      </div>
    </div>
    <div class="input-card" style="width: 22rem;">
      <div class="overlay-list">
        <h4>已绘制图形：</h4>
        <div v-for="(item) in overlays" :key="item.id" class="overlay-item">
          <input
              type="text"
              v-model="item.name"
              @input="updateOverlayName(item.id, $event.target.value)"
              class="overlay-name-input"
          >
          <button @click="removeOverlay(item.id)">删除</button>
        </div>
      </div>
      <div class="input-item" style="display: none;">
          <label>边框色：</label>
          <input type="color" v-model="strokeColor">
          <label style="margin-left:1rem;">填充色：</label>
          <input type="color" v-model="fillColor">
      </div>
      <div class="input-item">
        <input id="clear" type="button" class="btn" value="全部删除" style='margin-left: 1rem;' />
        <input id="close" type="button" class="btn" value="停止绘图" style='margin-left: 2rem;display: none;' />
      </div>
    </div>
    <div class="bottom-toolbar">
      <div>
        <el-button type="primary" :class="{ 'active': currentTool === 'marker' }" @click="handleMarkerRadioChange">画标记</el-button>
        <el-button type="primary" :class="{ 'active': currentTool === 'polyline' }" @click="handlePolylineRadioChange">画水带</el-button>
        <el-button type="primary" :class="{ 'active': currentTool === 'polygon' }" @click="handlePolygonRadioChange">画多边形</el-button>
        <el-button type="primary" :class="{ 'active': currentTool === 'rectangle' }" @click="handleRectangleRadioChange">画矩形</el-button>
        <el-button type="primary" :class="{ 'active': currentTool === 'circle' }" @click="handleCircleRadioChange">画圆</el-button>
      </div>
      <div class="operation-instructions">
        <span>操作说明：圆和矩形通过拖拽来绘制，其他覆盖物通过点击来绘制</span>
      </div>
    </div>

    <!-- 图标选择模态框 -->
    <el-dialog
      title="选择图标"
      :visible.sync="showIconSelector"
      width="60%"
      :close-on-click-modal="false"
    >
      <div class="icon-selector">
        <div class="icon-category">
          <el-select v-model="selectedCategory" placeholder="选择图标分类" @change="handleCategoryChange">
            <el-option v-for="category in iconCategories" :key="category.id" :label="category.name" :value="category.id"></el-option>
          </el-select>
        </div>
        <div class="icon-list">
          <div class="icon-item" v-for="icon in icons" :key="icon.id" @click="selectIcon(icon)">
            <img :src="'data:image/png;base64,' + icon.data" :title="icon.name" class="icon-img">
            <span class="icon-name">{{ icon.name }}</span>
          </div>
        </div>
      </div>
      <div class="pagination-container" style="margin-top: 15px; text-align: right;">
        <el-pagination
          @current-change="handlePageChange"
          :current-page="currentPage"
          :page-size="pageSize"
          :total="total"
          layout="total, prev, pager, next"
        ></el-pagination>
      </div>
    </el-dialog>
   </div>
</template>

<script>
import iconApi from '@/api/icon'
import buildingApi from '@/api/building'

export default {
  name: 'edit-baidu-map-view',
  mounted () {
    this.initBaiduMap()
  },
  unmounted () {
    if (this.map && !this.map.destroyed) {
      this.map.destroy()
    }
  },
  data () {
    return {
      map: null,
      BMapObject: null,
      drawingManager: null,
      overlays: [], // 存储覆盖物对象数组
      showIconSelector: false,
      selectedCategory: '',
      iconCategories: [],
      buildings: [],
      icons: [],
      selectedIcon: null,
      selectedMarkerTool: false,
      currentPage: 1,
      pageSize: 42,
      total: 0,
      currentMarker: null,
      currentTool: 'marker',
      currentId: 0,
      strokeColor: '#ff0000',
      fillColor: '#ff0000',
      isSatellite: false,
      showRoadNetwork: false,
      showRoads: true,
      customOverlays: [],
      textureImg: null,
      selectedCity: 'guiyang',
      cityCoordinates: {
        'guiyang': { lng: 106.717643, lat: 26.587177 },
        'zunyi': { lng: 106.900000, lat: 27.700000 },
        'liupanshui': { lng: 104.800000, lat: 26.580000 },
        'anshun': { lng: 105.920000, lat: 26.250000 },
        'bijie': { lng: 105.290000, lat: 27.310000 },
        'tongren': { lng: 109.190000, lat: 27.720000 },
        'qiandongnan': { lng: 108.080000, lat: 26.580000 },
        'qiannan': { lng: 107.520000, lat: 26.160000 },
        'qianxinan': { lng: 104.820000, lat: 25.000000 }
      },
      buildingMarkers: {}
    }
  },
  watch: {
    strokeColor () {
      this.reactivateTool()
    },
    fillColor () {
      this.reactivateTool()
    }
  },
  props: {
    mapOverlays: {
      type: Object,
      default: function () {
        return {
          center: { lng: 106.717643, lat: 26.587177 },
          overlays: []
        }
      }
    }
  },
  methods: {
    // 处理城市选择变化
    handleCityChange () {
      if (this.selectedCity && this.cityCoordinates[this.selectedCity]) {
        const { lng, lat } = this.cityCoordinates[this.selectedCity]
        this.map.setCenter(new this.BMapObject.Point(lng, lat))
        this.map.setZoom(11)
        // 加载城市建筑
        this.loadCityBuild()
        this.hideBuildingMarkers('dz')
        this.hideBuildingMarkers('zz')
        this.hideBuildingMarkers('zd')
        // 清除所有checkbox选中状态
        document.querySelectorAll('input[name="dz"], input[name="zz"], input[name="zd"]').forEach(checkbox => {
          checkbox.checked = false
        })
      }
    },

    // 清理自定义覆盖物
    clearCustomOverlays (id) {
      const index = this.customOverlays.findIndex(item => item.id === id)
      if (index > -1) {
        this.map.removeOverlay(this.customOverlays[index].textureOverlay)
        this.map.removeOverlay(this.customOverlays[index].headMarker)
        this.customOverlays.splice(index, 1)
      }
    },

    // 百度地图水带纹理覆盖物类
    createTextureOverlay (points, textureImg, id) {
      const _this = this
      
      function TextureOverlay(points, texture, id) {
        this._points = points
        this._texture = texture
        this._id = id
      }

      TextureOverlay.prototype = new this.BMapObject.Overlay()
      TextureOverlay.prototype.initialize = function(map) {
        this._map = map
        const canvas = document.createElement('canvas')
        canvas.style.position = 'absolute'
        canvas.style.zIndex = '10'
        canvas.width = map.getSize().width
        canvas.height = map.getSize().height
        
        map.getPanes().labelPane.appendChild(canvas)
        this._canvas = canvas
        return canvas
      }

      TextureOverlay.prototype.draw = function() {
        const map = this._map
        const canvas = this._canvas
        const ctx = canvas.getContext('2d')
        
        // 清除画布
        ctx.clearRect(0, 0, canvas.width, canvas.height)
        
        if (!this._texture || !this._texture.complete) {
          _this.drawTemporaryHose(ctx, this._points, map)
        } else {
          _this.drawHoseTexture(ctx, this._points, this._texture, map)
        }
      }

      return new TextureOverlay(points, textureImg, id)
    },

    // 绘制水带纹理
    drawHoseTexture (ctx, points, textureImg, map) {
      if (!points || points.length < 2) return

      ctx.save()

      for (let i = 0; i < points.length - 1; i++) {
        const p1 = map.pointToPixel(points[i])
        const p2 = map.pointToPixel(points[i + 1])

        const dx = p2.x - p1.x
        const dy = p2.y - p1.y
        const length = Math.sqrt(dx * dx + dy * dy)
        const angle = Math.atan2(dy, dx)

        ctx.save()
        ctx.translate(p1.x, p1.y)
        ctx.rotate(angle)

        // 创建纹理图案
        const pattern = ctx.createPattern(textureImg, 'repeat')
        ctx.fillStyle = pattern
        ctx.fillRect(0, -5, length, 10)

        ctx.restore()
      }

      ctx.restore()
    },

    // 绘制临时水带（图片加载前）
    drawTemporaryHose (ctx, points, map) {
      if (!points || points.length < 2) return

      ctx.beginPath()
      const firstPixel = map.pointToPixel(points[0])
      ctx.moveTo(firstPixel.x, firstPixel.y)

      for (let i = 1; i < points.length; i++) {
        const pixel = map.pointToPixel(points[i])
        ctx.lineTo(pixel.x, pixel.y)
      }

      ctx.lineWidth = 15
      ctx.strokeStyle = '#000'
      ctx.lineJoin = 'round'
      ctx.lineCap = 'round'
      ctx.stroke()
    },

    // 添加水带纹理
    addHoseTexture (polyline, id) {
      // 确保图片只加载一次
      if (!this.textureImg) {
        this.textureImg = new Image()
        this.textureImg.crossOrigin = 'anonymous'
        this.textureImg.src = require('@/assets/line.png')

        this.textureImg.onload = () => {
          if (this.map) {
            // 触发重绘
            this.customOverlays.forEach(overlay => {
              if (overlay.textureOverlay && overlay.textureOverlay.draw) {
                overlay.textureOverlay.draw()
              }
            })
          }
        }

        this.textureImg.onerror = () => {
          console.error('Failed to load hose texture image')
        }
      }

      const points = polyline.getPath()
      const textureOverlay = this.createTextureOverlay(points, this.textureImg, id)
      this.map.addOverlay(textureOverlay)

      this.addHoseHeadMarker(polyline, textureOverlay, id)
    },

    // 添加水带头标记
    addHoseHeadMarker (polyline, textureOverlay, id) {
      const headImgSrc = require('@/assets/line_top.png')
      const points = polyline.getPath()
      
      let rotation = 0
      if (points && points.length > 1) {
        const startPoint = points[0]
        const nextPoint = points[1]
        const dx = nextPoint.lng - startPoint.lng
        const dy = nextPoint.lat - startPoint.lat
        rotation = Math.atan2(-dy, dx) * 180 / Math.PI + 90
      }

      const icon = new this.BMapObject.Icon(headImgSrc, new this.BMapObject.Size(20, 20))
      const headMarker = new this.BMapObject.Marker(points[0], { icon: icon })
      
      // 百度地图不直接支持角度旋转，需要通过CSS transform实现
      headMarker.addEventListener('load', () => {
        const markerImg = headMarker.getIcon().imageUrl
        if (markerImg) {
          const markerElement = headMarker.getDom()
          if (markerElement) {
            markerElement.style.transform = `rotate(${rotation}deg)`
            markerElement.style.transformOrigin = 'center'
          }
        }
      })

      this.map.addOverlay(headMarker)

      this.customOverlays.push({
        id: id,
        textureOverlay: textureOverlay,
        headMarker: headMarker
      })
    },

    // 加载图标分类
    handleMarkerRadioChange () {
      this.currentTool = 'marker'
      this.showIconSelector = true
      this.selectedMarkerTool = true
    },

    async loadCityBuild () {
      try {
        console.log('开始加载城市建筑数据:', this.selectedCity)
        const resp = await buildingApi.listByCity(this.selectedCity)
        console.log('API响应:', resp)
        
        if (resp.code === 1) {
          this.buildings = resp.response
          console.log('建筑数据已设置:', this.buildings)

          // 更新计数显示
          const dzCount = this.buildings.dzCount || 0
          const zzCount = this.buildings.zzCount || 0
          const zdCount = this.buildings.zdCount || 0

          console.log('建筑计数:', { dzCount, zzCount, zdCount })

          // 找到对应的span标签并更新内容
          const dzSpan = document.querySelector('input[name="dz"] + span')
          const zzSpan = document.querySelector('input[name="zz"] + span')
          const zdSpan = document.querySelector('input[name="zd"] + span')
          
          if (dzSpan) dzSpan.textContent = dzCount
          if (zzSpan) zzSpan.textContent = zzCount
          if (zdSpan) zdSpan.textContent = zdCount
        } else {
          console.error('API返回错误:', resp)
          this.$message.error('加载建筑数据失败')
        }
      } catch (error) {
        console.error('加载建筑失败:', error)
        this.$message.error('加载建筑失败，请重试')
      }
    },

    async loadIconCategories () {
      try {
        const resp = await iconApi.getCategories()
        if (resp.code === 1) {
          this.iconCategories = resp.response

          // 如果有分类，默认选择第一个
          if (this.iconCategories.length > 0) {
            this.selectedCategory = this.iconCategories[0].id
            this.loadIconsByCategory(this.selectedCategory)
          }
        }
      } catch (error) {
        console.error('加载图标分类失败:', error)
        this.$message.error('加载图标分类失败，请重试')
      }
    },

    // 根据分类加载图标
    handleCategoryChange (categoryId) {
      this.currentPage = 1
      this.loadIconsByCategory(categoryId)
    },

    async loadIconsByCategory (categoryId) {
      if (!categoryId) return

      try {
        const resp = await iconApi.pageList({
          categoryId: categoryId,
          pageIndex: this.currentPage,
          pageSize: this.pageSize
        })
        this.icons = resp.response.list
        this.total = resp.response.total
      } catch (error) {
        console.error('加载图标失败:', error)
        this.$message.error('加载图标失败，请重试')
      }
    },

    // 处理建筑类型checkbox变化
    handleBuildingCheckboxChange (type, checked) {
      console.log('复选框状态改变:', type, checked)
      if (checked) {
        this.showBuildingMarkers(type)
      } else {
        this.hideBuildingMarkers(type)
      }
    },

    // 显示指定类型的建筑marker
    showBuildingMarkers (type) {
      console.log('显示建筑标记:', type, this.buildings)
      // 确保地图已初始化
      if (!this.map || !this.BMapObject) {
        console.error('地图尚未初始化，无法添加标记')
        this.$message.error('地图尚未初始化，请稍后再试')
        return
      }
      // 确保buildings数据已加载
      if (!this.buildings || !this.buildings[type + 'Building']) {
        console.log('建筑数据未加载或不存在:', type + 'Building')
        // 尝试重新加载建筑数据
        this.loadCityBuild().then(() => {
          // 重新调用显示方法
          if (this.buildings && this.buildings[type + 'Building']) {
            this.showBuildingMarkers(type)
          }
        })
        return
      }

      const buildingArray = this.buildings[type + 'Building']
      console.log('建筑数组:', buildingArray)
      
      if (!buildingArray || buildingArray.length === 0) {
        console.log('建筑数组为空')
        this.$message.info(`当前城市没有${type === 'dz' ? '消防站' : type === 'zz' ? '专职队' : '重点单位'}数据`)
        return
      }
      
      buildingArray.forEach((building, index) => {
        // 确保有gps坐标
        if (building.gps) {
          // 解析gps字符串为经纬度
          const [lng, lat] = building.gps.split(',').map(coord => parseFloat(coord))
          console.log(`建筑${index} GPS:`, lng, lat)
          
          if (!isNaN(lng) && !isNaN(lat)) {
            // 根据类型选择不同的图标
            let iconPath
            switch (type) {
              case 'dz':
                iconPath = require('@/assets/dz.png')
                break
              case 'zz':
                iconPath = require('@/assets/zz.png')
                break
              case 'zd':
                iconPath = require('@/assets/zd.png')
                break
              default:
                iconPath = require('@/assets/line_top.png')
            }

            console.log('图标路径:', iconPath)

            // 创建百度地图标记
            const point = new this.BMapObject.Point(lng, lat)
            const icon = new this.BMapObject.Icon(iconPath, new this.BMapObject.Size(40, 40), {
              anchor: new this.BMapObject.Size(20, 40),
              imageSize: new this.BMapObject.Size(40, 40)
            })
            const marker = new this.BMapObject.Marker(point, { icon: icon })
            marker.setTop(true)
            
            // 创建信息窗口与文本标签
            const labelText = building.orgName || building.name || type
            const infoWindow = new this.BMapObject.InfoWindow(labelText)
            const label = new this.BMapObject.Label(labelText, {
              position: point,
              offset: new this.BMapObject.Size(-20, -40)
            })
            label.setStyle({
              backgroundColor: 'rgba(255,255,255,0.7)',
              border: '1px solid #666',
              padding: '2px 5px',
              fontSize: '12px',
              borderRadius: '3px'
            })
            marker.setLabel(label)
            
            // 添加事件监听
            marker.addEventListener('click', () => {
              this.map.openInfoWindow(infoWindow, point)
            })
            
            marker.addEventListener('dblclick', () => {
              this.map.setZoom(18)
              this.map.setCenter(point)
            })

            this.map.addOverlay(marker)
            console.log('添加标记到地图:', marker)

            // 存储marker引用以便后续隐藏
            if (!this.buildingMarkers[type]) this.buildingMarkers[type] = []
            this.buildingMarkers[type].push(marker)
          }
        } else {
          console.log(`建筑${index}没有GPS坐标:`, building)
        }
      })
      
      console.log(`${type}类型标记添加完成，共${this.buildingMarkers[type] ? this.buildingMarkers[type].length : 0}个`)
    },

    // 隐藏指定类型的建筑marker
    hideBuildingMarkers (type) {
      if (this.buildingMarkers && this.buildingMarkers[type]) {
        this.buildingMarkers[type].forEach(marker => {
          this.map.removeOverlay(marker)
        })
        this.buildingMarkers[type] = []
      }
    },

    // 选择图标
    handlePageChange (page) {
      this.currentPage = page
      this.loadIconsByCategory(this.selectedCategory)
    },

    selectIcon (icon) {
      this.selectedIcon = icon
      this.showIconSelector = false
      if (this.selectedMarkerTool) {
        this.selectedMarkerTool = false
        this.enableDrawing('marker')
      } else if (this.currentMarker) {
        // 更新标记上的图标
        const iconObj = new this.BMapObject.Icon(`data:image/png;base64,${icon.data}`, new this.BMapObject.Size(50, 50))
        this.currentMarker.setIcon(iconObj)
        
        // 更新overlays数组中的图标信息
        const overlayIndex = this.overlays.findIndex(item => item.overlay === this.currentMarker)
        if (overlayIndex > -1) {
          this.overlays[overlayIndex].iconId = icon.id
          this.overlays[overlayIndex].iconData = icon.data
          this.saveOverlay()
        }
        this.showIconSelector = false
      }
    },

    async initBaiduMap () {
      // 加载图标分类
      await this.loadIconCategories()
      // 加载城市建筑
      await this.loadCityBuild()
      
      // 动态加载百度地图API
      if (!window.BMap) {
        await this.loadBaiduMapScript()
      }
      
      this.BMapObject = window.BMap
      
      // 初始化地图
      this.map = new this.BMapObject.Map('edit-baidu-container')
      const point = new this.BMapObject.Point(this.mapOverlays.center.lng, this.mapOverlays.center.lat)
      this.map.centerAndZoom(point, 11)
      
      // 启用滚轮缩放
      this.map.enableScrollWheelZoom(true)
      
      // 添加地图控件
      this.map.addControl(new this.BMapObject.NavigationControl())
      this.map.addControl(new this.BMapObject.ScaleControl())
      
      // 初始化绘制管理器
      this.initDrawingManager()
      
      // 初始化搜索功能
      this.initSearch()
      
      // 绑定清除和停止按钮事件
      this.bindButtonEvents()
      
      // 如果有预设覆盖物，绘制它们
      if (this.mapOverlays.overlays.length > 0) {
        this.drawOverlay()
      }
    },

    async loadBaiduMapScript () {
      return new Promise((resolve, reject) => {
        if (window.BMap) {
          resolve()
          return
        }

        const script = document.createElement('script')
        script.type = 'text/javascript'
        // 使用免费的百度地图API密钥，实际项目中需要申请自己的AK
        script.src = 'https://api.map.baidu.com/api?v=3.0&ak=E4805d16520de693a3fe707cdc962045&callback=initBaiduMap'
        script.onerror = (error) => {
          console.error('百度地图API加载失败:', error)
          reject(error)
        }
        
        window.initBaiduMap = () => {
          console.log('百度地图API加载成功')
          resolve()
          delete window.initBaiduMap
        }
        
        document.head.appendChild(script)
      })
    },

    initDrawingManager () {
      // 百度地图绘制工具需要额外加载
      if (!window.BMapLib || !window.BMapLib.DrawingManager) {
        this.loadDrawingManagerScript().then(() => {
          this.setupDrawingManager()
        })
      } else {
        this.setupDrawingManager()
      }
    },

    async loadDrawingManagerScript () {
      return new Promise((resolve, reject) => {
        const script = document.createElement('script')
        script.src = 'https://api.map.baidu.com/library/DrawingManager/1.4/src/DrawingManager_min.js'
        script.onload = resolve
        script.onerror = reject
        document.head.appendChild(script)

        // 同时加载CSS
        const link = document.createElement('link')
        link.rel = 'stylesheet'
        link.href = 'https://api.map.baidu.com/library/DrawingManager/1.4/src/DrawingManager_min.css'
        document.head.appendChild(link)
      })
    },

    setupDrawingManager () {
      const styleOptions = {
        strokeColor: this.strokeColor,
        fillColor: this.fillColor,
        strokeWeight: 3,
        strokeOpacity: 0.8,
        fillOpacity: 0.6,
        strokeStyle: 'solid'
      }

      this.drawingManager = new window.BMapLib.DrawingManager(this.map, {
        isOpen: false,
        enableDrawingTool: false,
        drawingToolOptions: {
          anchor: window.BMAP_ANCHOR_TOP_RIGHT,
          offset: new this.BMapObject.Size(5, 5)
        },
        circleOptions: styleOptions,
        polylineOptions: styleOptions,
        polygonOptions: styleOptions,
        rectangleOptions: styleOptions,
        markerOptions: { icon: null }
      })

      // 绑定绘制完成事件
      this.drawingManager.addEventListener('overlaycomplete', (e) => {
        this.handleOverlayComplete(e)
      })
    },

    handleOverlayComplete (e) {
      const overlay = e.overlay
      const id = `overlay-${this.currentId++}`
      
      // 创建标签
      let labelPosition
      if (overlay.getPosition) {
        labelPosition = overlay.getPosition()
      } else if (overlay.getBounds) {
        labelPosition = overlay.getBounds().getCenter()
      } else if (overlay.getCenter) {
        labelPosition = overlay.getCenter()
      } else if (overlay.getPath) {
        const path = overlay.getPath()
        labelPosition = path[0]
      }

      const label = new this.BMapObject.Label(`请输入描述-${this.currentId}`, {
        position: labelPosition,
        offset: new this.BMapObject.Size(-20, -40)
      })
      label.setStyle({
        backgroundColor: 'rgba(255,255,255,0.7)',
        border: '1px solid #666',
        padding: '2px 5px',
        fontSize: '12px',
        borderRadius: '3px'
      })
      this.map.addOverlay(label)

      // 如果是折线，添加水带效果
      if (e.drawingMode === window.BMAP_DRAWING_POLYLINE) {
        overlay.setStrokeColor('#ff0000')
        overlay.setStrokeWeight(10)
        overlay.setStrokeStyle('solid')
        this.addHoseTexture(overlay, id)
      }

      // 保存覆盖物信息
      this.overlays.push({
        id: id,
        name: `请输入描述-${this.currentId}`,
        overlay: overlay,
        label: label,
        type: this.getOverlayType(e.drawingMode),
        strokeColor: this.strokeColor,
        fillColor: this.fillColor,
        iconId: this.selectedIcon ? this.selectedIcon.id : ''
      })

      this.saveOverlay()
      this.drawingManager.close()
    },

    getOverlayType (drawingMode) {
      const typeMap = {
        [window.BMAP_DRAWING_MARKER]: 'marker',
        [window.BMAP_DRAWING_POLYLINE]: 'overlay.polyline',
        [window.BMAP_DRAWING_POLYGON]: 'overlay.polygon',
        [window.BMAP_DRAWING_RECTANGLE]: 'overlay.rectangle',
        [window.BMAP_DRAWING_CIRCLE]: 'overlay.circle'
      }
      return typeMap[drawingMode] || 'unknown'
    },

    initSearch () {
      // 百度地图搜索功能
      const localSearch = new this.BMapObject.LocalSearch(this.map, {
        renderOptions: { map: this.map }
      })

      const input = document.getElementById('edit-baidu-tipinput')
      if (input) {
        input.addEventListener('keypress', (e) => {
          if (e.key === 'Enter') {
            localSearch.search(e.target.value)
          }
        })
      }
    },

    bindButtonEvents () {
      const clearBtn = document.getElementById('clear')
      const closeBtn = document.getElementById('close')

      if (clearBtn) {
        clearBtn.onclick = () => {
          this.overlays.forEach(item => {
            this.map.removeOverlay(item.overlay)
            this.map.removeOverlay(item.label)
          })
          this.overlays = []
          this.customOverlays.forEach(item => {
            this.map.removeOverlay(item.textureOverlay)
            this.map.removeOverlay(item.headMarker)
          })
          this.customOverlays = []
        }
      }

      if (closeBtn) {
        closeBtn.onclick = () => {
          if (this.drawingManager) {
            this.drawingManager.close()
          }
        }
      }
    },

    async drawOverlay () {
      for (const overlay of this.mapOverlays.overlays) {
        const id = `overlay-${this.currentId++}`
        let overlayObj = null
        let label = null

        switch (overlay.type) {
          case 'marker':
            const point = new this.BMapObject.Point(overlay.coordinates[0].longitude, overlay.coordinates[0].latitude)
            const markerContent = await this.getMarkerContent(overlay)
            if (markerContent) {
              const icon = new this.BMapObject.Icon(markerContent, new this.BMapObject.Size(50, 50))
              overlayObj = new this.BMapObject.Marker(point, { icon: icon })
            } else {
              overlayObj = new this.BMapObject.Marker(point)
            }
            break

          case 'overlay.polyline':
            const plPath = overlay.coordinates.map(point => 
              new this.BMapObject.Point(Number(point.longitude || 0), Number(point.latitude || 0))
            )
            overlayObj = new this.BMapObject.Polyline(plPath, {
              strokeColor: '#ff0000',
              strokeWeight: 10,
              strokeStyle: 'solid'
            })
            this.addHoseTextureOld(overlay)
            if (plPath.length > 0) {
              this.addHoseHeadMarkerOld(plPath[0], plPath)
            }
            break

          case 'overlay.polygon':
            const pgPath = overlay.coordinates.map(point => 
              new this.BMapObject.Point(Number(point.longitude || 0), Number(point.latitude || 0))
            )
            overlayObj = new this.BMapObject.Polygon(pgPath, {
              strokeColor: overlay.stroke_color,
              strokeWeight: 4,
              fillColor: overlay.fill_color
            })
            break

          case 'overlay.rectangle':
            const bounds = overlay.coordinates.map(point => 
              new this.BMapObject.Point(Number(point.longitude || 0), Number(point.latitude || 0))
            )
            overlayObj = new this.BMapObject.Polygon([
              bounds[0],
              new this.BMapObject.Point(bounds[1].lng, bounds[0].lat),
              bounds[1],
              new this.BMapObject.Point(bounds[0].lng, bounds[1].lat)
            ], {
              strokeColor: overlay.stroke_color,
              fillColor: overlay.fill_color,
              strokeWeight: 4
            })
            break

          case 'overlay.circle':
            const circleData = overlay.coordinates[0]
            const center = new this.BMapObject.Point(circleData.longitude, circleData.latitude)
            overlayObj = new this.BMapObject.Circle(center, circleData.radius, {
              strokeColor: overlay.stroke_color,
              fillColor: overlay.fill_color,
              strokeWeight: 4
            })
            break
        }

        if (overlayObj) {
          // 创建标签
          let labelPosition
          if (overlayObj.getPosition) {
            labelPosition = overlayObj.getPosition()
          } else if (overlayObj.getBounds) {
            labelPosition = overlayObj.getBounds().getCenter()
          } else if (overlayObj.getCenter) {
            labelPosition = overlayObj.getCenter()
          } else if (overlayObj.getPath) {
            const path = overlayObj.getPath()
            labelPosition = path[0]
          }

          label = new this.BMapObject.Label(overlay.name || '', {
            position: labelPosition,
            offset: new this.BMapObject.Size(-20, -40)
          })
          label.setStyle({
            backgroundColor: 'rgba(255,255,255,0.7)',
            border: '1px solid #666',
            padding: '2px 5px',
            fontSize: '12px',
            borderRadius: '3px'
          })

          this.map.addOverlay(overlayObj)
          this.map.addOverlay(label)

          this.overlays.push({
            id: id,
            name: overlay.name,
            overlay: overlayObj,
            label: label,
            type: overlay.type,
            strokeColor: overlay.stroke_color,
            fillColor: overlay.fill_color,
            iconId: overlay.icon_id
          })
        }
      }
      
      // 自适应视图
      if (this.overlays.length > 0) {
        this.map.setViewport(this.overlays.map(item => {
          if (item.overlay.getPosition) {
            return item.overlay.getPosition()
          } else if (item.overlay.getBounds) {
            return item.overlay.getBounds().getCenter()
          } else if (item.overlay.getCenter) {
            return item.overlay.getCenter()
          }
          return null
        }).filter(point => point !== null))
      }
    },

    async getMarkerContent (overlay) {
      if (overlay.icon_id) {
        try {
          const resp = await iconApi.select(overlay.icon_id)
          const iconData = resp.response
          return `data:image/png;base64,${iconData.data}`
        } catch (error) {
          console.error('Failed to fetch icon:', error)
        }
      }
      return null
    },

    // 添加水带纹理（用于预设数据）
    addHoseTextureOld (overlay) {
      if (!this.textureImg) {
        this.textureImg = new Image()
        this.textureImg.crossOrigin = 'anonymous'
        this.textureImg.src = require('@/assets/line.png')
      }

      const points = overlay.coordinates.map(coord => 
        new this.BMapObject.Point(coord.longitude, coord.latitude)
      )
      const textureOverlay = this.createTextureOverlay(points, this.textureImg, `old-${this.currentId}`)
      this.map.addOverlay(textureOverlay)
    },

    // 添加水带头标记（用于预设数据）
    addHoseHeadMarkerOld (position, path) {
      const headImgSrc = require('@/assets/line_top.png')
      
      let rotation = 0
      if (path && path.length > 1) {
        const startPoint = path[0]
        const nextPoint = path[1]
        const dx = nextPoint.lng - startPoint.lng
        const dy = nextPoint.lat - startPoint.lat
        rotation = Math.atan2(-dy, dx) * 180 / Math.PI + 90
      }

      const icon = new this.BMapObject.Icon(headImgSrc, new this.BMapObject.Size(20, 20))
      const headMarker = new this.BMapObject.Marker(position, { icon: icon })
      
      this.map.addOverlay(headMarker)
      this.customOverlays.push({
        id: `old-${this.currentId}`,
        textureOverlay: null,
        headMarker: headMarker
      })
    },

    handlePolylineRadioChange () {
      this.currentTool = 'polyline'
      this.enableDrawing('polyline')
    },

    handlePolygonRadioChange () {
      this.currentTool = 'polygon'
      this.enableDrawing('polygon')
    },

    handleRectangleRadioChange () {
      this.currentTool = 'rectangle'
      this.enableDrawing('rectangle')
    },

    handleCircleRadioChange () {
      this.currentTool = 'circle'
      this.enableDrawing('circle')
    },

    enableDrawing (type) {
      if (!this.drawingManager) return

      const drawingModeMap = {
        'marker': window.BMAP_DRAWING_MARKER,
        'polyline': window.BMAP_DRAWING_POLYLINE,
        'polygon': window.BMAP_DRAWING_POLYGON,
        'rectangle': window.BMAP_DRAWING_RECTANGLE,
        'circle': window.BMAP_DRAWING_CIRCLE
      }

      if (type === 'marker' && !this.selectedIcon) {
        this.$message.warning('请先选择图标')
        return
      }

      if (type === 'marker' && this.selectedIcon) {
        const icon = new this.BMapObject.Icon(`data:image/png;base64,${this.selectedIcon.data}`, new this.BMapObject.Size(50, 50))
        this.drawingManager.setDrawingMode(drawingModeMap[type])
        this.drawingManager._opts.markerOptions.icon = icon
      } else {
        this.drawingManager.setDrawingMode(drawingModeMap[type])
      }

      this.drawingManager.open()
    },

    removeOverlay (id) {
      const index = this.overlays.findIndex(item => item.id === id)
      if (index > -1) {
        if (this.overlays[index].type === 'overlay.polyline') {
          this.clearCustomOverlays(id)
        }
        this.map.removeOverlay(this.overlays[index].overlay)
        this.map.removeOverlay(this.overlays[index].label)
        this.overlays.splice(index, 1)
        this.saveOverlay()
      }
    },

    updateOverlayName (id, newName) {
      const index = this.overlays.findIndex(item => item.id === id)
      if (index > -1) {
        this.overlays[index].name = newName
        // 更新地图上的文本标签
        if (this.overlays[index].label) {
          this.overlays[index].label.setContent(newName.split(' (')[0])
        }
        this.saveOverlay()
      }
    },

    reactivateTool () {
      if (this.currentTool && this.drawingManager) {
        this.enableDrawing(this.currentTool)
      }
    },

    getOverlayCoordinates (overlay, type) {
      if (type === 'marker') {
        const position = overlay.getPosition()
        return [{
          longitude: position.lng,
          latitude: position.lat
        }]
      }
      
      if (type === 'overlay.polyline' || type === 'overlay.polygon') {
        return overlay.getPath().map((point, idx) => ({
          sequence: idx,
          longitude: point.lng,
          latitude: point.lat
        }))
      }
      
      if (type === 'overlay.circle') {
        const center = overlay.getCenter()
        return [{
          longitude: center.lng,
          latitude: center.lat,
          radius: overlay.getRadius()
        }]
      }
      
      if (type === 'overlay.rectangle') {
        const bounds = overlay.getBounds()
        return [
          { longitude: bounds.getSouthWest().lng, latitude: bounds.getSouthWest().lat },
          { longitude: bounds.getNorthEast().lng, latitude: bounds.getNorthEast().lat }
        ]
      }
      
      return []
    },

    saveOverlay () {
      const result = []
      this.overlays.forEach(item => {
        const coordinates = this.getOverlayCoordinates(item.overlay, item.type)
        const payload = {
          name: item.name,
          type: item.type,
          stroke_color: item.strokeColor,
          fill_color: item.fillColor,
          coordinates: coordinates,
          icon_id: item.iconId
        }
        result.push(payload)
      })
      
      const centerPoint = this.map.getCenter()
      this.$emit('input', { 
        center: { lng: centerPoint.lng, lat: centerPoint.lat }, 
        overlays: result 
      })
      
      if (this.drawingManager) {
        this.drawingManager.close()
      }
    },

    toggleSatellite () {
      this.isSatellite = !this.isSatellite
      this.showRoadNetwork = this.isSatellite

      // 百度地图图层切换
      if (this.isSatellite) {
        this.map.setMapType(window.BMAP_SATELLITE_MAP)
      } else {
        this.map.setMapType(window.BMAP_NORMAL_MAP)
      }
    },

    toggleRoadNetwork () {
      this.showRoads = !this.showRoads
      // 百度地图路网显示切换
      if (this.showRoads) {
        this.map.setMapType(window.BMAP_HYBRID_MAP)
      } else {
        this.map.setMapType(window.BMAP_SATELLITE_MAP)
      }
    }
  }
}
</script>

<style lang="scss" scoped>
@use '~@/styles/amap.scss';

#edit-baidu-container {
  width: 100%;
  height: 900px;
}

.bottom-toolbar {
  position: absolute;
  bottom: 1rem;
  left: 50%;
  transform: translateX(-50%);
  z-index: 100;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  padding: 10px;
  background: white;
  border-radius: 4px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  min-width: 500px;
}

.bottom-toolbar .el-button.active {
  background-color: #1890ff;
  color: white;
}

.icon-selector {
  padding: 10px;
}

.icon-category {
  margin-bottom: 20px;
}

.icon-list {
  display: flex;
  flex-wrap: wrap;
  gap: 15px;
  max-height: 400px;
  overflow-y: auto;
}

.icon-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  cursor: pointer;
  padding: 10px;
  border-radius: 4px;
  transition: background-color 0.2s;
  width: 80px;
}

.icon-item:hover {
  background-color: #f5f5f5;
}

.icon-img {
  width: 40px;
  height: 40px;
  margin-bottom: 5px;
}

.icon-name {
  font-size: 12px;
  text-align: center;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  width: 100%;
}
</style>