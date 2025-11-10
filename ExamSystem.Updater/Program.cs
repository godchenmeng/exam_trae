using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Serilog;

namespace ExamSystem.Updater
{
    internal class Program
    {
        // Args:
        //   --packageUrl <url>                 (optional) ZIP文件更新包地址
        //   --updatesUrl <url>                 (optional) 统一补丁接口地址，返回 patches JSON（db_sql, file_zip）
        //   --appDir <dir>                     应用目录（拷贝文件目标）
        //   --processId <pid>                  需要等待退出的进程ID
        //   --launchAfter <exePath>            更新完成后重新启动的客户端exe路径
        //   --expectedVersion <v>              （可选）期望版本号，仅用于日志记录或校验
        //   --sha256 <hex>                     （可选）ZIP包的SHA256校验
        //   --dbPath <file>                    （可选）SQLite数据库文件路径；未提供时按约定路径推断
        //   --statePath <file>                 （可选）补丁状态文件路径；未提供时使用默认路径
        static async Task<int> Main(string[] args)
        {
            var opts = Options.Parse(args);
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ExamSystem", "Logs", "updates");
            Directory.CreateDirectory(logDir);
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(logDir, $"updater_{DateTime.Now:yyyyMMdd_HHmmss}.log"))
                .CreateLogger();

            try
            {
                ValidateOptions(opts);

                Log.Information("Updater started with options: {@opts}", opts);

                // 1) Wait for target process to exit
                if (opts.ProcessId > 0)
                {
                    try
                    {
                        var proc = Process.GetProcessById(opts.ProcessId);
                        Log.Information("Waiting for process {Pid} to exit...", opts.ProcessId);
                        try { proc.WaitForExit(60_000); } catch { }
                        // if still running, poll for a bit longer
                        var maxWait = DateTime.UtcNow.AddMinutes(3);
                        while (DateTime.UtcNow < maxWait)
                        {
                            bool alive = true;
                            try { alive = !proc.HasExited; } catch { alive = false; }
                            if (!alive) break;
                            await Task.Delay(1000);
                        }
                    }
                    catch (Exception)
                    {
                        Log.Warning("Target process {Pid} not found or already exited.", opts.ProcessId);
                    }
                }

                var appDir = opts.AppDir!;
                var tempRoot = Path.Combine(Path.GetTempPath(), "ExamSystem", "update");
                Directory.CreateDirectory(tempRoot);

                // 2) 执行文件更新（若提供 packageUrl 或 updatesUrl 中包含 file_zip）
                if (!string.IsNullOrWhiteSpace(opts.PackageUrl))
                {
                    await ApplyZipPackageAsync(opts.PackageUrl!, tempRoot, appDir, opts.Sha256);
                }

                // 3) 从统一接口获取补丁并处理（file_zip/db_sql）
                if (!string.IsNullOrWhiteSpace(opts.UpdatesUrl))
                {
                    await FetchAndApplyPatchesAsync(opts.UpdatesUrl!, appDir, tempRoot, opts);
                }

                // 6) Relaunch client
                if (!string.IsNullOrWhiteSpace(opts.LaunchAfter))
                {
                    Log.Information("Launching client: {LaunchAfter}", opts.LaunchAfter);
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = opts.LaunchAfter,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to relaunch client");
                    }
                }

