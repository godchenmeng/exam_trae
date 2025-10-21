using System;
using System.Data.SQLite;
using System.IO;

class Program
{
    static void Main()
    {
        string dbPath = @"e:\Project\exam_trae\ExamSystem.WPF\exam_system.db";
        
        if (!File.Exists(dbPath))
        {
            Console.WriteLine($"数据库文件不存在: {dbPath}");
            return;
        }

        string connectionString = $"Data Source={dbPath}";
        
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            
            Console.WriteLine("检查ExamPapers表结构...");
            
            // 检查IsPublished列是否存在
            bool hasIsPublished = false;
            using (var command = new SQLiteCommand("PRAGMA table_info(ExamPapers)", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    Console.WriteLine("当前列:");
                    while (reader.Read())
                    {
                        string columnName = reader.GetString("name");
                        Console.WriteLine($"  - {columnName}");
                        if (columnName.Equals("IsPublished", StringComparison.OrdinalIgnoreCase))
                        {
                            hasIsPublished = true;
                        }
                    }
                }
            }
            
            if (!hasIsPublished)
            {
                Console.WriteLine("添加IsPublished列...");
                using (var command = new SQLiteCommand("ALTER TABLE ExamPapers ADD COLUMN IsPublished INTEGER NOT NULL DEFAULT 0", connection))
                {
                    command.ExecuteNonQuery();
                }
                
                Console.WriteLine("更新IsPublished值...");
                using (var command = new SQLiteCommand("UPDATE ExamPapers SET IsPublished = 1 WHERE Status = '已发布'", connection))
                {
                    int updated = command.ExecuteNonQuery();
                    Console.WriteLine($"更新了 {updated} 行");
                }
            }
            else
            {
                Console.WriteLine("IsPublished列已存在");
            }
            
            // 验证结果
            Console.WriteLine("\n验证结果:");
            using (var command = new SQLiteCommand("SELECT PaperId, Name, Status, IsPublished FROM ExamPapers LIMIT 5", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"ID: {reader["PaperId"]}, Name: {reader["Name"]}, Status: {reader["Status"]}, IsPublished: {reader["IsPublished"]}");
                    }
                }
            }
        }
        
        Console.WriteLine("完成!");
    }
}