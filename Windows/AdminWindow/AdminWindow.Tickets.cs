using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Utilities;

namespace HelpDesk_System.Windows;

public partial class AdminWindow
{
    private async Task LoadTicketsAsync()
    {
        var selectedTicketId = _selectedTicket?.Id;

        try
        {
            var openTicketsTask = _ticketService.GetOpenTicketsAsync();
            var assignedTicketsTask = _ticketService.GetAssignedTicketsAsync(_currentAdmin.Id);

            await Task.WhenAll(openTicketsTask, assignedTicketsTask);
            HideMessage(WorkspaceStatusMessageText);

            _openTickets = await openTicketsTask;
            _assignedTickets = await assignedTicketsTask;

            OpenTicketsList.ItemsSource = _openTickets;
            AssignedTicketsList.ItemsSource = _assignedTickets;

            OpenTicketsCountText.Text = DisplayFormatter.FormatCount(
                _openTickets.Count,
                "ticket",
                "tickets");

            AssignedTicketsCountText.Text = DisplayFormatter.FormatCount(
                _assignedTickets.Count,
                "ticket",
                "tickets");

            OpenTicketsEmptyText.Visibility = _openTickets.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            AssignedTicketsEmptyText.Visibility = _assignedTickets.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (!selectedTicketId.HasValue)
            {
                return;
            }

            var currentTicket = _openTickets
                .Concat(_assignedTickets)
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
            ShowMessage(
                WorkspaceStatusMessageText,
                "Tickets could not be loaded. Check the database connection.");
        }
    }

