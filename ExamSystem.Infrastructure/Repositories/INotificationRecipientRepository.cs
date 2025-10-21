using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 通知接收人 Repository 接口
    /// </summary>
    public interface INotificationRecipientRepository : IRepository<NotificationRecipient>
    {
        /// <summary>
        /// 根据通知ID与接收人ID获取接收记录
        /// </summary>
        Task<NotificationRecipient?> GetByNotificationAndReceiverAsync(int notificationId, int receiverId);

        /// <summary>
        /// 获取用户的通知接收记录（分页，可按状态筛选）
        /// </summary>
        Task<(IEnumerable<NotificationRecipient> Items, int TotalCount)> GetUserRecipientsPagedAsync(int receiverId, DeliveryStatus? status, int pageIndex, int pageSize);

        /// <summary>
        /// 获取未读数量
        /// </summary>
        Task<int> GetUnreadCountAsync(int receiverId);
    }
}