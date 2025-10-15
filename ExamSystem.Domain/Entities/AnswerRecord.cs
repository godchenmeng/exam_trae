using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamSystem.Domain.Entities
{

/// <summary>
/// 答题记录实体
/// </summary>
public class AnswerRecord
{
    /// <summary>
    /// 答题记录ID
    /// </summary>
    [Key]
    public int AnswerId { get; set; }

    /// <summary>
    /// 考试记录ID
    /// </summary>
    [Required]
    public int RecordId { get; set; }

    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    public int QuestionId { get; set; }

    /// <summary>
    /// 用户答案
    /// </summary>
    public string? UserAnswer { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } = 0.0m;

    /// <summary>
    /// 是否正确
    /// </summary>
    public bool IsCorrect { get; set; } = false;

    /// <summary>
    /// 是否已评分
    /// </summary>
    public bool IsGraded { get; set; } = false;

    /// <summary>
    /// 评语
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 答题时间
    /// </summary>
    public DateTime? AnswerTime { get; set; }

    /// <summary>
    /// 评分时间
    /// </summary>
    public DateTime? GradeTime { get; set; }

    /// <summary>
    /// 评分者ID
    /// </summary>
    public int? GraderId { get; set; }

    // 导航属性
    /// <summary>
    /// 考试记录
    /// </summary>
    public virtual ExamRecord ExamRecord { get; set; } = null!;

    /// <summary>
    /// 题目
    /// </summary>
    public virtual Question Question { get; set; } = null!;

    /// <summary>
    /// 评分者
    /// </summary>
    public virtual User? Grader { get; set; }
    }
}
