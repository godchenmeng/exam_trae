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
    /// 考试记录Repository实现类
    /// </summary>
    public class ExamRecordRepository : BaseRepository<ExamRecord>, IExamRecordRepository
    {
        public ExamRecordRepository(ExamDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据用户ID获取考试记录
        /// </summary>
        public async Task<IEnumerable<ExamRecord>> GetRecordsByUserIdAsync(int userId)
        {
            return await _dbSet.Where(er => er.UserId == userId)
                              .Include(er => er.ExamPaper)
                              .OrderByDescending(er => er.StartTime)
                              .ToListAsync();
        }

        /// <summary>
        /// 根据试卷ID获取考试记录
        /// </summary>
        public async Task<IEnumerable<ExamRecord>> GetRecordsByPaperIdAsync(int paperId)
        {
            return await _dbSet.Where(er => er.PaperId == paperId)
                              .Include(er => er.User)
                              .OrderByDescending(er => er.StartTime)
                              .ToListAsync();
        }

        /// <summary>
        /// 获取用户在指定试卷的考试记录
        /// </summary>
        public async Task<IEnumerable<ExamRecord>> GetUserPaperRecordsAsync(int userId, int paperId)
        {
            return await _dbSet.Where(er => er.UserId == userId && er.PaperId == paperId)
                              .OrderByDescending(er => er.StartTime)
                              .ToListAsync();
        }

        /// <summary>
        /// 获取用户最新的考试记录
        /// </summary>
        public async Task<ExamRecord?> GetLatestRecordByUserAsync(int userId, int paperId)
        {
            return await _dbSet.Where(er => er.UserId == userId && er.PaperId == paperId)
                              .OrderByDescending(er => er.StartTime)
                              .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 获取正在进行的考试记录
        /// </summary>
        public async Task<IEnumerable<ExamRecord>> GetOngoingExamsAsync(int? userId = null)
        {
            var query = _dbSet.Where(er => er.EndTime == null);

            if (userId.HasValue)
            {
                query = query.Where(er => er.UserId == userId.Value);
            }

            return await query.Include(er => er.User)
                             .Include(er => er.ExamPaper)
                             .OrderByDescending(er => er.StartTime)
                             .ToListAsync();
        }

        /// <summary>
        /// 获取已完成的考试记录
        /// </summary>
        public async Task<IEnumerable<ExamRecord>> GetCompletedExamsAsync(int? userId = null, int? paperId = null)
        {
            var query = _dbSet.Where(er => er.EndTime != null);

            if (userId.HasValue)
            {
                query = query.Where(er => er.UserId == userId.Value);
            }

            if (paperId.HasValue)
            {
                query = query.Where(er => er.PaperId == paperId.Value);
            }

            return await query.Include(er => er.User)
                             .Include(er => er.ExamPaper)
                             .OrderByDescending(er => er.EndTime)
                             .ToListAsync();
        }

        /// <summary>
        /// 获取考试记录及答题记录
        /// </summary>
        public async Task<ExamRecord?> GetRecordWithAnswersAsync(int recordId)
        {
            return await _dbSet.Include(er => er.AnswerRecords)
                              .ThenInclude(ar => ar.Question)
                              .Include(er => er.User)
                              .Include(er => er.ExamPaper)
                              .FirstOrDefaultAsync(er => er.RecordId == recordId);
        }

        /// <summary>
        /// 获取考试记录的答题记录
        /// </summary>
        public async Task<IEnumerable<AnswerRecord>> GetExamAnswersAsync(int recordId)
        {
            return await _context.AnswerRecords
                                .Where(ar => ar.RecordId == recordId)
                                .Include(ar => ar.Question)
                                .OrderBy(ar => ar.Question.QuestionId)
                                .ToListAsync();
        }

        /// <summary>
        /// 添加答题记录
        /// </summary>
        public async Task AddAnswerRecordAsync(AnswerRecord answerRecord)
        {
            await _context.AnswerRecords.AddAsync(answerRecord);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 更新答题记录
        /// </summary>
        public async Task UpdateAnswerRecordAsync(AnswerRecord answerRecord)
        {
            _context.AnswerRecords.Update(answerRecord);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 获取指定题目的答题记录
        /// </summary>
        public async Task<AnswerRecord?> GetAnswerRecordAsync(int recordId, int questionId)
        {
            return await _context.AnswerRecords
                                .FirstOrDefaultAsync(ar => ar.RecordId == recordId && ar.QuestionId == questionId);
        }

        /// <summary>
        /// 完成考试
        /// </summary>
        public async Task CompleteExamAsync(int recordId, decimal score)
        {
            var record = await GetByIdAsync(recordId);
            if (record != null)
            {
                record.EndTime = DateTime.Now;
                record.TotalScore = score;
                await UpdateAsync(record);
            }
        }

        /// <summary>
        /// 获取考试统计信息
        /// </summary>
        public async Task<(int TotalExams, int CompletedExams, int OngoingExams, decimal AverageScore)> GetExamStatisticsAsync(int? userId = null, int? paperId = null)
        {
            var query = _dbSet.AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(er => er.UserId == userId.Value);
            }

            if (paperId.HasValue)
            {
                query = query.Where(er => er.PaperId == paperId.Value);
            }

            var totalExams = await query.CountAsync();
            var completedExams = await query.CountAsync(er => er.EndTime != null);
            var ongoingExams = totalExams - completedExams;
            var averageScore = await query.Where(er => er.EndTime != null && er.TotalScore > 0)
                                        .AverageAsync(er => er.TotalScore);

            return (totalExams, completedExams, ongoingExams, averageScore);
        }

        /// <summary>
        /// 获取成绩分布
        /// </summary>
        public async Task<Dictionary<string, int>> GetScoreDistributionAsync(int paperId)
        {
            var scores = await _dbSet.Where(er => er.PaperId == paperId && er.TotalScore > 0)
                                      .Select(er => er.TotalScore)
                                    .ToListAsync();

            var distribution = new Dictionary<string, int>
            {
                ["0-59"] = scores.Count(s => s < 60),
                ["60-69"] = scores.Count(s => s >= 60 && s < 70),
                ["70-79"] = scores.Count(s => s >= 70 && s < 80),
                ["80-89"] = scores.Count(s => s >= 80 && s < 90),
                ["90-100"] = scores.Count(s => s >= 90)
            };

            return distribution;
        }

        /// <summary>
        /// 获取及格率
        /// </summary>
        public async Task<decimal> GetPassRateAsync(int paperId, decimal passScore)
        {
            var totalCount = await _dbSet.CountAsync(er => er.PaperId == paperId && er.TotalScore > 0);
            if (totalCount == 0) return 0;

            var passCount = await _dbSet.CountAsync(er => er.PaperId == paperId && er.TotalScore >= passScore);
            return (decimal)passCount / totalCount * 100;
        }

        /// <summary>
        /// 获取平均分
        /// </summary>
        public async Task<decimal> GetAverageScoreAsync(int paperId)
        {
            return await _dbSet.Where(er => er.PaperId == paperId && er.TotalScore > 0)
                                .AverageAsync(er => er.TotalScore);
        }

        /// <summary>
        /// 获取最高分
        /// </summary>
        public async Task<decimal> GetHighestScoreAsync(int paperId)
        {
            return await _dbSet.Where(er => er.PaperId == paperId && er.TotalScore > 0)
                                .MaxAsync(er => er.TotalScore);
        }

        /// <summary>
        /// 获取最低分
        /// </summary>
        public async Task<decimal> GetLowestScoreAsync(int paperId)
        {
            return await _dbSet.Where(er => er.PaperId == paperId && er.TotalScore > 0)
                                .MinAsync(er => er.TotalScore);
        }

        /// <summary>
        /// 获取考试排名
        /// </summary>
        public async Task<IEnumerable<ExamRecord>> GetExamRankingAsync(int paperId, int topCount = 10)
        {
            return await _dbSet.Where(er => er.PaperId == paperId && er.TotalScore > 0)
                                .Include(er => er.User)
                                .OrderByDescending(er => er.TotalScore)
                              .ThenBy(er => er.EndTime)
                              .Take(topCount)
                              .ToListAsync();
        }

        /// <summary>
        /// 检查用户是否已参加考试
        /// </summary>
        public async Task<bool> HasUserTakenExamAsync(int userId, int paperId)
        {
            return await _dbSet.AnyAsync(er => er.UserId == userId && er.PaperId == paperId);
        }

        /// <summary>
        /// 获取用户考试次数
        /// </summary>
        public async Task<int> GetUserExamCountAsync(int userId, int paperId)
        {
            return await _dbSet.CountAsync(er => er.UserId == userId && er.PaperId == paperId);
        }

        /// <summary>
        /// 搜索考试记录
        /// </summary>
        public async Task<IEnumerable<ExamRecord>> SearchExamRecordsAsync(string keyword, int? userId = null, int? paperId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.Include(er => er.User)
                             .Include(er => er.ExamPaper)
                             .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(er => er.User.Username.Contains(keyword) ||
                                         er.ExamPaper.Name.Contains(keyword));
            }

            if (userId.HasValue)
            {
                query = query.Where(er => er.UserId == userId.Value);
            }

            if (paperId.HasValue)
            {
                query = query.Where(er => er.PaperId == paperId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(er => er.StartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(er => er.StartTime <= endDate.Value);
            }

            return await query.OrderByDescending(er => er.StartTime).ToListAsync();
        }

        /// <summary>
        /// 获取时间范围内的考试记录
        /// </summary>
        public async Task<IEnumerable<ExamRecord>> GetExamRecordsByDateRangeAsync(DateTime startDate, DateTime endDate, int? userId = null, int? paperId = null)
        {
            var query = _dbSet.Where(er => er.StartTime >= startDate && er.StartTime <= endDate);

            if (userId.HasValue)
            {
                query = query.Where(er => er.UserId == userId.Value);
            }

            if (paperId.HasValue)
            {
                query = query.Where(er => er.PaperId == paperId.Value);
            }

            return await query.Include(er => er.User)
                             .Include(er => er.ExamPaper)
                             .OrderByDescending(er => er.StartTime)
                             .ToListAsync();
        }

        /// <summary>
        /// 删除考试记录及相关答题记录
        /// </summary>
        public async Task DeleteExamRecordAsync(int recordId)
        {
            var record = await GetRecordWithAnswersAsync(recordId);
            if (record != null)
            {
                // 删除答题记录
                if (record.AnswerRecords != null && record.AnswerRecords.Any())
                {
                    _context.AnswerRecords.RemoveRange(record.AnswerRecords);
                }

                // 删除考试记录
                await RemoveAsync(record);
            }
        }

        /// <summary>
        /// 根据ID获取考试记录（重写以使用RecordId）
        /// </summary>
        public override async Task<ExamRecord?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(er => er.RecordId == id);
        }

        /// <summary>
        /// 根据ID删除考试记录（重写以使用RecordId）
        /// </summary>
        public override async Task<bool> RemoveByIdAsync(int id)
        {
            await DeleteExamRecordAsync(id);
            return true;
        }
    }
}