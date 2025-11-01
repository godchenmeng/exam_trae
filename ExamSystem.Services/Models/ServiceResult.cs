using System.Collections.Generic;

namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 服务操作结果（无数据）
    /// </summary>
    public class ServiceResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 错误详情列表
        /// </summary>
        public List<string> ErrorDetails { get; set; } = new List<string>();

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static ServiceResult Success()
        {
            return new ServiceResult { IsSuccess = true };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static ServiceResult Failure(string errorMessage)
        {
            return new ServiceResult 
            { 
                IsSuccess = false, 
                ErrorMessage = errorMessage 
            };
        }

        /// <summary>
        /// 创建失败结果（带详细错误）
        /// </summary>
        public static ServiceResult Failure(string errorMessage, List<string> errorDetails)
        {
            return new ServiceResult 
            { 
                IsSuccess = false, 
                ErrorMessage = errorMessage,
                ErrorDetails = errorDetails
            };
        }
    }

    /// <summary>
    /// 服务操作结果（带数据）
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class ServiceResult<T> : ServiceResult
    {
        /// <summary>
        /// 返回的数据
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T> 
            { 
                IsSuccess = true, 
                Data = data 
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static new ServiceResult<T> Failure(string errorMessage)
        {
            return new ServiceResult<T> 
            { 
                IsSuccess = false, 
                ErrorMessage = errorMessage 
            };
        }

        /// <summary>
        /// 创建失败结果（带详细错误）
        /// </summary>
        public static new ServiceResult<T> Failure(string errorMessage, List<string> errorDetails)
        {
            return new ServiceResult<T> 
            { 
                IsSuccess = false, 
                ErrorMessage = errorMessage,
                ErrorDetails = errorDetails
            };
        }
    }
}