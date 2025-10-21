using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;

namespace ExamSystem.Services.Services
{
    /// <summary>
    /// 消息通知服务实现
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ExamDbContext _context;
        private readonly INotificationRepository _notificationRepo;
        private readonly INotificationRecipientRepository _recipientRepo;
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ExamDbContext context,
            INotificationRepository notificationRepo,
            INotificationRecipientRepository recipientRepo,
            IUserService userService,
            IPermissionService permissionService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _notificationRepo = notificationRepo;
            _recipientRepo = recipientRepo;
            _userService = userService;
            _permissionService = permissionService;
            _logger = logger;
        }

        /// <summary>
        /// 发送通知
        /// </summary>
        public async Task<(bool Success, string? Error)> SendAsync(int senderId, string title, string content, NotificationPriority priority, NotificationScope scope, IEnumerable<int>? targetUserIds = null)
        {
            try
            {
                var sender = await _userService.GetUserByIdAsync(senderId);
                if (sender == null)
                {
                    return (false, "发送者不存在");
                }

                // 权限校验
                if (!_permissionService.HasPermission(sender.Role, PermissionKeys.SendNotification))
                {
                    _logger.LogWarning("用户 {UserId} 无权限发送通知", senderId);
                    return (false, "您没有发送通知的权限");
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return (false, "标题不能为空");
                }
                if (string.IsNullOrWhiteSpace(content))
                {
                    return (false, "内容不能为空");
                }

                // 计算接收人
                List<int> receiverIds;
                switch (scope)
                {
                    case NotificationScope.AllStudents:
                        receiverIds = (await _userService.GetUsersByRoleAsync(UserRole.Student))
                            .Select(u => u.UserId)
                            .ToList();
                        break;
                    case NotificationScope.AllUsers:
                        receiverIds = (await _userService.GetAllUsersAsync())
                            .Select(u => u.UserId)
                            .ToList();
                        break;
                    case NotificationScope.SpecificUsers:
                        if (targetUserIds == null || !targetUserIds.Any())
                        {
                            return (false, "请选择接收人");
                        }
                        receiverIds = targetUserIds.Distinct().ToList();
                        break;
                    default:
                        return (false, "未知的通知范围");
                }

                // 发送者不应成为接收者（避免自己收到自己发送的通知）
                receiverIds = receiverIds.Where(id => id != senderId).Distinct().ToList();
                if (!receiverIds.Any())
                {
                    return (false, "未匹配到有效的接收人");
                }

                var notification = new Notification
                {
                    Title = title.Trim(),
                    Content = content.Trim(),
                    Priority = priority,
                    Status = NotificationStatus.Sent,
                    Scope = scope,
                    CreatedAt = DateTime.Now,
                    SenderId = senderId
                };

                await _notificationRepo.AddAsync(notification);
                await _context.SaveChangesAsync(); // 保存以生成 NotificationId

                // 创建接收人记录
                foreach (var rid in receiverIds)
                {
                    var recipient = new NotificationRecipient
                    {
                        NotificationId = notification.NotificationId,
                        ReceiverId = rid,
                        DeliveryStatus = DeliveryStatus.Delivered,
                        CreatedAt = DateTime.Now
                    };
                    await _recipientRepo.AddAsync(recipient);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("通知已发送：NotificationId={NotificationId}, SenderId={SenderId}, Receivers={ReceiverCount}", notification.NotificationId, senderId, receiverIds.Count);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送通知失败");
                return (false, "发送通知失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 获取用户通知（分页）
        /// </summary>
        public async Task<(List<NotificationDto> Items, int TotalCount)> GetUserNotificationsAsync(int userId, bool? isRead, int pageIndex, int pageSize)
        {
            try
            {
                DeliveryStatus? statusFilter = null;
                if (isRead.HasValue && isRead.Value)
                {
                    statusFilter = DeliveryStatus.Read;
                }

                var (items, totalCountRaw) = await _recipientRepo.GetUserRecipientsPagedAsync(userId, statusFilter, pageIndex, pageSize);

                // 对未读筛选（不等于Read）
                var filteredItems = items.Where(r => !isRead.HasValue || (isRead.Value ? r.DeliveryStatus == DeliveryStatus.Read : r.DeliveryStatus != DeliveryStatus.Read))
                                         .ToList();

                // 计算总数（当筛选为未读时需要重新计算）
                var totalCount = totalCountRaw;
                if (isRead.HasValue && !isRead.Value)
                {
                    totalCount = await _context.NotificationRecipients.CountAsync(r => r.ReceiverId == userId && r.DeliveryStatus != DeliveryStatus.Read);
                }

                var dtos = filteredItems.Select(r => new NotificationDto
                {
                    NotificationId = r.NotificationId,
                    Title = r.Notification?.Title ?? string.Empty,
                    Content = r.Notification?.Content ?? string.Empty,
                    Priority = r.Notification?.Priority ?? NotificationPriority.Normal,
                    CreatedAt = r.Notification?.CreatedAt ?? DateTime.MinValue,
                    SenderName = r.Notification?.Sender?.RealName ?? r.Notification?.Sender?.Username ?? "",
                    IsRead = r.DeliveryStatus == DeliveryStatus.Read,
                    ReadAt = r.ReadAt
                }).ToList();

                return (dtos, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户通知失败");
                return (new List<NotificationDto>(), 0);
            }
        }

        /// <summary>
        /// 标记通知为已读
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                var record = await _recipientRepo.GetByNotificationAndReceiverAsync(notificationId, userId);
                if (record == null)
                {
                    _logger.LogWarning("未找到通知接收记录：NotificationId={NotificationId}, UserId={UserId}", notificationId, userId);
                    return false;
                }

                if (record.DeliveryStatus == DeliveryStatus.Read)
                {
                    return true; // 已是已读
                }

                record.DeliveryStatus = DeliveryStatus.Read;
                record.ReadAt = DateTime.Now;
                _recipientRepo.Update(record);

                _logger.LogInformation("通知已标记为已读：NotificationId={NotificationId}, UserId={UserId}", notificationId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记通知为已读失败");
                return false;
            }
        }

        /// <summary>
        /// 获取未读数量
        /// </summary>
        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                return await _recipientRepo.GetUnreadCountAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取未读通知数量失败");
                return 0;
            }
        }
    }
}