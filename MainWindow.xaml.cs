using System;
using System.Windows;
using ToyControlApp.ViewModels;

namespace ToyControlApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up resources when window closes
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.StopKeyboardHookCommand.Execute(null);
                viewModel.StopControllerHookCommand.Execute(null);
                viewModel.StopMouseHookCommand.Execute(null);
                viewModel.DisconnectCommand.Execute(null);
            }
            base.OnClosed(e);
        }
    }
}