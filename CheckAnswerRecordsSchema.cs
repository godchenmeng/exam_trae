using System;
using System.Data.SQLite;
using System.IO;

namespace ExamSystem.Migration
{
    /// <summary>
    /// 检查AnswerRecords表结构的工具
    /// </summary>
    class CheckAnswerRecordsSchema
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 检查AnswerRecords表结构 ===");
            
            try
            {
                // 检查根目录的数据库
                CheckDatabase("exam_system.db", "根目录");
                
                // 检查WPF项目中的数据库
                var wpfDbPath = Path.Combine("ExamSystem.WPF", "exam_system.db");
                CheckDatabase(wpfDbPath, "WPF项目");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
        
        static void CheckDatabase(string dbPath, string location)
        {
            Console.WriteLine($"\n--- 检查{location}数据库: {dbPath} ---");
            
            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"✗ 数据库文件不存在: {dbPath}");
                return;
            }
            
            var fileInfo = new FileInfo(dbPath);
            Console.WriteLine($"✓ 数据库文件存在，大小: {fileInfo.Length} 字节");
            
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
                    Console.WriteLine("✗ AnswerRecords表不存在");
                    return;
                }
                
                Console.WriteLine("✓ AnswerRecords表存在");
                
                // 获取表结构
                var schemaCmd = new SQLiteCommand("PRAGMA table_info(AnswerRecords);", connection);
                using var reader = schemaCmd.ExecuteReader();
                
                Console.WriteLine("\n当前表结构:");
                Console.WriteLine("列名\t\t类型\t\t非空\t默认值");
                Console.WriteLine("----------------------------------------");
                
                bool hasMapCenter = false;
                bool hasMapZoom = false;
                bool hasMapDrawingData = false;
                
                while (reader.Read())
                {
                    var columnName = reader["name"].ToString();
                    var columnType = reader["type"].ToString();
                    var notNull = reader["notnull"].ToString();
                    var defaultValue = reader["dflt_value"]?.ToString() ?? "NULL";
                    
                    Console.WriteLine($"{columnName,-15}\t{columnType,-15}\t{notNull}\t{defaultValue}");
                    
                    // 检查地图绘制相关字段
                    if (columnName == "MapCenter") hasMapCenter = true;
                    if (columnName == "MapZoom") hasMapZoom = true;
                    if (columnName == "MapDrawingData") hasMapDrawingData = true;
                }
                
                Console.WriteLine("\n地图绘制字段检查:");
                Console.WriteLine($"MapCenter: {(hasMapCenter ? "✓ 存在" : "✗ 缺少")}");
                Console.WriteLine($"MapZoom: {(hasMapZoom ? "✓ 存在" : "✗ 缺少")}");
                Console.WriteLine($"MapDrawingData: {(hasMapDrawingData ? "✓ 存在" : "✗ 缺少")}");
                
                if (!hasMapCenter || !hasMapZoom || !hasMapDrawingData)
                {
                    Console.WriteLine("\n⚠ 需要执行数据库迁移来添加缺少的字段");
                }
                else
                {
                    Console.WriteLine("\n✓ 所有地图绘制字段都已存在");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 检查数据库失败: {ex.Message}");
            }
        }
    }
}