using System;

namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 学生考试结果
    /// </summary>
    public class StudentExamResult
    {
        /// <summary>
        /// 考试记录ID
        /// </summary>
        public int RecordId { get; set; }
        
        /// <summary>
        /// 考试标题
        /// </summary>
        public string ExamTitle { get; set; } = string.Empty;
        
        /// <summary>
        /// 科目
        /// </summary>
        public string Subject { get; set; } = string.Empty;
        
        /// <summary>
        /// 考试时间
        /// </summary>
        public DateTime ExamDate { get; set; }
        
        /// <summary>
        /// 考试用时
        /// </summary>
        public string Duration { get; set; } = string.Empty;
        
        /// <summary>
        /// 题目总数
        /// </summary>
        public int QuestionCount { get; set; }
        
        /// <summary>
        /// 考试状态
        /// </summary>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// 总分
        /// </summary>
        public decimal TotalScore { get; set; }
        
        /// <summary>
        /// 得分
        /// </summary>
        public decimal Score { get; set; }
        
        /// <summary>
        /// 是否有错题
        /// </summary>
        public bool HasWrongAnswers { get; set; }
    }
}