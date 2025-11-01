using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ExamSystem.Domain.Entities;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;
using System.Text.RegularExpressions;

namespace ExamSystem.Services.Services
{
    /// <summary>
    /// 建筑物管理服务实现
    /// </summary>
    public class BuildingService : IBuildingService
    {
        private readonly IBuildingRepository _buildingRepository;
        private readonly ILogger<BuildingService> _logger;

        public BuildingService(IBuildingRepository buildingRepository, ILogger<BuildingService> logger)
        {
            _buildingRepository = buildingRepository;
            _logger = logger;
        }

        /// <summary>
        /// 分页获取建筑物列表
        /// </summary>
        public async Task<PagedResult<Building>> GetBuildingsPagedAsync(int pageIndex, int pageSize, 
            string? searchKeyword = null, string? cityFilter = null, byte? typeFilter = null)
        {
            try
            {
                _logger.LogInformation("获取建筑物分页列表 - 页码:{PageIndex}, 页大小:{PageSize}, 关键词:{SearchKeyword}, 城市:{CityFilter}, 类型:{TypeFilter}", 
                    pageIndex, pageSize, searchKeyword, cityFilter, typeFilter);

                // 获取所有符合条件的建筑物
                var allBuildings = await _buildingRepository.GetAllAsync();
                
                // 应用筛选条件
                var query = allBuildings.Where(b => !b.Deleted);

                if (!string.IsNullOrWhiteSpace(searchKeyword))
                {
                    query = query.Where(b => 
                        (b.OrgName != null && b.OrgName.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase)) ||
                        (b.Address != null && b.Address.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase)) ||
                        (b.CityCn != null && b.CityCn.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase)));
                }

                if (!string.IsNullOrWhiteSpace(cityFilter))
                {
                    query = query.Where(b => b.CityCn == cityFilter);
                }

                if (typeFilter.HasValue)
                {
                    query = query.Where(b => b.OrgType == typeFilter.Value);
                }

                // 排序
                query = query.OrderBy(b => b.CityCn).ThenBy(b => b.OrgType).ThenBy(b => b.OrgName);

                // 计算总数
                var totalCount = query.Count();

                // 分页
                var items = query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

