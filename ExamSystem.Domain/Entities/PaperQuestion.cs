using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamSystem.Domain.Entities
{

/// <summary>
/// 试卷题目关联实体
/// </summary>
public class PaperQuestion
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 试卷ID
    /// </summary>
    [Required]
    public int PaperId { get; set; }

    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    public int QuestionId { get; set; }

    /// <summary>
    /// 题目在试卷中的顺序
    /// </summary>
    public int OrderIndex { get; set; } = 0;

    /// <summary>
    /// 题目在试卷中的分值
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } = 1.0m;

    // 导航属性
    /// <summary>
    /// 所属试卷
    /// </summary>
    public virtual ExamPaper ExamPaper { get; set; } = null!;

    /// <summary>
    /// 关联题目
    /// </summary>
    public virtual Question Question { get; set; } = null!;
    }
}
