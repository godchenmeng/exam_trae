using System;
using System.Collections.Generic;
using System.Text.Json;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Models;

namespace ExamSystem.Domain.Extensions
{
    /// <summary>
    /// Question实体扩展方法
    /// </summary>
    public static class QuestionExtensions
    {
        /// <summary>
        /// 获取地图绘制题配置
        /// </summary>
        public static MapDrawingConfig? GetMapDrawingConfig(this Question question)
        {
            if (string.IsNullOrEmpty(question.MapDrawingConfigJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<MapDrawingConfig>(question.MapDrawingConfigJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// 设置地图绘制题配置
        /// </summary>
        public static void SetMapDrawingConfig(this Question question, MapDrawingConfig config)
        {
            question.MapDrawingConfigJson = JsonSerializer.Serialize(config);
        }

        /// <summary>
        /// 获取指引图层
        /// </summary>
        public static List<OverlayDTO>? GetGuidanceOverlays(this Question question)
        {
            if (string.IsNullOrEmpty(question.GuidanceOverlaysJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<List<OverlayDTO>>(question.GuidanceOverlaysJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// 设置指引图层
        /// </summary>
        public static void SetGuidanceOverlays(this Question question, List<OverlayDTO> overlays)
        {
            question.GuidanceOverlaysJson = JsonSerializer.Serialize(overlays);
        }

        /// <summary>
        /// 获取参考答案图层
        /// </summary>
        public static List<OverlayDTO>? GetReferenceOverlays(this Question question)
        {
            if (string.IsNullOrEmpty(question.ReferenceOverlaysJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<List<OverlayDTO>>(question.ReferenceOverlaysJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// 设置参考答案图层
        /// </summary>
        public static void SetReferenceOverlays(this Question question, List<OverlayDTO> overlays)
        {
            question.ReferenceOverlaysJson = JsonSerializer.Serialize(overlays);
        }

        /// <summary>
        /// 获取评分量表
        /// </summary>
        public static ReviewRubric? GetReviewRubric(this Question question)
        {
            if (string.IsNullOrEmpty(question.ReviewRubricJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ReviewRubric>(question.ReviewRubricJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// 设置评分量表
        /// </summary>
        public static void SetReviewRubric(this Question question, ReviewRubric rubric)
        {
            question.ReviewRubricJson = JsonSerializer.Serialize(rubric);
        }

        /// <summary>
        /// 获取建筑图层配置
        /// </summary>
        public static BuildingLayersConfig? GetBuildingLayersConfig(this Question question)
        {
            if (string.IsNullOrEmpty(question.ShowBuildingLayersJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<BuildingLayersConfig>(question.ShowBuildingLayersJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// 设置建筑图层配置
        /// </summary>
        public static void SetBuildingLayersConfig(this Question question, BuildingLayersConfig config)
        {
            question.ShowBuildingLayersJson = JsonSerializer.Serialize(config);
        }
    }
}