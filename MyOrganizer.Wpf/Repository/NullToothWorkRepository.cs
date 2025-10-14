using MyOrganizer.Wpf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Repository
{
    public sealed class NullToothWorkRepository 
    {
        public Task<List<ToothWork>> GetByClientAsync(int clientId)
            => Task.FromResult(new List<ToothWork>());

        public Task AddAsync(int clientId, string toothFdi, string procedure, string tier, int price)
            => Task.CompletedTask;

        public Task ClearToothAsync(int clientId, string toothFdi)
            => Task.CompletedTask;
    }

}
