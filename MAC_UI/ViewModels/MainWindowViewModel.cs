using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dock.Model.Controls;
using Dock.Model.Core;
using MAC_IDE.Core;
using MAC_IDE.Core.Interfaces;
using MAC_IDE.Workspace;
using MAC_UI.Dock;
using MAC_UI.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MAC_UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IRecipient<OpenFileRequestMessage>
{
    private readonly IDocumentService? _documentService;
    private readonly IWorkspaceService? _workspaceService;

    // ViewModels hijos especializados
    public SolutionExplorerViewModel ExplorerViewModel { get; }
    public StatusBarViewModel StatusBarViewModel { get; }
    public MainToolbarViewModel ToolbarViewModel { get; }
    public GlobalSearchViewModel SearchViewModel { get; }
    public TopMenuBarViewModel MenuBarViewModel { get; }
    public ModuleTabsBarViewModel ModuleTabsViewModel { get; }
    public TerminalViewModel TerminalViewModel { get; }
    public OutputViewModel OutputViewModel { get; }

    // Colección de documentos abiertos en pestañas
    public ObservableCollection<CodeEditorViewModel> OpenDocuments { get; } = new();

    [ObservableProperty]
    private CodeEditorViewModel? _selectedDocument;

    [ObservableProperty]
    private GridLength _explorerColumnWidth = new GridLength(260, GridUnitType.Pixel);

    [ObservableProperty]
    private bool _isExplorerVisible = true;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    [ObservableProperty]
    private IRootDock? _layout;

    public MainDockFactory? DockFactory { get; private set; }

    private GridLength _savedColumnWidth = new GridLength(260, GridUnitType.Pixel);

    // Constructor sin parámetros (usado por el diseñador de Avalonia y App.axaml.cs)
    public MainWindowViewModel() : this(null, null)
    {
    }

    // Constructor con dependencias
    public MainWindowViewModel(IDocumentService? documentService, IWorkspaceService? workspaceService)
    {
        _documentService = documentService ?? new DefaultFallbackDocumentService();
        _workspaceService = workspaceService ?? new FileSystemWorkspaceService();

        ExplorerViewModel = new SolutionExplorerViewModel(_workspaceService);
        StatusBarViewModel = new StatusBarViewModel();
        ToolbarViewModel = new MainToolbarViewModel();
        SearchViewModel = new GlobalSearchViewModel();
        MenuBarViewModel = new TopMenuBarViewModel(ExplorerViewModel, SearchViewModel, this);
        ModuleTabsViewModel = new ModuleTabsBarViewModel();
        TerminalViewModel = new TerminalViewModel();
        OutputViewModel = new OutputViewModel();

        // 1. Inicializar Dock
        DockFactory = new MainDockFactory(this);
        Layout = DockFactory.CreateLayout();
        if (Layout != null)
        {
            DockFactory.InitLayout(Layout);
        }

        WeakReferenceMessenger.Default.Register(this);

        // 2. Documento inicial ligero para sostener el layout y evitar que la salida ocupe toda la pantalla
        var initialTab = new CodeEditorViewModel(
            string.Empty,
            "// MAC AEROESPACE Studio\n// Abre un archivo del Explorador de soluciones o arrastra una carpeta para comenzar.\n",
            _documentService)
        {
            Title = "Inicio"
        };
        OpenDocuments.Add(initialTab);
        SelectedDocument = initialTab;
        DockFactory.OpenDocument(initialTab);

        // 3. Sincronización directa del proyecto activo con la barra de herramientas
        ExplorerViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SolutionExplorerViewModel.CurrentWorkspaceName))
            {
                var wsName = ExplorerViewModel.CurrentWorkspaceName;
                if (!string.IsNullOrWhiteSpace(wsName) && wsName != "Sin espacio de trabajo")
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        ToolbarViewModel.ActiveStartupProject = wsName;
                    });
                }
            }
        };

        InitWorkspace();
    }

    private void InitWorkspace()
    {
    }

    public void SetActiveWorkspace(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return;

        string folderName = Path.GetFileName(folderPath.TrimEnd('\\', '/'));

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ToolbarViewModel.ActiveStartupProject = folderName;
            ToolbarViewModel.SetWorkingDirectory(folderPath);
        });

        // Este mensaje actualiza simultáneamente TopMenuBarViewModel y cualquier otro suscriptor
        WeakReferenceMessenger.Default.Send(new ActiveProjectChangedMessage(folderName, folderPath));

        ExplorerViewModel.LoadWorkspace(folderPath);

        OutputViewModel.AppendLog("Compilación", $"[Espacio de trabajo cargado]: {folderPath}");
    }

    public void Receive(OpenFileRequestMessage message)
    {
        _ = OpenDocumentTabAsync(message.FilePath);
    }

    public async Task OpenDocumentTabAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        var existing = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            SelectedDocument = existing;
            DockFactory?.OpenDocument(existing);
            return;
        }

        string content = string.Empty;
        if (File.Exists(filePath))
        {
            content = await File.ReadAllTextAsync(filePath);
        }

        var newDoc = new CodeEditorViewModel(filePath, content, _documentService!)
        {
            Title = Path.GetFileName(filePath)
        };

        OpenDocuments.Add(newDoc);
        SelectedDocument = newDoc;
        DockFactory?.OpenDocument(newDoc);
    }

    [RelayCommand]
    public void CloseDocumentTab(CodeEditorViewModel? tab)
    {
        tab ??= SelectedDocument;
        if (tab == null) return;

        OpenDocuments.Remove(tab);
        if (SelectedDocument == tab)
        {
            SelectedDocument = OpenDocuments.LastOrDefault();
        }
    }

    [RelayCommand]
    public void ToggleExplorer()
    {
        if (IsExplorerVisible)
        {
            _savedColumnWidth = ExplorerColumnWidth;
            ExplorerColumnWidth = new GridLength(0, GridUnitType.Pixel);
            IsExplorerVisible = false;
        }
        else
        {
            ExplorerColumnWidth = _savedColumnWidth.Value > 50 ? _savedColumnWidth : new GridLength(260, GridUnitType.Pixel);
            IsExplorerVisible = true;
        }
    }

    [RelayCommand]
    public async Task OpenFileAsync(Window? window)
    {
        if (window == null && Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            window = desktop.MainWindow;
        }

        if (window == null) return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Abrir archivo",
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            string path = files[0].Path.LocalPath;
            await OpenDocumentTabAsync(path);
        }
    }

    [RelayCommand]
    public async Task OpenFolderAsync(Window? window)
    {
        if (window == null && Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            window = desktop.MainWindow;
        }

        if (window == null) return;

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Seleccionar carpeta de solución o proyecto",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            string folderPath = folders[0].Path.LocalPath;
            SetActiveWorkspace(folderPath);
        }
    }

    [RelayCommand]
    public async Task SaveFileAsync(Window? window)
    {
        if (SelectedDocument == null) return;

        if (string.IsNullOrEmpty(SelectedDocument.FilePath) && window != null)
        {
            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar archivo como...",
                DefaultExtension = "cs"
            });

            if (file != null)
            {
                SelectedDocument.FilePath = file.Path.LocalPath;
                SelectedDocument.Title = file.Name;
            }
        }

        await SelectedDocument.SaveAsync();
    }

    [RelayCommand]
    public async Task OpenSearchWindowAsync(Window? ownerWindow)
    {
        if (ownerWindow == null) return;

        SearchViewModel.SetWorkspaceNodes(ExplorerViewModel.WorkspaceNodes);
        SearchViewModel.Open();

        var searchDialog = new Views.GlobalSearchWindow
        {
            DataContext = SearchViewModel
        };

        var selectedResult = await searchDialog.ShowDialog<SearchResultItem?>(ownerWindow);
        if (selectedResult != null)
        {
            await SelectSearchResultAsync(selectedResult);
        }
    }

    [RelayCommand]
    public async Task SelectSearchResultAsync(SearchResultItem? item)
    {
        if (item == null) return;

        SearchViewModel.Close();

        if (!string.IsNullOrEmpty(item.FullFilePath))
        {
            await OpenDocumentTabAsync(item.FullFilePath);

            if (SelectedDocument != null && item.LineNumber > 0)
            {
                SelectedDocument.UpdateCaret(item.LineNumber, 1);
            }
        }
    }

    private sealed class DefaultFallbackDocumentService : IDocumentService
    {
        public string? CurrentFilePath { get; set; }
        public bool IsDirty { get; set; }

        public Task<string> LoadDocumentAsync(string path)
        {
            CurrentFilePath = path;
            IsDirty = false;
            return File.Exists(path) ? File.ReadAllTextAsync(path) : Task.FromResult(string.Empty);
        }

        public Task SaveDocumentAsync(string path, string content)
        {
            if (!string.IsNullOrEmpty(path))
            {
                CurrentFilePath = path;
                IsDirty = false;
                return File.WriteAllTextAsync(path, content);
            }
            return Task.CompletedTask;
        }
    }
}