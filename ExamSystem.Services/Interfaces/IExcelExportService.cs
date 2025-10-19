using ExamSystem.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// Excel导出服务接口
    /// </summary>
    public interface IExcelExportService
    {
        /// <summary>
        /// 导出题目到Excel文件
        /// </summary>
        /// <param name="questions">题目列表</param>
        /// <param name="questionBankName">题库名称</param>
        /// <returns>Excel文件字节数组</returns>
        Task<byte[]> ExportQuestionsToExcelAsync(IEnumerable<Question> questions, string questionBankName);

        /// <summary>
        /// 导出题库统计信息到Excel
        /// </summary>
        /// <param name="questionBankId">题库ID</param>
        /// <returns>Excel文件字节数组</returns>
        Task<byte[]> ExportQuestionBankStatisticsAsync(int questionBankId);

        /// <summary>
        /// 导出题目模板文件
        /// </summary>
        /// <returns>Excel模板文件字节数组</returns>
        byte[] ExportQuestionTemplate();

        /// <summary>
        /// 导出选定题目到Excel
        /// </summary>
        /// <param name="questionIds">题目ID列表</param>
        /// <param name="questionBankName">题库名称</param>
        /// <returns>Excel文件字节数组</returns>
        Task<byte[]> ExportSelectedQuestionsAsync(IEnumerable<int> questionIds, string questionBankName);
    }
}