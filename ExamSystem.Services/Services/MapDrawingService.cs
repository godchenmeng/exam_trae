using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Domain.Models;
using ExamSystem.Domain.DTOs;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Interfaces;
using ExamSystem.Data;

namespace ExamSystem.Services.Services
{
    /// <summary>
    /// 地图绘制题服务实现
    /// </summary>
    public class MapDrawingService : IMapDrawingService
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IRepository<AnswerRecord> _answerRecordRepository;
        private readonly IRepository<ExamRecord> _examRecordRepository;
        private readonly IRepository<User> _userRepository;
        private readonly ILogger<MapDrawingService> _logger;
        private readonly ExamDbContext _context;

        public MapDrawingService(
            IQuestionRepository questionRepository,
            IRepository<AnswerRecord> answerRecordRepository,
            IRepository<ExamRecord> examRecordRepository,
            IRepository<User> userRepository,
            ILogger<MapDrawingService> logger,
            ExamDbContext context)
        {
            _questionRepository = questionRepository;
            _answerRecordRepository = answerRecordRepository;
            _examRecordRepository = examRecordRepository;
            _userRepository = userRepository;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// 创建地图绘制题
        /// </summary>
        public async Task<Question> CreateMapDrawingQuestionAsync(
            int bankId,
            string title,
            string content,
            MapDrawingConfig config,
            List<OverlayDTO>? guidanceOverlays = null,
            List<OverlayDTO>? referenceOverlays = null,
            ReviewRubric? reviewRubric = null,
            BuildingLayersConfig? buildingLayersConfig = null,
            int timeLimitSeconds = 0,
            decimal score = 10m)
        {
            try
            {
                var question = new Question
                {
                    BankId = bankId,
                    QuestionType = QuestionType.MapDrawing,
                    Title = title,
                    Content = content,
                    Answer = "地图绘制题（参考答案见ReferenceOverlaysJson）",
                    Score = score,
                    TimeLimitSeconds = timeLimitSeconds,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 设置配置
                if (_questionRepository is QuestionRepository repo)
                {
                    repo.SetMapDrawingConfig(question, config);

                    if (guidanceOverlays != null)
                        repo.SetGuidanceOverlays(question, guidanceOverlays);

                    if (referenceOverlays != null)
                        repo.SetReferenceOverlays(question, referenceOverlays);

                    if (reviewRubric != null)
                        repo.SetReviewRubric(question, reviewRubric);

                    if (buildingLayersConfig != null)
                        repo.SetBuildingLayersConfig(question, buildingLayersConfig);
                }

                var result = await _questionRepository.AddAsync(question);
                _logger.LogInformation($"创建地图绘制题成功，题目ID: {result.QuestionId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建地图绘制题失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 更新地图绘制题配置
        /// </summary>
        public async Task<bool> UpdateMapDrawingConfigAsync(int questionId, MapDrawingConfig config)
        {
            try
            {
                var question = await _questionRepository.GetByIdAsync(questionId);
                if (question == null || question.QuestionType != QuestionType.MapDrawing)
                    return false;

                if (_questionRepository is QuestionRepository repo)
                {
                    repo.SetMapDrawingConfig(question, config);
                    question.UpdatedAt = DateTime.Now;
                    await _questionRepository.UpdateAsync(question);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新地图绘制题配置失败，题目ID: {questionId}");
                return false;
            }
        }

        /// <summary>
        /// 更新地图绘制题
        /// </summary>
        public async Task<bool> UpdateMapDrawingQuestionAsync(int questionId, MapDrawingQuestionDto questionDto)
        {
            try
            {
                var question = await _questionRepository.GetByIdAsync(questionId);
                if (question == null || question.QuestionType != QuestionType.MapDrawing)
                    return false;

                // 更新基本信息
                question.Title = questionDto.Title;
                question.Content = questionDto.Content;
                question.Score = questionDto.Score;
                question.TimeLimitSeconds = questionDto.TimeLimitSeconds;
                question.UpdatedAt = DateTime.Now;

                if (_questionRepository is QuestionRepository repo)
                {
                    // 更新地图配置
                    if (questionDto.Config != null)
                        repo.SetMapDrawingConfig(question, questionDto.Config);

                    // 更新引导图层
                    if (questionDto.GuidanceOverlays != null)
                        repo.SetGuidanceOverlays(question, questionDto.GuidanceOverlays);

                    // 更新参考答案
                    if (questionDto.ReferenceOverlays != null)
                        repo.SetReferenceOverlays(question, questionDto.ReferenceOverlays);

                    // 更新评分量表
                    if (questionDto.ReviewRubric != null)
                        repo.SetReviewRubric(question, questionDto.ReviewRubric);

                    // 更新建筑图层配置
                    if (questionDto.BuildingLayersConfig != null)
                        repo.SetBuildingLayersConfig(question, questionDto.BuildingLayersConfig);
                }

                await _questionRepository.UpdateAsync(question);
                _logger.LogInformation($"更新地图绘制题成功，题目ID: {questionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新地图绘制题失败，题目ID: {questionId}");
                return false;
            }
        }

        /// <summary>
        /// 获取地图绘制题详情（学生端）
        /// </summary>
        public async Task<MapDrawingQuestionDto?> GetMapDrawingQuestionForStudentAsync(int questionId)
        {
            try
            {
                var question = await _questionRepository.GetByIdAsync(questionId);
                if (question == null || question.QuestionType != QuestionType.MapDrawing)
                    return null;

                var dto = new MapDrawingQuestionDto
                {
                    QuestionId = question.QuestionId,
                    Title = question.Title,
                    Content = question.Content,
                    Score = question.Score,
                    TimeLimitSeconds = question.TimeLimitSeconds
                };

                if (_questionRepository is QuestionRepository repo)
                {
                    dto.Config = repo.GetMapDrawingConfig(question);
                    dto.GuidanceOverlays = repo.GetGuidanceOverlays(question);
                    dto.BuildingLayersConfig = repo.GetBuildingLayersConfig(question);
                    // 学生端不返回参考答案和评分量表
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取学生端地图绘制题失败，题目ID: {questionId}");
                return null;
            }
        }

        /// <summary>
        /// 获取地图绘制题详情（教师端）
        /// </summary>
        public async Task<MapDrawingQuestionDto?> GetMapDrawingQuestionForTeacherAsync(int questionId)
        {
            try
            {
                var question = await _questionRepository.GetByIdAsync(questionId);
                if (question == null || question.QuestionType != QuestionType.MapDrawing)
                    return null;

                var dto = new MapDrawingQuestionDto
                {
                    QuestionId = question.QuestionId,
                    Title = question.Title,
                    Content = question.Content,
                    Score = question.Score,
                    TimeLimitSeconds = question.TimeLimitSeconds
                };

                if (_questionRepository is QuestionRepository repo)
                {
                    dto.Config = repo.GetMapDrawingConfig(question);
                    dto.GuidanceOverlays = repo.GetGuidanceOverlays(question);
                    dto.ReferenceOverlays = repo.GetReferenceOverlays(question);
                    dto.ReviewRubric = repo.GetReviewRubric(question);
                    dto.BuildingLayersConfig = repo.GetBuildingLayersConfig(question);
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取教师端地图绘制题失败，题目ID: {questionId}");
                return null;
            }
        }

        /// <summary>
        /// 实时保存地图绘制数据
        /// </summary>
        public async Task<SaveMapDrawingResponse> SaveMapDrawingDataAsync(SaveMapDrawingRequest request)
        {
            try
            {
                // 验证答题记录是否存在
                var answerRecord = await _answerRecordRepository.GetByIdAsync(request.AnswerId);
                if (answerRecord == null)
                {
                    return new SaveMapDrawingResponse
                    {
                        Success = false,
                        Message = "答题记录不存在"
                    };
                }

                // 转换DTO为实体
                var mapDrawingDataList = request.DrawingData.Select((dto, index) => new MapDrawingData
                {
                    AnswerId = request.AnswerId,
                    ShapeType = dto.ShapeType,
                    CoordinatesJson = System.Text.Json.JsonSerializer.Serialize(dto.Coordinates),
                    StyleJson = dto.Style != null ? System.Text.Json.JsonSerializer.Serialize(dto.Style) : null,
                    Label = dto.Label,
                    OrderIndex = index,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsDeleted = false
                }).ToList();

                // 使用MapDrawingRepository保存数据
                var mapDrawingRepo = _context.Set<MapDrawingData>();
                
                // 先软删除现有数据
                var existingData = await mapDrawingRepo
                    .Where(md => md.AnswerId == request.AnswerId && !md.IsDeleted)
                    .ToListAsync();
                
                foreach (var item in existingData)
                {
                    item.IsDeleted = true;
                    item.UpdatedAt = DateTime.Now;
                }

                // 添加新数据
                await mapDrawingRepo.AddRangeAsync(mapDrawingDataList);
                
                // 保存更改
                await _context.SaveChangesAsync();

                _logger.LogInformation($"成功保存地图绘制数据，答题记录ID: {request.AnswerId}，图形数量: {mapDrawingDataList.Count}");

                return new SaveMapDrawingResponse
                {
                    Success = true,
                    Message = "保存成功",
                    SavedCount = mapDrawingDataList.Count,
                    SaveTime = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"保存地图绘制数据失败，答题记录ID: {request.AnswerId}");
                return new SaveMapDrawingResponse
                {
                    Success = false,
                    Message = $"保存失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 加载地图绘制数据
        /// </summary>
        public async Task<LoadMapDrawingResponse> LoadMapDrawingDataAsync(int answerId)
        {
            try
            {
                var mapDrawingRepo = _context.Set<MapDrawingData>();
                var drawingDataList = await mapDrawingRepo
                    .Where(md => md.AnswerId == answerId && !md.IsDeleted)
                    .OrderBy(md => md.OrderIndex)
                    .ThenBy(md => md.CreatedAt)
                    .ToListAsync();

                var drawingDtos = drawingDataList.Select(md => new MapDrawingDto
                {
                    DrawingId = md.DrawingId,
                    AnswerId = md.AnswerId,
                    ShapeType = md.ShapeType,
                    Coordinates = !string.IsNullOrEmpty(md.CoordinatesJson) 
                        ? System.Text.Json.JsonSerializer.Deserialize<List<MapCoordinate>>(md.CoordinatesJson) ?? new List<MapCoordinate>()
                        : new List<MapCoordinate>(),
                    Style = !string.IsNullOrEmpty(md.StyleJson) 
                        ? System.Text.Json.JsonSerializer.Deserialize<MapDrawingStyle>(md.StyleJson)
                        : null,
                    Label = md.Label,
                    OrderIndex = md.OrderIndex,
                    CreatedAt = md.CreatedAt,
                    UpdatedAt = md.UpdatedAt
                }).ToList();

                var statistics = new ExamSystem.Domain.DTOs.MapDrawingStatistics
                {
                    TotalShapeCount = drawingDtos.Count,
                    ShapeTypeCount = drawingDtos.GroupBy(d => d.ShapeType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    LastUpdateTime = drawingDataList.LastOrDefault()?.UpdatedAt
                };

                return new LoadMapDrawingResponse
                {
                    Success = true,
                    DrawingData = drawingDtos,
                    Statistics = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"加载地图绘制数据失败，答题记录ID: {answerId}");
                return new LoadMapDrawingResponse
                {
                    Success = false,
                    Message = $"加载失败: {ex.Message}",
                    DrawingData = new List<MapDrawingDto>()
                };
            }
        }

        /// <summary>
        /// 获取地图绘制统计信息
        /// </summary>
        public async Task<ExamSystem.Domain.DTOs.MapDrawingStatistics> GetMapDrawingStatisticsAsync(int answerId)
        {
            try
            {
                var mapDrawingRepo = _context.Set<MapDrawingData>();
                var drawingDataList = await mapDrawingRepo
                    .Where(md => md.AnswerId == answerId && !md.IsDeleted)
                    .ToListAsync();

                return new ExamSystem.Domain.DTOs.MapDrawingStatistics
                {
                    TotalShapeCount = drawingDataList.Count,
                    ShapeTypeCount = drawingDataList.GroupBy(d => d.ShapeType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    LastUpdateTime = drawingDataList.LastOrDefault()?.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取地图绘制统计信息失败，答题记录ID: {answerId}");
                return new ExamSystem.Domain.DTOs.MapDrawingStatistics
                {
                    TotalShapeCount = 0,
                    ShapeTypeCount = new Dictionary<string, int>(),
                    LastUpdateTime = null
                };
            }
        }

        /// <summary>
        /// 提交学生答案
        /// </summary>
        public async Task<bool> SubmitStudentAnswerAsync(
            int examRecordId,
            int questionId,
            List<OverlayDTO> studentOverlays,
            int drawDurationSeconds,
            ClientInfo? clientInfo = null)
        {
            try
            {
                // 查找现有答题记录或创建新记录
                var existingAnswer = await _answerRecordRepository.GetAllAsync();
                var answerRecord = existingAnswer.FirstOrDefault(a => 
                    a.RecordId == examRecordId && a.QuestionId == questionId);

                if (answerRecord == null)
                {
                    answerRecord = new AnswerRecord
                    {
                        RecordId = examRecordId,
                        QuestionId = questionId,
                        DrawDurationSeconds = drawDurationSeconds,
                        AnswerTime = DateTime.Now
                    };
                }
                else
                {
                    answerRecord.DrawDurationSeconds = drawDurationSeconds;
                    answerRecord.AnswerTime = DateTime.Now;
                }

                // 设置答案数据
                answerRecord.SetStudentOverlays(studentOverlays);
                
                if (clientInfo != null)
                    answerRecord.SetClientInfo(clientInfo);

                // 保存或更新
                if (answerRecord.AnswerId == 0)
                    await _answerRecordRepository.AddAsync(answerRecord);
                else
                    await _answerRecordRepository.UpdateAsync(answerRecord);

                _logger.LogInformation($"学生答案提交成功，考试记录ID: {examRecordId}, 题目ID: {questionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"提交学生答案失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取学生答案（阅卷用）
        /// </summary>
        public async Task<MapDrawingAnswerDto?> GetStudentAnswerForReviewAsync(int answerRecordId)
        {
            try
            {
                var answerRecord = await _answerRecordRepository.GetByIdAsync(answerRecordId);
                if (answerRecord == null)
                    return null;

                var examRecord = await _examRecordRepository.GetByIdAsync(answerRecord.RecordId);
                var student = examRecord != null ? await _userRepository.GetByIdAsync(examRecord.UserId) : null;
                var question = await _questionRepository.GetByIdAsync(answerRecord.QuestionId);

                var dto = new MapDrawingAnswerDto
                {
                    AnswerRecordId = answerRecord.AnswerId,
                    QuestionId = answerRecord.QuestionId,
                    StudentId = examRecord?.UserId ?? 0,
                    StudentName = student?.Username ?? "未知学生",
                    StudentOverlays = answerRecord.GetStudentOverlays(),
                    DrawDurationSeconds = answerRecord.DrawDurationSeconds,
                    Score = answerRecord.Score,
                    IsGraded = answerRecord.IsGraded,
                    RubricScores = answerRecord.GetRubricScores(),
                    Comment = answerRecord.Comment,
                    AnswerTime = answerRecord.AnswerTime,
                    GradeTime = answerRecord.GradeTime,
                    ClientInfo = answerRecord.GetClientInfo()
                };

                // 获取参考答案
                if (question != null && _questionRepository is QuestionRepository repo)
                {
                    dto.ReferenceOverlays = repo.GetReferenceOverlays(question);
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取学生答案失败，答题记录ID: {answerRecordId}");
                return null;
            }
        }

        /// <summary>
        /// 保存阅卷评分
        /// </summary>
        public async Task<bool> SaveReviewScoreAsync(
            int answerRecordId,
            RubricScores rubricScores,
            string? comment = null,
            int? graderId = null)
        {
            try
            {
                var answerRecord = await _answerRecordRepository.GetByIdAsync(answerRecordId);
                if (answerRecord == null)
                    return false;

                answerRecord.SetRubricScores(rubricScores);
                answerRecord.Score = rubricScores.TotalScore;
                answerRecord.Comment = comment;
                answerRecord.IsGraded = true;
                answerRecord.GradeTime = DateTime.Now;
                answerRecord.GraderId = graderId;

                await _answerRecordRepository.UpdateAsync(answerRecord);
                _logger.LogInformation($"阅卷评分保存成功，答题记录ID: {answerRecordId}, 得分: {rubricScores.TotalScore}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"保存阅卷评分失败，答题记录ID: {answerRecordId}");
                return false;
            }
        }

        /// <summary>
        /// 自动评分（基于参考答案的相似度）
        /// </summary>
        public async Task<decimal> AutoGradeAnswerAsync(int answerRecordId)
        {
            try
            {
                var answerRecord = await _answerRecordRepository.GetByIdAsync(answerRecordId);
                if (answerRecord == null)
                    return 0m;

                var question = await _questionRepository.GetByIdAsync(answerRecord.QuestionId);
                if (question == null || _questionRepository is not QuestionRepository repo)
                    return 0m;

                var studentOverlays = answerRecord.GetStudentOverlays();
                var referenceOverlays = repo.GetReferenceOverlays(question);

                if (studentOverlays == null || referenceOverlays == null)
                    return 0m;

                // 简单的相似度计算（实际应用中可能需要更复杂的算法）
                var similarity = CalculateOverlaySimilarity(studentOverlays, referenceOverlays);
                var autoScore = question.Score * similarity;

                _logger.LogInformation($"自动评分完成，答题记录ID: {answerRecordId}, 相似度: {similarity:P2}, 得分: {autoScore}");
                return autoScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"自动评分失败，答题记录ID: {answerRecordId}");
                return 0m;
            }
        }

        /// <summary>
        /// 批量导出地图绘制题答案
        /// </summary>
        public async Task<List<MapDrawingAnswerDto>> ExportMapDrawingAnswersAsync(int examPaperId)
        {
            try
            {
                var results = new List<MapDrawingAnswerDto>();
                var allAnswers = await _answerRecordRepository.GetAllAsync();
                var examRecords = await _examRecordRepository.GetAllAsync();
                
                var relevantRecords = examRecords.Where(er => er.PaperId == examPaperId).ToList();
                
                foreach (var examRecord in relevantRecords)
                {
                    var answers = allAnswers.Where(a => a.RecordId == examRecord.RecordId).ToList();
                    
                    foreach (var answer in answers)
                    {
                        var question = await _questionRepository.GetByIdAsync(answer.QuestionId);
                        if (question?.QuestionType == QuestionType.MapDrawing)
                        {
                            var dto = await GetStudentAnswerForReviewAsync(answer.AnswerId);
                            if (dto != null)
                                results.Add(dto);
                        }
                    }
                }

                _logger.LogInformation($"导出地图绘制题答案完成，试卷ID: {examPaperId}, 答案数量: {results.Count}");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导出地图绘制题答案失败，试卷ID: {examPaperId}");
                return new List<MapDrawingAnswerDto>();
            }
        }

        /// <summary>
        /// 计算覆盖物相似度
        /// </summary>
        private decimal CalculateOverlaySimilarity(List<OverlayDTO> student, List<OverlayDTO> reference)
        {
            if (!student.Any() && !reference.Any())
                return 1.0m;

            if (!student.Any() || !reference.Any())
                return 0.0m;

            // 简单的相似度计算：基于覆盖物数量和类型匹配
            var typeMatches = 0;
            var studentTypes = student.GroupBy(o => o.Type).ToDictionary(g => g.Key, g => g.Count());
            var referenceTypes = reference.GroupBy(o => o.Type).ToDictionary(g => g.Key, g => g.Count());

            foreach (var refType in referenceTypes)
            {
                if (studentTypes.ContainsKey(refType.Key))
                {
                    var matchCount = Math.Min(studentTypes[refType.Key], refType.Value);
                    typeMatches += matchCount;
                }
            }

            var totalReference = reference.Count;
            return totalReference > 0 ? (decimal)typeMatches / totalReference : 0m;
        }
    }
}