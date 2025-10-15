using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 题目Repository接口
    /// </summary>
    public interface IQuestionRepository : IRepository<Question>
    {
        /// <summary>
        /// 根据题库ID获取题目列表
        /// </summary>
        /// <param name="bankId">题库ID</param>
        /// <returns>题目列表</returns>
        Task<IEnumerable<Question>> GetQuestionsByBankIdAsync(int bankId);

        /// <summary>
        /// 根据题目类型获取题目列表
        /// </summary>
        /// <param name="questionType">题目类型</param>
        /// <returns>题目列表</returns>
        Task<IEnumerable<Question>> GetQuestionsByTypeAsync(QuestionType questionType);

        /// <summary>
        /// 根据难度获取题目列表
        /// </summary>
        /// <param name="difficulty">难度等级</param>
        /// <returns>题目列表</returns>
        Task<IEnumerable<Question>> GetQuestionsByDifficultyAsync(Difficulty difficulty);

        /// <summary>
        /// 搜索题目
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="bankId">题库ID（可选）</param>
        /// <param name="questionType">题目类型（可选）</param>
        /// <param name="difficulty">难度等级（可选）</param>
        /// <returns>符合条件的题目列表</returns>
        Task<IEnumerable<Question>> SearchQuestionsAsync(
            string keyword, 
            int? bankId = null, 
            QuestionType? questionType = null, 
            Difficulty? difficulty = null);

        /// <summary>
        /// 获取题目及其选项
        /// </summary>
        /// <param name="questionId">题目ID</param>
        /// <returns>包含选项的题目</returns>
        Task<Question?> GetQuestionWithOptionsAsync(int questionId);

        /// <summary>
        /// 获取题库中的题目及其选项
        /// </summary>
        /// <param name="bankId">题库ID</param>
        /// <returns>包含选项的题目列表</returns>
        Task<IEnumerable<Question>> GetQuestionsWithOptionsByBankIdAsync(int bankId);

        /// <summary>
        /// 随机获取题目
        /// </summary>
        /// <param name="bankId">题库ID</param>
        /// <param name="count">题目数量</param>
        /// <param name="questionType">题目类型（可选）</param>
        /// <param name="difficulty">难度等级（可选）</param>
        /// <returns>随机题目列表</returns>
        Task<IEnumerable<Question>> GetRandomQuestionsAsync(
            int bankId, 
            int count, 
            QuestionType? questionType = null, 
            Difficulty? difficulty = null);

        /// <summary>
        /// 获取题目统计信息
        /// </summary>
        /// <param name="bankId">题库ID（可选）</param>
        /// <returns>题目统计信息</returns>
        Task<(int TotalQuestions, int SingleChoice, int MultipleChoice, int TrueFalse, int FillInBlank, int Essay, 
               int EasyQuestions, int MediumQuestions, int HardQuestions)> GetQuestionStatisticsAsync(int? bankId = null);

        /// <summary>
        /// 复制题目到另一个题库
        /// </summary>
        /// <param name="questionId">题目ID</param>
        /// <param name="targetBankId">目标题库ID</param>
        /// <returns>复制后的题目</returns>
        Task<Question> CopyQuestionToBankAsync(int questionId, int targetBankId);

        /// <summary>
        /// 批量删除题库中的题目
        /// </summary>
        /// <param name="bankId">题库ID</param>
        Task DeleteQuestionsByBankIdAsync(int bankId);

        /// <summary>
        /// 检查题目是否被试卷使用
        /// </summary>
        /// <param name="questionId">题目ID</param>
        /// <returns>是否被使用</returns>
        Task<bool> IsQuestionUsedInPapersAsync(int questionId);

        /// <summary>
        /// 获取题目的使用情况
        /// </summary>
        /// <param name="questionId">题目ID</param>
        /// <returns>使用该题目的试卷列表</returns>
        Task<IEnumerable<ExamPaper>> GetPapersUsingQuestionAsync(int questionId);
    }
}