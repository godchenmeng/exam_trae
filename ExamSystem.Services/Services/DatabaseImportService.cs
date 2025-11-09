using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace ExamSystem.Services
{
  /// <summary>
  /// 数据库导入服务，执行SQL备份文件并显示进度。
  /// </summary>
  public class DatabaseImportService
  {
    private readonly string _dbPath;
    private readonly DatabaseBackupService _backupService;
    public DatabaseImportService(DatabaseBackupService backupService, string? dbPath = null)
    {
      _backupService = backupService;
      _dbPath = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exam_system.db");
    }

    private SqliteConnection Open()
    {
      var conn = new SqliteConnection($"Data Source={_dbPath}");
      conn.Open();
      return conn;
    }

    public async Task ImportAsync(string sqlFilePath, string operatorName, IProgress<int>? progress = null)
    {
      if (!File.Exists(sqlFilePath)) throw new FileNotFoundException("SQL文件不存在", sqlFilePath);
      var sqlText = await File.ReadAllTextAsync(sqlFilePath, Encoding.UTF8);

      // 简单分割语句：按分号拆分（不处理复杂情况），足够满足备份生成的格式
      var statements = Regex.Split(sqlText, @";\s*\r?\n", RegexOptions.Multiline);
      int total = statements.Length;
      int done = 0;

      // 预备份作为回滚点
      _backupService.BackupNow(operatorName, mode: "import_prebackup");

      using var conn = Open();
      using var tx = conn.BeginTransaction();
      try
      {
        foreach (var stmt in statements)
        {
          var sql = stmt.Trim();
          if (string.IsNullOrEmpty(sql)) { done++; progress?.Report(done * 100 / total); continue; }
          using var cmd = conn.CreateCommand();
          cmd.CommandText = sql;
          cmd.ExecuteNonQuery();
          done++;
          progress?.Report(done * 100 / total);
        }
        tx.Commit();
      }
      catch
      {
        try { tx.Rollback(); } catch { }
        throw;
      }
    }
  }
}