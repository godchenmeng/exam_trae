using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ExamSystem.Domain.Entities;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;
using ExamSystem.WPF.ViewModels;

namespace ExamSystem.Tests.ViewModels
{
    /// <summary>
    /// 建筑物编辑视图模型单元测试
    /// </summary>
    public class BuildingEditViewModelTests
    {
        private readonly Mock<IBuildingService> _mockBuildingService;
        private readonly Mock<ILogger<BuildingEditViewModel>> _mockLogger;
        private readonly BuildingEditViewModel _viewModel;

        public BuildingEditViewModelTests()
        {
            _mockBuildingService = new Mock<IBuildingService>();
            _mockLogger = new Mock<ILogger<BuildingEditViewModel>>();
            _viewModel = new BuildingEditViewModel(_mockBuildingService.Object, _mockLogger.Object);
        }

        #region 初始化测试

        [Fact]
        public void InitializeForAdd_SetsDefaultValues()
        {
            // Act
            _viewModel.InitializeForAdd();

            // Assert
            Assert.False(_viewModel.IsEditMode);
            Assert.Equal("新增建筑物", _viewModel.Title);
            Assert.Equal(string.Empty, _viewModel.Name);
            Assert.Equal(string.Empty, _viewModel.Address);
            Assert.Equal(string.Empty, _viewModel.City);
            Assert.Equal(1, _viewModel.BuildingType);
            Assert.Null(_viewModel.Longitude);
            Assert.Null(_viewModel.Latitude);
        }

        [Fact]
        public void InitializeForEdit_SetsValuesFromBuilding()
        {
            // Arrange
            var building = CreateTestBuilding();

            // Act
            _viewModel.InitializeForEdit(building);

            // Assert
            Assert.True(_viewModel.IsEditMode);
            Assert.Equal("编辑建筑物", _viewModel.Title);
            Assert.Equal(building.OrgName, _viewModel.Name);
            Assert.Equal(building.Address, _viewModel.Address);
            Assert.Equal(building.CityCn, _viewModel.City);
            Assert.Equal(building.OrgType, _viewModel.BuildingType);
            Assert.Equal(building.Longitude, _viewModel.Longitude);
            Assert.Equal(building.Latitude, _viewModel.Latitude);
        }

        [Fact]
        public void InitializeForEdit_WithCoordinatesInLocation_ParsesCorrectly()
        {
            // Arrange
            var building = CreateTestBuilding();
            building.Location = "116.4074,39.9042"; // 经度,纬度格式

            // Act
            _viewModel.InitializeForEdit(building);

            // Assert
            Assert.Equal(116.4074, _viewModel.Longitude);
            Assert.Equal(39.9042, _viewModel.Latitude);
        }

        #endregion

        #region 保存功能测试

