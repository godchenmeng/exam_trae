using System;

namespace ExamSystem.Domain.Entities
{
  /// <summary>
  /// 数据库备份与导入操作日志。
  /// </summary>
  public class BackupLog
  {
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty; // manual|auto|import
    public DateTime CreatedAt { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // success|failed|running
    public string? Message { get; set; }
  }
}