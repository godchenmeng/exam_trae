using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamSystem.Domain.Entities;
using ExamSystem.Services.Models;

namespace ExamSystem.Services.Interfaces
{
    /// <summary>
    /// 建筑物管理服务接口
    /// </summary>
    public interface IBuildingService
    {
        /// <summary>
        /// 分页获取建筑物列表
        /// </summary>
        /// <param name="pageIndex">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="searchKeyword">搜索关键词</param>
        /// <param name="cityFilter">城市筛选</param>
        /// <param name="typeFilter">类型筛选（1-消防队站；2-专职队；3-重点建筑）</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<Building>> GetBuildingsPagedAsync(int pageIndex, int pageSize, 
            string? searchKeyword = null, string? cityFilter = null, byte? typeFilter = null);

        /// <summary>
        /// 根据ID获取建筑物详情
        /// </summary>
        /// <param name="id">建筑物ID</param>
        /// <returns>建筑物实体</returns>
        Task<Building?> GetBuildingByIdAsync(int id);

        /// <summary>
        /// 创建建筑物
        /// </summary>
        /// <param name="building">建筑物实体</param>
        /// <param name="operatorName">操作员名称</param>
        /// <returns>创建结果</returns>
        Task<ServiceResult<Building>> CreateBuildingAsync(Building building, string? operatorName = null);

        /// <summary>
        /// 更新建筑物
        /// </summary>
        /// <param name="building">建筑物实体</param>
        /// <param name="operatorName">操作员名称</param>
        /// <returns>更新结果</returns>
        Task<ServiceResult<Building>> UpdateBuildingAsync(Building building, string? operatorName = null);

        /// <summary>
        /// 删除建筑物
        /// </summary>
        /// <param name="id">建筑物ID</param>
        /// <param name="operatorName">操作员名称</param>
        /// <returns>删除结果</returns>
        Task<ServiceResult> DeleteBuildingAsync(int id, string? operatorName = null);

        /// <summary>
        /// 批量删除建筑物
        /// </summary>
        /// <param name="ids">建筑物ID列表</param>
        /// <param name="operatorName">操作员名称</param>
        /// <returns>删除结果</returns>
        Task<ServiceResult> BatchDeleteBuildingsAsync(IEnumerable<int> ids, string? operatorName = null);

        /// <summary>
        /// 获取所有城市列表
        /// </summary>
        /// <returns>城市列表</returns>
        Task<IEnumerable<string>> GetCitiesAsync();

        /// <summary>
        /// 获取建筑物类型统计
        /// </summary>
        /// <param name="cityFilter">城市筛选</param>
        /// <returns>类型统计</returns>
        Task<Dictionary<byte, int>> GetBuildingTypeStatisticsAsync(string? cityFilter = null);

        /// <summary>
        /// 验证建筑物数据
        /// </summary>
        /// <param name="building">建筑物实体</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateBuildingAsync(Building building);

        /// <summary>
        /// 检查建筑物名称是否重复
        /// </summary>
        /// <param name="orgName">机构名称</param>
        /// <param name="cityCn">城市</param>
        /// <param name="excludeId">排除的ID（用于更新时检查）</param>
        /// <returns>是否重复</returns>
        Task<bool> IsBuildingNameDuplicateAsync(string orgName, string cityCn, int? excludeId = null);

        /// <summary>
        /// 批量导入建筑物数据
        /// </summary>
        /// <param name="buildings">建筑物列表</param>
        /// <returns>导入结果</returns>
        Task<ServiceResult<ImportResult>> BatchImportBuildingsAsync(IEnumerable<Building> buildings);

        /// <summary>
        /// 导出建筑物数据
        /// </summary>
        /// <param name="searchKeyword">搜索关键词</param>
        /// <param name="cityFilter">城市筛选</param>
        /// <param name="typeFilter">类型筛选</param>
        /// <returns>建筑物列表</returns>
        Task<IEnumerable<Building>> ExportBuildingsAsync(string? searchKeyword = null, 
            string? cityFilter = null, byte? typeFilter = null);
    }
}