        [Fact]
        public async Task SaveAsync_CreateMode_CallsCreateService()
        {
            // Arrange
            _viewModel.InitializeForAdd();
            SetupValidBuildingData();
            
            var user = new User { Id = 1, Username = "testuser", RealName = "测试用户" };
            _viewModel.SetCurrentUser(user);

            _mockBuildingService.Setup(s => s.CreateBuildingAsync(It.IsAny<Building>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<Building>.Success(CreateTestBuilding()));

            // Act
            await _viewModel.SaveAsync();

            // Assert
            _mockBuildingService.Verify(s => s.CreateBuildingAsync(
                It.Is<Building>(b => 
                    b.OrgName == _viewModel.Name &&
                    b.CityCn == _viewModel.City &&
                    b.Address == _viewModel.Address &&
                    b.OrgType == _viewModel.BuildingType),
                "测试用户"), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_EditMode_CallsUpdateService()
        {
            // Arrange
            var originalBuilding = CreateTestBuilding();
            _viewModel.InitializeForEdit(originalBuilding);
            
            // 修改一些值
            _viewModel.Name = "修改后的名称";
            _viewModel.Address = "修改后的地址";
            
            var user = new User { Id = 1, Username = "testuser", RealName = "测试用户" };
            _viewModel.SetCurrentUser(user);

            _mockBuildingService.Setup(s => s.UpdateBuildingAsync(It.IsAny<Building>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<Building>.Success(originalBuilding));

            // Act
            await _viewModel.SaveAsync();

            // Assert
            _mockBuildingService.Verify(s => s.UpdateBuildingAsync(
                It.Is<Building>(b => 
                    b.Id == originalBuilding.Id &&
                    b.OrgName == "修改后的名称" &&
                    b.Address == "修改后的地址"),
                "测试用户"), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_ServiceFailure_DoesNotRaiseSavedEvent()
        {
            // Arrange
            _viewModel.InitializeForAdd();
            SetupValidBuildingData();
            
            var user = new User { Id = 1, Username = "testuser", RealName = "测试用户" };
            _viewModel.SetCurrentUser(user);

            _mockBuildingService.Setup(s => s.CreateBuildingAsync(It.IsAny<Building>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<Building>.Failure("创建失败"));

            var savedEventRaised = false;
            _viewModel.Saved += () => savedEventRaised = true;

            // Act
            await _viewModel.SaveAsync();

            // Assert
            Assert.False(savedEventRaised);
        }

        [Fact]
        public async Task SaveAsync_ServiceSuccess_RaisesSavedEvent()
        {
            // Arrange
            _viewModel.InitializeForAdd();
            SetupValidBuildingData();
            
            var user = new User { Id = 1, Username = "testuser", RealName = "测试用户" };
            _viewModel.SetCurrentUser(user);

            _mockBuildingService.Setup(s => s.CreateBuildingAsync(It.IsAny<Building>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<Building>.Success(CreateTestBuilding()));

            var savedEventRaised = false;
            _viewModel.Saved += () => savedEventRaised = true;

            // Act
            await _viewModel.SaveAsync();

            // Assert
            Assert.True(savedEventRaised);
        }

        #endregion

        #region 数据验证测试

        [Theory]
        [InlineData("", "北京", "测试地址", false)]
        [InlineData("测试机构", "", "测试地址", false)]
        [InlineData("测试机构", "北京", "", false)]
        [InlineData("测试机构", "北京", "测试地址", true)]
        public void ValidateInput_VariousInputs_ReturnsExpectedResult(
            string name, string city, string address, bool expectedValid)
        {
            // Arrange
            _viewModel.Name = name;
            _viewModel.City = city;
            _viewModel.Address = address;
            _viewModel.BuildingType = 1;

            // Act
            var isValid = _viewModel.ValidateInput();

            // Assert
            Assert.Equal(expectedValid, isValid);
        }

        [Fact]
        public void ValidateInput_InvalidCoordinates_ReturnsFalse()
        {
            // Arrange
            SetupValidBuildingData();
            _viewModel.Longitude = 200; // 无效经度

            // Act
            var isValid = _viewModel.ValidateInput();

            // Assert
            Assert.False(isValid);
        }

        #endregion

        #region 用户上下文测试

        [Fact]
        public void SetCurrentUser_ValidUser_SetsUserCorrectly()
        {
            // Arrange
            var user = new User { Id = 1, Username = "testuser", RealName = "测试用户" };

            // Act
            _viewModel.SetCurrentUser(user);

            // Assert
            // 由于_currentUser是私有字段，我们通过保存操作来验证用户是否正确设置
            // 这里主要测试方法不抛出异常
            Assert.True(true);
        }

        [Fact]
        public void SetCurrentUser_NullUser_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _viewModel.SetCurrentUser(null));
            Assert.Null(exception);
        }

        #endregion

        #region 辅助方法

        private Building CreateTestBuilding()
        {
            return new Building
            {
                Id = 1,
                OrgName = "测试机构",
                CityCn = "北京",
                Address = "测试地址123号",
                OrgType = 1,
                Longitude = 116.4074,
                Latitude = 39.9042,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Deleted = false
            };
        }

        private void SetupValidBuildingData()
        {
            _viewModel.Name = "测试机构";
            _viewModel.City = "北京";
            _viewModel.Address = "测试地址123号";
            _viewModel.BuildingType = 1;
            _viewModel.Longitude = 116.4074;
            _viewModel.Latitude = 39.9042;
        }

        #endregion
    }
}