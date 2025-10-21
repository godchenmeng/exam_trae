using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string[] dbPaths = {
            @"e:\Project\exam_trae\exam_system.db",
            @"e:\Project\exam_trae\ExamSystem.WPF\exam_system.db"
        };

        foreach (string dbPath in dbPaths)
        {
            Console.WriteLine($"\n=== 检查数据库: {dbPath} ===");
            
            if (!System.IO.File.Exists(dbPath))
            {
                Console.WriteLine("数据库文件不存在！");
                continue;
            }

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            // 检查表结构
            Console.WriteLine("\nExamPapers表结构:");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(ExamPapers)";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"  {reader.GetInt32(0)}: {reader.GetString(1)} {reader.GetString(2)} {(reader.GetInt32(3) == 1 ? "NOT NULL" : "NULL")} {(reader.IsDBNull(4) ? "" : "DEFAULT " + reader.GetValue(4))}");
                }
            }

            // 检查数据
            Console.WriteLine("\nExamPapers数据示例:");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT PaperId, Name, Status, IsPublished FROM ExamPapers LIMIT 5";
                try
                {
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Console.WriteLine($"  ID: {reader.GetInt32(0)}, Name: {reader.GetString(1)}, Status: {reader.GetString(2)}, IsPublished: {reader.GetInt32(3)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"查询数据时出错: {ex.Message}");
                }
            }

            // 统计信息
            Console.WriteLine("\n统计信息:");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM ExamPapers";
                try
                {
                    var total = cmd.ExecuteScalar();
                    Console.WriteLine($"  总试卷数: {total}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"统计总数时出错: {ex.Message}");
                }
            }
        }
    }
}