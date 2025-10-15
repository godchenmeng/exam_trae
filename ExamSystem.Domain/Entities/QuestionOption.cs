using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Domain.Entities
{

/// <summary>
/// 题目选项实体（用于选择题）
/// </summary>
public class QuestionOption
{
    /// <summary>
    /// 选项ID
    /// </summary>
    [Key]
    public int OptionId { get; set; }

    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    public int QuestionId { get; set; }

    /// <summary>
    /// 选项标签（A、B、C、D等）
    /// </summary>
    [StringLength(10)]
    public string OptionLabel { get; set; } = string.Empty;

    /// <summary>
    /// 选项内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否为正确答案
    /// </summary>
    public bool IsCorrect { get; set; } = false;

    /// <summary>
    /// 选项顺序
    /// </summary>
    public int OrderIndex { get; set; } = 0;

    // 导航属性
    /// <summary>
    /// 所属题目
    /// </summary>
    public virtual Question Question { get; set; } = null!;
    }
}
