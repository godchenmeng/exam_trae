using System;
using Microsoft.Data.Sqlite;

class CheckDbSchema
{
    static void Main()
    {
        string connectionString = "Data Source=ExamSystem.WPF\\exam_system.db";
        
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            
            Console.WriteLine("=== 检查 ExamPapers 表结构 ===");
            
            // 检查表结构
            var command = new SqliteCommand("PRAGMA table_info(ExamPapers)", connection);
            using var reader = command.ExecuteReader();
            
            Console.WriteLine("列名\t\t类型\t\t非空\t默认值");
            Console.WriteLine("----------------------------------------");
            
            while (reader.Read())
            {
                string name = reader.GetString("name");
                string type = reader.GetString("type");
                bool notNull = reader.GetBoolean("notnull");
                var defaultValue = reader.IsDBNull("dflt_value") ? "NULL" : reader.GetString("dflt_value");
                
                Console.WriteLine($"{name}\t\t{type}\t\t{notNull}\t{defaultValue}");
            }
            
            Console.WriteLine("\n=== 检查 ExamPapers 表数据 ===");
            
            // 检查表数据
            var dataCommand = new SqliteCommand("SELECT PaperId, Name, Status FROM ExamPapers LIMIT 5", connection);
            using var dataReader = dataCommand.ExecuteReader();
            
            Console.WriteLine("PaperId\tName\t\tStatus");
            Console.WriteLine("--------------------------------");
            
            while (dataReader.Read())
            {
                int paperId = dataReader.GetInt32("PaperId");
                string name = dataReader.GetString("Name");
                string status = dataReader.IsDBNull("Status") ? "NULL" : dataReader.GetString("Status");
                
                Console.WriteLine($"{paperId}\t{name}\t\t{status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }
}