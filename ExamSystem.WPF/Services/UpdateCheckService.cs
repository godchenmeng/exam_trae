using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.Services
{
    /// <summary>
    /// 客户端更新检查服务：在应用启动后异步检查最新版本并提示用户。
    /// </summary>
    public class UpdateCheckService : IUpdateCheckService
    {
        private readonly ILogger<UpdateCheckService> _logger;

        public UpdateCheckService(ILogger<UpdateCheckService> logger)
        {
            _logger = logger;
        }

        public async Task CheckAndPromptAsync()
        {
            try
            {
                // 节流：默认每日检查一次；可通过环境变量 UPDATE_CHECK_INTERVAL_HOURS 覆盖
                var intervalHours = GetThrottleIntervalHours();
                var state = LoadState();
                if (state?.LastCheckUtc != null)
                {
                    var next = state.LastCheckUtc.Value.AddHours(intervalHours);
                    if (DateTime.UtcNow < next)
                    {
                        _logger.LogDebug("跳过更新检查：距离上次检查不足 {Hours} 小时", intervalHours);
                        return;
                    }
                }

                // 允许通过环境变量覆盖更新服务器地址
                var serverUrl = Environment.GetEnvironmentVariable("UPDATE_SERVER_URL") ?? "https://updates.example.com";
                var versionApi = serverUrl.TrimEnd('/') + "/api/version"; // 服务器返回 latest/minCompatibleClient/notes

                using var client = new HttpClient(new HttpClientHandler
                {
                    // 可根据需要定制证书策略/代理等
                })
                {
                    Timeout = TimeSpan.FromSeconds(8)
                };

                var resp = await client.GetAsync(versionApi);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();

                var info = JsonSerializer.Deserialize<VersionInfo>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (info == null || string.IsNullOrWhiteSpace(info.Latest))
                {
                    _logger.LogDebug("版本检查返回为空或缺少 latest 字段，忽略。");
                    return;
                }

                var localVersion = GetLocalVersion();
                var latestVersion = ParseVersionSafe(info.Latest);

                if (latestVersion == null || localVersion == null)
                {
                    _logger.LogDebug("版本号解析失败 local={Local} latest={Latest}", localVersion, latestVersion);
                    return;
                }

                if (latestVersion > localVersion)
                {
                    _logger.LogInformation("发现新版本：{Latest}（本地：{Local}）", info.Latest, localVersion);
                    // 如果该版本被忽略，则跳过提示
                    if (string.Equals(state?.IgnoredVersion, info.Latest, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("该版本已被用户忽略：{Latest}，跳过提示。", info.Latest);
                        SaveState(new UpdateCheckState { LastCheckUtc = DateTime.UtcNow, LastPromptUtc = state?.LastPromptUtc, IgnoredVersion = state?.IgnoredVersion });
                        return;
                    }
                    // 在 UI 线程弹出提示
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        var dlg = new ExamSystem.WPF.Dialogs.UpdatePromptDialog
                        {
                            Owner = Application.Current?.MainWindow,
                            LatestVersion = info.Latest,
                            CurrentVersion = localVersion.ToString(),
                            Notes = string.IsNullOrWhiteSpace(info.Notes) ? "" : ("更新说明：\n" + info.Notes)
                        };
                        var ok = dlg.ShowDialog();
                        var choice = ok == true ? dlg.Choice : ExamSystem.WPF.Dialogs.UpdatePromptDialog.PromptChoice.RemindLater;

                        // 优先使用服务器返回的下载地址，否则打开默认更新页面
                        var url = !string.IsNullOrWhiteSpace(info.DownloadUrl)
                            ? info.DownloadUrl
                            : (serverUrl.TrimEnd('/') + "/downloads/client");

                        switch (choice)
                        {
                            case ExamSystem.WPF.Dialogs.UpdatePromptDialog.PromptChoice.UpdateNow:
                                {
                                    var isZip = url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || url.Contains("type=zip", StringComparison.OrdinalIgnoreCase);
                                    var appDir = AppContext.BaseDirectory;
                                    var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                                    var updaterPath = System.IO.Path.Combine(appDir, "Updater.exe");

                                    if (isZip && System.IO.File.Exists(updaterPath))
                                    {
                                        try
                                        {
                                            var psi = new ProcessStartInfo
                                            {
                                                FileName = updaterPath,
                                                UseShellExecute = true,
                                                Verb = "runas",
                                                Arguments = $"--packageUrl \"{url}\" --appDir \"{appDir}\" --processId {Process.GetCurrentProcess().Id} --launchAfter \"{exePath}\" --expectedVersion \"{info.Latest}\""
                                            };
                                            Process.Start(psi);
                                            Application.Current?.Dispatcher?.BeginInvoke(new Action(() => Application.Current?.Shutdown()));
                                            SaveState(new UpdateCheckState { LastCheckUtc = DateTime.UtcNow, LastPromptUtc = DateTime.UtcNow, IgnoredVersion = state?.IgnoredVersion });
                                            return;
                                        }
                                        catch (Exception startUpdaterEx)
                                        {
                                            _logger.LogWarning(startUpdaterEx, "启动 Updater 失败，回退到打开下载页面");
                                        }
                                    }
                                    try
                                    {
                                        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                                    }
                                    catch (Exception openEx)
                                    {
                                        _logger.LogWarning(openEx, "打开更新页面失败：{Url}", url);
                                    }
                                    SaveState(new UpdateCheckState { LastCheckUtc = DateTime.UtcNow, LastPromptUtc = DateTime.UtcNow, IgnoredVersion = state?.IgnoredVersion });
                                    break;
                                }
                            case ExamSystem.WPF.Dialogs.UpdatePromptDialog.PromptChoice.IgnoreThisVersion:
                                {
                                    SaveState(new UpdateCheckState { LastCheckUtc = DateTime.UtcNow, LastPromptUtc = DateTime.UtcNow, IgnoredVersion = info.Latest });
                                    _logger.LogInformation("用户选择忽略版本：{Latest}", info.Latest);
                                    break;
                                }
                            case ExamSystem.WPF.Dialogs.UpdatePromptDialog.PromptChoice.RemindLater:
                            default:
                                {
                                    SaveState(new UpdateCheckState { LastCheckUtc = DateTime.UtcNow, LastPromptUtc = DateTime.UtcNow, IgnoredVersion = state?.IgnoredVersion });
                                    _logger.LogInformation("用户选择稍后提醒：{Latest}", info.Latest);
                                    break;
                                }
                        }
                    });
                }

                // 成功或失败都记录检查时间，避免频繁请求；同时保留上次提示时间与忽略版本信息
                SaveState(new UpdateCheckState { LastCheckUtc = DateTime.UtcNow, LastPromptUtc = state?.LastPromptUtc, IgnoredVersion = state?.IgnoredVersion });
            }
            catch (Exception ex)
            {
                // 静默处理更新检查异常，避免影响启动流程
                _logger.LogDebug(ex, "启动更新检查失败（已忽略）");
                try { var existing = LoadState(); SaveState(new UpdateCheckState { LastCheckUtc = DateTime.UtcNow, LastPromptUtc = existing?.LastPromptUtc, IgnoredVersion = existing?.IgnoredVersion }); } catch { }
            }
        }

        private static Version? GetLocalVersion()
        {
            try
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                return asm?.GetName().Version;
            }
            catch
            {
                return null;
            }
        }

        private static Version? ParseVersionSafe(string v)
        {
            try
            {
                return new Version(v);
            }
            catch
            {
                return null;
            }
        }

        private class VersionInfo
        {
            public string? Latest { get; set; }
            public string? MinCompatibleClient { get; set; }
            public string? Notes { get; set; }
            public string? DownloadUrl { get; set; }
        }

        private class UpdateCheckState
        {
            public DateTime? LastCheckUtc { get; set; }
            public DateTime? LastPromptUtc { get; set; }
            public string? IgnoredVersion { get; set; }
        }

        private static string GetStatePath()
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ExamSystem");
            try { System.IO.Directory.CreateDirectory(dir); } catch { }
            return System.IO.Path.Combine(dir, "update-state.json");
        }

        private static UpdateCheckState? LoadState()
        {
            try
            {
                var path = GetStatePath();
                if (!System.IO.File.Exists(path)) return null;
                var json = System.IO.File.ReadAllText(path);
                return JsonSerializer.Deserialize<UpdateCheckState>(json);
            }
            catch { return null; }
        }

        private static void SaveState(UpdateCheckState state)
        {
            try
            {
                var path = GetStatePath();
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(path, json);
            }
            catch { }
        }

        private static int GetThrottleIntervalHours()
        {
            var defaultHours = 24;
            try
            {
                var env = Environment.GetEnvironmentVariable("UPDATE_CHECK_INTERVAL_HOURS");
                if (!string.IsNullOrWhiteSpace(env) && int.TryParse(env, out var h) && h >= 1 && h <= 168)
                {
                    return h;
                }
            }
            catch { }
            return defaultHours;
        }
    }
}