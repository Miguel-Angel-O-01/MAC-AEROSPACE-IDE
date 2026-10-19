using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace MAC_UI.Controls;

public class StructureLineRenderer : IBackgroundRenderer
{
    private static readonly IPen GuidePen = new Pen(
        new SolidColorBrush(Color.Parse("#383838")),
        1,
        new DashStyle(new double[] { 2, 2 }, 0));

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView?.VisualLinesValid != true || textView.Document == null) return;

        var doc = textView.Document;
        var blockPairs = FindBraceBlocks(doc);

        foreach (var (openOffset, closeOffset) in blockPairs)
        {
            var openLine = doc.GetLineByOffset(openOffset);
            var closeLine = doc.GetLineByOffset(closeOffset);

            if (openLine.LineNumber >= closeLine.LineNumber) continue;

            var openVisualLine = textView.GetVisualLine(openLine.LineNumber);
            var closeVisualLine = textView.GetVisualLine(closeLine.LineNumber);

            if (openVisualLine != null || closeVisualLine != null)
            {
                double xPos = 0;
                if (closeVisualLine != null)
                {
                    int visualColumn = closeVisualLine.GetVisualColumn(closeOffset - closeLine.Offset);
                    var point = closeVisualLine.GetVisualPosition(visualColumn, VisualYPosition.LineTop);
                    xPos = point.X - textView.HorizontalOffset;
                }
                else if (openVisualLine != null)
                {
                    int visualColumn = openVisualLine.GetVisualColumn(openOffset - openLine.Offset);
                    var point = openVisualLine.GetVisualPosition(visualColumn, VisualYPosition.LineBottom);
                    xPos = point.X - textView.HorizontalOffset;
                }

                double yStart = openVisualLine != null ? openVisualLine.GetVisualPosition(0, VisualYPosition.LineBottom).Y - textView.VerticalOffset : 0;
                double yEnd = closeVisualLine != null ? (closeVisualLine.GetVisualPosition(0, VisualYPosition.LineTop).Y - textView.VerticalOffset) + (closeVisualLine.Height / 2.0) : textView.Bounds.Height;

                double lineX = Math.Floor(xPos) + 4.5;
                if (yEnd > yStart)
                {
                    drawingContext.DrawLine(GuidePen, new Point(lineX, yStart), new Point(lineX, yEnd));
                }
            }
        }
    }

    private static List<(int OpenOffset, int CloseOffset)> FindBraceBlocks(TextDocument doc)
    {
        var blocks = new List<(int, int)>();
        var stack = new Stack<int>();

        for (int i = 0; i < doc.TextLength; i++)
        {
            char c = doc.GetCharAt(i);
            if (c == '{') stack.Push(i);
            else if (c == '}' && stack.Count > 0) blocks.Add((stack.Pop(), i));
        }

        return blocks;
    }
}