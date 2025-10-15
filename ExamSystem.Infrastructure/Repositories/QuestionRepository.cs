using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 题目Repository实现类
    /// </summary>
    public class QuestionRepository : BaseRepository<Question>, IQuestionRepository
    {
        public QuestionRepository(ExamDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据题库ID获取题目列表
        /// </summary>
        public async Task<IEnumerable<Question>> GetQuestionsByBankIdAsync(int bankId)
        {
            return await _dbSet.Where(q => q.BankId == bankId)
                              .OrderBy(q => q.QuestionId)
                              .ToListAsync();
        }

        /// <summary>
        /// 根据题目类型获取题目列表
        /// </summary>
        public async Task<IEnumerable<Question>> GetQuestionsByTypeAsync(QuestionType questionType)
        {
            return await _dbSet.Where(q => q.QuestionType == questionType)
                              .OrderBy(q => q.QuestionId)
                              .ToListAsync();
        }

        /// <summary>
        /// 根据难度获取题目列表
        /// </summary>
        public async Task<IEnumerable<Question>> GetQuestionsByDifficultyAsync(Difficulty difficulty)
        {
            return await _dbSet.Where(q => q.Difficulty == difficulty)
                              .OrderBy(q => q.QuestionId)
                              .ToListAsync();
        }

        /// <summary>
        /// 搜索题目
        /// </summary>
        public async Task<IEnumerable<Question>> SearchQuestionsAsync(
            string keyword, 
            int? bankId = null, 
            QuestionType? questionType = null, 
            Difficulty? difficulty = null)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(q => q.Content.Contains(keyword) || 
                                        q.Analysis.Contains(keyword));
            }

            if (bankId.HasValue)
            {
                query = query.Where(q => q.BankId == bankId.Value);
            }

            if (questionType.HasValue)
            {
                query = query.Where(q => q.QuestionType == questionType.Value);
            }

            if (difficulty.HasValue)
            {
                query = query.Where(q => q.Difficulty == difficulty.Value);
            }

            return await query.OrderBy(q => q.QuestionId).ToListAsync();
        }

        /// <summary>
        /// 获取题目及其选项
        /// </summary>
        public async Task<Question?> GetQuestionWithOptionsAsync(int questionId)
        {
            return await _dbSet.Include(q => q.Options)
                              .FirstOrDefaultAsync(q => q.QuestionId == questionId);
        }

        /// <summary>
        /// 获取题库中的题目及其选项
        /// </summary>
        public async Task<IEnumerable<Question>> GetQuestionsWithOptionsByBankIdAsync(int bankId)
        {
            return await _dbSet.Include(q => q.Options)
                              .Where(q => q.BankId == bankId)
                              .OrderBy(q => q.QuestionId)
                              .ToListAsync();
        }

        /// <summary>
        /// 随机获取题目
        /// </summary>
        public async Task<IEnumerable<Question>> GetRandomQuestionsAsync(
            int bankId, 
            int count, 
            QuestionType? questionType = null, 
            Difficulty? difficulty = null)
        {
            var query = _dbSet.Where(q => q.BankId == bankId);

            if (questionType.HasValue)
            {
                query = query.Where(q => q.QuestionType == questionType.Value);
            }

            if (difficulty.HasValue)
            {
                query = query.Where(q => q.Difficulty == difficulty.Value);
            }

            // 使用GUID排序来实现随机
            return await query.OrderBy(q => Guid.NewGuid())
                             .Take(count)
                             .Include(q => q.Options)
                             .ToListAsync();
        }

        /// <summary>
        /// 获取题目统计信息
        /// </summary>
        public async Task<(int TotalQuestions, int SingleChoice, int MultipleChoice, int TrueFalse, int FillInBlank, int Essay, 
                          int EasyQuestions, int MediumQuestions, int HardQuestions)> GetQuestionStatisticsAsync(int? bankId = null)
        {
            var query = _dbSet.AsQueryable();

            if (bankId.HasValue)
            {
                query = query.Where(q => q.BankId == bankId.Value);
            }

            var totalQuestions = await query.CountAsync();
            var singleChoice = await query.CountAsync(q => q.QuestionType == QuestionType.SingleChoice);
            var multipleChoice = await query.CountAsync(q => q.QuestionType == QuestionType.MultipleChoice);
            var trueFalse = await query.CountAsync(q => q.QuestionType == QuestionType.TrueFalse);
            var fillInBlank = await query.CountAsync(q => q.QuestionType == QuestionType.FillInBlank);
            var essay = await query.CountAsync(q => q.QuestionType == QuestionType.Essay);

            var easyQuestions = await query.CountAsync(q => q.Difficulty == Difficulty.Easy);
            var mediumQuestions = await query.CountAsync(q => q.Difficulty == Difficulty.Medium);
            var hardQuestions = await query.CountAsync(q => q.Difficulty == Difficulty.Hard);

            return (totalQuestions, singleChoice, multipleChoice, trueFalse, fillInBlank, essay,
                   easyQuestions, mediumQuestions, hardQuestions);
        }

        /// <summary>
        /// 复制题目到另一个题库
        /// </summary>
        public async Task<Question> CopyQuestionToBankAsync(int questionId, int targetBankId)
        {
            var originalQuestion = await GetQuestionWithOptionsAsync(questionId);
            if (originalQuestion == null)
                throw new ArgumentException("题目不存在", nameof(questionId));

            var newQuestion = new Question
            {
                BankId = targetBankId,
                QuestionType = originalQuestion.QuestionType,
                Content = originalQuestion.Content,
                Answer = originalQuestion.Answer,
                Analysis = originalQuestion.Analysis,
                Difficulty = originalQuestion.Difficulty,
                Score = originalQuestion.Score,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // 复制选项（仅对单选题和多选题）
            if (originalQuestion.QuestionType == QuestionType.SingleChoice || originalQuestion.QuestionType == QuestionType.MultipleChoice)
            {
                if (originalQuestion.Options != null && originalQuestion.Options.Any())
                {
                    newQuestion.Options = originalQuestion.Options.Select(o => new QuestionOption
                    {
                        Content = o.Content,
                        IsCorrect = o.IsCorrect,
                        OrderIndex = o.OrderIndex
                    }).ToList();
                }
            }

            return await AddAsync(newQuestion);
        }

        /// <summary>
        /// 批量删除题库中的题目
        /// </summary>
        public async Task DeleteQuestionsByBankIdAsync(int bankId)
        {
            var questions = await _dbSet.Where(q => q.BankId == bankId).ToListAsync();
            if (questions.Any())
            {
                await RemoveRangeAsync(questions);
            }
        }

        /// <summary>
        /// 检查题目是否被试卷使用
        /// </summary>
        public async Task<bool> IsQuestionUsedInPapersAsync(int questionId)
        {
            return await _context.PaperQuestions.AnyAsync(pq => pq.QuestionId == questionId);
        }

        /// <summary>
        /// 获取题目的使用情况
        /// </summary>
        public async Task<IEnumerable<ExamPaper>> GetPapersUsingQuestionAsync(int questionId)
        {
            return await _context.PaperQuestions
                                .Where(pq => pq.QuestionId == questionId)
                                .Select(pq => pq.ExamPaper)
                                .Distinct()
                                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取题目（重写以使用QuestionId）
        /// </summary>
        public override async Task<Question?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(q => q.QuestionId == id);
        }

        /// <summary>
        /// 根据ID删除题目（重写以使用QuestionId）
        /// </summary>
        public override async Task<bool> RemoveByIdAsync(int id)
        {
            var question = await GetByIdAsync(id);
            if (question == null)
                return false;

            await RemoveAsync(question);
            return true;
        }
    }
}