using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ExamSystem.Data;

using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Data;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ExamSystem.Services.Services
{

/// <summary>
/// 题目服务实现
/// </summary>
public class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ILogger<QuestionService> _logger;
    private readonly ExamDbContext _context;

    public QuestionService(IQuestionRepository questionRepository, ILogger<QuestionService> logger, ExamDbContext context)
    {
        _questionRepository = questionRepository;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 根据题库ID获取题目列表
    /// </summary>
    public async Task<List<Question>> GetQuestionsByBankIdAsync(int bankId, QuestionFilter? filter = null)
    {
        try
        {
            if (filter != null)
            {
                return (await _questionRepository.SearchQuestionsAsync(
                    bankId: bankId,
                    keyword: filter.Keyword,
                    questionType: filter.QuestionType,
                    difficulty: filter.Difficulty
                )).ToList();
            }
            else
            {
                return (await _questionRepository.GetQuestionsByBankIdAsync(bankId)).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取题目列表失败，题库ID: {BankId}", bankId);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取题目
    /// </summary>
    public async Task<Question?> GetQuestionByIdAsync(int questionId)
    {
        try
        {
            return await _questionRepository.GetQuestionWithOptionsAsync(questionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取题目失败，题目ID: {QuestionId}", questionId);
            throw;
        }
    }

    /// <summary>
    /// 创建题目
    /// </summary>
    public async Task<bool> CreateQuestionAsync(Question question)
    {
        try
        {
            // 验证题目数据
            var validationResult = await ValidateQuestionAsync(question);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("题目数据验证失败: {Errors}", string.Join(", ", validationResult.Errors));
                return false;
            }

            question.CreatedAt = DateTime.Now;
            question.UpdatedAt = DateTime.Now;
            question.IsActive = true;

            await _questionRepository.AddAsync(question);

            _logger.LogInformation("创建题目成功: {Title}", question.Title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建题目失败: {Title}", question.Title);
            throw;
        }
    }

    /// <summary>
    /// 更新题目
    /// </summary>
    public async Task<bool> UpdateQuestionAsync(Question question)
    {
        try
        {
            // 验证题目数据
            var validationResult = await ValidateQuestionAsync(question);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("题目数据验证失败: {Errors}", string.Join(", ", validationResult.Errors));
                return false;
            }

            var existingQuestion = await _questionRepository.GetQuestionWithOptionsAsync(question.QuestionId);

            if (existingQuestion == null)
            {
                _logger.LogWarning("题目不存在: {QuestionId}", question.QuestionId);
                return false;
            }

            // 更新题目基本信息
            existingQuestion.Title = question.Title;
            existingQuestion.Content = question.Content;
            existingQuestion.QuestionType = question.QuestionType;
            existingQuestion.Difficulty = question.Difficulty;
            existingQuestion.Score = question.Score;
            existingQuestion.Answer = question.Answer;
            existingQuestion.Analysis = question.Analysis;
            existingQuestion.Tags = question.Tags;
            existingQuestion.UpdatedAt = DateTime.Now;

            // 更新选项（如果是选择题）
            if (question.QuestionType == QuestionType.SingleChoice || 
                question.QuestionType == QuestionType.MultipleChoice)
            {
                // 删除原有选项
                existingQuestion.Options.Clear();

                // 添加新选项
                foreach (var option in question.Options)
                {
                    option.QuestionId = existingQuestion.QuestionId;
                    existingQuestion.Options.Add(option);
                }
            }

            _questionRepository.Update(existingQuestion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("更新题目成功: {Title}", question.Title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新题目失败: {QuestionId}", question.QuestionId);
            throw;
        }
    }

    /// <summary>
    /// 删除题目
    /// </summary>
    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        try
        {
            // 检查是否被引用
            if (await _questionRepository.IsQuestionUsedInPapersAsync(questionId))
            {
                _logger.LogWarning("题目已被引用，无法删除: {QuestionId}", questionId);
                return false;
            }

            var result = await _questionRepository.RemoveByIdAsync(questionId);

            _logger.LogInformation("删除题目成功: {QuestionId}", questionId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除题目失败: {QuestionId}", questionId);
            throw;
        }
    }

    /// <summary>
    /// 批量删除题目
    /// </summary>
    public async Task<bool> DeleteQuestionsAsync(List<int> questionIds)
    {
        try
        {
            // 获取要删除的题目
            var questions = await _questionRepository.FindAsync(q => questionIds.Contains(q.QuestionId));
            
            // 删除题目
            _questionRepository.RemoveRange(questions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("批量删除题目成功，删除数量: {Count}", questionIds.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除题目失败");
            throw;
        }
    }

    /// <summary>
    /// 搜索题目
    /// </summary>
    public async Task<List<Question>> SearchQuestionsAsync(QuestionSearchCriteria searchCriteria)
    {
        try
        {
            return (await _questionRepository.SearchQuestionsAsync(
                keyword: searchCriteria.Keyword,
                bankId: searchCriteria.BankId,
                questionType: searchCriteria.QuestionType,
                difficulty: searchCriteria.Difficulty
            )).ToList();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索题目失败");
            throw;
        }
    }

    /// <summary>
    /// 搜索题目（简化版本）
    /// </summary>
    public async Task<List<Question>> SearchAsync(int bankId, string? keyword, QuestionType? questionType, Difficulty? difficulty)
    {
        try
        {
            return (await _questionRepository.SearchQuestionsAsync(
                keyword: keyword,
                bankId: bankId,
                questionType: questionType,
                difficulty: difficulty
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索题目失败: BankId={BankId}, Keyword={Keyword}", bankId, keyword);
            throw;
        }
    }

    /// <summary>
    /// 验证题目数据
    /// </summary>
    public async Task<ValidationResult> ValidateQuestionAsync(Question question)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            // 验证标题
            if (string.IsNullOrWhiteSpace(question.Title))
                result.AddError("题目标题不能为空");
            else if (question.Title.Length > 200)
                result.AddError("题目标题长度不能超过200个字符");

            // 验证内容
            if (string.IsNullOrWhiteSpace(question.Content))
                result.AddError("题目内容不能为空");
            else if (question.Content.Length > 2000)
                result.AddError("题目内容长度不能超过2000个字符");

            // 验证分值
            if (question.Score <= 0)
                result.AddError("题目分值必须大于0");
            else if (question.Score > 100)
                result.AddError("题目分值不能超过100分");

            // 验证答案
            if (string.IsNullOrWhiteSpace(question.Answer))
                result.AddError("题目答案不能为空");

            // 验证题库是否存在
            var bankExists = await _context.QuestionBanks
                .AnyAsync(qb => qb.BankId == question.BankId && qb.IsActive);
            if (!bankExists)
                result.AddError("指定的题库不存在");

            // 验证选择题选项
            if (question.QuestionType == QuestionType.SingleChoice || 
                question.QuestionType == QuestionType.MultipleChoice)
            {
                if (question.Options == null || !question.Options.Any())
                    result.AddError("选择题必须包含选项");
                else if (question.Options.Count < 2)
                    result.AddError("选择题至少需要2个选项");
                else if (question.Options.Count > 10)
                    result.AddError("选择题选项不能超过10个");

                // 验证选项内容
                foreach (var option in question.Options)
                {
                    if (string.IsNullOrWhiteSpace(option.Content))
                        result.AddError("选项内容不能为空");
                    else if (option.Content.Length > 500)
                        result.AddError("选项内容长度不能超过500个字符");
                }

                // 验证答案格式
                if (question.QuestionType == QuestionType.SingleChoice)
                {
                    if (!Regex.IsMatch(question.Answer, @"^[A-Z]$"))
                        result.AddError("单选题答案格式应为单个大写字母（如：A）");
                }
                else if (question.QuestionType == QuestionType.MultipleChoice)
                {
                    if (!Regex.IsMatch(question.Answer, @"^[A-Z]+(,[A-Z]+)*$"))
                        result.AddError("多选题答案格式应为大写字母用逗号分隔（如：A,C,D）");
                }
            }

            // 验证判断题答案
            if (question.QuestionType == QuestionType.TrueFalse)
            {
                if (question.Answer != "True" && question.Answer != "False")
                    result.AddError("判断题答案必须为True或False");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证题目数据失败");
            result.AddError("验证过程中发生错误");
            return result;
        }
    }

    /// <summary>
    /// 复制题目
    /// </summary>
    public async Task<Question?> DuplicateQuestionAsync(int questionId, int targetBankId)
    {
        try
        {
            var sourceQuestion = await GetQuestionByIdAsync(questionId);
            if (sourceQuestion == null)
            {
                _logger.LogWarning("源题目不存在: {QuestionId}", questionId);
                return null;
            }

            // 检查目标题库是否存在
            var targetBankExists = await _context.QuestionBanks
                .AnyAsync(qb => qb.BankId == targetBankId && qb.IsActive);
            if (!targetBankExists)
            {
                _logger.LogWarning("目标题库不存在: {BankId}", targetBankId);
                return null;
            }

            var newQuestion = new Question
            {
                Title = sourceQuestion.Title + " (副本)",
                Content = sourceQuestion.Content,
                QuestionType = sourceQuestion.QuestionType,
                Difficulty = sourceQuestion.Difficulty,
                Score = sourceQuestion.Score,
                Answer = sourceQuestion.Answer,
                Analysis = sourceQuestion.Analysis,
                Tags = sourceQuestion.Tags,
                BankId = targetBankId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true
            };

            // 复制选项
            foreach (var option in sourceQuestion.Options)
            {
                newQuestion.Options.Add(new QuestionOption
                {
                    Content = option.Content,
                    OptionLabel = option.OptionLabel,
                    IsCorrect = option.IsCorrect
                });
            }

            _context.Questions.Add(newQuestion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("复制题目成功: {SourceId} -> {TargetId}", questionId, newQuestion.QuestionId);
            return newQuestion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制题目失败: {QuestionId}", questionId);
            throw;
        }
    }

    /// <summary>
    /// 检查题目是否被引用
    /// </summary>
    public async Task<bool> IsQuestionReferencedAsync(int questionId)
    {
        try
        {
            // 检查是否在试卷中被引用
            var isReferencedInPaper = await _context.PaperQuestions
                .AnyAsync(pq => pq.QuestionId == questionId);

            // 检查是否有答题记录
            var hasAnswerRecords = await _context.AnswerRecords
                .AnyAsync(ar => ar.QuestionId == questionId);

            return isReferencedInPaper || hasAnswerRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查题目引用失败: {QuestionId}", questionId);
            throw;
        }
    }
}}
