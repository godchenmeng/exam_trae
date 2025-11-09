using System;

namespace ExamSystem.Domain.Entities
{
  /// <summary>
  /// 系统配置变更日志。
  /// </summary>
  public class SystemConfigLog
  {
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? OldValueHash { get; set; }
    public string? NewValueHash { get; set; }
    public string Operator { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Detail { get; set; }
  }
}