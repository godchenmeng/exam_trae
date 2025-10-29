using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExamSystem.Domain.Models
{
    /// <summary>
    /// 覆盖物类型
    /// </summary>
    public enum OverlayType
    {
        Marker,
        Polyline,
        Polygon,
        Rect,
        Circle
    }

    /// <summary>
    /// 经纬度坐标
    /// </summary>
    public class Coordinate
    {
        [JsonPropertyName("lng")] public double Lng { get; set; }
        [JsonPropertyName("lat")] public double Lat { get; set; }
    }

    /// <summary>
    /// 几何对象（根据 type 使用对应字段）
    /// </summary>
    public class OverlayGeometry
    {
        // marker
        [JsonPropertyName("lng")] public double? Lng { get; set; }
        [JsonPropertyName("lat")] public double? Lat { get; set; }

        // polyline / polygon
        [JsonPropertyName("path")] public List<Coordinate>? Path { get; set; }

        // rect
        [JsonPropertyName("sw")] public Coordinate? Sw { get; set; }
        [JsonPropertyName("ne")] public Coordinate? Ne { get; set; }

        // circle
        [JsonPropertyName("center")] public Coordinate? Center { get; set; }
        [JsonPropertyName("radius")] public double? Radius { get; set; }
    }

    /// <summary>
    /// 样式定义
    /// </summary>
    public class OverlayStyle
    {
        [JsonPropertyName("strokeColor")] public string? StrokeColor { get; set; }
        [JsonPropertyName("strokeWeight")] public double? StrokeWeight { get; set; }
        [JsonPropertyName("strokeOpacity")] public double? StrokeOpacity { get; set; }
        [JsonPropertyName("fillColor")] public string? FillColor { get; set; }
        [JsonPropertyName("fillOpacity")] public double? FillOpacity { get; set; }
        [JsonPropertyName("iconKey")] public string? IconKey { get; set; }
        [JsonPropertyName("dashArray")] public string? DashArray { get; set; }
        [JsonPropertyName("lineTexture")] public string? LineTexture { get; set; }
    }

    /// <summary>
    /// 元数据
    /// </summary>
    public class OverlayMeta
    {
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("note")] public string? Note { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
    }

    /// <summary>
    /// 统一覆盖物结构
    /// geometry 字段根据 Type 使用对应属性
    /// </summary>
    public class OverlayDTO
    {
        [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [JsonPropertyName("type")] [JsonConverter(typeof(JsonStringEnumConverter))] public OverlayType Type { get; set; }
        [JsonPropertyName("editable")] public bool Editable { get; set; } = false;
        [JsonPropertyName("visible")] public bool Visible { get; set; } = true;

        [JsonPropertyName("geometry")] public OverlayGeometry Geometry { get; set; } = new OverlayGeometry();
        [JsonPropertyName("style")] public OverlayStyle Style { get; set; } = new OverlayStyle();
        [JsonPropertyName("meta")] public OverlayMeta Meta { get; set; } = new OverlayMeta();
    }
}