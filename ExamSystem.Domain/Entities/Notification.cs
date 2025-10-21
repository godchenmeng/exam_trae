using System;
using System.Collections.Generic;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public NotificationStatus Status { get; set; } = NotificationStatus.Sent;
        public NotificationScope Scope { get; set; } = NotificationScope.AllStudents;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 发送者
        public int SenderId { get; set; }
        public User? Sender { get; set; }

        // 接收人关联表
        public ICollection<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();
    }
}