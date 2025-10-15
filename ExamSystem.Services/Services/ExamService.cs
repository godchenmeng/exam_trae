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
                TotalCount = examPaper.PaperQuestions.Count,
                RemainingTime = examPaper.Duration * 60, // 转换为秒
                CreatedAt = DateTime.Now
            };

            await _examRecordRepository.AddAsync(examRecord);

            // 创建答题记录
            foreach (var paperQuestion in examPaper.PaperQuestions.OrderBy(pq => pq.OrderIndex))
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

            var elapsedTime = DateTime.Now - examRecord.StartTime.Value;
            var timeLimit = TimeSpan.FromMinutes(examRecord.ExamPaper.Duration);

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
        try
        {
            // 检查试卷是否存在且已发布
            var examPaper = await _examPaperRepository.GetByIdAsync(paperId);

            if (examPaper == null || examPaper.Status != "已发布")
            {
                return false;
            }

            // 检查考试时间
            var now = DateTime.Now;
            if (examPaper.StartTime.HasValue && now < examPaper.StartTime.Value)
            {
                return false; // 考试未开始
            }

            if (examPaper.EndTime.HasValue && now > examPaper.EndTime.Value)
            {
                return false; // 考试已结束
            }

            // 检查用户是否已经参加过考试
            var existingRecord = await _examRecordRepository.GetLatestRecordByUserAsync(userId, paperId);

            // 如果允许重考或者没有考试记录，则可以参加
            if (existingRecord == null)
            {
                return true;
            }

            // 检查是否允许重考
            if (examPaper.AllowRetake && existingRecord.Status != ExamStatus.InProgress)
            {
                return true;
            }

            // 如果有进行中的考试，可以继续
            if (existingRecord.Status == ExamStatus.InProgress)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户考试权限失败，用户ID: {UserId}，试卷ID: {PaperId}", userId, paperId);
            return false;
        }
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
               questionType == QuestionType.TrueFalse ||
               questionType == QuestionType.FillInBlank;
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
}}
