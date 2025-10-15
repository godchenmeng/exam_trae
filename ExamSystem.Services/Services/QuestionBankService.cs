using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;

using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamSystem.Services.Services
{

/// <summary>
/// 题库服务实现
/// </summary>
public class QuestionBankService : IQuestionBankService
{
    private readonly IQuestionBankRepository _questionBankRepository;
    private readonly ILogger<QuestionBankService> _logger;
    private readonly ExamDbContext _context;

    public QuestionBankService(IQuestionBankRepository questionBankRepository, ILogger<QuestionBankService> logger, ExamDbContext context)
    {
        _questionBankRepository = questionBankRepository;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 获取所有题库
    /// </summary>
    public async Task<List<QuestionBank>> GetAllQuestionBanksAsync()
    {
        try
        {
            return (await _questionBankRepository.GetAllAsync()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取题库列表失败");
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取题库
    /// </summary>
    public async Task<QuestionBank?> GetQuestionBankByIdAsync(int bankId)
    {
        try
        {
            return await _questionBankRepository.GetBankWithQuestionsAsync(bankId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取题库失败，题库ID: {BankId}", bankId);
            throw;
        }
    }

    /// <summary>
    /// 创建题库
    /// </summary>
    public async Task<bool> CreateQuestionBankAsync(QuestionBank questionBank)
    {
        try
        {
            // 检查名称是否重复
            if (await _questionBankRepository.BankNameExistsAsync(questionBank.Name))
            {
                _logger.LogWarning("题库名称已存在: {Name}", questionBank.Name);
                return false;
            }

            questionBank.CreatedAt = DateTime.Now;
            questionBank.UpdatedAt = DateTime.Now;
            questionBank.IsActive = true;

            await _questionBankRepository.AddAsync(questionBank);

            _logger.LogInformation("创建题库成功: {Name}", questionBank.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建题库失败: {Name}", questionBank.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新题库
    /// </summary>
    public async Task<bool> UpdateQuestionBankAsync(QuestionBank questionBank)
    {
        try
        {
            // 检查名称是否重复（排除自己）
            if (await IsQuestionBankNameExistsAsync(questionBank.Name, questionBank.BankId))
            {
                _logger.LogWarning("题库名称已存在: {Name}", questionBank.Name);
                return false;
            }

            var existingBank = await _context.QuestionBanks
                .FirstOrDefaultAsync(qb => qb.BankId == questionBank.BankId);

            if (existingBank == null)
            {
                _logger.LogWarning("题库不存在: {BankId}", questionBank.BankId);
                return false;
            }

            existingBank.Name = questionBank.Name;
            existingBank.Description = questionBank.Description;
            existingBank.UpdatedAt = DateTime.Now;

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation("更新题库成功: {Name}", questionBank.Name);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新题库失败: {BankId}", questionBank.BankId);
            throw;
        }
    }

    /// <summary>
    /// 删除题库
    /// </summary>
    public async Task<bool> DeleteQuestionBankAsync(int bankId)
    {
        try
        {
            var questionBank = await _context.QuestionBanks
                .Include(qb => qb.Questions)
                .FirstOrDefaultAsync(qb => qb.BankId == bankId);

            if (questionBank == null)
            {
                _logger.LogWarning("题库不存在: {BankId}", bankId);
                return false;
            }

            // 软删除：标记为非活跃状态
            questionBank.IsActive = false;
            questionBank.UpdatedAt = DateTime.Now;

            // 同时软删除题库下的所有题目
            foreach (var question in questionBank.Questions)
            {
                question.IsActive = false;
                question.UpdatedAt = DateTime.Now;
            }

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation("删除题库成功: {BankId}", bankId);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除题库失败: {BankId}", bankId);
            throw;
        }
    }

    /// <summary>
    /// 检查题库名称是否存在
    /// </summary>
    public async Task<bool> IsQuestionBankNameExistsAsync(string name, int? excludeBankId = null)
    {
        try
        {
            var query = _context.QuestionBanks
                .Where(qb => qb.Name == name && qb.IsActive);

            if (excludeBankId.HasValue)
            {
                query = query.Where(qb => qb.BankId != excludeBankId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查题库名称失败: {Name}", name);
            throw;
        }
    }

    /// <summary>
    /// 获取题库统计信息
    /// </summary>
    public async Task<QuestionBankStatistics> GetQuestionBankStatisticsAsync(int bankId)
    {
        try
        {
            var questions = await _context.Questions
                .Where(q => q.BankId == bankId && q.IsActive)
                .ToListAsync();

            var statistics = new QuestionBankStatistics
            {
                TotalQuestions = questions.Count,
                SingleChoiceCount = questions.Count(q => q.QuestionType == QuestionType.SingleChoice),
                MultipleChoiceCount = questions.Count(q => q.QuestionType == QuestionType.MultipleChoice),
                TrueFalseCount = questions.Count(q => q.QuestionType == QuestionType.TrueFalse),
                FillInBlankCount = questions.Count(q => q.QuestionType == QuestionType.FillInBlank),
                EssayCount = questions.Count(q => q.QuestionType == QuestionType.Essay),
                EasyCount = questions.Count(q => q.Difficulty == Difficulty.Easy),
                MediumCount = questions.Count(q => q.Difficulty == Difficulty.Medium),
                HardCount = questions.Count(q => q.Difficulty == Difficulty.Hard)
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取题库统计信息失败: {BankId}", bankId);
            throw;
        }
    }
}}
