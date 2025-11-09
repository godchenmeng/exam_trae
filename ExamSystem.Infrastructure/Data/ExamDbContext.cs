using System;
using ExamSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Data
{
    public class ExamDbContext : DbContext
    {
        public ExamDbContext(DbContextOptions<ExamDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<QuestionBank> QuestionBanks { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;
        public DbSet<ExamPaper> ExamPapers { get; set; } = null!;
        public DbSet<PaperQuestion> PaperQuestions { get; set; } = null!;
        public DbSet<ExamRecord> ExamRecords { get; set; } = null!;
        public DbSet<AnswerRecord> AnswerRecords { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<NotificationRecipient> NotificationRecipients { get; set; } = null!;
        public DbSet<MapDrawingData> MapDrawingData { get; set; } = null!;
        public DbSet<Building> Buildings { get; set; } = null!;
        public DbSet<ExamSystem.Domain.Entities.SystemConfig> SystemConfigs { get; set; } = null!;
        public DbSet<ExamSystem.Domain.Entities.SystemConfigLog> SystemConfigLogs { get; set; } = null!;
        public DbSet<ExamSystem.Domain.Entities.BackupLog> BackupLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置User实体
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RealName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                // 创建索引
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // 配置题库表
            modelBuilder.Entity<QuestionBank>(entity =>
            {
                entity.HasKey(e => e.BankId);
                entity.HasOne(e => e.Creator)
                      .WithMany(e => e.CreatedQuestionBanks)
                      .HasForeignKey(e => e.CreatorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 配置题目表
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.QuestionId);
                entity.Property(e => e.QuestionType).HasConversion<int>();
                entity.Property(e => e.Difficulty).HasConversion<int>();
                entity.HasOne(e => e.QuestionBank)
                      .WithMany(e => e.Questions)
                      .HasForeignKey(e => e.BankId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置题目选项表
            modelBuilder.Entity<QuestionOption>(entity =>
            {
                entity.HasKey(e => e.OptionId);
                entity.HasOne(e => e.Question)
                      .WithMany(e => e.Options)
                      .HasForeignKey(e => e.QuestionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置试卷表
            modelBuilder.Entity<ExamPaper>(entity =>
            {
                entity.HasKey(e => e.PaperId);
                entity.HasOne(e => e.Creator)
                      .WithMany(e => e.CreatedExamPapers)
                      .HasForeignKey(e => e.CreatorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 配置试卷题目关联表
            modelBuilder.Entity<PaperQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.ExamPaper)
                      .WithMany(e => e.PaperQuestions)
                      .HasForeignKey(e => e.PaperId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Question)
                      .WithMany(e => e.PaperQuestions)
                      .HasForeignKey(e => e.QuestionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 配置考试记录表
            modelBuilder.Entity<ExamRecord>(entity =>
            {
                entity.HasKey(e => e.RecordId);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.HasOne(e => e.User)
                      .WithMany(e => e.ExamRecords)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ExamPaper)
                      .WithMany(e => e.ExamRecords)
                      .HasForeignKey(e => e.PaperId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Grader)
                      .WithMany()
                      .HasForeignKey(e => e.GraderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // 配置答题记录表
            modelBuilder.Entity<AnswerRecord>(entity =>
            {
                entity.HasKey(e => e.AnswerId);
                entity.HasOne(e => e.ExamRecord)
                      .WithMany(e => e.AnswerRecords)
                      .HasForeignKey(e => e.RecordId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Question)
                      .WithMany(e => e.AnswerRecords)
                      .HasForeignKey(e => e.QuestionId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Grader)
                      .WithMany()
                      .HasForeignKey(e => e.GraderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // 配置通知表
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Priority).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.Scope).HasConversion<int>();
                entity.HasOne(e => e.Sender)
                      .WithMany()
                      .HasForeignKey(e => e.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 配置通知接收人表
            modelBuilder.Entity<NotificationRecipient>(entity =>
            {
                entity.HasKey(e => e.NotificationRecipientId);
                entity.Property(e => e.DeliveryStatus).HasConversion<int>();
                entity.HasIndex(e => new { e.NotificationId, e.ReceiverId }).IsUnique();
                entity.HasOne(e => e.Notification)
                      .WithMany(e => e.Recipients)
                      .HasForeignKey(e => e.NotificationId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Receiver)
                      .WithMany()
                      .HasForeignKey(e => e.ReceiverId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 配置地图绘制数据表
            modelBuilder.Entity<MapDrawingData>(entity =>
            {
                entity.HasKey(e => e.DrawingId);
                entity.Property(e => e.ShapeType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CoordinatesJson).IsRequired().HasColumnType("TEXT");
                entity.Property(e => e.StyleJson).HasColumnType("TEXT");
                entity.Property(e => e.Label).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasOne(e => e.AnswerRecord)
                      .WithMany()
                      .HasForeignKey(e => e.AnswerId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // 创建索引
                entity.HasIndex(e => e.AnswerId);
                entity.HasIndex(e => e.ShapeType);
                entity.HasIndex(e => e.CreatedAt);
            });

            // 配置建筑表
            modelBuilder.Entity<Building>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.CityCn).HasMaxLength(255);
                entity.Property(e => e.OrgCity).HasMaxLength(255);
                entity.Property(e => e.OrgArea).HasMaxLength(255);
                entity.Property(e => e.OrgName).HasMaxLength(255);
                entity.Property(e => e.OrgType).IsRequired();
                entity.Property(e => e.Address).HasColumnType("TEXT");
                entity.Property(e => e.Gps).HasMaxLength(255);
                entity.Property(e => e.CreateDate).IsRequired();
                entity.Property(e => e.UpdateDate).IsRequired();
                entity.Property(e => e.Deleted).IsRequired();
                entity.Property(e => e.Amap).HasMaxLength(255);
                entity.Property(e => e.Location).HasMaxLength(255);

                // 创建索引
                entity.HasIndex(e => e.CityCn);
                entity.HasIndex(e => e.OrgType);
                entity.HasIndex(e => e.Deleted);
                entity.HasIndex(e => new { e.CityCn, e.OrgType, e.Deleted });
            });

            // 配置系统配置表
            modelBuilder.Entity<ExamSystem.Domain.Entities.SystemConfig>(entity =>
            {
                entity.HasKey(e => e.Key);
                entity.Property(e => e.Value).HasColumnType("TEXT");
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            });

            // 配置系统配置日志表
            modelBuilder.Entity<ExamSystem.Domain.Entities.SystemConfigLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).HasMaxLength(200).IsRequired();
                entity.Property(e => e.OldValueHash).HasMaxLength(200);
                entity.Property(e => e.NewValueHash).HasMaxLength(200);
                entity.Property(e => e.Operator).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ChangedAt).IsRequired();
                entity.Property(e => e.Detail).HasColumnType("TEXT");
            });

            // 配置备份日志表
            modelBuilder.Entity<ExamSystem.Domain.Entities.BackupLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Mode).HasMaxLength(50).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.Operator).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Message).HasColumnType("TEXT");
            });

            // 种子数据
            SeedData(modelBuilder);
        }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // 创建默认管理员用户
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                Username = "admin",
                PasswordHash = "admin123", // 临时使用明文，实际应用中需要哈希
                RealName = "系统管理员",
                Role = Domain.Enums.UserRole.Admin,
                Email = "admin@exam.com",
                IsActive = true,
                CreatedAt = DateTime.Now
            }
        );

        // 创建默认题库
        modelBuilder.Entity<QuestionBank>().HasData(
            new QuestionBank
            {
                BankId = 1,
                Name = "默认题库",
                Description = "系统默认题库",
                CreatorId = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        );
    }
    }
}