using MyOrganizer.Wpf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Repository
{
    public interface IToothWorkRepository
    {
        Task<List<ToothWork>> GetByClientAsync(int clientId);
        Task AddAsync(int clientId, string toothFdi, string procedure, string tier, int price);
        Task ClearToothAsync(int clientId, string toothFdi);
    }

}
