using System.Windows;
using HelpDesk_System.Utilities;

namespace HelpDesk_System.Windows;

public partial class WorkerWindow
{
    private async Task LoadUserTicketsAsync()
    {
        var selectedTicketId = _selectedTicket?.Id;

        try
        {
            _userTickets = await _ticketService.GetUserTicketsAsync(_currentUser.Id);
            UserTicketsList.ItemsSource = _userTickets;

            UserTicketsCountText.Text = DisplayFormatter.FormatCount(
                _userTickets.Count,
                "ticket",
                "tickets");

            UserTicketsEmptyText.Visibility = _userTickets.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            HideWorkspaceStatusMessage();

            if (!selectedTicketId.HasValue)
            {
                return;
            }

            var currentTicket = _userTickets
                .FirstOrDefault(ticket => ticket.Id == selectedTicketId.Value);

            if (currentTicket is null)
            {
                HideTicketDetails();
            }
            else
            {
                ShowTicketDetails(currentTicket, false);
            }
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowWorkspaceStatusMessage(
                "Tickets could not be loaded. Check the database connection.");
        }
    }

    private async void RefreshTicketsButton_Click(object sender, RoutedEventArgs e)
    {
        HideTicketListSuccessMessage();
        await LoadUserTicketsAsync();
    }
}
