using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 试卷服务接口
    /// </summary>
    public interface IExamPaperService
{
    /// <summary>
    /// 获取所有试卷
    /// </summary>
    /// <returns>试卷列表</returns>
    Task<List<ExamPaper>> GetAllExamPapersAsync();

    /// <summary>
    /// 根据ID获取试卷
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <returns>试卷信息</returns>
    Task<ExamPaper?> GetExamPaperByIdAsync(int paperId);

    /// <summary>
    /// 创建试卷
    /// </summary>
    /// <param name="examPaper">试卷信息</param>
    /// <returns>创建结果</returns>
    Task<bool> CreateExamPaperAsync(ExamPaper examPaper);

    /// <summary>
    /// 更新试卷
    /// </summary>
    /// <param name="examPaper">试卷信息</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateExamPaperAsync(ExamPaper examPaper);

    /// <summary>
    /// 删除试卷
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteExamPaperAsync(int paperId);

    /// <summary>
    /// 检查试卷名称是否存在
    /// </summary>
    /// <param name="name">试卷名称</param>
    /// <param name="excludePaperId">排除的试卷ID（用于编辑时检查）</param>
    /// <returns>是否存在</returns>
    Task<bool> IsExamPaperNameExistsAsync(string name, int? excludePaperId = null);

    /// <summary>
    /// 获取试卷题目列表
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <returns>题目列表</returns>
    Task<List<PaperQuestion>> GetPaperQuestionsAsync(int paperId);

    /// <summary>
    /// 添加题目到试卷
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <param name="questionIds">题目ID列表</param>
    /// <returns>添加结果</returns>
    Task<bool> AddQuestionsToExamPaperAsync(int paperId, List<int> questionIds);

    /// <summary>
    /// 添加单个题目到试卷
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <param name="questionId">题目ID</param>
    /// <param name="orderIndex">题目顺序</param>
    /// <param name="score">题目分值</param>
    /// <returns>添加结果</returns>
    Task<bool> AddQuestionAsync(int paperId, int questionId, int orderIndex, decimal score);

    /// <summary>
    /// 从试卷中移除题目
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <param name="questionIds">题目ID列表</param>
    /// <returns>移除结果</returns>
    Task<bool> RemoveQuestionsFromExamPaperAsync(int paperId, List<int> questionIds);

    /// <summary>
    /// 从试卷中移除单个题目
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <param name="questionId">题目ID</param>
    /// <returns>移除结果</returns>
    Task<bool> RemoveQuestionAsync(int paperId, int questionId);

    /// <summary>
    /// 更新试卷题目顺序
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <param name="questionOrders">题目顺序列表</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateQuestionOrdersAsync(int paperId, List<(int QuestionId, int OrderIndex)> questionOrders);

    /// <summary>
    /// 更新单个题目顺序
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <param name="questionId">题目ID</param>
    /// <param name="orderIndex">新的顺序索引</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateQuestionOrderAsync(int paperId, int questionId, int orderIndex);

    /// <summary>
    /// 更新试卷题目分值
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <param name="questionScores">题目分值列表</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateQuestionScoresAsync(int paperId, List<(int QuestionId, decimal Score)> questionScores);

    /// <summary>
    /// 更新单个题目分值
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <param name="questionId">题目ID</param>
    /// <param name="score">新的分值</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateQuestionScoreAsync(int paperId, int questionId, decimal score);

    /// <summary>
    /// 计算试卷总分
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <returns>总分</returns>
    Task<decimal> CalculateTotalScoreAsync(int paperId);

    /// <summary>
    /// 发布试卷
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <returns>发布结果</returns>
    Task<bool> PublishExamPaperAsync(int paperId);

    /// <summary>
    /// 取消发布试卷
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <returns>取消发布结果</returns>
    Task<bool> UnpublishExamPaperAsync(int paperId);

    /// <summary>
    /// 复制试卷
    /// </summary>
    /// <param name="paperId">源试卷ID</param>
    /// <param name="newName">新试卷名称</param>
    /// <param name="creatorId">创建者ID</param>
    /// <returns>复制结果</returns>
    Task<ExamPaper?> CopyExamPaperAsync(int paperId, string newName, int creatorId);

    /// <summary>
    /// 搜索试卷
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <param name="creatorId">创建者ID（可选）</param>
    /// <param name="status">状态（可选）</param>
    /// <returns>试卷列表</returns>
    Task<List<ExamPaper>> SearchExamPapersAsync(string? keyword = null, int? creatorId = null, string? status = null);
    }
}