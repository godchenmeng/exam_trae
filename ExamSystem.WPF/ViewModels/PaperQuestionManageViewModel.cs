using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ExamSystem.Domain.Entities;
using ExamSystem.Domain.Enums;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.Commands;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.ViewModels
{
    public class PaperQuestionManageViewModel : INotifyPropertyChanged
    {
        private readonly IExamPaperService _examPaperService;
        private readonly IQuestionBankService _questionBankService;
        private readonly IQuestionService _questionService;
        private readonly ILogger<PaperQuestionManageViewModel> _logger;
        
        private ExamPaper? _examPaper;
        private bool _isLoading;
        private string _validationMessage = string.Empty;
        private QuestionBank? _selectedQuestionBank;
        private Question? _selectedAvailableQuestion;
        private PaperQuestion? _selectedPaperQuestion;
        private string _searchKeyword = string.Empty;
        private Difficulty? _selectedDifficulty;
        private QuestionType? _selectedQuestionType;
        
        public PaperQuestionManageViewModel(
            IExamPaperService examPaperService,
            IQuestionBankService questionBankService,
            IQuestionService questionService,
            ILogger<PaperQuestionManageViewModel> logger)
        {
            _examPaperService = examPaperService;
            _questionBankService = questionBankService;
            _questionService = questionService;
            _logger = logger;
            
            QuestionBanks = new ObservableCollection<QuestionBank>();
            AvailableQuestions = new ObservableCollection<Question>();
            PaperQuestions = new ObservableCollection<PaperQuestion>();
            
            InitializeCommands();
            InitializeOptions();
        }
        
        #region Properties
        
        public string ExamPaperName => _examPaper?.Name ?? "";
        
        public decimal TotalScore => PaperQuestions.Sum(pq => pq.Score);
        
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
        
        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                _validationMessage = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<QuestionBank> QuestionBanks { get; } = new();
        
        public QuestionBank? SelectedQuestionBank
        {
            get => _selectedQuestionBank;
            set
            {
                _selectedQuestionBank = value;
                OnPropertyChanged();
                _ = LoadAvailableQuestionsAsync();
            }
        }
        
        public ObservableCollection<Question> AvailableQuestions { get; } = new();
        
        public Question? SelectedAvailableQuestion
        {
            get => _selectedAvailableQuestion;
            set
            {
                _selectedAvailableQuestion = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<PaperQuestion> PaperQuestions { get; } = new();
        
        public PaperQuestion? SelectedPaperQuestion
        {
            get => _selectedPaperQuestion;
            set
            {
                _selectedPaperQuestion = value;
                OnPropertyChanged();
            }
        }
        
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                _searchKeyword = value;
                OnPropertyChanged();
                _ = LoadAvailableQuestionsAsync();
            }
        }
        
        public Difficulty? SelectedDifficulty
        {
            get => _selectedDifficulty;
            set
            {
                _selectedDifficulty = value;
                OnPropertyChanged();
                _ = LoadAvailableQuestionsAsync();
            }
        }
        
        public QuestionType? SelectedQuestionType
        {
            get => _selectedQuestionType;
            set
            {
                _selectedQuestionType = value;
                OnPropertyChanged();
                _ = LoadAvailableQuestionsAsync();
            }
        }
        
        public List<Difficulty?> DifficultyOptions { get; private set; } = new();
        public List<QuestionType?> QuestionTypeOptions { get; private set; } = new();
        
        #endregion
        
        #region Commands
        
        public ICommand AddQuestionCommand { get; private set; } = null!;
        public ICommand RemoveQuestionCommand { get; private set; } = null!;
        public ICommand MoveUpCommand { get; private set; } = null!;
        public ICommand MoveDownCommand { get; private set; } = null!;
        public ICommand SaveCommand { get; private set; } = null!;
        
        #endregion
        
        #region Events
        
        public event EventHandler? SaveCompleted;
        
        #endregion
        
        #region Public Methods
        
        public async Task InitializeAsync(int examPaperId)
        {
            try
            {
                IsLoading = true;
                ValidationMessage = "";
                
                // 加载试卷信息
                var examPaper = await _examPaperService.GetExamPaperByIdAsync(examPaperId);
                if (examPaper == null)
                {
                    ValidationMessage = "试卷不存在";
                    return;
                }
                _examPaper = examPaper;
                OnPropertyChanged(nameof(ExamPaperName));
                
                // 加载题库列表
                await LoadQuestionBanksAsync();
                
                // 加载试卷题目
                await LoadPaperQuestionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化试卷题目管理失败");
                ValidationMessage = $"初始化失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeCommands()
        {
            AddQuestionCommand = new RelayCommand(
                async () => await AddQuestionAsync(),
                () => SelectedAvailableQuestion != null && !IsQuestionInPaper(SelectedAvailableQuestion.QuestionId));
                
            RemoveQuestionCommand = new RelayCommand(
                async () => await RemoveQuestionAsync(),
                () => SelectedPaperQuestion != null);
                
            MoveUpCommand = new RelayCommand(
                async () => await MoveQuestionUpAsync(),
                () => SelectedPaperQuestion != null && SelectedPaperQuestion.OrderIndex > 1);
                
            MoveDownCommand = new RelayCommand(
                async () => await MoveQuestionDownAsync(),
                () => SelectedPaperQuestion != null && SelectedPaperQuestion.OrderIndex < PaperQuestions.Count);
                
            SaveCommand = new RelayCommand(
                async () => await SaveAsync(),
                () => !IsLoading);
        }
        
        private void InitializeOptions()
        {
            DifficultyOptions = new List<Difficulty?>
            {
                null, // 全部
                Difficulty.Easy,
                Difficulty.Medium,
                Difficulty.Hard
            };
            
            QuestionTypeOptions = new List<QuestionType?>
            {
                null, // 全部
                QuestionType.SingleChoice,
                QuestionType.MultipleChoice,
                QuestionType.TrueFalse,
                QuestionType.FillInBlank,
                QuestionType.Essay
            };
        }
        
        private async Task LoadQuestionBanksAsync()
        {
            try
            {
                var questionBanks = await _questionBankService.GetAllQuestionBanksAsync();
                QuestionBanks.Clear();
                foreach (var bank in questionBanks)
                {
                    QuestionBanks.Add(bank);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载题库列表失败");
                ValidationMessage = $"加载题库失败: {ex.Message}";
            }
        }
        
        private async Task LoadAvailableQuestionsAsync()
        {
            if (SelectedQuestionBank == null)
            {
                AvailableQuestions.Clear();
                return;
            }
            
            try
            {
                var questions = await _questionService.SearchAsync(
                    SelectedQuestionBank.BankId,
                    SearchKeyword,
                    SelectedQuestionType,
                    SelectedDifficulty);
                
                AvailableQuestions.Clear();
                foreach (var question in questions)
                {
                    AvailableQuestions.Add(question);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载可选题目失败");
                ValidationMessage = $"加载题目失败: {ex.Message}";
            }
        }
        
        private async Task LoadPaperQuestionsAsync()
        {
            try
            {
                if (_examPaper == null)
                {
                    ValidationMessage = "试卷未加载";
                    return;
                }
                var paperQuestions = await _examPaperService.GetPaperQuestionsAsync(_examPaper.PaperId);
                PaperQuestions.Clear();
                foreach (var pq in paperQuestions.OrderBy(x => x.OrderIndex))
                {
                    PaperQuestions.Add(pq);
                }
                OnPropertyChanged(nameof(TotalScore));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载试卷题目失败");
                ValidationMessage = $"加载试卷题目失败: {ex.Message}";
            }
        }
        
        private async Task AddQuestionAsync()
        {
            if (SelectedAvailableQuestion == null || IsQuestionInPaper(SelectedAvailableQuestion.QuestionId))
                return;
            
            try
            {
                IsLoading = true;
                ValidationMessage = "";
                
                if (_examPaper == null)
                {
                    ValidationMessage = "试卷未加载";
                    return;
                }
                var nextOrder = PaperQuestions.Count + 1;
                await _examPaperService.AddQuestionAsync(
                    _examPaper.PaperId, 
                    SelectedAvailableQuestion.QuestionId, 
                    nextOrder, 
                    SelectedAvailableQuestion.Score);
                
                await LoadPaperQuestionsAsync();
                
                // 刷新命令状态
                ((RelayCommand)AddQuestionCommand).RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加题目失败");
                ValidationMessage = $"添加题目失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task RemoveQuestionAsync()
        {
            if (SelectedPaperQuestion == null)
                return;
            
            try
            {
                IsLoading = true;
                ValidationMessage = "";
                
                if (_examPaper == null)
                {
                    ValidationMessage = "试卷未加载";
                    return;
                }
                await _examPaperService.RemoveQuestionAsync(_examPaper.PaperId, SelectedPaperQuestion.QuestionId);
                await LoadPaperQuestionsAsync();
                
                // 刷新命令状态
                ((RelayCommand)AddQuestionCommand).RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除题目失败");
                ValidationMessage = $"移除题目失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task MoveQuestionUpAsync()
        {
            if (SelectedPaperQuestion == null || SelectedPaperQuestion.OrderIndex <= 1)
                return;
            
            try
            {
                IsLoading = true;
                ValidationMessage = "";
                
                if (_examPaper == null)
                {
                    ValidationMessage = "试卷未加载";
                    return;
                }
                var currentIndex = SelectedPaperQuestion.OrderIndex;
                var targetIndex = currentIndex - 1;
                
                await _examPaperService.UpdateQuestionOrderAsync(
                    _examPaper.PaperId, 
                    SelectedPaperQuestion.QuestionId, 
                    targetIndex);
                
                await LoadPaperQuestionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移动题目失败");
                ValidationMessage = $"移动题目失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task MoveQuestionDownAsync()
        {
            if (SelectedPaperQuestion == null || SelectedPaperQuestion.OrderIndex >= PaperQuestions.Count)
                return;
            
            try
            {
                IsLoading = true;
                ValidationMessage = "";
                
                if (_examPaper == null)
                {
                    ValidationMessage = "试卷未加载";
                    return;
                }
                var currentIndex = SelectedPaperQuestion.OrderIndex;
                var targetIndex = currentIndex + 1;
                
                await _examPaperService.UpdateQuestionOrderAsync(
                    _examPaper.PaperId, 
                    SelectedPaperQuestion.QuestionId, 
                    targetIndex);
                
                await LoadPaperQuestionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移动题目失败");
                ValidationMessage = $"移动题目失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;
                ValidationMessage = "";
                
                if (_examPaper == null)
                {
                    ValidationMessage = "试卷未加载";
                    return;
                }
                // 更新所有题目的分值
                foreach (var paperQuestion in PaperQuestions)
                {
                    await _examPaperService.UpdateQuestionScoreAsync(
                        _examPaper.PaperId, 
                        paperQuestion.QuestionId, 
                        paperQuestion.Score);
                }
                
                SaveCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存失败");
                ValidationMessage = $"保存失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private bool IsQuestionInPaper(int questionId)
        {
            return PaperQuestions.Any(pq => pq.QuestionId == questionId);
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
}