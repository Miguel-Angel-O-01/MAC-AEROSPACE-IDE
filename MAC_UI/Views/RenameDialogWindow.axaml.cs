using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MAC_UI.Views;

public partial class RenameDialogWindow : Window
{
    public RenameDialogWindow()
    {
        InitializeComponent();
    }

    public RenameDialogWindow(string currentName) : this()
    {
        var box = this.FindControl<TextBox>("NewNameBox");
        if (box != null)
        {
            box.Text = currentName;
            box.SelectAll();
        }
    }

    private void OnAcceptClick(object? sender, RoutedEventArgs e)
    {
        var box = this.FindControl<TextBox>("NewNameBox");
        Close(box?.Text);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}