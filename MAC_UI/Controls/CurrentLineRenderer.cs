using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;

namespace MAC_UI.Controls;

public class CurrentLineRenderer : IBackgroundRenderer
{
    private readonly TextEditor _editor;
    private static readonly IBrush LineBackground = new SolidColorBrush(Color.Parse("#23272E"));
    private static readonly IPen LineBorder = new Pen(new SolidColorBrush(Color.Parse("#353A42")), 1);

    public CurrentLineRenderer(TextEditor editor) => _editor = editor;

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!_editor.IsFocused || textView.VisualLinesValid != true || _editor.Document == null) return;

        var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
        var visualLine = textView.GetVisualLine(line.LineNumber);
        if (visualLine == null) return;

        var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.LineTop) - textView.VerticalOffset;
        var height = visualLine.Height;

        drawingContext.DrawRectangle(LineBackground, LineBorder, new Rect(0, y, textView.Bounds.Width, height));
    }
}