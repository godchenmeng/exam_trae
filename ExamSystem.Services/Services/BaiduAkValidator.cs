using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExamSystem.Services
{
  /// <summary>
  /// 调用百度地图API验证AK有效性。
  /// </summary>
  public class BaiduAkValidator
  {
    private readonly HttpClient _http;
    public BaiduAkValidator(HttpClient? httpClient = null)
    {
      _http = httpClient ?? new HttpClient();
    }

    public async Task<(bool ok, string? message)> ValidateAsync(string ak)
    {
      if (string.IsNullOrWhiteSpace(ak)) return (false, "AK不能为空");
      try
      {
        // 使用地理编码接口进行简单校验
        var url = $"https://api.map.baidu.com/api?v=1.0&type=webgl&ak={Uri.EscapeDataString(ak)}";
        var resp = await _http.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
        {
          return (false, $"HTTP错误: {(int)resp.StatusCode}");
        }
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("status", out var statusElem))
        {
          var status = statusElem.GetInt32();
          if (status == 0)
          {
            return (true, "AK 验证通过");
          }
          else
          {
            string msg = doc.RootElement.TryGetProperty("message", out var msgElem) ? msgElem.GetString() ?? "验证失败" : "验证失败";
            return (false, msg);
          }
        }
        return (false, "响应格式异常");
      }
      catch (Exception ex)
      {
        return (false, $"异常: {ex.Message}");
      }
    }
  }
}