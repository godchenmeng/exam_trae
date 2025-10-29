using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using ExamSystem.Domain.Models;

namespace ExamSystem.Tests
{
    /// <summary>
    /// OverlayDTO 序列化/反序列化一致性单元测试
    /// 确保 C# 模型与 JavaScript JSON 结构完全兼容
    /// </summary>
    public class OverlayDTOSerializationTests
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public OverlayDTOSerializationTests()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        [Fact]
        public void OverlayDTO_SerializeToJson_ShouldMatchExpectedStructure()
        {
            // Arrange - 创建测试对象
            var overlay = new OverlayDTO
            {
                Id = "test-overlay-001",
                Type = OverlayType.Polygon,
                Geometry = new OverlayGeometry
                {
                    Path = new List<Coordinate>
                    {
                        new Coordinate { Lat = 39.9042, Lng = 116.4074 },
                        new Coordinate { Lat = 39.9142, Lng = 116.4174 },
                        new Coordinate { Lat = 39.9242, Lng = 116.4274 }
                    }
                },
                Style = new OverlayStyle
                {
                    StrokeColor = "#FF0000",
                    StrokeWeight = 2,
                    StrokeOpacity = 0.8,
                    FillColor = "#00FF00",
                    FillOpacity = 0.3
                },
                Meta = new OverlayMeta
                {
                    Label = "测试区域",
                    Category = "测试"
                }
            };

            // Act - 序列化为 JSON
            var json = JsonSerializer.Serialize(overlay, new JsonSerializerOptions { WriteIndented = true });
            
            // Assert - 验证 JSON 结构 (基本结构验证)
            Assert.Contains("\"id\": \"test-overlay-001\"", json);
            Assert.Contains("\"type\": \"Polygon\"", json);
            Assert.Contains("\"geometry\":", json);
            Assert.Contains("\"style\":", json);
            Assert.Contains("\"meta\":", json);
            
            // 验证具体内容
            Assert.Contains("\"strokeColor\": \"#FF0000\"", json);
            Assert.Contains("\"strokeWeight\": 2", json);
            Assert.Contains("\"fillColor\": \"#00FF00\"", json);
        }

        [Fact]
        public void OverlayDTO_DeserializeFromJson_ShouldRestoreOriginalObject()
        {
            // Arrange - 模拟来自 JavaScript 的 JSON 数据
            var json = @"{
                ""id"": ""js-overlay-002"",
                ""type"": ""Marker"",
                ""geometry"": {
                    ""lng"": 121.4737,
                    ""lat"": 31.2304
                },
                ""style"": {
                    ""strokeColor"": ""#0000FF"",
                    ""strokeWeight"": 3,
                    ""strokeOpacity"": 1.0,
                    ""fillColor"": ""#FFFF00"",
                    ""fillOpacity"": 0.5
                },
                ""meta"": {
                    ""label"": ""上海标记点"",
                    ""note"": ""JavaScript 创建的标记点"",
                    ""category"": ""地标""
                }
            }";

            // Act - 反序列化为 C# 对象
            var overlay = JsonSerializer.Deserialize<OverlayDTO>(json, _jsonOptions);

            // Assert - 验证对象属性
            Assert.NotNull(overlay);
            Assert.Equal("js-overlay-002", overlay.Id);
            Assert.Equal(OverlayType.Marker, overlay.Type);
            
            Assert.NotNull(overlay.Geometry);
            Assert.Equal(31.2304, overlay.Geometry.Lat);
            Assert.Equal(121.4737, overlay.Geometry.Lng);
            
            Assert.NotNull(overlay.Style);
            Assert.Equal("#0000FF", overlay.Style.StrokeColor);
            Assert.Equal(3, overlay.Style.StrokeWeight);
            Assert.Equal(1.0, overlay.Style.StrokeOpacity);
            Assert.Equal("#FFFF00", overlay.Style.FillColor);
            Assert.Equal(0.5, overlay.Style.FillOpacity);
            
