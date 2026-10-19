using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using MAC_UI.Controls;
using MAC_UI.ViewModels;

namespace MAC_UI.Views;

public partial class CodeEditorView : UserControl
{
    private readonly TextEditor? _editor;
    private readonly ChangeTrackingMargin? _trackingMargin;
    private FoldingManager? _foldingManager;

    public CodeEditorView()
    {
        InitializeComponent();
        _editor = this.FindControl<TextEditor>("Editor");

        if (_editor != null)
        {
            // 1. Inicialización visual mediante extensiones modulares
            _trackingMargin = _editor.SetupIDEVisualEnhancements();
            _foldingManager = _editor.EnableCodeFolding();
            _editor.AttachIDEContextMenu(() => DataContext as CodeEditorViewModel);
            _editor.EnablePointerZoom(newSize => (DataContext as CodeEditorViewModel)?.UpdateZoom(newSize));

            // 2. Sincronización del cursor y el minimapa
            _editor.TextArea.TextView.ScrollOffsetChanged += (s, e) => UpdateMinimapSlider();
            _editor.TextArea.KeyDown += OnEditorKeyDown;

            _editor.TextArea.Caret.PositionChanged += (s, e) =>
            {
                if (DataContext is CodeEditorViewModel vm)
                    vm.UpdateCaret(_editor.TextArea.Caret.Line, _editor.TextArea.Caret.Column);
            };

            _editor.TextChanged += (s, e) =>
            {
                if (DataContext is CodeEditorViewModel vm && vm.HasUnsavedChanges)
                {
                    _trackingMargin?.MarkLineChanged(_editor.TextArea.Caret.Line);
                }
                UpdateMinimapSlider();
            };
        }

        // 3. Conexión de diálogos y reactividad del ViewModel
        DataContextChanged += (s, e) =>
        {
            if (DataContext is CodeEditorViewModel vm && _editor != null)
            {
                _trackingMargin?.ResetTracking();

                vm.RequestRenameDialog = async (currentName) =>
                {
                    if (TopLevel.GetTopLevel(this) is Window parent)
                    {
                        var dialog = new RenameDialogWindow(currentName);
                        return await dialog.ShowDialog<string?>(parent);
                    }
                    return null;
                };

                vm.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(CodeEditorViewModel.HasUnsavedChanges) && !vm.HasUnsavedChanges)
                    {
                        _trackingMargin?.ResetTracking();
                    }
                };
            }
        };

        SetupMinimapEvents();
    }

    private void SetupMinimapEvents()
    {
        var minimap = this.FindControl<Panel>("MinimapContainer");
        if (minimap == null) return;

        minimap.PointerMoved += (s, e) =>
        {
            if (DataContext is CodeEditorViewModel vm)
            {
                var pos = e.GetPosition(minimap);
                vm.CalculateMinimapPreview(pos.Y, minimap.Bounds.Height);
            }
        };

        minimap.PointerExited += (s, e) =>
        {
            if (DataContext is CodeEditorViewModel vm) vm.HideMinimapPreview();
        };

        minimap.PointerPressed += (s, e) =>
        {
            if (_editor?.Document == null) return;
            var pos = e.GetPosition(minimap);
            double ratio = Math.Clamp(pos.Y / minimap.Bounds.Height, 0.0, 1.0);
            int targetLine = Math.Clamp((int)(ratio * _editor.Document.LineCount) + 1, 1, _editor.Document.LineCount);

            _editor.ScrollToLine(targetLine);
            _editor.TextArea.Caret.Line = targetLine;
            _editor.Focus();
            UpdateMinimapSlider();
        };
    }

    private void UpdateMinimapSlider()
    {
        if (_editor == null || _editor.Document == null) return;

        var slider = this.FindControl<Border>("MinimapSlider");
        var container = this.FindControl<Panel>("MinimapContainer");
        if (slider == null || container == null) return;

        double totalHeight = _editor.ExtentHeight;
        double viewportHeight = _editor.ViewportHeight;
        double currentScroll = _editor.VerticalOffset;
        double minimapHeight = container.Bounds.Height;

        if (totalHeight > 0 && minimapHeight > 0)
        {
            double sliderHeight = Math.Clamp((viewportHeight / totalHeight) * minimapHeight, 35.0, minimapHeight);
            slider.Height = sliderHeight;

            double scrollRatio = currentScroll / Math.Max(1, totalHeight - viewportHeight);
            double maxSliderY = Math.Max(0, minimapHeight - sliderHeight);
            double targetY = Math.Clamp(scrollRatio * maxSliderY, 0, maxSliderY);

            slider.Margin = new Thickness(1, targetY, 1, 0);
        }
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2 && DataContext is CodeEditorViewModel vm && _editor != null)
        {
            _ = vm.RenameSymbolAsync(_editor.CaretOffset);
            e.Handled = true;
        }
    }
}