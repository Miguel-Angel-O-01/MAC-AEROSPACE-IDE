using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MAC_UI.Models;

public partial class DocumentTabItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _title = "Nuevo Documento";

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _hasUnsavedChanges = false;

    public DocumentTabItem(string filePath, string content)
    {
        FilePath = filePath;
        Content = content;
        Title = Path.GetFileName(filePath);
    }
}