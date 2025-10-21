using Microsoft.Data.Sqlite;
using System;
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
        
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            
            // 检查ExamPapers表结构
            Console.WriteLine("=== ExamPapers表结构 ===");
            using (var command = new SqliteCommand("PRAGMA table_info(ExamPapers)", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"列名: {reader.GetString("name")}, 类型: {reader.GetString("type")}, 非空: {reader.GetInt32("notnull")}, 默认值: {reader.GetValue("dflt_value")}");
                    }
                }
            }
            
            Console.WriteLine("\n=== ExamPapers表数据 ===");
            using (var command = new SqliteCommand("SELECT PaperId, Name, Status, IsPublished FROM ExamPapers", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"ID: {reader["PaperId"]}, 名称: {reader["Name"]}, 状态: {reader["Status"]}, 是否发布: {reader["IsPublished"]}");
                    }
                }
            }
            
            Console.WriteLine("\n=== 统计信息 ===");
            using (var command = new SqliteCommand("SELECT COUNT(*) as Total, SUM(IsPublished) as Published FROM ExamPapers", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Console.WriteLine($"总试卷数: {reader["Total"]}, 已发布试卷数: {reader["Published"]}");
                    }
                }
            }
            
            Console.WriteLine("\n=== 按状态分组统计 ===");
            using (var command = new SqliteCommand("SELECT Status, COUNT(*) as Count FROM ExamPapers GROUP BY Status", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"状态: {reader["Status"]}, 数量: {reader["Count"]}");
                    }
                }
            }
        }
    }
}