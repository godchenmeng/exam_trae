using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 通知 Repository 实现
    /// </summary>
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ExamDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetBySenderIdAsync(int senderId)
        {
            return await _dbSet.Where(n => n.SenderId == senderId)
                               .OrderByDescending(n => n.CreatedAt)
                               .ToListAsync();
        }

        public async Task<Notification?> GetWithRecipientsAsync(int notificationId)
        {
            return await _dbSet.Include(n => n.Recipients)
                               .ThenInclude(r => r.Receiver)
                               .Include(n => n.Sender)
                               .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
        }

        public override async Task<Notification?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(n => n.NotificationId == id);
        }
    }
}