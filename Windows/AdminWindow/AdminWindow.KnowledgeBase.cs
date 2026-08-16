using System.Windows;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Utilities;

namespace HelpDesk_System.Windows;

public partial class AdminWindow
{
    private KnowledgeBaseSection _activeKnowledgeBaseSection = KnowledgeBaseSection.Completed;

    private async Task LoadKnowledgeBaseAsync()
    {
        KnowledgeBaseButton.IsEnabled = false;
        RefreshKnowledgeBaseButton.IsEnabled = false;
        HideMessage(KnowledgeBaseStatusMessageText);

        try
        {
            var ticketsTask = _ticketService.GetKnowledgeBaseTicketsAsync();
            var registrationsTask = _registrationRequestService.GetProcessedRequestsAsync();

            await Task.WhenAll(ticketsTask, registrationsTask);

            BindKnowledgeBaseTickets(await ticketsTask);
            BindProcessedRegistrations(await registrationsTask);
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowMessage(
                KnowledgeBaseStatusMessageText,
                "Knowledge base could not be loaded. Check the database connection.");
        }
        finally
        {
            KnowledgeBaseButton.IsEnabled = true;
            RefreshKnowledgeBaseButton.IsEnabled = true;
        }
    }

    private void BindKnowledgeBaseTickets(List<Ticket> tickets)
    {
        var completedTickets = tickets
            .Where(ticket =>
                ticket.Status == TicketStatus.Resolved ||
                ticket.Status == TicketStatus.Closed)
            .ToList();

        var declinedTickets = tickets
            .Where(ticket => ticket.Status == TicketStatus.Declined)
            .ToList();

        var inProgressTickets = tickets
            .Where(ticket => ticket.Status == TicketStatus.InProgress)
            .ToList();

        CompletedKnowledgeBaseList.ItemsSource = completedTickets;
        DeclinedKnowledgeBaseList.ItemsSource = declinedTickets;
        InProgressKnowledgeBaseList.ItemsSource = inProgressTickets;

        CompletedKnowledgeBaseButton.Content = $"Completed ({completedTickets.Count})";
        DeclinedKnowledgeBaseButton.Content = $"Declined ({declinedTickets.Count})";
        InProgressKnowledgeBaseButton.Content = $"In progress ({inProgressTickets.Count})";

        CompletedKnowledgeBaseEmptyText.Visibility = GetEmptyStateVisibility(completedTickets.Count);
        DeclinedKnowledgeBaseEmptyText.Visibility = GetEmptyStateVisibility(declinedTickets.Count);
        InProgressKnowledgeBaseEmptyText.Visibility = GetEmptyStateVisibility(inProgressTickets.Count);
    }

    private void BindProcessedRegistrations(List<RegistrationRequest> requests)
    {
        var approvedRequests = requests
            .Where(request => request.Status == RegistrationRequestStatus.Approved)
            .ToList();

        var declinedRequests = requests
            .Where(request => request.Status == RegistrationRequestStatus.Declined)
            .ToList();

        ApprovedRegistrationsList.ItemsSource = approvedRequests;
        DeclinedRegistrationsList.ItemsSource = declinedRequests;

        var processedCount = approvedRequests.Count + declinedRequests.Count;
        RegistrationsKnowledgeBaseButton.Content = $"Registrations ({processedCount})";

        ApprovedRegistrationsEmptyText.Visibility = GetEmptyStateVisibility(approvedRequests.Count);
        DeclinedRegistrationsEmptyText.Visibility = GetEmptyStateVisibility(declinedRequests.Count);
    }

    private static Visibility GetEmptyStateVisibility(int itemCount)
    {
        return itemCount == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void KnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        HideTicketDetails();
        HideRegistrationRequestsPanel();
        ShowKnowledgeBaseSection(_activeKnowledgeBaseSection);
        KnowledgeBasePanel.Visibility = Visibility.Visible;

        await LoadKnowledgeBaseAsync();
    }

    private void CompletedKnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        ShowKnowledgeBaseSection(KnowledgeBaseSection.Completed);
    }

    private void DeclinedKnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        ShowKnowledgeBaseSection(KnowledgeBaseSection.Declined);
    }

    private void InProgressKnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        ShowKnowledgeBaseSection(KnowledgeBaseSection.InProgress);
    }

    private void RegistrationsKnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        ShowKnowledgeBaseSection(KnowledgeBaseSection.Registrations);
    }

    private void ShowKnowledgeBaseSection(KnowledgeBaseSection section)
    {
        _activeKnowledgeBaseSection = section;

        CompletedKnowledgeBaseView.Visibility = section == KnowledgeBaseSection.Completed
            ? Visibility.Visible
            : Visibility.Collapsed;

        DeclinedKnowledgeBaseView.Visibility = section == KnowledgeBaseSection.Declined
            ? Visibility.Visible
            : Visibility.Collapsed;

        InProgressKnowledgeBaseView.Visibility = section == KnowledgeBaseSection.InProgress
            ? Visibility.Visible
            : Visibility.Collapsed;

        RegistrationsKnowledgeBaseView.Visibility = section == KnowledgeBaseSection.Registrations
            ? Visibility.Visible
            : Visibility.Collapsed;

        CompletedKnowledgeBaseButton.Tag = section == KnowledgeBaseSection.Completed
            ? "Selected"
            : null;
        DeclinedKnowledgeBaseButton.Tag = section == KnowledgeBaseSection.Declined
            ? "Selected"
            : null;
        InProgressKnowledgeBaseButton.Tag = section == KnowledgeBaseSection.InProgress
            ? "Selected"
            : null;
        RegistrationsKnowledgeBaseButton.Tag = section == KnowledgeBaseSection.Registrations
            ? "Selected"
            : null;
    }

    private async void RefreshKnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadKnowledgeBaseAsync();
    }

    private void CloseKnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        HideKnowledgeBasePanel();
    }

    private void HideKnowledgeBasePanel()
    {
        KnowledgeBasePanel.Visibility = Visibility.Collapsed;
    }

    private enum KnowledgeBaseSection
    {
        Completed,
        Declined,
        InProgress,
        Registrations
    }
}
