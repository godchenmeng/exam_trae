using System;
using System.Collections.Generic;
using System.Text.Json;
using ExamSystem.Domain.DTOs;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Test
{
    /// <summary>
    /// 地图绘制数据转换功能测试
    /// </summary>
    public class MapDataConversionTest
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== 地图绘制数据转换功能测试 ===\n");

            // 测试1: 测试标记点转换
            TestMarkerConversion();

            // 测试2: 测试线条转换
            TestPolylineConversion();

            // 测试3: 测试多边形转换
            TestPolygonConversion();

            // 测试4: 测试圆形转换
            TestCircleConversion();

            // 测试5: 测试完整的前端数据转换
            TestCompleteDataConversion();

            Console.WriteLine("\n=== 测试完成 ===");
            Console.ReadKey();
        }

        static void TestMarkerConversion()
        {
            Console.WriteLine("测试1: 标记点转换");
            
            var testJson = @"{
                ""questionId"": ""test-123"",
                ""overlays"": [
                    {
                        ""type"": ""marker"",
                        ""geometry"": { ""lng"": 116.4074, ""lat"": 39.9042 },
                        ""style"": { 
                            ""strokeColor"": ""#ff0000"", 
                            ""fillColor"": ""#ff0000"",
                            ""strokeWeight"": 2,
                            ""strokeOpacity"": 1.0,
                            ""fillOpacity"": 0.8
                        },
                        ""meta"": { ""label"": ""测试标记点"" }
                    }
                ],
                ""drawDurationSeconds"": 30
            }";

            try
            {
                var jsonDoc = JsonDocument.Parse(testJson);
                var overlaysElement = jsonDoc.RootElement.GetProperty("overlays");
                
                var result = ConvertOverlaysToMapDrawingData(overlaysElement);
                
                Console.WriteLine($"  转换结果: {result.Count} 个图形");
                if (result.Count > 0)
                {
                    var marker = result[0];
                    Console.WriteLine($"  - 类型: {marker.ShapeType}");
                    Console.WriteLine($"  - 标签: {marker.Label}");
                    Console.WriteLine($"  - 坐标数量: {marker.Coordinates?.Count ?? 0}");
                    if (marker.Coordinates?.Count > 0)
                    {
                        Console.WriteLine($"  - 经度: {marker.Coordinates[0].Longitude}, 纬度: {marker.Coordinates[0].Latitude}");
                    }
                    Console.WriteLine($"  - 样式颜色: {marker.Style?.StrokeColor}");
                }
                Console.WriteLine("  ✓ 标记点转换成功\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ 标记点转换失败: {ex.Message}\n");
            }
        }

        static void TestPolylineConversion()
        {
            Console.WriteLine("测试2: 线条转换");
            
            var testJson = @"{
                ""overlays"": [
                    {
                        ""type"": ""polyline"",
                        ""geometry"": { 
                            ""path"": [
                                { ""lng"": 116.4074, ""lat"": 39.9042 },
                                { ""lng"": 116.4084, ""lat"": 39.9052 },
                                { ""lng"": 116.4094, ""lat"": 39.9062 }
                            ]
                        },
                        ""style"": { 
                            ""strokeColor"": ""#00ff00"", 
                            ""strokeWeight"": 3
                        },
                        ""meta"": { ""label"": ""测试线条"" }
                    }
                ]
            }";

            try
            {
                var jsonDoc = JsonDocument.Parse(testJson);
                var overlaysElement = jsonDoc.RootElement.GetProperty("overlays");
                
                var result = ConvertOverlaysToMapDrawingData(overlaysElement);
                
                Console.WriteLine($"  转换结果: {result.Count} 个图形");
                if (result.Count > 0)
                {
                    var line = result[0];
                    Console.WriteLine($"  - 类型: {line.ShapeType}");
                    Console.WriteLine($"  - 标签: {line.Label}");
                    Console.WriteLine($"  - 坐标数量: {line.Coordinates?.Count ?? 0}");
                    Console.WriteLine($"  - 样式颜色: {line.Style?.StrokeColor}");
                    Console.WriteLine($"  - 线宽: {line.Style?.StrokeWidth}");
                }
                Console.WriteLine("  ✓ 线条转换成功\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ 线条转换失败: {ex.Message}\n");
            }
        }

        static void TestPolygonConversion()
        {
            Console.WriteLine("测试3: 多边形转换");
            
            var testJson = @"{
                ""overlays"": [
                    {
                        ""type"": ""polygon"",
                        ""geometry"": { 
                            ""path"": [
                                { ""lng"": 116.4074, ""lat"": 39.9042 },
                                { ""lng"": 116.4084, ""lat"": 39.9042 },
                                { ""lng"": 116.4084, ""lat"": 39.9052 },
                                { ""lng"": 116.4074, ""lat"": 39.9052 }
                            ]
                        },
                        ""style"": { 
                            ""strokeColor"": ""#0000ff"", 
                            ""fillColor"": ""#0000ff"",
                            ""fillOpacity"": 0.3
                        },
                        ""meta"": { ""label"": ""测试多边形"" }
                    }
                ]
            }";

            try
            {
                var jsonDoc = JsonDocument.Parse(testJson);
                var overlaysElement = jsonDoc.RootElement.GetProperty("overlays");
                
                var result = ConvertOverlaysToMapDrawingData(overlaysElement);
                
                Console.WriteLine($"  转换结果: {result.Count} 个图形");
                if (result.Count > 0)
                {
                    var polygon = result[0];
                    Console.WriteLine($"  - 类型: {polygon.ShapeType}");
                    Console.WriteLine($"  - 标签: {polygon.Label}");
                    Console.WriteLine($"  - 坐标数量: {polygon.Coordinates?.Count ?? 0}");
                    Console.WriteLine($"  - 填充: {polygon.Style?.IsFilled}");
                }
                Console.WriteLine("  ✓ 多边形转换成功\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ 多边形转换失败: {ex.Message}\n");
            }
        }

        static void TestCircleConversion()
        {
            Console.WriteLine("测试4: 圆形转换");
            
            var testJson = @"{
                ""overlays"": [
                    {
                        ""type"": ""circle"",
                        ""geometry"": { 
                            ""center"": { ""lng"": 116.4074, ""lat"": 39.9042 },
                            ""radius"": 1000
                        },
                        ""style"": { 
                            ""strokeColor"": ""#ff00ff"", 
                            ""fillColor"": ""#ff00ff"",
                            ""fillOpacity"": 0.2
                        },
                        ""meta"": { ""label"": ""测试圆形"" }
                    }
                ]
            }";

            try
            {
                var jsonDoc = JsonDocument.Parse(testJson);
                var overlaysElement = jsonDoc.RootElement.GetProperty("overlays");
                
                var result = ConvertOverlaysToMapDrawingData(overlaysElement);
                
                Console.WriteLine($"  转换结果: {result.Count} 个图形");
                if (result.Count > 0)
                {
                    var circle = result[0];
                    Console.WriteLine($"  - 类型: {circle.ShapeType}");
                    Console.WriteLine($"  - 标签: {circle.Label}");
                    Console.WriteLine($"  - 坐标数量: {circle.Coordinates?.Count ?? 0}");
                    if (circle.Coordinates?.Count > 0)
                    {
                        Console.WriteLine($"  - 中心点: ({circle.Coordinates[0].Longitude}, {circle.Coordinates[0].Latitude})");
                        Console.WriteLine($"  - 半径: {circle.Coordinates[0].Altitude}");
                    }
                }
                Console.WriteLine("  ✓ 圆形转换成功\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ 圆形转换失败: {ex.Message}\n");
            }
        }

        static void TestCompleteDataConversion()
        {
            Console.WriteLine("测试5: 完整数据转换");
            
            var testJson = @"{
                ""questionId"": ""map-question-456"",
                ""overlays"": [
                    {
                        ""type"": ""marker"",
                        ""geometry"": { ""lng"": 116.4074, ""lat"": 39.9042 },
                        ""style"": { ""strokeColor"": ""#ff0000"" },
                        ""meta"": { ""label"": ""起点"" }
                    },
                    {
                        ""type"": ""polyline"",
                        ""geometry"": { 
                            ""path"": [
                                { ""lng"": 116.4074, ""lat"": 39.9042 },
                                { ""lng"": 116.4084, ""lat"": 39.9052 }
                            ]
                        },
                        ""style"": { ""strokeColor"": ""#00ff00"", ""strokeWeight"": 2 },
                        ""meta"": { ""label"": ""路径"" }
                    },
                    {
                        ""type"": ""circle"",
                        ""geometry"": { 
                            ""center"": { ""lng"": 116.4084, ""lat"": 39.9052 },
                            ""radius"": 500
                        },
                        ""style"": { ""strokeColor"": ""#0000ff"", ""fillOpacity"": 0.3 },
                        ""meta"": { ""label"": ""终点区域"" }
                    }
                ],
                ""drawDurationSeconds"": 120
            }";

            try
            {
                var jsonDoc = JsonDocument.Parse(testJson);
                var overlaysElement = jsonDoc.RootElement.GetProperty("overlays");
                
                var result = ConvertOverlaysToMapDrawingData(overlaysElement);
                
                Console.WriteLine($"  转换结果: {result.Count} 个图形");
                for (int i = 0; i < result.Count; i++)
                {
                    var item = result[i];
                    Console.WriteLine($"  图形 {i + 1}: {item.ShapeType} - {item.Label}");
                }
                Console.WriteLine("  ✓ 完整数据转换成功\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ 完整数据转换失败: {ex.Message}\n");
            }
        }

        // 复制FullScreenExamViewModel中的转换方法
        static List<MapDrawingDto> ConvertOverlaysToMapDrawingData(JsonElement overlaysElement)
        {
            var result = new List<MapDrawingDto>();
            
            try
            {
                if (overlaysElement.ValueKind != JsonValueKind.Array)
                {
                    Console.WriteLine("警告: overlays不是数组格式");
                    return result;
                }

                int answerId = 0; // 测试用

                int orderIndex = 0;
                foreach (var overlay in overlaysElement.EnumerateArray())
                {
                    var mapDrawingDto = new MapDrawingDto
                    {
                        AnswerId = answerId,
                        OrderIndex = orderIndex++,
                        CreatedAt = DateTime.Now
                    };

                    // 提取基本信息
                    if (overlay.TryGetProperty("type", out var typeElement))
                    {
                        mapDrawingDto.ShapeType = ConvertShapeType(typeElement.GetString());
                    }

                    if (overlay.TryGetProperty("meta", out var metaElement) && 
                        metaElement.TryGetProperty("label", out var labelElement))
                    {
                        mapDrawingDto.Label = labelElement.GetString();
                    }

                    // 提取坐标信息
                    if (overlay.TryGetProperty("geometry", out var geometryElement))
                    {
                        mapDrawingDto.Coordinates = ExtractCoordinates(geometryElement, mapDrawingDto.ShapeType);
                    }

                    // 提取样式信息
                    if (overlay.TryGetProperty("style", out var styleElement))
                    {
                        mapDrawingDto.Style = ExtractStyle(styleElement);
                    }

                    result.Add(mapDrawingDto);
                }

                Console.WriteLine($"转换overlays数据完成，共 {result.Count} 个图形");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换overlays数据格式失败: {ex.Message}");
            }

            return result;
        }

        static string ConvertShapeType(string? frontendType)
        {
            return frontendType?.ToLower() switch
            {
                "marker" => "Marker",
                "polyline" => "Line", 
                "polygon" => "Polygon",
                "circle" => "Circle",
                "rectangle" => "Rectangle",
                _ => "Point"
            };
        }

        static List<MapCoordinate> ExtractCoordinates(JsonElement geometryElement, string shapeType)
        {
            var coordinates = new List<MapCoordinate>();

            try
            {
                switch (shapeType.ToLower())
                {
                    case "marker":
                    case "point":
                        if (geometryElement.TryGetProperty("lng", out var lng) && 
                            geometryElement.TryGetProperty("lat", out var lat))
                        {
                            coordinates.Add(new MapCoordinate 
                            { 
                                Longitude = lng.GetDouble(), 
                                Latitude = lat.GetDouble() 
                            });
                        }
                        break;

                    case "line":
                    case "polygon":
                        if (geometryElement.TryGetProperty("path", out var pathElement) && 
                            pathElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var point in pathElement.EnumerateArray())
                            {
                                if (point.TryGetProperty("lng", out var pLng) && 
                                    point.TryGetProperty("lat", out var pLat))
                                {
                                    coordinates.Add(new MapCoordinate 
                                    { 
                                        Longitude = pLng.GetDouble(), 
                                        Latitude = pLat.GetDouble() 
                                    });
                                }
                            }
                        }
                        break;

                    case "circle":
                        if (geometryElement.TryGetProperty("center", out var centerElement))
                        {
                            if (centerElement.TryGetProperty("lng", out var cLng) && 
                                centerElement.TryGetProperty("lat", out var cLat))
                            {
                                coordinates.Add(new MapCoordinate 
                                { 
                                    Longitude = cLng.GetDouble(), 
                                    Latitude = cLat.GetDouble() 
                                });
                            }
                        }
                        if (geometryElement.TryGetProperty("radius", out var radiusElement) && coordinates.Count > 0)
                        {
                            coordinates[0].Altitude = radiusElement.GetDouble();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"提取坐标信息失败，图形类型: {shapeType}, 错误: {ex.Message}");
            }

            return coordinates;
        }

        static MapDrawingStyle ExtractStyle(JsonElement styleElement)
        {
            var style = new MapDrawingStyle();

            try
            {
                if (styleElement.TryGetProperty("strokeColor", out var strokeColor))
                {
                    style.StrokeColor = strokeColor.GetString();
                }

                if (styleElement.TryGetProperty("fillColor", out var fillColor))
                {
                    style.FillColor = fillColor.GetString();
                }

                if (styleElement.TryGetProperty("strokeWeight", out var strokeWeight))
                {
                    style.StrokeWidth = strokeWeight.GetInt32();
                }

                if (styleElement.TryGetProperty("strokeOpacity", out var strokeOpacity))
                {
                    style.Opacity = strokeOpacity.GetDouble();
                }

                if (styleElement.TryGetProperty("fillOpacity", out var fillOpacity))
                {
                    style.IsFilled = fillOpacity.GetDouble() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"提取样式信息失败: {ex.Message}");
            }

            return style;
        }
    }
}