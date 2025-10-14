// Services/IReminderService.cs
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Services;

public record ReminderItem(int Id, string FullName, System.DateTime When);

public interface IReminderService
{
    Task<IReadOnlyList<ReminderItem>> LoadTodaysAsync();
}
