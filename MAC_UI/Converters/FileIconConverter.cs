using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FluentIcons.Common;

namespace MAC_UI.Converters;

public class FileIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string fileName = value as string ?? string.Empty;
        string ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext switch
        {
            ".cs" => Symbol.Code,
            ".axaml" or ".xaml" => Symbol.WindowDevTools,
            ".sln" or ".slnx" or ".macsln" => Symbol.AppGeneric,
            ".csproj" or ".macproj" => Symbol.FolderZip,
            ".rs" => Symbol.DeveloperBoard,
            ".cpp" or ".c" or ".h" => Symbol.Code,
            ".json" or ".xml" or ".yaml" or ".toml" => Symbol.DocumentSettings,
            ".md" => Symbol.DocumentText,
            _ => Symbol.Document
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class FileColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string fileName = value as string ?? string.Empty;
        string ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext switch
        {
            ".cs" => Brush.Parse("#178600"),          // Verde C#
            ".axaml" or ".xaml" => Brush.Parse("#9A68A8"), // Púrpura XAML
            ".rs" => Brush.Parse("#DEA584"),          // Rust
            ".cpp" or ".c" => Brush.Parse("#004482"), // C++
            ".json" or ".yaml" => Brush.Parse("#FBC02D"), // Amarillo config
            ".md" => Brush.Parse("#007ACC"),          // Azul Markdown
            _ => Brush.Parse("#CCCCCC")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}