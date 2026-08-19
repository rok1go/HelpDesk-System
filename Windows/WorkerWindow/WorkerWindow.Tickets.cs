using System.Windows;
using System.Windows.Controls;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
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
            var visibleTickets = ApplyUserTicketFilters();

            HideWorkspaceStatusMessage();

            if (!selectedTicketId.HasValue)
            {
                return;
            }

            var currentTicket = visibleTickets
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

    private List<Ticket> ApplyUserTicketFilters()
    {
        var searchText = UserTicketSearchTextBox.Text.Trim();
        var selectedStatus = UserTicketStatusFilterComboBox.SelectedItem is ComboBoxItem
        {
            Tag: TicketStatus status
        }
            ? status
            : (TicketStatus?)null;

        var visibleTickets = _userTickets
            .Where(ticket =>
                (!selectedStatus.HasValue || ticket.Status == selectedStatus.Value) &&
                MatchesUserTicketSearch(ticket, searchText))
            .ToList();

        UserTicketsList.ItemsSource = visibleTickets;
        UserTicketsCountText.Text = DisplayFormatter.FormatCount(
            visibleTickets.Count,
            "ticket",
            "tickets");

        var filterIsActive = selectedStatus.HasValue || !string.IsNullOrWhiteSpace(searchText);
        UserTicketsEmptyText.Text = filterIsActive
            ? "No tickets match the selected filters."
            : "You have not submitted any tickets yet.";

        UserTicketsEmptyText.Visibility = visibleTickets.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        return visibleTickets;
    }

    private static bool MatchesUserTicketSearch(Ticket ticket, string searchText)
    {
        return string.IsNullOrWhiteSpace(searchText) ||
               ticket.Id.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               ticket.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               ticket.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void UserTicketSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyUserTicketFiltersAfterInput();
    }

    private void UserTicketStatusFilterComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        ApplyUserTicketFiltersAfterInput();
    }

    private void ApplyUserTicketFiltersAfterInput()
    {
        if (!IsLoaded)
        {
            return;
        }

        var visibleTickets = ApplyUserTicketFilters();
        if (_selectedTicket is not null &&
            visibleTickets.All(ticket => ticket.Id != _selectedTicket.Id))
        {
            HideTicketDetails();
        }
    }

    private async void RefreshTicketsButton_Click(object sender, RoutedEventArgs e)
    {
        HideTicketListSuccessMessage();
        await LoadUserTicketsAsync();
    }
}
