using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MAC_IDE.Core;

namespace MAC_UI.ViewModels;

public record ToggleExplorerMessage();

public partial class TopMenuBarViewModel : ObservableObject, IRecipient<ActiveProjectChangedMessage>
{
    private readonly SolutionExplorerViewModel _explorerViewModel;
    private readonly GlobalSearchViewModel _searchViewModel;
    private readonly MainWindowViewModel _mainViewModel;

    [ObservableProperty]
    private string _currentWorkspaceName = "Sin espacio de trabajo";

    public SolutionExplorerViewModel ExplorerViewModel => _explorerViewModel;

    public TopMenuBarViewModel(
        SolutionExplorerViewModel explorerViewModel,
        GlobalSearchViewModel searchViewModel,
        MainWindowViewModel mainViewModel)
    {
        _explorerViewModel = explorerViewModel;
        _searchViewModel = searchViewModel;
        _mainViewModel = mainViewModel;

        // Registrar recepción de mensajes del bus
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(ActiveProjectChangedMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            CurrentWorkspaceName = message.ProjectName;
        });
    }

    [RelayCommand]
    public async Task OpenFileAsync(Window? window) => await _mainViewModel.OpenFileAsync(window);

    [RelayCommand]
    public async Task OpenFolderAsync(Window? window) => await _mainViewModel.OpenFolderAsync(window);

    [RelayCommand]
    public async Task SaveFileAsync(Window? window) => await _mainViewModel.SaveFileAsync(window);

    [RelayCommand]
    public void ToggleExplorer() => _mainViewModel.ToggleExplorer();

    [RelayCommand]
    public async Task OpenSearchWindowAsync(Window? ownerWindow)
    {
        if (ownerWindow == null) return;

        _searchViewModel.SetWorkspaceNodes(_explorerViewModel.WorkspaceNodes);
        _searchViewModel.Open();

        var searchDialog = new Views.GlobalSearchWindow
        {
            DataContext = _searchViewModel
        };

        var selectedResult = await searchDialog.ShowDialog<Models.SearchResultItem?>(ownerWindow);
        if (selectedResult != null)
        {
            await _mainViewModel.SelectSearchResultAsync(selectedResult);
        }
    }

    [RelayCommand]
    public void ExitApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}