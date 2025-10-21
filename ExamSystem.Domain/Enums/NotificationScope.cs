namespace ExamSystem.Domain.Enums
{
    /// <summary>
    /// 通知目标范围
    /// </summary>
    public enum NotificationScope
    {
        /// <summary>
        /// 全体考生
        /// </summary>
        AllStudents = 0,
        /// <summary>
        /// 指定用户
        /// </summary>
        SpecificUsers = 1,
        /// <summary>
        /// 全部用户（可按需扩展）
        /// </summary>
        AllUsers = 2
    }
}