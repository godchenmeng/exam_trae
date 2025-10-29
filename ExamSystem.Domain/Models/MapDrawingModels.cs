using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExamSystem.Domain.Models
{
    /// <summary>
    /// 地图绘制题配置模型
    /// </summary>
    public class MapDrawingConfig
    {
        /// <summary>
        /// 城市名称
        /// </summary>
        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// 地图中心坐标 [经度, 纬度]
        /// </summary>
        [JsonPropertyName("center")]
        public double[] Center { get; set; } = new double[2];

        /// <summary>
        /// 地图缩放级别
        /// </summary>
        [JsonPropertyName("zoom")]
        public int Zoom { get; set; } = 10;

        /// <summary>
        /// 地图中心纬度（兼容属性）
        /// </summary>
        [JsonPropertyName("centerLat")]
        public double CenterLat { get; set; } = 39.9042;

        /// <summary>
        /// 地图中心经度（兼容属性）
        /// </summary>
        [JsonPropertyName("centerLng")]
        public double CenterLng { get; set; } = 116.4074;

        /// <summary>
        /// 地图缩放级别（兼容属性）
        /// </summary>
        [JsonPropertyName("zoomLevel")]
        public int ZoomLevel { get; set; } = 10;

        /// <summary>
        /// 地图边界
        /// </summary>
        [JsonPropertyName("bounds")]
        public MapBounds? Bounds { get; set; }

        /// <summary>
        /// 允许的绘图工具列表
        /// </summary>
        [JsonPropertyName("allowedTools")]
        public List<string> AllowedTools { get; set; } = new List<string>();

        /// <summary>
        /// 必需的覆盖物类型
        /// </summary>
        [JsonPropertyName("requiredOverlays")]
        public List<string> RequiredOverlays { get; set; } = new List<string>();

        /// <summary>
        /// 绘制约束条件
        /// </summary>
        [JsonPropertyName("constraints")]
        public MapDrawingConstraints? Constraints { get; set; }

        /// <summary>
        /// 是否使用离线底图（启用本地网格背景）
        /// </summary>
        [JsonPropertyName("useOffline")]
        public bool UseOffline { get; set; } = false;

        /// <summary>
        /// 自定义底图瓦片URL（例如：https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png）
        /// </summary>
        [JsonPropertyName("tileUrl")]
        public string? TileUrl { get; set; } = string.Empty;

        /// <summary>
        /// 是否显示建筑图层
        /// </summary>
        [JsonPropertyName("showBuildingLayers")]
        public bool ShowBuildingLayers { get; set; } = true;

        /// <summary>
        /// 作答时限（秒），0表示不限时
        /// </summary>
        [JsonPropertyName("timeLimitSeconds")]
        public int TimeLimitSeconds { get; set; } = 0;
    }

    /// <summary>
    /// 地图绘制约束条件
    /// </summary>
    public class MapDrawingConstraints
    {
        /// <summary>
        /// 最大覆盖物数量
        /// </summary>
        [JsonPropertyName("maxOverlays")]
        public int MaxOverlays { get; set; } = 10;

        /// <summary>
        /// 最小覆盖物数量
        /// </summary>
        [JsonPropertyName("minOverlays")]
        public int MinOverlays { get; set; } = 1;

        /// <summary>
        /// 是否允许绘制点
        /// </summary>
        [JsonPropertyName("allowPoints")]
        public bool AllowPoints { get; set; } = true;

        /// <summary>
        /// 是否允许绘制线
        /// </summary>
        [JsonPropertyName("allowLines")]
        public bool AllowLines { get; set; } = true;

        /// <summary>
        /// 是否允许绘制多边形
        /// </summary>
        [JsonPropertyName("allowPolygons")]
        public bool AllowPolygons { get; set; } = true;

        /// <summary>
        /// 是否允许绘制圆形
        /// </summary>
        [JsonPropertyName("allowCircles")]
        public bool AllowCircles { get; set; } = false;

        /// <summary>
        /// 允许的绘制区域边界
        /// </summary>
        [JsonPropertyName("allowedBounds")]
        public MapBounds? AllowedBounds { get; set; }
    }

    /// <summary>
    /// 地图边界
    /// </summary>
    public class MapBounds
    {
        /// <summary>
        /// 西南角经度
        /// </summary>
        [JsonPropertyName("swLng")]
        public double SwLng { get; set; }

        /// <summary>
        /// 西南角纬度
        /// </summary>
        [JsonPropertyName("swLat")]
        public double SwLat { get; set; }

        /// <summary>
        /// 东北角经度
        /// </summary>
        [JsonPropertyName("neLng")]
        public double NeLng { get; set; }

        /// <summary>
        /// 东北角纬度
        /// </summary>
        [JsonPropertyName("neLat")]
        public double NeLat { get; set; }

        /// <summary>
        /// 北边界（兼容属性）
        /// </summary>
        [JsonPropertyName("north")]
        public double North { get; set; }

        /// <summary>
        /// 南边界（兼容属性）
        /// </summary>
        [JsonPropertyName("south")]
        public double South { get; set; }

        /// <summary>
        /// 东边界（兼容属性）
        /// </summary>
        [JsonPropertyName("east")]
        public double East { get; set; }

        /// <summary>
        /// 西边界（兼容属性）
        /// </summary>
        [JsonPropertyName("west")]
        public double West { get; set; }
    }

    /// <summary>
    /// 人工评分量表
    /// </summary>
    public class ReviewRubric
    {
        /// <summary>
        /// 评分项列表
        /// </summary>
        [JsonPropertyName("criteria")]
        public List<RubricCriterion> Criteria { get; set; } = new List<RubricCriterion>();

        /// <summary>
        /// 总分
        /// </summary>
        [JsonPropertyName("totalScore")]
        public decimal TotalScore { get; set; } = 100m;
    }

    /// <summary>
    /// 评分项
    /// </summary>
    public class RubricCriterion
    {
        /// <summary>
        /// 评分项ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 评分项名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 评分项描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 权重（0-1）
        /// </summary>
        [JsonPropertyName("weight")]
        public decimal Weight { get; set; } = 0.1m;

        /// <summary>
        /// 最大分值
        /// </summary>
        [JsonPropertyName("maxScore")]
        public decimal MaxScore { get; set; } = 10m;
    }

    /// <summary>
    /// 量表评分结果
    /// </summary>
    public class RubricScores
    {
        /// <summary>
        /// 各评分项得分
        /// </summary>
        [JsonPropertyName("scores")]
        public Dictionary<string, decimal> Scores { get; set; } = new Dictionary<string, decimal>();

        /// <summary>
        /// 总得分
        /// </summary>
        [JsonPropertyName("totalScore")]
        public decimal TotalScore { get; set; } = 0m;

        /// <summary>
        /// 评分备注
        /// </summary>
        [JsonPropertyName("comments")]
        public Dictionary<string, string> Comments { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 建筑图层显示配置
    /// </summary>
    public class BuildingLayersConfig
    {
        /// <summary>
        /// 是否显示建筑轮廓
        /// </summary>
        [JsonPropertyName("showOutlines")]
        public bool ShowOutlines { get; set; } = true;

        /// <summary>
        /// 是否显示建筑标签
        /// </summary>
        [JsonPropertyName("showLabels")]
        public bool ShowLabels { get; set; } = true;

        /// <summary>
        /// 建筑类型过滤器
        /// </summary>
        [JsonPropertyName("typeFilters")]
        public List<string> TypeFilters { get; set; } = new List<string>();

        /// <summary>
        /// 透明度（0-1）
        /// </summary>
        [JsonPropertyName("opacity")]
        public double Opacity { get; set; } = 0.8;
    }

    /// <summary>
    /// 客户端信息
    /// </summary>
    public class ClientInfo
    {
        /// <summary>
        /// 设备类型
        /// </summary>
        [JsonPropertyName("deviceType")]
        public string DeviceType { get; set; } = string.Empty;

        /// <summary>
        /// 操作系统版本
        /// </summary>
        [JsonPropertyName("osVersion")]
        public string OsVersion { get; set; } = string.Empty;

        /// <summary>
        /// 浏览器版本
        /// </summary>
        [JsonPropertyName("browserVersion")]
        public string BrowserVersion { get; set; } = string.Empty;

        /// <summary>
        /// 屏幕分辨率
        /// </summary>
        [JsonPropertyName("screenResolution")]
        public string ScreenResolution { get; set; } = string.Empty;

        /// <summary>
        /// 时区
        /// </summary>
        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; } = string.Empty;
    }
}