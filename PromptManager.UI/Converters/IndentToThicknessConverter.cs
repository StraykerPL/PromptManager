using System.Globalization;
using Avalonia.Data.Converters;

namespace PromptManager.UI.Converters
{
    public sealed class IndentToThicknessConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var indent = value is int intValue ? intValue : 0;
            return new global::Avalonia.Thickness(indent, 0, 0, 0);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
