using System;
using System.IO;
using Microsoft.Data.Sqlite;

class ExecuteSql
{
    static void Main()
    {
        string connectionString = "Data Source=ExamSystem.WPF\\exam_system.db";
        string sqlFile = "AddIsPublishedColumn.sql";
        
        try
        {
            if (!File.Exists(sqlFile))
            {
                Console.WriteLine($"SQL文件不存在: {sqlFile}");
                return;
            }
            
            string sql = File.ReadAllText(sqlFile);
            string[] commands = sql.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            
            Console.WriteLine("开始执行SQL脚本...");
            
            foreach (string commandText in commands)
            {
                string trimmedCommand = commandText.Trim();
                if (string.IsNullOrEmpty(trimmedCommand) || trimmedCommand.StartsWith("--"))
                    continue;
                
                Console.WriteLine($"执行: {trimmedCommand.Substring(0, Math.Min(50, trimmedCommand.Length))}...");
                
                try
                {
                    var command = new SqliteCommand(trimmedCommand, connection);
                    
                    if (trimmedCommand.ToUpper().StartsWith("SELECT"))
                    {
                        using var reader = command.ExecuteReader();
                        Console.WriteLine("查询结果:");
                        
                        // 打印列标题
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.Write($"{reader.GetName(i)}\t");
                        }
                        Console.WriteLine();
                        
                        // 打印数据
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Console.Write($"{reader.GetValue(i)}\t");
                            }
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"影响行数: {rowsAffected}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"执行命令时出错: {ex.Message}");
                }
            }
            
            Console.WriteLine("SQL脚本执行完成!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }
}