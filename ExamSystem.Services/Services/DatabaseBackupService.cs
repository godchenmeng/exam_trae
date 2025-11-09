using System;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Services
{
  /// <summary>
  /// 数据库备份服务，生成包含结构与数据的SQL文件。
  /// </summary>
  public class DatabaseBackupService
  {
    private readonly string _dbPath;
    private readonly string _backupDir;
    private readonly ConfigurationService _config;

    public DatabaseBackupService(ConfigurationService config, string? dbPath = null, string? backupDir = null)
    {
      _config = config;
      _dbPath = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exam_system.db");
      _backupDir = backupDir ?? _config.GetConfig(ConfigurationService.Keys.BackupDir, decryptSensitive: false) ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
      Directory.CreateDirectory(_backupDir);
    }

    private SqliteConnection Open()
    {
      var conn = new SqliteConnection($"Data Source={_dbPath}");
      conn.Open();
      return conn;
    }

    public string BackupNow(string operatorName, string mode = "manual")
    {
      var fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".sql";
      var fullPath = Path.Combine(_backupDir, fileName);
      try
      {
        using var conn = Open();
        var sb = new StringBuilder();
        sb.AppendLine("PRAGMA foreign_keys=OFF;");
        sb.AppendLine("BEGIN TRANSACTION;");

        // 导出表结构
        using (var cmdTables = conn.CreateCommand())
        {
          cmdTables.CommandText = "SELECT type, name, sql FROM sqlite_master WHERE type IN ('table','index','trigger') AND name NOT LIKE 'sqlite_%' ORDER BY type, name";
          using var reader = cmdTables.ExecuteReader();
          while (reader.Read())
          {
            var type = reader.GetString(0);
            var name = reader.GetString(1);
            var sql = reader.IsDBNull(2) ? null : reader.GetString(2);
            if (!string.IsNullOrEmpty(sql))
            {
              sb.AppendLine(sql + ";");
            }
          }
        }

        // 导出数据
        using (var cmdTblNames = conn.CreateCommand())
        {
          cmdTblNames.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
          using var tblReader = cmdTblNames.ExecuteReader();
          while (tblReader.Read())
          {
            var table = tblReader.GetString(0);
            using var cmdCols = conn.CreateCommand();
            cmdCols.CommandText = $"PRAGMA table_info('{table}')";
            using var colReader = cmdCols.ExecuteReader();
            var cols = new System.Collections.Generic.List<string>();
            while (colReader.Read())
            {
              cols.Add(colReader.GetString(1)); // name
            }
            var colList = string.Join(",", cols);

            using var cmdData = conn.CreateCommand();
            cmdData.CommandText = $"SELECT {colList} FROM '{table}'";
            using var dataReader = cmdData.ExecuteReader();
            while (dataReader.Read())
            {
              var values = new string[cols.Count];
              for (int i = 0; i < cols.Count; i++)
              {
                if (dataReader.IsDBNull(i)) values[i] = "NULL";
                else
                {
                  var val = dataReader.GetValue(i);
                  if (val is string || val is DateTime)
                  {
                    values[i] = "'" + val.ToString().Replace("'", "''") + "'";
                  }
                  else if (val is byte[] bytes)
                  {
                    values[i] = "x'" + BitConverter.ToString(bytes).Replace("-", string.Empty) + "'";
                  }
                  else
                  {
                    values[i] = val.ToString();
                  }
                }
              }
              sb.AppendLine($"INSERT INTO '{table}' ({colList}) VALUES ({string.Join(",", values)});");
            }
          }
        }

        sb.AppendLine("COMMIT;");
        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

        LogBackup(fileName, operatorName, mode, "success", $"Backup saved to {fullPath}");
        return fullPath;
      }
      catch (Exception ex)
      {
        LogBackup(fileName, operatorName, mode, "failed", ex.Message);
        throw;
      }
    }

    private void LogBackup(string fileName, string operatorName, string mode, string status, string message)
    {
      using var conn = Open();
      using var cmd = conn.CreateCommand();
      cmd.CommandText = @"INSERT INTO BackupLog(FileName, Mode, CreatedAt, Operator, Status, Message)
VALUES($file, $mode, $ts, $op, $status, $msg)";
      cmd.Parameters.AddWithValue("$file", fileName);
      cmd.Parameters.AddWithValue("$mode", mode);
      cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow);
      cmd.Parameters.AddWithValue("$op", operatorName);
      cmd.Parameters.AddWithValue("$status", status);
      cmd.Parameters.AddWithValue("$msg", message);
      cmd.ExecuteNonQuery();
    }
  }
}