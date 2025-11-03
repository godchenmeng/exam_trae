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
using ExamSystem.Services.Models;
using Microsoft.Extensions.Logging;

namespace ExamSystem.Services.Services
{

/// <summary>
/// 考试服务实现
/// </summary>
public class ExamService : IExamService
{
    private readonly IExamRecordRepository _examRecordRepository;
    private readonly IExamPaperRepository _examPaperRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ExamService> _logger;
    private readonly ExamDbContext _context;

    public ExamService(
        IExamRecordRepository examRecordRepository,
        IExamPaperRepository examPaperRepository,
        IUserRepository userRepository,
        ILogger<ExamService> logger,
        ExamDbContext context)
    {
        _examRecordRepository = examRecordRepository;
        _examPaperRepository = examPaperRepository;
        _userRepository = userRepository;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 开始考试
    /// </summary>
    public async Task<ExamRecord> StartExamAsync(int userId, int paperId)
    {
        try
        {
            // 检查用户是否可以参加考试
            if (!await CanUserTakeExamAsync(userId, paperId))
            {
                throw new InvalidOperationException("用户不能参加此考试");
            }

            // 获取试卷信息
            var examPaper = await _examPaperRepository.GetPaperWithQuestionsAsync(paperId);

            if (examPaper == null)
            {
                throw new ArgumentException("试卷不存在", nameof(paperId));
            }

            // 创建考试记录
            var examRecord = new ExamRecord
            {
                UserId = userId,
                PaperId = paperId,
                StartTime = DateTime.Now,
                Status = ExamStatus.InProgress,
                TotalCount = examPaper.PaperQuestions != null ? examPaper.PaperQuestions.Count : 0,
                RemainingTime = examPaper.Duration * 60, // 转换为秒
                CreatedAt = DateTime.Now
            };

            await _examRecordRepository.AddAsync(examRecord);

            // 创建答题记录
            foreach (var paperQuestion in (examPaper.PaperQuestions ?? Enumerable.Empty<PaperQuestion>()).OrderBy(pq => pq.OrderIndex))
            {
                var answerRecord = new AnswerRecord
                {
                    RecordId = examRecord.RecordId,
                    QuestionId = paperQuestion.QuestionId,
                    Score = 0,
                    IsCorrect = false,
                    IsGraded = false
                };

                _context.AnswerRecords.Add(answerRecord);
            }

            await _context.SaveChangesAsync();

            // 关键：重新加载考试记录，包含题目及其选项，确保前端可用
            examRecord = await _context.ExamRecords
                .Include(r => r.ExamPaper)
                    .ThenInclude(p => p.PaperQuestions)
                .Include(r => r.AnswerRecords)
                    .ThenInclude(ar => ar.Question)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(r => r.RecordId == examRecord.RecordId) ?? examRecord;

            _logger.LogInformation("用户 {UserId} 开始考试，试卷ID: {PaperId}，记录ID: {RecordId}", 
                userId, paperId, examRecord.RecordId);

            return examRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始考试失败，用户ID: {UserId}，试卷ID: {PaperId}", userId, paperId);
            throw;
        }
    }

    /// <summary>
    /// 获取考试进度
    /// </summary>
    public async Task<ExamRecord?> GetExamProgressAsync(int recordId)
    {
        try
        {
            var examRecord = await _context.ExamRecords
                .Include(r => r.ExamPaper)
                .Include(r => r.User)
                .Include(r => r.AnswerRecords)
                    .ThenInclude(ar => ar.Question)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(r => r.RecordId == recordId);

            return examRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试进度失败，记录ID: {RecordId}", recordId);
            throw;
        }
    }

    /// <summary>
    /// 保存答案
    /// </summary>
    public async Task<bool> SaveAnswerAsync(int recordId, int questionId, string userAnswer)
    {
        try
        {
            var answerRecord = await _context.AnswerRecords
                .FirstOrDefaultAsync(ar => ar.RecordId == recordId && ar.QuestionId == questionId);

            if (answerRecord == null)
            {
                _logger.LogWarning("答题记录不存在，记录ID: {RecordId}，题目ID: {QuestionId}", recordId, questionId);
                return false;
            }

            answerRecord.UserAnswer = userAnswer;
            answerRecord.AnswerTime = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogDebug("保存答案成功，记录ID: {RecordId}，题目ID: {QuestionId}", recordId, questionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存答案失败，记录ID: {RecordId}，题目ID: {QuestionId}", recordId, questionId);
            return false;
        }
    }

    /// <summary>
    /// 保存地图绘制答案
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="questionId">题目ID</param>
    /// <param name="mapDrawingData">地图绘制数据JSON</param>
    /// <param name="mapCenter">地图中心点JSON</param>
    /// <param name="mapZoom">地图缩放级别</param>
    /// <returns>保存是否成功</returns>
    public async Task<bool> SaveMapDrawingAnswerAsync(int recordId, int questionId, string mapDrawingData, string? mapCenter = null, int? mapZoom = null)
    {
        try
        {
            var answerRecord = await _context.AnswerRecords
                .FirstOrDefaultAsync(ar => ar.RecordId == recordId && ar.QuestionId == questionId);

            if (answerRecord == null)
            {
                _logger.LogWarning("答题记录不存在，记录ID: {RecordId}，题目ID: {QuestionId}", recordId, questionId);
                return false;
            }

            // 保存地图绘制数据
            answerRecord.MapDrawingData = mapDrawingData;
            answerRecord.MapCenter = mapCenter;
            answerRecord.MapZoom = mapZoom;
            answerRecord.AnswerTime = DateTime.Now;

            // 同时将地图绘制数据作为用户答案保存（用于兼容性）
            answerRecord.UserAnswer = mapDrawingData;

            await _context.SaveChangesAsync();

            _logger.LogDebug("保存地图绘制答案成功，记录ID: {RecordId}，题目ID: {QuestionId}，数据长度: {DataLength}", 
                recordId, questionId, mapDrawingData?.Length ?? 0);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存地图绘制答案失败，记录ID: {RecordId}，题目ID: {QuestionId}", recordId, questionId);
            return false;
        }
    }

    /// <summary>
    /// 提交考试
    /// </summary>
    public async Task<bool> SubmitExamAsync(int recordId)
    {
        try
        {
            var examRecord = await _context.ExamRecords
                .FirstOrDefaultAsync(r => r.RecordId == recordId);

            if (examRecord == null)
            {
                _logger.LogWarning("考试记录不存在，记录ID: {RecordId}", recordId);
                return false;
            }

            if (examRecord.Status != ExamStatus.InProgress)
            {
                _logger.LogWarning("考试状态不正确，无法提交，记录ID: {RecordId}，状态: {Status}", recordId, examRecord.Status);
                return false;
            }

            // 更新考试记录状态
            examRecord.EndTime = DateTime.Now;
            examRecord.SubmitTime = DateTime.Now;
            examRecord.Status = ExamStatus.Submitted;

            await _context.SaveChangesAsync();

            // 自动评分客观题
            await AutoGradeObjectiveQuestionsAsync(recordId);

            _logger.LogInformation("考试提交成功，记录ID: {RecordId}", recordId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交考试失败，记录ID: {RecordId}", recordId);
            return false;
        }
    }

    /// <summary>
    /// 获取考试记录
    /// </summary>
    public async Task<ExamRecord?> GetExamRecordAsync(int recordId)
    {
        try
        {
            return await _context.ExamRecords
                .Include(r => r.ExamPaper)
                .Include(r => r.User)
                .Include(r => r.AnswerRecords)
                    .ThenInclude(ar => ar.Question)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(r => r.RecordId == recordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试记录失败，记录ID: {RecordId}", recordId);
            throw;
        }
    }

    /// <summary>
    /// 检查考试是否超时
    /// </summary>
    public async Task<bool> CheckExamTimeoutAsync(int recordId)
    {
        try
        {
            var examRecord = await _context.ExamRecords
                .Include(r => r.ExamPaper)
                .FirstOrDefaultAsync(r => r.RecordId == recordId);

            if (examRecord == null || examRecord.Status != ExamStatus.InProgress)
            {
                return false;
            }

            var startTime = examRecord.StartTime ?? DateTime.Now;
            var durationMinutes = examRecord.ExamPaper?.Duration ?? 0;
            var elapsedTime = DateTime.Now - startTime;
            var timeLimit = TimeSpan.FromMinutes(durationMinutes);

            if (elapsedTime >= timeLimit)
            {
                // 自动提交超时考试
                examRecord.Status = ExamStatus.Timeout;
                examRecord.EndTime = DateTime.Now;
                await _context.SaveChangesAsync();

                // 自动评分客观题
                await AutoGradeObjectiveQuestionsAsync(recordId);

                _logger.LogInformation("考试超时自动提交，记录ID: {RecordId}", recordId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查考试超时失败，记录ID: {RecordId}", recordId);
            throw;
        }
    }

    /// <summary>
    /// 继续考试
    /// </summary>
    public async Task<ExamRecord?> ResumeExamAsync(int recordId)
    {
        try
        {
            var examRecord = await GetExamProgressAsync(recordId);

            if (examRecord == null || examRecord.Status != ExamStatus.InProgress)
            {
                return null;
            }

            // 检查是否超时
            if (await CheckExamTimeoutAsync(recordId))
            {
                return await GetExamRecordAsync(recordId);
            }

            return examRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "继续考试失败，记录ID: {RecordId}", recordId);
            throw;
        }
    }

    /// <summary>
    /// 获取用户的考试记录列表
    /// </summary>
    public async Task<List<ExamRecord>> GetUserExamRecordsAsync(int userId, ExamStatus? status = null)
    {
        try
        {
            var query = _context.ExamRecords
                .Include(r => r.ExamPaper)
                .Where(r => r.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户考试记录失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取试卷的考试记录列表
    /// </summary>
    public async Task<List<ExamRecord>> GetPaperExamRecordsAsync(int paperId, ExamStatus? status = null)
    {
        try
        {
            var query = _context.ExamRecords
                .Include(r => r.User)
                .Include(r => r.ExamPaper)
                .Where(r => r.PaperId == paperId);

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取试卷考试记录失败，试卷ID: {PaperId}", paperId);
            throw;
        }
    }

    /// <summary>
    /// 获取答题记录
    /// </summary>
    public async Task<List<AnswerRecord>> GetAnswerRecordsAsync(int recordId)
    {
        try
        {
            return await _context.AnswerRecords
                .Include(ar => ar.Question)
                    .ThenInclude(q => q.Options)
                .Where(ar => ar.RecordId == recordId)
                .OrderBy(ar => ar.Question.QuestionId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取答题记录失败，记录ID: {RecordId}", recordId);
            throw;
        }
    }

    /// <summary>
    /// 自动评分客观题
    /// </summary>
    public async Task<bool> AutoGradeObjectiveQuestionsAsync(int recordId)
    {
        try
        {
            var answerRecords = await _context.AnswerRecords
                .Include(ar => ar.Question)
                .Where(ar => ar.RecordId == recordId && !ar.IsGraded)
                .ToListAsync();

            var examRecord = await _context.ExamRecords
                .Include(r => r.ExamPaper)
                .ThenInclude(p => p.PaperQuestions)
                .FirstOrDefaultAsync(r => r.RecordId == recordId);

            if (examRecord == null)
            {
                return false;
            }

            int correctCount = 0;
            decimal objectiveScore = 0;

            foreach (var answerRecord in answerRecords)
            {
                var question = answerRecord.Question;
                
                // 只评分客观题
                if (IsObjectiveQuestion(question.QuestionType))
                {
                    var paperQuestion = examRecord.ExamPaper.PaperQuestions
                        .FirstOrDefault(pq => pq.QuestionId == question.QuestionId);

                    if (paperQuestion != null)
                    {
                        bool isCorrect = CompareAnswers(answerRecord.UserAnswer, question.Answer, question.QuestionType);
                        
                        answerRecord.IsCorrect = isCorrect;
                        answerRecord.Score = isCorrect ? paperQuestion.Score : 0;
                        answerRecord.IsGraded = true;
                        answerRecord.GradeTime = DateTime.Now;

                        if (isCorrect)
                        {
                            correctCount++;
                            objectiveScore += answerRecord.Score;
                        }
                    }
                }
            }

            // 更新考试记录
            examRecord.CorrectCount = correctCount;
            examRecord.ObjectiveScore = objectiveScore;
            examRecord.TotalScore = objectiveScore + examRecord.SubjectiveScore;

            // 检查是否所有题目都已评分
            var allAnswersGraded = await _context.AnswerRecords
                .Where(ar => ar.RecordId == recordId)
                .AllAsync(ar => ar.IsGraded);

            if (allAnswersGraded)
            {
                examRecord.Status = ExamStatus.Graded;
                examRecord.GradedAt = DateTime.Now;
                
                // 计算是否通过
                var passScore = examRecord.ExamPaper.PassScore > 0 ? examRecord.ExamPaper.PassScore : (examRecord.ExamPaper.TotalScore * 0.6m);
                examRecord.IsPassed = examRecord.TotalScore >= passScore;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("自动评分完成，记录ID: {RecordId}，客观题得分: {ObjectiveScore}", recordId, objectiveScore);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动评分失败，记录ID: {RecordId}", recordId);
            return false;
        }
    }

    /// <summary>
    /// 计算考试总分
    /// </summary>
    public async Task<decimal> CalculateTotalScoreAsync(int recordId)
    {
        try
        {
            var answerRecords = await _context.AnswerRecords
                .Where(ar => ar.RecordId == recordId && ar.IsGraded)
                .ToListAsync();

            return answerRecords.Sum(ar => ar.Score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算总分失败，记录ID: {RecordId}", recordId);
            throw;
        }
    }

    /// <summary>
    /// 更新剩余时间
    /// </summary>
    public async Task<bool> UpdateRemainingTimeAsync(int recordId, int remainingTime)
    {
        try
        {
            var examRecord = await _context.ExamRecords
                .FirstOrDefaultAsync(r => r.RecordId == recordId);

            if (examRecord == null)
            {
                return false;
            }

            examRecord.RemainingTime = remainingTime;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新剩余时间失败，记录ID: {RecordId}", recordId);
            return false;
        }
    }

    /// <summary>
    /// 检查用户是否可以参加考试
    /// </summary>
    public async Task<bool> CanUserTakeExamAsync(int userId, int paperId)
    {
        var result = await ValidateUserExamEligibilityAsync(userId, paperId);
        return result.IsValid;
    }

    /// <summary>
    /// 验证用户考试资格（详细版本）
    /// </summary>
    public async Task<ExamValidationResult> ValidateUserExamEligibilityAsync(int userId, int paperId)
    {
        _logger.LogInformation("开始检查用户考试资格 - UserId: {UserId}, PaperId: {PaperId}", userId, paperId);
        
        // 检查试卷是否存在
        var paper = await _context.ExamPapers
            .Include(p => p.PaperQuestions)
            .ThenInclude(pq => pq.Question)
            .FirstOrDefaultAsync(p => p.PaperId == paperId);
        
        if (paper == null)
        {
            _logger.LogWarning("试卷不存在 - PaperId: {PaperId}", paperId);
            return ExamValidationResult.Failure("试卷不存在，请联系管理员。", ExamValidationErrorType.PaperNotFound);
        }
        
        _logger.LogInformation("试卷信息 - Name: {Name}, IsPublished: {IsPublished}, Status: {Status}", 
            paper.Name, paper.IsPublished, paper.Status);
    
        // 检查试卷是否已发布（兼容两种标识方式）
        bool isPublished = paper.IsPublished || paper.Status == "已发布";
        if (!isPublished)
        {
            _logger.LogWarning("试卷未发布 - PaperId: {PaperId}, IsPublished: {IsPublished}, Status: {Status}", 
                paperId, paper.IsPublished, paper.Status);
            return ExamValidationResult.Failure("试卷尚未发布，暂时无法参加考试。", ExamValidationErrorType.PaperNotPublished);
        }
        
        _logger.LogInformation("试卷发布状态检查通过");
    
        // 检查考试时间窗口
        var now = DateTime.Now;
        if (paper.StartTime.HasValue && now < paper.StartTime.Value)
        {
            _logger.LogWarning("考试尚未开始 - 当前时间: {Now}, 开始时间: {StartTime}", 
                now, paper.StartTime.Value);
            return ExamValidationResult.Failure(
                $"考试尚未开始，开始时间为：{paper.StartTime.Value:yyyy-MM-dd HH:mm:ss}。", 
                ExamValidationErrorType.ExamNotStarted);
        }
    
        if (paper.EndTime.HasValue && now > paper.EndTime.Value)
        {
            _logger.LogWarning("考试已结束 - 当前时间: {Now}, 结束时间: {EndTime}", 
                now, paper.EndTime.Value);
            return ExamValidationResult.Failure(
                $"考试已结束，结束时间为：{paper.EndTime.Value:yyyy-MM-dd HH:mm:ss}。", 
                ExamValidationErrorType.ExamEnded);
        }
        
        _logger.LogInformation("考试时间窗口检查通过 - 开始时间: {StartTime}, 结束时间: {EndTime}", 
            paper.StartTime, paper.EndTime);
    
        // 检查用户是否已参加过考试
        var existingRecord = await _context.ExamRecords
            .FirstOrDefaultAsync(r => r.UserId == userId && r.PaperId == paperId);
    
        if (existingRecord != null)
        {
            _logger.LogInformation("找到现有考试记录 - RecordId: {RecordId}, Status: {Status}, AllowRetake: {AllowRetake}", 
                existingRecord.RecordId, existingRecord.Status, paper.AllowRetake);
            
            // 如果不允许重考且已完成考试，则不能再次参加
            if (!paper.AllowRetake && existingRecord.Status == ExamStatus.Completed)
            {
                _logger.LogWarning("不允许重考且用户已完成考试 - UserId: {UserId}, PaperId: {PaperId}", 
                    userId, paperId);
                return ExamValidationResult.Failure("您已完成此考试，不允许重复参加。", ExamValidationErrorType.RetakeNotAllowed);
            }
        
            // 如果有进行中的考试，允许继续
            if (existingRecord.Status == ExamStatus.InProgress)
            {
                _logger.LogInformation("用户有进行中的考试，允许继续 -RecordId: {RecordId}", 
                    existingRecord.RecordId);
                return ExamValidationResult.Success();
            }
        }
        else
        {
            _logger.LogInformation("未找到现有考试记录，用户可以开始新考试");
        }
    
        _logger.LogInformation("用户考试资格检查通过 - UserId: {UserId}, PaperId: {PaperId}", userId, paperId);
        return ExamValidationResult.Success();
    }

    /// <summary>
    /// 获取考试统计信息
    /// </summary>
    public async Task<ExamStatistics> GetExamStatisticsAsync(int paperId)
    {
        try
        {
            var examRecords = await _context.ExamRecords
                .Where(r => r.PaperId == paperId)
                .ToListAsync();

            var completedRecords = examRecords
                .Where(r => r.Status == ExamStatus.Graded || r.Status == ExamStatus.Completed)
                .ToList();

            var examPaper = await _context.ExamPapers
                .FirstOrDefaultAsync(p => p.PaperId == paperId);

            var passScore = examPaper?.PassScore ?? (examPaper?.TotalScore * 0.6m ?? 60);
            var passedRecords = completedRecords.Where(r => r.TotalScore >= passScore).ToList();

            var statistics = new ExamStatistics
            {
                TotalParticipants = examRecords.Count,
                CompletedCount = completedRecords.Count,
                PassedCount = passedRecords.Count,
                AverageScore = completedRecords.Any() ? completedRecords.Average(r => r.TotalScore) : 0,
                MaxScore = completedRecords.Any() ? completedRecords.Max(r => r.TotalScore) : 0,
                MinScore = completedRecords.Any() ? completedRecords.Min(r => r.TotalScore) : 0,
                PassRate = completedRecords.Any() ? (decimal)passedRecords.Count / completedRecords.Count * 100 : 0
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试统计失败，试卷ID: {PaperId}", paperId);
            throw;
        }
    }

    #region Private Methods

    /// <summary>
    /// 判断是否为客观题
    /// </summary>
    private static bool IsObjectiveQuestion(QuestionType questionType)
    {
        return questionType == QuestionType.SingleChoice ||
               questionType == QuestionType.MultipleChoice ||
               questionType == QuestionType.TrueFalse;
        // 注意：填空题现在被归类为主观题，需要人工评分
    }

    /// <summary>
    /// 比较答案
    /// </summary>
    private static bool CompareAnswers(string? userAnswer, string correctAnswer, QuestionType questionType)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
        {
            return false;
        }

        userAnswer = userAnswer.Trim();
        correctAnswer = correctAnswer.Trim();

        switch (questionType)
        {
            case QuestionType.SingleChoice:
            case QuestionType.TrueFalse:
                return string.Equals(userAnswer, correctAnswer, StringComparison.OrdinalIgnoreCase);

            case QuestionType.MultipleChoice:
                // 多选题需要完全匹配所有选项
                var userOptions = userAnswer.Split(',').Select(s => s.Trim()).OrderBy(s => s).ToArray();
                var correctOptions = correctAnswer.Split(',').Select(s => s.Trim()).OrderBy(s => s).ToArray();
                return userOptions.SequenceEqual(correctOptions, StringComparer.OrdinalIgnoreCase);

            case QuestionType.FillInBlank:
                // 填空题支持多个答案，用|分隔
                var acceptableAnswers = correctAnswer.Split('|').Select(s => s.Trim());
                return acceptableAnswers.Any(answer => 
                    string.Equals(userAnswer, answer, StringComparison.OrdinalIgnoreCase));

            default:
                return false;
        }
    }

    #endregion

    #region 学生相关服务方法

    /// <summary>
    /// 获取学生可参加的考试列表
    /// </summary>
    public async Task<List<StudentExamInfo>> GetAvailableExamsForStudentAsync(int userId)
    {
        try
        {
            var currentTime = DateTime.Now;
            
            // 获取用户信息
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("用户不存在", nameof(userId));
            }

            // 获取可参加的考试（根据用户角色和考试状态）
            var availableExams = await _context.ExamPapers
                .Where(ep => (ep.IsPublished || ep.Status == "已发布") && 
                           ep.StartTime <= currentTime && 
                           ep.EndTime >= currentTime)
                .Select(ep => new StudentExamInfo
                {
                    PaperId = ep.PaperId,
                    Title = ep.Name,
                    Subject = "通用", // 暂时设置为通用，需要根据实际业务调整
                    Duration = ep.Duration,
                    TotalQuestions = ep.PaperQuestions != null ? ep.PaperQuestions.Count : 0,
                    StartTime = ep.StartTime ?? DateTime.Now,
                    EndTime = ep.EndTime ?? DateTime.Now.AddDays(30),
                    Description = ep.Description ?? string.Empty,
                    // 检查是否已参加过考试
                    HasTaken = _context.ExamRecords.Any(er => er.UserId == userId && er.PaperId == ep.PaperId)
                })
                .OrderBy(e => e.StartTime)
                .ToListAsync();

            return availableExams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可参加考试列表失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取学生考试结果列表（只返回已交卷试卷）
    /// </summary>
    public async Task<List<StudentExamResult>> GetStudentExamResultsAsync(int userId, string? searchKeyword = null, 
        string? subjectFilter = null, string? timeRangeFilter = null)
    {
        try
        {
            // 只返回已交卷的试卷：Submitted（已提交）、Completed（已完成）、Graded（已评分）
            // 为避免使用长生命周期 DbContext 的跟踪缓存导致数据不实时（例如得分一直为 0），在查询前清空跟踪器，并使用 AsNoTracking。
            _context.ChangeTracker.Clear();

            var query = _context.ExamRecords
                .AsNoTracking()
                .Include(er => er.ExamPaper)
                .ThenInclude(ep => ep.PaperQuestions)
                .Where(er => er.UserId == userId && 
                            (er.Status == ExamStatus.Submitted || 
                             er.Status == ExamStatus.Completed || 
                             er.Status == ExamStatus.Graded));

            // 按搜索关键词筛选
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query = query.Where(er => er.ExamPaper != null &&
                                        ((er.ExamPaper.Name ?? string.Empty).Contains(searchKeyword) ||
                                         (er.ExamPaper.Description ?? string.Empty).Contains(searchKeyword)));
            }

            // 按科目筛选
            if (!string.IsNullOrEmpty(subjectFilter) && subjectFilter != "全部")
            {
                // 由于ExamPaper没有Subject属性，暂时跳过科目筛选
                // query = query.Where(er => er.ExamPaper.Subject == subjectFilter);
            }

            // 按时间范围筛选
            if (!string.IsNullOrEmpty(timeRangeFilter) && timeRangeFilter != "全部")
            {
                var now = DateTime.Now;
                DateTime? startDate = null;

                switch (timeRangeFilter)
                {
                    case "最近一周":
                        startDate = now.AddDays(-7);
                        break;
                    case "最近一月":
                        startDate = now.AddMonths(-1);
                        break;
                    case "最近三月":
                        startDate = now.AddMonths(-3);
                        break;
                    case "最近半年":
                        startDate = now.AddMonths(-6);
                        break;
                    case "最近一年":
                        startDate = now.AddYears(-1);
                        break;
                }

                if (startDate.HasValue)
                {
                    query = query.Where(er => er.StartTime >= startDate.Value);
                }
            }

            // 先拉取到内存，再计算用时与评分状态
            var examRecords = await query
                .OrderByDescending(er => er.StartTime)
                .ToListAsync();

            var examResults = examRecords
                .Select(er =>
                {
                    int durationMinutes = 0;
                    if (er.EndTime.HasValue && er.StartTime.HasValue)
                    {
                        durationMinutes = (int)(er.EndTime.Value - er.StartTime.Value).TotalMinutes;
                    }
                    else if (er.StartTime.HasValue)
                    {
                        // 使用试卷时长减去剩余时间来估算已用时，避免出现始终为0的情况
                        var used = (er.ExamPaper?.Duration ?? 0) - (er.RemainingTime > 0 ? er.RemainingTime / 60 : 0);
                        if (used < 0) used = 0;
                        durationMinutes = used;
                    }

                    return new StudentExamResult
                    {
                        RecordId = er.RecordId,
                        ExamTitle = er.ExamPaper?.Name ?? string.Empty,
                        Subject = "通用", // 暂时使用固定值，后续可根据需要调整
                        ExamDate = er.StartTime ?? DateTime.MinValue,
                        Duration = er.StartTime.HasValue ? $"{durationMinutes}分钟" : "未完成",
                        QuestionCount = er.ExamPaper?.PaperQuestions != null ? er.ExamPaper.PaperQuestions.Count : 0,
                        TotalScore = er.ExamPaper?.PaperQuestions != null ? er.ExamPaper.PaperQuestions.Sum(pq => pq.Score) : 0,
                        Score = er.TotalScore,
                        // 状态基于评分情况：Graded=已评分，或存在评分时间/评分者则视为已评分
                        Status = (er.Status == ExamStatus.Graded || er.GradedAt.HasValue || er.GraderId.HasValue) ? "已评分" : "未评分",
                        HasWrongAnswers = er.CorrectCount < er.TotalCount
                    };
                })
                .OrderByDescending(r => r.ExamDate)
                .ToList();

            return examResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生考试结果列表失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取考试结果详情
    /// </summary>
    public async Task<ExamResultDetail> GetExamResultDetailAsync(int recordId)
    {
        try
        {
            var examRecord = await _context.ExamRecords
                .Include(er => er.ExamPaper)
                .ThenInclude(ep => ep.PaperQuestions)
                .ThenInclude(pq => pq.Question)
                .ThenInclude(q => q.Options)
                .Include(er => er.AnswerRecords)
                .FirstOrDefaultAsync(er => er.RecordId == recordId);

            if (examRecord == null)
            {
                throw new ArgumentException($"考试记录不存在: {recordId}");
            }

            var examDetail = new ExamResultDetail
            {
                RecordId = examRecord.RecordId,
                ExamTitle = examRecord.ExamPaper?.Name ?? string.Empty,
                ExamDate = examRecord.StartTime ?? DateTime.MinValue,
                Duration = examRecord.EndTime.HasValue && examRecord.StartTime.HasValue ? 
                    $"{(int)(examRecord.EndTime.Value - examRecord.StartTime.Value).TotalMinutes}分钟" : "未完成",
                TotalQuestions = examRecord.TotalCount,
                Status = examRecord.Status == ExamStatus.Completed ? "已完成" : "进行中",
                TotalScore = examRecord.ExamPaper?.PaperQuestions != null ? examRecord.ExamPaper.PaperQuestions.Sum(pq => pq.Score) : 0,
                Score = examRecord.TotalScore,
                CorrectCount = examRecord.CorrectCount,
                WrongCount = examRecord.TotalCount - examRecord.CorrectCount,
                TeacherComment = null, // ExamRecord中没有TeacherComment属性，暂时设为null
                QuestionDetails = new List<QuestionDetail>()
            };

            // 构建题目详情
            var paperQuestions = (examRecord.ExamPaper?.PaperQuestions ?? new List<PaperQuestion>()).OrderBy(pq => pq.OrderIndex);
            foreach (var paperQuestion in paperQuestions)
            {
                var answerRecord = examRecord.AnswerRecords.FirstOrDefault(ar => ar.QuestionId == paperQuestion.QuestionId);
                var question = paperQuestion.Question;

                var questionDetail = new QuestionDetail
                {
                    QuestionNumber = paperQuestion.OrderIndex,
                    QuestionType = GetQuestionTypeDisplayName(question.QuestionType),
                    Score = paperQuestion.Score,
                    EarnedScore = answerRecord?.Score ?? 0,
                    Content = question.Content,
                    CorrectAnswer = question.Answer,
                    StudentAnswer = answerRecord?.UserAnswer ?? string.Empty,
                    Explanation = question.Analysis,
                    Options = question.Options.Select(o => new OptionDetail
                    {
                        Text = o.Content,
                        IsCorrect = o.IsCorrect,
                        IsStudentAnswer = !string.IsNullOrEmpty(answerRecord?.UserAnswer) && 
                                        answerRecord.UserAnswer.Contains(o.OptionLabel)
                    }).ToList()
                };

                examDetail.QuestionDetails.Add(questionDetail);
            }

            return examDetail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试结果详情失败，记录ID: {RecordId}", recordId);
            throw;
        }
    }

    /// <summary>
    /// 获取题目类型显示名称
    /// </summary>
    private static string GetQuestionTypeDisplayName(QuestionType questionType)
    {
        return questionType switch
        {
            QuestionType.SingleChoice => "单选题",
            QuestionType.MultipleChoice => "多选题",
            QuestionType.TrueFalse => "判断题",
            QuestionType.FillInBlank => "填空题",
            QuestionType.Essay => "问答题",
            QuestionType.MapDrawing => "地图绘制题",
            _ => "未知类型"
        };
    }

    #endregion

    // ... existing code ...

    // 新增：搜索考试记录（教师侧）
    public async Task<List<ExamRecord>> SearchExamRecordsAsync(string keyword, int? userId = null, int? paperId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var results = await _examRecordRepository.SearchExamRecordsAsync(keyword, userId, paperId, startDate, endDate);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索考试记录失败，关键词: {Keyword}", keyword);
            throw;
        }
    }

    // 新增：主观题人工评分（教师侧）
    public async Task<bool> GradeSubjectiveAnswerAsync(int recordId, int questionId, decimal score, string? comment, int graderId)
    {
        try
        {
            // 获取答题记录
            var answerRecord = await _examRecordRepository.GetAnswerRecordAsync(recordId, questionId);
            if (answerRecord == null)
            {
                _logger.LogWarning("未找到答题记录，RecordId={RecordId}, QuestionId={QuestionId}", recordId, questionId);
                return false;
            }

            // 更新评分信息
            answerRecord.Score = score;
            answerRecord.Comment = comment;
            answerRecord.IsGraded = true;
            answerRecord.GradeTime = DateTime.Now;
            answerRecord.GraderId = graderId;

            await _examRecordRepository.UpdateAnswerRecordAsync(answerRecord);

            // 重新计算总分
            var total = await CalculateTotalScoreAsync(recordId);

            // 如果全部题目都已评分，则更新考试状态为 Graded
            var examRecord = await _examRecordRepository.GetRecordWithAnswersAsync(recordId);
            if (examRecord != null)
            {
                // 记录最后评分教师
                examRecord.GraderId = graderId;

                var allGraded = examRecord.AnswerRecords.All(ar => ar.IsGraded);
                // 同步更新主观题分数，便于统计与展示
                var subjectiveScore = examRecord.AnswerRecords
                    .Where(ar => ar.IsGraded && ar.Question != null && !IsObjectiveQuestion(ar.Question.QuestionType))
                    .Sum(ar => ar.Score);

                examRecord.TotalScore = total;
                examRecord.SubjectiveScore = subjectiveScore;
                if (allGraded)
                {
                    examRecord.Status = ExamStatus.Graded;
                    examRecord.GradedAt = DateTime.Now;
                    var passScore = (examRecord.ExamPaper?.PassScore ?? 0) > 0
                        ? examRecord.ExamPaper!.PassScore
                        : (examRecord.ExamPaper!.TotalScore * 0.6m);
                    examRecord.IsPassed = examRecord.TotalScore >= passScore;
                }
                await _examRecordRepository.UpdateAsync(examRecord);
            }

            _logger.LogInformation("人工评分成功：RecordId={RecordId}, QuestionId={QuestionId}, Score={Score}", recordId, questionId, score);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "人工评分失败：RecordId={RecordId}, QuestionId={QuestionId}", recordId, questionId);
            return false;
        }
    }

    // 新增：根据 AnswerRecords 回写/同步 ExamRecords（总分、主观分、状态）
    public async Task<bool> SyncExamRecordFromAnswerRecordsAsync(int recordId)
    {
        try
        {
            // 读取考试记录及答题记录
            var examRecord = await _examRecordRepository.GetRecordWithAnswersAsync(recordId);
            if (examRecord == null)
            {
                _logger.LogWarning("Sync失败，未找到考试记录，RecordId={RecordId}", recordId);
                return false;
            }

            // 重新计算客观题与主观题分数
            decimal objectiveScore = 0m;
            decimal subjectiveScore = 0m;
            int correctCount = 0;

            foreach (var ar in examRecord.AnswerRecords)
            {
                var type = ar.Question?.QuestionType ?? QuestionType.SingleChoice;
                var isObjective = IsObjectiveQuestion(type);

                if (ar.IsGraded)
                {
                    if (isObjective)
                    {
                        objectiveScore += ar.Score;
                        if (ar.IsCorrect) correctCount++;
                    }
                    else
                    {
                        subjectiveScore += ar.Score;
                    }
                }
            }

            var total = objectiveScore + subjectiveScore;
            examRecord.ObjectiveScore = objectiveScore;
            examRecord.SubjectiveScore = subjectiveScore;
            examRecord.TotalScore = total;
            examRecord.CorrectCount = correctCount;

            // 只要所有题目都已评分，则设置为已评分
            var allGraded = examRecord.AnswerRecords.All(x => x.IsGraded);
            if (allGraded)
            {
                examRecord.Status = ExamStatus.Graded;
                examRecord.GradedAt = examRecord.GradedAt ?? DateTime.Now;

                // 计算是否通过
                var passScore = (examRecord.ExamPaper?.PassScore ?? 0) > 0
                    ? examRecord.ExamPaper!.PassScore
                    : (examRecord.ExamPaper!.TotalScore * 0.6m);
                examRecord.IsPassed = examRecord.TotalScore >= passScore;
            }

            await _examRecordRepository.UpdateAsync(examRecord);
            _logger.LogInformation("已同步考试总表：RecordId={RecordId}，Objective={Objective}，Subjective={Subjective}，Total={Total}，AllGraded={AllGraded}", recordId, objectiveScore, subjectiveScore, total, allGraded);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步考试总表失败：RecordId={RecordId}", recordId);
            return false;
        }
    }
}
}
