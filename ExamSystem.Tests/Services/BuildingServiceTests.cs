using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ExamSystem.Domain.Entities;
using ExamSystem.Infrastructure.Repositories;
using ExamSystem.Services.Services;
using ExamSystem.Services.Models;

namespace ExamSystem.Tests.Services
{
    /// <summary>
    /// 建筑物服务单元测试
    /// </summary>
    public class BuildingServiceTests
    {
        private readonly Mock<IBuildingRepository> _mockRepository;
        private readonly Mock<ILogger<BuildingService>> _mockLogger;
        private readonly BuildingService _buildingService;

        public BuildingServiceTests()
        {
            _mockRepository = new Mock<IBuildingRepository>();
            _mockLogger = new Mock<ILogger<BuildingService>>();
            _buildingService = new BuildingService(_mockRepository.Object, _mockLogger.Object);
        }

        #region 创建建筑物测试

        [Fact]
        public async Task CreateBuildingAsync_ValidBuilding_ReturnsSuccess()
        {
            // Arrange
            var building = CreateValidBuilding();
            _mockRepository.Setup(r => r.GetBuildingsByCityAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Building>());
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Building>()))
                .Returns(Task.CompletedTask);
            _mockRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _buildingService.CreateBuildingAsync(building, "测试操作员");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Building>()), Times.Once);
            _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateBuildingAsync_InvalidBuilding_ReturnsFailure()
        {
            // Arrange
            var building = new Building
            {
                OrgName = "", // 无效：空名称
                CityCn = "北京",
                Address = "测试地址",
                OrgType = 1
            };

            // Act
            var result = await _buildingService.CreateBuildingAsync(building, "测试操作员");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("机构名称不能为空", result.ErrorMessage);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Building>()), Times.Never);
        }

        [Fact]
        public async Task CreateBuildingAsync_DuplicateName_ReturnsFailure()
        {
            // Arrange
            var building = CreateValidBuilding();
            var existingBuildings = new List<Building>
            {
                new Building { OrgName = building.OrgName, CityCn = building.CityCn, Deleted = false }
            };
            
            _mockRepository.Setup(r => r.GetBuildingsByCityAsync(It.IsAny<string>()))
                .ReturnsAsync(existingBuildings);

            // Act
            var result = await _buildingService.CreateBuildingAsync(building, "测试操作员");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("该城市已存在同名建筑物", result.ErrorMessage);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Building>()), Times.Never);
        }

        #endregion

        #region 更新建筑物测试

        [Fact]
        public async Task UpdateBuildingAsync_ValidBuilding_ReturnsSuccess()
        {
            // Arrange
            var existingBuilding = CreateValidBuilding();
            existingBuilding.Id = 1;
            
            var updatedBuilding = CreateValidBuilding();
            updatedBuilding.Id = 1;
            updatedBuilding.OrgName = "更新后的名称";

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingBuilding);
            _mockRepository.Setup(r => r.GetBuildingsByCityAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Building>());
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Building>()))
                .Returns(Task.CompletedTask);
            _mockRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _buildingService.UpdateBuildingAsync(updatedBuilding, "测试操作员");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("更新后的名称", result.Data!.OrgName);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Building>()), Times.Once);
            _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateBuildingAsync_BuildingNotFound_ReturnsFailure()
        {
            // Arrange
            var building = CreateValidBuilding();
            building.Id = 999;

            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Building?)null);

            // Act
            var result = await _buildingService.UpdateBuildingAsync(building, "测试操作员");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("建筑物不存在", result.ErrorMessage);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Building>()), Times.Never);
        }

        #endregion

        #region 删除建筑物测试

        [Fact]
        public async Task DeleteBuildingAsync_ExistingBuilding_ReturnsSuccess()
        {
            // Arrange
            var building = CreateValidBuilding();
            building.Id = 1;

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(building);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Building>()))
                .Returns(Task.CompletedTask);
            _mockRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _buildingService.DeleteBuildingAsync(1, "测试操作员");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(building.Deleted);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Building>()), Times.Once);
            _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteBuildingAsync_BuildingNotFound_ReturnsFailure()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Building?)null);

            // Act
            var result = await _buildingService.DeleteBuildingAsync(999, "测试操作员");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("建筑物不存在", result.ErrorMessage);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Building>()), Times.Never);
        }

        #endregion

        #region 数据验证测试

        [Theory]
        [InlineData("", "北京", "测试地址", 1, false, "机构名称不能为空")]
        [InlineData("测试机构", "", "测试地址", 1, false, "城市不能为空")]
        [InlineData("测试机构", "北京", "", 1, false, "地址不能为空")]
        [InlineData("测试机构", "北京", "测试地址", 0, false, "机构类型必须为1-消防队站、2-专职队或3-重点建筑")]
        [InlineData("测试机构", "北京", "测试地址", 4, false, "机构类型必须为1-消防队站、2-专职队或3-重点建筑")]
        [InlineData("测试机构", "北京", "测试地址", 1, true, "")]
        public async Task ValidateBuildingAsync_VariousInputs_ReturnsExpectedResult(
            string orgName, string cityCn, string address, byte orgType, 
            bool expectedValid, string expectedError)
        {
            // Arrange
            var building = new Building
            {
                OrgName = orgName,
                CityCn = cityCn,
                Address = address,
                OrgType = orgType,
                Longitude = 116.4074,
                Latitude = 39.9042
            };

            // Act
            var result = await _buildingService.ValidateBuildingAsync(building);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
            if (!expectedValid)
            {
                Assert.Contains(expectedError, result.Errors);
            }
        }

        [Fact]
        public async Task ValidateBuildingAsync_InvalidCoordinates_ReturnsFailure()
        {
            // Arrange
            var building = CreateValidBuilding();
            building.Longitude = 200; // 无效经度

            // Act
            var result = await _buildingService.ValidateBuildingAsync(building);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("经度必须在-180到180之间", result.Errors);
        }

        #endregion

        #region 批量导入测试

        [Fact]
        public async Task BatchImportBuildingsAsync_ValidBuildings_ReturnsSuccess()
        {
            // Arrange
            var buildings = new List<Building>
            {
                CreateValidBuilding("机构1", "北京"),
                CreateValidBuilding("机构2", "上海")
            };

            _mockRepository.Setup(r => r.GetBuildingsByCityAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Building>());
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Building>()))
                .Returns(Task.CompletedTask);
            _mockRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _buildingService.BatchImportBuildingsAsync(buildings);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.TotalCount);
            Assert.Equal(2, result.Data.SuccessCount);
            Assert.Equal(0, result.Data.FailedCount);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Building>()), Times.Exactly(2));
        }

        [Fact]
        public async Task BatchImportBuildingsAsync_MixedValidInvalid_ReturnsPartialSuccess()
        {
            // Arrange
            var buildings = new List<Building>
            {
                CreateValidBuilding("机构1", "北京"),
                new Building { OrgName = "", CityCn = "上海", Address = "地址", OrgType = 1 } // 无效
            };

            _mockRepository.Setup(r => r.GetBuildingsByCityAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Building>());
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Building>()))
                .Returns(Task.CompletedTask);
            _mockRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _buildingService.BatchImportBuildingsAsync(buildings);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.TotalCount);
            Assert.Equal(1, result.Data.SuccessCount);
            Assert.Equal(1, result.Data.FailedCount);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Building>()), Times.Once);
        }

        #endregion

        #region 辅助方法

        private Building CreateValidBuilding(string? orgName = null, string? cityCn = null)
        {
            return new Building
            {
                OrgName = orgName ?? "测试机构",
                CityCn = cityCn ?? "北京",
                Address = "测试地址123号",
                OrgType = 1,
                Longitude = 116.4074,
                Latitude = 39.9042,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Deleted = false
            };
        }

        #endregion
    }
}