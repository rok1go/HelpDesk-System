using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Services;
using HelpDesk_System.Utilities;

namespace HelpDesk_System.Windows;

public partial class WorkerWindow : Window
{
    private readonly User _currentUser;
    private readonly TicketService _ticketService;
    private readonly WindowNavigationService _navigationService;
    private List<Ticket> _userTickets = [];
    private Ticket? _selectedTicket;

    public WorkerWindow(
        User currentUser,
        TicketService ticketService,
        WindowNavigationService navigationService)
    {
        InitializeComponent();

        _currentUser = currentUser;
        _ticketService = ticketService;
        _navigationService = navigationService;

        ProblemTypeComboBox.ItemsSource = Enum.GetValues<ProblemType>();
        WorkImpactComboBox.ItemsSource = Enum.GetValues<WorkImpact>();
        AffectedPeopleComboBox.ItemsSource = Enum.GetValues<AffectedPeople>();

        Width = Math.Min(1380, SystemParameters.WorkArea.Width * 0.92);
        Height = Math.Min(820, SystemParameters.WorkArea.Height * 0.88);
        UserNameText.Text = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
        UserEmailText.Text = currentUser.Email;
    }

    private async void WorkerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadUserTicketsAsync();
    }

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

    private async void SubmitTicketButton_Click(object sender, RoutedEventArgs e)
    {
        ClearFormMessages();

        var title = TitleTextBox.Text.Trim();
        var description = DescriptionTextBox.Text.Trim();

        if (!TryGetTicketFormValues(
                title,
                description,
                out var problemType,
                out var workImpact,
                out var affectedPeople))
        {
            return;
        }

        SetTicketFormEnabled(false);

        try
        {
            await _ticketService.CreateTicketAsync(
                _currentUser.Id,
                title,
                description,
                problemType,
                workImpact,
                affectedPeople);

            ResetTicketForm();
            ShowFormSuccessMessage("Ticket was submitted successfully.");
            await LoadUserTicketsAsync();
        }
        catch (Exception exception) when (DatabaseExceptionClassifier.IsDatabaseFailure(exception))
        {
            ShowFormErrorMessage(
                "The ticket could not be sent. Check the database connection and try again.");
        }
        finally
        {
            SetTicketFormEnabled(true);
        }
    }

    private bool TryGetTicketFormValues(
        string title,
        string description,
        out ProblemType problemType,
        out WorkImpact workImpact,
        out AffectedPeople affectedPeople)
    {
        var isValid = true;

        if (string.IsNullOrWhiteSpace(title))
        {
            ShowMessage(TitleErrorText, "Enter a ticket title.");
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            ShowMessage(DescriptionErrorText, "Describe the issue.");
            isValid = false;
        }

        if (ProblemTypeComboBox.SelectedItem is ProblemType selectedProblemType)
        {
            problemType = selectedProblemType;
        }
        else
        {
            problemType = default;
            ShowMessage(ProblemTypeErrorText, "Select a problem type.");
            isValid = false;
        }

        if (WorkImpactComboBox.SelectedItem is WorkImpact selectedWorkImpact)
        {
            workImpact = selectedWorkImpact;
        }
        else
        {
            workImpact = default;
            ShowMessage(WorkImpactErrorText, "Select the work impact.");
            isValid = false;
        }

        if (AffectedPeopleComboBox.SelectedItem is AffectedPeople selectedAffectedPeople)
        {
            affectedPeople = selectedAffectedPeople;
        }
        else
        {
            affectedPeople = default;
            ShowMessage(AffectedPeopleErrorText, "Select who is affected.");
            isValid = false;
        }

        return isValid;
    }

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
        DeleteTicketButton.Visibility = _selectedTicket is not null && CanCurrentUserDelete(_selectedTicket)
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

    private async void RefreshTicketsButton_Click(object sender, RoutedEventArgs e)
    {
        HideTicketListSuccessMessage();
        await LoadUserTicketsAsync();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.OpenLogin(this);
    }

    private void ResetTicketForm()
    {
        TitleTextBox.Clear();
        DescriptionTextBox.Clear();
        ProblemTypeComboBox.SelectedIndex = -1;
        WorkImpactComboBox.SelectedIndex = -1;
        AffectedPeopleComboBox.SelectedIndex = -1;
    }

    private void SetTicketFormEnabled(bool isEnabled)
    {
        TitleTextBox.IsEnabled = isEnabled;
        DescriptionTextBox.IsEnabled = isEnabled;
        ProblemTypeComboBox.IsEnabled = isEnabled;
        WorkImpactComboBox.IsEnabled = isEnabled;
        AffectedPeopleComboBox.IsEnabled = isEnabled;
        SubmitTicketButton.IsEnabled = isEnabled;
    }

    private void ClearFormMessages()
    {
        HideMessage(TitleErrorText);
        HideMessage(DescriptionErrorText);
        HideMessage(ProblemTypeErrorText);
        HideMessage(WorkImpactErrorText);
        HideMessage(AffectedPeopleErrorText);
        HideMessage(FormSuccessMessageText);
        HideMessage(FormErrorMessageText);
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

    private static void ShowMessage(TextBlock textBlock, string message)
    {
        textBlock.Text = message;
        textBlock.Visibility = Visibility.Visible;
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

    private static void HideMessage(TextBlock textBlock)
    {
        textBlock.Text = string.Empty;
        textBlock.Visibility = Visibility.Collapsed;
    }
}