                var result = new PagedResult<Building>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };

                _logger.LogInformation("获取建筑物分页列表成功 - 总数:{TotalCount}, 当前页:{PageIndex}/{TotalPages}", 
                    totalCount, pageIndex, result.TotalPages);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取建筑物分页列表失败");
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取建筑物详情
        /// </summary>
        public async Task<Building?> GetBuildingByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("获取建筑物详情 - ID:{Id}", id);
                var building = await _buildingRepository.GetByIdAsync(id);
                
                if (building == null || building.Deleted)
                {
                    _logger.LogWarning("建筑物不存在或已删除 - ID:{Id}", id);
                    return null;
                }

                return building;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取建筑物详情失败 - ID:{Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建建筑物
        /// </summary>
        public async Task<ServiceResult<Building>> CreateBuildingAsync(Building building, string? operatorName = null)
        {
            try
            {
                _logger.LogInformation("【建筑物管理】创建建筑物开始 - 操作员:{Operator}, 名称:{OrgName}, 城市:{CityCn}, 类型:{OrgType}, 地址:{Address}", 
                    operatorName ?? "系统", building.OrgName, building.CityCn, building.OrgType, building.Address);

                // 数据验证
                var validationResult = await ValidateBuildingAsync(building);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("【建筑物管理】创建建筑物失败 - 数据验证不通过 - 操作员:{Operator}, 错误:{Errors}", 
                        operatorName ?? "系统", string.Join("; ", validationResult.Errors));
                    return ServiceResult<Building>.Failure(string.Join("; ", validationResult.Errors));
                }

                // 检查名称重复
                if (await IsBuildingNameDuplicateAsync(building.OrgName!, building.CityCn!))
                {
                    _logger.LogWarning("【建筑物管理】创建建筑物失败 - 名称重复 - 操作员:{Operator}, 名称:{OrgName}, 城市:{CityCn}", 
                        operatorName ?? "系统", building.OrgName, building.CityCn);
                    return ServiceResult<Building>.Failure("该城市已存在同名建筑物");
                }

                // 设置创建时间
                building.CreateDate = DateTime.Now;
                building.UpdateDate = DateTime.Now;

                // 保存到数据库
                var createdBuilding = await _buildingRepository.AddAsync(building);

                _logger.LogInformation("【建筑物管理】创建建筑物成功 - 操作员:{Operator}, ID:{Id}, 名称:{OrgName}, 城市:{CityCn}, 类型:{OrgType}", 
                    operatorName ?? "系统", createdBuilding.Id, createdBuilding.OrgName, createdBuilding.CityCn, createdBuilding.OrgType);
                return ServiceResult<Building>.Success(createdBuilding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "【建筑物管理】创建建筑物异常 - 操作员:{Operator}, 名称:{OrgName}", 
                    operatorName ?? "系统", building.OrgName);
                return ServiceResult<Building>.Failure($"创建建筑物失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新建筑物
        /// </summary>
        public async Task<ServiceResult<Building>> UpdateBuildingAsync(Building building, string? operatorName = null)
        {
            try
            {
                _logger.LogInformation("【建筑物管理】更新建筑物开始 - 操作员:{Operator}, ID:{Id}, 名称:{OrgName}, 城市:{CityCn}", 
                    operatorName ?? "系统", building.Id, building.OrgName, building.CityCn);

                // 检查建筑物是否存在
                var existingBuilding = await GetBuildingByIdAsync(building.Id);
                if (existingBuilding == null)
                {
                    _logger.LogWarning("【建筑物管理】更新建筑物失败 - 建筑物不存在 - 操作员:{Operator}, ID:{Id}", 
                        operatorName ?? "系统", building.Id);
                    return ServiceResult<Building>.Failure("建筑物不存在");
                }

                // 记录更新前的信息
                _logger.LogInformation("【建筑物管理】更新前信息 - 操作员:{Operator}, ID:{Id}, 原名称:{OldName}, 原城市:{OldCity}, 原类型:{OldType}, 原地址:{OldAddress}", 
                    operatorName ?? "系统", building.Id, existingBuilding.OrgName, existingBuilding.CityCn, existingBuilding.OrgType, existingBuilding.Address);

                // 数据验证
                var validationResult = await ValidateBuildingAsync(building);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("【建筑物管理】更新建筑物失败 - 数据验证不通过 - 操作员:{Operator}, ID:{Id}, 错误:{Errors}", 
                        operatorName ?? "系统", building.Id, string.Join("; ", validationResult.Errors));
                    return ServiceResult<Building>.Failure(string.Join("; ", validationResult.Errors));
                }

                // 检查名称重复（排除自身）
                if (await IsBuildingNameDuplicateAsync(building.OrgName!, building.CityCn!, building.Id))
                {
                    _logger.LogWarning("【建筑物管理】更新建筑物失败 - 名称重复 - 操作员:{Operator}, ID:{Id}, 名称:{OrgName}, 城市:{CityCn}", 
                        operatorName ?? "系统", building.Id, building.OrgName, building.CityCn);
                    return ServiceResult<Building>.Failure("该城市已存在同名建筑物");
                }

                // 更新属性
                existingBuilding.OrgName = building.OrgName;
                existingBuilding.CityCn = building.CityCn;
                existingBuilding.Address = building.Address;
                existingBuilding.OrgType = building.OrgType;
                
                // 更新坐标信息
                var coordinates = building.GetCoordinates();
                if (coordinates != null && coordinates.Length == 2)
                {
                    existingBuilding.SetCoordinates(coordinates[0], coordinates[1]);
                }
                
                existingBuilding.UpdateDate = DateTime.Now;

                // 保存更改
                await _buildingRepository.UpdateAsync(existingBuilding);

                _logger.LogInformation("【建筑物管理】更新建筑物成功 - 操作员:{Operator}, ID:{Id}, 新名称:{NewName}, 新城市:{NewCity}, 新类型:{NewType}, 新地址:{NewAddress}", 
                    operatorName ?? "系统", building.Id, building.OrgName, building.CityCn, building.OrgType, building.Address);
                return ServiceResult<Building>.Success(existingBuilding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "【建筑物管理】更新建筑物异常 - 操作员:{Operator}, ID:{Id}", 
                    operatorName ?? "系统", building.Id);
                return ServiceResult<Building>.Failure($"更新建筑物失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 删除建筑物
        /// </summary>
        public async Task<ServiceResult> DeleteBuildingAsync(int id, string? operatorName = null)
        {
            try
            {
                _logger.LogInformation("【建筑物管理】删除建筑物开始 - 操作员:{Operator}, ID:{Id}", 
                    operatorName ?? "系统", id);

                var building = await GetBuildingByIdAsync(id);
                if (building == null)
                {
                    _logger.LogWarning("【建筑物管理】删除建筑物失败 - 建筑物不存在 - 操作员:{Operator}, ID:{Id}", 
                        operatorName ?? "系统", id);
                    return ServiceResult.Failure("建筑物不存在");
                }

                // 记录删除的建筑物信息
                _logger.LogInformation("【建筑物管理】即将删除建筑物 - 操作员:{Operator}, ID:{Id}, 名称:{OrgName}, 城市:{CityCn}, 类型:{OrgType}, 地址:{Address}", 
                    operatorName ?? "系统", id, building.OrgName, building.CityCn, building.OrgType, building.Address);

                // 软删除
                building.Deleted = true;
                building.UpdateDate = DateTime.Now;

                await _buildingRepository.UpdateAsync(building);

                _logger.LogInformation("【建筑物管理】删除建筑物成功 - 操作员:{Operator}, ID:{Id}, 名称:{OrgName}", 
                    operatorName ?? "系统", id, building.OrgName);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "【建筑物管理】删除建筑物异常 - 操作员:{Operator}, ID:{Id}", 
                    operatorName ?? "系统", id);
                return ServiceResult.Failure($"删除建筑物失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量删除建筑物
        /// </summary>
        public async Task<ServiceResult> BatchDeleteBuildingsAsync(IEnumerable<int> ids, string? operatorName = null)
        {
            try
            {
                var idList = ids.ToList();
                _logger.LogInformation("【建筑物管理】批量删除建筑物开始 - 操作员:{Operator}, 数量:{Count}, IDs:{Ids}", 
                    operatorName ?? "系统", idList.Count, string.Join(",", idList));

                var deletedBuildings = new List<string>();
                var notFoundIds = new List<int>();

                foreach (var id in idList)
                {
                    var building = await GetBuildingByIdAsync(id);
                    if (building != null)
                    {
                        deletedBuildings.Add($"{building.OrgName}({building.CityCn})");
                        building.Deleted = true;
                        building.UpdateDate = DateTime.Now;
                        await _buildingRepository.UpdateAsync(building);
                        
                        _logger.LogInformation("【建筑物管理】批量删除项目 - 操作员:{Operator}, ID:{Id}, 名称:{OrgName}, 城市:{CityCn}", 
                            operatorName ?? "系统", id, building.OrgName, building.CityCn);
                    }
                    else
                    {
                        notFoundIds.Add(id);
                        _logger.LogWarning("【建筑物管理】批量删除跳过 - 建筑物不存在 - 操作员:{Operator}, ID:{Id}", 
                            operatorName ?? "系统", id);
                    }
                }

                _logger.LogInformation("【建筑物管理】批量删除建筑物完成 - 操作员:{Operator}, 成功删除:{SuccessCount}, 未找到:{NotFoundCount}, 删除列表:{DeletedList}", 
                    operatorName ?? "系统", deletedBuildings.Count, notFoundIds.Count, string.Join("; ", deletedBuildings));
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "【建筑物管理】批量删除建筑物异常 - 操作员:{Operator}", 
                    operatorName ?? "系统");
                return ServiceResult.Failure($"批量删除建筑物失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有城市列表
        /// </summary>
        public async Task<IEnumerable<string>> GetCitiesAsync()
        {
            try
            {
                return await _buildingRepository.GetCitiesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取城市列表失败");
                throw;
            }
        }

        /// <summary>
        /// 获取建筑物类型统计
        /// </summary>
        public async Task<Dictionary<byte, int>> GetBuildingTypeStatisticsAsync(string? cityFilter = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cityFilter))
                {
                    var allBuildings = await _buildingRepository.GetAllAsync();
                    return allBuildings.Where(b => !b.Deleted)
                        .GroupBy(b => b.OrgType)
                        .ToDictionary(g => g.Key, g => g.Count());
                }
                else
                {
                    return await _buildingRepository.GetBuildingCountByCityAsync(cityFilter);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取建筑物类型统计失败");
                throw;
            }
        }

        /// <summary>
        /// 验证建筑物数据
        /// </summary>
        public async Task<ValidationResult> ValidateBuildingAsync(Building building)
        {
            var result = new ValidationResult();

            // 必填字段验证
            if (string.IsNullOrWhiteSpace(building.OrgName))
            {
                result.AddError("机构名称不能为空");
            }
            else if (building.OrgName.Length > 100)
            {
                result.AddError("机构名称长度不能超过100个字符");
            }

            if (string.IsNullOrWhiteSpace(building.CityCn))
            {
                result.AddError("城市不能为空");
            }
            else if (building.CityCn.Length > 50)
            {
                result.AddError("城市名称长度不能超过50个字符");
            }

            if (string.IsNullOrWhiteSpace(building.Address))
            {
                result.AddError("地址不能为空");
            }
            else if (building.Address.Length > 200)
            {
                result.AddError("地址长度不能超过200个字符");
            }

            // 机构类型验证
            if (building.OrgType < 1 || building.OrgType > 3)
            {
                result.AddError("机构类型必须为1-消防队站、2-专职队或3-重点建筑");
            }

            // 坐标验证
            var coordinates = building.GetCoordinates();
            if (coordinates != null && coordinates.Length == 2)
            {
                var longitude = coordinates[0];
                var latitude = coordinates[1];
                
                if (longitude < -180 || longitude > 180)
                {
                    result.AddError("经度必须在-180到180之间");
                }

                if (latitude < -90 || latitude > 90)
                {
                    result.AddError("纬度必须在-90到90之间");
                }
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// 检查建筑物名称是否重复
        /// </summary>
        public async Task<bool> IsBuildingNameDuplicateAsync(string orgName, string cityCn, int? excludeId = null)
        {
            try
            {
                var buildings = await _buildingRepository.GetBuildingsByCityAsync(cityCn);
                return buildings.Any(b => !b.Deleted && 
                    b.OrgName == orgName && 
                    (excludeId == null || b.Id != excludeId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查建筑物名称重复失败");
                throw;
            }
        }

        /// <summary>
        /// 批量导入建筑物数据
        /// </summary>
        public async Task<ServiceResult<ImportResult>> BatchImportBuildingsAsync(IEnumerable<Building> buildings)
        {
            try
            {
                var buildingList = buildings.ToList();
                _logger.LogInformation("批量导入建筑物数据 - 数量:{Count}", buildingList.Count);

                var result = new ImportResult();
                var validBuildings = new List<Building>();

                foreach (var building in buildingList)
                {
                    // 验证数据
                    var validationResult = await ValidateBuildingAsync(building);
                    if (!validationResult.IsValid)
                    {
                        result.FailedItems.Add(new ImportFailedItem
                        {
                            Data = building.OrgName ?? "未知",
                            Errors = validationResult.Errors
                        });
                        continue;
                    }

                    // 检查重复
                    if (await IsBuildingNameDuplicateAsync(building.OrgName!, building.CityCn!))
                    {
                        result.FailedItems.Add(new ImportFailedItem
                        {
                            Data = building.OrgName!,
                            Errors = new List<string> { "该城市已存在同名建筑物" }
                        });
                        continue;
                    }

                    // 设置时间戳
                    building.CreateDate = DateTime.Now;
                    building.UpdateDate = DateTime.Now;

                    validBuildings.Add(building);
                }

                // 批量保存有效数据
                if (validBuildings.Any())
                {
                    foreach (var building in validBuildings)
                    {
                        await _buildingRepository.AddAsync(building);
                    }
                }

                result.TotalCount = buildingList.Count;
                result.SuccessCount = validBuildings.Count;
                result.FailureCount = result.FailedItems.Count;

                _logger.LogInformation("批量导入建筑物数据完成 - 总数:{TotalCount}, 成功:{SuccessCount}, 失败:{FailedCount}", 
                    result.TotalCount, result.SuccessCount, result.FailedCount);

                return ServiceResult<ImportResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入建筑物数据失败");
                return ServiceResult<ImportResult>.Failure($"批量导入失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 导出建筑物数据
        /// </summary>
        public async Task<IEnumerable<Building>> ExportBuildingsAsync(string? searchKeyword = null, 
            string? cityFilter = null, byte? typeFilter = null)
        {
            try
            {
                _logger.LogInformation("导出建筑物数据 - 关键词:{SearchKeyword}, 城市:{CityFilter}, 类型:{TypeFilter}", 
                    searchKeyword, cityFilter, typeFilter);

                // 获取所有符合条件的建筑物（不分页）
                var pagedResult = await GetBuildingsPagedAsync(1, int.MaxValue, searchKeyword, cityFilter, typeFilter);
                return pagedResult.Items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出建筑物数据失败");
                throw;
            }
        }
    }
}