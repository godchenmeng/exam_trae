using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 题库Repository实现类
    /// </summary>
    public class QuestionBankRepository : BaseRepository<QuestionBank>, IQuestionBankRepository
    {
        public QuestionBankRepository(ExamDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据创建者ID获取题库列表
        /// </summary>
        public async Task<IEnumerable<QuestionBank>> GetBanksByCreatorIdAsync(int creatorId)
        {
            return await _dbSet.Where(qb => qb.CreatorId == creatorId)
                              .OrderByDescending(qb => qb.CreatedAt)
                              .ToListAsync();
        }

        /// <summary>
        /// 搜索题库
        /// </summary>
        public async Task<IEnumerable<QuestionBank>> SearchBanksAsync(string keyword, int? creatorId = null)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(qb => qb.Name.Contains(keyword) || 
                                         qb.Description.Contains(keyword));
            }

            if (creatorId.HasValue)
            {
                query = query.Where(qb => qb.CreatorId == creatorId.Value);
            }

            return await query.OrderByDescending(qb => qb.CreatedAt).ToListAsync();
        }

        /// <summary>
        /// 获取题库及其题目
        /// </summary>
        public async Task<QuestionBank?> GetBankWithQuestionsAsync(int bankId)
        {
            return await _dbSet.Include(qb => qb.Questions)
                              .ThenInclude(q => q.Options)
                              .FirstOrDefaultAsync(qb => qb.BankId == bankId);
        }

        /// <summary>
        /// 获取题库的题目数量
        /// </summary>
        public async Task<int> GetQuestionCountAsync(int bankId)
        {
            return await _context.Questions
                                .CountAsync(q => q.BankId == bankId);
        }

        /// <summary>
        /// 获取题库的题目数量（按类型分组）
        /// </summary>
        public async Task<Dictionary<ExamSystem.Domain.Enums.QuestionType, int>> GetQuestionCountByTypeAsync(int bankId)
        {
            return await _context.Questions
                                .Where(q => q.BankId == bankId)
                                .GroupBy(q => q.QuestionType)
                                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取题库的题目数量（按难度分组）
        /// </summary>
        public async Task<Dictionary<string, int>> GetQuestionCountByDifficultyAsync(int bankId)
        {
            return await _context.Questions
                                .Where(q => q.BankId == bankId)
                                .GroupBy(q => q.Difficulty)
                                .ToDictionaryAsync(g => g.Key.ToString(), g => g.Count());
        }

        /// <summary>
        /// 检查题库是否被试卷使用
        /// </summary>
        public async Task<bool> IsBankUsedInPapersAsync(int bankId)
        {
            return await _context.PaperQuestions
                                .AnyAsync(pq => pq.Question.BankId == bankId);
        }

        /// <summary>
        /// 获取使用该题库的试卷列表
        /// </summary>
        public async Task<IEnumerable<ExamPaper>> GetPapersUsingBankAsync(int bankId)
        {
            return await _context.PaperQuestions
                                .Where(pq => pq.Question.BankId == bankId)
                                .Select(pq => pq.ExamPaper)
                                .Distinct()
                                .OrderByDescending(ep => ep.CreatedAt)
                                .ToListAsync();
        }

        /// <summary>
        /// 复制题库
        /// </summary>
        public async Task<QuestionBank> CopyBankAsync(int bankId, string newName, int creatorId)
        {
            var originalBank = await GetBankWithQuestionsAsync(bankId);
            if (originalBank == null)
                throw new ArgumentException("题库不存在", nameof(bankId));

            var newBank = new QuestionBank
            {
                Name = newName,
                Description = originalBank.Description,
                CreatorId = creatorId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await AddAsync(newBank);

            // 复制题目
            if (originalBank.Questions != null && originalBank.Questions.Any())
            {
                foreach (var question in originalBank.Questions)
                {
                    var newQuestion = new Question
                    {
                        BankId = newBank.BankId,
                        Content = question.Content,
                        QuestionType = question.QuestionType,
                        Difficulty = question.Difficulty,
                        Answer = question.Answer,
                        Analysis = question.Analysis,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    await _context.Questions.AddAsync(newQuestion);

                    // 复制选项
                    if (question.Options != null && question.Options.Any())
                    {
                        foreach (var option in question.Options)
                        {
                            var newOption = new QuestionOption
                            {
                                QuestionId = newQuestion.QuestionId,
                                Content = option.Content,
                                 IsCorrect = option.IsCorrect,
                                 OrderIndex = option.OrderIndex
                            };

                            await _context.QuestionOptions.AddAsync(newOption);
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }

            return newBank;
        }

        /// <summary>
        /// 获取题库统计信息
        /// </summary>
        public async Task<(int TotalBanks, int TotalQuestions, Dictionary<ExamSystem.Domain.Enums.QuestionType, int> QuestionsByType)> GetBankStatisticsAsync(int? creatorId = null)
        {
            var bankQuery = _dbSet.AsQueryable();
            var questionQuery = _context.Questions.AsQueryable();

            if (creatorId.HasValue)
            {
                bankQuery = bankQuery.Where(qb => qb.CreatorId == creatorId.Value);
                questionQuery = questionQuery.Where(q => q.QuestionBank.CreatorId == creatorId.Value);
            }

            var totalBanks = await bankQuery.CountAsync();
            var totalQuestions = await questionQuery.CountAsync();
            var questionsByType = await questionQuery
                                        .GroupBy(q => q.QuestionType)
                                        .ToDictionaryAsync(g => g.Key, g => g.Count());

            return (totalBanks, totalQuestions, questionsByType);
        }

        /// <summary>
        /// 检查题库名称是否存在
        /// </summary>
        public async Task<bool> BankNameExistsAsync(string name, int? excludeBankId = null)
        {
            var query = _dbSet.Where(qb => qb.Name == name);

            if (excludeBankId.HasValue)
            {
                query = query.Where(qb => qb.BankId != excludeBankId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 获取最近创建的题库
        /// </summary>
        public async Task<IEnumerable<QuestionBank>> GetRecentBanksAsync(int count = 10, int? creatorId = null)
        {
            var query = _dbSet.AsQueryable();

            if (creatorId.HasValue)
            {
                query = query.Where(qb => qb.CreatorId == creatorId.Value);
            }

            return await query.OrderByDescending(qb => qb.CreatedAt)
                             .Take(count)
                             .ToListAsync();
        }

        /// <summary>
        /// 获取题库的创建者信息
        /// </summary>
        public async Task<User?> GetBankCreatorAsync(int bankId)
        {
            return await _context.Users
                                .Where(u => u.CreatedQuestionBanks.Any(qb => qb.BankId == bankId))
                                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 根据ID获取题库（重写以使用BankId）
        /// </summary>
        public override async Task<QuestionBank?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(qb => qb.BankId == id);
        }

        /// <summary>
        /// 根据ID删除题库（重写以使用BankId）
        /// </summary>
        public override async Task<bool> RemoveByIdAsync(int id)
        {
            var bank = await GetByIdAsync(id);
            if (bank == null)
                return false;

            // 检查是否被试卷使用
            if (await IsBankUsedInPapersAsync(id))
            {
                throw new InvalidOperationException("题库正在被试卷使用，无法删除");
            }

            // 删除题库下的所有题目和选项
            var questions = await _context.Questions
                                         .Where(q => q.BankId == id)
                                         .Include(q => q.Options)
                                         .ToListAsync();

            foreach (var question in questions)
            {
                if (question.Options != null && question.Options.Any())
                {
                    _context.QuestionOptions.RemoveRange(question.Options);
                }
            }

            _context.Questions.RemoveRange(questions);
            await RemoveAsync(bank);
            return true;
        }
    }
}