using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Domain.Models;
using ExamSystem.Services.Interfaces;
using Microsoft.Web.WebView2.Wpf;

namespace ExamSystem.WPF.ViewModels
{
    public class MapDrawingAuthoringViewModel : INotifyPropertyChanged
    {
        private readonly IMapDrawingService _mapDrawingService;
        private readonly IQuestionService _questionService;
        private WebView2 _webView;

        private bool _isLoading;
        private bool _isMapLoading;
        private bool _canSave;
        private string _validationMessage;
        private string _layerInfoText;
        private Question _question;
        private MapDrawingConfig _mapConfig;
        private ReviewRubric _reviewRubric;
        private BuildingLayersConfig _buildingLayersConfig;

        public MapDrawingAuthoringViewModel(IMapDrawingService mapDrawingService, IQuestionService questionService)
        {
            _mapDrawingService = mapDrawingService ?? throw new ArgumentNullException(nameof(mapDrawingService));
            _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));

            InitializeCommands();
            InitializeData();
        }

        #region Properties

        public string DialogTitle => Question?.QuestionId > 0 ? "编辑地图绘制题" : "创建地图绘制题";

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsMapLoading
        {
            get => _isMapLoading;
            set => SetProperty(ref _isMapLoading, value);
        }

        public bool CanSave
        {
            get => _canSave;
            set => SetProperty(ref _canSave, value);
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        public string LayerInfoText
        {
            get => _layerInfoText;
            set => SetProperty(ref _layerInfoText, value);
        }

        public Question Question
        {
            get => _question;
            set
            {
                if (SetProperty(ref _question, value))
                {
                    OnPropertyChanged(nameof(DialogTitle));
                    ValidateForm();
                }
            }
        }

        public MapDrawingConfig MapConfig
        {
            get => _mapConfig;
            set
            {
                if (SetProperty(ref _mapConfig, value))
                {
                    ValidateForm();
                }
            }
        }

        public ReviewRubric ReviewRubric
        {
            get => _reviewRubric;
            set
            {
                if (SetProperty(ref _reviewRubric, value))
                {
                    ValidateForm();
                }
            }
        }

        public BuildingLayersConfig BuildingLayersConfig
        {
            get => _buildingLayersConfig;
            set => SetProperty(ref _buildingLayersConfig, value);
        }

        // 添加缺失的属性
        public ObservableCollection<OverlayDTO> GuidanceOverlays { get; set; } = new ObservableCollection<OverlayDTO>();
        public ObservableCollection<OverlayDTO> ReferenceOverlays { get; set; } = new ObservableCollection<OverlayDTO>();

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand PreviewMapCommand { get; private set; }
        public ICommand SetGuidanceOverlaysCommand { get; private set; }
        public ICommand SetReferenceAnswerCommand { get; private set; }
        public ICommand ClearOverlaysCommand { get; private set; }
        public ICommand PreviewQuestionCommand { get; private set; }
        public ICommand AddRubricCriterionCommand { get; private set; }
        public ICommand RemoveRubricCriterionCommand { get; private set; }

        #endregion

        #region Events

        public event EventHandler SaveCompleted;
        public event EventHandler MapPreviewRequested;
        public event EventHandler GuidanceOverlaysRequested;
        public event EventHandler ReferenceAnswerRequested;
        public event EventHandler ClearOverlaysRequested;

        #endregion

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave);
            PreviewMapCommand = new RelayCommand(() => MapPreviewRequested?.Invoke(this, EventArgs.Empty));
            SetGuidanceOverlaysCommand = new RelayCommand(() => GuidanceOverlaysRequested?.Invoke(this, EventArgs.Empty));
            SetReferenceAnswerCommand = new RelayCommand(() => ReferenceAnswerRequested?.Invoke(this, EventArgs.Empty));
            ClearOverlaysCommand = new RelayCommand(() => ClearOverlaysRequested?.Invoke(this, EventArgs.Empty));
            PreviewQuestionCommand = new RelayCommand(async () => await PreviewQuestionAsync());
            AddRubricCriterionCommand = new RelayCommand(AddRubricCriterion);
            RemoveRubricCriterionCommand = new RelayCommand<RubricCriterion>(RemoveRubricCriterion);
        }

        private void InitializeData()
        {
            Question = new Question
            {
                QuestionType = QuestionType.MapDrawing,
                Score = 10,
                TimeLimitSeconds = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            MapConfig = new MapDrawingConfig
            {
                CenterLat = 39.9042,
                CenterLng = 116.4074,
                ZoomLevel = 10,
                UseOffline = false,
                TileUrl = string.Empty,
                Bounds = new MapBounds
                {
                    North = 40.0,
                    South = 39.8,
                    East = 116.5,
                    West = 116.3
                },
                Constraints = new MapDrawingConstraints
                {
                    AllowPoints = true,
                    AllowLines = true,
                    AllowPolygons = true,
                    AllowCircles = false,
                    MaxOverlays = 10
                }
            };

            ReviewRubric = new ReviewRubric
            {
                Criteria = new List<RubricCriterion>
                {
                    new RubricCriterion
                    {
                        Name = "位置准确性",
                        Description = "绘制位置是否准确",
                        MaxScore = 5
                    },
                    new RubricCriterion
                    {
                        Name = "形状完整性",
                        Description = "绘制形状是否完整",
                        MaxScore = 3
                    },
                    new RubricCriterion
                    {
                        Name = "细节表现",
                        Description = "细节表现是否到位",
                        MaxScore = 2
                    }
                }
            };

            BuildingLayersConfig = new BuildingLayersConfig
            {
                ShowOutlines = true,
                ShowLabels = true,
                TypeFilters = new List<string>(),
                Opacity = 0.8
            };

            UpdateLayerInfo();
            ValidateForm();
        }

        public void SetWebView(WebView2 webView)
        {
            _webView = webView;
        }

        public void SendMessageToWebView(object message)
        {
            if (_webView?.CoreWebView2 != null)
            {
                var json = JsonSerializer.Serialize(message);
                _webView.CoreWebView2.PostWebMessageAsString(json);
            }
        }

        public void HandleWebViewMessage(string message)
        {
            try
            {
                var messageObj = JsonSerializer.Deserialize<JsonElement>(message);
                var messageType = messageObj.GetProperty("type").GetString();

                switch (messageType)
                {
                    case "MapLoaded":
                        IsMapLoading = false;
                        SendMapConfigToWebView();
                        break;

                    case "OverlaysUpdated":
                        HandleOverlaysUpdated(messageObj);
                        break;

                    case "Error":
                        var error = messageObj.GetProperty("message").GetString();
                        ValidationMessage = $"地图错误: {error}";
                        break;
                }
            }
            catch (Exception ex)
            {
                ValidationMessage = $"处理地图消息时出错: {ex.Message}";
            }
        }

        private void SendMapConfigToWebView()
        {
            SendMessageToWebView(new
            {
                type = "LoadConfig",
                config = MapConfig,
                buildingLayers = BuildingLayersConfig
            });
        }

        private void HandleOverlaysUpdated(JsonElement messageObj)
        {
            try
            {
                var overlaysJson = messageObj.GetProperty("overlays").GetRawText();
                var mode = messageObj.GetProperty("mode").GetString();

                // 根据模式更新相应的图层数据
                switch (mode)
                {
                    case "guidance":
                        // 更新指引图层
                        break;
                    case "reference":
                        // 更新参考答案图层
                        break;
                }

                UpdateLayerInfo();
            }
            catch (Exception ex)
            {
                ValidationMessage = $"更新图层数据时出错: {ex.Message}";
            }
        }

        private void UpdateLayerInfo()
        {
            var info = $"地图中心: ({MapConfig.CenterLat:F4}, {MapConfig.CenterLng:F4})\n";
            info += $"缩放级别: {MapConfig.ZoomLevel}\n";
            info += $"评分项数量: {ReviewRubric.Criteria.Count}";
            LayerInfoText = info;
        }

        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;
                ValidationMessage = string.Empty;

                if (!ValidateForm())
                {
                    return;
                }

                if (Question.QuestionId > 0)
                {
                    // 更新现有题目
                    await _mapDrawingService.UpdateMapDrawingQuestionAsync(Question.QuestionId, new MapDrawingQuestionDto
                    {
                        Title = Question.Title,
                        Content = Question.Content,
                        Score = Question.Score,
                        TimeLimitSeconds = Question.TimeLimitSeconds,
                        Tags = Question.Tags,
                        Config = MapConfig,
                        ReviewRubric = ReviewRubric,
                        BuildingLayersConfig = BuildingLayersConfig
                    });
                }
                else
                {
                    // 创建新题目
                    await _mapDrawingService.CreateMapDrawingQuestionAsync(
                        bankId: Question.BankId,
                        title: Question.Title,
                        content: Question.Content,
                        config: MapConfig,
                        guidanceOverlays: GuidanceOverlays?.ToList(),
                        referenceOverlays: ReferenceOverlays?.ToList(),
                        reviewRubric: ReviewRubric,
                        buildingLayersConfig: BuildingLayersConfig,
                        timeLimitSeconds: Question.TimeLimitSeconds,
                        score: Question.Score);
                }

                SaveCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ValidationMessage = $"保存失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private Task PreviewQuestionAsync()
        {
            try
            {
                // 发送预览数据到WebView
                SendMessageToWebView(new
                {
                    type = "PreviewQuestion",
                    question = new
                    {
                        title = Question.Title,
                        content = Question.Content,
                        score = Question.Score,
                        timeLimit = Question.TimeLimitSeconds
                    },
                    config = MapConfig,
                    buildingLayers = BuildingLayersConfig
                });
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                ValidationMessage = $"预览失败: {ex.Message}";
                return Task.CompletedTask;
            }
        }

        private void AddRubricCriterion()
        {
            ReviewRubric.Criteria.Add(new RubricCriterion
            {
                Name = "新评分项",
                Description = "请输入评分标准",
                MaxScore = 1
            });
            ValidateForm();
        }

        private void RemoveRubricCriterion(RubricCriterion criterion)
        {
            if (criterion != null)
            {
                ReviewRubric.Criteria.Remove(criterion);
                ValidateForm();
            }
        }

        private bool ValidateForm()
        {
            ValidationMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Question?.Title))
            {
                ValidationMessage = "请输入题目标题";
                CanSave = false;
                return false;
            }

            if (string.IsNullOrWhiteSpace(Question?.Content))
            {
                ValidationMessage = "请输入题目内容";
                CanSave = false;
                return false;
            }

            if (Question.Score <= 0)
            {
                ValidationMessage = "分值必须大于0";
                CanSave = false;
                return false;
            }

            if (MapConfig == null)
            {
                ValidationMessage = "地图配置不能为空";
                CanSave = false;
                return false;
            }

            if (ReviewRubric?.Criteria?.Count == 0)
            {
                ValidationMessage = "至少需要一个评分项";
                CanSave = false;
                return false;
            }

            var totalRubricScore = ReviewRubric.Criteria.Sum(c => c.MaxScore);
            if (totalRubricScore != Question.Score)
            {
                ValidationMessage = $"评分项总分({totalRubricScore})与题目分值({Question.Score})不匹配";
                CanSave = false;
                return false;
            }

            CanSave = true;
            return true;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    // 简单的RelayCommand实现
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) => _execute((T)parameter);
    }
}