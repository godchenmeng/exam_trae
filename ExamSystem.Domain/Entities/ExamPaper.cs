using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamSystem.Domain.Entities
{

/// <summary>
/// 试卷实体
/// </summary>
public class ExamPaper
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    [Key]
    public int PaperId { get; set; }

    /// <summary>
    /// 试卷名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public decimal TotalScore { get; set; } = 100.0m;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int Duration { get; set; } = 90;

    /// <summary>
    /// 创建者ID
    /// </summary>
    [Required]
    public int CreatorId { get; set; }

    /// <summary>
    /// 试卷状态
    /// </summary>
    [StringLength(20)]
    public string Status { get; set; } = "草稿";

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

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

    /// <summary>
    /// 是否随机出题
    /// </summary>
    public bool IsRandomOrder { get; set; } = false;

    /// <summary>
    /// 是否允许重考
    /// </summary>
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 是否允许查看答案
    /// </summary>
    public bool AllowViewAnswer { get; set; } = false;

    /// <summary>
    /// 及格分数
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public decimal PassScore { get; set; } = 60.0m;

    // 导航属性
    /// <summary>
    /// 创建者
    /// </summary>
    public virtual User Creator { get; set; } = null!;

    /// <summary>
    /// 试卷题目列表
    /// </summary>
    public virtual ICollection<PaperQuestion> PaperQuestions { get; set; } = new List<PaperQuestion>();

    /// <summary>
    /// 考试记录
    /// </summary>
    public virtual ICollection<ExamRecord> ExamRecords { get; set; } = new List<ExamRecord>();
    }
}
