using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamSystem.Domain.Entities
{
    /// <summary>
    /// 地图绘制数据实体
    /// </summary>
    public class MapDrawingData
    {
        /// <summary>
        /// 绘制数据ID
        /// </summary>
        [Key]
        public int DrawingId { get; set; }

        /// <summary>
        /// 答题记录ID
        /// </summary>
        [Required]
        public int AnswerId { get; set; }

        /// <summary>
        /// 图形类型 (Point, Line, Polygon, Circle, Rectangle, Marker)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ShapeType { get; set; } = string.Empty;

        /// <summary>
        /// 坐标点集合 (JSON格式存储)
        /// </summary>
        [Required]
        [Column(TypeName = "TEXT")]
        public string CoordinatesJson { get; set; } = string.Empty;

        /// <summary>
        /// 图形样式配置 (JSON格式存储，包含颜色、线宽等)
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? StyleJson { get; set; }

        /// <summary>
        /// 图形标签或描述
        /// </summary>
        [MaxLength(500)]
        public string? Label { get; set; }

        /// <summary>
        /// 绘制顺序
        /// </summary>
        public int OrderIndex { get; set; } = 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 是否已删除
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// 导航属性 - 关联的答题记录
        /// </summary>
        [ForeignKey("AnswerId")]
        public virtual AnswerRecord? AnswerRecord { get; set; }
    }

    /// <summary>
    /// 地图绘制坐标点
    /// </summary>
    public class MapCoordinate
    {
        /// <summary>
        /// 经度
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// 纬度
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// 高度 (可选)
        /// </summary>
        public double? Altitude { get; set; }
    }

    /// <summary>
    /// 地图绘制图形样式
    /// </summary>
    public class MapDrawingStyle
    {
        /// <summary>
        /// 填充颜色
        /// </summary>
        public string? FillColor { get; set; }

        /// <summary>
        /// 边框颜色
        /// </summary>
        public string? StrokeColor { get; set; }

        /// <summary>
        /// 线宽
        /// </summary>
        public int StrokeWidth { get; set; } = 2;

        /// <summary>
        /// 透明度 (0-1)
        /// </summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>
        /// 是否填充
        /// </summary>
        public bool IsFilled { get; set; } = false;

        /// <summary>
        /// 线型 (solid, dashed, dotted)
        /// </summary>
        public string LineStyle { get; set; } = "solid";

        /// <summary>
        /// 标记图标URL（仅对Marker类型有效）
        /// </summary>
        public string? IconUrl { get; set; }
    }

    /// <summary>
    /// 地图绘制图形类型枚举
    /// </summary>
    public enum MapShapeType
    {
        /// <summary>
        /// 点
        /// </summary>
        Point,

        /// <summary>
        /// 线
        /// </summary>
        Line,

        /// <summary>
        /// 多边形
        /// </summary>
        Polygon,

        /// <summary>
        /// 圆形
        /// </summary>
        Circle,

        /// <summary>
        /// 矩形
        /// </summary>
        Rectangle,

        /// <summary>
        /// 标记点
        /// </summary>
        Marker
    }
}