using System.Collections.Generic;

namespace ExamSystem.Services.Models
{
    /// <summary>
    /// 分页查询结果
    /// </summary>
    /// <typeparam name="T">数据项类型</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// 数据项列表
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 当前页码（从1开始）
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 0;

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// 创建分页结果
        /// </summary>
        /// <param name="items">数据项</param>
        /// <param name="totalCount">总记录数</param>
        /// <param name="pageIndex">当前页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>分页结果</returns>
        public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int pageIndex, int pageSize)
        {
            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 创建空的分页结果
        /// </summary>
        /// <param name="pageIndex">当前页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>空的分页结果</returns>
        public static PagedResult<T> Empty(int pageIndex = 1, int pageSize = 10)
        {
            return new PagedResult<T>
            {
                Items = new List<T>(),
                TotalCount = 0,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }
    }
}