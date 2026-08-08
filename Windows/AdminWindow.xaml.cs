using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HelpDesk_System.Windows;

public partial class AdminWindow : Window
{
    private readonly User _currentAdmin;
    private readonly TicketService _ticketService;
    private readonly RegistrationRequestService _registrationRequestService;
    private readonly WindowNavigationService _navigationService;
    private List<Ticket> _openTickets = [];
    private List<Ticket> _assignedTickets = [];
    private List<RegistrationRequest> _pendingRegistrationRequests = [];
    private Ticket? _selectedTicket;
    private RegistrationRequest? _selectedRegistrationRequest;

    public AdminWindow(
        User currentAdmin,
        TicketService ticketService,
        RegistrationRequestService registrationRequestService,
        WindowNavigationService navigationService)
    {
        InitializeComponent();

        _currentAdmin = currentAdmin;
        _ticketService = ticketService;
        _registrationRequestService = registrationRequestService;
        _navigationService = navigationService;
        AssignedRoleComboBox.ItemsSource = Enum.GetValues<UserRole>();

        Width = Math.Min(1380, SystemParameters.WorkArea.Width * 0.92);
        Height = Math.Min(820, SystemParameters.WorkArea.Height * 0.88);
        AdminNameText.Text = $"{currentAdmin.FirstName} {currentAdmin.LastName}".Trim();
        AdminEmailText.Text = currentAdmin.Email;
    }

