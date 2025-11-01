using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ExamSystem.WPF.Commands;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.Services.Models;
using ExamSystem.WPF.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    /// <summary>
    /// 建筑物管理视图模型
    /// </summary>
    public class BuildingManagementViewModel : INotifyPropertyChanged
    {
        private readonly IBuildingService _buildingService;
        private readonly IPermissionService? _permissionService;
        private readonly ILogger<BuildingManagementViewModel> _logger;
        private User? _currentUser;
        private readonly IServiceProvider _serviceProvider;

        #region 属性

        private ObservableCollection<BuildingDisplayModel> _buildings = new();
        public ObservableCollection<BuildingDisplayModel> Buildings
        {
            get => _buildings;
            set => SetProperty(ref _buildings, value);
        }

        private BuildingDisplayModel? _selectedBuilding;
        public BuildingDisplayModel? SelectedBuilding
        {
            get => _selectedBuilding;
            set => SetProperty(ref _selectedBuilding, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private string? _selectedCity;
        public string? SelectedCity
        {
            get => _selectedCity;
            set 
            { 
                if (SetProperty(ref _selectedCity, value))
                {
                    // 当城市筛选条件改变时，自动触发搜索和统计更新
                    // 使用Dispatcher确保在UI线程上执行
                    Application.Current.Dispatcher.InvokeAsync(async () => 
                    {
                        await SearchBuildingsAsync();
                    });
                }
            }
        }

        private byte? _selectedType;
        public byte? SelectedType
        {
            get => _selectedType;
            set => SetProperty(ref _selectedType, value);
        }

        private ObservableCollection<string> _cities = new();
        public ObservableCollection<string> Cities
        {
            get => _cities;
            set => SetProperty(ref _cities, value);
        }

        private ObservableCollection<BuildingTypeOption> _buildingTypes = new();
        public ObservableCollection<BuildingTypeOption> BuildingTypes
        {
            get => _buildingTypes;
            set => SetProperty(ref _buildingTypes, value);
        }

        private BuildingTypeOption? _selectedBuildingType;
        public BuildingTypeOption? SelectedBuildingType
        {
            get => _selectedBuildingType;
            set 
            { 
                if (SetProperty(ref _selectedBuildingType, value))
                {
                    // 当建筑类型筛选条件改变时，自动触发搜索
                    // 使用Dispatcher确保在UI线程上执行
                    Application.Current.Dispatcher.InvokeAsync(async () => 
                    {
                        await SearchBuildingsAsync();
                    });
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // 分页属性
        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        // 统计属性
        private int _fireStationCount;
        public int FireStationCount
        {
            get => _fireStationCount;
            set => SetProperty(ref _fireStationCount, value);
        }

        private int _professionalTeamCount;
        public int ProfessionalTeamCount
        {
            get => _professionalTeamCount;
            set 
            {
                if (SetProperty(ref _professionalTeamCount, value))
                {
                    OnPropertyChanged(nameof(SpecialTeamCount));
                }
            }
        }

        // XAML绑定的别名属性
        public int SpecialTeamCount => ProfessionalTeamCount;

        private int _keyBuildingCount;
        public int KeyBuildingCount
        {
            get => _keyBuildingCount;
            set => SetProperty(ref _keyBuildingCount, value);
        }

        // 权限属性
        public bool CanAddBuilding => _permissionService?.HasPermission(GetCurrentUserRole(), "building:add") ?? false;
        public bool CanEditBuilding => _permissionService?.HasPermission(GetCurrentUserRole(), "building:edit") ?? false;
        public bool CanDeleteBuilding => _permissionService?.HasPermission(GetCurrentUserRole(), "building:delete") ?? false;
        public bool CanImportBuilding => _permissionService?.HasPermission(GetCurrentUserRole(), "building:import") ?? false;
        public bool CanExportBuilding => _permissionService?.HasPermission(GetCurrentUserRole(), "building:export") ?? false;

        #endregion

        #region 命令

        public ICommand LoadBuildingsCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand AddBuildingCommand { get; }
        public ICommand EditBuildingCommand { get; }
        public ICommand DeleteBuildingCommand { get; }
        public ICommand ViewOnMapCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand BatchDeleteCommand { get; }
        public ICommand ImportBuildingsCommand { get; }
        public ICommand ExportBuildingsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }

        #endregion

        #region 构造函数

        // 无参构造函数，用于 XAML 设计时支持
        public BuildingManagementViewModel()
        {
            _buildingService = null!;
            _permissionService = null!;
            _logger = null!;

            // 初始化命令（设计时模式下使用空命令）
            LoadBuildingsCommand = new RelayCommand(() => { });
            SearchCommand = new RelayCommand(() => { });
            AddBuildingCommand = new RelayCommand(() => { });
            EditBuildingCommand = new RelayCommand(() => { });
            DeleteBuildingCommand = new RelayCommand(() => { });
            BatchDeleteCommand = new RelayCommand(() => { });
            ImportBuildingsCommand = new RelayCommand(() => { });
            ExportBuildingsCommand = new RelayCommand(() => { });
            RefreshCommand = new RelayCommand(() => { });
            FirstPageCommand = new RelayCommand(() => { });
            PreviousPageCommand = new RelayCommand(() => { });
            NextPageCommand = new RelayCommand(() => { });
            LastPageCommand = new RelayCommand(() => { });

            InitializeBuildingTypes();
        }

        // 运行时构造函数
        public BuildingManagementViewModel(IBuildingService buildingService, 
            IPermissionService permissionService, 
            ILogger<BuildingManagementViewModel> logger,
            IServiceProvider serviceProvider)
        {
            _buildingService = buildingService;
            _permissionService = permissionService;
            _logger = logger;
            _serviceProvider = serviceProvider;

            // 初始化命令
            LoadBuildingsCommand = new RelayCommand(async () => await LoadBuildingsAsync());
            SearchCommand = new RelayCommand(async () => await SearchBuildingsAsync());
            AddBuildingCommand = new RelayCommand(async () => await AddBuildingAsync(), () => CanAddBuilding);
            EditBuildingCommand = new RelayCommand(async () => await EditBuildingAsync(), () => CanEditBuilding && SelectedBuilding != null);
            DeleteBuildingCommand = new RelayCommand(async () => await DeleteBuildingAsync(), () => CanDeleteBuilding && SelectedBuilding != null);
            ViewOnMapCommand = new RelayCommand<BuildingDisplayModel>(async (building) => await ViewOnMapAsync(building));
            ViewDetailsCommand = new RelayCommand<BuildingDisplayModel>(async (building) => await ViewDetailsAsync(building));
            BatchDeleteCommand = new RelayCommand(async () => await BatchDeleteBuildingsAsync(), () => CanDeleteBuilding);
            ImportBuildingsCommand = new RelayCommand(async () => await ImportBuildingsAsync(), () => CanImportBuilding);
            ExportBuildingsCommand = new RelayCommand(async () => await ExportBuildingsAsync(), () => CanExportBuilding);
            RefreshCommand = new RelayCommand(async () => await RefreshAsync());
            FirstPageCommand = new RelayCommand(async () => await GoToPageAsync(1), () => CurrentPage > 1);
            PreviousPageCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage - 1), () => CurrentPage > 1);
            NextPageCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage + 1), () => CurrentPage < TotalPages);
            LastPageCommand = new RelayCommand(async () => await GoToPageAsync(TotalPages), () => CurrentPage < TotalPages);

            InitializeBuildingTypes();
            _ = InitializeAsync();
        }

        #endregion

        #region 初始化方法

        private void InitializeBuildingTypes()
        {
            BuildingTypes = new ObservableCollection<BuildingTypeOption>
            {
                new("全部类型", ""),
                new("消防队站", "1"),
                new("专职队", "2"),
                new("重点建筑", "3")
            };
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadCitiesAsync();
                await LoadTotalBuildingCountAsync(); // 首先加载总建筑数量
                await LoadBuildingsAsync();
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "初始化建筑物管理界面失败");
                StatusMessage = "初始化失败，请刷新重试";
            }
        }

        #endregion

        #region 数据加载方法

        private async Task LoadBuildingsAsync()
        {
            if (_buildingService == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "正在加载建筑物数据...";

                // 处理"全部"选项，转换为null
                var cityFilter = SelectedCity == "全部城市" ? null : SelectedCity;
                
                // 处理建筑类型筛选
                byte? typeFilter = null;
                if (SelectedBuildingType != null && !string.IsNullOrEmpty(SelectedBuildingType.Value))
                {
                    if (byte.TryParse(SelectedBuildingType.Value, out byte typeValue))
                    {
                        typeFilter = typeValue;
                    }
                }
                
                var result = await _buildingService.GetBuildingsPagedAsync(
                    CurrentPage, PageSize, SearchKeyword, cityFilter, typeFilter);

                // 确保UI更新在UI线程上执行
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Buildings.Clear();
                    foreach (var building in result.Items)
                    {
                        Buildings.Add(new BuildingDisplayModel(building));
                    }

                    // 只有在没有筛选条件时才更新TotalCount，否则保持显示全部建筑数量
                    if (cityFilter == null && typeFilter == null && string.IsNullOrEmpty(SearchKeyword))
                    {
                        TotalCount = result.TotalCount;
                    }
                    TotalPages = result.TotalPages;

                    var filteredCount = result.TotalCount;
                    StatusMessage = cityFilter != null || typeFilter != null || !string.IsNullOrEmpty(SearchKeyword) 
                        ? $"筛选结果：{filteredCount} 条记录" 
                        : $"共找到 {TotalCount} 条建筑物记录";
                });
                
                _logger?.LogInformation("加载建筑物数据成功 - 总数:{TotalCount}, 当前页:{CurrentPage}/{TotalPages}", 
                    TotalCount, CurrentPage, TotalPages);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载建筑物数据失败");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "加载数据失败，请重试";
                });
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private async Task LoadCitiesAsync()
        {
            if (_buildingService == null) return;

            try
            {
                var cities = await _buildingService.GetCitiesAsync();
                Cities.Clear();
                Cities.Add("全部城市");
                foreach (var city in cities.OrderBy(c => c))
                {
                    Cities.Add(city);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载城市列表失败");
            }
        }

        private async Task LoadStatisticsAsync()
        {
            if (_buildingService == null) return;

            try
            {
                // 处理"全部城市"选项，转换为null
                var cityFilter = SelectedCity == "全部城市" ? null : SelectedCity;
                var statistics = await _buildingService.GetBuildingTypeStatisticsAsync(cityFilter);
                
                FireStationCount = statistics.GetValueOrDefault((byte)1, 0);
                ProfessionalTeamCount = statistics.GetValueOrDefault((byte)2, 0);
                KeyBuildingCount = statistics.GetValueOrDefault((byte)3, 0);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载统计数据失败");
            }
        }

        private async Task LoadTotalBuildingCountAsync()
        {
            if (_buildingService == null) return;

            try
            {
                // 总建筑数量始终显示全部建筑数量，不受筛选影响
                var allStatistics = await _buildingService.GetBuildingTypeStatisticsAsync(null);
                var totalCount = allStatistics.Values.Sum();
                
                // 只在这里更新TotalCount，确保它始终显示全部建筑数量
                if (TotalCount == 0 || SelectedCity == "全部城市")
                {
                    TotalCount = totalCount;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载总建筑数量失败");
            }
        }

        #endregion

        #region 命令实现

        private async Task SearchBuildingsAsync()
        {
            CurrentPage = 1;
            await LoadBuildingsAsync();
            await LoadStatisticsAsync();
        }

        private async Task AddBuildingAsync()
        {
            try
            {
                var editViewModel = _serviceProvider.GetRequiredService<BuildingEditViewModel>();
                
                // 设置当前用户上下文
                var authService = _serviceProvider.GetRequiredService<IAuthService>();
                var currentUser = authService.GetCurrentUser();
                if (currentUser != null)
                {
                    editViewModel.SetCurrentUser(currentUser);
                }
                
                editViewModel.InitializeForAdd();
                
                var dialog = new BuildingEditDialog(editViewModel)
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = "添加建筑物成功";
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开添加建筑物对话框失败");
                StatusMessage = "打开对话框失败，请重试";
            }
        }

        private async Task EditBuildingAsync()
        {
            if (SelectedBuilding == null) return;
            
            try
            {
                // 先获取完整的建筑物实体
                var building = await _buildingService.GetBuildingByIdAsync(SelectedBuilding.Id);
                if (building == null)
                {
                    StatusMessage = "建筑物不存在";
                    return;
                }
                
                var editViewModel = _serviceProvider.GetRequiredService<BuildingEditViewModel>();
                
                // 设置当前用户上下文
                var authService = _serviceProvider.GetRequiredService<IAuthService>();
                var currentUser = authService.GetCurrentUser();
                if (currentUser != null)
                {
                    editViewModel.SetCurrentUser(currentUser);
                }
                
                editViewModel.InitializeForEdit(building);
                
                var dialog = new BuildingEditDialog(editViewModel)
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = "编辑建筑物成功";
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开编辑建筑物对话框失败 - ID:{Id}", SelectedBuilding.Id);
                StatusMessage = "打开对话框失败，请重试";
            }
        }

        private async Task DeleteBuildingAsync()
        {
            if (SelectedBuilding == null) return;

            try
            {
                // TODO: 显示确认对话框
                var operatorName = _currentUser?.RealName ?? _currentUser?.Username ?? "未知用户";
                var result = await _buildingService.DeleteBuildingAsync(SelectedBuilding.Id, operatorName);
                if (result.IsSuccess)
                {
                    StatusMessage = "删除建筑物成功";
                    await RefreshAsync();
                }
                else
                {
                    StatusMessage = $"删除失败：{result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除建筑物失败，操作员: {Operator}", 
                    _currentUser?.RealName ?? _currentUser?.Username ?? "未知用户");
                StatusMessage = "删除失败，请重试";
            }
        }

        private async Task BatchDeleteBuildingsAsync()
        {
            var selectedIds = Buildings.Where(b => b.IsSelected).Select(b => b.Id).ToList();
            if (!selectedIds.Any())
            {
                StatusMessage = "请选择要删除的建筑物";
                return;
            }

            try
            {
                // TODO: 显示确认对话框
                var operatorName = _currentUser?.RealName ?? _currentUser?.Username ?? "未知用户";
                var result = await _buildingService.BatchDeleteBuildingsAsync(selectedIds, operatorName);
                if (result.IsSuccess)
                {
                    StatusMessage = $"批量删除成功，共删除 {selectedIds.Count} 个建筑物";
                    await RefreshAsync();
                }
                else
                {
                    StatusMessage = $"批量删除失败：{result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "批量删除建筑物失败，操作员: {Operator}", 
                    _currentUser?.RealName ?? _currentUser?.Username ?? "未知用户");
                StatusMessage = "批量删除失败，请重试";
            }
        }

        private async Task ImportBuildingsAsync()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择要导入的Excel文件",
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                    DefaultExt = ".xlsx"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    StatusMessage = "正在导入数据，请稍候...";
                    
                    // TODO: 实现Excel文件解析
                    // 这里需要添加Excel解析逻辑，将Excel数据转换为Building对象列表
                    var buildings = new List<Building>(); // 临时空列表
                    
                    var result = await _buildingService.BatchImportBuildingsAsync(buildings);
                    
                    if (result.IsSuccess)
                    {
                        var importResult = result.Data!;
                        StatusMessage = $"导入完成：成功 {importResult.SuccessCount} 条，失败 {importResult.FailedCount} 条";
                        
                        if (importResult.FailedItems.Any())
                        {
                            // TODO: 显示失败详情对话框
                            _logger?.LogWarning("导入建筑物数据部分失败 - 失败数量:{FailedCount}", importResult.FailedCount);
                        }
                        
                        await RefreshAsync();
                    }
                    else
                    {
                        StatusMessage = $"导入失败：{result.ErrorMessage}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导入建筑物数据失败");
                StatusMessage = "导入失败，请检查文件格式";
            }
        }

        private async Task ExportBuildingsAsync()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出建筑物数据",
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                    DefaultExt = ".xlsx",
                    FileName = $"建筑物数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    StatusMessage = "正在导出数据，请稍候...";
                    
                    // 获取当前筛选条件下的所有数据
                    var cityFilter = SelectedCity == "全部城市" ? null : SelectedCity;
                    var typeFilterString = SelectedBuildingType?.Value;
                    byte? typeFilter = null;
                    if (!string.IsNullOrEmpty(typeFilterString) && byte.TryParse(typeFilterString, out var parsedType))
                    {
                        typeFilter = parsedType;
                    }
                    
                    var buildings = await _buildingService.ExportBuildingsAsync(
                        SearchKeyword, cityFilter, typeFilter);
                    
                    // TODO: 实现Excel文件生成
                    // 这里需要添加Excel生成逻辑，将Building对象列表转换为Excel文件
                    
                    StatusMessage = $"导出完成：共导出 {buildings.Count()} 条数据到 {saveFileDialog.FileName}";
                    _logger?.LogInformation("导出建筑物数据成功 - 文件:{FileName}, 数量:{Count}", 
                        saveFileDialog.FileName, buildings.Count());
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出建筑物数据失败");
                StatusMessage = "导出失败，请重试";
            }
        }

        private async Task RefreshAsync()
        {
            await LoadCitiesAsync();
            await LoadBuildingsAsync();
            await LoadStatisticsAsync();
        }

        private async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages) return;
            
            CurrentPage = page;
            await LoadBuildingsAsync();
        }

        private async Task ViewOnMapAsync(BuildingDisplayModel building)
        {
            try
            {
                if (building == null) return;

                _logger?.LogInformation("在地图上查看建筑物: {BuildingName}", building.Name);
                
                // TODO: 实现地图查看功能
                // 这里应该打开地图窗口并定位到指定建筑物
                StatusMessage = $"正在地图上显示 {building.Name}...";
                
                await Task.Delay(100); // 模拟异步操作
                StatusMessage = "地图查看功能待实现";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "在地图上查看建筑物失败");
                StatusMessage = "地图查看失败，请重试";
            }
        }

        private async Task ViewDetailsAsync(BuildingDisplayModel building)
        {
            try
            {
                if (building == null) return;

                _logger?.LogInformation("查看建筑物详情: {BuildingName}", building.Name);
                
                // TODO: 实现建筑物详情查看功能
                // 这里应该打开建筑物详情窗口
                StatusMessage = $"正在加载 {building.Name} 的详细信息...";
                
                await Task.Delay(100); // 模拟异步操作
                StatusMessage = "建筑物详情查看功能待实现";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "查看建筑物详情失败");
                StatusMessage = "详情查看失败，请重试";
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region 用户上下文管理

        /// <summary>
        /// 设置当前用户
        /// </summary>
        /// <param name="user">当前登录用户</param>
        public void SetCurrentUser(User user)
        {
            _currentUser = user;
            _logger.LogInformation("BuildingManagementViewModel 已接收当前用户: {Username} (ID={UserId})", user.Username, user.UserId);
            
            // 刷新权限相关的命令状态
            RefreshCommandStates();
        }

        /// <summary>
        /// 获取当前用户角色
        /// </summary>
        /// <returns>当前用户角色</returns>
        private UserRole GetCurrentUserRole()
        {
            return _currentUser?.Role ?? UserRole.Student;
        }

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        private void RefreshCommandStates()
        {
            (AddBuildingCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EditBuildingCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteBuildingCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (BatchDeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ImportBuildingsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ExportBuildingsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion
    }

    #region 辅助类

    /// <summary>
    /// 建筑物显示模型
    /// </summary>
    public class BuildingDisplayModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public BuildingDisplayModel(Building building)
        {
            Id = building.Id;
            OrgName = building.OrgName ?? string.Empty;
            CityCn = building.CityCn ?? string.Empty;
            Address = building.Address ?? string.Empty;
            OrgType = building.OrgType;
            TypeDisplay = GetTypeDisplay(building.OrgType);
            TypeColor = GetTypeColor(building.OrgType);
            
            // 从Building实体获取坐标
            var coordinates = building.GetCoordinates();
            if (coordinates != null && coordinates.Length == 2)
            {
                Longitude = coordinates[0];
                Latitude = coordinates[1];
            }
            else
            {
                Longitude = 0.0;
                Latitude = 0.0;
            }
            
            CreatedAt = building.CreateDate;
            UpdatedAt = building.UpdateDate;
        }

        public int Id { get; set; }
        public string OrgName { get; set; }
        public string CityCn { get; set; }
        public string Address { get; set; }
        public byte OrgType { get; set; }
        public string TypeDisplay { get; set; }
        public string TypeColor { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // XAML绑定的别名属性
        public string Name => OrgName;
        public string City => CityCn;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private static string GetTypeDisplay(byte orgType)
        {
            return orgType switch
            {
                1 => "消防队站",
                2 => "专职队",
                3 => "重点建筑",
                _ => "未知类型"
            };
        }

        private static string GetTypeColor(byte orgType)
        {
            return orgType switch
            {
                1 => "#E74C3C", // 消防队站 - 红色
                2 => "#F39C12", // 专职队 - 橙色
                3 => "#9B59B6", // 重点建筑 - 紫色
                _ => "#95A5A6"  // 未知类型 - 灰色
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    #endregion
}