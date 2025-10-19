using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string connectionString = "Data Source=ExamSystem.WPF/exam_system.db";
        
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            
            Console.WriteLine("=== 检查用户表 ===");
            using (var cmd = new SqliteCommand("SELECT UserId, Username, RealName, Role FROM Users", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"UserId: {reader["UserId"]}, Username: {reader["Username"]}, RealName: {reader["RealName"]}, Role: {reader["Role"]}");
                }
            }
            
            Console.WriteLine("\n=== 检查题库表 ===");
            using (var cmd = new SqliteCommand("SELECT BankId, Name, CreatorId FROM QuestionBanks", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"BankId: {reader["BankId"]}, Name: {reader["Name"]}, CreatorId: {reader["CreatorId"]}");
                }
            }
            
            Console.WriteLine("\n=== 检查外键约束 ===");
            using (var cmd = new SqliteCommand("PRAGMA foreign_keys", connection))
            {
                var result = cmd.ExecuteScalar();
                Console.WriteLine($"外键约束状态: {result}");
            }
            
            Console.WriteLine("\n=== 检查表结构 ===");
            using (var cmd = new SqliteCommand("PRAGMA table_info(QuestionBanks)", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"Column: {reader["name"]}, Type: {reader["type"]}, NotNull: {reader["notnull"]}, Default: {reader["dflt_value"]}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }
}