using System.Threading.Tasks;

namespace ExamSystem.WPF.Services
{
    /// <summary>
    /// 客户端更新检查服务接口：在应用启动时异步检查是否有新版本，并在发现新版本时提示用户更新。
    /// </summary>
    public interface IUpdateCheckService
    {
        /// <summary>
        /// 异步检查并在需要时提示用户更新。
        /// </summary>
        Task CheckAndPromptAsync();
    }
}