using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ExamSystem.WPF
{
    public partial class TestOptionDisplay : Window, INotifyPropertyChanged
    {
        private bool _isSingleChoice;
        private bool _isMultipleChoice;
        private bool _isTrueFalse;
        
        public bool IsSingleChoice
        {
            get => _isSingleChoice;
            set
            {
                _isSingleChoice = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsMultipleChoice
        {
            get => _isMultipleChoice;
            set
            {
                _isMultipleChoice = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsTrueFalse
        {
            get => _isTrueFalse;
            set
            {
                _isTrueFalse = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<TestOption> Options { get; set; }
        
        public TestOptionDisplay()
        {
            InitializeComponent();
            DataContext = this;
            
            Options = new ObservableCollection<TestOption>();
            
            // 设置ItemsSource
            SingleChoiceOptions.ItemsSource = Options;
            MultipleChoiceOptions.ItemsSource = Options;
        }
        
        private void ShowSingleChoice_Click(object sender, RoutedEventArgs e)
        {
            ResetQuestionType();
            IsSingleChoice = true;
            
            Options.Clear();
            Options.Add(new TestOption { Text = "A. 选项A - 这是第一个选项", IsSelected = false });
            Options.Add(new TestOption { Text = "B. 选项B - 这是第二个选项", IsSelected = false });
            Options.Add(new TestOption { Text = "C. 选项C - 这是第三个选项", IsSelected = false });
            Options.Add(new TestOption { Text = "D. 选项D - 这是第四个选项", IsSelected = false });
            
            txtQuestionType.Text = "单选题";
            txtOptionCount.Text = Options.Count.ToString();
        }
        
        private void ShowMultipleChoice_Click(object sender, RoutedEventArgs e)
        {
            ResetQuestionType();
            IsMultipleChoice = true;
            
            Options.Clear();
            Options.Add(new TestOption { Text = "A. 多选选项A", IsSelected = false });
            Options.Add(new TestOption { Text = "B. 多选选项B", IsSelected = false });
            Options.Add(new TestOption { Text = "C. 多选选项C", IsSelected = false });
            Options.Add(new TestOption { Text = "D. 多选选项D", IsSelected = false });
            Options.Add(new TestOption { Text = "E. 多选选项E", IsSelected = false });
            
            txtQuestionType.Text = "多选题";
            txtOptionCount.Text = Options.Count.ToString();
        }
        
        private void ShowTrueFalse_Click(object sender, RoutedEventArgs e)
        {
            ResetQuestionType();
            IsTrueFalse = true;
            
            Options.Clear();
            
            txtQuestionType.Text = "判断题";
            txtOptionCount.Text = "2";
        }
        
        private void ResetQuestionType()
        {
            IsSingleChoice = false;
            IsMultipleChoice = false;
            IsTrueFalse = false;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class TestOption : INotifyPropertyChanged
    {
        private string _text = string.Empty;
        private bool _isSelected;
        
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}