namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 考试统计信息
    /// </summary>
    public class ExamStatistics
{
    /// <summary>
    /// 总参与人数
    /// </summary>
    public int TotalParticipants { get; set; }

    /// <summary>
    /// 完成考试人数
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// 通过人数
    /// </summary>
    public int PassedCount { get; set; }

    /// <summary>
    /// 平均分
    /// </summary>
    public decimal AverageScore { get; set; }

    /// <summary>
    /// 最高分
    /// </summary>
    public decimal MaxScore { get; set; }

    /// <summary>
    /// 最低分
    /// </summary>
    public decimal MinScore { get; set; }

    /// <summary>
    /// 通过率（百分比）
    /// </summary>
    public decimal PassRate { get; set; }

    /// <summary>
    /// 完成率（百分比）
    /// </summary>
    public decimal CompletionRate => TotalParticipants > 0 ? (decimal)CompletedCount / TotalParticipants * 100 : 0;
    }
}