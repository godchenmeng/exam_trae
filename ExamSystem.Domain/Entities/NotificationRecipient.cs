using System;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Domain.Entities
{
    public class NotificationRecipient
    {
        public int NotificationRecipientId { get; set; }

        // 外键到通知
        public int NotificationId { get; set; }
        public Notification? Notification { get; set; }

        // 外键到用户（接收人）
        public int ReceiverId { get; set; }
        public User? Receiver { get; set; }

        public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}