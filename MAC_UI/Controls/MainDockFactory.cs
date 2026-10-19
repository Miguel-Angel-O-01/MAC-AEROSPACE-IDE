using System;
using System.Collections.Generic;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using MAC_UI.ViewModels;

namespace MAC_UI.Dock;

public class MainDockFactory : Factory
{
    private readonly MainWindowViewModel _context;
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;
    private IToolDock? _bottomToolDock;
    private ITool? _outputDockable;

    public MainDockFactory(MainWindowViewModel context)
    {
        _context = context;
    }

    public override IRootDock CreateLayout()
    {
        // 1. Herramienta Izquierda: Explorador de soluciones compacto
        var explorerDockable = new Tool
        {
            Id = "SolutionExplorerTool",
            Title = "Explorador de soluciones",
            CanClose = false,
            CanFloat = true,
            CanPin = true,
            Context = _context.ExplorerViewModel
        };

        var solutionExplorerTool = new ToolDock
        {
            Id = "SolutionExplorerPane",
            Title = "Explorador de soluciones",
            Proportion = 0.16,
            MinWidth = 200,
            MaxWidth = 360,
            Alignment = Alignment.Left,
            GripMode = GripMode.Visible,
            VisibleDockables = CreateList<IDockable>(explorerDockable),
            ActiveDockable = explorerDockable
        };

        // 2. Panel Central de Documentos
        var documentDock = new DocumentDock
        {
            Id = "DocumentsPane",
            Title = "Documentos",
            Proportion = 0.72,
            MinHeight = 150
        };
        _documentDock = documentDock;

        // 3. Herramientas Inferiores (Salida y Terminal) declaradas UNA SOLA VEZ
        var outputDockable = new Tool
        {
            Id = "OutputTool",
            Title = "Salida",
            CanClose = true,
            CanFloat = true,
            CanPin = true,
            Context = _context.OutputViewModel
        };
        _outputDockable = outputDockable;

        var terminalDockable = new Tool
        {
            Id = "TerminalTool",
            Title = "Terminal",
            CanClose = false,
            CanFloat = true,
            CanPin = true,
            Context = _context.TerminalViewModel
        };

        var bottomToolDock = new ToolDock
        {
            Id = "BottomPane",
            Title = "Diagnóstico",
            Proportion = 0.28,
            MinHeight = 100,
            Alignment = Alignment.Bottom,
            GripMode = GripMode.Visible,
            VisibleDockables = CreateList<IDockable>(outputDockable, terminalDockable),
            ActiveDockable = outputDockable
        };
        _bottomToolDock = bottomToolDock;

        // 4. Centro Vertical: Documentos ARRIBA y Salida ABAJO
        var centerVerticalLayout = new ProportionalDock
        {
            Id = "CenterVerticalLayout",
            Orientation = Orientation.Vertical,
            Proportion = 0.84,
            VisibleDockables = CreateList<IDockable>(
                documentDock,
                new ProportionalDockSplitter(),
                bottomToolDock
            )
        };

        // 5. Layout Principal Horizontal
        var mainLayout = new ProportionalDock
        {
            Id = "MainLayout",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                solutionExplorerTool,
                new ProportionalDockSplitter(),
                centerVerticalLayout
            )
        };

        var rootDock = CreateRootDock();
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;

        _rootDock = rootDock;
        return rootDock;
    }

    public override void InitLayout(IDockable layout)
    {
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }

    public void OpenDocument(CodeEditorViewModel editorVm)
    {
        if (_documentDock == null) return;

        AddDockable(_documentDock, editorVm);
        SetActiveDockable(editorVm);
        SetFocusedDockable(_documentDock, editorVm);
    }

    public void UpdateDocumentTitle(CodeEditorViewModel editorVm)
    {
        if (_documentDock?.VisibleDockables == null) return;

        foreach (var dockable in _documentDock.VisibleDockables)
        {
            if (dockable is Document doc && doc.Context == editorVm)
            {
                doc.Title = editorVm.Title;
                break;
            }
        }
    }

    /// <summary>
    /// Despliega y enfoca la pestaña de Salida de forma tipada y segura (resuelve CS1503)
    /// </summary>
    public void ShowOutputPane()
    {
        if (_bottomToolDock != null && _outputDockable != null)
        {
            // Traer al frente la pestaña de Salida
            SetActiveDockable(_outputDockable);

            // Asegurarse de que el panel padre (CenterVerticalLayout) asigne alto suficiente (proporción)
            SetFocusedDockable(_bottomToolDock, _outputDockable);
        }
    }
}