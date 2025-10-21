namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 模块访问键值常量，集中管理，避免字符串拼写错误
    /// </summary>
    public static class ModuleKeys
    {
        public const string MessageCenter = "MessageCenter";       // 消息中心：老师/学生/管理员
        public const string LearningResources = "LearningResources"; // 学习资源：老师/学生/管理员
        public const string ExamManagement = "ExamManagement";     // 考试管理：所有角色
    }
}