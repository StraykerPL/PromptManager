using Avalonia.Controls;
using Avalonia.Input;
using PromptManager.UI.ViewModels;
using PromptManager.Models;

namespace PromptManager.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closed += (_, _) =>
            {
                if (DataContext is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }

        private void OnTreeNodeKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter &&
                sender is ListBox { SelectedItem: PromptTreeNode node } &&
                DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SelectTreeNode(node);
                e.Handled = true;
            }
        }

        private void OnTreeNodePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
                sender is Control { DataContext: PromptTreeNode node } &&
                DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SelectTreeNode(node);
            }
        }
    }
}
