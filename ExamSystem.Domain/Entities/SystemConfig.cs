using System;

namespace ExamSystem.Domain.Entities
{
  /// <summary>
  /// 系统配置项（敏感值需加密存储）。
  /// </summary>
  public class SystemConfig
  {
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty; // 加密或明文，视内容而定
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
  }
}