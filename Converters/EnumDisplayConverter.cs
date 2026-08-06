using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace HelpDesk_System.Converters;

public class EnumDisplayConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is Enum ? Regex.Replace(value.ToString()!, "([a-z])([A-Z])", "$1 $2") : value?.ToString() ?? string.Empty;

	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
