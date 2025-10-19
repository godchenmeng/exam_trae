using System;
using System.Data;
using System.Data.Common;
using System.IO;

class Program
{
    static void Main()
    {
        string dbPath = @"ExamSystem.WPF\exam_system.db";
        
        if (!File.Exists(dbPath))
        {
            Console.WriteLine($"数据库文件不存在: {dbPath}");
            return;
        }
        
        Console.WriteLine($"数据库文件存在: {dbPath}");
        Console.WriteLine($"文件大小: {new FileInfo(dbPath).Length} bytes");
        Console.WriteLine($"最后修改时间: {File.GetLastWriteTime(dbPath)}");
        
        // 检查是否有其他进程在使用数据库
        try
        {
            using (var fs = File.Open(dbPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Console.WriteLine("数据库文件可以正常访问");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"数据库文件访问异常: {ex.Message}");
        }
        
        // 检查WAL和SHM文件
        string walPath = dbPath + "-wal";
        string shmPath = dbPath + "-shm";
        
        if (File.Exists(walPath))
        {
            Console.WriteLine($"WAL文件存在: {new FileInfo(walPath).Length} bytes");
        }
        
        if (File.Exists(shmPath))
        {
            Console.WriteLine($"SHM文件存在: {new FileInfo(shmPath).Length} bytes");
        }
    }
}