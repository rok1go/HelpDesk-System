using System.Text.RegularExpressions;

namespace HelpDesk_System.Utilities;

public static class DisplayFormatter
{
    private const string DateTimePattern = "dd.MM.yyyy HH:mm";

    private static readonly Regex EnumWordBoundaryPattern = new(
        "([a-z])([A-Z])",
        RegexOptions.Compiled);

    public static string FormatEnum(Enum value)
    {
        return EnumWordBoundaryPattern.Replace(value.ToString(), "$1 $2");
    }

    public static string FormatLocalDateTime(DateTime value)
    {
        return value.ToLocalTime().ToString(DateTimePattern);
    }

    public static string FormatCount(int count, string singular, string plural)
    {
        return count == 1 ? $"1 {singular}" : $"{count} {plural}";
    }
}
