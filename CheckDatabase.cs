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
            
            Console.WriteLine("=== 检查外键约束状态 ===");
            using var pragmaCommand = new SqliteCommand("PRAGMA foreign_keys", connection);
            var foreignKeysEnabled = pragmaCommand.ExecuteScalar();
            Console.WriteLine($"外键约束状态: {(foreignKeysEnabled.ToString() == "1" ? "启用" : "禁用")}");
            
            Console.WriteLine("\n=== 检查Users表数据 ===");
            using var userCommand = new SqliteCommand("SELECT UserId, Username, Email, Role, IsActive FROM Users ORDER BY UserId", connection);
            using var userReader = userCommand.ExecuteReader();
            
            if (!userReader.HasRows)
            {
                Console.WriteLine("Users表中没有数据！");
            }
            else
            {
                Console.WriteLine("UserId\tUsername\tEmail\t\tRole\tIsActive");
                Console.WriteLine("------\t--------\t-----\t\t----\t--------");
                while (userReader.Read())
                {
                    Console.WriteLine($"{userReader["UserId"]}\t{userReader["Username"]}\t{userReader["Email"]}\t{userReader["Role"]}\t{userReader["IsActive"]}");
                }
            }
            
            Console.WriteLine("\n=== 检查QuestionBanks表数据 ===");
            using var bankCommand = new SqliteCommand("SELECT BankId, Name, CreatorId, IsActive FROM QuestionBanks ORDER BY BankId", connection);
            using var bankReader = bankCommand.ExecuteReader();
            
            if (!bankReader.HasRows)
            {
                Console.WriteLine("QuestionBanks表中没有数据！");
            }
            else
            {
                Console.WriteLine("BankId\tName\t\tCreatorId\tIsActive");
                Console.WriteLine("------\t----\t\t---------\t--------");
                while (bankReader.Read())
                {
                    Console.WriteLine($"{bankReader["BankId"]}\t{bankReader["Name"]}\t\t{bankReader["CreatorId"]}\t\t{bankReader["IsActive"]}");
                }
            }
            
            Console.WriteLine("\n=== 测试外键约束 ===");
            // 尝试插入一个无效的CreatorId
            try
            {
                using var testCommand = new SqliteCommand(@"
                    INSERT INTO QuestionBanks (Name, Description, CreatorId, IsActive, CreatedAt, UpdatedAt) 
                    VALUES ('测试题库', '测试描述', 999, 1, datetime('now'), datetime('now'))", connection);
                testCommand.ExecuteNonQuery();
                Console.WriteLine("警告：外键约束未生效！成功插入了无效的CreatorId");
            }
            catch (SqliteException ex)
            {
                Console.WriteLine($"外键约束正常工作：{ex.Message}");
            }
            
            Console.WriteLine("\n=== 检查数据库完整性 ===");
            using var integrityCommand = new SqliteCommand("PRAGMA integrity_check", connection);
            var integrityResult = integrityCommand.ExecuteScalar();
            Console.WriteLine($"数据库完整性检查: {integrityResult}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}