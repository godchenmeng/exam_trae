using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    public class BuildingEditViewModel : INotifyPropertyChanged
    {
        private readonly IBuildingService _buildingService;
        private readonly ILogger<BuildingEditViewModel> _logger;
        private User? _currentUser;
        private Building? _originalBuilding;

        public event PropertyChangedEventHandler? PropertyChanged;

        // 编辑模式
        public bool IsEditMode { get; private set; }
        public int? BuildingId { get; private set; }

        // 建筑物属性
        private string _name = string.Empty;
        [Required(ErrorMessage = "建筑名称不能为空")]
        [StringLength(100, ErrorMessage = "建筑名称长度不能超过100个字符")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                ValidateProperty(value);
            }
        }

        private string _address = string.Empty;
        [Required(ErrorMessage = "地址不能为空")]
        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged();
                ValidateProperty(value);
            }
        }

        private string _city = string.Empty;
        [Required(ErrorMessage = "城市不能为空")]
        [StringLength(50, ErrorMessage = "城市名称长度不能超过50个字符")]
        public string City
        {
            get => _city;
            set
            {
                _city = value;
                OnPropertyChanged();
                ValidateProperty(value);
            }
        }

        private string _buildingType = string.Empty;
        [Required(ErrorMessage = "建筑类型不能为空")]
        public string BuildingType
        {
            get => _buildingType;
            set
            {
                _buildingType = value;
                OnPropertyChanged();
                ValidateProperty(value);
            }
        }

        private double? _longitude;
        [Range(-180, 180, ErrorMessage = "经度必须在-180到180之间")]
        public double? Longitude
        {
            get => _longitude;
            set
            {
                _longitude = value;
                OnPropertyChanged();
                ValidateProperty(value);
            }
        }

        private double? _latitude;
        [Range(-90, 90, ErrorMessage = "纬度必须在-90到90之间")]
        public double? Latitude
        {
            get => _latitude;
            set
            {
                _latitude = value;
                OnPropertyChanged();
                ValidateProperty(value);
            }
        }

        private string? _description;
        [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
        public string? Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
                ValidateProperty(value);
            }
        }

        // 建筑类型选项
        public List<BuildingTypeOption> BuildingTypeOptions { get; }

        // 验证错误
        private Dictionary<string, string> _validationErrors = new();
        public Dictionary<string, string> ValidationErrors
        {
            get => _validationErrors;
            set
            {
                _validationErrors = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasValidationErrors));
            }
        }

        public bool HasValidationErrors => ValidationErrors.Any();

        // 状态属性
        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                _isSaving = value;
                OnPropertyChanged();
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        // 命令
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectLocationCommand { get; }

        // 事件
        public event EventHandler<Building>? BuildingSaved;
        public event EventHandler? EditCancelled;

        public BuildingEditViewModel(IBuildingService buildingService, ILogger<BuildingEditViewModel> logger)
        {
            _buildingService = buildingService ?? throw new ArgumentNullException(nameof(buildingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            BuildingTypeOptions = new List<BuildingTypeOption>
            {
                new("消防队站", "1"),
                new("专职队", "2"),
                new("重点建筑", "3")
            };

            SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
            CancelCommand = new RelayCommand(Cancel);
            SelectLocationCommand = new RelayCommand(SelectLocation);
        }

        /// <summary>
        /// 设置当前用户
        /// </summary>
        /// <param name="user">当前用户</param>
        public void SetCurrentUser(User user)
        {
            _currentUser = user;
            _logger.LogDebug("BuildingEditViewModel: 设置当前用户 - {Username} ({Role})", 
                user?.Username, user?.Role);
        }

        // 初始化编辑模式
        public void InitializeForEdit(Building building)
        {
            _originalBuilding = building;
            IsEditMode = true;
            BuildingId = building.Id;

            Name = building.OrgName ?? string.Empty;
            Address = building.Address ?? string.Empty;
            City = building.CityCn ?? string.Empty;
            BuildingType = building.OrgType.ToString();
            
            // 解析坐标
            var coordinates = building.GetCoordinates();
            if (coordinates != null)
            {
                Longitude = coordinates[0];
                Latitude = coordinates[1];
            }
            else
            {
                Longitude = null;
                Latitude = null;
            }
            
            Description = building.Location ?? string.Empty;

            OnPropertyChanged(nameof(IsEditMode));
        }

        // 初始化新增模式
        public void InitializeForAdd()
        {
            _originalBuilding = null;
            IsEditMode = false;
            BuildingId = null;

            Name = string.Empty;
            Address = string.Empty;
            City = string.Empty;
            BuildingType = BuildingTypeOptions.First().Value;
            Longitude = null;
            Latitude = null;
            Description = string.Empty;

            ValidationErrors.Clear();

            OnPropertyChanged(nameof(IsEditMode));
        }

        private bool CanSave()
        {
            return !HasValidationErrors && !IsSaving && 
                   !string.IsNullOrWhiteSpace(Name) && 
                   !string.IsNullOrWhiteSpace(Address) && 
                   !string.IsNullOrWhiteSpace(City) && 
                   !string.IsNullOrWhiteSpace(BuildingType);
        }

        private async Task SaveAsync()
        {
            try
            {
                IsSaving = true;
                StatusMessage = "正在保存...";

                // 验证所有属性
                if (!ValidateAllProperties())
                {
                    StatusMessage = "请修正验证错误后再保存";
                    return;
                }

                // 获取操作员名称
                var operatorName = _currentUser?.RealName ?? _currentUser?.Username ?? "未知用户";

                var building = new Building
                {
                    OrgName = Name?.Trim(),
                    Address = Address?.Trim(),
                    CityCn = City?.Trim(),
                    OrgType = byte.Parse(BuildingType ?? "1"),
                    CreatorId = _currentUser?.UserId,
                    UpdateDate = DateTime.Now
                };

                // 设置坐标
                if (Longitude.HasValue && Latitude.HasValue && Longitude != 0 && Latitude != 0)
                {
                    building.SetCoordinates(Longitude.Value, Latitude.Value);
                }

                Building savedBuilding;
                if (IsEditMode && BuildingId.HasValue)
                {
                    building.Id = BuildingId.Value;
                    building.CreateDate = _originalBuilding?.CreateDate ?? DateTime.Now;
                    var updateResult = await _buildingService.UpdateBuildingAsync(building, operatorName);
                    if (updateResult.IsSuccess)
                    {
                        savedBuilding = updateResult.Data!;
                        StatusMessage = "建筑信息更新成功";
                        _logger.LogInformation("建筑信息更新成功，ID: {BuildingId}, 名称: {Name}, 操作员: {Operator}", 
                            building.Id, building.OrgName, operatorName);
                    }
                    else
                    {
                        StatusMessage = $"更新失败: {updateResult.ErrorMessage}";
                    _logger.LogError("建筑信息更新失败: {ErrorMessage}", updateResult.ErrorMessage);
                        return;
                    }
                }
                else
                {
                    building.CreateDate = DateTime.Now;
                    var createResult = await _buildingService.CreateBuildingAsync(building, operatorName);
                    if (createResult.IsSuccess)
                    {
                        savedBuilding = createResult.Data!;
                        StatusMessage = "建筑添加成功";
                        _logger.LogInformation("新建筑添加成功，ID: {BuildingId}, 名称: {Name}, 操作员: {Operator}", 
                            savedBuilding.Id, savedBuilding.OrgName, operatorName);
                    }
                    else
                    {
                        StatusMessage = $"添加失败: {createResult.ErrorMessage}";
                    _logger.LogError("建筑添加失败: {ErrorMessage}", createResult.ErrorMessage);
                        return;
                    }
                }

                BuildingSaved?.Invoke(this, savedBuilding);
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
                _logger.LogError(ex, "保存建筑信息时发生错误，操作员: {Operator}", 
                    _currentUser?.RealName ?? _currentUser?.Username ?? "未知用户");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void Cancel()
        {
            EditCancelled?.Invoke(this, EventArgs.Empty);
        }

        private void SelectLocation()
        {
            // TODO: 打开地图选择位置对话框
            StatusMessage = "地图选择功能待实现";
        }

        private bool ValidateAllProperties()
        {
            ValidationErrors.Clear();

            ValidateProperty(Name, nameof(Name));
            ValidateProperty(Address, nameof(Address));
            ValidateProperty(City, nameof(City));
            ValidateProperty(BuildingType, nameof(BuildingType));
            ValidateProperty(Longitude, nameof(Longitude));
            ValidateProperty(Latitude, nameof(Latitude));
            ValidateProperty(Description, nameof(Description));

            return !HasValidationErrors;
        }

        private void ValidateProperty(object? value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (propertyName == null) return;

            var context = new ValidationContext(this) { MemberName = propertyName };
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            if (Validator.TryValidateProperty(value, context, results))
            {
                if (ValidationErrors.ContainsKey(propertyName))
                {
                    ValidationErrors.Remove(propertyName);
                    OnPropertyChanged(nameof(ValidationErrors));
                    OnPropertyChanged(nameof(HasValidationErrors));
                }
            }
            else
            {
                var error = results.First().ErrorMessage ?? "验证失败";
                ValidationErrors[propertyName] = error;
                OnPropertyChanged(nameof(ValidationErrors));
                OnPropertyChanged(nameof(HasValidationErrors));
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BuildingTypeOption
    {
        public string DisplayName { get; }
        public string Value { get; }

        public BuildingTypeOption(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }
}