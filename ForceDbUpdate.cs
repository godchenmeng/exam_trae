using System;
using System.IO;
using System.Data.SQLite;

class Program
{
    static void Main()
    {
        string wpfDbPath = @"E:\Project\exam_trae\ExamSystem.WPF\exam_system.db";
        string rootDbPath = @"E:\Project\exam_trae\exam_system.db";
        
        try
        {
            Console.WriteLine("开始强制更新WPF数据库...");
            
            // 1. 删除WPF目录下的数据库文件
            if (File.Exists(wpfDbPath))
            {
                File.Delete(wpfDbPath);
                Console.WriteLine($"已删除WPF数据库文件: {wpfDbPath}");
            }
            
            // 2. 复制根目录的数据库文件到WPF目录
            if (File.Exists(rootDbPath))
            {
                File.Copy(rootDbPath, wpfDbPath);
                Console.WriteLine($"已复制根目录数据库到WPF目录");
            }
            else
            {
                Console.WriteLine("根目录数据库文件不存在，创建新的数据库文件");
            }
            
            // 3. 验证新数据库的表结构
            using (var connection = new SQLiteConnection($"Data Source={wpfDbPath}"))
            {
                connection.Open();
                Console.WriteLine("成功连接到新的WPF数据库");
                
                // 检查ExamPapers表结构
                string schemaQuery = "PRAGMA table_info(ExamPapers)";
                using (var command = new SQLiteCommand(schemaQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    Console.WriteLine("\n新数据库ExamPapers表结构:");
                    Console.WriteLine("列名\t\t类型\t\t非空\t默认值\t主键");
                    Console.WriteLine("----------------------------------------------------");
                    
                    bool hasIsPublished = false;
                    while (reader.Read())
                    {
                        string columnName = reader["name"].ToString();
                        string type = reader["type"].ToString();
                        string notNull = reader["notnull"].ToString();
                        string defaultValue = reader["dflt_value"]?.ToString() ?? "NULL";
                        string pk = reader["pk"].ToString();
                        
                        Console.WriteLine($"{columnName}\t\t{type}\t\t{notNull}\t{defaultValue}\t{pk}");
                        
                        if (columnName == "IsPublished")
                        {
                            hasIsPublished = true;
                        }
                    }
                    
                    Console.WriteLine($"\nIsPublished列存在: {hasIsPublished}");
                }
                
                // 检查表中的数据数量
                string countQuery = "SELECT COUNT(*) FROM ExamPapers";
                using (var command = new SQLiteCommand(countQuery, connection))
                {
                    var count = command.ExecuteScalar();
                    Console.WriteLine($"ExamPapers表中的记录数: {count}");
                }
            }
            
            Console.WriteLine("\n数据库更新完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}