using System.Windows;
using System.Windows.Controls;

namespace HelpDesk_System.Windows;

public partial class AdminWindow
{
    private static void ShowMessage(TextBlock target, string message)
    {
        target.Text = message;
        target.Visibility = Visibility.Visible;
    }

    private static void HideMessage(TextBlock target)
    {
        target.Text = string.Empty;
        target.Visibility = Visibility.Collapsed;
    }
}