    private async void AdminWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadTicketsAsync();
        await LoadRegistrationRequestsAsync();
    }

    private async Task LoadTicketsAsync()
    {
        var selectedTicketId = _selectedTicket?.Id;

        try
        {
            var openTicketsTask = _ticketService.GetOpenTicketsAsync();
            var assignedTicketsTask = _ticketService.GetAssignedTicketsAsync(_currentAdmin.Id);
            await Task.WhenAll(openTicketsTask, assignedTicketsTask);
            HideWorkspaceStatusMessage();

            _openTickets = await openTicketsTask;
            _assignedTickets = await assignedTicketsTask;
            OpenTicketsList.ItemsSource = _openTickets;
            AssignedTicketsList.ItemsSource = _assignedTickets;

            OpenTicketsCountText.Text = GetTicketCountText(_openTickets.Count);
            AssignedTicketsCountText.Text = GetTicketCountText(_assignedTickets.Count);
            OpenTicketsEmptyText.Visibility = _openTickets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AssignedTicketsEmptyText.Visibility = _assignedTickets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (selectedTicketId.HasValue)
            {
                var currentTicket = _openTickets.Concat(_assignedTickets).FirstOrDefault(ticket => ticket.Id == selectedTicketId.Value);
                if (currentTicket is null)
                {
                    HideTicketDetails();
                }
                else
                {
                    ShowTicketDetails(currentTicket, false);
                }
            }
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            ShowWorkspaceStatusMessage("Tickets could not be loaded. Check the database connection.");
        }
    }

    private void TicketCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Ticket ticket })
        {
            HideRegistrationRequestsPanel();
            ShowTicketDetails(ticket);
        }
    }

    private async Task LoadRegistrationRequestsAsync()
    {
        var selectedRequestId = _selectedRegistrationRequest?.Id;

        try
        {
            _pendingRegistrationRequests = await _registrationRequestService.GetPendingRequestsAsync();
            RegistrationRequestsList.ItemsSource = _pendingRegistrationRequests;

            RegistrationRequestsCountText.Text = GetRegistrationRequestCountText(_pendingRegistrationRequests.Count);
            RegistrationRequestsButton.Content = $"Registration requests ({_pendingRegistrationRequests.Count})";
            RegistrationRequestsEmptyText.Visibility = _pendingRegistrationRequests.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (!selectedRequestId.HasValue)
            {
                return;
            }

            var currentRequest = _pendingRegistrationRequests
                .FirstOrDefault(request => request.Id == selectedRequestId.Value);

            if (currentRequest is null)
            {
                HideRegistrationRequestDetails();
            }
            else
            {
                ShowRegistrationRequestDetails(currentRequest);
            }
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            ShowRegistrationStatusMessage("Registration requests could not be loaded. Check the database connection.");
        }
    }

    private void RegistrationRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        HideTicketDetails();
        RegistrationRequestsPanel.Visibility = Visibility.Visible;
    }

    private void RegistrationRequestCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RegistrationRequest request })
        {
            ShowRegistrationRequestDetails(request);
        }
    }

    private void ShowRegistrationRequestDetails(RegistrationRequest request)
    {
        _selectedRegistrationRequest = request;
        HideRegistrationStatusMessage();
        RegistrationApplicantNameText.Text = $"{request.FirstName} {request.LastName}".Trim();
        RegistrationApplicantEmailText.Text = request.Email;
        RegistrationRequestedRoleText.Text = FormatEnum(request.RequestedRole);
        RegistrationCreatedText.Text = request.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        AssignedRoleComboBox.SelectedItem = request.RequestedRole;
        RegistrationDeclineReasonTextBox.Clear();
        HideRegistrationFormError();
        HideRegistrationDeclineReasonError();

        RegistrationSelectionHintText.Visibility = Visibility.Collapsed;
        RegistrationDetailsContent.Visibility = Visibility.Visible;
    }

    private void HideRegistrationRequestDetails()
    {
        _selectedRegistrationRequest = null;
        RegistrationDetailsContent.Visibility = Visibility.Collapsed;
        RegistrationSelectionHintText.Visibility = Visibility.Visible;
        HideRegistrationFormError();
        HideRegistrationDeclineReasonError();
    }

    private void HideRegistrationRequestsPanel()
    {
        RegistrationRequestsPanel.Visibility = Visibility.Collapsed;
    }

    private async void ApproveRegistrationRequestButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRegistrationRequest is null || AssignedRoleComboBox.SelectedItem is not UserRole assignedRole)
        {
            ShowRegistrationFormError("Select the role that will be assigned to the user.");
            return;
        }

        HideRegistrationFormError();
        SetRegistrationActionButtonsEnabled(false);

        try
        {
            var requestWasApproved = await _registrationRequestService.ApproveAsync(
                _selectedRegistrationRequest.Id,
                _currentAdmin.Id,
                assignedRole);

            if (!requestWasApproved)
            {
                ShowRegistrationFormError("The request has already been processed or the email is already in use.");
                return;
            }

            HideRegistrationRequestDetails();
            ShowRegistrationStatusMessage("Request approved. The user can now sign in.");
            await LoadRegistrationRequestsAsync();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            ShowRegistrationStatusMessage("The request could not be processed. Check the database connection.");
        }
        finally
        {
            SetRegistrationActionButtonsEnabled(true);
        }
    }

    private async void DeclineRegistrationRequestButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRegistrationRequest is null)
        {
            return;
        }

        var reason = RegistrationDeclineReasonTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            ShowRegistrationDeclineReasonError("Enter a decline reason.");
            return;
        }

        HideRegistrationDeclineReasonError();
        HideRegistrationFormError();
        SetRegistrationActionButtonsEnabled(false);

        try
        {
            var requestWasDeclined = await _registrationRequestService.DeclineAsync(
                _selectedRegistrationRequest.Id,
                _currentAdmin.Id,
                reason);

            if (!requestWasDeclined)
            {
                ShowRegistrationFormError("The request is no longer pending. Refresh the list and choose another one.");
                return;
            }

            HideRegistrationRequestDetails();
            ShowRegistrationStatusMessage("Request declined.");
            await LoadRegistrationRequestsAsync();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            ShowRegistrationStatusMessage("The request could not be processed. Check the database connection.");
        }
        finally
        {
            SetRegistrationActionButtonsEnabled(true);
        }
    }

    private async void RefreshRegistrationRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        HideRegistrationStatusMessage();
        await LoadRegistrationRequestsAsync();
    }

    private void CloseRegistrationRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        HideRegistrationRequestsPanel();
    }

    private void ShowTicketDetails(Ticket ticket, bool animate = true)
    {
        _selectedTicket = ticket;
        DetailsStatusText.Text = FormatEnum(ticket.Status);
        DetailsAuthorNameText.Text = $"{ticket.Author.FirstName} {ticket.Author.LastName}".Trim();
        DetailsAuthorEmailText.Text = ticket.Author.Email;
        DetailsTitleText.Text = ticket.Title;
        DetailsPriorityText.Text = FormatEnum(ticket.Priority);
        DetailsCreatedText.Text = ticket.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        DetailsProblemTypeText.Text = FormatEnum(ticket.ProblemType);
        DetailsWorkImpactText.Text = FormatEnum(ticket.WorkImpact);
        DetailsAffectedPeopleText.Text = FormatEnum(ticket.AffectedPeople);
        DetailsDescriptionText.Text = ticket.Description;
        DeclineReasonTextBox.Clear();
        HideActionMessage();
        HideTicketDeclineReasonError();

        var isOpen = ticket.Status == TicketStatus.Open;
        OpenTicketActionsPanel.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
        AssignedTicketInfo.Visibility = isOpen ? Visibility.Collapsed : Visibility.Visible;
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
        TicketDetailsPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
        slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(150)));
    }

    private void HideTicketDetails()
    {
        _selectedTicket = null;

        if (TicketDetailsPanel.Visibility != Visibility.Visible)
        {
            return;
        }

        var slide = TicketDetailsPanel.RenderTransform as TranslateTransform ?? new TranslateTransform();
        TicketDetailsPanel.RenderTransform = slide;
        var fadeOut = new DoubleAnimation(TicketDetailsPanel.Opacity, 0, TimeSpan.FromMilliseconds(120));
        fadeOut.Completed += (_, _) => TicketDetailsPanel.Visibility = Visibility.Collapsed;
        TicketDetailsPanel.BeginAnimation(OpacityProperty, fadeOut);
        slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(slide.X, 24, TimeSpan.FromMilliseconds(120)));
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
            if (!await _ticketService.TakeTicketAsync(_selectedTicket.Id, _currentAdmin.Id))
            {
                ShowActionMessage("This ticket is no longer available. Refresh the list and choose another one.");
                return;
            }

            await LoadTicketsAsync();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            ShowActionMessage("The ticket could not be taken. Please try again.");
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
            ShowTicketDeclineReasonError("Enter a decline reason.");
            return;
        }

        HideTicketDeclineReasonError();
        SetActionButtonsEnabled(false);

        try
        {
            if (!await _ticketService.DeclineTicketAsync(_selectedTicket.Id, _currentAdmin.Id, reason))
            {
                ShowActionMessage("This ticket is no longer available. Refresh the list and choose another one.");
                return;
            }

            HideTicketDetails();
            await LoadTicketsAsync();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            ShowActionMessage("The ticket could not be declined. Please try again.");
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

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.OpenLogin(this);
    }

    private void SetActionButtonsEnabled(bool isEnabled)
    {
        TakeTicketButton.IsEnabled = isEnabled;
        DeclineTicketButton.IsEnabled = isEnabled;
    }

    private void SetRegistrationActionButtonsEnabled(bool isEnabled)
    {
        ApproveRegistrationRequestButton.IsEnabled = isEnabled;
        DeclineRegistrationRequestButton.IsEnabled = isEnabled;
        AssignedRoleComboBox.IsEnabled = isEnabled;
    }

    private void ShowActionMessage(string message)
    {
        TicketActionMessageText.Text = message;
        TicketActionMessageText.Visibility = Visibility.Visible;
    }

    private void HideActionMessage()
    {
        TicketActionMessageText.Text = string.Empty;
        TicketActionMessageText.Visibility = Visibility.Collapsed;
    }

    private void ShowWorkspaceStatusMessage(string message)
    {
        WorkspaceStatusMessageText.Text = message;
        WorkspaceStatusMessageText.Visibility = Visibility.Visible;
    }

    private void HideWorkspaceStatusMessage()
    {
        WorkspaceStatusMessageText.Text = string.Empty;
        WorkspaceStatusMessageText.Visibility = Visibility.Collapsed;
    }

    private void ShowRegistrationFormError(string message)
    {
        RegistrationFormErrorText.Text = message;
        RegistrationFormErrorText.Visibility = Visibility.Visible;
    }

    private void HideRegistrationFormError()
    {
        RegistrationFormErrorText.Text = string.Empty;
        RegistrationFormErrorText.Visibility = Visibility.Collapsed;
    }

    private void ShowTicketDeclineReasonError(string message)
    {
        TicketDeclineReasonErrorText.Text = message;
        TicketDeclineReasonErrorText.Visibility = Visibility.Visible;
    }

    private void HideTicketDeclineReasonError()
    {
        TicketDeclineReasonErrorText.Text = string.Empty;
        TicketDeclineReasonErrorText.Visibility = Visibility.Collapsed;
    }

    private void ShowRegistrationDeclineReasonError(string message)
    {
        RegistrationDeclineReasonErrorText.Text = message;
        RegistrationDeclineReasonErrorText.Visibility = Visibility.Visible;
    }

    private void HideRegistrationDeclineReasonError()
    {
        RegistrationDeclineReasonErrorText.Text = string.Empty;
        RegistrationDeclineReasonErrorText.Visibility = Visibility.Collapsed;
    }

    private void ShowRegistrationStatusMessage(string message)
    {
        RegistrationStatusMessageText.Text = message;
        RegistrationStatusMessageText.Visibility = Visibility.Visible;
    }

    private void HideRegistrationStatusMessage()
    {
        RegistrationStatusMessageText.Text = string.Empty;
        RegistrationStatusMessageText.Visibility = Visibility.Collapsed;
    }

    private void DeclineReasonTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            HideTicketDeclineReasonError();
        }
    }

    private void RegistrationDeclineReasonTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            HideRegistrationDeclineReasonError();
        }
    }

    private static string GetTicketCountText(int count)
    {
        return count == 1 ? "1 ticket" : $"{count} tickets";
    }

    private static string GetRegistrationRequestCountText(int count)
    {
        return count == 1 ? "1 pending request" : $"{count} pending requests";
    }

    private static bool IsDatabaseFailure(Exception exception)
    {
        return exception is DbUpdateException or NpgsqlException;
    }

    private static string FormatEnum(Enum value)
    {
        return Regex.Replace(value.ToString(), "([a-z])([A-Z])", "$1 $2");
    }
}