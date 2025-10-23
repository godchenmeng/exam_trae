using ExamSystem.Domain.Entities;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ExamSystem.WPF.ViewModels
{
    public class ExamPaperEditViewModel : INotifyPropertyChanged
    {
        private readonly IExamPaperService _examPaperService;
        private ExamPaper _examPaper = new ExamPaper();
        private bool _isLoading;
        private ValidationMessages _validationMessages;

        public ExamPaperEditViewModel(IExamPaperService examPaperService)
        {
            _examPaperService = examPaperService;
            _validationMessages = new ValidationMessages();
            
            // 初始化命令
            SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
        }

        #region 属性

        public ExamPaper ExamPaper
        {
            get => _examPaper;
            set
            {
                _examPaper = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        public ValidationMessages ValidationMessages
        {
            get => _validationMessages;
            set
            {
                _validationMessages = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region 命令

        public ICommand SaveCommand { get; }

        #endregion

        #region 事件

        public event EventHandler<bool>? SaveCompleted;

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化编辑模式
        /// </summary>
        public void InitializeForEdit(ExamPaper examPaper)
        {
            ExamPaper = new ExamPaper
            {
                PaperId = examPaper.PaperId,
                Name = examPaper.Name,
                Description = examPaper.Description,
                Duration = examPaper.Duration,
                PassScore = examPaper.PassScore,
                AllowRetake = examPaper.AllowRetake,
                AllowViewAnswer = examPaper.AllowViewAnswer,
                IsRandomOrder = examPaper.IsRandomOrder,
                StartTime = examPaper.StartTime,
                EndTime = examPaper.EndTime,
                Status = examPaper.Status,
                CreatorId = examPaper.CreatorId,
                CreatedAt = examPaper.CreatedAt
            };
        }

        /// <summary>
        /// 初始化创建模式
        /// </summary>
        public void InitializeForCreate(int creatorId)
        {
            ExamPaper = new ExamPaper
            {
                Name = "",
                Description = "",
                Duration = 120, // 默认2小时
                PassScore = 60, // 默认60分及格
                AllowRetake = false,
                AllowViewAnswer = true,
                IsRandomOrder = false,
                StartTime = DateTime.Now.Date.AddDays(1), // 默认明天开始
                EndTime = DateTime.Now.Date.AddDays(7), // 默认一周后结束
                Status = "草稿",
                CreatorId = creatorId,
                CreatedAt = DateTime.Now
            };
        }

        #endregion

        #region 私有方法

        private bool CanSave()
        {
            return !IsLoading;
        }

        private async Task SaveAsync()
        {
            if (!ValidateInput())
                return;

            IsLoading = true;

            try
            {
                bool success;
                if (ExamPaper.PaperId == 0)
                {
                    // 创建新试卷
                    success = await _examPaperService.CreateExamPaperAsync(ExamPaper);
                }
                else
                {
                    // 更新现有试卷
                    success = await _examPaperService.UpdateExamPaperAsync(ExamPaper);
                }

                if (!success)
                {
                    // 服务返回失败但未抛异常，给出通用失败提示
                    ValidationMessages.General = "保存失败，请稍后重试或联系管理员。";
                }

                SaveCompleted?.Invoke(this, success);
            }
            catch (Exception ex)
            {
                // 这里可以添加日志记录
                ValidationMessages.General = $"保存失败：{ex.Message}";
                SaveCompleted?.Invoke(this, false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool ValidateInput()
        {
            ValidationMessages.Clear();
            bool isValid = true;

            // 验证试卷名称
            if (string.IsNullOrWhiteSpace(ExamPaper.Name))
            {
                ValidationMessages.Name = "试卷名称不能为空";
                isValid = false;
            }
            else if (ExamPaper.Name.Length > 100)
            {
                ValidationMessages.Name = "试卷名称不能超过100个字符";
                isValid = false;
            }

            // 验证描述长度
            if (!string.IsNullOrEmpty(ExamPaper.Description) && ExamPaper.Description.Length > 500)
            {
                ValidationMessages.Description = "试卷描述不能超过500个字符";
                isValid = false;
            }

            // 验证考试时长
            if (ExamPaper.Duration <= 0)
            {
                ValidationMessages.Duration = "考试时长必须大于0";
                isValid = false;
            }
            else if (ExamPaper.Duration > 600) // 最大10小时
            {
                ValidationMessages.Duration = "考试时长不能超过600分钟";
                isValid = false;
            }

            // 验证及格分数
            if (ExamPaper.PassScore < 0)
            {
                ValidationMessages.PassScore = "及格分数不能小于0";
                isValid = false;
            }
            else if (ExamPaper.PassScore > 100)
            {
                ValidationMessages.PassScore = "及格分数不能大于100";
                isValid = false;
            }

            // 验证时间设置
            if (ExamPaper.StartTime.HasValue && ExamPaper.EndTime.HasValue)
            {
                if (ExamPaper.StartTime >= ExamPaper.EndTime)
                {
                    ValidationMessages.EndTime = "结束时间必须晚于开始时间";
                    isValid = false;
                }
            }

            return isValid;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 验证消息类
    /// </summary>
    public class ValidationMessages : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private string _duration = string.Empty;
        private string _passScore = string.Empty;
        private string _startTime = string.Empty;
        private string _endTime = string.Empty;
        private string _general = string.Empty;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public string Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                OnPropertyChanged();
            }
        }

        public string PassScore
        {
            get => _passScore;
            set
            {
                _passScore = value;
                OnPropertyChanged();
            }
        }

        public string StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
            }
        }

        public string EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
            }
        }

        public string General
        {
            get => _general;
            set
            {
                _general = value;
                OnPropertyChanged();
            }
        }

        public void Clear()
        {
            Name = string.Empty;
            Description = string.Empty;
            Duration = string.Empty;
            PassScore = string.Empty;
            StartTime = string.Empty;
            EndTime = string.Empty;
            General = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}