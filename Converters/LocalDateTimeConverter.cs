using System.Globalization;
using System.Windows.Data;
using HelpDesk_System.Utilities;

namespace HelpDesk_System.Converters;

public class LocalDateTimeConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is DateTime dateTime
            ? DisplayFormatter.FormatLocalDateTime(dateTime)
            : string.Empty;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
