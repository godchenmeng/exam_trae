using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

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
    /// 获取考试统计信息
    /// </summary>
    /// <param name="paperId">试卷ID</param>
    /// <returns>统计信息</returns>
    Task<ExamStatistics> GetExamStatisticsAsync(int paperId);
}

/// <summary>
/// 考试统计信息
/// </summary>
public class ExamStatistics
{
    /// <summary>
    /// 参考人数
    /// </summary>
    public int TotalParticipants { get; set; }
    
    /// <summary>
    /// 已完成人数
    /// </summary>
    public int CompletedCount { get; set; }
    
    /// <summary>
    /// 通过人数
    /// </summary>
    public int PassedCount { get; set; }
    
    /// <summary>
    /// 平均分
    /// </summary>
    public decimal AverageScore { get; set; }
    
    /// <summary>
    /// 最高分
    /// </summary>
    public decimal MaxScore { get; set; }
    
    /// <summary>
    /// 最低分
    /// </summary>
    public decimal MinScore { get; set; }
    
    /// <summary>
    /// 通过率
    /// </summary>
    public decimal PassRate { get; set; }
    }
}