using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities
{
    /// <summary>
    /// 用户实体
    /// </summary>
    public class User
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Key]
    public int UserId { get; set; }

    /// <summary>
    /// 通用ID属性（为了兼容性）
    /// </summary>
    public int Id => UserId;

    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希值
    /// </summary>
    [Required]
    [StringLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    [StringLength(50)]
    public string? RealName { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    [Required]
    public UserRole Role { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [StringLength(100)]
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 登录失败次数
    /// </summary>
    public int LoginFailCount { get; set; } = 0;

    /// <summary>
    /// 账户锁定到期时间
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// 是否被锁定
    /// </summary>
    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.Now;

    // 导航属性
    /// <summary>
    /// 创建的题库
    /// </summary>
    public virtual ICollection<QuestionBank> CreatedQuestionBanks { get; set; } = new List<QuestionBank>();

    /// <summary>
    /// 创建的试卷
    /// </summary>
    public virtual ICollection<ExamPaper> CreatedExamPapers { get; set; } = new List<ExamPaper>();

    /// <summary>
    /// 考试记录
    /// </summary>
    public virtual ICollection<ExamRecord> ExamRecords { get; set; } = new List<ExamRecord>();
    }
}