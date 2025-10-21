using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 通知 Repository 接口
    /// </summary>
    public interface INotificationRepository : IRepository<Notification>
    {
        /// <summary>
        /// 根据发送者获取通知列表
        /// </summary>
        Task<IEnumerable<Notification>> GetBySenderIdAsync(int senderId);

        /// <summary>
        /// 获取包含接收人明细的通知
        /// </summary>
        Task<Notification?> GetWithRecipientsAsync(int notificationId);
    }
}