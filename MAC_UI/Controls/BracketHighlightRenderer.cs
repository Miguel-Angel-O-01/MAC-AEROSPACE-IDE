using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace MAC_UI.Controls;

public class BracketHighlightRenderer : IBackgroundRenderer
{
    private static readonly IBrush BracketBrush = new SolidColorBrush(Color.Parse("#3E3E42"));
    private static readonly IPen BracketPen = new Pen(new SolidColorBrush(Color.Parse("#0E639C")), 1);

    public KnownLayer Layer => KnownLayer.Selection;
    public int? OpenOffset { get; set; }
    public int? CloseOffset { get; set; }

    public void SetMatch(int? open, int? close)
    {
        OpenOffset = open;
        CloseOffset = close;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (OpenOffset.HasValue) DrawBracket(textView, drawingContext, OpenOffset.Value);
        if (CloseOffset.HasValue) DrawBracket(textView, drawingContext, CloseOffset.Value);
    }

    private void DrawBracket(TextView textView, DrawingContext context, int offset)
    {
        if (offset < 0 || offset >= textView.Document.TextLength) return;

        var segment = new TextSegment { StartOffset = offset, Length = 1 };
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
        {
            context.DrawRectangle(BracketBrush, BracketPen, rect);
        }
    }
}