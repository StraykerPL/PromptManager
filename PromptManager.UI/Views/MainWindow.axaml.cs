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

        private void OnTreeNodePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control { DataContext: PromptTreeNode node } &&
                DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SelectTreeNode(node);
            }
        }
    }
}
