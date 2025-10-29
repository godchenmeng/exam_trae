using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string connectionString = "Data Source=E:\\Project\\exam_trae\\ExamSystem.WPF\\bin\\Debug\\net8.0-windows10.0.19041\\exam_system.db";
        
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            
            Console.WriteLine("=== 检查 Questions 表结构 ===");
            CheckTableSchema(connection, "Questions");
            
            Console.WriteLine("\n=== 检查 AnswerRecords 表结构 ===");
            CheckTableSchema(connection, "AnswerRecords");
            
            Console.WriteLine("\n=== 检查需要的字段是否存在 ===");
            
            // 检查 Questions 表的地图绘制字段
            var questionsFields = new[]
            {
                "GuidanceOverlaysJson",
                "MapDrawingConfigJson", 
                "ReferenceOverlaysJson",
                "ReviewRubricJson",
                "ShowBuildingLayersJson",
                "TimeLimitSeconds"
            };
            
            foreach (var field in questionsFields)
            {
                bool exists = CheckColumnExists(connection, "Questions", field);
                Console.WriteLine($"Questions.{field}: {(exists ? "存在" : "缺失")}");
            }
            
            // 检查 AnswerRecords 表的字段
            var answerRecordsFields = new[]
            {
                "ClientInfoJson",
                "DrawDurationSeconds",
                "RubricScoresJson"
            };
            
            foreach (var field in answerRecordsFields)
            {
                bool exists = CheckColumnExists(connection, "AnswerRecords", field);
                Console.WriteLine($"AnswerRecords.{field}: {(exists ? "存在" : "缺失")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }
    
    static void CheckTableSchema(SqliteConnection connection, string tableName)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName})";
        
        using var reader = command.ExecuteReader();
        Console.WriteLine($"{tableName} 表字段:");
        while (reader.Read())
        {
            string name = reader.GetString(1);  // name column
            string type = reader.GetString(2);  // type column
            bool notNull = reader.GetBoolean(3); // notnull column
            string defaultValue = reader.IsDBNull(4) ? "NULL" : reader.GetString(4); // dflt_value column
            
            Console.WriteLine($"  {name} ({type}) {(notNull ? "NOT NULL" : "NULL")} DEFAULT {defaultValue}");
        }
    }
    
    static bool CheckColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName})";
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}