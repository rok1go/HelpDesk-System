using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Utilities;

namespace HelpDesk_System.Windows;

public partial class WorkerWindow
{
    private void TicketCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Ticket ticket })
        {
            ShowTicketDetails(ticket);
        }
    }

    private void ShowTicketDetails(Ticket ticket, bool animate = true)
    {
        _selectedTicket = ticket;
        DetailsStatusText.Text = DisplayFormatter.FormatEnum(ticket.Status);
        DetailsTitleText.Text = ticket.Title;
        DetailsPriorityText.Text = DisplayFormatter.FormatEnum(ticket.Priority);
        DetailsCreatedText.Text = DisplayFormatter.FormatLocalDateTime(ticket.CreatedAt);
        DetailsProblemTypeText.Text = DisplayFormatter.FormatEnum(ticket.ProblemType);
        DetailsWorkImpactText.Text = DisplayFormatter.FormatEnum(ticket.WorkImpact);
        DetailsAffectedPeopleText.Text = DisplayFormatter.FormatEnum(ticket.AffectedPeople);
        DetailsAssignedAdminText.Text = ticket.AssignedAdmin is null
            ? "Not assigned yet"
            : $"{ticket.AssignedAdmin.FirstName} {ticket.AssignedAdmin.LastName}".Trim();
        DetailsDescriptionText.Text = ticket.Description;

        ConfigureDeleteActions(ticket);
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

    private void CloseDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        HideTicketDetails();
    }

    private void DeleteTicketButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTicket is null || !CanCurrentUserDelete(_selectedTicket))
        {
            return;
        }

        HideMessage(DeleteTicketErrorText);
        DeleteTicketButton.Visibility = Visibility.Collapsed;
        DeleteTicketConfirmationPanel.Visibility = Visibility.Visible;
    }

    private void CancelDeleteTicketButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteTicketConfirmationPanel.Visibility = Visibility.Collapsed;
        DeleteTicketButton.Visibility = _selectedTicket is not null
            && CanCurrentUserDelete(_selectedTicket)
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private async void ConfirmDeleteTicketButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTicket is null)
        {
            return;
        }

        var ticketId = _selectedTicket.Id;
        SetDeleteConfirmationEnabled(false);
        HideMessage(DeleteTicketErrorText);
        HideTicketListSuccessMessage();

        try
        {
            var ticketWasDeleted = await _ticketService.DeleteOpenTicketAsync(
                ticketId,
                _currentUser.Id);

            if (!ticketWasDeleted)
            {
                await LoadUserTicketsAsync();
                ShowWorkspaceStatusMessage(
                    "The ticket can no longer be deleted because it has been taken or is no longer open.");
                return;
            }

            HideTicketDetails();
            await LoadUserTicketsAsync();
            ShowTicketListSuccessMessage("Ticket was deleted.");
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            DeleteTicketConfirmationPanel.Visibility = Visibility.Collapsed;
            DeleteTicketButton.Visibility = Visibility.Visible;
            ShowMessage(
                DeleteTicketErrorText,
                "The ticket could not be deleted. Check the database connection and try again.");
        }
        finally
        {
            SetDeleteConfirmationEnabled(true);
        }
    }

    private void ConfigureDeleteActions(Ticket ticket)
    {
        HideMessage(DeleteTicketErrorText);
        DeleteTicketConfirmationPanel.Visibility = Visibility.Collapsed;
        DeleteTicketButton.Visibility = CanCurrentUserDelete(ticket)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private bool CanCurrentUserDelete(Ticket ticket)
    {
        return ticket.AuthorId == _currentUser.Id
            && ticket.Status == TicketStatus.Open
            && ticket.AssignedAdminId is null;
    }

    private void SetDeleteConfirmationEnabled(bool isEnabled)
    {
        CancelDeleteTicketButton.IsEnabled = isEnabled;
        ConfirmDeleteTicketButton.IsEnabled = isEnabled;
    }
}
