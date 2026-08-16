using System.Windows;
using System.Windows.Controls;
using HelpDesk_System.Utilities;

namespace HelpDesk_System.Windows;

public partial class WorkerWindow
{
    private const int WorkerKnowledgeBaseSearchDelayMilliseconds = 350;

    private CancellationTokenSource? _workerKnowledgeBaseLoadCancellation;

    private async Task LoadWorkerKnowledgeBaseAsync(
        string? searchText,
        CancellationToken cancellationToken)
    {
        HideMessage(WorkerKnowledgeBaseStatusMessageText);

        try
        {
            var tickets = await _ticketService.GetPublishedKnowledgeBaseTicketsAsync(
                searchText,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            WorkerKnowledgeBaseList.ItemsSource = tickets;
            WorkerKnowledgeBaseCountText.Text = DisplayFormatter.FormatCount(
                tickets.Count,
                "published solution",
                "published solutions");

            var searchIsActive = !string.IsNullOrWhiteSpace(searchText);
            WorkerKnowledgeBaseEmptyText.Text = searchIsActive
                ? "No published solutions match your search."
                : "There are no published solutions yet.";
            WorkerKnowledgeBaseEmptyText.Visibility = tickets.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowMessage(
                WorkerKnowledgeBaseStatusMessageText,
                "Knowledge base could not be loaded. Check the database connection.");
        }
    }

    private async Task ReloadWorkerKnowledgeBaseAsync(bool delaySearch)
    {
        _workerKnowledgeBaseLoadCancellation?.Cancel();

        var cancellation = new CancellationTokenSource();
        _workerKnowledgeBaseLoadCancellation = cancellation;
        SetWorkerKnowledgeBaseLoading(true);

        try
        {
            if (delaySearch)
            {
                await Task.Delay(
                    WorkerKnowledgeBaseSearchDelayMilliseconds,
                    cancellation.Token);
            }

            await LoadWorkerKnowledgeBaseAsync(
                WorkerKnowledgeBaseSearchTextBox.Text,
                cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_workerKnowledgeBaseLoadCancellation, cancellation))
            {
                _workerKnowledgeBaseLoadCancellation = null;
                SetWorkerKnowledgeBaseLoading(false);
            }

            cancellation.Dispose();
        }
    }

    private void SetWorkerKnowledgeBaseLoading(bool isLoading)
    {
        KnowledgeBaseButton.IsEnabled = !isLoading;
        RefreshWorkerKnowledgeBaseButton.IsEnabled = !isLoading;
    }

    private async void KnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        HideTicketDetails();
        KnowledgeBasePanel.Visibility = Visibility.Visible;

        await ReloadWorkerKnowledgeBaseAsync(false);
    }

    private async void RefreshWorkerKnowledgeBaseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ReloadWorkerKnowledgeBaseAsync(false);
    }

    private async void WorkerKnowledgeBaseSearchTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ClearWorkerKnowledgeBaseSearchButton.IsEnabled =
            !string.IsNullOrWhiteSpace(WorkerKnowledgeBaseSearchTextBox.Text);

        if (!IsLoaded || KnowledgeBasePanel.Visibility != Visibility.Visible)
        {
            return;
        }

        await ReloadWorkerKnowledgeBaseAsync(true);
    }

    private async void ClearWorkerKnowledgeBaseSearchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(WorkerKnowledgeBaseSearchTextBox.Text))
        {
            await ReloadWorkerKnowledgeBaseAsync(false);
            return;
        }

        WorkerKnowledgeBaseSearchTextBox.Clear();
        await ReloadWorkerKnowledgeBaseAsync(false);
    }

    private void CloseKnowledgeBaseButton_Click(object sender, RoutedEventArgs e)
    {
        _workerKnowledgeBaseLoadCancellation?.Cancel();
        KnowledgeBasePanel.Visibility = Visibility.Collapsed;
    }
}
