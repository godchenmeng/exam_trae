using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Models;
using ExamSystem.Domain.DTOs;
using ExamSystem.Infrastructure.Repositories;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 地图绘制题服务接口
    /// </summary>
    public interface IMapDrawingService
    {
        /// <summary>
        /// 创建地图绘制题
        /// </summary>
        Task<Question> CreateMapDrawingQuestionAsync(
            int bankId,
            string title,
            string content,
            MapDrawingConfig config,
            List<OverlayDTO>? guidanceOverlays = null,
            List<OverlayDTO>? referenceOverlays = null,
            ReviewRubric? reviewRubric = null,
            BuildingLayersConfig? buildingLayersConfig = null,
            int timeLimitSeconds = 0,
            decimal score = 10m);

        /// <summary>
        /// 更新地图绘制题配置
        /// </summary>
        Task<bool> UpdateMapDrawingConfigAsync(int questionId, MapDrawingConfig config);

        /// <summary>
        /// 更新地图绘制题
        /// </summary>
        Task<bool> UpdateMapDrawingQuestionAsync(int questionId, MapDrawingQuestionDto questionDto);

        /// <summary>
        /// 获取地图绘制题详情（学生端）
        /// </summary>
        Task<MapDrawingQuestionDto?> GetMapDrawingQuestionForStudentAsync(int questionId);

        /// <summary>
        /// 获取地图绘制题详情（教师端）
        /// </summary>
        Task<MapDrawingQuestionDto?> GetMapDrawingQuestionForTeacherAsync(int questionId);

        /// <summary>
        /// 实时保存地图绘制数据
        /// </summary>
        /// <param name="request">保存请求</param>
        /// <returns>保存结果</returns>
        Task<SaveMapDrawingResponse> SaveMapDrawingDataAsync(SaveMapDrawingRequest request);

        /// <summary>
        /// 加载地图绘制数据
        /// </summary>
        /// <param name="answerId">答题记录ID</param>
        /// <returns>加载结果</returns>
        Task<LoadMapDrawingResponse> LoadMapDrawingDataAsync(int answerId);

        /// <summary>
        /// 获取地图绘制统计信息
        /// </summary>
        /// <param name="answerId">答题记录ID</param>
        /// <returns>统计信息</returns>
        Task<ExamSystem.Domain.DTOs.MapDrawingStatistics> GetMapDrawingStatisticsAsync(int answerId);

        /// <summary>
        /// 提交学生答案
        /// </summary>
        Task<bool> SubmitStudentAnswerAsync(
            int examRecordId,
            int questionId,
            List<OverlayDTO> studentOverlays,
            int drawDurationSeconds,
            ClientInfo? clientInfo = null);

        /// <summary>
        /// 获取学生答案（阅卷用）
        /// </summary>
        Task<MapDrawingAnswerDto?> GetStudentAnswerForReviewAsync(int answerRecordId);

        /// <summary>
        /// 保存阅卷评分
        /// </summary>
        Task<bool> SaveReviewScoreAsync(
            int answerRecordId,
            RubricScores rubricScores,
            string? comment = null,
            int? graderId = null);

        /// <summary>
        /// 自动评分（基于参考答案的相似度）
        /// </summary>
        Task<decimal> AutoGradeAnswerAsync(int answerRecordId);

        /// <summary>
        /// 批量导出地图绘制题答案
        /// </summary>
        Task<List<MapDrawingAnswerDto>> ExportMapDrawingAnswersAsync(int examPaperId);
    }

    /// <summary>
    /// 地图绘制题DTO
    /// </summary>
    public class MapDrawingQuestionDto
    {
        public int QuestionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public int TimeLimitSeconds { get; set; }
        public string? Tags { get; set; }
        public MapDrawingConfig? Config { get; set; }
        public List<OverlayDTO>? GuidanceOverlays { get; set; }
        public List<OverlayDTO>? ReferenceOverlays { get; set; }
        public ReviewRubric? ReviewRubric { get; set; }
        public BuildingLayersConfig? BuildingLayersConfig { get; set; }
    }

    /// <summary>
    /// 地图绘制题答案DTO
    /// </summary>
    public class MapDrawingAnswerDto
    {
        public int AnswerRecordId { get; set; }
        public int QuestionId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public List<OverlayDTO>? StudentOverlays { get; set; }
        public List<OverlayDTO>? ReferenceOverlays { get; set; }
        public int DrawDurationSeconds { get; set; }
        public decimal Score { get; set; }
        public bool IsGraded { get; set; }
        public RubricScores? RubricScores { get; set; }
        public string? Comment { get; set; }
        public DateTime? AnswerTime { get; set; }
        public DateTime? GradeTime { get; set; }
        public ClientInfo? ClientInfo { get; set; }
    }
}