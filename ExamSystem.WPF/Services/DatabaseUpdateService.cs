using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace ExamSystem.WPF.Services
{
  /// <summary>
  /// 远程更新服务入口：统一通过 Updater.exe 执行文件更新与数据库更新。
  /// </summary>
  public class DatabaseUpdateService : IDatabaseUpdateService
  {
    private readonly ILogger<DatabaseUpdateService> _logger;

    public DatabaseUpdateService(ILogger<DatabaseUpdateService> logger)
    {
      _logger = logger;
    }

    public async Task FetchAndApplyAsync()
    {
      try
      {
        var serverUrl = Environment.GetEnvironmentVariable("UPDATE_SERVER_URL") ?? "https://updates.example.com";
        var version = GetLocalVersion()?.ToString() ?? "unknown";
        var updatesUrl = serverUrl.TrimEnd('/') + $"/api/updates?clientVersion={Uri.EscapeDataString(version)}&db=sqlite";

        // 先快速探测是否存在补丁，避免每次都启动 Updater
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        _logger.LogDebug("检查统一补丁接口：{Url}", updatesUrl);
        var resp = await http.GetAsync(updatesUrl);
        if (!resp.IsSuccessStatusCode)
        {
          _logger.LogDebug("补丁接口返回非成功状态：{Status}", resp.StatusCode);
          return;
        }
        var json = await resp.Content.ReadAsStringAsync();
        var updates = JsonSerializer.Deserialize<UpdatesResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (updates == null || updates.Patches == null || updates.Patches.Count == 0)
        {
          _logger.LogDebug("无可用补丁，跳过。");
          return;
        }

        var appDir = AppContext.BaseDirectory;
        var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        var updaterPath = Path.Combine(appDir, "Updater.exe");
        if (!File.Exists(updaterPath))
        {
          _logger.LogWarning("未找到 Updater.exe，无法执行统一更新。");
          return;
        }

        // 解析可能的数据库路径并传递给 Updater（若存在）
        var dbPath1 = Path.Combine(appDir, "exam_system.db");
        var dbPath2 = Path.Combine(appDir, "ExamSystem.WPF", "exam_system.db");
        var dbPath = File.Exists(dbPath1) ? dbPath1 : (File.Exists(dbPath2) ? dbPath2 : string.Empty);

        try
        {
          _logger.LogInformation("发现补丁，启动 Updater 统一处理（文件+数据库）...");
          var args = $"--updatesUrl \"{updatesUrl}\" --appDir \"{appDir}\" --processId {Process.GetCurrentProcess().Id} --launchAfter \"{exePath}\"";
          if (!string.IsNullOrWhiteSpace(dbPath))
          {
            args += $" --dbPath \"{dbPath}\"";
          }
          var psi = new ProcessStartInfo
          {
            FileName = updaterPath,
            UseShellExecute = true,
            Verb = "runas",
            Arguments = args
          };
          Process.Start(psi);
          // 启动 Updater 后，优雅关闭当前应用程序
          Application.Current?.Dispatcher?.BeginInvoke(new Action(() => Application.Current?.Shutdown()));
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "启动 Updater 失败，统一更新未执行");
        }
      }
      catch (Exception ex)
      {
        _logger.LogDebug(ex, "远程统一更新检查失败（忽略，不影响主流程）");
      }
    }

    private static Version? GetLocalVersion()
    {
      try
      {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return asm?.GetName().Version;
      }
      catch { return null; }
    }

    // 保留解析结构用的类型（仅用于探测是否存在补丁）

    private class UpdatesResponse
    {
      public List<Patch>? Patches { get; set; }
    }

    private class Patch
    {
      public string? Id { get; set; }
      public string? Type { get; set; } // db_sql or file_zip
      public string Sql { get; set; } = string.Empty; // for db_sql
      public string? Url { get; set; } // for file_zip
      public string? ExpectedVersion { get; set; } // optional
      public string? Sha256 { get; set; } // optional
    }

    // 统一更新改造后，本地不再负责补丁状态持久化；由 Updater 维护
  }
}