using System.Windows;
using System.Windows.Media.Animation;
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
        SwitchWindow(currentWindow, _services.GetRequiredService<RegisterWindow>(), true);
    }

    public void OpenLogin(Window currentWindow)
    {
        SwitchWindow(currentWindow, _services.GetRequiredService<LoginWindow>(), currentWindow is RegisterWindow);
    }

    public void OpenWorkspace(Window currentWindow, User user)
    {
        Window nextWindow = user.Role == UserRole.Admin
            ? ActivatorUtilities.CreateInstance<AdminWindow>(_services, user)
            : ActivatorUtilities.CreateInstance<WorkerWindow>(_services, user);

        SwitchWindow(currentWindow, nextWindow, false);
    }

    private static void SwitchWindow(Window currentWindow, Window nextWindow, bool preserveSize)
    {
        if (preserveSize)
        {
            nextWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            nextWindow.Left = currentWindow.Left;
            nextWindow.Top = currentWindow.Top;
            nextWindow.Width = currentWindow.ActualWidth;
            nextWindow.Height = currentWindow.ActualHeight;
        }

        nextWindow.Opacity = 0;
        Application.Current.MainWindow = nextWindow;
        nextWindow.Show();

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160));
        fadeIn.Completed += (_, _) => currentWindow.Close();
        nextWindow.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    }
}