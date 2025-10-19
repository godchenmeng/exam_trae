using ExamSystem.Domain.Enums;

namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 题目导入数据传输对象
    /// </summary>
    public class QuestionImportDto
    {
        /// <summary>
        /// 题型
        /// </summary>
        public string QuestionType { get; set; } = string.Empty;

        /// <summary>
        /// 题目内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 选项A
        /// </summary>
        public string OptionA { get; set; } = string.Empty;

        /// <summary>
        /// 选项B
        /// </summary>
        public string OptionB { get; set; } = string.Empty;

        /// <summary>
        /// 选项C
        /// </summary>
        public string OptionC { get; set; } = string.Empty;

        /// <summary>
        /// 选项D
        /// </summary>
        public string OptionD { get; set; } = string.Empty;

        /// <summary>
        /// 选项E
        /// </summary>
        public string OptionE { get; set; } = string.Empty;

        /// <summary>
        /// 选项F
        /// </summary>
        public string OptionF { get; set; } = string.Empty;

        /// <summary>
        /// 正确答案
        /// </summary>
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// 题目解析
        /// </summary>
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// 分值
        /// </summary>
        public string Points { get; set; } = string.Empty;

        /// <summary>
        /// 难度
        /// </summary>
        public string Difficulty { get; set; } = string.Empty;

        /// <summary>
        /// 标签
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// 行号（用于错误定位）
        /// </summary>
        public int RowNumber { get; set; }
    }
}