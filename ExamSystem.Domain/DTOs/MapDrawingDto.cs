using System;
using System.Collections.Generic;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Domain.DTOs
{
    /// <summary>
    /// 地图绘制数据传输对象
    /// </summary>
    public class MapDrawingDto
    {
        /// <summary>
        /// 绘制数据ID
        /// </summary>
        public int DrawingId { get; set; }

        /// <summary>
        /// 答题记录ID
        /// </summary>
        public int AnswerId { get; set; }

        /// <summary>
        /// 图形类型
        /// </summary>
        public string ShapeType { get; set; } = string.Empty;

        /// <summary>
        /// 坐标点集合
        /// </summary>
        public List<MapCoordinate> Coordinates { get; set; } = new List<MapCoordinate>();

        /// <summary>
        /// 图形样式
        /// </summary>
        public MapDrawingStyle? Style { get; set; }

        /// <summary>
        /// 图形标签
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// 绘制顺序
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// 地图绘制保存请求
    /// </summary>
    public class SaveMapDrawingRequest
    {
        /// <summary>
        /// 答题记录ID
        /// </summary>
        public int AnswerId { get; set; }

        /// <summary>
        /// 考试记录ID
        /// </summary>
        public int ExamRecordId { get; set; }

        /// <summary>
        /// 题目ID
        /// </summary>
        public int QuestionId { get; set; }

        /// <summary>
        /// 绘制数据列表
        /// </summary>
        public List<MapDrawingDto> DrawingData { get; set; } = new List<MapDrawingDto>();

        /// <summary>
        /// 保存时间
        /// </summary>
        public DateTime SaveTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否自动保存
        /// </summary>
        public bool IsAutoSave { get; set; } = false;
    }

    /// <summary>
    /// 地图绘制加载响应
    /// </summary>
    public class LoadMapDrawingResponse
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 绘制数据列表
        /// </summary>
        public List<MapDrawingDto> DrawingData { get; set; } = new List<MapDrawingDto>();

        /// <summary>
        /// 最后保存时间
        /// </summary>
        public DateTime? LastSaveTime { get; set; }

        /// <summary>
        /// 总图形数量
        /// </summary>
        public int TotalShapeCount { get; set; }

        /// <summary>
        /// 统计信息
        /// </summary>
        public MapDrawingStatistics? Statistics { get; set; }
    }

    /// <summary>
    /// 地图绘制保存响应
    /// </summary>
    public class SaveMapDrawingResponse
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 保存的图形数量
        /// </summary>
        public int SavedShapeCount { get; set; }

        /// <summary>
        /// 保存数量
        /// </summary>
        public int SavedCount { get; set; }

        /// <summary>
        /// 保存时间
        /// </summary>
        public DateTime SaveTime { get; set; }

        /// <summary>
        /// 是否为自动保存
        /// </summary>
        public bool IsAutoSave { get; set; }
    }

    /// <summary>
    /// 地图绘制统计信息
    /// </summary>
    public class MapDrawingStatistics
    {
        /// <summary>
        /// 总图形数量
        /// </summary>
        public int TotalShapeCount { get; set; }

        /// <summary>
        /// 各类型图形数量
        /// </summary>
        public Dictionary<string, int> ShapeTypeCount { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 绘制时长 (秒)
        /// </summary>
        public int DrawingDurationSeconds { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime? LastUpdateTime { get; set; }

        /// <summary>
        /// 自动保存次数
        /// </summary>
        public int AutoSaveCount { get; set; }

        /// <summary>
        /// 手动保存次数
        /// </summary>
        public int ManualSaveCount { get; set; }
    }
}