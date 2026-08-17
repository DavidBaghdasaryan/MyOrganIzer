using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Repository;

public interface IToothWorkRepository
{
    Task<List<ToothWork>> GetByClientAsync(int clientId);
    Task AddAsync(int clientId, string toothFdi, string procedure, string tier, int price, string? surface = null);
    Task ClearToothAsync(int clientId, string toothFdi);
    Task ClearSurfacesAsync(int clientId, string toothFdi, IEnumerable<string> surfaces);
}
