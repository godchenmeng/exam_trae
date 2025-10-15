using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 试卷Repository接口
    /// </summary>
    public interface IExamPaperRepository : IRepository<ExamPaper>
    {
        /// <summary>
        /// 根据创建者ID获取试卷列表
        /// </summary>
        /// <param name="creatorId">创建者ID</param>
        /// <returns>试卷列表</returns>
        Task<IEnumerable<ExamPaper>> GetPapersByCreatorIdAsync(int creatorId);

        /// <summary>
        /// 获取已发布的试卷列表
        /// </summary>
        /// <returns>已发布的试卷列表</returns>
        Task<IEnumerable<ExamPaper>> GetPublishedPapersAsync();

        /// <summary>
        /// 搜索试卷
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="creatorId">创建者ID（可选）</param>
        /// <param name="isPublished">是否已发布（可选）</param>
        /// <returns>符合条件的试卷列表</returns>
        Task<IEnumerable<ExamPaper>> SearchPapersAsync(string keyword, int? creatorId = null, bool? isPublished = null);

        /// <summary>
        /// 获取试卷及其题目
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <returns>包含题目的试卷</returns>
        Task<ExamPaper?> GetPaperWithQuestionsAsync(int paperId);

        /// <summary>
        /// 获取试卷的题目列表
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <returns>题目列表</returns>
        Task<IEnumerable<Question>> GetPaperQuestionsAsync(int paperId);

        /// <summary>
        /// 获取试卷的题目及选项
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <returns>包含选项的题目列表</returns>
        Task<IEnumerable<Question>> GetPaperQuestionsWithOptionsAsync(int paperId);

        /// <summary>
        /// 添加题目到试卷
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <param name="questionId">题目ID</param>
        /// <param name="score">分值</param>
        /// <param name="sortOrder">排序</param>
        Task AddQuestionToPaperAsync(int paperId, int questionId, decimal score, int sortOrder);

        /// <summary>
        /// 从试卷中移除题目
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <param name="questionId">题目ID</param>
        Task RemoveQuestionFromPaperAsync(int paperId, int questionId);

        /// <summary>
        /// 更新试卷题目分值
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <param name="questionId">题目ID</param>
        /// <param name="score">新分值</param>
        Task UpdateQuestionScoreAsync(int paperId, int questionId, decimal score);

        /// <summary>
        /// 更新试卷题目排序
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <param name="questionId">题目ID</param>
        /// <param name="sortOrder">新排序</param>
        Task UpdateQuestionSortOrderAsync(int paperId, int questionId, int sortOrder);

        /// <summary>
        /// 复制试卷
        /// </summary>
        /// <param name="paperId">原试卷ID</param>
        /// <param name="newTitle">新试卷标题</param>
        /// <param name="creatorId">创建者ID</param>
        /// <returns>复制后的试卷</returns>
        Task<ExamPaper> CopyPaperAsync(int paperId, string newTitle, int creatorId);

        /// <summary>
        /// 发布/取消发布试卷
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <param name="isPublished">是否发布</param>
        Task SetPaperPublishStatusAsync(int paperId, bool isPublished);

        /// <summary>
        /// 获取试卷统计信息
        /// </summary>
        /// <param name="creatorId">创建者ID（可选）</param>
        /// <returns>试卷统计信息</returns>
        Task<(int TotalPapers, int PublishedPapers, int DraftPapers)> GetPaperStatisticsAsync(int? creatorId = null);

        /// <summary>
        /// 检查试卷是否被考试使用
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <returns>是否被使用</returns>
        Task<bool> IsPaperUsedInExamsAsync(int paperId);

        /// <summary>
        /// 获取使用该试卷的考试记录
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <returns>考试记录列表</returns>
        Task<IEnumerable<ExamRecord>> GetExamRecordsUsingPaperAsync(int paperId);

        /// <summary>
        /// 计算试卷总分
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <returns>试卷总分</returns>
        Task<decimal> CalculatePaperTotalScoreAsync(int paperId);

        /// <summary>
        /// 获取试卷题目数量
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        /// <returns>题目数量</returns>
        Task<int> GetPaperQuestionCountAsync(int paperId);

        /// <summary>
        /// 清空试卷所有题目
        /// </summary>
        /// <param name="paperId">试卷ID</param>
        Task ClearPaperQuestionsAsync(int paperId);
    }
}