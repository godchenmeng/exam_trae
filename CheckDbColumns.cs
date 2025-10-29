using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string connectionString = "Data Source=ExamSystem.WPF\\bin\\Debug\\net8.0-windows10.0.19041\\exam_system.db";
        
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        Console.WriteLine("=== 检查Questions表结构 ===");
        CheckTableColumns(connection, "Questions");
        
        Console.WriteLine("\n=== 检查AnswerRecords表结构 ===");
        CheckTableColumns(connection, "AnswerRecords");
        
        Console.WriteLine("\n=== 检查特定字段是否存在 ===");
        CheckSpecificColumns(connection);
    }
    
    static void CheckTableColumns(SqliteConnection connection, string tableName)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName})";
        
        Console.WriteLine($"{tableName}表的所有字段:");
        Console.WriteLine("序号\t字段名\t\t\t类型\t\t非空\t默认值\t主键");
        Console.WriteLine("----------------------------------------------------------------");
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            int cid = reader.GetInt32("cid");
            string name = reader.GetString("name");
            string type = reader.GetString("type");
            bool notNull = reader.GetBoolean("notnull");
            var defaultValue = reader.IsDBNull("dflt_value") ? "NULL" : reader.GetString("dflt_value");
            bool pk = reader.GetBoolean("pk");
            
            Console.WriteLine($"{cid}\t{name.PadRight(20)}\t{type.PadRight(10)}\t{notNull}\t{defaultValue}\t{pk}");
        }
    }
    
    static void CheckSpecificColumns(SqliteConnection connection)
    {
        string[] questionsColumns = {
            "GuidanceOverlaysJson", "MapDrawingConfigJson", "ReferenceOverlaysJson", 
            "ReviewRubricJson", "ShowBuildingLayersJson", "TimeLimitSeconds"
        };
        
        string[] answerRecordsColumns = {
            "ClientInfoJson", "DrawDurationSeconds", "RubricScoresJson"
        };
        
        Console.WriteLine("Questions表中的地图绘制相关字段:");
        foreach (var column in questionsColumns)
        {
            bool exists = CheckColumnExists(connection, "Questions", column);
            Console.WriteLine($"  {column}: {(exists ? "存在" : "不存在")}");
        }
        
        Console.WriteLine("\nAnswerRecords表中的地图绘制相关字段:");
        foreach (var column in answerRecordsColumns)
        {
            bool exists = CheckColumnExists(connection, "AnswerRecords", column);
            Console.WriteLine($"  {column}: {(exists ? "存在" : "不存在")}");
        }
    }
    
    static bool CheckColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = '{columnName}'";
        var count = (long)command.ExecuteScalar();
        return count > 0;
    }
}