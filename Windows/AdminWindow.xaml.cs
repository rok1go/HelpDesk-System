using System.Windows;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Services;

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

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.OpenLogin(this);
    }
}
