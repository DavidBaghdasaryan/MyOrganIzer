using System.Collections.ObjectModel;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Infrastructure;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    public string Title => "Dashboard".T();
    public string Welcome => "ShellWelcome".T();
    public string Hint => "ShellWelcomeHint".T();
    public string AppointmentsTitle => "TodayAppointments".T();
    public string EmptyAppointments => "NoRemindersToday".T();

    public ObservableCollection<string> Appointments { get; } = [];

    public bool HasAppointments => Appointments.Count > 0;
    public bool IsEmpty => Appointments.Count == 0;

    public void SetAppointments(IEnumerable<ReminderItem> items)
    {
        Appointments.Clear();
        foreach (var item in items.OrderBy(i => i.When))
            Appointments.Add($"{item.FullName}  ·  {item.When:HH:mm}");
        OnPropertyChanged(nameof(HasAppointments));
        OnPropertyChanged(nameof(IsEmpty));
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Welcome));
        OnPropertyChanged(nameof(Hint));
        OnPropertyChanged(nameof(AppointmentsTitle));
        OnPropertyChanged(nameof(EmptyAppointments));
    }
}
