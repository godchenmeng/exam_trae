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
/// 试卷服务实现
/// </summary>
public class ExamPaperService : IExamPaperService
{
    private readonly IExamPaperRepository _examPaperRepository;
    private readonly ILogger<ExamPaperService> _logger;
    private readonly ExamDbContext _context;

    public ExamPaperService(IExamPaperRepository examPaperRepository, ILogger<ExamPaperService> logger, ExamDbContext context)
    {
        _examPaperRepository = examPaperRepository;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 获取所有试卷
    /// </summary>
    public async Task<List<ExamPaper>> GetAllExamPapersAsync()
    {
        try
        {
            return (await _examPaperRepository.GetAllAsync()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取试卷列表时发生错误");
            return new List<ExamPaper>();
        }
    }

    /// <summary>
    /// 根据ID获取试卷
    /// </summary>
    public async Task<ExamPaper?> GetExamPaperByIdAsync(int paperId)
    {
        try
        {
            return await _examPaperRepository.GetPaperWithQuestionsAsync(paperId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取试卷时发生错误，试卷ID: {PaperId}", paperId);
            return null;
        }
    }

    /// <summary>
    /// 创建试卷
    /// </summary>
    public async Task<bool> CreateExamPaperAsync(ExamPaper examPaper)
    {
        try
        {
            examPaper.CreatedAt = DateTime.Now;
            examPaper.UpdatedAt = DateTime.Now;

            await _examPaperRepository.AddAsync(examPaper);

            _logger.LogInformation("创建试卷成功: {PaperName}", examPaper.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建试卷时发生错误: {PaperName}", examPaper.Name);
            return false;
        }
    }

    /// <summary>
    /// 更新试卷
    /// </summary>
    public async Task<bool> UpdateExamPaperAsync(ExamPaper examPaper)
    {
        try
        {
            var existingPaper = await _examPaperRepository.GetByIdAsync(examPaper.PaperId);
            if (existingPaper == null)
            {
                _logger.LogWarning("试卷不存在: {PaperId}", examPaper.PaperId);
                return false;
            }

            // 更新属性
            existingPaper.Name = examPaper.Name;
            existingPaper.Description = examPaper.Description;
            existingPaper.TotalScore = examPaper.TotalScore;
            existingPaper.Duration = examPaper.Duration;
            existingPaper.Status = examPaper.Status;
            existingPaper.StartTime = examPaper.StartTime;
            existingPaper.EndTime = examPaper.EndTime;
            existingPaper.IsActive = examPaper.IsActive;
            existingPaper.IsRandomOrder = examPaper.IsRandomOrder;
            existingPaper.AllowViewAnswer = examPaper.AllowViewAnswer;
            existingPaper.PassScore = examPaper.PassScore;
            existingPaper.UpdatedAt = DateTime.Now;

            _examPaperRepository.Update(existingPaper);
            await _context.SaveChangesAsync();

            _logger.LogInformation("更新试卷成功: {PaperName}", examPaper.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新试卷时发生错误: {PaperId}", examPaper.PaperId);
            return false;
        }
    }

    /// <summary>
    /// 删除试卷
    /// </summary>
    public async Task<bool> DeleteExamPaperAsync(int paperId)
    {
        try
        {
            // 检查是否有考试记录
            if (await _examPaperRepository.IsPaperUsedInExamsAsync(paperId))
            {
                _logger.LogWarning("试卷已有考试记录，无法删除: {PaperId}", paperId);
                return false;
            }

            var result = await _examPaperRepository.RemoveByIdAsync(paperId);

            _logger.LogInformation("删除试卷成功: {PaperId}", paperId);
            return result;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除试卷时发生错误: {PaperId}", paperId);
            return false;
        }
    }

    /// <summary>
    /// 检查试卷名称是否存在
    /// </summary>
    public async Task<bool> IsExamPaperNameExistsAsync(string name, int? excludePaperId = null)
    {
        try
        {
            var query = _context.ExamPapers.Where(p => p.Name == name);
            
            if (excludePaperId.HasValue)
            {
                query = query.Where(p => p.PaperId != excludePaperId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查试卷名称是否存在时发生错误: {Name}", name);
            return false;
        }
    }

    /// <summary>
    /// 获取试卷题目列表
    /// </summary>
    public async Task<List<PaperQuestion>> GetPaperQuestionsAsync(int paperId)
    {
        try
        {
            return await _context.PaperQuestions
                .Include(pq => pq.Question)
                .ThenInclude(q => q.Options)
                .Where(pq => pq.PaperId == paperId)
                .OrderBy(pq => pq.OrderIndex)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取试卷题目列表时发生错误: {PaperId}", paperId);
            return new List<PaperQuestion>();
        }
    }

    /// <summary>
    /// 添加题目到试卷
    /// </summary>
    public async Task<bool> AddQuestionsToExamPaperAsync(int paperId, List<int> questionIds)
    {
        try
        {
            // 获取当前试卷的最大顺序号
            var maxOrder = await _context.PaperQuestions
                .Where(pq => pq.PaperId == paperId)
                .MaxAsync(pq => (int?)pq.OrderIndex) ?? 0;

            var paperQuestions = new List<PaperQuestion>();
            for (int i = 0; i < questionIds.Count; i++)
            {
                // 检查题目是否已存在于试卷中
                var exists = await _context.PaperQuestions
                    .AnyAsync(pq => pq.PaperId == paperId && pq.QuestionId == questionIds[i]);

                if (!exists)
                {
                    var question = await _context.Questions.FindAsync(questionIds[i]);
                    if (question != null)
                    {
                        paperQuestions.Add(new PaperQuestion
                        {
                            PaperId = paperId,
                            QuestionId = questionIds[i],
                            OrderIndex = maxOrder + i + 1,
                            Score = question.Score
                        });
                    }
                }
            }

            if (paperQuestions.Any())
            {
                _context.PaperQuestions.AddRange(paperQuestions);
                var result = await _context.SaveChangesAsync();

                // 更新试卷总分
                await UpdateTotalScoreAsync(paperId);

                _logger.LogInformation("添加题目到试卷成功: {PaperId}, 题目数量: {Count}", paperId, paperQuestions.Count);
                return result > 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加题目到试卷时发生错误: {PaperId}", paperId);
            return false;
        }
    }

    /// <summary>
    /// 从试卷中移除题目
    /// </summary>
    public async Task<bool> RemoveQuestionsFromExamPaperAsync(int paperId, List<int> questionIds)
    {
        try
        {
            var paperQuestions = await _context.PaperQuestions
                .Where(pq => pq.PaperId == paperId && questionIds.Contains(pq.QuestionId))
                .ToListAsync();

            if (paperQuestions.Any())
            {
                _context.PaperQuestions.RemoveRange(paperQuestions);
                var result = await _context.SaveChangesAsync();

                // 更新试卷总分
                await UpdateTotalScoreAsync(paperId);

                _logger.LogInformation("从试卷中移除题目成功: {PaperId}, 题目数量: {Count}", paperId, paperQuestions.Count);
                return result > 0;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从试卷中移除题目时发生错误: {PaperId}", paperId);
            return false;
        }
    }

    /// <summary>
    /// 更新试卷题目顺序
    /// </summary>
    public async Task<bool> UpdateQuestionOrdersAsync(int paperId, List<(int QuestionId, int OrderIndex)> questionOrders)
    {
        try
        {
            foreach (var (questionId, orderIndex) in questionOrders)
            {
                var paperQuestion = await _context.PaperQuestions
                    .FirstOrDefaultAsync(pq => pq.PaperId == paperId && pq.QuestionId == questionId);

                if (paperQuestion != null)
                {
                    paperQuestion.OrderIndex = orderIndex;
                }
            }

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation("更新试卷题目顺序成功: {PaperId}", paperId);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新试卷题目顺序时发生错误: {PaperId}", paperId);
            return false;
        }
    }

    /// <summary>
    /// 更新试卷题目分值
    /// </summary>
    public async Task<bool> UpdateQuestionScoresAsync(int paperId, List<(int QuestionId, decimal Score)> questionScores)
    {
        try
        {
            foreach (var (questionId, score) in questionScores)
            {
                var paperQuestion = await _context.PaperQuestions
                    .FirstOrDefaultAsync(pq => pq.PaperId == paperId && pq.QuestionId == questionId);

                if (paperQuestion != null)
                {
                    paperQuestion.Score = score;
                }
            }

            var result = await _context.SaveChangesAsync();

            // 更新试卷总分
            await UpdateTotalScoreAsync(paperId);

            _logger.LogInformation("更新试卷题目分值成功: {PaperId}", paperId);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新试卷题目分值时发生错误: {PaperId}", paperId);
            return false;
        }
    }

    /// <summary>
    /// 计算试卷总分
    /// </summary>
    public async Task<decimal> CalculateTotalScoreAsync(int paperId)
    {
        try
        {
            return await _context.PaperQuestions
                .Where(pq => pq.PaperId == paperId)
                .SumAsync(pq => pq.Score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算试卷总分时发生错误: {PaperId}", paperId);
            return 0;
        }
    }

    /// <summary>
    /// 发布试卷
    /// </summary>
    public async Task<bool> PublishExamPaperAsync(int paperId)
    {
        try
        {
            var examPaper = await _context.ExamPapers.FindAsync(paperId);
            if (examPaper == null)
            {
                _logger.LogWarning("试卷不存在: {PaperId}", paperId);
                return false;
            }

            examPaper.Status = "已发布";
            examPaper.UpdatedAt = DateTime.Now;

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation("发布试卷成功: {PaperId}", paperId);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布试卷时发生错误: {PaperId}", paperId);
            return false;
        }
    }

    /// <summary>
    /// 取消发布试卷
    /// </summary>
    public async Task<bool> UnpublishExamPaperAsync(int paperId)
    {
        try
        {
            var examPaper = await _context.ExamPapers.FindAsync(paperId);
            if (examPaper == null)
            {
                _logger.LogWarning("试卷不存在: {PaperId}", paperId);
                return false;
            }

            examPaper.Status = "草稿";
            examPaper.UpdatedAt = DateTime.Now;

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation("取消发布试卷成功: {PaperId}", paperId);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消发布试卷时发生错误: {PaperId}", paperId);
            return false;
        }
    }

    /// <summary>
    /// 复制试卷
    /// </summary>
    public async Task<ExamPaper?> CopyExamPaperAsync(int paperId, string newName, int creatorId)
    {
        try
        {
            var originalPaper = await GetExamPaperByIdAsync(paperId);
            if (originalPaper == null)
            {
                _logger.LogWarning("源试卷不存在: {PaperId}", paperId);
                return null;
            }

            var newPaper = new ExamPaper
            {
                Name = newName,
                Description = originalPaper.Description,
                TotalScore = originalPaper.TotalScore,
                Duration = originalPaper.Duration,
                CreatorId = creatorId,
                Status = "草稿",
                IsActive = true,
                IsRandomOrder = originalPaper.IsRandomOrder,
                AllowViewAnswer = originalPaper.AllowViewAnswer,
                PassScore = originalPaper.PassScore,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.ExamPapers.Add(newPaper);
            await _context.SaveChangesAsync();

            // 复制题目
            var originalQuestions = await GetPaperQuestionsAsync(paperId);
            var newPaperQuestions = originalQuestions.Select(pq => new PaperQuestion
            {
                PaperId = newPaper.PaperId,
                QuestionId = pq.QuestionId,
                OrderIndex = pq.OrderIndex,
                Score = pq.Score
            }).ToList();

            if (newPaperQuestions.Any())
            {
                _context.PaperQuestions.AddRange(newPaperQuestions);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("复制试卷成功: {OriginalPaperId} -> {NewPaperId}", paperId, newPaper.PaperId);
            return newPaper;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制试卷时发生错误: {PaperId}", paperId);
            return null;
        }
    }

    /// <summary>
    /// 搜索试卷
    /// </summary>
    public async Task<List<ExamPaper>> SearchExamPapersAsync(string? keyword = null, int? creatorId = null, string? status = null)
    {
        try
        {
            var query = _context.ExamPapers
                .Include(p => p.Creator)
                .Include(p => p.PaperQuestions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword) || 
                                        (p.Description != null && p.Description.Contains(keyword)));
            }

            if (creatorId.HasValue)
            {
                query = query.Where(p => p.CreatorId == creatorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索试卷时发生错误");
            return new List<ExamPaper>();
        }
    }

    /// <summary>
    /// 更新试卷总分（私有方法）
    /// </summary>
    private async Task UpdateTotalScoreAsync(int paperId)
    {
        try
        {
            var totalScore = await CalculateTotalScoreAsync(paperId);
            var examPaper = await _context.ExamPapers.FindAsync(paperId);
            if (examPaper != null)
            {
                examPaper.TotalScore = totalScore;
                examPaper.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新试卷总分时发生错误: {PaperId}", paperId);
        }
    }
}}
