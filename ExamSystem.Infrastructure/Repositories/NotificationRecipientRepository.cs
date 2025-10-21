using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 通知接收人 Repository 实现
    /// </summary>
    public class NotificationRecipientRepository : BaseRepository<NotificationRecipient>, INotificationRecipientRepository
    {
        public NotificationRecipientRepository(ExamDbContext context) : base(context)
        {
        }

        public async Task<NotificationRecipient?> GetByNotificationAndReceiverAsync(int notificationId, int receiverId)
        {
            return await _dbSet.Include(r => r.Notification)
                               .ThenInclude(n => n.Sender)
                               .Include(r => r.Receiver)
                               .FirstOrDefaultAsync(r => r.NotificationId == notificationId && r.ReceiverId == receiverId);
        }

        public async Task<(IEnumerable<NotificationRecipient> Items, int TotalCount)> GetUserRecipientsPagedAsync(int receiverId, DeliveryStatus? status, int pageIndex, int pageSize)
        {
            var query = _dbSet.Include(r => r.Notification)
                               .ThenInclude(n => n.Sender)
                               .Where(r => r.ReceiverId == receiverId);

            if (status.HasValue)
            {
                query = query.Where(r => r.DeliveryStatus == status.Value);
            }

            var totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(r => r.Notification!.CreatedAt)
                                   .Skip(pageIndex * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();
            return (items, totalCount);
        }

        public async Task<int> GetUnreadCountAsync(int receiverId)
        {
            return await _dbSet.CountAsync(r => r.ReceiverId == receiverId && r.DeliveryStatus != DeliveryStatus.Read);
        }
    }
}