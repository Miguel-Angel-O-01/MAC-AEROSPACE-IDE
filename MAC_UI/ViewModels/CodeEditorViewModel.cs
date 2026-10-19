using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dock.Model.Mvvm.Controls;
using MAC_IDE.Core;
using MAC_IDE.Core.Interfaces;

namespace MAC_UI.ViewModels;

// 1. DEFINICIÓN DEL ENUM (Esto resuelve los errores CS0246 y CS0103)
public enum EditorSplitMode
{
    CodeOnly,
    SplitHorizontal,
    SplitVertical,
    PreviewOnly
}

// 2. CLASE DEL VIEWMODEL (Hereda de Document para sincronizar directamente con Dock.Avalonia)
public partial class CodeEditorViewModel : Document
{
    private readonly IDocumentService _documentService;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private bool _hasUnsavedChanges = false;

    // Telemetría
    [ObservableProperty]
    private string _caretTelemetry = "Línea: 1, Carácter: 1";

    [ObservableProperty]
    private string _zoomPercentage = "100%";

    [ObservableProperty]
    private string _lineEnding = "CRLF";

    [ObservableProperty]
    private string _encoding = "UTF-8";

    // Minimapa
    [ObservableProperty]
    private bool _isMinimapPreviewVisible = false;

    [ObservableProperty]
    private string _minimapPreviewTitle = string.Empty;

    [ObservableProperty]
    private string _minimapPreviewText = string.Empty;

    [ObservableProperty]
    private double _minimapPreviewY = 0;

    // Modos de visualización / Split View
    [ObservableProperty]
    private EditorSplitMode _currentSplitMode = EditorSplitMode.CodeOnly;

    [ObservableProperty]
    private bool _isCodeVisible = true;

    [ObservableProperty]
    private bool _isPreviewVisible = false;

    public TextDocument Document { get; }

    [ObservableProperty]
    private GridLength _previewColumnWidth = new GridLength(0, GridUnitType.Pixel);

    public Func<string, Task<string?>>? RequestRenameDialog { get; set; }

    public CodeEditorViewModel(string filePath, string initialContent, IDocumentService documentService)
    {
        _documentService = documentService;
        FilePath = filePath;

        Id = string.IsNullOrEmpty(filePath) ? Guid.NewGuid().ToString() : filePath;
        CanClose = true;
        CanFloat = true;

        Document = new TextDocument(initialContent);

        // Título limpio inicial
        string baseName = string.IsNullOrEmpty(filePath) ? "Bienvenido.cs" : Path.GetFileName(filePath);
        Title = baseName;
        HasUnsavedChanges = false;

        // Detectar modificaciones del usuario
        Document.TextChanged += (s, e) =>
        {
            if (!HasUnsavedChanges)
            {
                HasUnsavedChanges = true;
            }
        };
    }

    partial void OnHasUnsavedChangesChanged(bool value)
    {
        UpdateTabTitle();
    }

    private void UpdateTabTitle()
    {
        string baseName = string.IsNullOrEmpty(FilePath) ? "Bienvenido.cs" : Path.GetFileName(FilePath);
        Title = HasUnsavedChanges ? $"{baseName}*" : baseName;
    }

    [RelayCommand]
    public void SetSplitMode(string mode)
    {
        CurrentSplitMode = mode switch
        {
            "CodeOnly" => EditorSplitMode.CodeOnly,
            "SplitHorizontal" => EditorSplitMode.SplitHorizontal,
            "SplitVertical" => EditorSplitMode.SplitVertical,
            "PreviewOnly" => EditorSplitMode.PreviewOnly,
            _ => EditorSplitMode.CodeOnly
        };

        switch (CurrentSplitMode)
        {
            case EditorSplitMode.CodeOnly:
                IsCodeVisible = true;
                IsPreviewVisible = false;
                PreviewColumnWidth = new GridLength(0, GridUnitType.Pixel);
                break;

            case EditorSplitMode.SplitHorizontal:
            case EditorSplitMode.SplitVertical:
                IsCodeVisible = true;
                IsPreviewVisible = true;
                PreviewColumnWidth = new GridLength(1, GridUnitType.Star);
                break;

            case EditorSplitMode.PreviewOnly:
                IsCodeVisible = false;
                IsPreviewVisible = true;
                PreviewColumnWidth = new GridLength(1, GridUnitType.Star);
                break;
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        await _documentService.SaveDocumentAsync(FilePath, Document.Text);
        HasUnsavedChanges = false;

        WeakReferenceMessenger.Default.Send(new StatusUpdateMessage($"{Title} guardado", ResetDelayMs: 3000));
    }

    public void UpdateCaret(int line, int col) => CaretTelemetry = $"Línea: {line}, Carácter: {col}";

    public void UpdateZoom(double currentFontSize, double baseSize = 15.0)
    {
        int percent = (int)Math.Round((currentFontSize / baseSize) * 100);
        ZoomPercentage = $"{percent}%";
    }

    public void CalculateMinimapPreview(double relativeY, double panelHeight)
    {
        if (panelHeight <= 0 || Document == null) return;

        double ratio = Math.Clamp(relativeY / panelHeight, 0.0, 1.0);
        int totalLines = Document.LineCount;
        int targetLine = Math.Clamp((int)(ratio * totalLines) + 1, 1, totalLines);

        int startLine = Math.Max(1, targetLine - 3);
        int endLine = Math.Min(totalLines, targetLine + 4);

        var sb = new StringBuilder();
        for (int i = startLine; i <= endLine; i++)
        {
            var lineSeg = Document.GetLineByNumber(i);
            string marker = (i == targetLine) ? "► " : "  ";
            sb.AppendLine($"{marker}{i,3}: {Document.GetText(lineSeg)}");
        }

        MinimapPreviewTitle = $"Vista previa — Línea {targetLine} de {totalLines}";
        MinimapPreviewText = sb.ToString();
        MinimapPreviewY = Math.Clamp(relativeY - 70, 8, Math.Max(8, panelHeight - 170));
        IsMinimapPreviewVisible = true;
    }

    public void HideMinimapPreview() => IsMinimapPreviewVisible = false;

    [RelayCommand]
    public async Task RenameSymbolAsync(int caretOffset)
    {
        if (caretOffset < 0 || caretOffset > Document.TextLength) return;

        int start = caretOffset;
        while (start > 0 && (char.IsLetterOrDigit(Document.GetCharAt(start - 1)) || Document.GetCharAt(start - 1) == '_'))
            start--;

        int end = caretOffset;
        while (end < Document.TextLength && (char.IsLetterOrDigit(Document.GetCharAt(end)) || Document.GetCharAt(end) == '_'))
            end++;

        if (start == end || RequestRenameDialog == null) return;
        string currentName = Document.GetText(start, end - start);

        string? newName = await RequestRenameDialog(currentName);
        if (string.IsNullOrWhiteSpace(newName) || newName == currentName) return;

        using (Document.RunUpdate())
        {
            string pattern = $@"\b{Regex.Escape(currentName)}\b";
            Document.Text = Regex.Replace(Document.Text, pattern, newName);
        }
    }
}