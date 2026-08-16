using System.Windows;
using System.Windows.Controls;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Utilities;

namespace HelpDesk_System.Windows;

public partial class AdminWindow
{
    private const int KnowledgeBaseSearchDelayMilliseconds = 350;

    private KnowledgeBaseSection _activeKnowledgeBaseSection = KnowledgeBaseSection.Completed;
    private CancellationTokenSource? _knowledgeBaseLoadCancellation;

    private async Task LoadKnowledgeBaseAsync(
        string? searchText,
        CancellationToken cancellationToken)
    {
        HideMessage(KnowledgeBaseStatusMessageText);

        try
        {
            var ticketsTask = _ticketService.GetKnowledgeBaseTicketsAsync(
                searchText,
                cancellationToken);

            var registrationsTask = _registrationRequestService.GetProcessedRequestsAsync(
                searchText,
                cancellationToken);

            await Task.WhenAll(ticketsTask, registrationsTask);
            cancellationToken.ThrowIfCancellationRequested();

            UpdateKnowledgeBaseEmptyText(!string.IsNullOrWhiteSpace(searchText));
            BindKnowledgeBaseTickets(await ticketsTask);
            BindProcessedRegistrations(await registrationsTask);
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowMessage(
                KnowledgeBaseStatusMessageText,
                "Knowledge base could not be loaded. Check the database connection.");
        }
    }

    private async Task ReloadKnowledgeBaseAsync(bool delaySearch)
    {
        _knowledgeBaseLoadCancellation?.Cancel();

        var cancellation = new CancellationTokenSource();
        _knowledgeBaseLoadCancellation = cancellation;
        SetKnowledgeBaseLoading(true);

        try
        {
            if (delaySearch)
            {
                await Task.Delay(
                    KnowledgeBaseSearchDelayMilliseconds,
                    cancellation.Token);
            }

            await LoadKnowledgeBaseAsync(
                KnowledgeBaseSearchTextBox.Text,
                cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_knowledgeBaseLoadCancellation, cancellation))
            {
                _knowledgeBaseLoadCancellation = null;
                SetKnowledgeBaseLoading(false);
            }

            cancellation.Dispose();
        }
    }

    private void SetKnowledgeBaseLoading(bool isLoading)
    {
        KnowledgeBaseButton.IsEnabled = !isLoading;
        RefreshKnowledgeBaseButton.IsEnabled = !isLoading;
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

    private void UpdateKnowledgeBaseEmptyText(bool isSearchActive)
    {
        CompletedKnowledgeBaseEmptyText.Text = isSearchActive
            ? "No completed tickets match your search."
            : "There are no completed tickets yet.";

        DeclinedKnowledgeBaseEmptyText.Text = isSearchActive
            ? "No declined tickets match your search."
            : "There are no declined tickets yet.";

        InProgressKnowledgeBaseEmptyText.Text = isSearchActive
            ? "No in-progress tickets match your search."
            : "There are no tickets in progress.";

        ApprovedRegistrationsEmptyText.Text = isSearchActive
            ? "No approved registrations match your search."
            : "There are no approved registrations yet.";

        DeclinedRegistrationsEmptyText.Text = isSearchActive
            ? "No declined registrations match your search."
            : "There are no declined registrations yet.";
    }

    private async void KnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        HideTicketDetails();
        HideRegistrationRequestsPanel();
        ShowKnowledgeBaseSection(_activeKnowledgeBaseSection);
        KnowledgeBasePanel.Visibility = Visibility.Visible;

        await ReloadKnowledgeBaseAsync(false);
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
        await ReloadKnowledgeBaseAsync(false);
    }

    private async void KnowledgeBaseSearchTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ClearKnowledgeBaseSearchButton.IsEnabled =
            !string.IsNullOrWhiteSpace(KnowledgeBaseSearchTextBox.Text);

        if (!IsLoaded || KnowledgeBasePanel.Visibility != Visibility.Visible)
        {
            return;
        }

        await ReloadKnowledgeBaseAsync(true);
    }

    private async void ClearKnowledgeBaseSearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(KnowledgeBaseSearchTextBox.Text))
        {
            await ReloadKnowledgeBaseAsync(false);
            return;
        }

        KnowledgeBaseSearchTextBox.Clear();
        await ReloadKnowledgeBaseAsync(false);
    }

    private void CloseKnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        HideKnowledgeBasePanel();
    }

    private void HideKnowledgeBasePanel()
    {
        _knowledgeBaseLoadCancellation?.Cancel();
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
