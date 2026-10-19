using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MAC_IDE.Core;
using MAC_IDE.Core.Interfaces;
using MAC_IDE.Core.Models;

namespace MAC_UI.ViewModels;

public partial class SolutionExplorerViewModel : ObservableObject
{
    private readonly IWorkspaceService _workspaceService;

    [ObservableProperty]
    private string _currentWorkspaceName = "Sin espacio de trabajo";

    [ObservableProperty]
    private ObservableCollection<ProjectItemNode> _workspaceNodes = new();

    public SolutionExplorerViewModel(IWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    public void LoadWorkspace(string path)
    {
        _ = LoadWorkspaceAsync(path);
    }

    public async Task LoadWorkspaceAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;

        try
        {
            // 1. EL SECRETO: Limpiar la barra final para que GetFileName no devuelva texto vacío
            string cleanPath = path.TrimEnd('\\', '/');
            CurrentWorkspaceName = Path.GetFileName(cleanPath);

            var rootNode = await _workspaceService.OpenWorkspaceFolderAsync(path);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                WorkspaceNodes.Clear();
                if (rootNode != null)
                {
                    WorkspaceNodes.Add(rootNode);
                }
            });

            // 2. Ahora sí enviamos el nombre correcto ("EjerciciosMath")
            WeakReferenceMessenger.Default.Send(new ActiveProjectChangedMessage(CurrentWorkspaceName, path));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando workspace: {ex.Message}");
        }
    }

    [RelayCommand]
    public void OpenNode(ProjectItemNode? node)
    {
        if (node != null && node.Type != ProjectItemType.Folder && !string.IsNullOrEmpty(node.FullPath))
        {
            WeakReferenceMessenger.Default.Send(new OpenFileRequestMessage(node.FullPath));
        }
    }
}