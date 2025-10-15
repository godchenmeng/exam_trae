using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 题目服务接口
    /// </summary>
    public interface IQuestionService
{
    /// <summary>
    /// 根据题库ID获取题目列表
    /// </summary>
    /// <param name="bankId">题库ID</param>
    /// <param name="filter">筛选条件</param>
    /// <returns>题目列表</returns>
    Task<List<Question>> GetQuestionsByBankIdAsync(int bankId, QuestionFilter? filter = null);

    /// <summary>
    /// 根据ID获取题目
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns>题目信息</returns>
    Task<Question?> GetQuestionByIdAsync(int questionId);

    /// <summary>
    /// 创建题目
    /// </summary>
    /// <param name="question">题目信息</param>
    /// <returns>创建结果</returns>
    Task<bool> CreateQuestionAsync(Question question);

    /// <summary>
    /// 更新题目
    /// </summary>
    /// <param name="question">题目信息</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateQuestionAsync(Question question);

    /// <summary>
    /// 删除题目
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteQuestionAsync(int questionId);

    /// <summary>
    /// 批量删除题目
    /// </summary>
    /// <param name="questionIds">题目ID列表</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteQuestionsAsync(List<int> questionIds);

    /// <summary>
    /// 搜索题目
    /// </summary>
    /// <param name="searchCriteria">搜索条件</param>
    /// <returns>题目列表</returns>
    Task<List<Question>> SearchQuestionsAsync(QuestionSearchCriteria searchCriteria);

    /// <summary>
    /// 验证题目数据
    /// </summary>
    /// <param name="question">题目信息</param>
    /// <returns>验证结果</returns>
    Task<ValidationResult> ValidateQuestionAsync(Question question);

    /// <summary>
    /// 复制题目
    /// </summary>
    /// <param name="questionId">源题目ID</param>
    /// <param name="targetBankId">目标题库ID</param>
    /// <returns>复制结果</returns>
    Task<Question?> DuplicateQuestionAsync(int questionId, int targetBankId);

    /// <summary>
    /// 检查题目是否被引用
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns>是否被引用</returns>
    Task<bool> IsQuestionReferencedAsync(int questionId);
}

/// <summary>
/// 题目筛选条件
/// </summary>
public class QuestionFilter
{
    public QuestionType? QuestionType { get; set; }
    public Difficulty? Difficulty { get; set; }
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// 题目搜索条件
/// </summary>
public class QuestionSearchCriteria
{
    public string? Keyword { get; set; }
    public QuestionType? QuestionType { get; set; }
    public Difficulty? Difficulty { get; set; }
    public int? BankId { get; set; }
    public bool? IsActive { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    
    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
    }
    }
}