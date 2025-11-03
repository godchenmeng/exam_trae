using System;
using System.Data.SQLite;
using System.IO;

namespace ExamSystem.Migration
{
    /// <summary>
    /// 执行地图绘制字段迁移的工具
    /// </summary>
    class ExecuteMapDrawingMigration
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 执行地图绘制字段迁移 ===");
            
            try
            {
                // 迁移根目录的数据库
                MigrateDatabase("exam_system.db", "根目录");
                
                // 迁移WPF项目中的数据库
                var wpfDbPath = Path.Combine("ExamSystem.WPF", "exam_system.db");
                MigrateDatabase(wpfDbPath, "WPF项目");
                
                Console.WriteLine("\n=== 迁移完成 ===");
                Console.WriteLine("所有数据库已成功添加地图绘制相关字段！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"迁移失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
        
        static void MigrateDatabase(string dbPath, string location)
        {
            Console.WriteLine($"\n--- 迁移{location}数据库: {dbPath} ---");
            
            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"✗ 数据库文件不存在: {dbPath}");
                return;
            }
            
            try
            {
                using var connection = new SQLiteConnection($"Data Source={dbPath}");
                connection.Open();
                
                // 检查AnswerRecords表是否存在
                var checkTableCmd = new SQLiteCommand(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='AnswerRecords';", 
                    connection);
                var tableName = checkTableCmd.ExecuteScalar() as string;
                
                if (string.IsNullOrEmpty(tableName))
                {
                    Console.WriteLine("✗ AnswerRecords表不存在，跳过迁移");
                    return;
                }
                
                Console.WriteLine("✓ AnswerRecords表存在，开始迁移...");
                
                // 检查字段是否已存在
                bool hasMapCenter = CheckColumnExists(connection, "AnswerRecords", "MapCenter");
                bool hasMapZoom = CheckColumnExists(connection, "AnswerRecords", "MapZoom");
                bool hasMapDrawingData = CheckColumnExists(connection, "AnswerRecords", "MapDrawingData");
                
                Console.WriteLine($"MapCenter字段: {(hasMapCenter ? "已存在" : "需要添加")}");
                Console.WriteLine($"MapZoom字段: {(hasMapZoom ? "已存在" : "需要添加")}");
                Console.WriteLine($"MapDrawingData字段: {(hasMapDrawingData ? "已存在" : "需要添加")}");
                
                // 添加缺少的字段
                if (!hasMapDrawingData)
                {
                    Console.WriteLine("添加MapDrawingData字段...");
                    var addMapDrawingDataCmd = new SQLiteCommand(
                        "ALTER TABLE AnswerRecords ADD COLUMN MapDrawingData TEXT NULL;", 
                        connection);
                    addMapDrawingDataCmd.ExecuteNonQuery();
                    Console.WriteLine("✓ MapDrawingData字段添加成功");
                }
                
                if (!hasMapCenter)
                {
                    Console.WriteLine("添加MapCenter字段...");
                    var addMapCenterCmd = new SQLiteCommand(
                        "ALTER TABLE AnswerRecords ADD COLUMN MapCenter TEXT NULL;", 
                        connection);
                    addMapCenterCmd.ExecuteNonQuery();
                    Console.WriteLine("✓ MapCenter字段添加成功");
                }
                
                if (!hasMapZoom)
                {
                    Console.WriteLine("添加MapZoom字段...");
                    var addMapZoomCmd = new SQLiteCommand(
                        "ALTER TABLE AnswerRecords ADD COLUMN MapZoom INTEGER NULL;", 
                        connection);
                    addMapZoomCmd.ExecuteNonQuery();
                    Console.WriteLine("✓ MapZoom字段添加成功");
                }
                
                if (hasMapCenter && hasMapZoom && hasMapDrawingData)
                {
                    Console.WriteLine("✓ 所有字段都已存在，无需迁移");
                }
                
                // 验证迁移结果
                Console.WriteLine("\n验证迁移结果:");
                var schemaCmd = new SQLiteCommand("PRAGMA table_info(AnswerRecords);", connection);
                using var reader = schemaCmd.ExecuteReader();
                
                bool verifyMapCenter = false;
                bool verifyMapZoom = false;
                bool verifyMapDrawingData = false;
                
                while (reader.Read())
                {
                    var columnName = reader["name"].ToString();
                    if (columnName == "MapCenter") verifyMapCenter = true;
                    if (columnName == "MapZoom") verifyMapZoom = true;
                    if (columnName == "MapDrawingData") verifyMapDrawingData = true;
                }
                
                Console.WriteLine($"MapCenter: {(verifyMapCenter ? "✓" : "✗")}");
                Console.WriteLine($"MapZoom: {(verifyMapZoom ? "✓" : "✗")}");
                Console.WriteLine($"MapDrawingData: {(verifyMapDrawingData ? "✓" : "✗")}");
                
                if (verifyMapCenter && verifyMapZoom && verifyMapDrawingData)
                {
                    Console.WriteLine($"✓ {location}数据库迁移成功！");
                }
                else
                {
                    Console.WriteLine($"✗ {location}数据库迁移失败！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 迁移{location}数据库失败: {ex.Message}");
                throw;
            }
        }
        
        static bool CheckColumnExists(SQLiteConnection connection, string tableName, string columnName)
        {
            var cmd = new SQLiteCommand($"PRAGMA table_info({tableName});", connection);
            using var reader = cmd.ExecuteReader();
            
            while (reader.Read())
            {
                if (reader["name"].ToString() == columnName)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}