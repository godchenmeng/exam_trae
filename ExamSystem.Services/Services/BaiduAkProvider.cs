using System;
using System.Threading;

namespace ExamSystem.Services
{
  /// <summary>
  /// 全局AK提供者，从配置服务读取并缓存。
  /// </summary>
  public class BaiduAkProvider
  {
    private readonly ConfigurationService _config;
    private string? _cachedAk;
    private DateTime _lastLoadAt;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);
    private readonly object _lock = new object();

    public BaiduAkProvider(ConfigurationService config)
    {
      _config = config;
    }

    public string? GetAk(bool forceReload = false)
    {
      lock (_lock)
      {
        if (forceReload || _cachedAk == null || DateTime.UtcNow - _lastLoadAt > _cacheDuration)
        {
          _cachedAk = _config.GetConfig(ConfigurationService.Keys.BaiduMapAk, decryptSensitive: true);
          _lastLoadAt = DateTime.UtcNow;
        }
        return _cachedAk;
      }
    }
  }
}