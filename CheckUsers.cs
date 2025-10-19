using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string connectionString = @"Data Source=E:\Project\exam_trae\ExamSystem.WPF\exam_system.db";
        
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            
            Console.WriteLine("=== 检查Users表中的数据 ===");
            using var command = new SqliteCommand("SELECT UserId, Username, Email, Role FROM Users ORDER BY UserId", connection);
            using var reader = command.ExecuteReader();
            
            if (!reader.HasRows)
            {
                Console.WriteLine("Users表中没有数据！");
            }
            else
            {
                Console.WriteLine("UserId\tUsername\tEmail\t\tRole");
                Console.WriteLine("------\t--------\t-----\t\t----");
                while (reader.Read())
                {
                    Console.WriteLine($"{reader["UserId"]}\t{reader["Username"]}\t{reader["Email"]}\t{reader["Role"]}");
                }
            }
            
            Console.WriteLine("\n=== 检查外键约束是否启用 ===");
            using var pragmaCommand = new SqliteCommand("PRAGMA foreign_keys", connection);
            var foreignKeysEnabled = pragmaCommand.ExecuteScalar();
            Console.WriteLine($"外键约束状态: {(foreignKeysEnabled.ToString() == "1" ? "启用" : "禁用")}");
            
            Console.WriteLine("\n=== 检查QuestionBanks表结构 ===");
            using var schemaCommand = new SqliteCommand("PRAGMA table_info(QuestionBanks)", connection);
            using var schemaReader = schemaCommand.ExecuteReader();
            
            Console.WriteLine("列名\t\t类型\t\t非空\t默认值\t主键");
            Console.WriteLine("----\t\t----\t\t----\t------\t----");
            while (schemaReader.Read())
            {
                Console.WriteLine($"{schemaReader["name"]}\t\t{schemaReader["type"]}\t\t{schemaReader["notnull"]}\t{schemaReader["dflt_value"]}\t{schemaReader["pk"]}");
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