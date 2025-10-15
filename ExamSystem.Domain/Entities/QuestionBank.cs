using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Domain.Entities
{

/// <summary>
/// 题库实体
/// </summary>
public class QuestionBank
{
    /// <summary>
    /// 题库ID
    /// </summary>
    [Key]
    public int BankId { get; set; }

    /// <summary>
    /// 题库名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 题库描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    [Required]
    public int CreatorId { get; set; }

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
    /// 创建者
    /// </summary>
    public virtual User Creator { get; set; } = null!;

    /// <summary>
    /// 题目列表
    /// </summary>
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
