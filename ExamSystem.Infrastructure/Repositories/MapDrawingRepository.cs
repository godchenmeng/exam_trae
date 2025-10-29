using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 地图绘制数据Repository实现类
    /// </summary>
    public class MapDrawingRepository : BaseRepository<MapDrawingData>, IMapDrawingRepository
    {
        public MapDrawingRepository(ExamDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据答题记录ID获取地图绘制数据
        /// </summary>
        public async Task<IEnumerable<MapDrawingData>> GetByAnswerIdAsync(int answerId)
        {
            return await _dbSet
                .Where(md => md.AnswerId == answerId && !md.IsDeleted)
                .OrderBy(md => md.OrderIndex)
                .ThenBy(md => md.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据考试记录ID和题目ID获取地图绘制数据
        /// </summary>
        public async Task<IEnumerable<MapDrawingData>> GetByExamRecordAndQuestionAsync(int examRecordId, int questionId)
        {
            return await _dbSet
                .Include(md => md.AnswerRecord)
                .Where(md => md.AnswerRecord != null && 
                           md.AnswerRecord.RecordId == examRecordId && 
                           md.AnswerRecord.QuestionId == questionId && 
                           !md.IsDeleted)
                .OrderBy(md => md.OrderIndex)
                .ThenBy(md => md.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 批量保存地图绘制数据
        /// </summary>
        public async Task<bool> SaveDrawingDataAsync(int answerId, IEnumerable<MapDrawingData> drawingDataList)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 先清空现有数据（软删除）
                var existingData = await _dbSet
                    .Where(md => md.AnswerId == answerId && !md.IsDeleted)
                    .ToListAsync();

                foreach (var item in existingData)
                {
                    item.IsDeleted = true;
                    item.UpdatedAt = DateTime.Now;
                }

                // 添加新数据
                var newDataList = drawingDataList.Select((data, index) => new MapDrawingData
                {
                    AnswerId = answerId,
                    ShapeType = data.ShapeType,
                    CoordinatesJson = data.CoordinatesJson,
                    StyleJson = data.StyleJson,
                    Label = data.Label,
                    OrderIndex = index,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                }).ToList();

                await _dbSet.AddRangeAsync(newDataList);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        /// <summary>
        /// 清空指定答题记录的地图绘制数据
        /// </summary>
        public async Task<bool> ClearDrawingDataAsync(int answerId)
        {
            try
            {
                var existingData = await _dbSet
                    .Where(md => md.AnswerId == answerId && !md.IsDeleted)
                    .ToListAsync();

                foreach (var item in existingData)
                {
                    item.IsDeleted = true;
                    item.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取地图绘制统计信息
        /// </summary>
        public async Task<MapDrawingStatistics> GetDrawingStatisticsAsync(int answerId)
        {
            var drawingData = await _dbSet
                .Where(md => md.AnswerId == answerId && !md.IsDeleted)
                .ToListAsync();

            var statistics = new MapDrawingStatistics
            {
                TotalShapeCount = drawingData.Count,
                ShapeTypeCount = drawingData
                    .GroupBy(md => md.ShapeType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LastUpdateTime = drawingData.Max(md => md.UpdatedAt ?? md.CreatedAt),
                FirstCreateTime = drawingData.Min(md => md.CreatedAt)
            };

            // 计算绘制时长
            if (statistics.FirstCreateTime.HasValue && statistics.LastUpdateTime.HasValue)
            {
                statistics.DrawingDurationSeconds = (int)(statistics.LastUpdateTime.Value - statistics.FirstCreateTime.Value).TotalSeconds;
            }

            return statistics;
        }

        /// <summary>
        /// 根据图形类型获取绘制数据
        /// </summary>
        public async Task<IEnumerable<MapDrawingData>> GetByShapeTypeAsync(int answerId, string shapeType)
        {
            return await _dbSet
                .Where(md => md.AnswerId == answerId && 
                           md.ShapeType == shapeType && 
                           !md.IsDeleted)
                .OrderBy(md => md.OrderIndex)
                .ThenBy(md => md.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取最后更新时间
        /// </summary>
        public async Task<DateTime?> GetLastUpdateTimeAsync(int answerId)
        {
            var lastUpdate = await _dbSet
                .Where(md => md.AnswerId == answerId && !md.IsDeleted)
                .MaxAsync(md => (DateTime?)(md.UpdatedAt ?? md.CreatedAt));

            return lastUpdate;
        }

        /// <summary>
        /// 软删除地图绘制数据
        /// </summary>
        public async Task<bool> SoftDeleteAsync(int drawingId)
        {
            try
            {
                var drawingData = await _dbSet.FindAsync(drawingId);
                if (drawingData != null)
                {
                    drawingData.IsDeleted = true;
                    drawingData.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 恢复软删除的地图绘制数据
        /// </summary>
        public async Task<bool> RestoreAsync(int drawingId)
        {
            try
            {
                var drawingData = await _dbSet.FindAsync(drawingId);
                if (drawingData != null)
                {
                    drawingData.IsDeleted = false;
                    drawingData.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}