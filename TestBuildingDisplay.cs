using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ExamSystem.Services.Interfaces;
using ExamSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Tests
{
    class TestBuildingDisplay
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 建筑物数据显示测试 ===");
            
            // 配置服务
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // 配置数据库
                    services.AddDbContext<ExamDbContext>(options =>
                        options.UseSqlite("Data Source=ExamSystem.WPF/exam_system.db"));
                    
                    // 注册服务
                    services.AddScoped<IBuildingService, ExamSystem.Services.Services.BuildingService>();
                    services.AddScoped<ExamSystem.Infrastructure.Data.Repositories.IBuildingRepository, 
                        ExamSystem.Infrastructure.Data.Repositories.BuildingRepository>();
                })
                .Build();

            using var scope = host.Services.CreateScope();
            var buildingService = scope.ServiceProvider.GetRequiredService<IBuildingService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TestBuildingDisplay>>();

            try
            {
                Console.WriteLine("1. 测试获取所有建筑物...");
                var allBuildings = await buildingService.GetAllBuildingsAsync();
                Console.WriteLine($"   总建筑物数量: {allBuildings.Count()}");
                
                foreach (var building in allBuildings.Take(5))
                {
                    Console.WriteLine($"   - {building.Name} ({building.City}) - {building.Type}");
                }
                
                Console.WriteLine("\n2. 测试分页获取建筑物...");
                var pagedResult = await buildingService.GetBuildingsPagedAsync(1, 10);
                Console.WriteLine($"   分页结果 - 总数: {pagedResult.TotalCount}, 当前页: {pagedResult.Items.Count()}");
                
                foreach (var building in pagedResult.Items)
                {
                    Console.WriteLine($"   - {building.Name} ({building.City}) - {building.Type}");
                }
                
                Console.WriteLine("\n3. 测试获取城市列表...");
                var cities = await buildingService.GetCitiesAsync();
                Console.WriteLine($"   城市数量: {cities.Count()}");
                foreach (var city in cities.Take(10))
                {
                    Console.WriteLine($"   - {city}");
                }
                
                Console.WriteLine("\n4. 测试获取统计信息...");
                var stats = await buildingService.GetBuildingStatisticsAsync();
                Console.WriteLine($"   消防队站: {stats.FireStationCount}");
                Console.WriteLine($"   专职队: {stats.SpecializedTeamCount}");
                Console.WriteLine($"   重点建筑: {stats.KeyBuildingCount}");
                
                Console.WriteLine("\n✅ 所有测试通过！建筑物数据加载正常。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}