                // 7) Cleanup old backups optionally (keep last few)
                Log.Information("Update finished successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Update failed. Attempting rollback.");
                try
                {
                    // find latest backup and restore
                    var appDir = opts.AppDir ?? AppContext.BaseDirectory;
                    var backups = Directory.GetDirectories(appDir, "backup_*");
                    Array.Sort(backups);
                    if (backups.Length > 0)
                    {
                        var latest = backups[^1];
                        Log.Information("Restoring backup from {Latest}", latest);
                        CopyDirectory(latest, appDir, overwrite: true);
                    }
                }
                catch (Exception rEx)
                {
                    Log.Error(rEx, "Rollback failed");
                }
                return 1;
            }
            finally
            {
                try { Log.CloseAndFlush(); } catch { }
            }
        }

        private static void ValidateOptions(Options o)
        {
            if (string.IsNullOrWhiteSpace(o.AppDir)) throw new ArgumentException("--appDir is required");
            if (string.IsNullOrWhiteSpace(o.PackageUrl) && string.IsNullOrWhiteSpace(o.UpdatesUrl))
            {
                throw new ArgumentException("Either --packageUrl or --updatesUrl must be provided");
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir, bool overwrite = false)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(file);
                var target = Path.Combine(destDir, name);
                File.Copy(file, target, overwrite);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(dir);
                var target = Path.Combine(destDir, name);
                CopyDirectory(dir, target, overwrite);
            }
        }

        private record Options(string? PackageUrl, string? UpdatesUrl, string? AppDir, int ProcessId, string? LaunchAfter, string? ExpectedVersion, string? Sha256, string? DbPath, string? StatePath)
        {
            public static Options Parse(string[] args)
            {
                string? packageUrl = null, updatesUrl = null, appDir = null, launchAfter = null, expectedVersion = null, sha256 = null, dbPath = null, statePath = null;
                int pid = 0;
                for (int i = 0; i < args.Length; i++)
                {
                    var a = args[i];
                    string? next() => (i + 1 < args.Length) ? args[++i] : null;
                    switch (a)
                    {
                        case "--packageUrl": packageUrl = next(); break;
                        case "--updatesUrl": updatesUrl = next(); break;
                        case "--appDir": appDir = next(); break;
                        case "--processId": int.TryParse(next(), out pid); break;
                        case "--launchAfter": launchAfter = next(); break;
                        case "--expectedVersion": expectedVersion = next(); break;
                        case "--sha256": sha256 = next(); break;
                        case "--dbPath": dbPath = next(); break;
                        case "--statePath": statePath = next(); break;
                        default: break;
                    }
                }
                return new Options(packageUrl, updatesUrl, appDir, pid, launchAfter, expectedVersion, sha256, dbPath, statePath);
            }
        }

        private static async Task ApplyZipPackageAsync(string packageUrl, string tempRoot, string appDir, string? sha256)
        {
            var packagePath = Path.Combine(tempRoot, "package.zip");
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
            {
                Log.Information("Downloading package from {Url}...", packageUrl);
                var data = await client.GetByteArrayAsync(packageUrl);
                await File.WriteAllBytesAsync(packagePath, data);
            }

            if (!File.Exists(packagePath)) throw new InvalidOperationException("Package download failed.");

            if (!string.IsNullOrWhiteSpace(sha256))
            {
                try
                {
                    using var fs = File.OpenRead(packagePath);
                    using var sha = System.Security.Cryptography.SHA256.Create();
                    var h = sha.ComputeHash(fs);
                    var hex = BitConverter.ToString(h).Replace("-", string.Empty).ToLowerInvariant();
                    if (!string.Equals(hex, sha256!.ToLowerInvariant()))
                    {
                        throw new InvalidOperationException($"SHA256 mismatch. expected={sha256} actual={hex}");
                    }
                    Log.Information("SHA256 verified: {Hex}", hex);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "SHA256 verification failed");
                    throw;
                }
            }

            var extractDir = Path.Combine(tempRoot, "extracted");
            if (Directory.Exists(extractDir))
            {
                try { Directory.Delete(extractDir, true); } catch { }
            }
            Directory.CreateDirectory(extractDir);
            Log.Information("Extracting ZIP to {Dir}", extractDir);
            ZipFile.ExtractToDirectory(packagePath, extractDir);

            var backupDir = Path.Combine(appDir, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}");
            Log.Information("Backing up {AppDir} -> {BackupDir}", appDir, backupDir);
            CopyDirectory(appDir, backupDir);

            Log.Information("Copying extracted files to app dir {AppDir}", appDir);
            CopyDirectory(extractDir, appDir, overwrite: true);
        }

        private static async Task FetchAndApplyPatchesAsync(string updatesUrl, string appDir, string tempRoot, Options opts)
        {
            Log.Information("Fetching patches from {UpdatesUrl}...", updatesUrl);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var resp = await client.GetAsync(updatesUrl);
            if (!resp.IsSuccessStatusCode)
            {
                Log.Warning("Updates API returned {Status}", resp.StatusCode);
                return;
            }
            var json = await resp.Content.ReadAsStringAsync();
            var updates = JsonSerializer.Deserialize<UpdatesResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (updates == null || updates.Patches == null || updates.Patches.Count == 0)
            {
                Log.Information("No patches returned.");
                return;
            }

            var statePath = GetStatePath(opts.StatePath);
            var state = LoadState(statePath);
            var seedApplied = state?.AppliedPatchIds as IEnumerable<string> ?? Array.Empty<string>();
            var applied = new HashSet<string>(seedApplied, StringComparer.OrdinalIgnoreCase);

            // 先处理 file_zip（若未提供 --packageUrl，则从补丁中下载并应用）
            foreach (var p in updates.Patches)
            {
                if (!string.Equals(p.Type, "file_zip", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(p.Url)) continue;
                if (!string.IsNullOrWhiteSpace(opts.PackageUrl))
                {
                    Log.Information("PackageUrl provided; skipping file_zip patch {Id} from updates.", p.Id);
                    continue;
                }
                Log.Information("Applying file ZIP patch {Id} from updates.", p.Id);
                await ApplyZipPackageAsync(p.Url!, tempRoot, appDir, p.Sha256);
            }

            // 再处理 db_sql
            var dbPath = ResolveDbPath(appDir, opts.DbPath);
            if (!File.Exists(dbPath))
            {
                Log.Warning("SQLite database not found at {DbPath}. Skipping DB patches.", dbPath);
                return;
            }

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            await conn.OpenAsync();

            foreach (var p in updates.Patches)
            {
                if (!string.Equals(p.Type, "db_sql", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.Sql)) continue;
                if (applied.Contains(p.Id))
                {
                    Log.Information("DB patch already applied: {Id}", p.Id);
                    continue;
                }

                Log.Information("Applying DB patch: {Id}", p.Id);
                using var tx = conn.BeginTransaction();
                try
                {
                    foreach (var stmt in SplitStatements(p.Sql))
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.Transaction = tx;
                        cmd.CommandText = stmt;
                        await cmd.ExecuteNonQueryAsync();
                    }
                    tx.Commit();
                    applied.Add(p.Id);
                    SaveState(statePath, new DbUpdateState { LastCheckUtc = DateTime.UtcNow, AppliedPatchIds = new List<string>(applied) });
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to apply DB patch {Id}. Rolled back.", p.Id);
                    try { tx.Rollback(); } catch { }
                }
            }
        }

        private static string ResolveDbPath(string appDir, string? provided)
        {
            if (!string.IsNullOrWhiteSpace(provided)) return provided!;
            var p1 = Path.Combine(appDir, "exam_system.db");
            if (File.Exists(p1)) return p1;
            var p2 = Path.Combine(appDir, "ExamSystem.WPF", "exam_system.db");
            return p2; // may or may not exist; caller will check
        }

        private static List<string> SplitStatements(string sql)
        {
            var list = new List<string>();
            using var reader = new StringReader(sql);
            var buf = new System.Text.StringBuilder();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                buf.AppendLine(line);
                if (line.TrimEnd().EndsWith(";", StringComparison.Ordinal))
                {
                    var stmt = buf.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(stmt))
                    {
                        list.Add(stmt);
                    }
                    buf.Clear();
                }
            }
            var last = buf.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(last))
            {
                list.Add(last);
            }
            return list;
        }

        private class UpdatesResponse
        {
            public List<Patch>? Patches { get; set; }
        }

        private class Patch
        {
            public string? Id { get; set; }
            public string? Type { get; set; }
            public string Sql { get; set; } = string.Empty;
            public string? Url { get; set; }
            public string? ExpectedVersion { get; set; }
            public string? Sha256 { get; set; }
        }

        private class DbUpdateState
        {
            public DateTime? LastCheckUtc { get; set; }
            public List<string> AppliedPatchIds { get; set; } = new List<string>();
        }

        private static string GetStatePath(string? provided)
        {
            if (!string.IsNullOrWhiteSpace(provided)) return provided!;
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ExamSystem");
            try { Directory.CreateDirectory(dir); } catch { }
            return Path.Combine(dir, "db-patches.json");
        }

        private static DbUpdateState? LoadState(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<DbUpdateState>(json);
            }
            catch { return null; }
        }

        private static void SaveState(string path, DbUpdateState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }
}