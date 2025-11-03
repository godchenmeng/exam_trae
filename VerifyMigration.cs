using System;
using System.IO;
using Microsoft.Data.Sqlite;

class VerifyMigration
{
    static void Main()
    {
        Console.WriteLine("=== 验证数据库迁移结果 ===\n");
        
        // 检查根目录数据库
        string rootDbPath = "exam_system.db";
        CheckDatabase(rootDbPath, "根目录");
        
        // 检查WPF项目数据库
        string wpfDbPath = @"ExamSystem.WPF\exam_system.db";
        CheckDatabase(wpfDbPath, "WPF项目");
        
        Console.WriteLine("\n=== 验证完成 ===");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
    
    static void CheckDatabase(string dbPath, string location)
    {
        Console.WriteLine($"检查 {location} 数据库: {dbPath}");
        
        if (!File.Exists(dbPath))
        {
            Console.WriteLine($"  ❌ 数据库文件不存在");
            return;
        }
        
        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            
            // 检查AnswerRecords表结构
            var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(AnswerRecords)";
            
            Console.WriteLine("  AnswerRecords表字段:");
            using var reader = command.ExecuteReader();
            bool hasMapCenter = false;
            bool hasMapZoom = false;
            bool hasMapDrawingData = false;
            
            while (reader.Read())
            {
                string columnName = reader.GetString("name");
                string columnType = reader.GetString("type");
                
                if (columnName == "MapCenter")
                {
                    hasMapCenter = true;
                    Console.WriteLine($"    ✅ {columnName} ({columnType})");
                }
                else if (columnName == "MapZoom")
                {
                    hasMapZoom = true;
                    Console.WriteLine($"    ✅ {columnName} ({columnType})");
                }
                else if (columnName == "MapDrawingData")
                {
                    hasMapDrawingData = true;
                    Console.WriteLine($"    ✅ {columnName} ({columnType})");
                }
            }
            
            // 检查是否所有字段都存在
            if (!hasMapCenter) Console.WriteLine("    ❌ 缺少 MapCenter 字段");
            if (!hasMapZoom) Console.WriteLine("    ❌ 缺少 MapZoom 字段");
            if (!hasMapDrawingData) Console.WriteLine("    ❌ 缺少 MapDrawingData 字段");
            
            // 检查MapDrawingData表是否存在
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='MapDrawingData'";
            var result = command.ExecuteScalar();
            
            if (result != null)
            {
                Console.WriteLine("  ✅ MapDrawingData表已创建");
                
                // 检查MapDrawingData表结构
                command.CommandText = "PRAGMA table_info(MapDrawingData)";
                using var mapReader = command.ExecuteReader();
                Console.WriteLine("  MapDrawingData表字段:");
                while (mapReader.Read())
                {
                    string columnName = mapReader.GetString("name");
                    string columnType = mapReader.GetString("type");
                    Console.WriteLine($"    - {columnName} ({columnType})");
                }
            }
            else
            {
                Console.WriteLine("  ❌ MapDrawingData表不存在");
            }
            
            Console.WriteLine($"  ✅ {location} 数据库迁移验证完成\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 验证失败: {ex.Message}\n");
        }
    }
}