using Avalonia.Controls;
using Avalonia.Input;
using MAC_IDE.Core.Models;
using MAC_UI.ViewModels;

namespace MAC_UI.Views;

public partial class SolutionExplorerView : UserControl
{
    public SolutionExplorerView()
    {
        InitializeComponent();
    }

    private void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: ProjectItemNode node } && DataContext is SolutionExplorerViewModel vm)
        {
            vm.OpenNode(node);
        }
    }
}