using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ExamSystem.Domain.Entities;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Services;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.Tests.Integration
{
    /// <summary>
    /// 建筑物管理系统集成测试
    /// </summary>
    public class BuildingManagementIntegrationTests : IDisposable
    {
        private readonly ExamDbContext _context;
        private readonly IBuildingService _buildingService;
        private readonly IBuildingRepository _buildingRepository;

        public BuildingManagementIntegrationTests()
        {
            // 配置内存数据库
            var options = new DbContextOptionsBuilder<ExamDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ExamDbContext(options);
            _buildingRepository = new BuildingRepository(_context);
            
            // 配置日志
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole())
                .BuildServiceProvider();
            
            var logger = serviceProvider.GetRequiredService<ILogger<BuildingService>>();
            _buildingService = new BuildingService(_buildingRepository, logger);

            // 确保数据库已创建
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task CompleteWorkflow_CreateUpdateDelete_WorksCorrectly()
        {
            // 1. 创建建筑物
            var building = new Building
            {
                OrgName = "集成测试机构",
                CityCn = "北京",
                Address = "集成测试地址123号",
                OrgType = 1,
                Longitude = 116.4074,
                Latitude = 39.9042
            };

            var createResult = await _buildingService.CreateBuildingAsync(building, "集成测试操作员");
            
            Assert.True(createResult.IsSuccess);
            Assert.NotNull(createResult.Data);
            Assert.True(createResult.Data.Id > 0);

            var createdId = createResult.Data.Id;

            // 2. 验证创建成功
            var retrievedBuilding = await _buildingService.GetBuildingByIdAsync(createdId);
            Assert.NotNull(retrievedBuilding);
            Assert.Equal("集成测试机构", retrievedBuilding.OrgName);
            Assert.Equal("北京", retrievedBuilding.CityCn);

            // 3. 更新建筑物
            retrievedBuilding.OrgName = "更新后的机构名称";
            retrievedBuilding.Address = "更新后的地址";
            
            var updateResult = await _buildingService.UpdateBuildingAsync(retrievedBuilding, "集成测试操作员");
            
            Assert.True(updateResult.IsSuccess);
            Assert.Equal("更新后的机构名称", updateResult.Data!.OrgName);
            Assert.Equal("更新后的地址", updateResult.Data.Address);

            // 4. 验证更新成功
            var updatedBuilding = await _buildingService.GetBuildingByIdAsync(createdId);
            Assert.Equal("更新后的机构名称", updatedBuilding!.OrgName);
            Assert.Equal("更新后的地址", updatedBuilding.Address);

            // 5. 删除建筑物
            var deleteResult = await _buildingService.DeleteBuildingAsync(createdId, "集成测试操作员");
            
            Assert.True(deleteResult.IsSuccess);

            // 6. 验证软删除成功
            var deletedBuilding = await _buildingService.GetBuildingByIdAsync(createdId);
            Assert.Null(deletedBuilding); // 软删除后应该查询不到

            // 7. 验证数据库中记录仍存在但标记为已删除
            var allBuildings = await _buildingRepository.GetAllAsync();
            var deletedRecord = allBuildings.FirstOrDefault(b => b.Id == createdId);
            Assert.NotNull(deletedRecord);
            Assert.True(deletedRecord.Deleted);
        }

        [Fact]
        public async Task BatchOperations_ImportAndExport_WorksCorrectly()
        {
            // 1. 准备批量导入数据
            var buildings = new[]
            {
                new Building
                {
                    OrgName = "批量测试机构1",
                    CityCn = "上海",
                    Address = "批量测试地址1",
                    OrgType = 1,
                    Longitude = 121.4737,
                    Latitude = 31.2304
                },
                new Building
                {
                    OrgName = "批量测试机构2",
                    CityCn = "广州",
                    Address = "批量测试地址2",
                    OrgType = 2,
                    Longitude = 113.2644,
                    Latitude = 23.1291
                },
                new Building
                {
                    OrgName = "批量测试机构3",
                    CityCn = "深圳",
                    Address = "批量测试地址3",
                    OrgType = 3,
                    Longitude = 114.0579,
                    Latitude = 22.5431
                }
            };

            // 2. 批量导入
            var importResult = await _buildingService.BatchImportBuildingsAsync(buildings);
            
            Assert.True(importResult.IsSuccess);
            Assert.Equal(3, importResult.Data!.TotalCount);
            Assert.Equal(3, importResult.Data.SuccessCount);
            Assert.Equal(0, importResult.Data.FailedCount);

            // 3. 验证导入成功
            var pagedResult = await _buildingService.GetBuildingsPagedAsync(1, 10);
            Assert.True(pagedResult.Items.Count() >= 3);

            // 4. 测试导出功能
            var exportedBuildings = await _buildingService.ExportBuildingsAsync();
            Assert.True(exportedBuildings.Count() >= 3);

            // 5. 测试按城市筛选导出
            var shanghaiBuildings = await _buildingService.ExportBuildingsAsync(cityFilter: "上海");
            Assert.Single(shanghaiBuildings);
            Assert.Equal("批量测试机构1", shanghaiBuildings.First().OrgName);

            // 6. 测试按类型筛选导出
            var type2Buildings = await _buildingService.ExportBuildingsAsync(typeFilter: 2);
            Assert.Single(type2Buildings);
            Assert.Equal("批量测试机构2", type2Buildings.First().OrgName);
        }

        [Fact]
        public async Task ValidationAndDuplicateCheck_WorksCorrectly()
        {
            // 1. 创建第一个建筑物
            var building1 = new Building
            {
                OrgName = "重复测试机构",
                CityCn = "北京",
                Address = "重复测试地址",
                OrgType = 1,
                Longitude = 116.4074,
                Latitude = 39.9042
            };

            var result1 = await _buildingService.CreateBuildingAsync(building1, "测试操作员");
            Assert.True(result1.IsSuccess);

            // 2. 尝试创建同名建筑物（应该失败）
            var building2 = new Building
            {
                OrgName = "重复测试机构", // 同名
                CityCn = "北京", // 同城市
                Address = "不同的地址",
                OrgType = 2,
                Longitude = 116.4074,
                Latitude = 39.9042
            };

            var result2 = await _buildingService.CreateBuildingAsync(building2, "测试操作员");
            Assert.False(result2.IsSuccess);
            Assert.Contains("该城市已存在同名建筑物", result2.ErrorMessage);

            // 3. 在不同城市创建同名建筑物（应该成功）
            var building3 = new Building
            {
                OrgName = "重复测试机构", // 同名
                CityCn = "上海", // 不同城市
                Address = "上海测试地址",
                OrgType = 1,
                Longitude = 121.4737,
                Latitude = 31.2304
            };

            var result3 = await _buildingService.CreateBuildingAsync(building3, "测试操作员");
            Assert.True(result3.IsSuccess);

            // 4. 测试数据验证
            var invalidBuilding = new Building
            {
                OrgName = "", // 无效：空名称
                CityCn = "北京",
                Address = "测试地址",
                OrgType = 1
            };

            var validationResult = await _buildingService.ValidateBuildingAsync(invalidBuilding);
            Assert.False(validationResult.IsValid);
            Assert.Contains("机构名称不能为空", validationResult.Errors);
        }

        [Fact]
        public async Task PaginationAndFiltering_WorksCorrectly()
        {
            // 1. 创建测试数据
            var testBuildings = new[]
            {
                new Building { OrgName = "北京消防站1", CityCn = "北京", Address = "地址1", OrgType = 1, Longitude = 116.4074, Latitude = 39.9042 },
                new Building { OrgName = "北京消防站2", CityCn = "北京", Address = "地址2", OrgType = 1, Longitude = 116.4074, Latitude = 39.9042 },
                new Building { OrgName = "上海专职队1", CityCn = "上海", Address = "地址3", OrgType = 2, Longitude = 121.4737, Latitude = 31.2304 },
                new Building { OrgName = "上海重点建筑1", CityCn = "上海", Address = "地址4", OrgType = 3, Longitude = 121.4737, Latitude = 31.2304 },
                new Building { OrgName = "广州消防站1", CityCn = "广州", Address = "地址5", OrgType = 1, Longitude = 113.2644, Latitude = 23.1291 }
            };

            foreach (var building in testBuildings)
            {
                await _buildingService.CreateBuildingAsync(building, "测试操作员");
            }

            // 2. 测试分页
            var page1 = await _buildingService.GetBuildingsPagedAsync(1, 2);
            Assert.Equal(2, page1.Items.Count());
            Assert.True(page1.TotalCount >= 5);
            Assert.True(page1.TotalPages >= 3);

            var page2 = await _buildingService.GetBuildingsPagedAsync(2, 2);
            Assert.Equal(2, page2.Items.Count());

            // 3. 测试城市筛选
            var beijingBuildings = await _buildingService.GetBuildingsPagedAsync(1, 10, cityFilter: "北京");
            Assert.Equal(2, beijingBuildings.Items.Count());
            Assert.All(beijingBuildings.Items, b => Assert.Equal("北京", b.CityCn));

            // 4. 测试类型筛选
            var type1Buildings = await _buildingService.GetBuildingsPagedAsync(1, 10, typeFilter: 1);
            Assert.Equal(3, type1Buildings.Items.Count());
            Assert.All(type1Buildings.Items, b => Assert.Equal((byte)1, b.OrgType));

            // 5. 测试关键词搜索
            var searchResults = await _buildingService.GetBuildingsPagedAsync(1, 10, searchKeyword: "专职队");
            Assert.Single(searchResults.Items);
            Assert.Contains("专职队", searchResults.Items.First().OrgName);

            // 6. 测试组合筛选
            var combinedResults = await _buildingService.GetBuildingsPagedAsync(1, 10, 
                searchKeyword: "消防站", cityFilter: "北京");
            Assert.Equal(2, combinedResults.Items.Count());
            Assert.All(combinedResults.Items, b => 
            {
                Assert.Equal("北京", b.CityCn);
                Assert.Contains("消防站", b.OrgName);
            });
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}