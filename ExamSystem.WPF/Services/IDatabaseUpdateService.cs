using System.Threading.Tasks;

namespace ExamSystem.WPF.Services
{
  /// <summary>
  /// 从服务器获取数据库更新语句并应用到本地客户端数据库的服务。
  /// </summary>
  public interface IDatabaseUpdateService
  {
    /// <summary>
    /// 异步获取服务器数据库更新语句并应用到本地 SQLite 数据库。
    /// </summary>
    Task FetchAndApplyAsync();
  }
}