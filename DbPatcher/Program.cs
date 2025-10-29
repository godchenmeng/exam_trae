using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main(string[] args)
    {
        // 收集目标数据库路径：
        // 1) 如果有命令行参数，按参数指定的路径进行补丁（支持多个）。
        // 2) 否则尝试自动探测常见位置：解决方案根目录和 WPF 项目目录下的 exam_system.db。
        var targets = new List<string>();

        if (args != null && args.Length > 0)
        {
            foreach (var p in args)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    targets.Add(Path.GetFullPath(p));
                }
            }
        }
        else
        {
            var cwd = Environment.CurrentDirectory;
            var baseDir = AppContext.BaseDirectory;
            // 可能的候选位置
            var candidates = new[]
            {
                Path.Combine(cwd, "exam_system.db"),
                Path.Combine(cwd, "ExamSystem.WPF", "exam_system.db"),
                Path.Combine(baseDir, "exam_system.db"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "exam_system.db")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "ExamSystem.WPF", "exam_system.db")),
            };
            foreach (var c in candidates)
            {
                try
                {
                    var full = Path.GetFullPath(c);
                    if (File.Exists(full) && !targets.Contains(full))
                    {
                        targets.Add(full);
                    }
                }
                catch { /* 忽略路径解析错误 */ }
            }
        }

        if (targets.Count == 0)
        {
            Console.WriteLine("未找到任何现有数据库文件。请通过命令行参数显式传入数据库路径，例如：\n  dotnet run --project DbPatcher/DbPatcher.csproj -- E:/Project/exam_trae/ExamSystem.WPF/exam_system.db E:/Project/exam_trae/exam_system.db");
            return;
        }

        Console.WriteLine("将对以下数据库执行补丁：");
        foreach (var t in targets)
        {
            Console.WriteLine(" - " + t);
        }

        foreach (var dbPath in targets)
        {
            PatchDatabase(dbPath);
        }

        Console.WriteLine("\n所有目标数据库补丁执行完成。");
    }

    static void PatchDatabase(string dbPath)
    {
        string connectionString = $"Data Source={dbPath}";
        Console.WriteLine($"\n[开始] 目标数据库: {dbPath}");

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // 开启外键约束
            using (var pragmaOn = connection.CreateCommand())
            {
                pragmaOn.CommandText = "PRAGMA foreign_keys = ON";
                pragmaOn.ExecuteNonQuery();
            }

            // 创建 Notifications 表
            string createNotifications = @"
CREATE TABLE IF NOT EXISTS Notifications (
    NotificationId INTEGER NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Content TEXT NOT NULL,
    Priority INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    Scope INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    SenderId INTEGER NOT NULL,
    CONSTRAINT FK_Notifications_Users_SenderId FOREIGN KEY (SenderId) REFERENCES Users (UserId) ON DELETE RESTRICT
);";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = createNotifications;
                cmd.ExecuteNonQuery();
            }

            // 创建 NotificationRecipients 表
            string createRecipients = @"
CREATE TABLE IF NOT EXISTS NotificationRecipients (
    NotificationRecipientId INTEGER NOT NULL CONSTRAINT PK_NotificationRecipients PRIMARY KEY AUTOINCREMENT,
    NotificationId INTEGER NOT NULL,
    ReceiverId INTEGER NOT NULL,
    DeliveryStatus INTEGER NOT NULL,
    ReadAt TEXT NULL,
    CreatedAt TEXT NOT NULL,
    CONSTRAINT FK_NotificationRecipients_Notifications_NotificationId FOREIGN KEY (NotificationId) REFERENCES Notifications (NotificationId) ON DELETE CASCADE,
    CONSTRAINT FK_NotificationRecipients_Users_ReceiverId FOREIGN KEY (ReceiverId) REFERENCES Users (UserId) ON DELETE RESTRICT
);";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = createRecipients;
                cmd.ExecuteNonQuery();
            }

            // 创建唯一索引 (NotificationId, ReceiverId)
            string createUniqueIndex = @"
