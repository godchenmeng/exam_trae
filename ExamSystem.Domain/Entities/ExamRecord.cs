using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities
{
    /// <summary>
    /// 考试记录实体
    /// </summary>
    public class ExamRecord
{
    /// <summary>
    /// 考试记录ID
    /// </summary>
    [Key]
    public int RecordId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// 试卷ID
    /// </summary>
    [Required]
    public int PaperId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 提交时间
    /// </summary>
    public DateTime? SubmitTime { get; set; }

    /// <summary>
    /// 考试状态
    /// </summary>
    public ExamStatus Status { get; set; } = ExamStatus.NotStarted;

    /// <summary>
    /// 总得分
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public decimal TotalScore { get; set; } = 0.0m;

    /// <summary>
    /// 客观题得分
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public decimal ObjectiveScore { get; set; } = 0.0m;

    /// <summary>
    /// 主观题得分
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public decimal SubjectiveScore { get; set; } = 0.0m;

    /// <summary>
    /// 正确题目数
    /// </summary>
    public int CorrectCount { get; set; } = 0;

    /// <summary>
    /// 总题目数
    /// </summary>
    public int TotalCount { get; set; } = 0;

    /// <summary>
    /// 是否通过
    /// </summary>
    public bool IsPassed { get; set; } = false;

    /// <summary>
    /// 剩余时间（秒）
    /// </summary>
    public int RemainingTime { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 评分时间
    /// </summary>
    public DateTime? GradedAt { get; set; }

    /// <summary>
    /// 评分者ID
    /// </summary>
    public int? GraderId { get; set; }

    // 导航属性
    /// <summary>
    /// 考试用户
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 考试试卷
    /// </summary>
    public virtual ExamPaper ExamPaper { get; set; } = null!;

    /// <summary>
    /// 评分者
    /// </summary>
    public virtual User? Grader { get; set; }

    /// <summary>
    /// 答题记录
    /// </summary>
    public virtual ICollection<AnswerRecord> AnswerRecords { get; set; } = new List<AnswerRecord>();
    }
}