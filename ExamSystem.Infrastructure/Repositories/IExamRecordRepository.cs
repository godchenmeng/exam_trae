using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 考试记录Repository接口
    /// </summary>
    public interface IExamRecordRepository : IRepository<ExamRecord>
    {
        /// <summary>
        /// 根据用户ID获取考试记录
        /// </summary>
        Task<IEnumerable<ExamRecord>> GetRecordsByUserIdAsync(int userId);

        /// <summary>
        /// 根据试卷ID获取考试记录
        /// </summary>
        Task<IEnumerable<ExamRecord>> GetRecordsByPaperIdAsync(int paperId);

        /// <summary>
        /// 获取用户在指定试卷的考试记录
        /// </summary>
        Task<IEnumerable<ExamRecord>> GetUserPaperRecordsAsync(int userId, int paperId);

        /// <summary>
        /// 获取用户最新的考试记录
        /// </summary>
        Task<ExamRecord?> GetLatestRecordByUserAsync(int userId, int paperId);

        /// <summary>
        /// 获取正在进行的考试记录
        /// </summary>
        Task<IEnumerable<ExamRecord>> GetOngoingExamsAsync(int? userId = null);

        /// <summary>
        /// 获取已完成的考试记录
        /// </summary>
        Task<IEnumerable<ExamRecord>> GetCompletedExamsAsync(int? userId = null, int? paperId = null);

        /// <summary>
        /// 获取考试记录及答题记录
        /// </summary>
        Task<ExamRecord?> GetRecordWithAnswersAsync(int recordId);

        /// <summary>
        /// 获取考试记录的答题记录
        /// </summary>
        Task<IEnumerable<AnswerRecord>> GetExamAnswersAsync(int recordId);

        /// <summary>
        /// 添加答题记录
        /// </summary>
        Task AddAnswerRecordAsync(AnswerRecord answerRecord);

        /// <summary>
        /// 更新答题记录
        /// </summary>
        Task UpdateAnswerRecordAsync(AnswerRecord answerRecord);

        /// <summary>
        /// 获取指定题目的答题记录
        /// </summary>
        Task<AnswerRecord?> GetAnswerRecordAsync(int recordId, int questionId);

        /// <summary>
        /// 完成考试
        /// </summary>
        Task CompleteExamAsync(int recordId, decimal score);

        /// <summary>
        /// 获取考试统计信息
        /// </summary>
        Task<(int TotalExams, int CompletedExams, int OngoingExams, decimal AverageScore)> GetExamStatisticsAsync(int? userId = null, int? paperId = null);

        /// <summary>
        /// 获取成绩分布
        /// </summary>
        Task<Dictionary<string, int>> GetScoreDistributionAsync(int paperId);

        /// <summary>
        /// 获取及格率
        /// </summary>
        Task<decimal> GetPassRateAsync(int paperId, decimal passScore);

        /// <summary>
        /// 获取平均分
        /// </summary>
        Task<decimal> GetAverageScoreAsync(int paperId);

        /// <summary>
        /// 获取最高分
        /// </summary>
        Task<decimal> GetHighestScoreAsync(int paperId);

        /// <summary>
        /// 获取最低分
        /// </summary>
        Task<decimal> GetLowestScoreAsync(int paperId);

        /// <summary>
        /// 获取考试排名
        /// </summary>
        Task<IEnumerable<ExamRecord>> GetExamRankingAsync(int paperId, int topCount = 10);

        /// <summary>
        /// 检查用户是否已参加考试
        /// </summary>
        Task<bool> HasUserTakenExamAsync(int userId, int paperId);

        /// <summary>
        /// 获取用户考试次数
        /// </summary>
        Task<int> GetUserExamCountAsync(int userId, int paperId);

        /// <summary>
        /// 搜索考试记录
        /// </summary>
        Task<IEnumerable<ExamRecord>> SearchExamRecordsAsync(string keyword, int? userId = null, int? paperId = null, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 获取时间范围内的考试记录
        /// </summary>
        Task<IEnumerable<ExamRecord>> GetExamRecordsByDateRangeAsync(DateTime startDate, DateTime endDate, int? userId = null, int? paperId = null);

        /// <summary>
        /// 删除考试记录及相关答题记录
        /// </summary>
        Task DeleteExamRecordAsync(int recordId);
    }
}