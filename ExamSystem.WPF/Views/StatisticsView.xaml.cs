using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ExamSystem.Services.Interfaces;
using ExamSystem.WPF.ViewModels;
using Microsoft.Extensions.Logging;

namespace ExamSystem.WPF.Views
{
    public partial class StatisticsView : UserControl
    {
        public StatisticsView()
        {
            InitializeComponent();
        }

        public StatisticsView(StatisticsViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}