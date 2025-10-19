using System.IO;
using System.Threading.Tasks;
using ExamSystem.Services.Models;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// Excel导入服务接口
    /// </summary>
    public interface IExcelImportService
    {
        /// <summary>
        /// 导入题目从Excel文件
        /// </summary>
        /// <param name="fileStream">Excel文件流</param>
        /// <param name="questionBankId">题库ID</param>
        /// <returns>导入结果</returns>
        Task<ImportResult> ImportQuestionsFromExcelAsync(Stream fileStream, int questionBankId);

        /// <summary>
        /// 验证Excel文件格式
        /// </summary>
        /// <param name="fileStream">Excel文件流</param>
        /// <returns>验证结果</returns>
        Task<ImportResult> ValidateExcelFormatAsync(Stream fileStream);

        /// <summary>
        /// 获取Excel模板文件
        /// </summary>
        /// <returns>模板文件字节数组</returns>
        byte[] GetExcelTemplate();
    }
}