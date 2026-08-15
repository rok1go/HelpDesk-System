using System.Windows;
using System.Windows.Controls;

namespace HelpDesk_System.Windows;

public partial class WorkerWindow
{
    private static void ShowMessage(TextBlock textBlock, string message)
    {
        textBlock.Text = message;
        textBlock.Visibility = Visibility.Visible;
    }

    private static void HideMessage(TextBlock textBlock)
    {
        textBlock.Text = string.Empty;
        textBlock.Visibility = Visibility.Collapsed;
    }

    private void ShowFormSuccessMessage(string message)
    {
        ShowMessage(FormSuccessMessageText, message);
    }

    private void ShowFormErrorMessage(string message)
    {
        ShowMessage(FormErrorMessageText, message);
    }

    private void ShowWorkspaceStatusMessage(string message)
    {
        ShowMessage(WorkspaceStatusMessageText, message);
    }

    private void HideWorkspaceStatusMessage()
    {
        HideMessage(WorkspaceStatusMessageText);
    }

    private void ShowTicketListSuccessMessage(string message)
    {
        ShowMessage(TicketListSuccessMessageText, message);
    }

    private void HideTicketListSuccessMessage()
    {
        HideMessage(TicketListSuccessMessageText);
    }
}
