namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 考试验证结果
    /// </summary>
    public class ExamValidationResult
    {
        /// <summary>
        /// 验证是否通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 错误类型
        /// </summary>
        public ExamValidationErrorType ErrorType { get; set; }

        /// <summary>
        /// 创建成功的验证结果
        /// </summary>
        public static ExamValidationResult Success()
        {
            return new ExamValidationResult
            {
                IsValid = true,
                ErrorMessage = string.Empty,
                ErrorType = ExamValidationErrorType.None
            };
        }

        /// <summary>
        /// 创建失败的验证结果
        /// </summary>
        public static ExamValidationResult Failure(string errorMessage, ExamValidationErrorType errorType)
        {
            return new ExamValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                ErrorType = errorType
            };
        }
    }

    /// <summary>
    /// 考试验证错误类型
    /// </summary>
    public enum ExamValidationErrorType
    {
        /// <summary>
        /// 无错误
        /// </summary>
        None = 0,

        /// <summary>
        /// 试卷不存在
        /// </summary>
        PaperNotFound = 1,

        /// <summary>
        /// 试卷未发布
        /// </summary>
        PaperNotPublished = 2,

        /// <summary>
        /// 考试尚未开始
        /// </summary>
        ExamNotStarted = 3,

        /// <summary>
        /// 考试已结束
        /// </summary>
        ExamEnded = 4,

        /// <summary>
        /// 不允许重考
        /// </summary>
        RetakeNotAllowed = 5,

        /// <summary>
        /// 其他错误
        /// </summary>
        Other = 99
    }
}