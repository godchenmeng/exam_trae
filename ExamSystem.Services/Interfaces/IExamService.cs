using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Models;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 考试服务接口
    /// </summary>
    public interface IExamService
{
    /// <summary>
    /// 开始考试
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="paperId">试卷ID</param>
    /// <returns>考试记录</returns>
    Task<ExamRecord> StartExamAsync(int userId, int paperId);
    
    /// <summary>
    /// 获取考试进度
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试记录</returns>
    Task<ExamRecord?> GetExamProgressAsync(int recordId);
    
    /// <summary>
    /// 保存答案
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="questionId">题目ID</param>
    /// <param name="userAnswer">用户答案</param>
    /// <returns>是否保存成功</returns>
    Task<bool> SaveAnswerAsync(int recordId, int questionId, string userAnswer);
    
    /// <summary>
    /// 提交考试
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>是否提交成功</returns>
    Task<bool> SubmitExamAsync(int recordId);
    
    /// <summary>
    /// 获取考试记录
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试记录</returns>
    Task<ExamRecord?> GetExamRecordAsync(int recordId);
    
    /// <summary>
    /// 检查考试是否超时
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>是否超时</returns>
    Task<bool> CheckExamTimeoutAsync(int recordId);
    
    /// <summary>
    /// 继续考试
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试记录</returns>
    Task<ExamRecord?> ResumeExamAsync(int recordId);
    
    /// <summary>
    /// 获取用户的考试记录列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="status">考试状态筛选</param>
    /// <returns>考试记录列表</returns>
    Task<List<ExamRecord>> GetUserExamRecordsAsync(int userId, ExamStatus? status = null);
    
    /// <summary>
    /// 获取试卷的考试记录列表
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <param name="status">考试状态筛选</param>
    /// <returns>考试记录列表</returns>
    Task<List<ExamRecord>> GetPaperExamRecordsAsync(int paperId, ExamStatus? status = null);
    
    /// <summary>
    /// 获取答题记录
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>答题记录列表</returns>
    Task<List<AnswerRecord>> GetAnswerRecordsAsync(int recordId);
    
    /// <summary>
    /// 自动评分客观题
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>是否评分成功</returns>
    Task<bool> AutoGradeObjectiveQuestionsAsync(int recordId);
    
    /// <summary>
    /// 计算考试总分
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>总分</returns>
    Task<decimal> CalculateTotalScoreAsync(int recordId);
    
    /// <summary>
    /// 更新剩余时间
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="remainingTime">剩余时间（秒）</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateRemainingTimeAsync(int recordId, int remainingTime);
    
    /// <summary>
    /// 检查用户是否可以参加考试
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="paperId">试卷ID</param>
    /// <returns>是否可以参加</returns>
    Task<bool> CanUserTakeExamAsync(int userId, int paperId);

    /// <summary>
    /// 验证用户考试资格（详细版本）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="paperId">试卷ID</param>
    /// <returns>验证结果</returns>
    Task<ExamValidationResult> ValidateUserExamEligibilityAsync(int userId, int paperId);
    
    /// <summary>
    /// 获取考试统计信息
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <returns>统计信息</returns>
    Task<ExamStatistics> GetExamStatisticsAsync(int paperId);
    
    /// <summary>
    /// 获取学生可参加的考试列表
    /// </summary>
    /// <param name="userId">学生用户ID</param>
    /// <returns>可参加的试卷列表</returns>
    Task<List<StudentExamInfo>> GetAvailableExamsForStudentAsync(int userId);
    
    /// <summary>
    /// 获取学生考试结果列表
    /// </summary>
    /// <param name="userId">学生用户ID</param>
    /// <param name="searchKeyword">搜索关键词</param>
    /// <param name="subjectFilter">科目筛选</param>
    /// <param name="timeRangeFilter">时间范围筛选</param>
    /// <returns>考试结果列表</returns>
    Task<List<StudentExamResult>> GetStudentExamResultsAsync(int userId, string? searchKeyword = null, 
        string? subjectFilter = null, string? timeRangeFilter = null);
    
    /// <summary>
    /// 获取考试结果详情
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试结果详情</returns>
    Task<ExamResultDetail> GetExamResultDetailAsync(int recordId);
}

/// <summary>
/// 考试结果详情
/// </summary>
public class ExamResultDetail
{
    /// <summary>
    /// 考试记录ID
    /// </summary>
    public int RecordId { get; set; }
    
    /// <summary>
    /// 考试标题
    /// </summary>
    public string ExamTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// 考试时间
    /// </summary>
    public DateTime ExamDate { get; set; }
    
    /// <summary>
    /// 考试用时
    /// </summary>
    public string Duration { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目总数
    /// </summary>
    public int TotalQuestions { get; set; }
    
    /// <summary>
    /// 考试状态
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; }
    
    /// <summary>
    /// 得分
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// 正确题数
    /// </summary>
    public int CorrectCount { get; set; }
    
    /// <summary>
    /// 错误题数
    /// </summary>
    public int WrongCount { get; set; }
    
    /// <summary>
    /// 教师评语
    /// </summary>
    public string? TeacherComment { get; set; }
    
    /// <summary>
    /// 题目详情列表
    /// </summary>
    public List<QuestionDetail> QuestionDetails { get; set; } = new List<QuestionDetail>();
}

/// <summary>
/// 题目详情
/// </summary>
public class QuestionDetail
{
    /// <summary>
    /// 题目序号
    /// </summary>
    public int QuestionNumber { get; set; }
    
    /// <summary>
    /// 题目类型
    /// </summary>
    public string QuestionType { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目分数
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// 获得分数
    /// </summary>
    public decimal EarnedScore { get; set; }
    
    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 标准答案
    /// </summary>
    public string CorrectAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 学生答案
    /// </summary>
    public string StudentAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目解析
    /// </summary>
    public string? Explanation { get; set; }
    
    /// <summary>
    /// 选项列表（选择题）
    /// </summary>
    public List<OptionDetail> Options { get; set; } = new List<OptionDetail>();
}

/// <summary>
/// 选项详情
/// </summary>
public class OptionDetail
{
    /// <summary>
    /// 选项文本
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否为正确答案
    /// </summary>
    public bool IsCorrect { get; set; }
    
    /// <summary>
    /// 是否为学生选择的答案
    /// </summary>
    public bool IsStudentAnswer { get; set; }
}
}