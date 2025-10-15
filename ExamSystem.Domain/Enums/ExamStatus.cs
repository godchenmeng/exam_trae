namespace ExamSystem.Domain.Enums
{

/// <summary>
/// 考试状态枚举
/// </summary>
public enum ExamStatus
{
    /// <summary>
    /// 未开始
    /// </summary>
    NotStarted = 1,

    /// <summary>
    /// 进行中
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 3,

    /// <summary>
    /// 已提交
    /// </summary>
    Submitted = 4,

    /// <summary>
    /// 已评分
    /// </summary>
    Graded = 5,

    /// <summary>
    /// 已超时
    /// </summary>
    Timeout = 6
    }
}
