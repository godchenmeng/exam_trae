using System.Collections.Generic;

namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 导入结果模型
    /// </summary>
    public class ImportResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 成功导入数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 失败数量（别名）
        /// </summary>
        public int FailedCount => FailureCount;

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new List<string>();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> WarningMessages { get; set; } = new List<string>();

        /// <summary>
        /// 详细错误信息（行号和错误描述）
        /// </summary>
        public Dictionary<int, List<string>> DetailedErrors { get; set; } = new Dictionary<int, List<string>>();

        /// <summary>
        /// 成功导入的题目信息
        /// </summary>
        public List<ImportedQuestionInfo> SuccessfulQuestions { get; set; } = new List<ImportedQuestionInfo>();

        /// <summary>
        /// 导入失败的题目信息
        /// </summary>
        public List<ImportFailureInfo> FailedQuestions { get; set; } = new List<ImportFailureInfo>();

        /// <summary>
        /// 导入失败的项目信息（通用）
        /// </summary>
        public List<ImportFailedItem> FailedItems { get; set; } = new List<ImportFailedItem>();

        /// <summary>
        /// 导入摘要信息
        /// </summary>
        public string Summary => $"总计 {TotalCount} 条记录，成功 {SuccessCount} 条，失败 {FailedCount} 条";
    }

    /// <summary>
    /// 成功导入的题目信息
    /// </summary>
    public class ImportedQuestionInfo
    {
        public int RowNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Tags { get; set; } = string.Empty;
    }

    /// <summary>
    /// 导入失败的题目信息
    /// </summary>
    public class ImportFailureInfo
    {
        public int RowNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string RawData { get; set; } = string.Empty;
    }

    /// <summary>
    /// 导入失败的项目信息（通用）
    /// </summary>
    public class ImportFailedItem
    {
        public string Data { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
    }
}