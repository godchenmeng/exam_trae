using System;
using System.IO;
using Microsoft.Data.Sqlite;

/// <summary>
/// 简单的数据库迁移执行器
/// </summary>
class SimpleDbMigration
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 应用数据库迁移 ===");
        
        var dbPaths = new[] { "exam_system.db", "ExamSystem.WPF/exam_system.db" };
        var migrationSql = @"
BEGIN TRANSACTION;

-- Questions 表新增列（均允许为 NULL，除 TimeLimitSeconds 默认 0）
ALTER TABLE Questions ADD COLUMN MapDrawingConfigJson TEXT;
ALTER TABLE Questions ADD COLUMN GuidanceOverlaysJson TEXT;
ALTER TABLE Questions ADD COLUMN ReferenceOverlaysJson TEXT;
ALTER TABLE Questions ADD COLUMN ReviewRubricJson TEXT;
ALTER TABLE Questions ADD COLUMN TimeLimitSeconds INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Questions ADD COLUMN ShowBuildingLayersJson TEXT;

-- AnswerRecords 表新增列（时长与客户端信息、评分量表明细）
ALTER TABLE AnswerRecords ADD COLUMN DrawDurationSeconds INTEGER NOT NULL DEFAULT 0;
ALTER TABLE AnswerRecords ADD COLUMN ClientInfoJson TEXT;
ALTER TABLE AnswerRecords ADD COLUMN RubricScoresJson TEXT;

COMMIT;
";

        foreach (var dbPath in dbPaths)
        {
            Console.WriteLine($"\n--- 处理数据库: {dbPath} ---");
            
            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"⚠️  数据库文件不存在，跳过: {dbPath}");
                continue;
            }

            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();

                // 检查是否已经应用过迁移
                var checkSql = "PRAGMA table_info(Questions);";
                using var checkCmd = new SqliteCommand(checkSql, connection);
                using var reader = checkCmd.ExecuteReader();
                
                bool hasMapDrawingColumns = false;
                while (reader.Read())
                {
                    var columnName = reader.GetString("name");
                    if (columnName == "MapDrawingConfigJson")
                    {
                        hasMapDrawingColumns = true;
                        break;
                    }
                }
                reader.Close();

                if (hasMapDrawingColumns)
                {
                    Console.WriteLine("✅ 迁移已应用，跳过此数据库");
                    continue;
                }

                // 执行迁移
                using var command = new SqliteCommand(migrationSql, connection);
                command.ExecuteNonQuery();
                
                Console.WriteLine("✅ 数据库迁移执行成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 迁移失败: {ex.Message}");
                if (ex.Message.Contains("duplicate column name"))
                {
                    Console.WriteLine("✅ 列已存在，迁移可能已应用");
                }
                else
                {
                    Console.WriteLine($"详细信息: {ex}");
                }
            }
        }
        
        Console.WriteLine("\n=== 迁移完成 ===");
    }
}