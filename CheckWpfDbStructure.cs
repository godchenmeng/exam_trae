using System;
using System.Data.SQLite;

class Program
{
    static void Main()
    {
        string connectionString = @"Data Source=E:\Project\exam_trae\ExamSystem.WPF\exam_system.db";
        
        try
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("成功连接到WPF数据库");
                
                // 检查ExamPapers表结构
                string schemaQuery = "PRAGMA table_info(ExamPapers)";
                using (var command = new SQLiteCommand(schemaQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    Console.WriteLine("\nExamPapers表结构:");
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}