namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 操作权限键值常量，集中管理，避免字符串拼写错误
    /// </summary>
    public static class PermissionKeys
    {
        // 消息通知相关
        public const string SendNotification = "SendNotification";        // 老师/管理员
        public const string ViewNotification = "ViewNotification";        // 所有角色
        public const string ReceiveNotification = "ReceiveNotification";  // 学生

        // 学习资源相关
        public const string UploadResource = "UploadResource";            // 老师/管理员
        public const string ManageResource = "ManageResource";            // 老师/管理员（上下架、分类等）
        public const string ViewResource = "ViewResource";                // 所有角色
        public const string DownloadResource = "DownloadResource";        // 学生/老师（管理员默认允许）
    }
}