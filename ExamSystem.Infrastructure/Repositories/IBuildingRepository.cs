using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 建筑数据访问接口
    /// </summary>
    public interface IBuildingRepository : IRepository<Building>
    {
        /// <summary>
        /// 根据城市和机构类型获取建筑列表
        /// </summary>
        /// <param name="cityCn">城市中文名称</param>
        /// <param name="orgType">机构类型：1-消防队站；2-专职队；3-重点建筑</param>
        /// <returns>建筑列表</returns>
        Task<IEnumerable<Building>> GetBuildingsByCityAndTypeAsync(string cityCn, byte orgType);

        /// <summary>
        /// 根据城市获取所有建筑列表
        /// </summary>
        /// <param name="cityCn">城市中文名称</param>
        /// <returns>建筑列表</returns>
        Task<IEnumerable<Building>> GetBuildingsByCityAsync(string cityCn);

        /// <summary>
        /// 根据城市获取建筑类型统计
        /// </summary>
        /// <param name="cityCn">城市中文名称</param>
        /// <returns>建筑类型统计字典</returns>
        Task<Dictionary<byte, int>> GetBuildingCountByCityAsync(string cityCn);

        /// <summary>
        /// 获取所有城市列表
        /// </summary>
        /// <returns>城市列表</returns>
        Task<IEnumerable<string>> GetCitiesAsync();

        /// <summary>
        /// 根据坐标范围获取建筑列表
        /// </summary>
        /// <param name="minLng">最小经度</param>
        /// <param name="maxLng">最大经度</param>
        /// <param name="minLat">最小纬度</param>
        /// <param name="maxLat">最大纬度</param>
        /// <returns>建筑列表</returns>
        Task<IEnumerable<Building>> GetBuildingsByBoundsAsync(double minLng, double maxLng, double minLat, double maxLat);

        /// <summary>
        /// 根据机构名称搜索建筑
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>建筑列表</returns>
        Task<IEnumerable<Building>> SearchBuildingsByNameAsync(string keyword);

        /// <summary>
        /// 批量获取指定ID的建筑列表
        /// </summary>
        /// <param name="ids">建筑ID列表</param>
        /// <returns>建筑列表</returns>
        Task<IEnumerable<Building>> GetBuildingsByIdsAsync(IEnumerable<int> ids);

        /// <summary>
        /// 获取有坐标信息的建筑列表
        /// </summary>
        /// <param name="cityCn">城市中文名称（可选）</param>
        /// <returns>建筑列表</returns>
        Task<IEnumerable<Building>> GetBuildingsWithCoordinatesAsync(string? cityCn = null);
    }
}