using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace MAC_UI.Controls;

public class ChangeTrackingMargin : AbstractMargin
{
    private readonly HashSet<int> _unsavedLines = new();

    private static readonly IBrush UnsavedBrush = new SolidColorBrush(Color.Parse("#D08770"));

    public ChangeTrackingMargin() => Width = 4;

    public void MarkLineChanged(int lineNumber)
    {
        _unsavedLines.Add(lineNumber);
        InvalidateVisual();
    }

    public void ResetTracking()
    {
        _unsavedLines.Clear();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (TextView?.VisualLinesValid != true) return;

        foreach (var visualLine in TextView.VisualLines)
        {
            int lineNumber = visualLine.FirstDocumentLine.LineNumber;
            if (_unsavedLines.Contains(lineNumber))
            {
                double y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop) - TextView.VerticalOffset;
                context.FillRectangle(UnsavedBrush, new Rect(0, y, Width, visualLine.Height));
            }
        }
    }
}