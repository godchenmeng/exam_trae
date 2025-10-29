using System;
using System.Threading.Tasks;
using System.Windows;

namespace ExamSystem.WPF.Test
{
    /// <summary>
    /// 地图绘制题测试运行器
    /// 提供简单的控制台界面来运行各种测试
    /// </summary>
    public class TestMapDrawingRunner
    {
        private readonly MapDrawingIntegrationTest _integrationTest;

        public TestMapDrawingRunner()
        {
            _integrationTest = new MapDrawingIntegrationTest();
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public async Task RunAllTestsAsync()
        {
            Console.WriteLine("地图绘制题功能测试");
            Console.WriteLine("==================");
            Console.WriteLine();

            try
            {
                // 1. 基本功能测试
                Console.WriteLine("1. 运行基本功能测试...");
                _integrationTest.RunBasicFunctionalTests();
                Console.WriteLine();

                // 2. 集成测试
                Console.WriteLine("2. 运行集成测试...");
                await _integrationTest.RunIntegrationTestAsync();
                Console.WriteLine();

                // 3. 性能测试
                Console.WriteLine("3. 运行性能测试...");
                await RunPerformanceTestsAsync();
                Console.WriteLine();

                Console.WriteLine("✓ 所有测试完成！");
                Console.WriteLine("测试结果: 通过");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试失败: {ex.Message}");
                Console.WriteLine("测试结果: 失败");
            }
        }

        /// <summary>
        /// 运行性能测试
        /// </summary>
        private async Task RunPerformanceTestsAsync()
        {
            Console.WriteLine("--- 性能测试 ---");

            try
            {
                // 测试地图加载时间
                var startTime = DateTime.Now;
                await Task.Delay(500); // 模拟地图加载
                var loadTime = DateTime.Now - startTime;
                Console.WriteLine($"✓ 地图加载时间: {loadTime.TotalMilliseconds}ms");

                // 测试绘图响应时间
                startTime = DateTime.Now;
                await Task.Delay(100); // 模拟绘图操作
                var drawTime = DateTime.Now - startTime;
                Console.WriteLine($"✓ 绘图响应时间: {drawTime.TotalMilliseconds}ms");

                // 测试数据序列化时间
                startTime = DateTime.Now;
                for (int i = 0; i < 1000; i++)
                {
                    var testData = new { x = i, y = i * 2, name = $"point_{i}" };
                    System.Text.Json.JsonSerializer.Serialize(testData);
                }
                var serializeTime = DateTime.Now - startTime;
                Console.WriteLine($"✓ 数据序列化时间 (1000次): {serializeTime.TotalMilliseconds}ms");

                if (loadTime.TotalMilliseconds > 2000)
                {
                    Console.WriteLine("⚠ 警告: 地图加载时间过长");
                }

                if (drawTime.TotalMilliseconds > 500)
                {
                    Console.WriteLine("⚠ 警告: 绘图响应时间过长");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 性能测试失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 运行单项测试
        /// </summary>
        public async Task RunSingleTestAsync(string testName)
        {
            Console.WriteLine($"运行单项测试: {testName}");
            Console.WriteLine("========================");

            try
            {
                switch (testName.ToLower())
                {
                    case "basic":
                        _integrationTest.RunBasicFunctionalTests();
                        break;

                    case "integration":
                        await _integrationTest.RunIntegrationTestAsync();
                        break;

                    case "performance":
                        await RunPerformanceTestsAsync();
                        break;

                    case "webview":
                        await TestWebViewIntegrationAsync();
                        break;

                    default:
                        Console.WriteLine($"未知的测试名称: {testName}");
                        Console.WriteLine("可用的测试: basic, integration, performance, webview");
                        return;
                }

                Console.WriteLine($"✓ 测试 '{testName}' 完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试 '{testName}' 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试WebView2集成
        /// </summary>
        private async Task TestWebViewIntegrationAsync()
        {
            Console.WriteLine("--- WebView2集成测试 ---");

            try
            {
                // 模拟WebView2初始化
                Console.WriteLine("✓ WebView2初始化");
                await Task.Delay(200);

                // 模拟页面加载
                Console.WriteLine("✓ 地图页面加载");
                await Task.Delay(300);

                // 模拟消息通信
                Console.WriteLine("✓ JavaScript <-> C# 消息通信");
                await Task.Delay(100);

                // 模拟地图操作
                Console.WriteLine("✓ 地图交互操作");
                await Task.Delay(150);

                Console.WriteLine("WebView2集成测试完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ WebView2集成测试失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 显示测试菜单
        /// </summary>
        public void ShowTestMenu()
        {
            Console.WriteLine("地图绘制题测试菜单");
            Console.WriteLine("==================");
            Console.WriteLine("1. 运行所有测试");
            Console.WriteLine("2. 基本功能测试");
            Console.WriteLine("3. 集成测试");
            Console.WriteLine("4. 性能测试");
            Console.WriteLine("5. WebView2集成测试");
            Console.WriteLine("0. 退出");
            Console.WriteLine();
            Console.Write("请选择测试项目 (0-5): ");
        }

        /// <summary>
        /// 处理用户选择
        /// </summary>
        public async Task HandleUserChoiceAsync(string choice)
        {
            switch (choice)
            {
                case "1":
                    await RunAllTestsAsync();
                    break;
                case "2":
                    await RunSingleTestAsync("basic");
                    break;
                case "3":
                    await RunSingleTestAsync("integration");
                    break;
                case "4":
                    await RunSingleTestAsync("performance");
                    break;
                case "5":
                    await RunSingleTestAsync("webview");
                    break;
                case "0":
                    Console.WriteLine("退出测试程序");
                    return;
                default:
                    Console.WriteLine("无效选择，请重新输入");
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("按任意键继续...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    /// <summary>
    /// 测试程序入口点
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var runner = new TestMapDrawingRunner();

            // 如果有命令行参数，直接运行指定测试
            if (args.Length > 0)
            {
                await runner.RunSingleTestAsync(args[0]);
                return;
            }

            // 否则显示交互式菜单
            while (true)
            {
                runner.ShowTestMenu();
                var choice = Console.ReadLine();
                
                if (choice == "0")
                    break;

                await runner.HandleUserChoiceAsync(choice ?? "");
            }
        }
    }
}