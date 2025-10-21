using Microsoft.Data.Sqlite;
using System;
using System.IO;

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
            if (File.Exists(dbPath))
            {
                Console.WriteLine($"处理数据库: {dbPath}");
                ProcessDatabase(dbPath);
            }
            else
            {
                Console.WriteLine($"数据库文件不存在: {dbPath}");
            }
        }
    }

    static void ProcessDatabase(string dbPath)
    {
        string connectionString = $"Data Source={dbPath}";
        
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            
            // 检查ExamPapers表结构
            Console.WriteLine("检查ExamPapers表结构...");
            using (var command = new SqliteCommand("PRAGMA table_info(ExamPapers)", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    bool hasIsPublished = false;
                    Console.WriteLine("ExamPapers表的列:");
                    while (reader.Read())
                    {
                        string columnName = reader.GetString("name");
                        Console.WriteLine($"  - {columnName}");
                        if (columnName.Equals("IsPublished", StringComparison.OrdinalIgnoreCase))
                        {
                            hasIsPublished = true;
                        }
                    }
                    
                    if (!hasIsPublished)
                    {
                        Console.WriteLine("IsPublished列不存在，正在添加...");
                        
                        // 添加IsPublished列
                        using (var addColumnCommand = new SqliteCommand(
                            "ALTER TABLE ExamPapers ADD COLUMN IsPublished INTEGER NOT NULL DEFAULT 0", 
                            connection))
                        {
                            addColumnCommand.ExecuteNonQuery();
                            Console.WriteLine("IsPublished列添加成功");
                        }
                        
                        // 根据Status字段更新IsPublished值
                        using (var updateCommand = new SqliteCommand(
                            "UPDATE ExamPapers SET IsPublished = 1 WHERE Status = '已发布'", 
                            connection))
                        {
                            int updatedRows = updateCommand.ExecuteNonQuery();
                            Console.WriteLine($"更新了 {updatedRows} 行记录的IsPublished状态");
                        }
                    }
                    else
                    {
                        Console.WriteLine("IsPublished列已存在");
                        
                        // 检查数据
                        using (var dataCommand = new SqliteCommand(
                            "SELECT PaperId, Name, Status, IsPublished FROM ExamPapers LIMIT 5", 
                            connection))
                        {
                            using (var dataReader = dataCommand.ExecuteReader())
                            {
                                Console.WriteLine("ExamPapers表数据示例:");
                                while (dataReader.Read())
                                {
                                    Console.WriteLine($"  ID: {dataReader["PaperId"]}, Name: {dataReader["Name"]}, Status: {dataReader["Status"]}, IsPublished: {dataReader["IsPublished"]}");
                                }
                            }
                        }
                    }
                }
            }
            
            Console.WriteLine($"数据库 {dbPath} 处理完成\n");
        }
    }
}