            Assert.NotNull(overlay.Meta);
            Assert.Equal("上海标记点", overlay.Meta.Label);
            Assert.Equal("JavaScript 创建的标记点", overlay.Meta.Note);
            Assert.Equal("地标", overlay.Meta.Category);
        }

        [Fact]
        public void OverlayDTO_RoundTripSerialization_ShouldPreserveAllData()
        {
            // Arrange - 创建原始对象
            var original = new OverlayDTO
            {
                Id = "roundtrip-test",
                Type = OverlayType.Polyline,
                Geometry = new OverlayGeometry
                {
                    Path = new List<Coordinate>
                    {
                        new Coordinate { Lat = 40.7128, Lng = -74.0060 },
                        new Coordinate { Lat = 34.0522, Lng = -118.2437 }
                    }
                },
                Style = new OverlayStyle
                {
                    StrokeColor = "#800080",
                    StrokeWeight = 5,
                    StrokeOpacity = 0.9,
                    FillColor = null, // 线条不需要填充色
                    FillOpacity = null
                },
                Meta = new OverlayMeta
                {
                    Label = "纽约到洛杉矶",
                    Note = null,
                    Category = "路线"
                }
            };

            // Act - 序列化后再反序列化
            var json = JsonSerializer.Serialize(original, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<OverlayDTO>(json, _jsonOptions);

            // Assert - 验证往返序列化后数据完整性
            Assert.NotNull(deserialized);
            Assert.Equal(original.Id, deserialized.Id);
            Assert.Equal(original.Type, deserialized.Type);
            
            Assert.Equal(original.Geometry.Path.Count, deserialized.Geometry.Path.Count);
            for (int i = 0; i < original.Geometry.Path.Count; i++)
            {
                Assert.Equal(original.Geometry.Path[i].Lat, deserialized.Geometry.Path[i].Lat);
                Assert.Equal(original.Geometry.Path[i].Lng, deserialized.Geometry.Path[i].Lng);
            }
            
            Assert.Equal(original.Style.StrokeColor, deserialized.Style.StrokeColor);
            Assert.Equal(original.Style.StrokeWeight, deserialized.Style.StrokeWeight);
            Assert.Equal(original.Style.StrokeOpacity, deserialized.Style.StrokeOpacity);
            Assert.Equal(original.Style.FillColor, deserialized.Style.FillColor);
            Assert.Equal(original.Style.FillOpacity, deserialized.Style.FillOpacity);
            
            Assert.Equal(original.Meta.Label, deserialized.Meta.Label);
            Assert.Equal(original.Meta.Note, deserialized.Meta.Note);
            Assert.Equal(original.Meta.Category, deserialized.Meta.Category);
        }

        [Theory]
        [InlineData(OverlayType.Marker)]
        [InlineData(OverlayType.Polyline)]
        [InlineData(OverlayType.Polygon)]
        [InlineData(OverlayType.Circle)]
        public void OverlayType_SerializeDeserialize_ShouldHandleAllEnumValues(OverlayType overlayType)
        {
            // Arrange
            var overlay = new OverlayDTO
            {
                Id = $"test-{overlayType.ToString().ToLower()}",
                Type = overlayType,
                Geometry = new OverlayGeometry
                {
                    Path = new List<Coordinate> { new Coordinate { Lat = 0, Lng = 0 } }
                },
                Style = new OverlayStyle(),
                Meta = new OverlayMeta { Category = "test" }
            };

            // Act
            var json = JsonSerializer.Serialize(overlay, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<OverlayDTO>(json, _jsonOptions);

            // Assert
            Assert.Equal(overlayType, deserialized.Type);
            Assert.Contains($"\"type\": \"{overlayType}\"", json);
        }

        [Fact]
        public void OverlayDTO_HandleNullValues_ShouldSerializeCorrectly()
        {
            // Arrange - 测试可空字段的处理
            var overlay = new OverlayDTO
            {
                Id = "null-test",
                Type = OverlayType.Marker,
                Geometry = new OverlayGeometry
                {
                    Path = new List<Coordinate> { new Coordinate { Lat = 0, Lng = 0 } }
                },
                Style = new OverlayStyle
                {
                    StrokeColor = "#000000",
                    StrokeWeight = 1,
                    StrokeOpacity = 1.0,
                    FillColor = null, // 测试 null 值
                    FillOpacity = null // 测试 null 值
                },
                Meta = new OverlayMeta
                {
                    Label = "空值测试",
                    Note = null, // 测试 null 值
                    Category = "test"
                }
            };

            // Act
            var json = JsonSerializer.Serialize(overlay, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<OverlayDTO>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Null(deserialized.Style.FillColor);
            Assert.Null(deserialized.Style.FillOpacity);
            Assert.Null(deserialized.Meta.Note);
            
            // 验证 JSON 中包含 null 值
            Assert.Contains("\"fillColor\": null", json);
            Assert.Contains("\"fillOpacity\": null", json);
            Assert.Contains("\"note\": null", json);
        }
    }
}