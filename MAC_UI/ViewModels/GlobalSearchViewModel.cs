using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAC_IDE.Core.Models;
using MAC_UI.Models;

namespace MAC_UI.ViewModels;

public partial class GlobalSearchViewModel : ObservableObject
{
    private CancellationTokenSource? _searchCts;
    private ObservableCollection<ProjectItemNode>? _workspaceNodes;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private int _selectedTabIndex = 0; // 0 = Código, 1 = Funciones

    [ObservableProperty]
    private string _activeFilter = "all"; // all, f, t, m, x

    [ObservableProperty]
    private string _selectedScope = "Toda la solución";

    [ObservableProperty]
    private SearchResultItem? _selectedItem;

    public ObservableCollection<SearchResultItem> Results { get; } = new();

    // Evento para que la vista cierre la ventana sin que el ViewModel toque la API de ventanas
    public event Action<SearchResultItem?>? RequestClose;

    public void SetWorkspaceNodes(ObservableCollection<ProjectItemNode> nodes)
    {
        _workspaceNodes = nodes;
    }

    partial void OnSearchQueryChanged(string value) => DebouncedSearch();
    partial void OnSelectedTabIndexChanged(int value) => DebouncedSearch();
    partial void OnActiveFilterChanged(string value) => DebouncedSearch();

    [RelayCommand]
    public void SelectCodeTab() => SelectedTabIndex = 0;

    [RelayCommand]
    public void SelectFunctionsTab() => SelectedTabIndex = 1;

    [RelayCommand]
    public void SetFilter(string filter)
    {
        ActiveFilter = (ActiveFilter == filter) ? "all" : filter;
    }

    [RelayCommand]
    public void ConfirmSelection(SearchResultItem? item)
    {
        item ??= SelectedItem;
        if (item != null)
        {
            RequestClose?.Invoke(item);
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        RequestClose?.Invoke(null);
    }

    private void DebouncedSearch()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            await Task.Delay(150, token);
            if (!token.IsCancellationRequested)
            {
                await ExecuteSearchAsync(SearchQuery, token);
            }
        }, token);
    }

    private async Task ExecuteSearchAsync(string query, CancellationToken token)
    {
        var localResults = new ObservableCollection<SearchResultItem>();

        if (string.IsNullOrWhiteSpace(query))
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Results.Clear();
            });
            return;
        }

        if (SelectedTabIndex == 0)
        {
            if (_workspaceNodes != null)
            {
                foreach (var node in _workspaceNodes)
                {
                    ScanNode(node, query, localResults, token);
                    if (token.IsCancellationRequested) return;
                }
            }
        }
        else
        {
            var ideCommands = new[]
            {
                new SearchResultItem { Title = "Compilar solución", Subtitle = "Compilar > Compilar solución (Ctrl+Shift+B)", Kind = SearchResultKind.IdeCommand },
                new SearchResultItem { Title = "Guardar archivo actual", Subtitle = "Archivo > Guardar (Ctrl+S)", Kind = SearchResultKind.IdeCommand },
                new SearchResultItem { Title = "Alternar Explorador de Soluciones", Subtitle = "Ver > Explorador (Ctrl+B)", Kind = SearchResultKind.IdeCommand },
                new SearchResultItem { Title = "Dar formato al documento", Subtitle = "Formato > Formato al documento (Alt+Shift+F)", Kind = SearchResultKind.IdeCommand },
                new SearchResultItem { Title = "Cambiar nombre (Renombrar)", Subtitle = "Editar > Refactorizar > Renombrar (F2)", Kind = SearchResultKind.IdeCommand }
            };

            foreach (var cmd in ideCommands.Where(c => c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                                      c.Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                localResults.Add(cmd);
            }
        }

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            Results.Clear();
            foreach (var item in localResults.Take(40))
            {
                Results.Add(item);
            }
        });
    }

    private void ScanNode(ProjectItemNode node, string query, ObservableCollection<SearchResultItem> list, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        if (node.Type == ProjectItemType.File)
        {
            if (ActiveFilter is "all" or "f" && node.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new SearchResultItem
                {
                    Title = node.Name,
                    Subtitle = Path.GetDirectoryName(node.FullPath) ?? string.Empty,
                    LineInfo = "Archivo",
                    LineNumber = 1,
                    FullFilePath = node.FullPath,
                    Kind = SearchResultKind.File
                });
            }

            if (ActiveFilter is "all" or "x" or "t" or "m" && File.Exists(node.FullPath))
            {
                try
                {
                    var lines = File.ReadLines(node.FullPath).Take(400);
                    int lineIdx = 1;
                    foreach (var line in lines)
                    {
                        if (line.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(new SearchResultItem
                            {
                                Title = line.Trim(),
                                Subtitle = node.FullPath,
                                LineInfo = $"Línea {lineIdx}",
                                LineNumber = lineIdx,
                                FullFilePath = node.FullPath,
                                Kind = SearchResultKind.Code
                            });

                            if (list.Count >= 40) return;
                        }
                        lineIdx++;
                    }
                }
                catch { }
            }
        }

        foreach (var child in node.Children)
        {
            ScanNode(child, query, list, token);
        }
    }

    public void Open()
    {
        SearchQuery = string.Empty;
        Results.Clear();
        SelectedItem = null;
    }

    public void Close()
    {
        SearchQuery = string.Empty;
        Results.Clear();
        SelectedItem = null;
    }
}