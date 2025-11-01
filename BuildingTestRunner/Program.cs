using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ExamSystem.Domain.Entities;
using ExamSystem.Data;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Services;
using ExamSystem.Services.Interfaces;

namespace ExamSystem.Tests
{
    /// <summary>
    /// 建筑物管理系统功能验证程序
    /// </summary>
    class BuildingTestRunner
    {
        private static IBuildingService? _buildingService;
        private static ExamDbContext? _context;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 建筑物管理系统功能验证 ===");
            Console.WriteLine();

            try
            {
                // 初始化服务
                await InitializeServicesAsync();

                // 运行测试
                await RunBasicCrudTestAsync();
                await RunValidationTestAsync();
                await RunBatchOperationsTestAsync();
                await RunPaginationTestAsync();

                Console.WriteLine();
                Console.WriteLine("✅ 所有测试通过！建筑物管理系统功能正常。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            finally
            {
                _context?.Dispose();
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        private static async Task InitializeServicesAsync()
        {
            Console.WriteLine("🔧 初始化服务...");

            // 配置内存数据库
            var options = new DbContextOptionsBuilder<ExamDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ExamDbContext(options);
            var buildingRepository = new BuildingRepository(_context);

            // 配置日志
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<BuildingService>>();
            _buildingService = new BuildingService(buildingRepository, logger);

            // 确保数据库已创建
            await _context.Database.EnsureCreatedAsync();

            Console.WriteLine("✅ 服务初始化完成");
        }

        private static async Task RunBasicCrudTestAsync()
        {
            Console.WriteLine();
            Console.WriteLine("🧪 测试基本CRUD操作...");

            // 1. 创建建筑物
            var building = new Building
            {
                OrgName = "测试消防站",
                CityCn = "北京",
                Address = "北京市朝阳区测试路123号",
                OrgType = 1
            };
            building.SetCoordinates(116.4074, 39.9042);

            // 验证建筑物数据
            var validation = await _buildingService!.ValidateBuildingAsync(building);
            if (!validation.IsValid)
            {
                Console.WriteLine($"验证失败，错误信息：");
                foreach (var error in validation.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                throw new Exception($"建筑物数据验证失败");
            }

            var createResult = await _buildingService!.CreateBuildingAsync(building, "测试操作员");
            if (!createResult.IsSuccess)
                throw new Exception($"创建建筑物失败: {createResult.ErrorMessage}");

            var createdId = createResult.Data!.Id;
            Console.WriteLine($"  ✅ 创建建筑物成功，ID: {createdId}");

            // 2. 查询建筑物
            var retrievedBuilding = await _buildingService.GetBuildingByIdAsync(createdId);
            if (retrievedBuilding == null)
                throw new Exception("查询建筑物失败");

            Console.WriteLine($"  ✅ 查询建筑物成功: {retrievedBuilding.OrgName}");

            // 3. 更新建筑物
            retrievedBuilding.OrgName = "更新后的消防站";
            retrievedBuilding.Address = "更新后的地址";

            var updateResult = await _buildingService.UpdateBuildingAsync(retrievedBuilding, "测试操作员");
            if (!updateResult.IsSuccess)
                throw new Exception($"更新建筑物失败: {updateResult.ErrorMessage}");

            Console.WriteLine($"  ✅ 更新建筑物成功: {updateResult.Data!.OrgName}");

            // 4. 删除建筑物
            var deleteResult = await _buildingService.DeleteBuildingAsync(createdId, "测试操作员");
            if (!deleteResult.IsSuccess)
                throw new Exception($"删除建筑物失败: {deleteResult.ErrorMessage}");

            Console.WriteLine("  ✅ 删除建筑物成功");

            // 5. 验证软删除
            var deletedBuilding = await _buildingService.GetBuildingByIdAsync(createdId);
            if (deletedBuilding != null)
                throw new Exception("软删除验证失败，建筑物仍可查询");

            Console.WriteLine("  ✅ 软删除验证成功");
        }

        private static async Task RunValidationTestAsync()
        {
            Console.WriteLine();
            Console.WriteLine("🧪 测试数据验证...");

            // 1. 测试必填字段验证
            var invalidBuilding = new Building
            {
                OrgName = "", // 空名称
                CityCn = "北京",
                Address = "测试地址",
                OrgType = 1
            };

            var validationResult = await _buildingService!.ValidateBuildingAsync(invalidBuilding);
            if (validationResult.IsValid)
                throw new Exception("数据验证失败，应该检测到空名称错误");

            Console.WriteLine("  ✅ 必填字段验证成功");

            // 2. 测试重复名称检查
            var building1 = new Building
            {
                OrgName = "重复测试机构",
                CityCn = "北京",
                Address = "地址1",
                OrgType = 1
            };
            building1.SetCoordinates(116.4074, 39.9042);

            var result1 = await _buildingService.CreateBuildingAsync(building1, "测试操作员");
            if (!result1.IsSuccess)
                throw new Exception($"创建第一个建筑物失败: {result1.ErrorMessage}");

            var building2 = new Building
            {
                OrgName = "重复测试机构", // 同名
                CityCn = "北京", // 同城市
                Address = "地址2",
                OrgType = 2
            };
            building2.SetCoordinates(116.4074, 39.9042);

            var result2 = await _buildingService.CreateBuildingAsync(building2, "测试操作员");
            if (result2.IsSuccess)
                throw new Exception("重复名称检查失败，应该阻止创建同名建筑物");

            Console.WriteLine("  ✅ 重复名称检查成功");

            // 3. 测试坐标验证
            var invalidCoordBuilding = new Building
            {
                OrgName = "坐标测试机构",
                CityCn = "北京",
                Address = "测试地址",
                OrgType = 1
            };
            invalidCoordBuilding.SetCoordinates(200, 39.9042); // 无效经度

            var coordValidation = await _buildingService.ValidateBuildingAsync(invalidCoordBuilding);
            if (coordValidation.IsValid)
                throw new Exception("坐标验证失败，应该检测到无效经度");

            Console.WriteLine("  ✅ 坐标验证成功");
        }

        private static async Task RunBatchOperationsTestAsync()
        {
            Console.WriteLine();
            Console.WriteLine("🧪 测试批量操作...");

            // 1. 准备批量数据
            var buildings = new[]
            {
                new Building
                {
                    OrgName = "批量测试机构1",
                    CityCn = "上海",
                    Address = "上海市浦东新区测试路1号",
                    OrgType = 1
                },
                new Building
                {
                    OrgName = "批量测试机构2",
                    CityCn = "广州",
                    Address = "广州市天河区测试路2号",
                    OrgType = 2
                },
                new Building
                {
                    OrgName = "批量测试机构3",
                    CityCn = "深圳",
                    Address = "深圳市南山区测试路3号",
                    OrgType = 3
                }
            };

            // 设置坐标
            buildings[0].SetCoordinates(121.4737, 31.2304);
            buildings[1].SetCoordinates(113.2644, 23.1291);
            buildings[2].SetCoordinates(114.0579, 22.5431);

            // 2. 批量导入
            var importResult = await _buildingService!.BatchImportBuildingsAsync(buildings);
            if (!importResult.IsSuccess)
                throw new Exception($"批量导入失败: {importResult.ErrorMessage}");

            if (importResult.Data!.SuccessCount != 3)
                throw new Exception($"批量导入数量不正确，期望3个，实际{importResult.Data.SuccessCount}个");

            Console.WriteLine($"  ✅ 批量导入成功: {importResult.Data.SuccessCount}条记录");

            // 3. 测试导出
            var exportedBuildings = await _buildingService.ExportBuildingsAsync();
            if (exportedBuildings.Count() < 3)
                throw new Exception("导出数据数量不足");

            Console.WriteLine($"  ✅ 数据导出成功: {exportedBuildings.Count()}条记录");

            // 4. 测试筛选导出
            var shanghaiBuildings = await _buildingService.ExportBuildingsAsync(cityFilter: "上海");
            if (shanghaiBuildings.Count() != 1)
                throw new Exception("按城市筛选导出失败");

            Console.WriteLine("  ✅ 筛选导出成功");
        }

        private static async Task RunPaginationTestAsync()
        {
            Console.WriteLine();
            Console.WriteLine("🧪 测试分页和筛选...");

            // 1. 测试分页
            var pagedResult = await _buildingService!.GetBuildingsPagedAsync(1, 2);
            if (pagedResult.Items.Count() > 2)
                throw new Exception("分页大小控制失败");

            Console.WriteLine($"  ✅ 分页查询成功: 第1页，每页2条，共{pagedResult.TotalCount}条");

            // 2. 测试城市筛选
            var beijingBuildings = await _buildingService.GetBuildingsPagedAsync(1, 10, cityFilter: "上海");
            if (beijingBuildings.Items.Any(b => b.CityCn != "上海"))
                throw new Exception("城市筛选失败");

            Console.WriteLine($"  ✅ 城市筛选成功: 上海地区{beijingBuildings.Items.Count()}条记录");

            // 3. 测试类型筛选
            var type1Buildings = await _buildingService.GetBuildingsPagedAsync(1, 10, typeFilter: 1);
            if (type1Buildings.Items.Any(b => b.OrgType != 1))
                throw new Exception("类型筛选失败");

            Console.WriteLine($"  ✅ 类型筛选成功: 消防队站{type1Buildings.Items.Count()}条记录");

            // 4. 测试关键词搜索
            var searchResults = await _buildingService.GetBuildingsPagedAsync(1, 10, searchKeyword: "批量测试");
            if (!searchResults.Items.Any())
                throw new Exception("关键词搜索失败");

            Console.WriteLine($"  ✅ 关键词搜索成功: 找到{searchResults.Items.Count()}条匹配记录");
        }
    }
}