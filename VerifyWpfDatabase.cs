using System;
using System.IO;
using Microsoft.Data.Sqlite;

class VerifyWpfDatabase
{
    static void Main()
    {
        Console.WriteLine("=== 验证WPF项目数据库迁移结果 ===\n");
        
        string wpfDbPath = @"ExamSystem.WPF\exam_system.db";
        
        if (!File.Exists(wpfDbPath))
        {
            Console.WriteLine($"❌ WPF数据库文件不存在: {wpfDbPath}");
            return;
        }
        
        try
        {
            using var connection = new SqliteConnection($"Data Source={wpfDbPath}");
            connection.Open();
            
            Console.WriteLine("✅ WPF数据库连接成功");
            
            // 检查AnswerRecords表结构
            var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(AnswerRecords)";
            
            Console.WriteLine("\n📋 AnswerRecords表字段:");
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
                    Console.WriteLine($"  ✅ {columnName} ({columnType})");
                }
                else if (columnName == "MapZoom")
                {
                    hasMapZoom = true;
                    Console.WriteLine($"  ✅ {columnName} ({columnType})");
                }
                else if (columnName == "MapDrawingData")
                {
                    hasMapDrawingData = true;
                    Console.WriteLine($"  ✅ {columnName} ({columnType})");
                }
                else if (columnName.Contains("Id") || columnName.Contains("Answer") || columnName.Contains("User"))
                {
                    Console.WriteLine($"  - {columnName} ({columnType})");
                }
            }
            
            // 检查缺失字段
            if (!hasMapCenter) Console.WriteLine("  ❌ 缺少 MapCenter 字段");
            if (!hasMapZoom) Console.WriteLine("  ❌ 缺少 MapZoom 字段");
            if (!hasMapDrawingData) Console.WriteLine("  ❌ 缺少 MapDrawingData 字段");
            
            // 检查MapDrawingData表
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='MapDrawingData'";
            var result = command.ExecuteScalar();
            
            if (result != null)
            {
                Console.WriteLine("\n✅ MapDrawingData表已创建");
                
                // 检查表结构
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
                Console.WriteLine("\n❌ MapDrawingData表不存在");
            }
            
            // 检查迁移历史
            command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId";
            using var migrationReader = command.ExecuteReader();
            Console.WriteLine("\n📜 已应用的迁移:");
            while (migrationReader.Read())
            {
                string migrationId = migrationReader.GetString("MigrationId");
                Console.WriteLine($"  - {migrationId}");
            }
            
            Console.WriteLine("\n🎉 WPF数据库迁移验证完成！");
            
            if (hasMapCenter && hasMapZoom && hasMapDrawingData && result != null)
            {
                Console.WriteLine("✅ 所有地图绘制相关字段和表都已正确创建");
                Console.WriteLine("✅ 现在可以正常使用地图绘制功能了");
            }
            else
            {
                Console.WriteLine("❌ 部分字段或表缺失，需要重新检查迁移");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 验证失败: {ex.Message}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}