using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;
using ExamSystem.Infrastructure.Repositories;

namespace TestBuildingData
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 建筑数据测试程序 ===");
            
            // 配置服务
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddDbContext<ExamDbContext>(options =>
                options.UseSqlite("Data Source=exam_system.db"));
            services.AddScoped<IBuildingRepository, BuildingRepository>();
            
            var serviceProvider = services.BuildServiceProvider();
            
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ExamDbContext>();
                var buildingRepository = scope.ServiceProvider.GetRequiredService<IBuildingRepository>();
                
                // 确保数据库存在
                await context.Database.EnsureCreatedAsync();
                
                // 检查是否已有测试数据
                var existingBuildings = await buildingRepository.GetBuildingsByCityAsync("北京");
                if (!existingBuildings.Any())
                {
                    Console.WriteLine("正在创建测试数据...");
                    await CreateTestData(context);
                }
                
                // 测试数据加载
                await TestBuildingDataLoading(buildingRepository);
                
                Console.WriteLine("测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
        }
        
        static async Task CreateTestData(ExamDbContext context)
        {
            var testBuildings = new List<Building>
            {
                // 北京消防队站
                new Building
                {
                    CityCn = "北京",
                    OrgName = "北京市消防救援总队朝阳支队",
                    OrgType = 1, // 消防队站
                    Address = "北京市朝阳区建国路88号",
                    Longitude = 116.4074,
                    Latitude = 39.9042,
                    Phone = "010-12345678",
                    Description = "朝阳区主要消防救援力量"
                },
                new Building
                {
                    CityCn = "北京",
                    OrgName = "北京市消防救援总队海淀支队",
                    OrgType = 1, // 消防队站
                    Address = "北京市海淀区中关村大街1号",
                    Longitude = 116.3074,
                    Latitude = 39.9642,
                    Phone = "010-87654321",
                    Description = "海淀区主要消防救援力量"
                },
                
                // 北京专职队
                new Building
                {
                    CityCn = "北京",
                    OrgName = "中关村科技园专职消防队",
                    OrgType = 2, // 专职队
                    Address = "北京市海淀区中关村软件园",
                    Longitude = 116.2974,
                    Latitude = 39.9742,
                    Phone = "010-11223344",
                    Description = "中关村科技园区专职消防力量"
                },
                new Building
                {
                    CityCn = "北京",
                    OrgName = "首都机场专职消防队",
                    OrgType = 2, // 专职队
                    Address = "北京市顺义区首都机场",
                    Longitude = 116.5974,
                    Latitude = 40.0742,
                    Phone = "010-55667788",
                    Description = "首都机场专职消防力量"
                },
                
                // 北京重点建筑
                new Building
                {
                    CityCn = "北京",
                    OrgName = "国家大剧院",
                    OrgType = 3, // 重点建筑
                    Address = "北京市西城区西长安街2号",
                    Longitude = 116.3874,
                    Latitude = 39.9042,
                    Phone = "010-66550000",
                    Description = "国家级重点文化建筑"
                },
                new Building
                {
                    CityCn = "北京",
                    OrgName = "北京大学",
                    OrgType = 3, // 重点建筑
                    Address = "北京市海淀区颐和园路5号",
                    Longitude = 116.3074,
                    Latitude = 39.9942,
                    Phone = "010-62751234",
                    Description = "重点高等教育机构"
                },
                
                // 上海测试数据
                new Building
                {
                    CityCn = "上海",
                    OrgName = "上海市消防救援总队浦东支队",
                    OrgType = 1, // 消防队站
                    Address = "上海市浦东新区世纪大道1000号",
                    Longitude = 121.5074,
                    Latitude = 31.2342,
                    Phone = "021-12345678",
                    Description = "浦东新区主要消防救援力量"
                },
                new Building
                {
                    CityCn = "上海",
                    OrgName = "上海中心大厦",
                    OrgType = 3, // 重点建筑
                    Address = "上海市浦东新区银城中路501号",
                    Longitude = 121.5174,
                    Latitude = 31.2442,
                    Phone = "021-87654321",
                    Description = "上海地标性超高层建筑"
                }
            };
            
            context.Buildings.AddRange(testBuildings);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"已创建 {testBuildings.Count} 条测试数据");
        }
        
        static async Task TestBuildingDataLoading(IBuildingRepository buildingRepository)
        {
            Console.WriteLine("\n=== 测试建筑数据加载 ===");
            
            // 测试按城市加载
            var cities = new[] { "北京", "上海" };
            
            foreach (var city in cities)
            {
                Console.WriteLine($"\n--- {city}市建筑数据 ---");
                var buildings = await buildingRepository.GetBuildingsByCityAsync(city);
                
                var dzCount = 0; // 消防队站
                var zzCount = 0; // 专职队
                var zdCount = 0; // 重点建筑
                
                foreach (var building in buildings)
                {
                    Console.WriteLine($"ID: {building.Id}, 名称: {building.OrgName}, 类型: {building.OrgType}, 坐标: ({building.Longitude}, {building.Latitude})");
                    
                    switch (building.OrgType)
                    {
                        case 1: dzCount++; break;
                        case 2: zzCount++; break;
                        case 3: zdCount++; break;
                    }
                }
                
                Console.WriteLine($"统计: 消防队站 {dzCount}个, 专职队 {zzCount}个, 重点建筑 {zdCount}个");
            }
            
            // 测试按类型加载
            Console.WriteLine("\n=== 测试按类型加载 ===");
            var beijingFireStations = await buildingRepository.GetBuildingsByCityAndTypeAsync("北京", 1);
            Console.WriteLine($"北京消防队站数量: {beijingFireStations.Count()}");
            
            var beijingKeyBuildings = await buildingRepository.GetBuildingsByCityAndTypeAsync("北京", 3);
            Console.WriteLine($"北京重点建筑数量: {beijingKeyBuildings.Count()}");
        }
    }
}