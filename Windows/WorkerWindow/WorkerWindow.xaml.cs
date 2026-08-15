using System.Windows;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Services;

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

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.OpenLogin(this);
    }
}
