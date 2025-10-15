using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 题库Repository接口
    /// </summary>
    public interface IQuestionBankRepository : IRepository<QuestionBank>
    {
        /// <summary>
        /// 根据创建者ID获取题库列表
        /// </summary>
        Task<IEnumerable<QuestionBank>> GetBanksByCreatorIdAsync(int creatorId);

        /// <summary>
        /// 搜索题库
        /// </summary>
        Task<IEnumerable<QuestionBank>> SearchBanksAsync(string keyword, int? creatorId = null);

        /// <summary>
        /// 获取题库及其题目
        /// </summary>
        Task<QuestionBank?> GetBankWithQuestionsAsync(int bankId);

        /// <summary>
        /// 获取题库的题目数量
        /// </summary>
        Task<int> GetQuestionCountAsync(int bankId);

        /// <summary>
        /// 获取题库的题目数量（按类型分组）
        /// </summary>
        Task<Dictionary<ExamSystem.Domain.Enums.QuestionType, int>> GetQuestionCountByTypeAsync(int bankId);

        /// <summary>
        /// 获取题库的题目数量（按难度分组）
        /// </summary>
        Task<Dictionary<string, int>> GetQuestionCountByDifficultyAsync(int bankId);

        /// <summary>
        /// 检查题库是否被试卷使用
        /// </summary>
        Task<bool> IsBankUsedInPapersAsync(int bankId);

        /// <summary>
        /// 获取使用该题库的试卷列表
        /// </summary>
        Task<IEnumerable<ExamPaper>> GetPapersUsingBankAsync(int bankId);

        /// <summary>
        /// 复制题库
        /// </summary>
        Task<QuestionBank> CopyBankAsync(int bankId, string newName, int creatorId);

        /// <summary>
        /// 获取题库统计信息
        /// </summary>
        Task<(int TotalBanks, int TotalQuestions, Dictionary<ExamSystem.Domain.Enums.QuestionType, int> QuestionsByType)> GetBankStatisticsAsync(int? creatorId = null);

        /// <summary>
        /// 检查题库名称是否存在
        /// </summary>
        Task<bool> BankNameExistsAsync(string name, int? excludeBankId = null);

        /// <summary>
        /// 获取最近创建的题库
        /// </summary>
        Task<IEnumerable<QuestionBank>> GetRecentBanksAsync(int count = 10, int? creatorId = null);

        /// <summary>
        /// 获取题库的创建者信息
        /// </summary>
        Task<User?> GetBankCreatorAsync(int bankId);
    }
}