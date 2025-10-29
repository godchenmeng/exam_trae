using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ExamSystem.Migration
{
    /// <summary>
    /// 地图绘制题数据库迁移执行器
    /// 为双数据库副本一致性应用 AddMapDrawingColumns.sql 迁移脚本
    /// </summary>
    class ApplyMapDrawingMigration
    {
        private static readonly string[] DatabasePaths = {
            @"exam_system.db",                    // 项目根目录副本
            @"ExamSystem.WPF\exam_system.db"      // WPF 项目副本
        };

        private static readonly string MigrationSqlPath = @"DbMigrations\AddMapDrawingColumns.sql";

        static void Main(string[] args)
        {
            Console.WriteLine("=== 地图绘制题数据库迁移执行器 ===");
            Console.WriteLine($"迁移脚本: {MigrationSqlPath}");
            Console.WriteLine($"目标数据库: {string.Join(", ", DatabasePaths)}");
            Console.WriteLine();

            // 检查迁移脚本是否存在
            if (!File.Exists(MigrationSqlPath))
            {
                Console.WriteLine($"❌ 错误: 找不到迁移脚本文件 {MigrationSqlPath}");
                Console.WriteLine("请确保在项目根目录运行此程序。");
                Console.ReadKey();
                return;
            }

            // 读取 SQL 脚本内容
            string migrationSql;
            try
            {
                migrationSql = File.ReadAllText(MigrationSqlPath);
                Console.WriteLine($"✅ 已读取迁移脚本 ({migrationSql.Length} 字符)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 读取迁移脚本失败: {ex.Message}");
                Console.ReadKey();
                return;
            }

            // 逐个处理数据库
            int successCount = 0;
            foreach (var dbPath in DatabasePaths)
            {
                Console.WriteLine($"\n--- 处理数据库: {dbPath} ---");
                
                if (!File.Exists(dbPath))
                {
                    Console.WriteLine($"⚠️  警告: 数据库文件不存在，跳过 {dbPath}");
                    continue;
                }

                try
                {
                    // 备份数据库（可选）
                    var backupPath = $"{dbPath}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                    File.Copy(dbPath, backupPath);
                    Console.WriteLine($"📁 已创建备份: {backupPath}");

                    // 执行迁移
                    using var connection = new SqliteConnection($"Data Source={dbPath}");
                    connection.Open();

                    // 检查是否已经应用过迁移（检查新列是否存在）
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
                        Console.WriteLine("ℹ️  迁移已应用，跳过此数据库");
                        successCount++;
                        continue;
                    }

                    // 执行迁移 SQL
                    using var command = new SqliteCommand(migrationSql, connection);
                    var affectedRows = command.ExecuteNonQuery();
                    
                    Console.WriteLine($"✅ 迁移执行成功 (影响行数: {affectedRows})");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 迁移失败: {ex.Message}");
                    Console.WriteLine($"   详细信息: {ex}");
                }
            }

            Console.WriteLine($"\n=== 迁移完成 ===");
            Console.WriteLine($"成功: {successCount}/{DatabasePaths.Length} 个数据库");
            
            if (successCount == DatabasePaths.Length)
            {
                Console.WriteLine("🎉 所有数据库迁移成功！");
                Console.WriteLine("\n后续步骤:");
                Console.WriteLine("1. 重新编译 WPF 项目以应用 Domain 层实体更新");
                Console.WriteLine("2. 运行 TestMapDrawingView.cs 验证地图绘制题功能");
                Console.WriteLine("3. 如需回滚，请使用 DbMigrations\\RollbackMapDrawingColumns.sql");
            }
            else
            {
                Console.WriteLine("⚠️  部分数据库迁移失败，请检查错误信息并手动处理。");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}