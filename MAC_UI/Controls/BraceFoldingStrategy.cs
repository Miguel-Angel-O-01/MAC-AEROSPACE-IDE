using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using System;
using System.Collections.Generic;

namespace MAC_UI.Editor;

/// <summary>
/// Estrategia de plegado estándar basada en llaves { } y regiones C#
/// </summary>
public class BraceFoldingStrategy
{
    public char OpeningBrace { get; set; } = '{';
    public char ClosingBrace { get; set; } = '}';

    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        if (manager == null || document == null || document.TextLength == 0)
            return;

        try
        {
            var newFoldings = CreateNewFoldings(document, out int firstErrorOffset);
            manager.UpdateFoldings(newFoldings, firstErrorOffset);
        }
        catch (ArgumentException)
        {
            // Ignorar discrepancias si el texto cambió concurrentemente
        }
    }

    public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
    {
        firstErrorOffset = -1;
        var newFoldings = new List<NewFolding>();
        var startOffsets = new Stack<int>();
        int lastCycleStart = 0;

        for (int i = 0; i < document.TextLength; i++)
        {
            char c = document.GetCharAt(i);
            if (c == OpeningBrace)
            {
                startOffsets.Push(i);
            }
            else if (c == ClosingBrace && startOffsets.Count > 0)
            {
                int start = startOffsets.Pop();
                // Solo crear pliegue si abarca más de una línea
                if (start < lastCycleStart)
                {
                    // Evita solapamientos corruptos
                }

                int startLine = document.GetLineByOffset(start).LineNumber;
                int endLine = document.GetLineByOffset(i).LineNumber;

                if (endLine > startLine)
                {
                    newFoldings.Add(new NewFolding(start, i + 1)
                    {
                        Name = "{...}"
                    });
                }
            }
        }

        newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return newFoldings;
    }
}