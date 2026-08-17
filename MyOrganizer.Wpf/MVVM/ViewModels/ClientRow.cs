using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.MVVM.Infrastructure;

namespace MyOrganizer.Wpf.MVVM.ViewModels;

public sealed class ClientRow : ObservableObject
{
    public ClientRow(Client entity)
    {
        Entity = entity;
    }

    public Client Entity { get; }

    public int Id => Entity.Id;

    public string FullName => string.Join(" ", new[]
    {
        Entity.LastName,
        Entity.FirstName,
        Entity.MidlName
    }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public string Phone => Entity.PhoneNumber ?? "";
    public DateTime DateJoin => Entity.DateJoin;
    public DateTime? Reminder => Entity.DateDobleJoin is { } d && d > DateTime.MinValue ? d : null;
    public decimal? Price => Entity.Price;
    public decimal? Debet => Entity.Debet;
    public bool HasReminderToday => IsToday(Entity.DateJoin) || (Reminder is DateTime r && IsToday(r));

    private static bool IsToday(DateTime value) => value.Date == DateTime.Today;
}
