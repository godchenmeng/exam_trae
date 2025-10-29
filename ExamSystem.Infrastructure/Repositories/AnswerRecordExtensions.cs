using System.Collections.Generic;
using System.Text.Json;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Models;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// AnswerRecord扩展方法
    /// </summary>
    public static class AnswerRecordExtensions
    {
        /// <summary>
        /// 获取学生答案覆盖物
        /// </summary>
        public static List<OverlayDTO>? GetStudentOverlays(this AnswerRecord answerRecord)
        {
            if (string.IsNullOrEmpty(answerRecord.UserAnswer))
                return null;

            try
            {
                return JsonSerializer.Deserialize<List<OverlayDTO>>(answerRecord.UserAnswer);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// 设置学生答案覆盖物
        /// </summary>
        public static void SetStudentOverlays(this AnswerRecord answerRecord, List<OverlayDTO> overlays)
        {
            answerRecord.UserAnswer = JsonSerializer.Serialize(overlays);
        }

        /// <summary>
        /// 获取客户端信息
        /// </summary>
        public static ClientInfo? GetClientInfo(this AnswerRecord answerRecord)
        {
            if (string.IsNullOrEmpty(answerRecord.ClientInfoJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ClientInfo>(answerRecord.ClientInfoJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// 设置客户端信息
        /// </summary>
        public static void SetClientInfo(this AnswerRecord answerRecord, ClientInfo clientInfo)
        {
            answerRecord.ClientInfoJson = JsonSerializer.Serialize(clientInfo);
        }

        /// <summary>
        /// 获取量表评分结果
        /// </summary>
        public static RubricScores? GetRubricScores(this AnswerRecord answerRecord)
        {
            if (string.IsNullOrEmpty(answerRecord.RubricScoresJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<RubricScores>(answerRecord.RubricScoresJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// 设置量表评分结果
        /// </summary>
        public static void SetRubricScores(this AnswerRecord answerRecord, RubricScores rubricScores)
        {
            answerRecord.RubricScoresJson = JsonSerializer.Serialize(rubricScores);
        }

        /// <summary>
        /// 计算地图绘制题总分
        /// </summary>
        public static decimal CalculateMapDrawingScore(this AnswerRecord answerRecord)
        {
            var rubricScores = answerRecord.GetRubricScores();
            return rubricScores?.TotalScore ?? 0m;
        }

        /// <summary>
        /// 检查是否为地图绘制题答案
        /// </summary>
        public static bool IsMapDrawingAnswer(this AnswerRecord answerRecord)
        {
            if (string.IsNullOrEmpty(answerRecord.UserAnswer))
                return false;

            try
            {
                var overlays = JsonSerializer.Deserialize<List<OverlayDTO>>(answerRecord.UserAnswer);
                return overlays != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}