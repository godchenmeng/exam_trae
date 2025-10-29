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

    /// <summary>
    /// 地图绘制题配置（JSON）：allowedTools、requiredOverlays、constraints、showBuildingLayers、timeLimitSeconds
    /// </summary>
    public string? MapDrawingConfigJson { get; set; }

    /// <summary>
    /// 辅助指引图层（JSON）：仅学生端可见，教师端可编辑
    /// </summary>
    public string? GuidanceOverlaysJson { get; set; }

    /// <summary>
    /// 参考答案图层（JSON）：仅教师端可见，用于阅卷对比
    /// </summary>
    public string? ReferenceOverlaysJson { get; set; }

    /// <summary>
    /// 人工评分量表（JSON）：定义评分项与权重
    /// </summary>
    public string? ReviewRubricJson { get; set; }

    /// <summary>
    /// 作答时限（秒），0 表示不限时
    /// </summary>
    public int TimeLimitSeconds { get; set; } = 0;

    /// <summary>
    /// 建筑图层显示配置（JSON）
    /// </summary>
    public string? ShowBuildingLayersJson { get; set; }
    }
}