using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities
{
    /// <summary>
    /// 题目实体
    /// </summary>
    public class Question
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [Key]
    public int QuestionId { get; set; }

    /// <summary>
    /// 题库ID
    /// </summary>
    [Required]
    public int BankId { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    public QuestionType QuestionType { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 正确答案
    /// </summary>
    [Required]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 题目解析
    /// </summary>
    public string? Analysis { get; set; }

    /// <summary>
    /// 默认分值
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } = 1.0m;

    /// <summary>
    /// 难度等级
    /// </summary>
    public Difficulty Difficulty { get; set; } = Difficulty.Medium;

    /// <summary>
    /// 标签（用逗号分隔）
    /// </summary>
    [StringLength(200)]
    public string? Tags { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    // 导航属性
    /// <summary>
    /// 所属题库
    /// </summary>
    public virtual QuestionBank QuestionBank { get; set; } = null!;

    /// <summary>
    /// 选项列表（用于选择题）
    /// </summary>
    public virtual ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();

    /// <summary>
    /// 试卷题目关联
    /// </summary>
    public virtual ICollection<PaperQuestion> PaperQuestions { get; set; } = new List<PaperQuestion>();

    /// <summary>
    /// 答题记录
    /// </summary>
    public virtual ICollection<AnswerRecord> AnswerRecords { get; set; } = new List<AnswerRecord>();
    }
}