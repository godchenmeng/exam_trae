using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Models;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 消息通知服务接口
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// 发送通知
        /// </summary>
        Task<(bool Success, string? Error)> SendAsync(int senderId, string title, string content, NotificationPriority priority, NotificationScope scope, IEnumerable<int>? targetUserIds = null);

        /// <summary>
        /// 获取用户通知（分页）
        /// </summary>
        Task<(List<NotificationDto> Items, int TotalCount)> GetUserNotificationsAsync(int userId, bool? isRead, int pageIndex, int pageSize);

        /// <summary>
        /// 标记通知为已读
        /// </summary>
        Task<bool> MarkAsReadAsync(int notificationId, int userId);

        /// <summary>
        /// 获取未读数量
        /// </summary>
        Task<int> GetUnreadCountAsync(int userId);
    }
}