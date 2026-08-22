using System.Windows;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using HelpDesk_System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDesk_System.Services;

public class WindowNavigationService
{
    private readonly IServiceProvider _services;

    public WindowNavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public void OpenRegister(Window currentWindow)
    {
        var registerWindow = _services.GetRequiredService<RegisterWindow>();
        SwitchContent(currentWindow, registerWindow, resizeHost: false);
    }

    public void OpenLogin(Window currentWindow)
    {
        var loginWindow = _services.GetRequiredService<LoginWindow>();
        var resizeHost = currentWindow is not RegisterWindow;

        SwitchContent(currentWindow, loginWindow, resizeHost);
    }

    public void OpenWorkspace(Window currentWindow, User user)
    {
        Window workspaceWindow = user.Role == UserRole.Admin
            ? ActivatorUtilities.CreateInstance<AdminWindow>(_services, user)
            : ActivatorUtilities.CreateInstance<WorkerWindow>(_services, user);

        SwitchContent(currentWindow, workspaceWindow, resizeHost: true);
    }

    private void SwitchContent(
        Window currentWindow,
        Window nextWindow,
        bool resizeHost)
    {
        var hostWindow = Application.Current.MainWindow ?? currentWindow;
        var nextContent = nextWindow.Content;

        nextWindow.Content = null;

        if (resizeHost)
        {
            ApplyWindowLayout(hostWindow, nextWindow);
        }

        hostWindow.Title = nextWindow.Title;
        hostWindow.Content = nextContent;
    }

    private static void ApplyWindowLayout(Window hostWindow, Window sourceWindow)
    {
        var centerX = hostWindow.Left + hostWindow.ActualWidth / 2;
        var centerY = hostWindow.Top + hostWindow.ActualHeight / 2;

        hostWindow.Style = sourceWindow.Style;
        hostWindow.Width = sourceWindow.Width;
        hostWindow.Height = sourceWindow.Height;
        hostWindow.Left = centerX - sourceWindow.Width / 2;
        hostWindow.Top = centerY - sourceWindow.Height / 2;
    }
}
