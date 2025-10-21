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
            
            Console.WriteLine("=== 检查试卷发布状态和时间窗口 ===");
            using (var cmd = new SqliteCommand(@"
                SELECT PaperId, Name, IsPublished, Status, StartTime, EndTime, AllowRetake, Duration 
                FROM ExamPapers 
                ORDER BY PaperId", connection))
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                {
                    Console.WriteLine("ExamPapers 表中没有数据！");
                }
                else
                {
                    Console.WriteLine("PaperId\tName\t\tIsPublished\tStatus\t\tStartTime\t\tEndTime\t\tAllowRetake\tDuration");
                    Console.WriteLine("-------\t----\t\t-----------\t------\t\t---------\t\t-------\t\t-----------\t--------");
                    
                    int publishedCount = 0;
                    var now = DateTime.Now;
                    
                    while (reader.Read())
                    {
                        var paperId = reader["PaperId"];
                        var name = reader["Name"]?.ToString() ?? "";
                        var isPublished = reader["IsPublished"].ToString() == "1" || reader["IsPublished"].ToString()?.ToLower() == "true";
                        var status = reader["Status"]?.ToString() ?? "";
                        var startTime = reader["StartTime"]?.ToString() ?? "";
                        var endTime = reader["EndTime"]?.ToString() ?? "";
                        var allowRetake = reader["AllowRetake"].ToString() == "1" || reader["AllowRetake"].ToString()?.ToLower() == "true";
                        var duration = reader["Duration"];
                        
                        if (isPublished || status == "已发布") publishedCount++;
                        
                        Console.WriteLine($"{paperId}\t{name.Substring(0, Math.Min(name.Length, 10))}\t{isPublished}\t\t{status}\t\t{startTime}\t{endTime}\t{allowRetake}\t\t{duration}");
                        
                        // 检查时间窗口
                        if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime, out var start))
                        {
                            if (now < start)
                            {
                                Console.WriteLine($"  -> 试卷 {paperId} 尚未开始 (开始时间: {start})");
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime, out var end))
                        {
                            if (now > end)
                            {
                                Console.WriteLine($"  -> 试卷 {paperId} 已结束 (结束时间: {end})");
                            }
                        }
                    }
                    Console.WriteLine($"\n已发布试卷数量: {publishedCount}");
                    Console.WriteLine($"当前时间: {now}");
                }
            }
            
            Console.WriteLine("\n=== 检查考试记录 ===");
            using (var cmd = new SqliteCommand(@"
                SELECT RecordId, UserId, PaperId, Status, StartTime, EndTime 
                FROM ExamRecords 
                ORDER BY RecordId DESC LIMIT 10", connection))
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                {
                    Console.WriteLine("ExamRecords 表中没有数据！");
                }
                else
                {
                    Console.WriteLine("RecordId\tUserId\tPaperId\tStatus\t\tStartTime\t\tEndTime");
                    Console.WriteLine("--------\t------\t-------\t------\t\t---------\t\t-------");
                    
                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["RecordId"]}\t\t{reader["UserId"]}\t{reader["PaperId"]}\t{reader["Status"]}\t{reader["StartTime"]}\t{reader["EndTime"]}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"数据库连接或查询失败: {ex.Message}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}