CREATE UNIQUE INDEX IF NOT EXISTS IX_NotificationRecipients_NotificationId_ReceiverId
ON NotificationRecipients (NotificationId, ReceiverId);";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = createUniqueIndex;
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("通知相关表结构已创建/存在，无需重复创建。");

            // 检查并添加 IsPublished 列到 ExamPapers 表
            bool isPublishedExists = false;
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = "PRAGMA table_info(ExamPapers)";
                using var reader = checkCmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetString(1) == "IsPublished")
                    {
                        isPublishedExists = true;
                        break;
                    }
                }
            }

            // 强制删除并重新创建IsPublished列
            if (isPublishedExists)
            {
                Console.WriteLine("删除现有的 IsPublished 列...");
                try
                {
                    // SQLite不支持DROP COLUMN，需要重建表
                    using (var dropCmd = connection.CreateCommand())
                    {
                        dropCmd.CommandText = @"
                            CREATE TABLE ExamPapers_backup AS SELECT 
                                PaperId, Name, Description, Duration, TotalScore, PassScore, 
                                StartTime, EndTime, Status, IsActive, IsRandomOrder, 
                                AllowRetake, AllowViewAnswer, CreatedAt, UpdatedAt, CreatorId
                            FROM ExamPapers";
                        dropCmd.ExecuteNonQuery();
                    }
                    
                    using (var dropCmd = connection.CreateCommand())
                    {
                        dropCmd.CommandText = "DROP TABLE ExamPapers";
                        dropCmd.ExecuteNonQuery();
                    }
                    
                    using (var recreateCmd = connection.CreateCommand())
                    {
                        recreateCmd.CommandText = @"
                            CREATE TABLE ExamPapers (
                                PaperId INTEGER NOT NULL CONSTRAINT PK_ExamPapers PRIMARY KEY AUTOINCREMENT,
                                Name TEXT NOT NULL,
                                Description TEXT NULL,
                                Duration INTEGER NOT NULL,
                                TotalScore REAL NOT NULL,
                                PassScore REAL NOT NULL,
                                StartTime TEXT NULL,
                                EndTime TEXT NULL,
                                Status TEXT NOT NULL,
                                IsActive INTEGER NOT NULL,
                                IsRandomOrder INTEGER NOT NULL,
                                AllowRetake INTEGER NOT NULL,
                                AllowViewAnswer INTEGER NOT NULL,
                                CreatedAt TEXT NOT NULL,
                                UpdatedAt TEXT NULL,
                                CreatorId INTEGER NOT NULL,
                                IsPublished INTEGER NOT NULL DEFAULT 0,
                                CONSTRAINT FK_ExamPapers_Users_CreatorId FOREIGN KEY (CreatorId) REFERENCES Users (UserId) ON DELETE RESTRICT
                            )";
                        recreateCmd.ExecuteNonQuery();
                    }
                    
                    using (var restoreCmd = connection.CreateCommand())
                    {
                        restoreCmd.CommandText = @"
                            INSERT INTO ExamPapers (PaperId, Name, Description, Duration, TotalScore, PassScore, 
                                StartTime, EndTime, Status, IsActive, IsRandomOrder, AllowRetake, AllowViewAnswer, 
                                CreatedAt, UpdatedAt, CreatorId, IsPublished)
                            SELECT PaperId, Name, Description, Duration, TotalScore, PassScore, 
                                StartTime, EndTime, Status, IsActive, IsRandomOrder, AllowRetake, AllowViewAnswer, 
                                CreatedAt, UpdatedAt, CreatorId, 
                                CASE WHEN Status = '已发布' THEN 1 ELSE 0 END
                            FROM ExamPapers_backup";
                        int restored = restoreCmd.ExecuteNonQuery();
                        Console.WriteLine($"恢复了 {restored} 行数据");
                    }
                    
                    using (var cleanupCmd = connection.CreateCommand())
                    {
                        cleanupCmd.CommandText = "DROP TABLE ExamPapers_backup";
                        cleanupCmd.ExecuteNonQuery();
                    }
                    
                    Console.WriteLine("ExamPapers表重建完成，IsPublished列已正确添加");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"重建表时出错: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("正在添加 IsPublished 列到 ExamPapers 表...");
                
                // 添加 IsPublished 列
                using (var addColumnCmd = connection.CreateCommand())
                {
                    addColumnCmd.CommandText = "ALTER TABLE ExamPapers ADD COLUMN IsPublished INTEGER NOT NULL DEFAULT 0";
                    addColumnCmd.ExecuteNonQuery();
                }

                // 根据 Status 字段更新 IsPublished 值
                using (var updateCmd = connection.CreateCommand())
                {
                    updateCmd.CommandText = @"
                        UPDATE ExamPapers 
                        SET IsPublished = CASE 
                            WHEN Status = '已发布' THEN 1 
                            ELSE 0 
                        END";
                    int updatedRows = updateCmd.ExecuteNonQuery();
                    Console.WriteLine($"已更新 {updatedRows} 行记录的 IsPublished 值");
                }

                Console.WriteLine("IsPublished 列添加完成");
            }

            // 添加地图绘制题相关列到 Questions 表
            AddMapDrawingColumns(connection);

            // 添加地图绘制题相关列到 AnswerRecords 表
            AddMapDrawingAnswerColumns(connection);

            // 验证表是否存在
            using (var verifyCmd = connection.CreateCommand())
            {
                verifyCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('Notifications','NotificationRecipients')";
                using var reader = verifyCmd.ExecuteReader();
                Console.WriteLine("\n已存在的通知相关表:");
                while (reader.Read())
                {
                    Console.WriteLine("- " + reader.GetString(0));
                }
            }

            Console.WriteLine("\n[完成] 数据库补丁完成。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[失败] 补丁执行失败: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.ExitCode = 1;
        }
    }

    static void AddMapDrawingColumns(SqliteConnection connection)
    {
        Console.WriteLine("\n[地图绘制题] 检查 Questions 表的地图绘制相关列...");
        
        // 检查 MapDrawingConfigJson 列是否存在
        bool mapDrawingConfigExists = false;
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "PRAGMA table_info(Questions)";
            using var reader = checkCmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(1) == "MapDrawingConfigJson")
                {
                    mapDrawingConfigExists = true;
                    break;
                }
            }
        }

        if (!mapDrawingConfigExists)
        {
            Console.WriteLine("正在添加 MapDrawingConfigJson 列到 Questions 表...");
            using (var addColumnCmd = connection.CreateCommand())
            {
                addColumnCmd.CommandText = "ALTER TABLE Questions ADD COLUMN MapDrawingConfigJson TEXT NULL";
                addColumnCmd.ExecuteNonQuery();
            }
            Console.WriteLine("MapDrawingConfigJson 列添加完成");
        }
        else
        {
            Console.WriteLine("MapDrawingConfigJson 列已存在，跳过");
        }
    }

    static void AddMapDrawingAnswerColumns(SqliteConnection connection)
    {
        Console.WriteLine("\n[地图绘制题] 检查 AnswerRecords 表的地图绘制相关列...");
        
        // 检查需要添加的列
        var columnsToAdd = new Dictionary<string, string>
        {
            { "DrawDurationSeconds", "INTEGER NULL" },
            { "ClientInfoJson", "TEXT NULL" },
            { "RubricScoresJson", "TEXT NULL" }
        };

        foreach (var column in columnsToAdd)
        {
            bool columnExists = false;
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = "PRAGMA table_info(AnswerRecords)";
                using var reader = checkCmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetString(1) == column.Key)
                    {
                        columnExists = true;
                        break;
                    }
                }
            }

            if (!columnExists)
            {
                Console.WriteLine($"正在添加 {column.Key} 列到 AnswerRecords 表...");
                using (var addColumnCmd = connection.CreateCommand())
                {
                    addColumnCmd.CommandText = $"ALTER TABLE AnswerRecords ADD COLUMN {column.Key} {column.Value}";
                    addColumnCmd.ExecuteNonQuery();
                }
                Console.WriteLine($"{column.Key} 列添加完成");
            }
            else
            {
                Console.WriteLine($"{column.Key} 列已存在，跳过");
            }
        }
    }
}