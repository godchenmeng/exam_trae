using System;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 通知展示模型
    /// </summary>
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public DateTime CreatedAt { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}