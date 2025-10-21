using System;

namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 学生考试信息
    /// </summary>
    public class StudentExamInfo
    {
        /// <summary>
        /// 试卷ID
        /// </summary>
        public int PaperId { get; set; }
        
        /// <summary>
        /// 考试标题
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// 科目
        /// </summary>
        public string Subject { get; set; } = string.Empty;
        
        /// <summary>
        /// 考试时长（分钟）
        /// </summary>
        public int Duration { get; set; }
        
        /// <summary>
        /// 题目总数
        /// </summary>
        public int TotalQuestions { get; set; }
        
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// 考试描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否已参加过考试
        /// </summary>
        public bool HasTaken { get; set; }
    }
}