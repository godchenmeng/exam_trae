using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 题库服务接口
    /// </summary>
    public interface IQuestionBankService
{
    /// <summary>
    /// 获取所有题库
    /// </summary>
    /// <returns>题库列表</returns>
    Task<List<QuestionBank>> GetAllQuestionBanksAsync();

    /// <summary>
    /// 根据ID获取题库
    /// </summary>
    /// <param name="bankId">题库ID</param>
    /// <returns>题库信息</returns>
    Task<QuestionBank?> GetQuestionBankByIdAsync(int bankId);

    /// <summary>
    /// 创建题库
    /// </summary>
    /// <param name="questionBank">题库信息</param>
    /// <returns>创建结果</returns>
    Task<bool> CreateQuestionBankAsync(QuestionBank questionBank);

    /// <summary>
    /// 更新题库
    /// </summary>
    /// <param name="questionBank">题库信息</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateQuestionBankAsync(QuestionBank questionBank);

    /// <summary>
    /// 删除题库
    /// </summary>
    /// <param name="bankId">题库ID</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteQuestionBankAsync(int bankId);

    /// <summary>
    /// 检查题库名称是否存在
    /// </summary>
    /// <param name="name">题库名称</param>
    /// <param name="excludeBankId">排除的题库ID（用于编辑时检查）</param>
    /// <returns>是否存在</returns>
    Task<bool> IsQuestionBankNameExistsAsync(string name, int? excludeBankId = null);

    /// <summary>
    /// 获取题库统计信息
    /// </summary>
    /// <param name="bankId">题库ID</param>
    /// <returns>统计信息</returns>
    Task<QuestionBankStatistics> GetQuestionBankStatisticsAsync(int bankId);
}

/// <summary>
/// 题库统计信息
/// </summary>
public class QuestionBankStatistics
{
    public int TotalQuestions { get; set; }
    public int SingleChoiceCount { get; set; }
    public int MultipleChoiceCount { get; set; }
    public int TrueFalseCount { get; set; }
    public int FillInBlankCount { get; set; }
    public int EssayCount { get; set; }
    public int MapDrawingCount { get; set; }
    public int EasyCount { get; set; }
    public int MediumCount { get; set; }
    public int HardCount { get; set; }
    }
}