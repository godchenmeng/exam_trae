using System;
using System.Collections.Generic;
using System.Text.Json;
using ExamSystem.Domain.DTOs;
using ExamSystem.Domain.Entities;

namespace ConversionDebugTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ConvertOverlaysToMapDrawingData 转换调试测试 ===");
            Console.WriteLine();

            // 测试实际的rawMapData格式
            TestActualRawMapDataFormat();

            Console.WriteLine();
            Console.WriteLine("=== 测试完成 ===");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        static void TestActualRawMapDataFormat()
        {
            Console.WriteLine("测试: 实际rawMapData格式转换");
            
            // 模拟实际的rawMapData格式（嵌套JSON）
            string rawMapData = "{\"data\":\"{\\\"center\\\":{\\\"lng\\\":106.62999999999998,\\\"lat\\\":26.64999997900177},\\\"zoom\\\":12,\\\"overlays\\\":[{\\\"id\\\":\\\"ov-1\\\",\\\"type\\\":\\\"marker\\\",\\\"name\\\":\\\"剧毒品 1\\\",\\\"point\\\":{\\\"lng\\\":106.61821423171293,\\\"lat\\\":26.61382968456443}},{\\\"id\\\":\\\"ov-2\\\",\\\"type\\\":\\\"marker\\\",\\\"name\\\":\\\"有毒品 远离食品 2\\\",\\\"point\\\":{\\\"lng\\\":106.75216954931659,\\\"lat\\\":26.64999995800354}}]}\"}";

            try
            {
                // 解析外层JSON
                var outerJsonDoc = JsonDocument.Parse(rawMapData);
                string innerDataString = null;
                
                if (outerJsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    innerDataString = dataElement.GetString();
                }
                
                if (string.IsNullOrEmpty(innerDataString))
                {
                    Console.WriteLine("  ❌ 未找到data字段或data字段为空");
                    return;
                }

                Console.WriteLine("  内层JSON长度: " + innerDataString.Length);

                // 解析内层JSON数据
                var innerJsonDoc = JsonDocument.Parse(innerDataString);
                var hasOverlays = innerJsonDoc.RootElement.TryGetProperty("overlays", out var overlaysElement);

                if (!hasOverlays)
                {
                    Console.WriteLine("  ❌ 未找到overlays字段");
                    return;
                }

                Console.WriteLine("  Overlays类型: " + overlaysElement.ValueKind);
                Console.WriteLine("  Overlays数组长度: " + overlaysElement.GetArrayLength());

                // 调用转换方法
                var convertedOverlays = ConvertOverlaysToMapDrawingData(overlaysElement);
                
                Console.WriteLine("  转换结果: " + convertedOverlays.Count + " 个图形");
                for (int i = 0; i < convertedOverlays.Count; i++)
                {
                    var overlay = convertedOverlays[i];
                    Console.WriteLine("    图形 " + (i + 1) + ":");
                    Console.WriteLine("      类型: " + overlay.ShapeType);
                    Console.WriteLine("      标签: " + overlay.Label);
                    Console.WriteLine("      坐标数量: " + overlay.Coordinates.Count);
                    if (overlay.Coordinates.Count > 0)
                    {
                        var coord = overlay.Coordinates[0];
                        Console.WriteLine("      第一个坐标: 经度=" + coord.Longitude + ", 纬度=" + coord.Latitude);
                    }
                }
                
                Console.WriteLine("  ✓ 实际格式转换成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  ❌ 转换失败: " + ex.Message);
                Console.WriteLine("  堆栈跟踪: " + ex.StackTrace);
            }
        }

        // 复制FullScreenExamViewModel中的转换方法
        static List<MapDrawingDto> ConvertOverlaysToMapDrawingData(JsonElement overlaysElement)
        {
            var result = new List<MapDrawingDto>();

            try
            {
                Console.WriteLine("    ConvertOverlaysToMapDrawingData 开始转换");
                Console.WriteLine("    overlaysElement类型: " + overlaysElement.ValueKind);
                
                if (overlaysElement.ValueKind != JsonValueKind.Array)
                {
                    Console.WriteLine("    警告: overlays不是数组格式");
                    return result;
                }

                int answerId = 0; // 测试用
                int orderIndex = 0;
                foreach (var overlay in overlaysElement.EnumerateArray())
                {
                    Console.WriteLine("    处理第 " + orderIndex + " 个overlay");
                    
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
                        Console.WriteLine("    图形类型: " + typeElement.GetString() + " -> " + mapDrawingDto.ShapeType);
                    }

                    // 提取标签信息 - 新格式使用name字段
                    if (overlay.TryGetProperty("name", out var nameElement))
                    {
                        mapDrawingDto.Label = nameElement.GetString();
                        Console.WriteLine("    标签(name): " + mapDrawingDto.Label);
                    }
                    else if (overlay.TryGetProperty("meta", out var metaElement) && 
                             metaElement.TryGetProperty("label", out var labelElement))
                    {
                        mapDrawingDto.Label = labelElement.GetString();
                        Console.WriteLine("    标签(meta.label): " + mapDrawingDto.Label);
                    }

                    // 提取坐标信息 - 支持新格式和旧格式
                    mapDrawingDto.Coordinates = ExtractCoordinatesFromNewFormat(overlay, mapDrawingDto.ShapeType);
                    Console.WriteLine("    坐标数量: " + mapDrawingDto.Coordinates.Count);
                    if (mapDrawingDto.Coordinates.Count > 0)
                    {
                        var firstCoord = mapDrawingDto.Coordinates[0];
                        Console.WriteLine("    第一个坐标: 经度=" + firstCoord.Longitude + ", 纬度=" + firstCoord.Latitude);
                    }

                    // 提取样式信息 - 新格式可能没有style字段，使用默认样式
                    if (overlay.TryGetProperty("style", out var styleElement))
                    {
                        mapDrawingDto.Style = ExtractStyle(styleElement);
                    }
                    else
                    {
                        // 为新格式设置默认样式
                        mapDrawingDto.Style = new MapDrawingStyle
                        {
                            StrokeColor = "#ff0000",
                            FillColor = "#ff0000",
                            StrokeWidth = 2,
                            Opacity = 1.0,
                            IsFilled = false
                        };
                    }

                    result.Add(mapDrawingDto);
                }

                Console.WriteLine("    转换overlays数据完成，共 " + result.Count + " 个图形");
            }
            catch (Exception ex)
            {
                Console.WriteLine("    转换overlays数据格式失败: " + ex.Message);
            }

            return result;
        }

        static string ConvertShapeType(string frontendType)
        {
            if (string.IsNullOrEmpty(frontendType))
                return "Point";
                
            return frontendType.ToLower() switch
            {
                "marker" => "Marker",
                "polyline" => "Line", 
                "polygon" => "Polygon",
                "circle" => "Circle",
                "rectangle" => "Rectangle",
                _ => "Point"
            };
        }

        static List<MapCoordinate> ExtractCoordinatesFromNewFormat(JsonElement overlay, string shapeType)
        {
            var coordinates = new List<MapCoordinate>();

            try
            {
                switch (shapeType.ToLower())
                {
                    case "marker":
                    case "point":
                        // 新格式：{ point: { lng: 116.4, lat: 39.9 } }
                        if (overlay.TryGetProperty("point", out var pointElement))
                        {
                            if (pointElement.TryGetProperty("lng", out var lng) && 
                                pointElement.TryGetProperty("lat", out var lat))
                            {
                                coordinates.Add(new MapCoordinate 
                                { 
                                    Longitude = lng.GetDouble(), 
                                    Latitude = lat.GetDouble() 
                                });
                            }
                        }
                        // 兼容旧格式
                        else if (overlay.TryGetProperty("geometry", out var geometryElement))
                        {
                            coordinates = ExtractCoordinates(geometryElement, shapeType);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("    从新格式提取坐标信息失败，图形类型: " + shapeType + ", 错误: " + ex.Message);
            }

            return coordinates;
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
                        // 点：{ lng: 116.4, lat: 39.9 }
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("    提取坐标信息失败，图形类型: " + shapeType + ", 错误: " + ex.Message);
            }

            return coordinates;
        }

        static MapDrawingStyle ExtractStyle(JsonElement styleElement)
        {
            var style = new MapDrawingStyle();

            if (styleElement.TryGetProperty("strokeColor", out var strokeColor))
                style.StrokeColor = strokeColor.GetString();
            if (styleElement.TryGetProperty("fillColor", out var fillColor))
                style.FillColor = fillColor.GetString();
            if (styleElement.TryGetProperty("strokeWidth", out var strokeWidth))
                style.StrokeWidth = strokeWidth.GetInt32();
            if (styleElement.TryGetProperty("opacity", out var opacity))
                style.Opacity = opacity.GetDouble();
            if (styleElement.TryGetProperty("isFilled", out var isFilled))
                style.IsFilled = isFilled.GetBoolean();

            return style;
        }
    }
}