using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using MAC_UI.Editor;
using MAC_UI.ViewModels;
using System;

namespace MAC_UI.Controls;

public static class TextEditorExtensions
{
    /// <summary>
    /// Configura los renderizadores gráficos base de Visual Studio 2026.
    /// </summary>
    public static ChangeTrackingMargin SetupIDEVisualEnhancements(this TextEditor editor)
    {
        editor.Options.HighlightCurrentLine = false;

        var trackingMargin = new ChangeTrackingMargin();
        editor.TextArea.LeftMargins.Insert(0, trackingMargin);

        editor.TextArea.TextView.BackgroundRenderers.Add(new StructureLineRenderer());
        editor.TextArea.TextView.BackgroundRenderers.Add(new CurrentLineRenderer(editor));
        editor.TextArea.TextView.BackgroundRenderers.Add(new BracketHighlightRenderer());

        return trackingMargin;
    }

    /// <summary>
    /// Activa el sistema de Code Folding (plegado de bloques {...}).
    /// </summary>
    /// <summary>
    /// Activa el sistema de Code Folding (plegado de bloques {...}) de forma segura.
    /// </summary>
    /// <summary>
    /// Activa el sistema de Code Folding y personaliza los colores de los botones [+] y [-].
    /// </summary>
    public static FoldingManager? EnableCodeFolding(this TextEditor editor)
    {
        FoldingManager? foldingManager = null;
        var strategy = new BraceFoldingStrategy();

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        void SafeUpdate()
        {
            try
            {
                if (editor.Document != null && foldingManager != null && editor.Document.TextLength > 0)
                {
                    strategy.UpdateFoldings(foldingManager, editor.Document);
                }
            }
            catch (ArgumentException)
            {
                // Ignorar estados transitorios
            }
        }

        timer.Tick += (s, e) =>
        {
            timer.Stop();
            SafeUpdate();
        };

        editor.Loaded += (s, e) =>
        {
            if (foldingManager == null && editor.TextArea != null)
            {
                foldingManager = FoldingManager.Install(editor.TextArea);

                // --- PERSONALIZACIÓN DE COLOR DE LOS BOTONES [+] y [-] ---
                // Buscar el FoldingMargin recién instalado en los márgenes izquierdos
                foreach (var margin in editor.TextArea.LeftMargins)
                {
                    if (margin is FoldingMargin foldMargin)
                    {
                        // Color de las líneas, borde del cuadro y signos + / - (Gris sutil estilo VS)
                        foldMargin.FoldingMarkerBrush = Brush.Parse("#858585");

                        // Color de fondo dentro del cuadrito (para que combine con el fondo oscuro #1E1E1E o #252526)
                        foldMargin.FoldingMarkerBackgroundBrush = Brush.Parse("#252526");

                        // Color cuando pasas el cursor por encima (Hover)
                        foldMargin.SelectedFoldingMarkerBrush = Brush.Parse("#007ACC");
                        foldMargin.SelectedFoldingMarkerBackgroundBrush = Brush.Parse("#2D2D30");
                    }
                }

                SafeUpdate();
            }
        };

        editor.TextChanged += (s, e) =>
        {
            timer.Stop();
            timer.Start();
        };

        return foldingManager;
    }

    /// <summary>
    /// Construye y conecta el menú contextual enriquecido del editor.
    /// </summary>
    public static ContextMenu AttachIDEContextMenu(this TextEditor editor, Func<CodeEditorViewModel?> getViewModel)
    {
        var contextMenu = new ContextMenu
        {
            Background = Brush.Parse("#252526"),
            BorderBrush = Brush.Parse("#3E3E42"),
            BorderThickness = new Thickness(1)
        };

        var itemChat = new MenuItem { Header = "Chat con Asistente M.A.R.K", InputGesture = new KeyGesture(Key.C, KeyModifiers.Alt) };
        var itemExplain = new MenuItem { Header = "Explicar Código" };
        var sep1 = new Separator { Background = Brush.Parse("#3E3E42") };

        var itemRename = new MenuItem { Header = "Cambiar nombre (Renombrar)...", InputGesture = new KeyGesture(Key.F2) };
        itemRename.Click += (s, e) =>
        {
            var vm = getViewModel();
            if (vm != null)
            {
                _ = vm.RenameSymbolAsync(editor.CaretOffset);
            }
        };

        var sep2 = new Separator { Background = Brush.Parse("#3E3E42") };

        var itemCut = new MenuItem { Header = "Cortar", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) };
        itemCut.Click += (s, e) => editor.Cut();

        var itemCopy = new MenuItem { Header = "Copiar", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) };
        itemCopy.Click += (s, e) => editor.Copy();

        var itemPaste = new MenuItem { Header = "Pegar", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) };
        itemPaste.Click += (s, e) => editor.Paste();

        contextMenu.Items.Add(itemChat);
        contextMenu.Items.Add(itemExplain);
        contextMenu.Items.Add(sep1);
        contextMenu.Items.Add(itemRename);
        contextMenu.Items.Add(sep2);
        contextMenu.Items.Add(itemCut);
        contextMenu.Items.Add(itemCopy);
        contextMenu.Items.Add(itemPaste);

        editor.TextArea.ContextRequested += (s, e) =>
        {
            contextMenu.Open(editor.TextArea);
            e.Handled = true;
        };

        return contextMenu;
    }

    /// <summary>
    /// Habilita el zoom interactivo con Ctrl + Rueda de ratón.
    /// </summary>
    public static void EnablePointerZoom(this TextEditor editor, Action<double> onZoomChanged)
    {
        editor.AddHandler(InputElement.PointerWheelChangedEvent, (s, e) =>
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                double newSize = Math.Clamp(editor.FontSize + (e.Delta.Y > 0 ? 1.5 : -1.5), 9.0, 40.0);
                editor.FontSize = newSize;
                onZoomChanged(newSize);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
    }
}