    private void TicketCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Ticket ticket })
        {
            HideRegistrationRequestsPanel();
            HideKnowledgeBasePanel();
            ShowTicketDetails(ticket);
        }
    }

    private void ShowTicketDetails(Ticket ticket, bool animate = true)
    {
        _selectedTicket = ticket;
        TicketDetailsPanel.DataContext = ticket;

        DetailsStatusText.Text = DisplayFormatter.FormatEnum(ticket.Status);
        DetailsAuthorNameText.Text = $"{ticket.Author.FirstName} {ticket.Author.LastName}".Trim();
        DetailsAuthorEmailText.Text = ticket.Author.Email;
        DetailsTitleText.Text = ticket.Title;
        DetailsPriorityText.Text = DisplayFormatter.FormatEnum(ticket.Priority);
        DetailsCreatedText.Text = DisplayFormatter.FormatLocalDateTime(ticket.CreatedAt);
        DetailsProblemTypeText.Text = DisplayFormatter.FormatEnum(ticket.ProblemType);
        DetailsWorkImpactText.Text = DisplayFormatter.FormatEnum(ticket.WorkImpact);
        DetailsAffectedPeopleText.Text = DisplayFormatter.FormatEnum(ticket.AffectedPeople);
        DetailsDescriptionText.Text = ticket.Description;

        DeclineReasonTextBox.Clear();
        ResolutionTextBox.Clear();
        AdminCommentTextBox.Clear();
        HideMessage(TicketActionMessageText);
        HideMessage(TicketDeclineReasonErrorText);
        HideMessage(TicketCommentErrorText);
        HideMessage(TicketCommentSuccessText);

        var isOpen = ticket.Status == TicketStatus.Open;
        OpenTicketActionsPanel.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
        AssignedTicketActionsPanel.Visibility = isOpen
            ? Visibility.Collapsed
            : Visibility.Visible;
        TicketDetailsPanel.Visibility = Visibility.Visible;

        if (!animate)
        {
            TicketDetailsPanel.Opacity = 1;
            TicketDetailsPanel.RenderTransform = Transform.Identity;
            return;
        }

        var slide = new TranslateTransform(24, 0);
        TicketDetailsPanel.RenderTransform = slide;
        TicketDetailsPanel.Opacity = 0;

        TicketDetailsPanel.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));

        slide.BeginAnimation(
            TranslateTransform.XProperty,
            new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(150)));
    }

    private void HideTicketDetails()
    {
        _selectedTicket = null;
        TicketDetailsPanel.DataContext = null;

        if (TicketDetailsPanel.Visibility != Visibility.Visible)
        {
            return;
        }

        var slide = TicketDetailsPanel.RenderTransform as TranslateTransform
            ?? new TranslateTransform();

        TicketDetailsPanel.RenderTransform = slide;

        var fadeOut = new DoubleAnimation(
            TicketDetailsPanel.Opacity,
            0,
            TimeSpan.FromMilliseconds(120));

        fadeOut.Completed += (_, _) => TicketDetailsPanel.Visibility = Visibility.Collapsed;
        TicketDetailsPanel.BeginAnimation(OpacityProperty, fadeOut);

        slide.BeginAnimation(
            TranslateTransform.XProperty,
            new DoubleAnimation(slide.X, 24, TimeSpan.FromMilliseconds(120)));
    }

    private async void TakeTicketButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTicket is null)
        {
            return;
        }

        SetActionButtonsEnabled(false);

        try
        {
            var ticketWasTaken = await _ticketService.TakeTicketAsync(
                _selectedTicket.Id,
                _currentAdmin.Id);

            if (!ticketWasTaken)
            {
                ShowMessage(
                    TicketActionMessageText,
                    "This ticket is no longer available. Refresh the list and choose another one.");

                return;
            }

            await LoadTicketsAsync();
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowMessage(TicketActionMessageText, "The ticket could not be taken. Please try again.");
        }
        finally
        {
            SetActionButtonsEnabled(true);
        }
    }

    private async void DeclineTicketButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTicket is null)
        {
            return;
        }

        var reason = DeclineReasonTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            ShowMessage(TicketDeclineReasonErrorText, "Enter a decline reason.");
            return;
        }

        HideMessage(TicketDeclineReasonErrorText);
        SetActionButtonsEnabled(false);

        try
        {
            var ticketWasDeclined = await _ticketService.DeclineTicketAsync(
                _selectedTicket.Id,
                _currentAdmin.Id,
                reason);

            if (!ticketWasDeclined)
            {
                ShowMessage(
                    TicketActionMessageText,
                    "This ticket is no longer available. Refresh the list and choose another one.");

                return;
            }

            HideTicketDetails();
            await LoadTicketsAsync();
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowMessage(TicketActionMessageText, "The ticket could not be declined. Please try again.");
        }
        finally
        {
            SetActionButtonsEnabled(true);
        }
    }

    private async void CompleteTicketButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTicket is null)
        {
            return;
        }

        SetActionButtonsEnabled(false);

        try
        {
            var ticketWasCompleted = await _ticketService.CompleteTicketAsync(
                _selectedTicket.Id,
                _currentAdmin.Id,
                ResolutionTextBox.Text);

            if (!ticketWasCompleted)
            {
                ShowMessage(
                    TicketActionMessageText,
                    "This ticket can no longer be completed. Refresh the list and try again.");

                return;
            }

            HideTicketDetails();
            await LoadTicketsAsync();
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowMessage(TicketActionMessageText, "The ticket could not be completed. Please try again.");
        }
        finally
        {
            SetActionButtonsEnabled(true);
        }
    }

    private async void AddAdminCommentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTicket is null)
        {
            return;
        }

        var comment = AdminCommentTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(comment))
        {
            ShowMessage(TicketCommentErrorText, "Enter a comment.");
            return;
        }

        HideMessage(TicketCommentErrorText);
        HideMessage(TicketCommentSuccessText);
        SetActionButtonsEnabled(false);

        try
        {
            var commentWasAdded = await _ticketService.AddCommentAsync(
                _selectedTicket.Id,
                _currentAdmin.Id,
                comment);

            if (!commentWasAdded)
            {
                await LoadTicketsAsync();
                ShowMessage(
                    TicketCommentErrorText,
                    "The comment could not be added because the ticket state changed.");

                return;
            }

            await LoadTicketsAsync();
            ShowMessage(TicketCommentSuccessText, "Comment added.");
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowMessage(
                TicketCommentErrorText,
                "The comment could not be added. Check the database connection.");
        }
        finally
        {
            SetActionButtonsEnabled(true);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadTicketsAsync();
    }

    private void CloseDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        HideTicketDetails();
    }

    private void SetActionButtonsEnabled(bool isEnabled)
    {
        TakeTicketButton.IsEnabled = isEnabled;
        DeclineTicketButton.IsEnabled = isEnabled;
        CompleteTicketButton.IsEnabled = isEnabled;
        AddAdminCommentButton.IsEnabled = isEnabled;
    }

    private void DeclineReasonTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            HideMessage(TicketDeclineReasonErrorText);
        }
    }
}
