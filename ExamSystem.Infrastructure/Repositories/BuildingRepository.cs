using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 建筑数据访问层
    /// </summary>
    public class BuildingRepository : BaseRepository<Building>, IBuildingRepository
    {
        public BuildingRepository(ExamDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据城市和机构类型获取建筑列表
        /// </summary>
        /// <param name="cityCn">城市中文名称</param>
        /// <param name="orgType">机构类型：1-消防队站；2-专职队；3-重点建筑</param>
        /// <returns>建筑列表</returns>
        public async Task<IEnumerable<Building>> GetBuildingsByCityAndTypeAsync(string cityCn, byte orgType)
        {
            return await _dbSet
                .Where(b => b.CityCn == cityCn && b.OrgType == orgType && !b.Deleted)
                .OrderBy(b => b.OrgName)
                .ToListAsync();
        }

        /// <summary>
        /// 根据城市获取所有建筑列表
        /// </summary>
        /// <param name="cityCn">城市中文名称</param>
        /// <returns>建筑列表</returns>
        public async Task<IEnumerable<Building>> GetBuildingsByCityAsync(string cityCn)
        {
            return await _dbSet
                .Where(b => b.CityCn == cityCn && !b.Deleted)
                .OrderBy(b => b.OrgType)
                .ThenBy(b => b.OrgName)
                .ToListAsync();
        }

        /// <summary>
        /// 根据城市获取建筑类型统计
        /// </summary>
        /// <param name="cityCn">城市中文名称</param>
        /// <returns>建筑类型统计字典</returns>
        public async Task<Dictionary<byte, int>> GetBuildingCountByCityAsync(string cityCn)
        {
            return await _dbSet
                .Where(b => b.CityCn == cityCn && !b.Deleted)
                .GroupBy(b => b.OrgType)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取所有城市列表
        /// </summary>
        /// <returns>城市列表</returns>
        public async Task<IEnumerable<string>> GetCitiesAsync()
        {
            return await _dbSet
                .Where(b => !b.Deleted && !string.IsNullOrEmpty(b.CityCn))
                .Select(b => b.CityCn!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        /// <summary>
        /// 根据坐标范围获取建筑列表
        /// </summary>
        /// <param name="minLng">最小经度</param>
        /// <param name="maxLng">最大经度</param>
        /// <param name="minLat">最小纬度</param>
        /// <param name="maxLat">最大纬度</param>
        /// <returns>建筑列表</returns>
        public async Task<IEnumerable<Building>> GetBuildingsByBoundsAsync(double minLng, double maxLng, double minLat, double maxLat)
        {
            var buildings = await _dbSet
                .Where(b => !b.Deleted && !string.IsNullOrEmpty(b.Amap))
                .ToListAsync();
                
            return buildings
                .Where(b =>
                {
                    var coords = b.GetCoordinates();
                    if (coords == null) return false;
                    
                    var lng = coords[0];
                    var lat = coords[1];
                    
                    return lng >= minLng && lng <= maxLng && lat >= minLat && lat <= maxLat;
                })
                .OrderBy(b => b.OrgType)
                .ThenBy(b => b.OrgName)
                .ToList();
        }

        /// <summary>
        /// 根据机构名称搜索建筑
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>建筑列表</returns>
        public async Task<IEnumerable<Building>> SearchBuildingsByNameAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<Building>();

            return await _dbSet
                .Where(b => !b.Deleted && 
                           (b.OrgName!.Contains(keyword) || 
                            b.Address!.Contains(keyword) ||
                            b.CityCn!.Contains(keyword)))
                .OrderBy(b => b.CityCn)
                .ThenBy(b => b.OrgType)
                .ThenBy(b => b.OrgName)
                .ToListAsync();
        }

        /// <summary>
        /// 批量获取指定ID的建筑列表
        /// </summary>
        /// <param name="ids">建筑ID列表</param>
        /// <returns>建筑列表</returns>
        public async Task<IEnumerable<Building>> GetBuildingsByIdsAsync(IEnumerable<int> ids)
        {
            return await _dbSet
                .Where(b => ids.Contains(b.Id) && !b.Deleted)
                .OrderBy(b => b.OrgType)
                .ThenBy(b => b.OrgName)
                .ToListAsync();
        }

        /// <summary>
        /// 获取有坐标信息的建筑列表
        /// </summary>
        /// <param name="cityCn">城市中文名称（可选）</param>
        /// <returns>建筑列表</returns>
        public async Task<IEnumerable<Building>> GetBuildingsWithCoordinatesAsync(string? cityCn = null)
        {
            var query = _dbSet.Where(b => !b.Deleted && !string.IsNullOrEmpty(b.Amap));
            
            if (!string.IsNullOrEmpty(cityCn))
            {
                query = query.Where(b => b.CityCn == cityCn);
            }

            return await query
                .OrderBy(b => b.CityCn)
                .ThenBy(b => b.OrgType)
                .ThenBy(b => b.OrgName)
                .ToListAsync();
        }
    }
}