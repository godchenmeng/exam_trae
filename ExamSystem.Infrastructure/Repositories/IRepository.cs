using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 基础Repository接口，定义通用的数据访问操作
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>实体对象</returns>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// 获取所有实体
        /// </summary>
        /// <returns>实体集合</returns>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// 根据条件查找实体
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <returns>符合条件的实体集合</returns>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 根据条件获取单个实体
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <returns>符合条件的实体</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 添加实体
        /// </summary>
        /// <param name="entity">要添加的实体</param>
        /// <returns>添加后的实体</returns>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// 批量添加实体
        /// </summary>
        /// <param name="entities">要添加的实体集合</param>
        Task AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// 更新实体
        /// </summary>
        /// <param name="entity">要更新的实体</param>
        void Update(T entity);

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="entity">要删除的实体</param>
        void Remove(T entity);

        /// <summary>
        /// 批量删除实体
        /// </summary>
        /// <param name="entities">要删除的实体集合</param>
        void RemoveRange(IEnumerable<T> entities);

        /// <summary>
        /// 根据ID删除实体
        /// </summary>
        /// <param name="id">实体ID</param>
        Task<bool> RemoveByIdAsync(int id);

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 获取实体数量
        /// </summary>
        /// <returns>实体总数</returns>
        Task<int> CountAsync();

        /// <summary>
        /// 根据条件获取实体数量
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <returns>符合条件的实体数量</returns>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 获取可查询的实体集合
        /// </summary>
        /// <returns>IQueryable对象</returns>
        IQueryable<T> GetQueryable();

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex">页码（从0开始）</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="predicate">查询条件</param>
        /// <returns>分页结果</returns>
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageIndex, 
            int pageSize, 
            Expression<Func<T, bool>>? predicate = null);

        /// <summary>
        /// 分页查询（带排序）
        /// </summary>
        /// <typeparam name="TKey">排序键类型</typeparam>
        /// <param name="pageIndex">页码（从0开始）</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="predicate">查询条件</param>
        /// <param name="orderBy">排序表达式</param>
        /// <param name="ascending">是否升序</param>
        /// <returns>分页结果</returns>
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync<TKey>(
            int pageIndex,
            int pageSize,
            Expression<Func<T, bool>>? predicate,
            Expression<Func<T, TKey>> orderBy,
            bool ascending = true);
    }
}