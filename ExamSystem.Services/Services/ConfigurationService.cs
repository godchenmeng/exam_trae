using System;
using System.IO;
using Microsoft.Data.Sqlite;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Services
{
  /// <summary>
  /// 读取/写入系统配置，并对敏感值进行加密，记录变更日志。
  /// </summary>
  public class ConfigurationService
  {
    private readonly string _dbPath;
    public ConfigurationService(string? dbPath = null)
    {
      _dbPath = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exam_system.db");
    }

    private SqliteConnection Open()
    {
      var conn = new SqliteConnection($"Data Source={_dbPath}");
      conn.Open();
      return conn;
    }

    public string? GetConfig(string key, bool decryptSensitive = true)
    {
      using var conn = Open();
      using var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT Value FROM SystemConfig WHERE Key = $key";
      cmd.Parameters.AddWithValue("$key", key);
      var result = cmd.ExecuteScalar()?.ToString();
      if (result == null) return null;
      if (decryptSensitive && IsSensitiveKey(key))
      {
        try { return EncryptionUtil.UnprotectString(result); } catch { return null; }
      }
      return result;
    }

    public void SetConfig(string key, string? value, string operatorName, bool encryptSensitive = true, string? detail = null)
    {
      value ??= string.Empty;
      string toStore = value;
      if (encryptSensitive && IsSensitiveKey(key))
      {
        toStore = EncryptionUtil.ProtectString(value);
      }

      using var conn = Open();
      using var tx = conn.BeginTransaction();

      // 读取旧值以记录日志
      string? oldVal = null;
      using (var readCmd = conn.CreateCommand())
      {
        readCmd.CommandText = "SELECT Value FROM SystemConfig WHERE Key = $key";
        readCmd.Parameters.AddWithValue("$key", key);
        oldVal = readCmd.ExecuteScalar()?.ToString();
      }

      using (var upsertCmd = conn.CreateCommand())
      {
        var ts = DateTime.UtcNow.ToString("o"); // 统一使用 ISO 8601 文本时间
        upsertCmd.CommandText = @"INSERT INTO SystemConfig(Key, Value, UpdatedAt, UpdatedBy)
VALUES($key, $val, $ts, $op)
ON CONFLICT(Key) DO UPDATE SET Value=$val, UpdatedAt=$ts, UpdatedBy=$op";
        upsertCmd.Parameters.AddWithValue("$key", key);
        upsertCmd.Parameters.AddWithValue("$val", toStore);
        upsertCmd.Parameters.AddWithValue("$ts", ts);
        upsertCmd.Parameters.AddWithValue("$op", operatorName);
        upsertCmd.ExecuteNonQuery();
      }

      using (var logCmd = conn.CreateCommand())
      {
        logCmd.CommandText = @"INSERT INTO SystemConfigLog(Key, OldValueHash, NewValueHash, Operator, ChangedAt, Detail)
VALUES($key, $oldHash, $newHash, $op, $ts, $detail)";
        var oldPlain = oldVal;
        if (!string.IsNullOrEmpty(oldVal) && IsSensitiveKey(key))
        {
          try { oldPlain = EncryptionUtil.UnprotectString(oldVal); } catch { }
        }
        var newPlain = value;
        var tsLog = DateTime.UtcNow.ToString("o");
        logCmd.Parameters.AddWithValue("$key", key);
        // 注意：AddWithValue 遇到 null 无法推断类型，会抛出 "Value must be set"，因此显式传入 DBNull.Value
        var pOld = logCmd.CreateParameter();
        pOld.ParameterName = "$oldHash";
        pOld.Value = oldPlain != null ? EncryptionUtil.Sha256Hash(oldPlain) : (object)DBNull.Value;
        logCmd.Parameters.Add(pOld);
        var pNew = logCmd.CreateParameter();
        pNew.ParameterName = "$newHash";
        pNew.Value = newPlain != null ? EncryptionUtil.Sha256Hash(newPlain) : (object)DBNull.Value;
        logCmd.Parameters.Add(pNew);
        logCmd.Parameters.AddWithValue("$op", operatorName);
        logCmd.Parameters.AddWithValue("$ts", tsLog);
        logCmd.Parameters.AddWithValue("$detail", detail ?? "");
        logCmd.ExecuteNonQuery();
      }

      tx.Commit();
    }

    public static bool IsSensitiveKey(string key)
    {
      return string.Equals(key, Keys.BaiduMapAk, StringComparison.OrdinalIgnoreCase);
    }

    public static class Keys
    {
      public const string BaiduMapAk = "BAIDU_MAP_AK";
      public const string BackupIntervalDays = "BackupIntervalDays";
      public const string BackupDir = "BackupDir";
    }
  }
}