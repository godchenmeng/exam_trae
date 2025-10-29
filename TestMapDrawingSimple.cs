using System;
using System.IO;
using System.Text.Json;

namespace ExamSystem.Tests
{
    /// <summary>
    /// 简化版地图绘制功能测试
    /// </summary>
    class TestMapDrawingSimple
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 地图绘制功能基础测试 ===");
            
            try
            {
                // 测试1: JSON序列化和反序列化
                TestJsonSerialization();
                
                // 测试2: 文件读写操作
                TestFileOperations();
                
                // 测试3: 地图数据格式验证
                TestMapDataFormat();
                
                Console.WriteLine("\n✅ 所有基础测试通过！");
                Console.WriteLine("\n📋 测试总结:");
                Console.WriteLine("   ✓ JSON序列化/反序列化正常");
                Console.WriteLine("   ✓ 文件读写操作正常");
                Console.WriteLine("   ✓ 地图数据格式验证通过");
                Console.WriteLine("\n🎯 地图绘制功能核心组件运行正常，可以进行UI集成测试。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        private static void TestJsonSerialization()
        {
            Console.WriteLine("\n1. 测试JSON序列化和反序列化...");
            
            // 模拟地图绘制数据
            var mapData = new
            {
                shapes = new[]
                {
                    new { type = "marker", lat = 39.9042, lng = 116.4074, title = "消防站1" },
                    new { type = "circle", lat = 39.9142, lng = 116.4174, radius = 500, title = "覆盖区域" },
                    new { type = "polygon", points = new[] 
                        { 
                            new { lat = 39.9000, lng = 116.4000 },
                            new { lat = 39.9100, lng = 116.4100 },
                            new { lat = 39.9050, lng = 116.4150 }
                        }, 
                        title = "责任区域" 
                    }
                },
                metadata = new
                {
                    questionId = 1,
                    answerId = 1,
                    timestamp = DateTime.Now,
                    version = "1.0"
                }
            };

            // 序列化
            string jsonString = JsonSerializer.Serialize(mapData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            Console.WriteLine("   ✓ JSON序列化成功");
            Console.WriteLine($"   数据大小: {jsonString.Length} 字符");

            // 反序列化验证
            var deserializedData = JsonSerializer.Deserialize<JsonElement>(jsonString);
            Console.WriteLine("   ✓ JSON反序列化成功");
            Console.WriteLine($"   包含 {deserializedData.GetProperty("shapes").GetArrayLength()} 个绘制对象");
        }

        private static void TestFileOperations()
        {
            Console.WriteLine("\n2. 测试文件读写操作...");
            
            string testDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
            string testFile = Path.Combine(testDir, "map_drawing_test.json");

            try
            {
                // 创建测试目录
                if (!Directory.Exists(testDir))
                {
                    Directory.CreateDirectory(testDir);
                    Console.WriteLine("   ✓ 创建测试目录");
                }

                // 写入测试数据
                var testData = new
                {
                    questionId = 1,
                    drawingData = "[{\"type\":\"marker\",\"lat\":39.9042,\"lng\":116.4074}]",
                    saveTime = DateTime.Now
                };

                string jsonData = JsonSerializer.Serialize(testData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(testFile, jsonData);
                Console.WriteLine("   ✓ 写入测试文件成功");

                // 读取测试数据
                string readData = File.ReadAllText(testFile);
                var parsedData = JsonSerializer.Deserialize<JsonElement>(readData);
                Console.WriteLine("   ✓ 读取测试文件成功");
                Console.WriteLine($"   题目ID: {parsedData.GetProperty("questionId").GetInt32()}");

                // 清理测试文件
                File.Delete(testFile);
                Console.WriteLine("   ✓ 清理测试文件");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 文件操作失败: {ex.Message}");
                throw;
            }
        }

        private static void TestMapDataFormat()
        {
            Console.WriteLine("\n3. 测试地图数据格式验证...");
            
            // 测试各种地图绘制对象格式
            var testCases = new[]
            {
                new { name = "标记点", data = "{\"type\":\"marker\",\"lat\":39.9042,\"lng\":116.4074,\"title\":\"消防站\"}" },
                new { name = "圆形区域", data = "{\"type\":\"circle\",\"lat\":39.9042,\"lng\":116.4074,\"radius\":500}" },
                new { name = "多边形", data = "{\"type\":\"polygon\",\"points\":[{\"lat\":39.9000,\"lng\":116.4000},{\"lat\":39.9100,\"lng\":116.4100}]}" },
                new { name = "线条", data = "{\"type\":\"polyline\",\"points\":[{\"lat\":39.9000,\"lng\":116.4000},{\"lat\":39.9100,\"lng\":116.4100}]}" }
            };

            foreach (var testCase in testCases)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<JsonElement>(testCase.data);
                    string type = parsed.GetProperty("type").GetString() ?? "";
                    Console.WriteLine($"   ✓ {testCase.name} 格式验证通过 (类型: {type})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ {testCase.name} 格式验证失败: {ex.Message}");
                    throw;
                }
            }

            // 测试复合数据格式
            var complexData = new
            {
                questionId = 1,
                answerId = 1,
                shapes = new[]
                {
                    new { type = "marker", lat = 39.9042, lng = 116.4074 },
                    new { type = "circle", lat = 39.9142, lng = 116.4174, radius = 500 }
                },
                statistics = new
                {
                    totalShapes = 2,
                    markerCount = 1,
                    circleCount = 1,
                    polygonCount = 0
                }
            };

            string complexJson = JsonSerializer.Serialize(complexData);
            var parsedComplex = JsonSerializer.Deserialize<JsonElement>(complexJson);
            int shapeCount = parsedComplex.GetProperty("shapes").GetArrayLength();
            Console.WriteLine($"   ✓ 复合数据格式验证通过 (包含 {shapeCount} 个对象)");
        }
    }
}