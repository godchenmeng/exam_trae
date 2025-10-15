using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// Repository基础实现类
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly ExamDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(ExamDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// 获取所有实体
        /// </summary>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>
        /// 根据条件查找实体
        /// </summary>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <summary>
        /// 根据条件获取单个实体
        /// </summary>
        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// 添加实体
        /// </summary>
        public virtual async Task<T> AddAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 批量添加实体
        /// </summary>
        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            await _dbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 更新实体
        /// </summary>
        public virtual void Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            _context.SaveChanges();
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        public virtual void Remove(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
            _context.SaveChanges();
        }

        /// <summary>
        /// 批量删除实体
        /// </summary>
        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _dbSet.RemoveRange(entities);
            _context.SaveChanges();
        }

        /// <summary>
        /// 根据ID删除实体
        /// </summary>
        public virtual async Task<bool> RemoveByIdAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;

            Remove(entity);
            return true;
        }

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>
        /// 获取实体数量
        /// </summary>
        public virtual async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        /// <summary>
        /// 根据条件获取实体数量
        /// </summary>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        /// <summary>
        /// 获取可查询的实体集合
        /// </summary>
        public virtual IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageIndex, 
            int pageSize, 
            Expression<Func<T, bool>>? predicate = null)
        {
            var query = _dbSet.AsQueryable();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// 分页查询（带排序）
        /// </summary>
        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync<TKey>(
            int pageIndex,
            int pageSize,
            Expression<Func<T, bool>>? predicate,
            Expression<Func<T, TKey>> orderBy,
            bool ascending = true)
        {
            var query = _dbSet.AsQueryable();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// 异步更新实体
        /// </summary>
        public virtual async Task UpdateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 异步删除实体
        /// </summary>
        public virtual async Task RemoveAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 异步批量删除实体
        /// </summary>
        public virtual async Task RemoveRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}