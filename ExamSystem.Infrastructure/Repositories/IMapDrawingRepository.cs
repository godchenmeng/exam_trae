using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 地图绘制数据Repository接口
    /// </summary>
    public interface IMapDrawingRepository : IRepository<MapDrawingData>
    {
        /// <summary>
        /// 根据答题记录ID获取地图绘制数据
        /// </summary>
        /// <param name="answerId">答题记录ID</param>
        /// <returns>地图绘制数据列表</returns>
        Task<IEnumerable<MapDrawingData>> GetByAnswerIdAsync(int answerId);

        /// <summary>
        /// 根据考试记录ID和题目ID获取地图绘制数据
        /// </summary>
        /// <param name="examRecordId">考试记录ID</param>
        /// <param name="questionId">题目ID</param>
        /// <returns>地图绘制数据列表</returns>
        Task<IEnumerable<MapDrawingData>> GetByExamRecordAndQuestionAsync(int examRecordId, int questionId);

        /// <summary>
        /// 批量保存地图绘制数据
        /// </summary>
        /// <param name="answerId">答题记录ID</param>
        /// <param name="drawingDataList">绘制数据列表</param>
        /// <returns>保存结果</returns>
        Task<bool> SaveDrawingDataAsync(int answerId, IEnumerable<MapDrawingData> drawingDataList);

        /// <summary>
        /// 清空指定答题记录的地图绘制数据
        /// </summary>
        /// <param name="answerId">答题记录ID</param>
        /// <returns>操作结果</returns>
        Task<bool> ClearDrawingDataAsync(int answerId);

        /// <summary>
        /// 获取地图绘制统计信息
        /// </summary>
        /// <param name="answerId">答题记录ID</param>
        /// <returns>统计信息</returns>
        Task<MapDrawingStatistics> GetDrawingStatisticsAsync(int answerId);

        /// <summary>
        /// 根据图形类型获取绘制数据
        /// </summary>
        /// <param name="answerId">答题记录ID</param>
        /// <param name="shapeType">图形类型</param>
        /// <returns>指定类型的绘制数据</returns>
        Task<IEnumerable<MapDrawingData>> GetByShapeTypeAsync(int answerId, string shapeType);

        /// <summary>
        /// 获取最后更新时间
        /// </summary>
        /// <param name="answerId">答题记录ID</param>
        /// <returns>最后更新时间</returns>
        Task<DateTime?> GetLastUpdateTimeAsync(int answerId);

        /// <summary>
        /// 软删除地图绘制数据
        /// </summary>
        /// <param name="drawingId">绘制数据ID</param>
        /// <returns>操作结果</returns>
        Task<bool> SoftDeleteAsync(int drawingId);

        /// <summary>
        /// 恢复软删除的地图绘制数据
        /// </summary>
        /// <param name="drawingId">绘制数据ID</param>
        /// <returns>操作结果</returns>
        Task<bool> RestoreAsync(int drawingId);
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
        /// 创建时间
        /// </summary>
        public DateTime? FirstCreateTime { get; set; }
    }
}